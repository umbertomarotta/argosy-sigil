# C# binding

.NET 8 P/Invoke binding over the shared sigil library (`Argosy.Sigil`, namespace `Argosy.Sigil`).
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
