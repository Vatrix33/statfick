using System.Runtime.CompilerServices;
using Statventure.Core;
using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Records;

namespace Statventure.Tests.Storage;

/// <summary>A game folder of the test's own under the system's temporary folder, deleted afterwards (never the real one).</summary>
sealed class StorageFolder : IDisposable
{
    public StorageFolder() => Root = Directory.CreateTempSubdirectory("statventure-storage-").FullName;

    public string Root { get; }

    public string this[string relative] => Path.Combine(Root, relative);

    public string Write(string relative, byte[] content)
    {
        var file = this[relative];
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, content);
        return file;
    }

    /// <summary>The files below the folder, relative, in ordinal order.</summary>
    public string[] Files() =>
        [.. Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(Root, f).Replace('\\', '/')).Order(StringComparer.Ordinal)];

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>
/// The committed storage data (Tests/Statventure.Tests/Data/Storage): old-version saves, character
/// files and settings files, and the Documents folder reference. Missing save and character fixtures
/// are written when the tests run with STATVENTURE_RECORD_FIXTURES=1 (never an existing one).
/// </summary>
static class StorageTestData
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Folder => Path.Combine(ProjectDirectory(), "Data", "Storage");

    public static string PathOf(string relative) => Path.Combine(Folder, relative);

    public static byte[] Read(string relative) => File.ReadAllBytes(PathOf(relative));

    /// <summary>Makes sure a fixture exists: written from <paramref name="make"/> when recording, else a failure.</summary>
    public static void Ensure(string relative, Func<byte[]> make)
    {
        var path = PathOf(relative);
        if (File.Exists(path))
            return;
        Assert.True(RecordFixtures.Recording, $"no fixture {path}: run the tests with STATVENTURE_RECORD_FIXTURES=1 to write it");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, make());
    }

    /// <summary>The creature the save fixtures were written from (test.creature, step 2's test family).</summary>
    public static Creature Sample() => new()
    {
        Name = "Ogre",
        Speed = 1.5,
        Title = "the Grim",
        Limbs =
        [
            new() { Kind = LimbKind.Arm, Length = 0.75 }, new() { Kind = LimbKind.Leg, Length = 1.25 },
            new() { Kind = LimbKind.Leg, Length = 1.25 }, new() { Kind = LimbKind.Tail, Length = 2.1 },
        ],
        Moves = [new Walk { Cue = "step", Pace = 0.8 }, new Leap { Cue = "jump", Height = 3.3, Spin = true }, new Fly { Cue = "soar", Altitude = 12.5 }],
    };

    /// <summary>What the frozen copies of the past versions held of a current value, to write a past version as it was written then.</summary>
    static object Downgrade(object value, int version) => (value, version) switch
    {
        (Creature c, 1) => new Creature.V1 { Name = c.Name, Legs = c.Limbs.Count(l => l.Kind == LimbKind.Leg), Limbs = [.. c.Limbs.Where(l => l.Kind != LimbKind.Leg)] },
        (Creature c, 2) => new Creature.V2 { Name = c.Name, Limbs = c.Limbs, Speed = c.Speed * 2 },
        (Limb l, 1) => new Limb.V1 { Kind = l.Kind.ToString().ToLowerInvariant(), Length = (float)l.Length },
        _ => throw new ArgumentException($"no version {version} of a {value.GetType().Name}"),
    };

    /// <summary>The versions of the record fixtures, each written from <see cref="Sample"/>, and what each reads as now.</summary>
    public static readonly (string Label, Dictionary<string, int> Versions, Creature Expected)[] Versions =
    [
        ("creature-1", new() { ["test.creature"] = 1, ["test.limb"] = 1 }, new Creature
        {
            // Legs came back as limbs of the default length; the speed is version 2's default, halved; no moves yet.
            Name = "Ogre",
            Limbs = [new() { Kind = LimbKind.Arm, Length = 0.75 }, new() { Kind = LimbKind.Tail, Length = 2.1f }, new() { Kind = LimbKind.Leg }, new() { Kind = LimbKind.Leg }],
            Speed = 0.5,
        }),
        ("creature-2", new() { ["test.creature"] = 2, ["test.limb"] = 1 }, new Creature
        {
            Name = "Ogre",
            Limbs =
            [
                new() { Kind = LimbKind.Arm, Length = 0.75 }, new() { Kind = LimbKind.Leg, Length = 1.25 },
                new() { Kind = LimbKind.Leg, Length = 1.25 }, new() { Kind = LimbKind.Tail, Length = 2.1f },
            ],
            Speed = 1.5,
        }),
        ("creature-3", new(), Sample()),
    ];

    /// <summary>The binary form of <see cref="Sample"/> in the given versions.</summary>
    public static byte[] Binary(Dictionary<string, int> versions) =>
        RecordSerializer.WriteBinary(typeof(Creature), Sample(), new RecordWriteOptions { Versions = versions, Downgrade = Downgrade });

    /// <summary>The JSON form of <see cref="Sample"/> in the given versions.</summary>
    public static byte[] Json(Dictionary<string, int> versions) =>
        RecordSerializer.WriteJson(typeof(Creature), Sample(), new RecordWriteOptions { Versions = versions, Downgrade = Downgrade });

    /// <summary>Records the log lines of the "Storage" category while it runs (tests using it are in the non-parallel "Storage log" collection).</summary>
    public static List<string> CaptureLog(Action action)
    {
        var lines = new List<string>();
        var (sink, level) = (Log.Sink, Log.Level);
        Log.Sink = (lineLevel, category, message) =>
        {
            sink?.Invoke(lineLevel, category, message);
            if (category == "Storage")
                lock (lines)
                    lines.Add($"{lineLevel}: {message}");
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
}

/// <summary>
/// Holds the storage writer at its work on the files <c>which</c> picks (all by default) until
/// released. Disposing releases it, and it is declared after the storage, so a test that fails while
/// holding it can't leave the writer waiting (its thread keeps the process alive, and the storage's
/// Dispose would wait for it forever).
/// </summary>
sealed class HeldWriter : IDisposable
{
    readonly ManualResetEventSlim _release = new();
    readonly ManualResetEventSlim _held = new();

    public HeldWriter(GameStorage storage, Func<string, bool>? which = null, Action<string>? seen = null) =>
        storage.Writer.BeforeWork = file =>
        {
            seen?.Invoke(file);
            if (which?.Invoke(file) == false)
                return;
            _held.Set();
            _release.Wait();
        };

    /// <summary>Waits until the writer is held at a piece of work.</summary>
    public void WaitUntilHeld() => Assert.True(_held.Wait(TimeSpan.FromSeconds(10)), "the writer didn't start");

    public void Release() => _release.Set();

    // The events aren't disposed: the writer thread may still be waking from one.
    public void Dispose() => _release.Set();
}

/// <summary>Tests that read the log run alone: <see cref="Log.Sink"/> is one for the process.</summary>
[CollectionDefinition("Storage log", DisableParallelization = true)]
public sealed class StorageLogCollection;
