# C# binding

.NET Framework 4.6.2 (`net462`) P/Invoke binding over the shared sigil library (`Argosy.Sigil`, namespace `Argosy.Sigil`).
Guide: [C#](../../docs/quickstart-guides/csharp.md).

```
make test                      # builds the shared lib, runs dotnet test
make example ROM=game.iso      # identifies a rom
```

The native library is `libsigil.so`, `libsigil.dylib` or `sigil.dll` / `libsigil.dll`. It is looked up in
`SIGIL_LIB_DIR`, then beside the assembly, then through the default .NET probing. Build it with
`cmake -DSIGIL_BUILD_SHARED=ON` ([building.md](../../docs/building.md)). On Windows both `sigil.dll` (MSVC) and `libsigil.dll` (MinGW) are tried.

The binding allocates no managed memory the C side keeps: strings and buffers live for one call, and the
file callbacks used by `Collect` and `Restore` are held in static fields so they are never collected mid-call.
The package version tracks the library version; `bindings/python/test_contract.py` fails when they differ.

## net462

The binding targets `net462` because Playnite plugins run on .NET Framework 4.6.2. It also loads under Mono.
`Compat.cs` supplies the language-support types the framework lacks (`IsExternalInit`, the tuple name attribute,
`ValueTuple<,>`) and is compiled only for that target. Differences from a .NET 8 build:

- The framework has no `NativeLibrary`, so on Windows the DLL is loaded by full path before the first call, from
  `SIGIL_LIB_DIR`, beside the assembly, or its `x86`/`x64` (or `native/x86`, `native/x64`) folder. Anywhere else the
  default probing finds `libsigil.so` or `libsigil.dylib` (set `LD_LIBRARY_PATH` or `DYLD_LIBRARY_PATH`).
  A MinGW `libsigil.dll` is copied to `%TEMP%\Argosy.Sigil\<size>-<time>\sigil.dll` first, because the runtime
  matches `DllImport("sigil")` by module name.
- String parameters are passed as NUL-terminated UTF-8 by hand, since the framework's marshaller has no `LPUTF8Str`.
- `Restore` replaces a file with `File.Replace`. Unix file modes are not preserved, so a restore over an existing
  save on Mono may reset its permissions.
