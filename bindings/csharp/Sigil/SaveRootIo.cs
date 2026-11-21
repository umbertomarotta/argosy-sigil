// SPDX-License-Identifier: MPL-2.0

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Argosy.Sigil;

/// <summary>
/// The save root sigil reads and writes through callbacks. Sigil never touches the file system itself: it asks for a
/// member by its root-relative '/' path, and this opens, replaces, patches and removes the file under the root.
/// </summary>
internal sealed class SaveRoot : IDisposable
{
    private GCHandle _handle;

    internal SaveRoot(string path)
    {
        Path = path;
        _handle = GCHandle.Alloc(this);
    }

    internal string Path { get; }

    /// <summary>The ctx the callbacks receive.</summary>
    internal IntPtr Context => GCHandle.ToIntPtr(_handle);

    public void Dispose()
    {
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
    }
}

internal static unsafe class SaveRootIo
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr OpenFn(IntPtr ctx, IntPtr relativePath);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int WriteFn(IntPtr ctx, IntPtr relativePath, IntPtr data, nuint length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int WriteAtFn(IntPtr ctx, IntPtr relativePath, ulong offset, IntPtr data, nuint length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int RemoveFn(IntPtr ctx, IntPtr relativePath);

    // The delegates live in static fields, so the function pointers handed to sigil stay valid for the process.
    private static readonly OpenFn OpenDelegate = OpenMember;
    private static readonly WriteFn WriteDelegate = WriteMember;
    private static readonly WriteAtFn WriteAtDelegate = WriteMemberAt;
    private static readonly RemoveFn RemoveDelegate = RemoveMember;

    internal static readonly IntPtr Open = Marshal.GetFunctionPointerForDelegate(OpenDelegate);
    internal static readonly IntPtr Write = Marshal.GetFunctionPointerForDelegate(WriteDelegate);
    internal static readonly IntPtr WriteAt = Marshal.GetFunctionPointerForDelegate(WriteAtDelegate);
    internal static readonly IntPtr Remove = Marshal.GetFunctionPointerForDelegate(RemoveDelegate);

    // A callback that throws would take the process down, so each one answers a failure with its C error value.

    private static string FullPath(IntPtr ctx, IntPtr relativePath)
    {
        var root = (SaveRoot)GCHandle.FromIntPtr(ctx).Target!;
        return root.Path + "/" + Interop.Text(relativePath);
    }

    private static IntPtr OpenMember(IntPtr ctx, IntPtr relativePath)
    {
        try
        {
            return Native.sigil_io_open_file(FullPath(ctx, relativePath));
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    // Writes data as the file at path, whole: the bytes go to a temporary file beside it, which is renamed over the
    // file, so a write that fails leaves the file as it was. The file keeps its permissions.
    private static int WriteMember(IntPtr ctx, IntPtr relativePath, IntPtr data, nuint length)
    {
        try
        {
            ReplaceFile(FullPath(ctx, relativePath), (byte*)data, length);
            return 0;
        }
        catch
        {
            return -1;
        }
    }

    // Writes part of a file that is there, stored before it returns: an Xbox drive image gets only what changed.
    private static int WriteMemberAt(IntPtr ctx, IntPtr relativePath, ulong offset, IntPtr data, nuint length)
    {
        try
        {
            using (var file = new FileStream(FullPath(ctx, relativePath), FileMode.Open, FileAccess.Write, FileShare.Read,
                       1, FileOptions.WriteThrough))
            {
                file.Seek(checked((long)offset), SeekOrigin.Begin);
                WriteAll(file, (byte*)data, length);
                file.Flush(true);
            }
            return 0;
        }
        catch
        {
            return -1;
        }
    }

    // A path ending in '/' names a directory sigil emptied: remove the directory.
    private static int RemoveMember(IntPtr ctx, IntPtr relativePath)
    {
        try
        {
            string path = FullPath(ctx, relativePath);
            if (path.EndsWith("/", StringComparison.Ordinal))
            {
                Directory.Delete(path.TrimEnd('/'), false);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                return -1;
            }
            return 0;
        }
        catch
        {
            return -1;
        }
    }

    private static void WriteAll(FileStream file, byte* data, nuint length)
    {
        ulong remaining = length;
        while (remaining > 0)
        {
            int n = (int)Math.Min(remaining, 1u << 30);
            byte[] chunk = new byte[n];
            Marshal.Copy((IntPtr)data, chunk, 0, n);
            file.Write(chunk, 0, n);
            data += n;
            remaining -= (ulong)n;
        }
    }

    private static void ReplaceFile(string path, byte* data, nuint length)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        string temp = Path.Combine(directory ?? "", ".sigil-" + Guid.NewGuid().ToString("N").Substring(0, 16) + ".tmp");
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16,
                       FileOptions.WriteThrough))
            {
                WriteAll(file, data, length);
                file.Flush(true);
            }
            // net462: no File.Move(overwrite) and no Unix file modes (the plugin only runs on Windows).
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }
        catch
        {
            try
            {
                File.Delete(temp);
            }
            catch
            {
                // The write's own failure is the one to report.
            }
            throw;
        }
    }
}
