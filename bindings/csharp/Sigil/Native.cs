// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CS0649 // Fields the native side fills.
#pragma warning disable IDE1006 // The structs keep include/sigil.h's names so each field can be checked against it.

namespace Argosy.Sigil;

// One struct per sigil.h struct, in the same field order. The layout is the C compiler's: every field is a
// fixed-size integer, a pointer, a size_t (nuint) or a char array, so sequential layout with natural alignment
// gives the same offsets as the header on every 64-bit platform sigil builds for.

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeResult
{
    public uint struct_version;
    public fixed byte title_id[32];
    public fixed byte raw_serial[32];
    public fixed byte save_id[32];
    public int platform;
    public int source;
    public int usage;
    public int experimental;
    public int switch_content_type;
    public uint title_version;
    public uint features;
    public fixed byte n64_header[24];
    public fixed byte n64_md5[33];
    public fixed byte n64_md5_n64[33];
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSupport
{
    public uint struct_version;
    public byte* switch_header_key;
    public byte* switch_prod_keys_path;
    public byte* switch_prod_keys_text;
    public nuint switch_prod_keys_text_len;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeOptions
{
    public uint struct_version;
    public NativeSupport* support;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSaveMember
{
    public fixed byte path[512];
    public fixed byte entry[256];
    public int role;
    public int present;
    public int area;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSaveProfile
{
    public fixed byte id[64];
    public fixed byte name[64];
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSaveOption
{
    public byte* key;
    public byte* value;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSaveAlternate
{
    public fixed byte path[512];
    public int shared;
    // sigil_save_option options[2]: two fields, because a fixed buffer holds only primitives and the
    // pointers inside need the 8-byte alignment the C array has.
    public NativeSaveOption options0;
    public NativeSaveOption options1;
    public nuint option_count;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSaveRequest
{
    public uint struct_version;
    public byte* layout;
    public byte* platform;
    public byte* content_path;
    public NativeResult* result;
    public uint features;
    public NativeSaveOption* options;
    public nuint option_count;
    public byte** listing;
    public nuint listing_count;
    public IntPtr open;
    public IntPtr open_ctx;
    public byte* root_path;
    public byte* profile;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSaveUnit
{
    public uint struct_version;
    public fixed byte key[256];
    public int shape;
    public NativeSaveMember* members;
    public nuint member_count;
    public NativeSaveMember* expected;
    public nuint expected_count;
    public byte* unkeyed; // char (*)[SIGIL_SAVE_PATH_MAX]: unkeyed_count strings of 512 bytes each
    public nuint unkeyed_count;
    public fixed byte artifact[256];
    public fixed byte content_hash[33];
    public fixed byte identity_hash[33];
    public NativeSaveAlternate* alternates;
    public nuint alternate_count;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeLayoutOption
{
    public fixed byte key[64];
    public fixed byte values[256]; // char [SIGIL_LAYOUT_OPTION_VALUES][32]
    public nuint value_count;
    public fixed byte default_value[32];
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeLayoutInfo
{
    public fixed byte id[64];
    public fixed byte platform[16];
    // sigil_layout_option options[SIGIL_LAYOUT_OPTIONS] (16): sixteen fields rather than a byte buffer, because an
    // option is 360 bytes on x64 but 356 on x86 (size_t), and a fixed buffer's size must be a constant.
    public NativeLayoutOption options0;
    public NativeLayoutOption options1;
    public NativeLayoutOption options2;
    public NativeLayoutOption options3;
    public NativeLayoutOption options4;
    public NativeLayoutOption options5;
    public NativeLayoutOption options6;
    public NativeLayoutOption options7;
    public NativeLayoutOption options8;
    public NativeLayoutOption options9;
    public NativeLayoutOption options10;
    public NativeLayoutOption options11;
    public NativeLayoutOption options12;
    public NativeLayoutOption options13;
    public NativeLayoutOption options14;
    public NativeLayoutOption options15;
    public nuint option_count;
    public fixed byte region_option[64];
    public int profiles;
    public int needs_existing;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeCardEntry
{
    public fixed byte name[64];
    public fixed byte owner_id[16];
    public uint blocks;
    public uint first_block;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeCardListing
{
    public uint struct_version;
    public int format;
    public uint total_blocks;
    public uint free_blocks;
    public uint free_slots;
    public uint corrupt_count;
    public NativeCardEntry* entries;
    public nuint entry_count;
    public NativeCardEntry* corrupt_entries;
    public nuint corrupt_entry_count;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeCardRepairEntry
{
    public fixed byte name[64];
    public fixed byte owner_id[16];
    public uint first_block;
    public int damage;
    public int repaired;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeCardRepairReport
{
    public uint struct_version;
    public int format;
    public byte* data;
    public nuint len;
    public NativeCardRepairEntry* entries;
    public nuint entry_count;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSyncCompanion
{
    public byte** game_ids;
    public nuint game_id_count;
    public byte* unit;
    public nuint unit_len;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSyncCompanionResult
{
    public byte* data;
    public nuint len;
    public fixed byte content_hash[33];
    public fixed byte identity_hash[33];
    public int changed;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSyncRequest
{
    public uint struct_version;
    public NativeSaveRequest save;
    public byte** game_ids;
    public nuint game_id_count;
    public int mode;
    public byte* state;
    public nuint state_len;
    public int overwrite_local;
    public IntPtr write;
    public IntPtr write_ctx;
    public byte** claimed;
    public nuint claimed_count;
    public IntPtr remove;
    public NativeSyncCompanion* companions;
    public nuint companion_count;
    public int repair;
    public IntPtr write_at;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeSyncResult
{
    public uint struct_version;
    public fixed byte artifact[256];
    public int shape;
    public byte* data;
    public nuint len;
    public fixed byte content_hash[33];
    public fixed byte identity_hash[33];
    public int changed;
    public int conflict;
    public byte* state;
    public nuint state_len;
    public byte* holding;
    public nuint holding_len;
    public byte* unowned; // char (*)[SIGIL_CARD_NAME_MAX]: unowned_count strings of 64 bytes each
    public nuint unowned_count;
    public int restore_again;
    public NativeSyncCompanionResult* companions;
    public nuint companion_count;
    public fixed byte problem[512];
    public uint blocks_short;
    public NativeSaveProfile* profiles;
    public nuint profile_count;
    public fixed byte profile[64];
    public NativeSaveAlternate* alternates;
    public nuint alternate_count;
    public int hardcore_marker;
    public nuint unowned_changed;
    public fixed byte maker_profile[64];
}

internal static unsafe class Native
{
    private const string Lib = "sigil";

    internal const uint RESULT_V4 = 4;
    internal const uint SAVE_REQUEST_V1 = 1;
    internal const uint SAVE_UNIT_V1 = 1;
    internal const uint SYNC_REQUEST_V2 = 2;
    internal const uint SUPPORT_V1 = 1;
    internal const uint OPTIONS_V1 = 1;

    internal const uint FLAG_FILENAME_FALLBACK = 1u << 0;
    internal const uint FLAG_3DS_ALLOW_HOMEBREW = 1u << 1;

    internal const int SIGIL_SYNC_MANAGED = 0;
    internal const int SIGIL_SYNC_UNMANAGED = 1;

    internal const int SAVE_ENTRY_MAX = 256;
    internal const int SAVE_PATH_MAX = 512;
    internal const int PROFILE_ID_MAX = 64;
    internal const int CARD_NAME_MAX = 64;
    internal const int LAYOUT_OPTION_VALUES = 8;
    internal const int LAYOUT_OPTION_VALUE_LEN = 32;

    // The native library is found, in order, in the folder SIGIL_LIB_DIR names, beside this assembly (then its x86/x64
    // subfolder), and by the default probing. net462 has no NativeLibrary/DllImportResolver, so on Windows the DLL is
    // loaded by full path before the first call; a module already loaded as "sigil.dll" then satisfies DllImport("sigil").
    // A module's name is its file name, so a MinGW build (libsigil.dll) is first copied to a temporary sigil.dll.
    // Elsewhere (Mono on Linux/macOS, as in the tests) the default probing finds libsigil.so/.dylib.
    static Native()
    {
        EnsureLoaded();
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryExW(string path, IntPtr file, uint flags);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectoryW(string? path);

    private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x8;

    private static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

    private static void EnsureLoaded()
    {
        if (!IsWindows)
        {
            return;
        }
        string arch = IntPtr.Size == 8 ? "x64" : "x86";
        string assemblyFolder = Path.GetDirectoryName(typeof(Native).Assembly.Location) ?? "";
        string[] folders =
        {
            Environment.GetEnvironmentVariable("SIGIL_LIB_DIR") ?? "",
            assemblyFolder,
            Path.Combine(assemblyFolder, arch),
            Path.Combine(assemblyFolder, "native", arch),
        };
        string failures = "";
        foreach (string folder in folders)
        {
            if (folder.Length == 0)
            {
                continue;
            }
            // MSVC builds sigil.dll; MinGW prefixes it, libsigil.dll.
            foreach (string file in new[] { "sigil.dll", "libsigil.dll" })
            {
                string full = Path.Combine(folder, file);
                if (!File.Exists(full))
                {
                    continue;
                }
                string toLoad = file == "sigil.dll" ? full : CopyAsSigilDll(full);
                // The DLL's own dependencies (a MinGW runtime next to it) are looked for in its folder too.
                SetDllDirectoryW(folder);
                IntPtr handle = LoadLibraryExW(toLoad, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
                int error = Marshal.GetLastWin32Error();
                SetDllDirectoryW(null);
                if (handle != IntPtr.Zero)
                {
                    return;
                }
                failures += "\n  " + full + ": Win32 error " + error + (error == 193 ? " (wrong bitness)" : "");
            }
        }
        if (failures.Length > 0)
        {
            throw new DllNotFoundException("sigil was found but could not be loaded by this " + arch + " process:" + failures);
        }
    }

    private static string CopyAsSigilDll(string source)
    {
        FileInfo info = new FileInfo(source);
        string folder = Path.Combine(Path.GetTempPath(), "Argosy.Sigil",
            info.Length.ToString("x") + "-" + info.LastWriteTimeUtc.Ticks.ToString("x"));
        string target = Path.Combine(folder, "sigil.dll");
        if (!File.Exists(target))
        {
            Directory.CreateDirectory(folder);
            try
            {
                File.Copy(source, target, false);
            }
            catch (IOException)
            {
                // Another process is copying or has it loaded; use whatever is there.
                if (!File.Exists(target))
                {
                    throw;
                }
            }
        }
        return target;
    }

    private static byte[]? Utf8z(string? s)
    {
        if (s == null)
        {
            return null;
        }
        byte[] bytes = new byte[Encoding.UTF8.GetByteCount(s) + 1];
        Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
        return bytes;
    }

    // sigil.h declares plain C functions (cdecl). DllImport defaults to stdcall on 32-bit Windows, which would corrupt the
    // stack on every call there; on x64 there is one convention and it makes no difference.
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_extract_from_path(byte* path, int hint, NativeOptions* opts, NativeResult* @out);

    internal static int sigil_extract_from_path(string path, int hint, NativeOptions* opts, NativeResult* @out)
    {
        fixed (byte* p_path = Utf8z(path))
        return sigil_extract_from_path(p_path, hint, opts, @out);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_platform_from_slug(byte* slug);

    internal static int sigil_platform_from_slug(string? slug)
    {
        fixed (byte* p_slug = Utf8z(slug))
        return sigil_platform_from_slug(p_slug);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sigil_platform_to_slug(int platform);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sigil_io_open_file(byte* path);

    internal static IntPtr sigil_io_open_file(string path)
    {
        fixed (byte* p_path = Utf8z(path))
        return sigil_io_open_file(p_path);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_io_close(IntPtr io);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_load_header_key_from_prod_keys(byte* path, byte* @out);

    internal static int sigil_load_header_key_from_prod_keys(string path, byte* @out)
    {
        fixed (byte* p_path = Utf8z(path))
        return sigil_load_header_key_from_prod_keys(p_path, @out);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_save_resolve(NativeSaveRequest* req, NativeSaveUnit** @out);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_save_unit_free(NativeSaveUnit* unit);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_save_hash(NativeSaveUnit* unit, IntPtr open, IntPtr openCtx);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nuint sigil_save_layout_subdirs(byte* layout, byte** @out, nuint cap);

    internal static nuint sigil_save_layout_subdirs(string layout, byte** @out, nuint cap)
    {
        fixed (byte* p_layout = Utf8z(layout))
        return sigil_save_layout_subdirs(p_layout, @out, cap);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_save_base(byte* layout, byte* path, byte* @base, nuint baseCap, byte* profile, nuint profileCap);

    internal static int sigil_save_base(string layout, string path, byte* @base, nuint baseCap, byte* profile, nuint profileCap)
    {
        fixed (byte* p_layout = Utf8z(layout))
        fixed (byte* p_path = Utf8z(path))
        return sigil_save_base(p_layout, p_path, @base, baseCap, profile, profileCap);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sigil_save_layout_top(byte* layout);

    internal static IntPtr sigil_save_layout_top(string layout)
    {
        fixed (byte* p_layout = Utf8z(layout))
        return sigil_save_layout_top(p_layout);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_save_profiles(NativeSaveRequest* req, NativeSaveProfile** @out, nuint* count);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_save_profiles_free(NativeSaveProfile* profiles);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_layouts(byte* platform, NativeLayoutInfo** @out, nuint* count);

    internal static int sigil_layouts(string? platform, NativeLayoutInfo** @out, nuint* count)
    {
        fixed (byte* p_platform = Utf8z(platform))
        return sigil_layouts(p_platform, @out, count);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_layouts_free(NativeLayoutInfo* layouts);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sigil_content_stem(byte* contentPath, byte* @out, nuint cap);

    internal static IntPtr sigil_content_stem(string contentPath, byte* @out, nuint cap)
    {
        fixed (byte* p_contentPath = Utf8z(contentPath))
        return sigil_content_stem(p_contentPath, @out, cap);
    }

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_card_list(IntPtr io, NativeCardListing** @out);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_card_listing_free(NativeCardListing* listing);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_card_repair(IntPtr io, NativeCardRepairReport** @out);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_card_repair_report_free(NativeCardRepairReport* report);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_collect(NativeSyncRequest* req, NativeSyncResult** @out);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int sigil_restore(NativeSyncRequest* req, byte* unit, nuint unitLen, NativeSyncResult** @out);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void sigil_sync_result_free(NativeSyncResult* result);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sigil_strerror(int code);

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr sigil_version();
}
