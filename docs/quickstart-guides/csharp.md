# C#

`using Argosy.Sigil;` (.NET Framework 4.6.2, P/Invoke over the shared library). Build the library with
`cmake -DSIGIL_BUILD_SHARED=ON` and point `SIGIL_LIB_DIR` at the folder holding `libsigil.so`,
`libsigil.dylib` or `sigil.dll` / `libsigil.dll`; without it the assembly's own folder (and its `x86`/`x64` folder)
and the default probing are tried.
`make test` in [`bindings/csharp/`](../../bindings/csharp/) does both. Failures throw a `SigilException`
subclass per C code (`SigilNotFoundException`, `SigilNeedsKeyException`, `SigilIOException`, ...), each with
`Code`, and `Problem` and `Profiles` where the call names them.

## 1. Identify the game

Once, at import. Skip when the result is already stored.

```csharp
Sigil.Extract(
    string path,                    // required. Rom file.
    Platform platform = Auto,       // Platform.PSP etc., or Sigil.PlatformFromSlug(slug).
                                    //   Auto sniffs the extension; a bare .iso or .zip needs one.
    ExtractOptions? options = null) // optional.
// -> SigilResult; throws when nothing identified the file.

class ExtractOptions {
    byte[]? SwitchHeaderKey;        // Switch header key, 32 bytes. Wins over the two below.
    string? SwitchProdKeysText;     // Switch prod.keys contents. Wins over the path.
    string? SwitchProdKeysPath;     // Switch prod.keys file.
    bool FilenameFallback = true;   // Scan the file name when the binary gives nothing.
    bool Allow3DSHomebrew;
}
```

`SigilResult` carries `TitleId`, `RawSerial`, `SaveId`, `Platform`, `PlatformSlug`, `Source` (`Binary`,
`Filename`), `Usage`, `Features` and the N64 fields `N64Header`, `N64Md5`, `N64Md5N64`. Store these. To rebuild a
result from stored columns: `Sigil.PersistedResult(platformSlug, titleId, saveId, features, rawSerial, n64Header, n64Md5, n64Md5N64)`.

`Sigil.LoadHeaderKeyFromProdKeys(path)` returns the 32-byte header key to keep instead of the keys file.

## 2. Locate the saves

```csharp
Sigil.Layouts(string? platform = null)           // IReadOnlyList<SigilLayout>: id, platform, options, profiles.
Sigil.LayoutSubdirs(string layout)               // Folders under the save root a listing must reach.
Sigil.SaveBase(string layout, string path)       // (Base, Profile): where this layout's saves sit under a path.
Sigil.ListSaveRoot(string root, string layout)   // Root-relative '/' paths, for a listing sigil can read.
Sigil.ListProfiles(string core, string saveRoot) // Layouts with profiles: which ones exist.
Sigil.ContentStem(string contentPath)            // The name RetroArch names saves by.

Sigil.LocateSaves(
    SigilResult game,               // required.
    string core,                    // required. Layout id, as listed by Layouts.
    string contentPath,             // required. The rom as the emulator loads it.
    LocateOptions? options = null)  // SaveRoot or Listing; Options, Profile.
// -> SigilSaveUnit: Key, Shape, Members, Artifact, ContentHash, IdentityHash.
```

Each `SigilSaveMember` has `Path`, `Role` and `Present`.

## 3. Hash or sync

```csharp
Sigil.HashSaves(SigilSaveUnit unit, string saveRoot)   // -> unit with ContentHash and IdentityHash filled in.

Sigil.Collect(game, core, contentPath, saveRoot, SyncOptions? options = null)
Sigil.Restore(byte[] unit, game, core, contentPath, saveRoot, SyncOptions? options = null)
// -> SigilSyncResult: Data (null when there is nothing to send), Shape, Artifact, State, Changed, ...
```

Store `State` after the unit reached RomM and pass it back in `SyncOptions.State` on the next call.
`Restore` throws `SigilConflictException` when the saves changed locally since then, unless
`SyncOptions.OverwriteLocal` is set. `GameIds`, `Companions`, `Claimed`, `Unmanaged`, `Repair`, `Profile`,
`Listing`, `Options` and `WritesParts` behave as in [sync.md](../sync.md).

## Memory cards

```csharp
Sigil.ListCard(string path)    // -> SigilCardListing: format and entries.
Sigil.RepairCard(string path)  // -> SigilCardRepair: what was fixed.
```
