// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;

namespace Argosy.Sigil;

// The numbers below are sigil.h's, written out so that bindings/python/test_contract.py can check them against it.

/// <summary>A console platform. <c>Auto</c> sniffs the platform from the file extension.</summary>
public enum Platform
{
    Auto = 0,
    PSP = 1,
    PSX = 2,
    PS2 = 3,
    PSVita = 4,
    Switch = 5,
    ThreeDS = 6,
    Wii = 7,
    WiiU = 8,
    GameCube = 9,
    PS3 = 10,
    Xbox360 = 11,
    Dreamcast = 12,
    Xbox = 13,
    GB = 14,
    GBC = 15,
    SNES = 16,
    N64 = 17,
    GBA = 18,
}

/// <summary>Whether an id came out of the binary or out of a community naming pattern in the file name.</summary>
public enum Source
{
    Binary = 0,
    Filename = 1,
}

/// <summary>How a platform uses the save id for save artifacts; docs/identification.md, "usage", says how to apply it.</summary>
public enum Usage
{
    FolderExact = 0,
    FolderPrefix = 1,
    FileExact = 2,
    FilePrefix = 3,
    FolderSplit = 4,
}

/// <summary>The CNMT content-meta type of a Switch dump.</summary>
public enum SwitchContentType
{
    Unknown = 0,
    Application = 1,
    Patch = 2,
    Addon = 3,
}

/// <summary>Cart facts read from the ROM header that change what a save unit holds. A bit set.</summary>
[Flags]
public enum Features : uint
{
    None = 0,
    /// <summary>The cart carries a real-time clock; a libretro frontend persists it beside the save as &lt;stem&gt;.rtc.</summary>
    RTC = 1u << 0,
    /// <summary>A Game Boy cart whose RAM is MBC2's 512 four-bit cells.</summary>
    MBC2 = 1u << 1,
    /// <summary>The GBA save chip the ROM's Nintendo library id names; at most one of these is set.</summary>
    GBASram = 1u << 2,
    GBAFlash = 1u << 3,
    GBAFlash1M = 1u << 4,
    GBAEeprom = 1u << 5,
}

/// <summary>The archive shape a save unit travels in; see docs/save-units.md.</summary>
public enum SaveShape
{
    None = 0,
    Single = 1,
    Multi = 2,
    Folder = 3,
}

/// <summary>What a member is to its unit.</summary>
public enum SaveRole
{
    Primary = 0,
    Sidecar = 1,
    RTC = 2,
}

/// <summary>Whose a save is on an emulator that keeps saves per user profile.</summary>
public enum SaveArea
{
    None = 0,
    Account = 1,
    Device = 2,
}

/// <summary>The file format a memory card was read from.</summary>
public enum CardFormat
{
    Unknown = 0,
    PS1Raw = 1,
    PS1GME = 2,
    PS1VMP = 3,
    PS2 = 4,
    GameCubeRaw = 5,
    DreamcastVMU = 6,
    SaturnBackup = 7,
    SegaCDBRAM = 8,
}

/// <summary>What is wrong with a damaged save or card structure.</summary>
public enum CardDamage
{
    Chain = 1,
    ChainUnwritten = 2,
    ChainBroken = 3,
    SizeMismatch = 4,
    BlocksShared = 5,
    BlocksInUse = 6,
    ChecksumMismatch = 7,
    Unverifiable = 8,
    CopyDamaged = 9,
}

/// <summary>The strings the other bindings and the CLI use for each enum.</summary>
public static class SigilNames
{
    /// <summary>The platform's slug: "psp", "psx", "3ds", ...</summary>
    public static string Slug(this Platform platform)
    {
        return Interop.Text(Native.sigil_platform_to_slug((int)platform));
    }

    public static string Name(this Source source)
    {
        switch (source)
        {
            case Source.Binary: return "binary";
            case Source.Filename: return "filename";
            default: return $"Source({(int)source})";
        }
    }

    public static string Name(this Usage usage)
    {
        switch (usage)
        {
            case Usage.FolderExact: return "folder-exact";
            case Usage.FolderPrefix: return "folder-prefix";
            case Usage.FileExact: return "file-exact";
            case Usage.FilePrefix: return "file-prefix";
            case Usage.FolderSplit: return "folder-split";
            default: return $"Usage({(int)usage})";
        }
    }

    public static string Name(this SwitchContentType type)
    {
        switch (type)
        {
            case SwitchContentType.Application: return "application";
            case SwitchContentType.Patch: return "patch";
            case SwitchContentType.Addon: return "addon";
            default: return "unknown";
        }
    }

    public static string Name(this SaveShape shape)
    {
        switch (shape)
        {
            case SaveShape.Single: return "single";
            case SaveShape.Multi: return "multi";
            case SaveShape.Folder: return "folder";
            default: return "none";
        }
    }

    public static string Name(this SaveRole role)
    {
        switch (role)
        {
            case SaveRole.Primary: return "primary";
            case SaveRole.RTC: return "rtc";
            default: return "sidecar";
        }
    }

    public static string Name(this SaveArea area)
    {
        switch (area)
        {
            case SaveArea.Account: return "account";
            case SaveArea.Device: return "device";
            default: return "none";
        }
    }

    public static string Name(this CardFormat format)
    {
        switch (format)
        {
            case CardFormat.PS1Raw: return "ps1-raw";
            case CardFormat.PS1GME: return "ps1-gme";
            case CardFormat.PS1VMP: return "ps1-vmp";
            case CardFormat.PS2: return "ps2";
            case CardFormat.GameCubeRaw: return "gamecube-raw";
            case CardFormat.DreamcastVMU: return "dreamcast-vmu";
            case CardFormat.SaturnBackup: return "saturn-backup";
            case CardFormat.SegaCDBRAM: return "segacd-bram";
            default: return "unknown";
        }
    }

    public static string Name(this CardDamage damage)
    {
        switch (damage)
        {
            case CardDamage.ChainUnwritten: return "chain-unwritten";
            case CardDamage.ChainBroken: return "chain-broken";
            case CardDamage.SizeMismatch: return "size-mismatch";
            case CardDamage.BlocksShared: return "blocks-shared";
            case CardDamage.BlocksInUse: return "blocks-in-use";
            case CardDamage.ChecksumMismatch: return "checksum-mismatch";
            case CardDamage.Unverifiable: return "unverifiable";
            case CardDamage.CopyDamaged: return "copy-damaged";
            default: return "chain";
        }
    }
}

/// <summary>A successful extraction.</summary>
public sealed record SigilResult
{
    /// <summary>The game's id; "" on gb, gbc, gba, snes. A Switch update or DLC gives its game's id.</summary>
    public string TitleId { get; init; } = "";
    /// <summary>The id as found in the binary; a Switch update's or DLC's own id.</summary>
    public string RawSerial { get; init; } = "";
    /// <summary>The on-disk name the emulator keys the save by; "" on gb, gbc, gba, snes.</summary>
    public string SaveId { get; init; } = "";
    public Platform Platform { get; init; }
    public string PlatformSlug { get; init; } = "";
    public Source Source { get; init; }
    public Usage Usage { get; init; }
    public bool Experimental { get; init; }
    public SwitchContentType SwitchContentType { get; init; }
    /// <summary>Switch only: the content's version from CNMT; 0 when unavailable.</summary>
    public uint TitleVersion { get; init; }
    public Features Features { get; init; }
    /// <summary>N64 only: the cart's name; "" when it is not plain ASCII.</summary>
    public string N64Header { get; init; } = "";
    /// <summary>N64 only: the ROM's MD5 in .z64 byte order, uppercase.</summary>
    public string N64Md5 { get; init; } = "";
    /// <summary>N64 only: the same in .n64 byte order, as Project64 hashes it.</summary>
    public string N64Md5N64 { get; init; } = "";

    /// <summary>Whether the cart carries a real-time clock.</summary>
    public bool HasRtc => (Features & Features.RTC) != 0;
}

/// <summary>One file of a save unit. <c>Path</c> is relative to the save root; <c>Entry</c> is its archive name.</summary>
public sealed record SigilSaveMember
{
    public string Path { get; init; } = "";
    public string Entry { get; init; } = "";
    public SaveRole Role { get; init; }
    public bool Present { get; init; }
    public SaveArea Area { get; init; }
}

/// <summary>A user profile the emulator lists. <c>Id</c> is how its save folder is named.</summary>
public sealed record SigilProfile
{
    public string Id { get; init; } = "";
    /// <summary>The nickname; "" when the emulator keeps none.</summary>
    public string Name { get; init; } = "";
}

/// <summary>A file under the save root the layout would take with other option values.</summary>
public sealed record SigilSaveAlternate
{
    public string Path { get; init; } = "";
    /// <summary>A file every game shares, as in <c>Unkeyed</c>.</summary>
    public bool Shared { get; init; }
    /// <summary>The values that take it; pass them as <c>Options</c> to read it.</summary>
    public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
}

/// <summary>Every file under a save root that belongs to one game, and the hashes the RomM server computes for it.</summary>
public sealed record SigilSaveUnit
{
    /// <summary>The stem, or the save id for folder layouts.</summary>
    public string Key { get; init; } = "";
    public SaveShape Shape { get; init; }
    public IReadOnlyList<SigilSaveMember> Members { get; init; } = Array.Empty<SigilSaveMember>();
    /// <summary>Files the core should have written but has not.</summary>
    public IReadOnlyList<SigilSaveMember> Expected { get; init; } = Array.Empty<SigilSaveMember>();
    /// <summary>Root files shared by every game. Never bundle them.</summary>
    public IReadOnlyList<string> Unkeyed { get; init; } = Array.Empty<string>();
    /// <summary>The file name the upload travels under.</summary>
    public string Artifact { get; init; } = "";
    /// <summary>"" until <c>Sigil.HashSaves</c>.</summary>
    public string ContentHash { get; init; } = "";
    /// <summary>"" until <c>Sigil.HashSaves</c>.</summary>
    public string IdentityHash { get; init; } = "";
    public IReadOnlyList<SigilSaveAlternate> Alternates { get; init; } = Array.Empty<SigilSaveAlternate>();
}

/// <summary>An option a layout's files depend on. <c>Values</c> is empty for a free-form value.</summary>
public sealed record SigilLayoutOption
{
    public string Key { get; init; } = "";
    public IReadOnlyList<string> Values { get; init; } = Array.Empty<string>();
    /// <summary>The value an absent option counts as; "" when the layout's files need it set.</summary>
    public string DefaultValue { get; init; } = "";
}

/// <summary>One layout row. <c>Id</c> is what <c>Collect</c> and <c>Restore</c> take as <c>core</c>.</summary>
public sealed record SigilLayout
{
    public string Id { get; init; } = "";
    /// <summary>The platform slug the row is limited to; "" for a row that applies to any platform.</summary>
    public string Platform { get; init; } = "";
    public IReadOnlyList<SigilLayoutOption> Options { get; init; } = Array.Empty<SigilLayoutOption>();
    /// <summary>The option that picks a shared file by the disc's region, or "".</summary>
    public string RegionOption { get; init; } = "";
    /// <summary>An account save needs a user profile.</summary>
    public bool Profiles { get; init; }
    /// <summary>Restore can name a new file only after one of the game's files is already there.</summary>
    public bool NeedsExisting { get; init; }
}

/// <summary>One save on a memory card. <c>OwnerId</c> is the game id it carries, or "" when the format has none.</summary>
public sealed record SigilCardEntry
{
    public string Name { get; init; } = "";
    public string OwnerId { get; init; } = "";
    public uint Blocks { get; init; }
    public uint FirstBlock { get; init; }
}

/// <summary>The saves on a memory card and the space left on it.</summary>
public sealed record SigilCardListing
{
    public CardFormat Format { get; init; }
    public uint TotalBlocks { get; init; }
    public uint FreeBlocks { get; init; }
    public uint FreeSlots { get; init; }
    public uint CorruptCount { get; init; }
    public IReadOnlyList<SigilCardEntry> Entries { get; init; } = Array.Empty<SigilCardEntry>();
    /// <summary>The saves left out as corrupt that the card still names, with <c>Blocks</c> 0.</summary>
    public IReadOnlyList<SigilCardEntry> CorruptEntries { get; init; } = Array.Empty<SigilCardEntry>();
}

/// <summary>One damaged item on a card; <c>Repaired</c> says whether <c>SigilCardRepair.Data</c> holds the fix.</summary>
public sealed record SigilCardRepairEntry
{
    /// <summary>The save as <c>CorruptEntries</c> names it; "" for the card's own structures.</summary>
    public string Name { get; init; } = "";
    public string OwnerId { get; init; } = "";
    public uint FirstBlock { get; init; }
    public CardDamage Damage { get; init; }
    public bool Repaired { get; init; }
}

/// <summary>What <c>Sigil.RepairCard</c> found.</summary>
public sealed record SigilCardRepair
{
    public CardFormat Format { get; init; }
    /// <summary>The whole card with every repair applied, in the form it was read; null when nothing was repaired.</summary>
    public byte[]? Data { get; init; }
    public IReadOnlyList<SigilCardRepairEntry> Entries { get; init; } = Array.Empty<SigilCardRepairEntry>();
}

/// <summary>The optional inputs to <c>Sigil.Extract</c>.</summary>
public sealed class ExtractOptions
{
    /// <summary>Switch header key, 32 bytes; wins over <c>SwitchProdKeysText</c> and <c>SwitchProdKeysPath</c>.</summary>
    public byte[]? SwitchHeaderKey { get; init; }
    /// <summary>Path of a Switch prod.keys file.</summary>
    public string? SwitchProdKeysPath { get; init; }
    /// <summary>The contents of a Switch prod.keys file; wins over <c>SwitchProdKeysPath</c>.</summary>
    public string? SwitchProdKeysText { get; init; }
    /// <summary>Scan the file name when the binary gives nothing; <c>Source</c> reports which happened.</summary>
    public bool FilenameFallback { get; init; } = true;
    public bool Allow3DSHomebrew { get; init; }
}

/// <summary>The optional inputs to <c>Sigil.LocateSaves</c>.</summary>
public sealed class LocateOptions
{
    /// <summary>The directory the emulator writes this game's save into; sigil lists it and the subfolders the core writes into.</summary>
    public string? SaveRoot { get; init; }
    /// <summary>Instead of <c>SaveRoot</c>: root-relative '/' paths of every file in the root and under <c>Sigil.LayoutSubdirs</c>.</summary>
    public IReadOnlyList<string>? Listing { get; init; }
    /// <summary>Core option key to value, the strings the core defines and never display labels.</summary>
    public IReadOnlyDictionary<string, string>? Options { get; init; }
    /// <summary>Layouts with profiles: the profile whose saves to take, by <c>SigilProfile.Id</c>.</summary>
    public string? Profile { get; init; }
}

/// <summary>A game whose saves this game reads, as a sequel reads its prequel's.</summary>
public sealed class SigilCompanion
{
    /// <summary>The companion's ids, as for <c>SyncOptions.GameIds</c>.</summary>
    public IReadOnlyList<string> GameIds { get; init; } = Array.Empty<string>();
    /// <summary>Restore: its unit from RomM, or null to leave its saves as they are.</summary>
    public byte[]? Unit { get; init; }
}

/// <summary>What <c>Collect</c> found of a companion's saves.</summary>
public sealed record SigilCompanionResult
{
    /// <summary>The companion's unit; null when none of its saves are there.</summary>
    public byte[]? Data { get; init; }
    public string ContentHash { get; init; } = "";
    public string IdentityHash { get; init; } = "";
    public bool Changed { get; init; }
}

/// <summary>The optional inputs to <c>Sigil.Collect</c> and <c>Sigil.Restore</c>.</summary>
public sealed class SyncOptions
{
    /// <summary>Root-relative paths; null lists the save root.</summary>
    public IReadOnlyList<string>? Listing { get; init; }
    public IReadOnlyDictionary<string, string>? Options { get; init; }
    /// <summary>Every id the game's saves may carry: all discs of a set.</summary>
    public IReadOnlyList<string>? GameIds { get; init; }
    /// <summary>What the last call returned for this game.</summary>
    public byte[]? State { get; init; }
    /// <summary>The game runs outside the caller.</summary>
    public bool Unmanaged { get; init; }
    /// <summary>Restore: the user chose to replace saves that changed locally.</summary>
    public bool OverwriteLocal { get; init; }
    /// <summary>Saturn, Sega CD, Dreamcast: names from <c>Unowned</c> the user said belong to this game.</summary>
    public IReadOnlyList<string>? Claimed { get; init; }
    /// <summary>Games whose saves this game reads, in the order they go on.</summary>
    public IReadOnlyList<SigilCompanion>? Companions { get; init; }
    /// <summary>Rebuild what <c>SigilDamagedException</c> named, where sigil can.</summary>
    public bool Repair { get; init; }
    /// <summary>Layouts with profiles: the profile whose saves to take, by <c>SigilProfile.Id</c>.</summary>
    public string? Profile { get; init; }
    /// <summary>
    /// Restore: write only the parts that change of a file every game's saves sit in (an Xbox drive image), in place,
    /// instead of the whole file through a temporary file renamed over it. docs/sync.md, "Writing and removing", says
    /// what a restore stopped part way leaves.
    /// </summary>
    public bool WritesParts { get; init; }
}

/// <summary>What <c>Collect</c> or <c>Restore</c> produced. Store <c>State</c> and pass it to the next call for this game.</summary>
public sealed record SigilSyncResult
{
    /// <summary>The file name the unit travels under.</summary>
    public string Artifact { get; init; } = "";
    public SaveShape Shape { get; init; }
    /// <summary>Collect: the unit to upload; null when the game has no saves.</summary>
    public byte[]? Data { get; init; }
    /// <summary>RomM's content_hash of the unit.</summary>
    public string ContentHash { get; init; } = "";
    /// <summary>Over the saves themselves; placement and timestamps don't move it.</summary>
    public string IdentityHash { get; init; } = "";
    /// <summary>The identity hash differs from the last sync.</summary>
    public bool Changed { get; init; }
    /// <summary>Store it once every upload succeeded; pass it back next time. Empty when there is none.</summary>
    public byte[] State { get; init; } = Array.Empty<byte>();
    /// <summary>Saturn, Sega CD, Dreamcast: a zip of the unclaimed saves on a shared volume; null when none.</summary>
    public byte[]? Holding { get; init; }
    /// <summary>The names of the saves in <c>Holding</c>.</summary>
    public IReadOnlyList<string> Unowned { get; init; } = Array.Empty<string>();
    /// <summary>Collect: the first this many names in <c>Unowned</c> are new or rewritten since the last collect.</summary>
    public int UnownedChanged { get; init; }
    /// <summary>Unmanaged: the saves the last restore wrote were overwritten; restore again instead of uploading.</summary>
    public bool RestoreAgain { get; init; }
    /// <summary>Collect: one per <c>SyncOptions.Companions</c>, in order.</summary>
    public IReadOnlyList<SigilCompanionResult> Companions { get; init; } = Array.Empty<SigilCompanionResult>();
    /// <summary>Layouts with profiles: every profile the emulator lists.</summary>
    public IReadOnlyList<SigilProfile> Profiles { get; init; } = Array.Empty<SigilProfile>();
    /// <summary>The profile whose saves were taken or written; "" for none.</summary>
    public string Profile { get; init; } = "";
    /// <summary>The profile the account saves' own records name as their maker; "" when they name none or differ.</summary>
    public string MakerProfile { get; init; } = "";
    public IReadOnlyList<SigilSaveAlternate> Alternates { get; init; } = Array.Empty<SigilSaveAlternate>();
    /// <summary>Restore: the unit ended in the hardcore marker Argosy appended to uploads before it used sigil.</summary>
    public bool HardcoreMarker { get; init; }
}
