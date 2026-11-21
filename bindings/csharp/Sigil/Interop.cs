// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Argosy.Sigil;

internal static unsafe class Interop
{
    /// <summary>The string behind a NUL-terminated UTF-8 pointer; "" for NULL.</summary>
    internal static string Text(IntPtr chars)
    {
        return chars == IntPtr.Zero ? "" : Utf8ToString(chars);
    }

    // net462: Marshal.PtrToStringUTF8 does not exist there.
    private static string Utf8ToString(IntPtr chars)
    {
        int length = 0;
        while (Marshal.ReadByte(chars, length) != 0)
        {
            length++;
        }
        byte[] bytes = new byte[length];
        Marshal.Copy(chars, bytes, 0, length);
        return Encoding.UTF8.GetString(bytes);
    }

    internal static string Text(byte* chars)
    {
        return Text((IntPtr)chars);
    }

    /// <summary>Copies s into a char array of cap bytes as UTF-8, cut to fit with its NUL.</summary>
    internal static void CopyChars(byte* destination, int cap, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        int n = Math.Min(bytes.Length, cap - 1);
        for (int i = 0; i < n; i++)
        {
            destination[i] = bytes[i];
        }
        destination[n] = 0;
    }

    internal static byte[]? CopyBytes(byte* data, nuint length)
    {
        if (data == null)
        {
            return null;
        }
        byte[] copy = new byte[checked((int)length)];
        if (copy.Length > 0)
        {
            Marshal.Copy((IntPtr)data, copy, 0, copy.Length);
        }
        return copy;
    }

    internal static SigilException Error(int code)
    {
        return SigilException.FromCode(code);
    }

    internal static void Check(int code)
    {
        if (code != 0)
        {
            throw SigilException.FromCode(code);
        }
    }
}

/// <summary>Native memory a call hands to sigil, freed together afterwards.</summary>
internal sealed unsafe class Allocs : IDisposable
{
    private readonly List<IntPtr> _pointers = new List<IntPtr>();

    /// <summary>Zeroed memory; never NULL, even for a size of 0.</summary>
    internal void* Alloc(nuint size)
    {
        // net462: no NativeMemory; AllocHGlobal plus an explicit zero-fill does the same job.
        int bytes = checked((int)(size == 0 ? 1 : size));
        byte* p = (byte*)Marshal.AllocHGlobal(bytes);
        for (int i = 0; i < bytes; i++)
        {
            p[i] = 0;
        }
        _pointers.Add((IntPtr)p);
        return p;
    }

    /// <summary>A NUL-terminated UTF-8 copy of s.</summary>
    internal byte* Str(string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        byte* p = (byte*)Alloc((nuint)bytes.Length + 1);
        for (int i = 0; i < bytes.Length; i++)
        {
            p[i] = bytes[i];
        }
        return p;
    }

    /// <summary>A copy of data followed by a NUL, so a stream of length 0 still has an address.</summary>
    internal byte* Bytes(byte[] data)
    {
        byte* p = (byte*)Alloc((nuint)data.Length + 1);
        for (int i = 0; i < data.Length; i++)
        {
            p[i] = data[i];
        }
        return p;
    }

    /// <summary>An array of NUL-terminated UTF-8 strings, with one more slot after them that is NULL.</summary>
    internal byte** Strings(IReadOnlyList<string>? items)
    {
        int count = items?.Count ?? 0;
        byte** array = (byte**)Alloc((nuint)(count + 1) * (nuint)sizeof(byte*));
        for (int i = 0; i < count; i++)
        {
            array[i] = Str(items![i]);
        }
        return array;
    }

    public void Dispose()
    {
        foreach (IntPtr p in _pointers)
        {
            Marshal.FreeHGlobal(p);
        }
        _pointers.Clear();
    }
}
