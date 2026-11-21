// SPDX-License-Identifier: MPL-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Argosy.Sigil;
using Xunit;

namespace Argosy.Sigil.Tests;

// Run with SIGIL_LIB_DIR naming the folder that holds the shared library (see bindings/csharp/README.md).

public sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sigil-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
        }
    }
}

public class ExtractTests
{
    [Fact]
    public void VersionIsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(Sigil.Version()));
    }

    [Theory]
    [InlineData("psp", Platform.PSP)]
    [InlineData("psx", Platform.PSX)]
    [InlineData("ps2", Platform.PS2)]
    [InlineData("psvita", Platform.PSVita)]
    [InlineData("switch", Platform.Switch)]
    [InlineData("3ds", Platform.ThreeDS)]
    [InlineData("wii", Platform.Wii)]
    [InlineData("wiiu", Platform.WiiU)]
    [InlineData("gamecube", Platform.GameCube)]
    [InlineData("ps3", Platform.PS3)]
    [InlineData("xbox360", Platform.Xbox360)]
    [InlineData("dreamcast", Platform.Dreamcast)]
    [InlineData("xbox", Platform.Xbox)]
    [InlineData("gb", Platform.GB)]
    [InlineData("gbc", Platform.GBC)]
    [InlineData("snes", Platform.SNES)]
    [InlineData("n64", Platform.N64)]
    [InlineData("gba", Platform.GBA)]
    public void PlatformSlugRoundtrip(string slug, Platform platform)
    {
        Assert.Equal(platform, Sigil.PlatformFromSlug(slug));
        Assert.Equal(slug, platform.Slug());
    }

    [Fact]
    public void UnknownSlugIsAuto()
    {
        Assert.Equal(Platform.Auto, Sigil.PlatformFromSlug("bogus"));
    }

    [Fact]
    public void ExtractFallsBackToTheFileName()
    {
        using var dir = new TempDir();
        string path = Path.Combine(dir.Path, "Game [ULUS10064].iso");
        File.WriteAllBytes(path, new byte[400]);

        SigilResult result = Sigil.Extract(path, Platform.PSP);

        Assert.Equal("ULUS10064", result.TitleId);
        Assert.Equal("ULUS10064", result.SaveId);
        Assert.Equal(Platform.PSP, result.Platform);
        Assert.Equal("psp", result.PlatformSlug);
        Assert.Equal(Source.Filename, result.Source);
        Assert.Equal("filename", result.Source.Name());
        Assert.Equal(Usage.FolderPrefix, result.Usage);
        Assert.Equal("folder-prefix", result.Usage.Name());
    }

    [Fact]
    public void ExtractMissingFileThrowsIo()
    {
        using var dir = new TempDir();
        var error = Assert.Throws<SigilIOException>(() => Sigil.Extract(Path.Combine(dir.Path, "missing.iso"), Platform.PSP));
        Assert.Equal(SigilException.IO, error.Code);
    }

    [Fact]
    public void ExtractRejectsAShortHeaderKey()
    {
        using var dir = new TempDir();
        string path = Path.Combine(dir.Path, "a.nsp");
        File.WriteAllBytes(path, new byte[16]);
        Assert.Throws<SigilInvalidArgException>(() =>
            Sigil.Extract(path, Platform.Switch, new ExtractOptions { SwitchHeaderKey = new byte[5] }));
    }

    [Fact]
    public void MissingProdKeysThrowsIo()
    {
        using var dir = new TempDir();
        Assert.Throws<SigilIOException>(() => Sigil.LoadHeaderKeyFromProdKeys(Path.Combine(dir.Path, "none.keys")));
    }
}

public class LayoutTests
{
    [Theory]
    [InlineData("Chrono Cross (USA).cue", "Chrono Cross (USA)")]
    [InlineData("/roms/a.zip#Game (USA).gba", "Game (USA)")]
    [InlineData("/roms/日本語 (JP).cue", "日本語 (JP)")]
    public void ContentStem(string contentPath, string stem)
    {
        Assert.Equal(stem, Sigil.ContentStem(contentPath));
    }

    [Fact]
    public void LayoutsListsEveryRow()
    {
        IReadOnlyList<SigilLayout> all = Sigil.Layouts();
        Assert.NotEmpty(all);
        Assert.Contains(all, row => row.Id == "libretro");
        Assert.All(Sigil.Layouts("psx"), row => Assert.True(row.Platform == "psx" || row.Platform == ""));
    }

    [Fact]
    public void LayoutOptionsCarryTheirValues()
    {
        SigilLayout row = Sigil.Layouts("segacd").Single(r => r.Id == "genesis_plus_gx");
        SigilLayoutOption option = row.Options.Single(o => o.Key == "genesis_plus_gx_system_bram");
        Assert.Equal(new[] { "per game", "per bios" }, option.Values);
        Assert.Equal("per bios", option.DefaultValue);
    }

    [Fact]
    public void LayoutSubdirs()
    {
        Assert.Empty(Sigil.LayoutSubdirs("gambatte"));
        Assert.Contains("nand/user/save/0000000000000000", Sigil.LayoutSubdirs("eden"));
    }

    [Fact]
    public void ListSaveRootFollowsTheLayout()
    {
        using var dir = new TempDir();
        string folder = Path.Combine(dir.Path, "nand", "user", "save", "0000000000000000", "AAAA", "0100ABCD12345000");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "file.bin"), "1");

        Assert.Equal(new[] { "nand/user/save/0000000000000000/AAAA/0100ABCD12345000/file.bin" },
            Sigil.ListSaveRoot(dir.Path, "eden"));
        Assert.Equal(dir.Path, Sigil.SaveBase("gambatte", dir.Path).Base);
    }
}

public class SaveTests
{
    private static readonly byte[] Data = Enumerable.Range(0, 2048).Select(i => (byte)(i % 256)).ToArray();

    private static SigilResult Game()
    {
        return Sigil.PersistedResult("gb", "", "", Features.None);
    }

    [Fact]
    public void LocateAndHash()
    {
        using var dir = new TempDir();
        File.WriteAllBytes(Path.Combine(dir.Path, "Pokemon Red.srm"), Data);
        File.WriteAllBytes(Path.Combine(dir.Path, "Other Game.srm"), new byte[] { 1 });

        SigilSaveUnit unit = Sigil.LocateSaves(Game(), "gambatte", "Pokemon Red.gb",
            new LocateOptions { SaveRoot = dir.Path });

        Assert.Equal("Pokemon Red", unit.Key);
        Assert.Equal(SaveShape.Single, unit.Shape);
        SigilSaveMember member = Assert.Single(unit.Members);
        Assert.Equal("Pokemon Red.srm", member.Path);
        Assert.Equal(SaveRole.Primary, member.Role);
        Assert.True(member.Present);
        Assert.Equal("Pokemon Red.srm", unit.Artifact);
        Assert.Equal("", unit.ContentHash);

        SigilSaveUnit hashed = Sigil.HashSaves(unit, dir.Path);
        string md5;
        using (MD5 hasher = MD5.Create())
        {
            md5 = BitConverter.ToString(hasher.ComputeHash(Data)).Replace("-", "").ToLowerInvariant();
        }
        Assert.Equal(md5, hashed.ContentHash);
        Assert.Equal(md5, hashed.IdentityHash);
    }

    [Fact]
    public void CollectRestoreAndConflict()
    {
        using var source = new TempDir();
        using var target = new TempDir();
        File.WriteAllBytes(Path.Combine(source.Path, "Pokemon Red.srm"), Data);

        SigilSyncResult collected = Sigil.Collect(Game(), "gambatte", "Pokemon Red.gb", source.Path);
        Assert.Equal("Pokemon Red.srm", collected.Artifact);
        Assert.Equal(SaveShape.Single, collected.Shape);
        Assert.Equal(Data, collected.Data);
        Assert.True(collected.Changed);
        Assert.NotEmpty(collected.State);

        SigilSyncResult restored = Sigil.Restore(collected.Data!, Game(), "gambatte", "Pokemon Red.gb", target.Path);
        Assert.Equal(Data, File.ReadAllBytes(Path.Combine(target.Path, "Pokemon Red.srm")));
        Assert.Equal(new[] { "Pokemon Red.srm" },
            Directory.GetFileSystemEntries(target.Path).Select(Path.GetFileName).ToArray());

        SigilSyncResult again = Sigil.Collect(Game(), "gambatte", "Pokemon Red.gb", target.Path);
        File.WriteAllText(Path.Combine(target.Path, "Pokemon Red.srm"), "changed locally");

        var options = new SyncOptions { State = again.State };
        Assert.Throws<SigilConflictException>(() =>
            Sigil.Restore(collected.Data!, Game(), "gambatte", "Pokemon Red.gb", target.Path, options));

        Sigil.Restore(collected.Data!, Game(), "gambatte", "Pokemon Red.gb", target.Path,
            new SyncOptions { State = again.State, OverwriteLocal = true });
        Assert.Equal(Data, File.ReadAllBytes(Path.Combine(target.Path, "Pokemon Red.srm")));
        Assert.False(restored.RestoreAgain);
    }

    [Fact]
    public void CollectFromAnEmptyRootHasNoData()
    {
        using var dir = new TempDir();
        SigilSyncResult result = Sigil.Collect(Game(), "gambatte", "Pokemon Red.gb", dir.Path);
        Assert.Null(result.Data);
        Assert.Equal(SaveShape.None, result.Shape);
        Assert.False(result.Changed);
    }

    [Fact]
    public void RestoreOfASingleFileSaveWritesTheBytesAsGiven()
    {
        // A gb save is the raw battery ram, so any bytes are a valid unit.
        using var dir = new TempDir();
        byte[] raw = { 1, 2, 3 };
        Sigil.Restore(raw, Game(), "gambatte", "Pokemon Red.gb", dir.Path);
        Assert.Equal(raw, File.ReadAllBytes(Path.Combine(dir.Path, "Pokemon Red.srm")));
    }
}

public class CardTests
{
    [Fact]
    public void ListCardOfAnUnreadableFileIsUnsupported()
    {
        using var dir = new TempDir();
        string path = Path.Combine(dir.Path, "card.mcr");
        File.WriteAllBytes(path, new byte[1000]);
        Assert.Throws<SigilUnsupportedFormatException>(() => Sigil.ListCard(path));
        Assert.Throws<SigilUnsupportedFormatException>(() => Sigil.RepairCard(path));
    }

    [Fact]
    public void ListCardOfAMissingFileThrowsIo()
    {
        using var dir = new TempDir();
        Assert.Throws<SigilIOException>(() => Sigil.ListCard(Path.Combine(dir.Path, "nope.mcr")));
    }
}

public class ContractTests
{
    [Fact]
    public void FeatureBitsMatchTheHeader()
    {
        Assert.Equal(1u, (uint)Features.RTC);
        Assert.Equal(2u, (uint)Features.MBC2);
        Assert.Equal(4u, (uint)Features.GBASram);
        Assert.Equal(8u, (uint)Features.GBAFlash);
        Assert.Equal(16u, (uint)Features.GBAFlash1M);
        Assert.Equal(32u, (uint)Features.GBAEeprom);
    }

    [Fact]
    public void ErrorCodesMapToTheirClasses()
    {
        Assert.IsType<SigilConflictException>(SigilException.FromCode(SigilException.CONFLICT));
        Assert.IsType<SigilNoSpaceException>(SigilException.FromCode(SigilException.NO_SPACE));
        Assert.IsType<SigilAmbiguousException>(SigilException.FromCode(SigilException.AMBIGUOUS));
        Assert.IsType<SigilKeysIncompatibleException>(SigilException.FromCode(SigilException.KEYS_INCOMPATIBLE));
        Assert.Equal(SigilException.IO, SigilException.FromCode(SigilException.IO).Code);
    }
}
