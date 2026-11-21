// SPDX-License-Identifier: MPL-2.0

using System;
using Argosy.Sigil;

// dotnet run --project Example -- <rom> [platform-slug] [core] [save-root]
// Identifies the rom; with a core and save root it also lists the game's saves.

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: Example <rom> [platform-slug] [core] [save-root]");
    return 2;
}

try
{
    Platform platform = args.Length > 1 ? Sigil.PlatformFromSlug(args[1]) : Platform.Auto;
    SigilResult game = Sigil.Extract(args[0], platform);
    Console.WriteLine($"{game.PlatformSlug} {game.TitleId} (save id {game.SaveId}, from {game.Source.Name()})");

    if (args.Length > 3)
    {
        SigilSaveUnit unit = Sigil.HashSaves(Sigil.LocateSaves(game, args[2], args[0], new LocateOptions { SaveRoot = args[3] }), args[3]);
        Console.WriteLine($"{unit.Shape.Name()} {unit.Key} {unit.ContentHash}");
        foreach (SigilSaveMember member in unit.Members)
        {
            Console.WriteLine($"  {member.Path} {(member.Present ? "present" : "missing")}");
        }
    }
    return 0;
}
catch (SigilException error)
{
    Console.Error.WriteLine($"{error.GetType().Name}: {error.Message}");
    return 1;
}
