// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;

namespace Argosy.Sigil;

/// <summary>
/// Base class for sigil failures. <c>Code</c> holds the C error code. After <c>Collect</c> or <c>Restore</c>,
/// <c>Problem</c> names the save, unit member or files at fault when the error has one (no space, region, damaged, no
/// target, ambiguous, exists).
/// </summary>
public class SigilException : Exception
{
    public const int INVALID_ARG = -1;
    public const int IO = -2;
    public const int UNKNOWN_PLATFORM = -3;
    public const int UNSUPPORTED_FORMAT = -4;
    public const int NOT_FOUND = -5;
    public const int NEEDS_KEY = -6;
    public const int CRYPTO = -7;
    public const int OOM = -8;
    public const int CONFLICT = -9;
    public const int EXISTS = -10;
    public const int NO_SPACE = -11;
    public const int UNCOLLECTED = -12;
    public const int DAMAGED = -13;
    public const int REGION = -14;
    public const int NO_TARGET = -15;
    public const int AMBIGUOUS = -16;
    public const int KEYS_INCOMPATIBLE = -17;

    public SigilException(int code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>The C error code, one of the constants above.</summary>
    public int Code { get; }

    /// <summary>The save, unit member or files at fault, one per line for several; "" when the error names none.</summary>
    public string Problem { get; internal set; } = "";

    /// <summary>Layouts with profiles: every profile the emulator lists.</summary>
    public IReadOnlyList<SigilProfile> Profiles { get; internal set; } = Array.Empty<SigilProfile>();

    internal static SigilException FromCode(int code)
    {
        string message = Interop.Text(Native.sigil_strerror(code));
        switch (code)
        {
            case INVALID_ARG: return new SigilInvalidArgException(code, message);
            case IO: return new SigilIOException(code, message);
            case UNKNOWN_PLATFORM: return new SigilUnknownPlatformException(code, message);
            case UNSUPPORTED_FORMAT: return new SigilUnsupportedFormatException(code, message);
            case NOT_FOUND: return new SigilNotFoundException(code, message);
            case NEEDS_KEY: return new SigilNeedsKeyException(code, message);
            case CRYPTO: return new SigilCryptoException(code, message);
            case OOM: return new SigilOOMException(code, message);
            case CONFLICT: return new SigilConflictException(code, message);
            case EXISTS: return new SigilExistsException(code, message);
            case NO_SPACE: return new SigilNoSpaceException(code, message);
            case UNCOLLECTED: return new SigilUncollectedException(code, message);
            case DAMAGED: return new SigilDamagedException(code, message);
            case REGION: return new SigilRegionException(code, message);
            case NO_TARGET: return new SigilNoTargetException(code, message);
            case AMBIGUOUS: return new SigilAmbiguousException(code, message);
            case KEYS_INCOMPATIBLE: return new SigilKeysIncompatibleException(code, message);
            default: return new SigilException(code, message);
        }
    }
}

public sealed class SigilInvalidArgException : SigilException
{
    public SigilInvalidArgException(int code, string message) : base(code, message) { }
}

public sealed class SigilIOException : SigilException
{
    public SigilIOException(int code, string message) : base(code, message) { }
}

public sealed class SigilUnknownPlatformException : SigilException
{
    public SigilUnknownPlatformException(int code, string message) : base(code, message) { }
}

public sealed class SigilUnsupportedFormatException : SigilException
{
    public SigilUnsupportedFormatException(int code, string message) : base(code, message) { }
}

public sealed class SigilNotFoundException : SigilException
{
    public SigilNotFoundException(int code, string message) : base(code, message) { }
}

public sealed class SigilNeedsKeyException : SigilException
{
    public SigilNeedsKeyException(int code, string message) : base(code, message) { }
}

public sealed class SigilCryptoException : SigilException
{
    public SigilCryptoException(int code, string message) : base(code, message) { }
}

public sealed class SigilOOMException : SigilException
{
    public SigilOOMException(int code, string message) : base(code, message) { }
}

/// <summary>The saves on disk changed since the last sync; restore wrote nothing.</summary>
public sealed class SigilConflictException : SigilException
{
    public SigilConflictException(int code, string message) : base(code, message) { }
}

public sealed class SigilExistsException : SigilException
{
    public SigilExistsException(int code, string message) : base(code, message) { }
}

/// <summary>
/// The saves don't fit; <c>Problem</c> names the save that didn't and <c>BlocksShort</c> the blocks it lacked (0 when
/// the free blocks were there but a directory slot or a Dreamcast game file's starting blocks weren't).
/// </summary>
public sealed class SigilNoSpaceException : SigilException
{
    public SigilNoSpaceException(int code, string message) : base(code, message) { }

    public uint BlocksShort { get; internal set; }
}

/// <summary>A shared volume holds saves no collect has passed on yet; restore wrote nothing.</summary>
public sealed class SigilUncollectedException : SigilException
{
    public SigilUncollectedException(int code, string message) : base(code, message) { }
}

/// <summary>
/// A file the saves are in is damaged; <c>Problem</c> names it. <c>SyncOptions.Repair</c> rebuilds it where sigil can;
/// a card sigil can't read at all stays refused.
/// </summary>
public sealed class SigilDamagedException : SigilException
{
    public SigilDamagedException(int code, string message) : base(code, message) { }
}

/// <summary>A companion's save, named in <c>Problem</c>, belongs to another region than the game.</summary>
public sealed class SigilRegionException : SigilException
{
    public SigilRegionException(int code, string message) : base(code, message) { }
}

/// <summary>
/// The unit holds a volume or member, named in <c>Problem</c>, that has no file to go in: the emulator's settings keep
/// none, no profile is there for an account save, or the folder lies outside the save root. Restore wrote nothing.
/// </summary>
public sealed class SigilNoTargetException : SigilException
{
    public SigilNoTargetException(int code, string message) : base(code, message) { }
}

/// <summary>
/// More than one file could be the card the emulator uses, more than one profile and none picked, or more than one
/// emulator folder under the save root; <c>Problem</c> names them, one per line, and <c>Profiles</c> lists the
/// profiles. Nothing was written. When you don't know which profile the user plays as, ask them from <c>Profiles</c>
/// (or <c>Sigil.ListProfiles</c>) and pass <c>SyncOptions.Profile</c>.
/// </summary>
public sealed class SigilAmbiguousException : SigilException
{
    public SigilAmbiguousException(int code, string message) : base(code, message) { }
}

/// <summary>The keys given don't open this content: the key file lacks the key for its key generation, or its header key is wrong.</summary>
public sealed class SigilKeysIncompatibleException : SigilException
{
    public SigilKeysIncompatibleException(int code, string message) : base(code, message) { }
}
