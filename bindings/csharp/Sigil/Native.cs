// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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
    public fixed byte options[5760]; // NativeLayoutOption [SIGIL_LAYOUT_OPTIONS]
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

    // The native library is found, in order, in the folder SIGIL_LIB_DIR names, beside this assembly, and by the
    // runtime's default probing (the NuGet runtimes/<rid>/native folder, the application folder, the system paths).
    static Native()
    {
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, Resolve);
    }

    private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name != Lib)
        {
            return IntPtr.Zero;
        }
        string[] folders =
        {
            Environment.GetEnvironmentVariable("SIGIL_LIB_DIR") ?? "",
            Path.GetDirectoryName(assembly.Location) ?? "",
        };
        foreach (string folder in folders)
        {
            if (folder.Length == 0)
            {
                continue;
            }
            // MSVC builds sigil.dll; MinGW prefixes it, libsigil.dll.
            string[] files = OperatingSystem.IsWindows() ? new[] { "sigil.dll", "libsigil.dll" }
                : OperatingSystem.IsMacOS() ? new[] { "libsigil.dylib" }
                : new[] { "libsigil.so" };
            foreach (string file in files)
            {
                if (NativeLibrary.TryLoad(Path.Combine(folder, file), out IntPtr handle))
                {
                    return handle;
                }
            }
        }
        return IntPtr.Zero;
    }

    [DllImport(Lib)]
    internal static extern int sigil_extract_from_path([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int hint,
        NativeOptions* opts, NativeResult* @out);

    [DllImport(Lib)]
    internal static extern int sigil_platform_from_slug([MarshalAs(UnmanagedType.LPUTF8Str)] string? slug);

    [DllImport(Lib)]
    internal static extern IntPtr sigil_platform_to_slug(int platform);

    [DllImport(Lib)]
    internal static extern IntPtr sigil_io_open_file([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport(Lib)]
    internal static extern void sigil_io_close(IntPtr io);

    [DllImport(Lib)]
    internal static extern int sigil_load_header_key_from_prod_keys([MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        byte* @out);

    [DllImport(Lib)]
    internal static extern int sigil_save_resolve(NativeSaveRequest* req, NativeSaveUnit** @out);

    [DllImport(Lib)]
    internal static extern void sigil_save_unit_free(NativeSaveUnit* unit);

    [DllImport(Lib)]
    internal static extern int sigil_save_hash(NativeSaveUnit* unit, IntPtr open, IntPtr openCtx);

    [DllImport(Lib)]
    internal static extern nuint sigil_save_layout_subdirs([MarshalAs(UnmanagedType.LPUTF8Str)] string layout,
        byte** @out, nuint cap);

    [DllImport(Lib)]
    internal static extern int sigil_save_base([MarshalAs(UnmanagedType.LPUTF8Str)] string layout,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path, byte* @base, nuint baseCap, byte* profile, nuint profileCap);

    [DllImport(Lib)]
    internal static extern IntPtr sigil_save_layout_top([MarshalAs(UnmanagedType.LPUTF8Str)] string layout);

    [DllImport(Lib)]
    internal static extern int sigil_save_profiles(NativeSaveRequest* req, NativeSaveProfile** @out, nuint* count);

    [DllImport(Lib)]
    internal static extern void sigil_save_profiles_free(NativeSaveProfile* profiles);

    [DllImport(Lib)]
    internal static extern int sigil_layouts([MarshalAs(UnmanagedType.LPUTF8Str)] string? platform,
        NativeLayoutInfo** @out, nuint* count);

    [DllImport(Lib)]
    internal static extern void sigil_layouts_free(NativeLayoutInfo* layouts);

    [DllImport(Lib)]
    internal static extern IntPtr sigil_content_stem([MarshalAs(UnmanagedType.LPUTF8Str)] string contentPath, byte* @out,
        nuint cap);

    [DllImport(Lib)]
    internal static extern int sigil_card_list(IntPtr io, NativeCardListing** @out);

    [DllImport(Lib)]
    internal static extern void sigil_card_listing_free(NativeCardListing* listing);

    [DllImport(Lib)]
    internal static extern int sigil_card_repair(IntPtr io, NativeCardRepairReport** @out);

    [DllImport(Lib)]
    internal static extern void sigil_card_repair_report_free(NativeCardRepairReport* report);

    [DllImport(Lib)]
    internal static extern int sigil_collect(NativeSyncRequest* req, NativeSyncResult** @out);

    [DllImport(Lib)]
    internal static extern int sigil_restore(NativeSyncRequest* req, byte* unit, nuint unitLen, NativeSyncResult** @out);

    [DllImport(Lib)]
    internal static extern void sigil_sync_result_free(NativeSyncResult* result);

    [DllImport(Lib)]
    internal static extern IntPtr sigil_strerror(int code);

    [DllImport(Lib)]
    internal static extern IntPtr sigil_version();
}
