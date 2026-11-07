using Statventure.Core;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Tests.Packs;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Core;

/// <summary>
/// <see cref="ApplicationOptions.GamePacksOnly"/> (Docs/Editor.md, step 3; the editor shows one pack over the game's
/// packs, P6): <see cref="Application.Data"/> holds the area packs only, no <c>classes/</c>, no mods, no development
/// folder, so a damaged mod in the player's folder doesn't stop the start; Reload follows. Without it, the game's data
/// as before (<c>Packs/ApplicationDataTests</c>, <c>ApplicationModsTests</c>). The start on a real application in a
/// hidden window counted as minimized (nothing is shown or presented), in the "Window" collection.
/// </summary>
[Collection("Window")]
public class ApplicationGamePacksTests
{
    static readonly Func<string, string?> NoVariables = _ => null;

    static string[] Describe(IEnumerable<Mount> mounts, string root) =>
        [.. mounts.Select(m => $"{m.Kind} {Path.GetRelativePath(root, m.Path).Replace('\\', '/')}{(m.At.Length > 0 ? " at " + m.At : "")}")];

    [Fact]
    public void TheDataIsTheAreaPacksOnly()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/data.pak"], Entry("data/x", "pack"));
        MakePack(folder["game/packs/audio.pak"], Entry("audio/x", "pack"));
        folder.Write("game/classes/black-knight.json", "{}");
        Directory.CreateDirectory(folder["dev"]);
        Directory.CreateDirectory(folder["variable"]);
        Func<string, string?> variable = name => name == Application.DevelopmentFolderVariable ? folder["variable"] : null;
        var options = new ApplicationOptions { GamePacksOnly = true };
        Assert.Equal(["Pack packs/audio.pak", "Pack packs/data.pak"],
            Describe(Application.DataMounts(options with { DevelopmentFolder = folder["dev"] }, folder["game"], NoVariables, folder["player/mods"]), folder["game"]));
        Assert.Equal(["Pack packs/audio.pak", "Pack packs/data.pak"],
            Describe(Application.DataMounts(options, folder["game"], variable, folder["player/mods"]), folder["game"]));
        // A missing development folder, which stops the game's start, isn't looked at.
        Assert.Equal(2, Application.DataMounts(options with { DevelopmentFolder = folder["nowhere"] }, folder["game"], NoVariables).Count);
        // The default: the game's data, as before.
        Assert.False(new ApplicationOptions().GamePacksOnly);
        Assert.Equal(["Pack packs/audio.pak", "Pack packs/data.pak", "Folder classes at classes", "Mods ../player/mods", "Folder ../dev"],
            Describe(Application.DataMounts(new ApplicationOptions { DevelopmentFolder = folder["dev"] }, folder["game"], NoVariables, folder["player/mods"]), folder["game"]));
        Assert.Equal("Folder ../variable", Describe(Application.DataMounts(new ApplicationOptions(), folder["game"], variable), folder["game"])[^1]);
    }

    /// <summary>An application in a hidden, minimized window that reads its data in its first update, reloads it, reads it again and quits.</summary>
    sealed class Probe(ApplicationOptions options) : Application(options with
    {
        Window = new WindowOptions { Title = "Statventure data test", Width = 160, Height = 100, Visible = false },
        Audio = false,
    })
    {
        public (bool Mod, bool Development)? Before, After;
        public bool? Reloaded;

        protected override void Load() => Window.MinimizedForTests = true;

        protected override void Update(in GameTime time)
        {
            Before = (Data.Exists("ui/mod.txt"), Data.Exists("ui/dev.txt"));
            Reloaded = ReloadData();
            After = (Data.Exists("ui/mod.txt"), Data.Exists("ui/dev.txt"));
            Quit();
        }

        protected override void Render(Frame frame, GpuImage target, in GameTime time) =>
            throw new InvalidOperationException("a minimized window has no frames");
    }

    [Fact]
    public void ADamagedModDoesntStopTheStartAndNoModNorTheDevelopmentFolderIsInTheData()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        MakePack(folder["player/mods/a-good.pak"], Entry("ui/mod.txt", "mod"));
        var broken = folder.Write("player/mods/b-broken.pak", "not a pack"u8.ToArray());
        folder.Write("dev/ui/dev.txt", "dev");
        var options = new ApplicationOptions { GameFolder = folder["player"], DevelopmentFolder = folder["dev"] };
        var probe = new Probe(options with { GamePacksOnly = true });
        probe.Run();
        Assert.Equal((false, false), probe.Before);
        Assert.True(probe.Reloaded);
        Assert.Equal((false, false), probe.After);
        // Without it the damaged mod stops the start, as always.
        var game = new Probe(options);
        Assert.Equal(broken, Assert.Throws<DataFileException>(game.Run).File);
        Assert.Null(game.Before);
        // And with the damaged mod gone, the game's data has the mod and the development folder.
        File.Delete(broken);
        var mended = new Probe(options);
        mended.Run();
        Assert.Equal((true, true), mended.Before);
        Assert.Equal((true, true), mended.After);
    }
}
