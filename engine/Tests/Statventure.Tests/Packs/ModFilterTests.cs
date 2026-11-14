using Statventure.Core;
using Statventure.Packs;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// The engine's hook for the game's mod check (GM1-P13; Docs/Game/Foundation.md, "Mods: the list and the check at start"): a
/// mods mount given a filter asks it at every mount, with the mods found in the order they mount and the mounts below, and
/// mounts only those it accepts, in that order, each logged as before; without one every mod mounts, as before; the
/// application gives the mods folder its options' filter. Run alone: they read the log (<see cref="Log.Sink"/> is one for
/// the process).
/// </summary>
[Collection("Mod filter log")]
public class ModFilterTests
{
    static readonly Func<string, string?> NoVariables = _ => null;

    /// <summary>A game's pack and classes, and four mods in the player's folder (each holding <c>audio/who</c>, the last one winning).</summary>
    static void Lay(TempFolder folder)
    {
        MakePack(folder["game/packs/audio.pak"], Entry("audio/who", "the game"), Entry("audio/game", "the game"));
        folder.Write("game/classes/black-knight.json", "{}");
        foreach (var name in new[] { "a-first", "B-second", "c-third", "d-fourth" })
            MakePack(folder[$"player/mods/{name}.pak"], Entry("audio/who", name), Entry($"audio/{name.ToLowerInvariant()}", name));
        // Not mods: another ending, a pack in a folder below.
        MakePack(folder["player/mods/e-fifth.pak.off"], Entry("audio/who", "not a mod"));
        MakePack(folder["player/mods/later/f-sixth.pak"], Entry("audio/who", "not a mod"));
    }

    /// <summary>The "Data" lines logged while <paramref name="action"/> runs, the folder's root as &lt;root&gt;.</summary>
    static List<string> DataLog(TempFolder folder, Action action)
    {
        var lines = new List<string>();
        var (sink, level) = (Log.Sink, Log.Level);
        Log.Sink = (lineLevel, category, message) =>
        {
            sink?.Invoke(lineLevel, category, message);
            if (category == "Data")
                lock (lines)
                    lines.Add(message.Replace(folder.Root, "<root>"));
        };
        Log.Level = LogLevel.Info;
        try
        {
            action();
        }
        finally
        {
            (Log.Sink, Log.Level) = (sink, level);
        }
        lock (lines)
            return [.. lines];
    }

    [Fact]
    public void WithoutAFilterEveryModMounts()
    {
        using var folder = new TempFolder();
        Lay(folder);
        var mods = Mount.Mods(folder["player/mods"]);
        Assert.Null(mods.Filter);
        List<string> log = [];
        log = DataLog(folder, () =>
        {
            using var data = new VirtualFileSystem([Mount.Pack(folder["game/packs/audio.pak"]), mods]);
            Assert.Equal("d-fourth", data.ReadText("audio/who"));
            Assert.Equal(["audio/a-first", "audio/b-second", "audio/c-third", "audio/d-fourth", "audio/game", "audio/who"], data.List("audio"));
        });
        // Ordinal order: the capital first.
        Assert.Equal(["mod <root>/player/mods/B-second.pak", "mod <root>/player/mods/a-first.pak", "mod <root>/player/mods/c-third.pak", "mod <root>/player/mods/d-fourth.pak"], log);
    }

    [Fact]
    public void AFilterIsGivenTheModsAndTheMountsBelowAndOnlyThoseItAcceptsMount()
    {
        using var folder = new TempFolder();
        Lay(folder);
        var asked = new List<(string[] Mods, Mount[] Below)>();
        // It accepts the third and the first (given out of order), and a file that isn't one of the mods.
        ModFilter filter = (mods, below) =>
        {
            asked.Add(([.. mods], [.. below]));
            return [mods[2], mods[1], folder["player/mods/e-fifth.pak.off"], folder["player/mods/later/f-sixth.pak"]];
        };
        var game = Mount.Pack(folder["game/packs/audio.pak"]);
        var classes = Mount.Folder(folder["game/classes"], "classes");
        var modsMount = Mount.Mods(folder["player/mods"], filter);
        folder.Write("dev/notes.txt", "notes");
        var development = Mount.Folder(folder["dev"]);
        Assert.Same(filter, modsMount.Filter);
        List<string> log = [];
        log = DataLog(folder, () =>
        {
            using var data = new VirtualFileSystem([game, classes, modsMount, development]);
            Assert.Equal("c-third", data.ReadText("audio/who"));
            Assert.Equal(["audio/a-first", "audio/c-third", "audio/game", "audio/who"], data.List("audio"));
            Assert.Equal(folder["player/mods/c-third.pak"], data.Origin("audio/who"));
            Assert.False(data.Exists("audio/b-second"));
        });
        // Asked once, with the mods found (full paths, in the order they mount) and the mounts below the mods (not those above).
        var (mods, below) = Assert.Single(asked);
        Assert.Equal([folder["player/mods/B-second.pak"], folder["player/mods/a-first.pak"], folder["player/mods/c-third.pak"], folder["player/mods/d-fourth.pak"]], mods);
        Assert.Equal([game, classes], below);
        // Only the mods mounted are logged, in the order they mount.
        Assert.Equal(["mod <root>/player/mods/a-first.pak", "mod <root>/player/mods/c-third.pak"], log);
    }

    [Fact]
    public void TheFilterIsAskedAtEveryMountAndAcceptingNoneMountsNone()
    {
        using var folder = new TempFolder();
        Lay(folder);
        var calls = 0;
        var accept = new HashSet<string>(StringComparer.Ordinal) { "a-first.pak" };
        var modsMount = Mount.Mods(folder["player/mods"], (mods, _) =>
        {
            calls++;
            return mods.Where(m => accept.Contains(Path.GetFileName(m)));
        });
        using var data = new VirtualFileSystem([Mount.Pack(folder["game/packs/audio.pak"]), modsMount]);
        Assert.Equal((1, "a-first"), (calls, data.ReadText("audio/who")));
        // A Reload asks again: a new mod found, one accepted before refused now.
        MakePack(folder["player/mods/g-seventh.pak"], Entry("audio/who", "g-seventh"));
        accept.Clear();
        accept.Add("c-third.pak");
        accept.Add("g-seventh.pak");
        data.Reload();
        Assert.Equal((2, "g-seventh"), (calls, data.ReadText("audio/who")));
        Assert.False(data.Exists("audio/a-first"));
        Assert.True(data.Exists("audio/c-third"));
        // None accepted: no mod, the game's data as it is.
        accept.Clear();
        data.Reload();
        Assert.Equal((3, "the game"), (calls, data.ReadText("audio/who")));
        Assert.Equal(["audio/game", "audio/who"], data.List("audio"));
    }

    [Fact]
    public void TheApplicationGivesTheModsFolderItsOptionsFilter()
    {
        using var folder = new TempFolder();
        Lay(folder);
        var mods = new List<string>();
        ModFilter filter = (found, below) =>
        {
            mods.AddRange(found.Select(Path.GetFileName)!);
            return found.Where(m => Path.GetFileName(m) != "B-second.pak");
        };
        var options = new ApplicationOptions { ModFilter = filter };
        var mounts = Application.DataMounts(options, folder["game"], NoVariables, folder["player/mods"]);
        Assert.Same(filter, mounts.Single(m => m.Kind == MountKind.Mods).Filter);
        using (var data = new VirtualFileSystem(mounts))
        {
            Assert.Equal(["B-second.pak", "a-first.pak", "c-third.pak", "d-fourth.pak"], mods);
            Assert.False(data.Exists("audio/b-second"));
            Assert.Equal("d-fourth", data.ReadText("audio/who"));
        }
        // Without the option the mods folder has none (every mod mounts); with the game's packs only, no mods folder at all.
        Assert.Null(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables, folder["player/mods"]).Single(m => m.Kind == MountKind.Mods).Filter);
        Assert.DoesNotContain(Application.DataMounts(options with { GamePacksOnly = true }, folder["game"], NoVariables, folder["player/mods"]), m => m.Kind == MountKind.Mods);
        Assert.Null(new ApplicationOptions().ModFilter);
    }
}

/// <summary>Tests that read the log run alone: <see cref="Log.Sink"/> is one for the process.</summary>
[CollectionDefinition("Mod filter log", DisableParallelization = true)]
public sealed class ModFilterLogCollection;
