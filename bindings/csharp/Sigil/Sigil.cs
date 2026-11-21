// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Argosy.Sigil;

/// <summary>
/// Derives the platform-native title id from a ROM file and resolves, collects and restores the save unit an emulator
/// keeps for it. docs/quickstart-guides/csharp.md defines every input.
/// </summary>
public static unsafe class Sigil
{
    private const int SubdirListDepth = 12;
    private const int BaseSearchDepth = 5;
    private const int PathCap = 4096;

    /// <summary>The sigil C library version.</summary>
    public static string Version()
    {
        return Interop.Text(Native.sigil_version());
    }

    /// <summary>Parses a platform slug; an unknown slug gives <c>Platform.Auto</c>.</summary>
    public static Platform PlatformFromSlug(string slug)
    {
        return (Platform)Native.sigil_platform_from_slug(slug);
    }

    /// <summary>Reads the 32-byte Switch header key from a prod.keys file.</summary>
    public static byte[] LoadHeaderKeyFromProdKeys(string path)
    {
        byte[] key = new byte[32];
        fixed (byte* p = key)
        {
            Interop.Check(Native.sigil_load_header_key_from_prod_keys(path, p));
        }
        return key;
    }

    /// <summary>
    /// Reads the title id from <paramref name="path"/>. <c>Platform.Auto</c> sniffs the platform from the file
    /// extension; a bare .iso or .zip needs one. Throws when nothing identified the file.
    /// </summary>
    public static SigilResult Extract(string path, Platform platform = Platform.Auto, ExtractOptions? options = null)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        using var a = new Allocs();
        var coptions = (NativeOptions*)a.Alloc((nuint)sizeof(NativeOptions));
        coptions->struct_version = Native.OPTIONS_V1;
        if (options is null)
        {
            coptions->flags = Native.FLAG_FILENAME_FALLBACK;
        }
        else
        {
            if (options.FilenameFallback)
            {
                coptions->flags |= Native.FLAG_FILENAME_FALLBACK;
            }
            if (options.Allow3DSHomebrew)
            {
                coptions->flags |= Native.FLAG_3DS_ALLOW_HOMEBREW;
            }
            byte[]? headerKey = options.SwitchHeaderKey;
            string? keysPath = options.SwitchProdKeysPath;
            byte[]? keysText = string.IsNullOrEmpty(options.SwitchProdKeysText)
                ? null
                : Encoding.UTF8.GetBytes(options.SwitchProdKeysText);
            bool hasKey = headerKey is { Length: > 0 };
            bool hasPath = !string.IsNullOrEmpty(keysPath);
            bool hasText = keysText is { Length: > 0 };
            if (hasKey || hasPath || hasText)
            {
                var support = (NativeSupport*)a.Alloc((nuint)sizeof(NativeSupport));
                support->struct_version = Native.SUPPORT_V1;
                if (hasKey)
                {
                    if (headerKey!.Length != 32)
                    {
                        throw new SigilInvalidArgException(SigilException.INVALID_ARG,
                            $"SwitchHeaderKey must be 32 bytes, got {headerKey.Length}");
                    }
                    support->switch_header_key = a.Bytes(headerKey);
                }
                if (hasPath)
                {
                    support->switch_prod_keys_path = a.Str(keysPath!);
                }
                if (hasText)
                {
                    support->switch_prod_keys_text = a.Bytes(keysText!);
                    support->switch_prod_keys_text_len = (nuint)keysText!.Length;
                }
                coptions->support = support;
            }
        }

        NativeResult result = default;
        result.struct_version = Native.RESULT_V4;
        Interop.Check(Native.sigil_extract_from_path(path, (int)platform, coptions, &result));
        return ResultFrom(&result);
    }

    private static SigilResult ResultFrom(NativeResult* r)
    {
        var platform = (Platform)r->platform;
        return new SigilResult
        {
            TitleId = Interop.Text(r->title_id),
            RawSerial = Interop.Text(r->raw_serial),
            SaveId = Interop.Text(r->save_id),
            Platform = platform,
            PlatformSlug = platform.Slug(),
            Source = (Source)r->source,
            Usage = (Usage)r->usage,
            Experimental = r->experimental != 0,
            SwitchContentType = (SwitchContentType)r->switch_content_type,
            TitleVersion = r->title_version,
            Features = (Features)r->features,
            N64Header = Interop.Text(r->n64_header),
            N64Md5 = Interop.Text(r->n64_md5),
            N64Md5N64 = Interop.Text(r->n64_md5_n64),
        };
    }

    /// <summary>
    /// Rebuilds a result from stored columns, or builds one for a platform that has no title id.
    /// <paramref name="platformSlug"/> selects the save layout. <paramref name="rawSerial"/> names pcsx_rearmed's
    /// per-disc cards, which follow the boot file as written; the <c>n64*</c> values name the standalone N64
    /// emulators' saves.
    /// </summary>
    public static SigilResult PersistedResult(string platformSlug, string titleId, string saveId, Features features,
        string rawSerial = "", string n64Header = "", string n64Md5 = "", string n64Md5N64 = "")
    {
        return new SigilResult
        {
            TitleId = titleId,
            RawSerial = rawSerial,
            SaveId = saveId,
            Platform = PlatformFromSlug(platformSlug),
            PlatformSlug = platformSlug,
            Features = features,
            N64Header = n64Header,
            N64Md5 = n64Md5,
            N64Md5N64 = n64Md5N64,
        };
    }

    /// <summary>The base name RetroArch derives for save files: the loaded path's file name without its extension.</summary>
    public static string ContentStem(string contentPath)
    {
        byte[] buffer = new byte[Native.SAVE_ENTRY_MAX];
        fixed (byte* p = buffer)
        {
            Native.sigil_content_stem(contentPath, p, (nuint)buffer.Length);
            return Interop.Text(p);
        }
    }

    /// <summary>The subfolders under the save root a layout writes into, so the caller knows what to list.</summary>
    public static IReadOnlyList<string> LayoutSubdirs(string layout)
    {
        const int cap = 16;
        byte** subdirs = stackalloc byte*[cap];
        int n = Math.Min((int)Native.sigil_save_layout_subdirs(layout, subdirs, cap), cap);
        var result = new List<string>(n);
        for (int i = 0; i < n; i++)
        {
            result.Add(Interop.Text(subdirs[i]));
        }
        return result;
    }

    private static string LayoutTop(string layout)
    {
        return Interop.Text(Native.sigil_save_layout_top(layout));
    }

    /// <summary>
    /// On a layout with profiles, the emulator's base folder for <paramref name="path"/> and the profile folder the path
    /// lies in ("" for none); <c>Collect</c>, <c>Restore</c> and <c>LocateSaves</c> re-root there themselves. On other
    /// layouts, <paramref name="path"/> and "".
    /// </summary>
    public static (string Base, string Profile) SaveBase(string layout, string path)
    {
        byte[] baseBuffer = new byte[PathCap];
        byte[] profileBuffer = new byte[Native.PROFILE_ID_MAX];
        fixed (byte* b = baseBuffer)
        fixed (byte* p = profileBuffer)
        {
            Interop.Check(Native.sigil_save_base(layout, path, b, (nuint)baseBuffer.Length, p,
                (nuint)profileBuffer.Length));
            return (Interop.Text(b), Interop.Text(p));
        }
    }

    // The folder to list and write under for saveRoot, and the profile: one given wins over the one saveRoot lies in.
    private static (string Root, string Profile) Rooted(string core, string saveRoot, string? profile)
    {
        (string root, string implied) = SaveBase(core, saveRoot);
        return (root, string.IsNullOrEmpty(profile) ? implied : profile!);
    }

    /// <summary>The layout rows for <paramref name="platform"/> (a slug, aliases accepted), or every row when it is null.</summary>
    public static IReadOnlyList<SigilLayout> Layouts(string? platform = null)
    {
        NativeLayoutInfo* infos = null;
        nuint count = 0;
        Interop.Check(Native.sigil_layouts(platform, &infos, &count));
        try
        {
            var result = new List<SigilLayout>((int)count);
            for (int i = 0; i < (int)count; i++)
            {
                NativeLayoutInfo* info = infos + i;
                var options = new List<SigilLayoutOption>();
                for (int j = 0; j < (int)info->option_count; j++)
                {
                    NativeLayoutOption* option = (NativeLayoutOption*)info->options + j;
                    var values = new List<string>();
                    for (int k = 0; k < (int)option->value_count; k++)
                    {
                        values.Add(Interop.Text(option->values + k * Native.LAYOUT_OPTION_VALUE_LEN));
                    }
                    options.Add(new SigilLayoutOption
                    {
                        Key = Interop.Text(option->key),
                        Values = values,
                        DefaultValue = Interop.Text(option->default_value),
                    });
                }
                result.Add(new SigilLayout
                {
                    Id = Interop.Text(info->id),
                    Platform = Interop.Text(info->platform),
                    Options = options,
                    RegionOption = Interop.Text(info->region_option),
                    Profiles = info->profiles != 0,
                    NeedsExisting = info->needs_existing != 0,
                });
            }
            return result;
        }
        finally
        {
            Native.sigil_layouts_free(infos);
        }
    }

    // ---- Listing the save root -------------------------------------------------------------------------------------

    private static string[] EntryNames(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }
        try
        {
            string[] names = Directory.GetFileSystemEntries(directory);
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = Path.GetFileName(names[i]);
            }
            Array.Sort(names, StringComparer.Ordinal);
            return names;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            throw new SigilIOException(SigilException.IO, e.Message);
        }
    }

    // Says whether the entry is a folder or a regular file, following a symbolic link to what it points at, as the
    // Python, Go and Kotlin listings do. A link that points nowhere is neither.
    private static void EntryKind(string directory, string name, out bool isDirectory, out bool isFile)
    {
        string path = Path.Combine(directory, name);
        isDirectory = Directory.Exists(path);
        isFile = !isDirectory && File.Exists(path);
    }

    // The root-relative folders under root that hold the layout's top folder.
    private static List<string> BasesBelow(string root, string top)
    {
        var found = new List<string>();
        void Walk(string directory, string relative, int depth)
        {
            if (depth == 0)
            {
                return;
            }
            foreach (string name in EntryNames(directory))
            {
                EntryKind(directory, name, out bool isDirectory, out _);
                if (!isDirectory)
                {
                    continue;
                }
                if (name == top)
                {
                    if (relative.Length > 0)
                    {
                        found.Add(relative);
                    }
                    continue;
                }
                Walk(Path.Combine(directory, name), relative.Length > 0 ? relative + "/" + name : name, depth - 1);
            }
        }
        Walk(root, "", BaseSearchDepth);
        return found;
    }

    private static void ListRecursive(string directory, string relative, int depth, List<string> output)
    {
        if (depth == 0)
        {
            return;
        }
        foreach (string name in EntryNames(directory))
        {
            string rel = relative + "/" + name;
            EntryKind(directory, name, out bool isDirectory, out bool isFile);
            if (isDirectory)
            {
                ListRecursive(Path.Combine(directory, name), rel, depth - 1, output);
            }
            else if (isFile)
            {
                output.Add(rel);
            }
        }
    }

    /// <summary>
    /// The root-relative paths of the files directly in <paramref name="root"/> plus those under the layout's
    /// subfolders. On a layout with profiles whose base sits below root, the subfolders under each such base.
    /// </summary>
    public static IReadOnlyList<string> ListSaveRoot(string root, string layout)
    {
        var output = new List<string>();
        foreach (string name in EntryNames(root))
        {
            EntryKind(root, name, out _, out bool isFile);
            if (isFile)
            {
                output.Add(name);
            }
        }
        var bases = new List<string> { "" };
        string top = LayoutTop(layout);
        if (top.Length > 0 && !Directory.Exists(Path.Combine(root, top)))
        {
            List<string> found = BasesBelow(root, top);
            if (found.Count > 0)
            {
                bases = found;
            }
        }
        foreach (string baseFolder in bases)
        {
            foreach (string subdir in LayoutSubdirs(layout))
            {
                string relative = baseFolder.Length > 0 ? baseFolder + "/" + subdir : subdir;
                ListRecursive(Path.Combine(root, relative), relative, SubdirListDepth, output);
            }
        }
        return output;
    }

    // ---- Saves ---------------------------------------------------------------------------------------------------

    // Fills req for game running under core; every buffer it points at is owned by a.
    private static void FillSaveRequest(Allocs a, NativeSaveRequest* req, SigilResult game, string core,
        string contentPath, IReadOnlyList<string> listing, IReadOnlyDictionary<string, string>? options, string root,
        string profile)
    {
        if (root.Length > 0)
        {
            req->root_path = a.Str(root);
        }
        if (profile.Length > 0)
        {
            req->profile = a.Str(profile);
        }
        var cresult = (NativeResult*)a.Alloc((nuint)sizeof(NativeResult));
        cresult->struct_version = Native.RESULT_V4;
        cresult->features = (uint)game.Features;
        cresult->platform = (int)game.Platform;
        Interop.CopyChars(cresult->title_id, 32, game.TitleId);
        Interop.CopyChars(cresult->raw_serial, 32, game.RawSerial);
        Interop.CopyChars(cresult->save_id, 32, game.SaveId);
        Interop.CopyChars(cresult->n64_header, 24, game.N64Header);
        Interop.CopyChars(cresult->n64_md5, 33, game.N64Md5);
        Interop.CopyChars(cresult->n64_md5_n64, 33, game.N64Md5N64);

        req->struct_version = Native.SAVE_REQUEST_V1;
        req->layout = a.Str(core);
        if (game.PlatformSlug.Length > 0)
        {
            req->platform = a.Str(game.PlatformSlug);
        }
        req->content_path = a.Str(contentPath);
        req->result = cresult;
        req->features = (uint)game.Features;

        var keys = new List<string>(options?.Keys ?? (IEnumerable<string>)Array.Empty<string>());
        keys.Sort(StringComparer.Ordinal);
        var coptions = (NativeSaveOption*)a.Alloc((nuint)(keys.Count + 1) * (nuint)sizeof(NativeSaveOption));
        for (int i = 0; i < keys.Count; i++)
        {
            coptions[i].key = a.Str(keys[i]);
            coptions[i].value = a.Str(options![keys[i]]);
        }
        req->options = coptions;
        req->option_count = (nuint)keys.Count;
        req->listing = a.Strings(listing);
        req->listing_count = (nuint)listing.Count;
    }

    private static IReadOnlyList<SigilSaveMember> Members(NativeSaveMember* members, nuint count)
    {
        var result = new List<SigilSaveMember>((int)count);
        for (int i = 0; i < (int)count; i++)
        {
            NativeSaveMember* m = members + i;
            result.Add(new SigilSaveMember
            {
                Path = Interop.Text(m->path),
                Entry = Interop.Text(m->entry),
                Role = (SaveRole)m->role,
                Present = m->present != 0,
                Area = (SaveArea)m->area,
            });
        }
        return result;
    }

    private static IReadOnlyList<SigilSaveAlternate> Alternates(NativeSaveAlternate* alternates, nuint count)
    {
        var result = new List<SigilSaveAlternate>((int)count);
        for (int i = 0; i < (int)count; i++)
        {
            NativeSaveAlternate* alternate = alternates + i;
            NativeSaveOption* options = &alternate->options0; // options1 follows it, as in the C array
            var values = new Dictionary<string, string>();
            for (int j = 0; j < (int)alternate->option_count; j++)
            {
                values[Interop.Text(options[j].key)] = Interop.Text(options[j].value);
            }
            result.Add(new SigilSaveAlternate
            {
                Path = Interop.Text(alternate->path),
                Shared = alternate->shared != 0,
                Options = values,
            });
        }
        return result;
    }

    /// <summary>
    /// The files under a save root that belong to <paramref name="game"/> when <paramref name="core"/> runs
    /// <paramref name="contentPath"/>. No save is read; on a layout with profiles the emulator's profile list is, when
    /// <c>SaveRoot</c> is given. The hashes are filled by <c>HashSaves</c>.
    /// </summary>
    public static SigilSaveUnit LocateSaves(SigilResult game, string core, string contentPath,
        LocateOptions? options = null)
    {
        if (game is null)
        {
            throw Interop.Error(SigilException.INVALID_ARG);
        }
        options ??= new LocateOptions();
        string root = "";
        string profile = options.Profile ?? "";
        if (!string.IsNullOrEmpty(options.SaveRoot))
        {
            (root, profile) = Rooted(core, options.SaveRoot!, profile);
        }
        IReadOnlyList<string>? listing = options.Listing;
        if (listing is null && root.Length > 0)
        {
            listing = ListSaveRoot(root, core);
        }

        using var a = new Allocs();
        using var saveRoot = new SaveRoot(root);
        var request = (NativeSaveRequest*)a.Alloc((nuint)sizeof(NativeSaveRequest));
        FillSaveRequest(a, request, game, core, contentPath, listing ?? Array.Empty<string>(), options.Options, root,
            profile);
        if (root.Length > 0 && LayoutTop(core).Length > 0)
        {
            request->open = SaveRootIo.Open;
            request->open_ctx = saveRoot.Context;
        }

        NativeSaveUnit* cunit = null;
        Interop.Check(Native.sigil_save_resolve(request, &cunit));
        try
        {
            var unkeyed = new List<string>();
            for (int i = 0; i < (int)cunit->unkeyed_count; i++)
            {
                unkeyed.Add(Interop.Text(cunit->unkeyed + i * Native.SAVE_PATH_MAX));
            }
            return new SigilSaveUnit
            {
                Key = Interop.Text(cunit->key),
                Shape = (SaveShape)cunit->shape,
                Members = Members(cunit->members, cunit->member_count),
                Expected = Members(cunit->expected, cunit->expected_count),
                Unkeyed = unkeyed,
                Artifact = Interop.Text(cunit->artifact),
                ContentHash = Interop.Text(cunit->content_hash),
                IdentityHash = Interop.Text(cunit->identity_hash),
                Alternates = Alternates(cunit->alternates, cunit->alternate_count),
            };
        }
        finally
        {
            Native.sigil_save_unit_free(cunit);
        }
    }

    /// <summary>
    /// The unit with <c>ContentHash</c> and <c>IdentityHash</c> filled from the files under
    /// <paramref name="saveRoot"/>. Throws <c>SigilIOException</c> when a member cannot be opened.
    /// </summary>
    public static SigilSaveUnit HashSaves(SigilSaveUnit unit, string saveRoot)
    {
        if (unit is null || string.IsNullOrEmpty(saveRoot))
        {
            throw Interop.Error(SigilException.INVALID_ARG);
        }
        if (unit.Members.Count == 0)
        {
            return unit;
        }

        using var a = new Allocs();
        using var root = new SaveRoot(saveRoot);
        var cmembers = (NativeSaveMember*)a.Alloc((nuint)unit.Members.Count * (nuint)sizeof(NativeSaveMember));
        for (int i = 0; i < unit.Members.Count; i++)
        {
            Interop.CopyChars(cmembers[i].path, Native.SAVE_PATH_MAX, unit.Members[i].Path);
            Interop.CopyChars(cmembers[i].entry, Native.SAVE_ENTRY_MAX, unit.Members[i].Entry);
            cmembers[i].role = (int)unit.Members[i].Role;
            cmembers[i].present = 1;
        }
        var cunit = (NativeSaveUnit*)a.Alloc((nuint)sizeof(NativeSaveUnit));
        cunit->struct_version = Native.SAVE_UNIT_V1;
        cunit->shape = (int)unit.Shape;
        Interop.CopyChars(cunit->key, Native.SAVE_ENTRY_MAX, unit.Key);
        cunit->members = cmembers;
        cunit->member_count = (nuint)unit.Members.Count;

        Interop.Check(Native.sigil_save_hash(cunit, SaveRootIo.Open, root.Context));
        return unit with
        {
            ContentHash = Interop.Text(cunit->content_hash),
            IdentityHash = Interop.Text(cunit->identity_hash),
        };
    }

    /// <summary>
    /// The profiles the emulator lists around <paramref name="saveRoot"/>, for asking the user which one they play as
    /// when <c>Collect</c> or <c>Restore</c> threw <c>SigilAmbiguousException</c>. Throws
    /// <c>SigilUnsupportedFormatException</c> for a core whose saves aren't kept per profile.
    /// </summary>
    public static IReadOnlyList<SigilProfile> ListProfiles(string core, string saveRoot)
    {
        (string root, _) = Rooted(core, saveRoot, null);
        IReadOnlyList<string> listing = ListSaveRoot(root, core);

        using var a = new Allocs();
        using var saveRootIo = new SaveRoot(root);
        var request = (NativeSaveRequest*)a.Alloc((nuint)sizeof(NativeSaveRequest));
        request->struct_version = Native.SAVE_REQUEST_V1;
        request->layout = a.Str(core);
        request->listing = a.Strings(listing);
        request->listing_count = (nuint)listing.Count;
        request->root_path = a.Str(root);
        request->open = SaveRootIo.Open;
        request->open_ctx = saveRootIo.Context;

        NativeSaveProfile* profiles = null;
        nuint count = 0;
        Interop.Check(Native.sigil_save_profiles(request, &profiles, &count));
        try
        {
            return Profiles(profiles, count);
        }
        finally
        {
            Native.sigil_save_profiles_free(profiles);
        }
    }

    private static IReadOnlyList<SigilProfile> Profiles(NativeSaveProfile* profiles, nuint count)
    {
        var result = new List<SigilProfile>((int)count);
        for (int i = 0; i < (int)count; i++)
        {
            result.Add(new SigilProfile { Id = Interop.Text(profiles[i].id), Name = Interop.Text(profiles[i].name) });
        }
        return result;
    }

    // ---- Memory cards --------------------------------------------------------------------------------------------

    private static IReadOnlyList<SigilCardEntry> CardEntries(NativeCardEntry* entries, nuint count)
    {
        var result = new List<SigilCardEntry>((int)count);
        for (int i = 0; i < (int)count; i++)
        {
            result.Add(new SigilCardEntry
            {
                Name = Interop.Text(entries[i].name),
                OwnerId = Interop.Text(entries[i].owner_id),
                Blocks = entries[i].blocks,
                FirstBlock = entries[i].first_block,
            });
        }
        return result;
    }

    private static IntPtr OpenFile(string path)
    {
        IntPtr io = Native.sigil_io_open_file(path);
        if (io == IntPtr.Zero)
        {
            throw Interop.Error(SigilException.IO);
        }
        return io;
    }

    /// <summary>The saves on the memory card at <paramref name="path"/>; the card format is detected from its content.</summary>
    public static SigilCardListing ListCard(string path)
    {
        IntPtr io = OpenFile(path);
        try
        {
            NativeCardListing* listing = null;
            Interop.Check(Native.sigil_card_list(io, &listing));
            try
            {
                return new SigilCardListing
                {
                    Format = (CardFormat)listing->format,
                    TotalBlocks = listing->total_blocks,
                    FreeBlocks = listing->free_blocks,
                    FreeSlots = listing->free_slots,
                    CorruptCount = listing->corrupt_count,
                    Entries = CardEntries(listing->entries, listing->entry_count),
                    CorruptEntries = CardEntries(listing->corrupt_entries, listing->corrupt_entry_count),
                };
            }
            finally
            {
                Native.sigil_card_listing_free(listing);
            }
        }
        finally
        {
            Native.sigil_io_close(io);
        }
    }

    /// <summary>
    /// Repairs what the memory card at <paramref name="path"/> proves about itself and reports the rest as
    /// unrecoverable, for a card <c>Collect</c> refused with <c>SigilDamagedException</c>. It writes nothing: write
    /// <c>Data</c> back if you choose, keeping the original, then collect again.
    /// </summary>
    public static SigilCardRepair RepairCard(string path)
    {
        IntPtr io = OpenFile(path);
        try
        {
            NativeCardRepairReport* report = null;
            Interop.Check(Native.sigil_card_repair(io, &report));
            try
            {
                var entries = new List<SigilCardRepairEntry>((int)report->entry_count);
                for (int i = 0; i < (int)report->entry_count; i++)
                {
                    NativeCardRepairEntry* e = report->entries + i;
                    entries.Add(new SigilCardRepairEntry
                    {
                        Name = Interop.Text(e->name),
                        OwnerId = Interop.Text(e->owner_id),
                        FirstBlock = e->first_block,
                        Damage = (CardDamage)e->damage,
                        Repaired = e->repaired != 0,
                    });
                }
                return new SigilCardRepair
                {
                    Format = (CardFormat)report->format,
                    Data = Interop.CopyBytes(report->data, report->len),
                    Entries = entries,
                };
            }
            finally
            {
                Native.sigil_card_repair_report_free(report);
            }
        }
        finally
        {
            Native.sigil_io_close(io);
        }
    }

    // ---- Sync ----------------------------------------------------------------------------------------------------

    private static SigilSyncResult RunSync(byte[]? unit, SigilResult game, string core, string contentPath,
        string saveRoot, SyncOptions? options)
    {
        if (game is null || string.IsNullOrEmpty(saveRoot))
        {
            throw Interop.Error(SigilException.INVALID_ARG);
        }
        options ??= new SyncOptions();
        (string root, string profile) = Rooted(core, saveRoot, options.Profile);
        IReadOnlyList<string> listing = options.Listing ?? ListSaveRoot(root, core);
        IReadOnlyList<string> gameIds = options.GameIds ?? Array.Empty<string>();
        IReadOnlyList<string> claimed = options.Claimed ?? Array.Empty<string>();

        using var a = new Allocs();
        using var saveRootIo = new SaveRoot(root);
        var request = (NativeSyncRequest*)a.Alloc((nuint)sizeof(NativeSyncRequest));
        request->struct_version = Native.SYNC_REQUEST_V2;
        FillSaveRequest(a, &request->save, game, core, contentPath, listing, options.Options, root, profile);
        request->game_ids = a.Strings(gameIds);
        request->game_id_count = (nuint)gameIds.Count;
        request->claimed = a.Strings(claimed);
        request->claimed_count = (nuint)claimed.Count;
        if (options.Companions is { Count: > 0 } companions)
        {
            var slots = (NativeSyncCompanion*)a.Alloc((nuint)companions.Count * (nuint)sizeof(NativeSyncCompanion));
            for (int i = 0; i < companions.Count; i++)
            {
                slots[i].game_ids = a.Strings(companions[i].GameIds);
                slots[i].game_id_count = (nuint)companions[i].GameIds.Count;
                if (companions[i].Unit is { } companionUnit)
                {
                    slots[i].unit = a.Bytes(companionUnit);
                    slots[i].unit_len = (nuint)companionUnit.Length;
                }
            }
            request->companions = slots;
            request->companion_count = (nuint)companions.Count;
        }
        request->mode = options.Unmanaged ? Native.SIGIL_SYNC_UNMANAGED : Native.SIGIL_SYNC_MANAGED;
        request->overwrite_local = options.OverwriteLocal ? 1 : 0;
        request->repair = options.Repair ? 1 : 0;
        if (options.State is { Length: > 0 } state)
        {
            request->state = a.Bytes(state);
            request->state_len = (nuint)state.Length;
        }
        request->save.open = SaveRootIo.Open;
        request->save.open_ctx = saveRootIo.Context;
        request->write = SaveRootIo.Write;
        request->write_at = options.WritesParts ? SaveRootIo.WriteAt : IntPtr.Zero;
        request->remove = SaveRootIo.Remove;
        request->write_ctx = saveRootIo.Context;

        NativeSyncResult* result = null;
        int rc;
        if (unit is null)
        {
            rc = Native.sigil_collect(request, &result);
        }
        else
        {
            rc = Native.sigil_restore(request, a.Bytes(unit), (nuint)unit.Length, &result);
        }
        try
        {
            if (rc != 0)
            {
                SigilException error = SigilException.FromCode(rc);
                if (result != null)
                {
                    error.Problem = Interop.Text(result->problem);
                    error.Profiles = Profiles(result->profiles, result->profile_count);
                    if (error is SigilNoSpaceException noSpace)
                    {
                        noSpace.BlocksShort = result->blocks_short;
                    }
                }
                throw error;
            }
            return SyncResultFrom(result);
        }
        finally
        {
            if (result != null)
            {
                Native.sigil_sync_result_free(result);
            }
        }
    }

    private static SigilSyncResult SyncResultFrom(NativeSyncResult* r)
    {
        var unowned = new List<string>((int)r->unowned_count);
        for (int i = 0; i < (int)r->unowned_count; i++)
        {
            unowned.Add(Interop.Text(r->unowned + i * Native.CARD_NAME_MAX));
        }
        var companions = new List<SigilCompanionResult>((int)r->companion_count);
        for (int i = 0; i < (int)r->companion_count; i++)
        {
            NativeSyncCompanionResult* c = r->companions + i;
            companions.Add(new SigilCompanionResult
            {
                Data = Interop.CopyBytes(c->data, c->len),
                ContentHash = Interop.Text(c->content_hash),
                IdentityHash = Interop.Text(c->identity_hash),
                Changed = c->changed != 0,
            });
        }
        return new SigilSyncResult
        {
            Artifact = Interop.Text(r->artifact),
            Shape = (SaveShape)r->shape,
            Data = Interop.CopyBytes(r->data, r->len),
            ContentHash = Interop.Text(r->content_hash),
            IdentityHash = Interop.Text(r->identity_hash),
            Changed = r->changed != 0,
            State = Interop.CopyBytes(r->state, r->state_len) ?? Array.Empty<byte>(),
            Holding = Interop.CopyBytes(r->holding, r->holding_len),
            Unowned = unowned,
            UnownedChanged = (int)r->unowned_changed,
            RestoreAgain = r->restore_again != 0,
            Companions = companions,
            Profiles = Profiles(r->profiles, r->profile_count),
            Profile = Interop.Text(r->profile),
            MakerProfile = Interop.Text(r->maker_profile),
            Alternates = Alternates(r->alternates, r->alternate_count),
            HardcoreMarker = r->hardcore_marker != 0,
        };
    }

    /// <summary>
    /// Gathers <paramref name="game"/>'s saves under <paramref name="saveRoot"/> into the unit that travels to RomM.
    /// Store the returned <c>State</c> once the unit reached RomM.
    /// </summary>
    public static SigilSyncResult Collect(SigilResult game, string core, string contentPath, string saveRoot,
        SyncOptions? options = null)
    {
        return RunSync(null, game, core, contentPath, saveRoot, options);
    }

    /// <summary>
    /// Puts <paramref name="unit"/> back under <paramref name="saveRoot"/> and reads it back. Each of these writes
    /// nothing: <c>SigilConflictException</c> when the saves there changed since the last sync and
    /// <c>OverwriteLocal</c> is false; <c>SigilUncollectedException</c> when a shared volume holds saves no collect has
    /// passed on yet; and, naming what is at fault in <c>Problem</c>, <c>SigilNoSpaceException</c>,
    /// <c>SigilRegionException</c>, <c>SigilNoTargetException</c>, <c>SigilAmbiguousException</c> and
    /// <c>SigilDamagedException</c> unless <c>Repair</c> rebuilt it (a card sigil can't read stays damaged).
    /// </summary>
    public static SigilSyncResult Restore(byte[] unit, SigilResult game, string core, string contentPath,
        string saveRoot, SyncOptions? options = null)
    {
        if (unit is null)
        {
            throw Interop.Error(SigilException.INVALID_ARG);
        }
        return RunSync(unit, game, core, contentPath, saveRoot, options);
    }
}
