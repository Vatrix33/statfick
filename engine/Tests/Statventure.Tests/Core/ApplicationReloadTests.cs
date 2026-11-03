using Statventure.Audio;
using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Audio;
using Statventure.Tests.Packs;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Core;

/// <summary>
/// <see cref="Application.ReloadData"/> (Docs/Assets.md, step 9; S9-C): the data mounted afresh, the sound's
/// music reloaded from it before the game's listeners run, and a Reload that fails logged, giving false, with
/// the data (or only the music) left as it was. A real window and the default output device (nothing is
/// played), the music from a development folder holding the committed pack's entries. In the non-parallel
/// "Storage log" collection, as it reads the log.
/// </summary>
[Collection("Storage log")]
public class ApplicationReloadTests
{
    static readonly double Tempo = TestMusic.Library.Theme("alder-vale")!.Tempo;

    /// <summary>A game folder for the player and a development folder with the game's music.</summary>
    static ApplicationOptions Options(TempFolder folder, bool audio = true)
    {
        using (var pack = PackFile.Open(TestMusic.Pack))
            pack.ExtractTo(folder["dev"]);
        return new ApplicationOptions { GameFolder = folder["player"], DevelopmentFolder = folder["dev"], Audio = audio };
    }

    static void WriteTheme(TempFolder folder, ThemeSpec theme) =>
        File.WriteAllText(folder["dev/audio/themes/alder-vale.json"], RecordSerializer.ToJson(theme));

    sealed class Seen
    {
        public bool Reloaded;
        public MusicLibrary? Before, After;
        public double? TempoTheGameSaw;
        public int GameListenerRuns;
        public bool? NewFile, Mod;
    }

    [Fact]
    public void ReloadDataRemountsThenTheMusicThenTheGamesListeners()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        var seen = new Seen();
        var application = new TestApplication(Options(folder))
        {
            OnLoad = a => a.Data.Reloaded += () =>
            {
                seen.GameListenerRuns++;
                seen.TempoTheGameSaw = a.Audio!.Music.Library.Theme("alder-vale")!.Tempo;
            },
            OnUpdate = (a, _) =>
            {
                seen.Before = a.Audio!.Music.Library;
                WriteTheme(folder, TestMusic.Library.Theme("alder-vale")! with { Tempo = 141 });
                folder.Write("dev/ui/new.txt", "new");
                MakePack(folder["player/mods/mod.pak"], Entry("ui/mod.txt", "mod"));
                seen.Reloaded = a.ReloadData();
                seen.After = a.Audio.Music.Library;
                (seen.NewFile, seen.Mod) = (a.Data.Exists("ui/new.txt"), a.Data.Exists("ui/mod.txt"));
                return false;
            },
        };
        var log = TestApplication.CaptureLog("Data", application.Run);
        Assert.True(seen.Reloaded);
        Assert.Equal(Tempo, seen.Before!.Theme("alder-vale")!.Tempo);
        Assert.Equal(141, seen.After!.Theme("alder-vale")!.Tempo);
        Assert.Equal(TestMusic.Library.Themes, seen.After.Themes);
        // The game's listener ran once, after the sound had the new music.
        Assert.Equal(1, seen.GameListenerRuns);
        Assert.Equal(141, seen.TempoTheGameSaw);
        Assert.Equal((true, true), (seen.NewFile, seen.Mod));
        Assert.Contains("Info: reloaded", log);
        Assert.Contains($"Info: mod {folder["player/mods/mod.pak"]}", log);
    }

    [Fact]
    public void AReloadThatCantMountLeavesTheDataAndTheMusic()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        var seen = new Seen();
        bool? again = null, newFileAfterwards = null, brokenAgain = null, newerFile = null;
        var application = new TestApplication(Options(folder))
        {
            OnLoad = a => a.Data.Reloaded += () => seen.GameListenerRuns++,
            OnUpdate = (a, _) =>
            {
                seen.Before = a.Audio!.Music.Library;
                WriteTheme(folder, TestMusic.Library.Theme("alder-vale")! with { Tempo = 141 });
                folder.Write("dev/ui/new.txt", "new");
                // A name outside the path rule fails the development folder's mount (Q29).
                folder.Write("dev/ui/Bad Name.txt", "bad");
                seen.Reloaded = a.ReloadData();
                seen.After = a.Audio.Music.Library;
                seen.NewFile = a.Data.Exists("ui/new.txt");
                // Mended, it reloads.
                File.Delete(folder["dev/ui/Bad Name.txt"]);
                again = a.ReloadData();
                newFileAfterwards = a.Data.Exists("ui/new.txt");
                // And after one that worked, a failing one is still refused.
                folder.Write("dev/ui/newer.txt", "newer");
                folder.Write("dev/ui/Bad Name.txt", "bad");
                brokenAgain = a.ReloadData();
                newerFile = a.Data.Exists("ui/newer.txt");
                return false;
            },
        };
        var log = TestApplication.CaptureLog("Data", application.Run);
        Assert.False(seen.Reloaded);
        Assert.Same(seen.Before, seen.After);
        Assert.False(seen.NewFile);
        var failed = log.Where(line => line.StartsWith("Error: ")).ToArray();
        Assert.Equal(2, failed.Length);
        Assert.All(failed, line => Assert.StartsWith("Error: not reloaded, the data stays as it was: ", line));
        Assert.All(failed, line => Assert.Contains(folder["dev/ui/Bad Name.txt"], line));
        // Only the Reload that worked told the game.
        Assert.Equal(1, seen.GameListenerRuns);
        Assert.Equal((true, true), (again, newFileAfterwards));
        Assert.Equal((false, false), (brokenAgain, newerFile));
        Assert.Equal(["Info: reloaded"], log.Where(line => line.StartsWith("Info: reloaded")));
    }

    [Fact]
    public void AReloadWhoseMusicDoesntLoadKeepsTheMusicItHad()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        var seen = new Seen();
        bool? mended = null;
        double? mendedTempo = null;
        var application = new TestApplication(Options(folder))
        {
            OnLoad = a => a.Data.Reloaded += () => seen.GameListenerRuns++,
            OnUpdate = (a, _) =>
            {
                seen.Before = a.Audio!.Music.Library;
                // A theme whose id isn't its path's (S6-1): the data mounts, the music doesn't load.
                WriteTheme(folder, TestMusic.Library.Theme("alder-vale")! with { Id = "elsewhere" });
                folder.Write("dev/ui/new.txt", "new");
                seen.Reloaded = a.ReloadData();
                seen.After = a.Audio.Music.Library;
                seen.NewFile = a.Data.Exists("ui/new.txt");
                // Mended, the music reloads too.
                WriteTheme(folder, TestMusic.Library.Theme("alder-vale")! with { Tempo = 141 });
                mended = a.ReloadData();
                mendedTempo = a.Audio.Music.Library.Theme("alder-vale")!.Tempo;
                return false;
            },
        };
        var log = TestApplication.CaptureLog("Data", application.Run);
        Assert.False(seen.Reloaded);
        Assert.Same(seen.Before, seen.After);
        // The data is the new one, and the game was told.
        Assert.True(seen.NewFile);
        Assert.Equal(2, seen.GameListenerRuns);
        Assert.Equal((true, 141.0), (mended, mendedTempo));
        var failed = Assert.Single(log, line => line.StartsWith("Error: "));
        Assert.Equal($"Error: the music isn't reloaded, the sound keeps what it had: {folder["dev/audio/themes/alder-vale.json"]}: "
            + "the theme's id is 'elsewhere', but its path holds 'alder-vale' (a theme's path is made from its id)", failed);
        Assert.Equal(["Info: reloaded, but not the music", "Info: reloaded"], log.Where(line => line.StartsWith("Info: reloaded")));
    }

    [Fact]
    public void AGameListenersExceptionIsntCaught()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        var seen = new Seen();
        Exception? thrown = null;
        var application = new TestApplication(Options(folder, audio: false))
        {
            OnLoad = a => a.Data.Reloaded += () => throw new InvalidOperationException("the game's own problem"),
            OnUpdate = (a, _) =>
            {
                folder.Write("dev/ui/new.txt", "new");
                thrown = Record.Exception(() => a.ReloadData());
                seen.NewFile = a.Data.Exists("ui/new.txt");
                return false;
            },
        };
        var log = TestApplication.CaptureLog("Data", application.Run);
        Assert.Equal("the game's own problem", Assert.IsType<InvalidOperationException>(thrown).Message);
        // The data was reloaded; nothing says otherwise.
        Assert.True(seen.NewFile);
        Assert.DoesNotContain(log, line => line.StartsWith("Error: ") || line.StartsWith("Info: reloaded"));
    }

    [Fact]
    public void WithoutSoundOnlyTheDataReloads()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new TempFolder();
        var seen = new Seen();
        var application = new TestApplication(Options(folder, audio: false))
        {
            OnUpdate = (a, _) =>
            {
                folder.Write("dev/ui/new.txt", "new");
                seen.Reloaded = a.ReloadData();
                seen.NewFile = a.Data.Exists("ui/new.txt");
                return false;
            },
        };
        application.Run();
        Assert.True(seen.Reloaded);
        Assert.True(seen.NewFile);
    }

    [Fact]
    public void TheDataIsReloadedOnlyWhileTheApplicationRuns()
    {
        var application = new TestApplication(new ApplicationOptions());
        Assert.Equal("The data is reloaded only while the application runs", Assert.Throws<InvalidOperationException>(() => application.ReloadData()).Message);
    }
}
