using Statventure.Core;
using Statventure.Editor;
using Statventure.Editor.About;
using Statventure.Editor.Game;
using Statventure.Editor.Shell;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Editor.Game;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Editor.EndToEnd.EndToEndTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.EndToEnd;

/// <summary>
/// A mod made against an installed game's folder that the command line names (M12 step 8; Docs/Packaging.md, "How it
/// works", step 8): <c>--game &lt;folder&gt;</c> read by the program's own <see cref="EditorArguments"/>, the game's data
/// library loaded from that folder, its version the editor's (Q18); step 14's flow through the UI
/// (<see cref="ModFromScratchTests.MakeMod"/>) and, in the same mod, an entry of the game's own kind (its library's
/// stand-in item) made with New entry and the form; the mod mounted as the game mounts the player's mods over that
/// folder's packs and read back, the game's kind as the library's type; and the About dialog's credits naming the
/// editor's version. In the suite the folder is a scratch one laid out as the game's download
/// (<see cref="GameTesting.MakeGame"/>, the suite's dev build). <c>Tools/Scripts/end-to-end.sh</c> runs this test built
/// with the packages' version on the game's package unpacked (<see cref="GameVariable"/>, <see cref="VersionVariable"/>),
/// and takes the mod into its player's mods folder for the packaged game (<see cref="ModsVariable"/>) and a picture
/// of the About dialog (<see cref="AboutVariable"/>).
/// </summary>
[Collection("GPU")]
public class GameFolderModTests(GpuFixture gpu)
{
    /// <summary>The game's folder to work on (end-to-end.sh: the game's package unpacked); unset, a scratch one.</summary>
    public const string GameVariable = "STATVENTURE_END_TO_END_GAME";

    /// <summary>The version the editor must have (end-to-end.sh: the packages'); unset, the suite's build's, whatever it is.</summary>
    public const string VersionVariable = "STATVENTURE_END_TO_END_VERSION";

    /// <summary>A mods folder the mod is copied into once read back (end-to-end.sh: its player's, for the packaged game).</summary>
    public const string ModsVariable = "STATVENTURE_END_TO_END_MODS";

    /// <summary>A PNG file the About dialog over the editor is drawn into (end-to-end.sh: the editor's credits captured).</summary>
    public const string AboutVariable = "STATVENTURE_END_TO_END_ABOUT";

    const string Lantern = "stand-in/items/lantern";

    [Fact]
    public void AModMadeAgainstTheGameFolderNamedWithGameLoadsInThatGame()
    {
        using var scratch = new Folders();
        var folder = Path.GetFullPath(Environment.GetEnvironmentVariable(GameVariable) is { Length: > 0 } given ? given : GameTesting.MakeGame(scratch["game"]));
        if (Environment.GetEnvironmentVariable(VersionVariable) is { Length: > 0 } version)
            Assert.Equal(version, EditorView.Version);

        // The program's command line: the game's folder found, its library read from there and its kinds loaded.
        var arguments = EditorArguments.Parse(["--game", folder, "--player-folder", scratch.Player]);
        Assert.Null(arguments.Problem);
        Assert.Equal((folder, scratch.Player), (arguments.GameFolder, arguments.PlayerFolder));
        var found = arguments.FindGame(EditorView.Version);
        Assert.Null(found.Problem);
        var game = found.Game!;
        Assert.Equal((folder, Path.Combine(folder, "packs"), false), (game.Folder, game.PacksFolder, game.DevSetup));
        var library = GameLibrary.Read(game.Library!);
        Assert.Equal((Path.Combine(folder, GameLibrary.FileName), EditorView.Version), (library.File, library.Version));
        var standIn = game.Types.Find(GameTesting.StandInKind) ?? throw new InvalidOperationException($"the game's kinds have no {GameTesting.StandInKind}");
        Assert.Equal(GameLibrary.AssemblyName, standIn.Assembly.GetName().Name);

        Dictionary<string, (Type Type, string Binary, string Json)> shown;
        object lantern;
        using (var editor = new EditorHarness(game, 1280, 900, 1))
        {
            editor.Session.PlayerFolder = scratch.Player;
            editor.Frame();
            shown = ModFromScratchTests.MakeMod(editor, scratch);

            // The game's own kind in the same mod: New entry offers it (the library's), the form fills it, Save.
            var item = editor.NewEntry(NewBy.Button, "Stand-in item (stand-in.item)", "lantern");
            Assert.Equal(Lantern, item.Path);
            editor.Set("$.title", "Ember Lantern");
            editor.Set("$.price", "12");
            editor.Pick("$.rarity", "Rare");
            lantern = item.Built();
            Assert.Same(standIn, lantern.GetType());
            Assert.Equal(("Ember Lantern", 12, "Rare"), (standIn.GetProperty("Title")!.GetValue(lantern), standIn.GetProperty("Price")!.GetValue(lantern), standIn.GetProperty("Rarity")!.GetValue(lantern)));
            editor.Key("s", "KeyS", KeyModifiers.Control);
            Assert.Null(editor.View.Dialogs.Top);
            Assert.Equal("Saved ember-rite.pak.", editor.View.Status);
            Assert.False(editor.Session.HasUnsavedWork);

            // About: the engine's credits naming the editor's version (the game's, Q18).
            _ = editor.Run(() => editor.View.Part<AboutPart>().Show());
            editor.Frame();
            Assert.Equal($"Version {EditorView.Version}", editor.Q(".about-dialog .version").TextContent);
            if (Environment.GetEnvironmentVariable(AboutVariable) is { Length: > 0 } picture)
                ImageFile.SavePng(picture, EditorScreenTests.Render(gpu.Device, editor, 1280, 900));
        }

        // The game: that folder's packs and the player's mods, mounted as the game mounts them; each entry the value the
        // editor showed, the game's kind read as its library's type.
        var mod = Path.Combine(scratch.Mods, ModFromScratchTests.ModFile);
        using (var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder, _ => null, scratch.Mods)))
        {
            foreach (var (path, (type, binary, _)) in shown)
                Assert.Equal(binary, Binary(type, data.Load(type, path)));
            Assert.Equal(Binary(standIn, lantern), Binary(standIn, data.Load(standIn, Lantern)));
            Assert.Equal(mod, VirtualFileSystem.ModFiles(scratch.Mods).Single());
        }
        if (Environment.GetEnvironmentVariable(ModsVariable) is { Length: > 0 } mods)
        {
            Directory.CreateDirectory(mods);
            File.Copy(mod, Path.Combine(mods, ModFromScratchTests.ModFile), overwrite: true);
        }
    }
}
