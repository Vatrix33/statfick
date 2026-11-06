using System.Security.Cryptography;
using Statventure.Audio;
using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records;

namespace Statventure.Tests.Packs;

/// <summary>
/// The mod's info entry (Docs/Editor.md, step 7; G1, M11-15): the engine's record kind <c>mod.info</c>, a mod's
/// name, author, version and description as text, at <c>mod/info</c>; through both forms, in a pack, its dump
/// and a loose folder.
/// </summary>
public class ModInfoTests
{
    /// <summary>Every field set, with text an author may write: non-ASCII letters, a line end, quotes, an ampersand.</summary>
    internal static ModInfo EveryField() => new()
    {
        Name = "Faster Vale",
        Author = "Vatrix & Ŝon",
        Version = "1.2 beta",
        Description = "Alder Vale at a quicker tempo.\nAnd a new chime: \"Ember\" — for the marsh.",
    };

    [Fact]
    public void TheFixturesAreRecorded()
    {
        RecordFixtures.Ensure("every-field", EveryField());
        RecordFixtures.Ensure("empty", new ModInfo());
    }

    [Fact]
    public void ItIsTheEnginesKindModInfoAtModInfo()
    {
        Assert.Equal("mod/info", ModInfo.Path);
        Assert.True(PackPath.IsValid(ModInfo.Path));
        Assert.Equal(typeof(ModInfo), RecordTypes.Engine.Find("mod.info"));
        // For the editor and, later, the game's mod list, which aren't the engine.
        Assert.True(typeof(ModInfo).IsPublic);
        Assert.Equal("mod.info", RecordSerializer.RootTypeName(RecordSerializer.ToBinary(new ModInfo())));
    }

    [Fact]
    public void ItComesBackFromBothForms()
    {
        foreach (var info in new[] { EveryField(), new ModInfo(), new ModInfo { Name = "Only a Name" }, new ModInfo { Description = "Only\r\na description" } })
        {
            RecordAssert.Equal(info, RecordSerializer.FromBinary<ModInfo>(RecordSerializer.ToBinary(info)));
            RecordAssert.Equal(info, RecordSerializer.FromJson<ModInfo>(RecordSerializer.ToJson(info)));
        }
    }

    /// <summary>The JSON form: the four as text in their order, each written even when empty (Q33: only a null whose default is null is left out).</summary>
    [Fact]
    public void ItsJsonFormIsTheFourAsText()
    {
        Assert.Equal("""
            {
              "$types": {
                "mod.info": 1
              },
              "name": "Faster Vale",
              "author": "Vatrix & Ŝon",
              "version": "1.2 beta",
              "description": "Alder Vale at a quicker tempo.\nAnd a new chime: \"Ember\" — for the marsh."
            }

            """.Replace("\r", ""), RecordSerializer.ToJson(EveryField()));
        Assert.Equal("""
            {
              "$types": {
                "mod.info": 1
              },
              "name": "",
              "author": "",
              "version": "",
              "description": ""
            }

            """.Replace("\r", ""), RecordSerializer.ToJson(new ModInfo()));
    }

    /// <summary>Written by hand, a key left out is empty text, so an info may give only a name, or nothing.</summary>
    [Fact]
    public void AKeyLeftOutIsEmptyText()
    {
        RecordAssert.Equal(new ModInfo { Name = "Faster Vale" }, RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}, "name": "Faster Vale"}"""u8));
        RecordAssert.Equal(new ModInfo { Author = "Vatrix" }, RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}, "author": "Vatrix"}"""u8));
        RecordAssert.Equal(new ModInfo { Version = "2" }, RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}, "version": "2"}"""u8));
        RecordAssert.Equal(new ModInfo { Description = "x" }, RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}, "description": "x"}"""u8));
        RecordAssert.Equal(new ModInfo(), RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}}"""u8));
        // Text, not a number or null.
        Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}, "version": 2}"""u8));
        Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<ModInfo>("""{"$types": {"mod.info": 1}, "name": null}"""u8));
    }

    /// <summary>
    /// In a pack, read by the engine's tools as any record of its kinds (G1's reason): the dump shows its JSON form,
    /// extracting writes <c>mod/info.json</c> and packing that folder again gives the same pack; the file system
    /// loads it from the pack and from a loose folder.
    /// </summary>
    [Fact]
    public void APacksInfoIsShownExtractedAndLoaded()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord(ModInfo.Path, EveryField());
        writer.Add("audio/readme.txt", "notes"u8);
        writer.Write(folder["faster-vale.pak"]);

        using (var pack = PackFile.Open(folder["faster-vale.pak"]))
        {
            var entry = pack.Find("mod/info")!;
            Assert.True(entry.IsRecord);
            RecordAssert.Equal(EveryField(), RecordSerializer.FromBinary<ModInfo>(pack.Read(entry)));
            var dump = PackDump.ToText(pack);
            Assert.DoesNotContain("\"problem\"", dump);
            Assert.Contains("\"path\": \"mod/info\"", dump);
            Assert.Contains("\"author\": \"Vatrix & Ŝon\"", dump);
            pack.ExtractTo(folder["extracted"]);
        }
        Assert.Equal(RecordSerializer.ToJson(EveryField()), File.ReadAllText(folder["extracted/mod/info.json"]));
        var again = new PackWriter();
        again.AddFolder(folder["extracted"]);
        again.Write(folder["again.pak"]);
        Assert.Equal(File.ReadAllBytes(folder["faster-vale.pak"]), File.ReadAllBytes(folder["again.pak"]));

        using var data = new VirtualFileSystem([Mount.Pack(folder["faster-vale.pak"])]);
        RecordAssert.Equal(EveryField(), data.Load<ModInfo>("mod/info"));
        using var loose = new VirtualFileSystem([Mount.Folder(folder["extracted"])]);
        RecordAssert.Equal(EveryField(), loose.Load<ModInfo>("mod/info"));
    }
}

/// <summary>
/// A mod carrying its info entry is mounted and loaded as the same mod without it (Docs/Editor.md, step 7): the
/// game never reads the entry, so the paths of its data, what each holds and where it comes from, the music it
/// loads and the lines it logs are the same; an entry the engine can't read changes nothing until something reads
/// it, and then its error names the mod (G2, as today); the game's own packs leave the entry's folder to mods, so
/// the entry replaces none of the game's data. Run alone: they read the log (<see cref="Log.Sink"/> is one for
/// the process).
/// </summary>
[Collection("Mod info log")]
public class ModInfoModsTests
{
    static readonly Func<string, string?> NoVariables = _ => null;

    /// <summary>
    /// The game's folder as the build lays it out (the committed audio and ui packs, a classes folder) and the
    /// player's two mods: the first changes a theme's tempo and a class, the second adds an effect patch; each with
    /// <paramref name="info"/>'s entry for it when given (null leaves the entry out).
    /// </summary>
    static List<Mount> Shipped(TempFolder folder, Func<string, Action<PackWriter>?> info)
    {
        foreach (var area in new[] { "audio", "ui" })
            File.Copy(Path.Combine(TestPaths.Packs, area + ".pak"), folder.Write($"game/packs/{area}.pak", []), overwrite: true);
        folder.Write("game/classes/black-knight.json", "{\"name\": \"shipped\"}");

        ThemeSpec theme;
        using (var audio = PackFile.Open(folder["game/packs/audio.pak"]))
            theme = RecordSerializer.FromBinary<ThemeSpec>(audio.Read(audio.Entries.First(e => e.Path.StartsWith(MusicNames.Themes + "/", StringComparison.Ordinal))));
        var first = new PackWriter();
        first.AddRecord(MusicNames.PathOf(MusicNames.Themes, theme.Id), theme with { Tempo = theme.Tempo * 1.25 });
        first.Add("classes/black-knight.json", "{\"name\": \"first mod\"}"u8);
        info("1-first")?.Invoke(first);
        first.Write(folder["player/mods/1-first.pak"]);
        var second = new PackWriter();
        second.AddRecord(MusicNames.PathOf(MusicNames.Effects, "ember"), new EffectPatch { Layers = [new() { Source = EffectSource.Sine, Frequency = 660, Length = 0.1 }] });
        info("2-second")?.Invoke(second);
        second.Write(folder["player/mods/2-second.pak"]);
        return Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables, folder["player/mods"]);
    }

    static Action<PackWriter> Info(string name) => writer => writer.AddRecord(ModInfo.Path, new ModInfo { Name = name, Author = "a modder", Version = "1", Description = "the mod " + name });

    static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes))[..16];

    static string Record<T>(T value) where T : notnull => Hash(RecordSerializer.ToBinary(value));

    /// <summary>
    /// What the game gets from its data, the info entry left out: the mounts and the "Data" lines logged while
    /// mounting, loading the music and reloading (as <see cref="Application.Run"/> and <see cref="Application.ReloadData"/>
    /// do: each mount, each mod), every other path with its kind, origin and content, and the music library.
    /// </summary>
    static List<string> WhatTheGameGets(TempFolder folder, List<Mount> mounts)
    {
        var got = new List<string>();
        var packs = new Dictionary<string, PackFile>();
        string Named(string? file) => file is null ? "nothing" : Path.GetRelativePath(folder.Root, file).Replace('\\', '/');
        // A record's binary form as its pack holds it (the file system gives records only as values).
        byte[] Stored(string pack, string path) => (packs.TryGetValue(pack, out var open) ? open : packs[pack] = PackFile.Open(pack)).Read(path);
        var (sink, level) = (Log.Sink, Log.Level);
        Log.Sink = (lineLevel, category, message) =>
        {
            sink?.Invoke(lineLevel, category, message);
            if (category == "Data")
                lock (got)
                    got.Add($"log {lineLevel}: {message.Replace(folder.Root, "<root>")}");
        };
        Log.Level = LogLevel.Info;
        try
        {
            foreach (var mount in mounts)
                Log.Info("Data", mount.ToString());
            using var data = new VirtualFileSystem(mounts);
            var music = MusicLibrary.Load(data);
            data.Reload();
            var reloaded = MusicLibrary.Load(data);
            Assert.Equal(music.Themes, reloaded.Themes);
            foreach (var path in data.List().Where(p => p != ModInfo.Path))
            {
                var origin = data.Origin(path)!;
                var content = data.IsRecord(path) ? Stored(origin, path) : data.ReadAllBytes(path);
                got.Add($"{path}: {(data.IsRecord(path) ? "record " + RecordSerializer.RootTypeName(content) : "file")} {Hash(content)} from {Named(origin)}");
            }
            got.Add("themes: " + string.Join(", ", music.Themes));
            got.AddRange(music.Themes.Select(id => $"theme {id}: {Record(music.Theme(id)!)}"));
            got.AddRange(music.Instruments.Select(p => $"instrument {p.Key}: {Record(p.Value)}"));
            got.AddRange(music.Percussion.Select(p => $"percussion {p.Key}: {Record(p.Value)}"));
            got.AddRange(music.Rooms.Select(p => $"room {p.Key}: {Record(p.Value)}"));
            got.AddRange(music.Effects.Select(p => $"effect {p.Key}: {Record(p.Value)}"));
        }
        finally
        {
            (Log.Sink, Log.Level) = (sink, level);
            foreach (var pack in packs.Values)
                pack.Dispose();
        }
        return got;
    }

    static List<string> WithoutTheEntry()
    {
        using var folder = new TempFolder();
        return WhatTheGameGets(folder, Shipped(folder, _ => null));
    }

    [Fact]
    public void AModWithItsInfoMountsAndLoadsAsWithout()
    {
        var without = WithoutTheEntry();
        Assert.Contains(without, line => line.StartsWith("log Info: mod <root>/player/mods/1-first.pak", StringComparison.Ordinal));
        Assert.Contains("classes/black-knight.json: file " + Hash("{\"name\": \"first mod\"}"u8.ToArray()) + " from player/mods/1-first.pak", without);
        Assert.Contains(without, line => line.StartsWith("effect ember: ", StringComparison.Ordinal));

        using var folder = new TempFolder();
        var mounts = Shipped(folder, Info);
        Assert.Equal(without, WhatTheGameGets(folder, mounts));
        // The entry is there as any entry of a mod: the later mod's at the path, each mod file holding its own.
        using var data = new VirtualFileSystem(mounts);
        Assert.Equal(new ModInfo { Name = "2-second", Author = "a modder", Version = "1", Description = "the mod 2-second" }, data.Load<ModInfo>(ModInfo.Path));
        Assert.Equal(folder["player/mods/2-second.pak"], data.Origin(ModInfo.Path));
        using var first = PackFile.Open(folder["player/mods/1-first.pak"]);
        Assert.Equal("1-first", RecordSerializer.FromBinary<ModInfo>(first.Read(ModInfo.Path)).Name);
        // Only one mod with it.
        using var one = new TempFolder();
        Assert.Equal(without, WhatTheGameGets(one, Shipped(one, mod => mod == "1-first" ? Info(mod) : null)));
    }

    /// <summary>The entries at <c>mod/info</c> the engine can't read as a mod's info, each written into the first mod.</summary>
    public static TheoryData<string> Unreadable => ["newer version", "damaged", "a file", "another kind", "not a record"];

    static Action<PackWriter>? Unreadably(string how, string mod) => mod != "1-first" ? null : how switch
    {
        "newer version" => writer =>
        {
            var binary = RecordSerializer.ToBinary(new ModInfo { Name = "from a newer editor" });
            // The table: one type, its name's length and bytes, then its version.
            var version = 2 + "mod.info".Length;
            Assert.Equal(1, binary[version]);
            binary[version] = 2;
            writer.AddRecordBinary(ModInfo.Path, binary);
        },
        "damaged" => writer => writer.AddRecord(ModInfo.Path, new ModInfo { Name = "damaged later" }),
        "a file" => writer => writer.Add(ModInfo.Path, "Faster Vale, by a modder"u8),
        "another kind" => writer => writer.AddRecord(ModInfo.Path, new RoomSpec(1, 0.5, 0.25)),
        "not a record" => writer => writer.AddRecordBinary(ModInfo.Path, [0]),
        _ => throw new ArgumentException(how),
    };

    /// <summary>A byte of one entry's stored bytes flipped, the index left as it was (so the pack opens and the entry is damaged).</summary>
    static void Damage(string file, string path)
    {
        long offset;
        using (var pack = PackFile.Open(file))
        {
            var entry = pack.Find(path)!;
            // Stored as it is, so the flipped byte is the content's (a compressed stream's first byte may not change it).
            Assert.False(entry.Compressed);
            offset = entry.Offset;
        }
        var bytes = File.ReadAllBytes(file);
        bytes[offset] ^= 0xFF;
        File.WriteAllBytes(file, bytes);
    }

    /// <summary>
    /// G2 as today: a mod whose info the engine can't read (a newer version, damaged, not a mod's info) mounts and
    /// loads as the mod without it, since nothing reads it; reading it is an error naming the mod and the entry.
    /// </summary>
    [Theory]
    [MemberData(nameof(Unreadable))]
    public void AnInfoTheEngineCantReadChangesNothingUntilRead(string how)
    {
        var without = WithoutTheEntry();
        using var folder = new TempFolder();
        var mounts = Shipped(folder, mod => Unreadably(how, mod));
        if (how == "damaged")
            Damage(folder["player/mods/1-first.pak"], ModInfo.Path);
        Assert.Equal(without, WhatTheGameGets(folder, mounts));
        using var data = new VirtualFileSystem(mounts);
        var problem = Assert.Throws<DataFileException>(() => data.Load<ModInfo>(ModInfo.Path));
        Assert.Equal((folder["player/mods/1-first.pak"], ModInfo.Path), (problem.File, problem.Entry));
        Assert.Contains(how switch
        {
            "newer version" => "version 2",
            "damaged" => "damaged",
            "a file" => "a file, not a record (expected a mod.info)",
            "another kind" => "a record of type audio.room, not mod.info",
            _ => "not a readable",
        }, problem.Message);
    }

    /// <summary>
    /// The game's committed packs hold nothing at the entry's path or in its folder (<c>mod/</c>), so a mod's info
    /// replaces none of the game's data, and the folders the game lists (the music's) aren't the entry's.
    /// </summary>
    [Fact]
    public void TheGamesDataLeavesTheEntrysFolderToMods()
    {
        var folder = ModInfo.Path[..ModInfo.Path.LastIndexOf('/')];
        Assert.Equal("mod", folder);
        var packs = Directory.GetFiles(TestPaths.Packs, "*.pak");
        Assert.NotEmpty(packs);
        foreach (var file in packs)
        {
            using var pack = PackFile.Open(file);
            Assert.DoesNotContain(pack.Entries, e => e.Path == ModInfo.Path || e.Path.StartsWith(folder + "/", StringComparison.Ordinal));
        }
        foreach (var listed in new[] { MusicNames.Themes, MusicNames.Instruments, MusicNames.Percussion, MusicNames.Rooms, MusicNames.Effects })
            Assert.False(ModInfo.Path.StartsWith(listed + "/", StringComparison.Ordinal), listed);
    }
}

/// <summary>Tests that read the log run alone: <see cref="Log.Sink"/> is one for the process.</summary>
[CollectionDefinition("Mod info log", DisableParallelization = true)]
public sealed class ModInfoLogCollection;
