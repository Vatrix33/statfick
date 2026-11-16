using System.Text.Json.Nodes;
using Statventure.Core;
using Statventure.Game.Data.Saves;
using Statventure.Game.Mods;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Storage;

namespace Statventure.Game.Tests;

/// <summary>
/// What a player's own runs leave in the per-user folder, made through the game's real parts for the packaged game to
/// start on (GM1 step 11; Docs/Game/Foundation.md, "How it works", step 11): a character made and saved in its slot by the
/// session (<see cref="GameSession.New"/>, then left, as Quit leaves it), a setting changed (the effects volume, in
/// <c>settings.json</c> by <see cref="GameSettings"/>), the character's file written (<see cref="GameSession.Export"/>),
/// and a mod the game must leave out because it holds a kind the game doesn't know. Each read back as the game reads it.
/// In the suite the folders are scratch ones; <c>Tools/Scripts/end-to-end.sh</c> runs this test built with the packages'
/// version on its player's per-user folder (<see cref="PlayerVariable"/>) and a scratch folder of its own for the files
/// (<see cref="FilesVariable"/>: never the real Documents), then starts the packaged game there.
/// </summary>
public class PlayerFolderTests
{
    /// <summary>The per-user game folder to make them in (end-to-end.sh: its player's <c>stat-fick</c> folder); unset, a scratch one.</summary>
    public const string PlayerVariable = "STATVENTURE_END_TO_END_PLAYER";

    /// <summary>A folder for the character file and the mod (end-to-end.sh: a scratch folder of its own); unset, a scratch one.</summary>
    public const string FilesVariable = "STATVENTURE_END_TO_END_FILES";

    /// <summary>The version the game's program must have (end-to-end.sh: the packages'); unset, the suite's build's.</summary>
    public const string VersionVariable = "STATVENTURE_END_TO_END_VERSION";

    /// <summary>The character's name, its slot, the effects volume set, the character file's and the mod's names.</summary>
    public const string Name = "Ser Alrik", Slot = "ser-alrik", CharacterFileName = "stat-fick-character.json", ModFileName = "odd-charms.pak";
    public const double EffectsVolume = 0.4;

    /// <summary>A kind no game knows (a mod holding one is left out whole, GM1-10, Q4).</summary>
    [Record("e2e.charm", 1)]
    public sealed record Charm
    {
        [Field(1)] public string Title { get; init; } = "";
    }

    [Fact]
    public void ACharacterASettingACharacterFileAndAModTheGameLeavesOutForThePackagedGame()
    {
        using var scratch = new ScratchFolder();
        var player = Environment.GetEnvironmentVariable(PlayerVariable) is { Length: > 0 } given ? Path.GetFullPath(given) : scratch["player/stat-fick"];
        var files = Environment.GetEnvironmentVariable(FilesVariable) is { Length: > 0 } folder ? Path.GetFullPath(folder) : scratch["files"];
        if (Environment.GetEnvironmentVariable(VersionVariable) is { Length: > 0 } version)
            Assert.Equal(version, ProgramInfo.Of(typeof(StatFickGame).Assembly).Version);
        Directory.CreateDirectory(files);
        var characterFile = Path.Combine(files, CharacterFileName);

        // As the game's start makes its parts on the per-user folder (a folder the game's first starts left, or none).
        using (var storage = new GameStorage(player))
        {
            var settings = GameSettings.Load(storage);
            var session = new GameSession(storage, settings);
            Assert.Null(session.ContinueSlot());
            Assert.Empty(session.Slots());
            Assert.Equal(Slot, session.New(Name));
            settings.EffectsVolume = EffectsVolume;
            Assert.True(session.Export(characterFile));
            storage.Flush();
            session.Leave();
            storage.Flush();
        }

        // Read back as the next start reads them: the slot (its save, and the first write kept as its backup), Continue's
        // slot, the setting; the character file as Load Character reads it.
        using (var storage = new GameStorage(player))
        {
            var settings = GameSettings.Load(storage);
            Assert.Equal((Slot, EffectsVolume), (settings.LastCharacter, settings.EffectsVolume));
            var session = new GameSession(storage, settings);
            Assert.Equal(Slot, session.ContinueSlot());
            var slot = session.Slots().Single();
            Assert.Equal((Slot, Name, false), (slot.Slot, slot.Name, slot.Damaged));
            Assert.True(File.Exists(storage.Saves.SavePath(Slot)) && File.Exists(storage.Saves.BackupPath(Slot)));
            Assert.Equal(Name, CharacterFile.Import<SaveGame>(characterFile).Name);
        }
        var written = JsonNode.Parse(File.ReadAllText(Path.Combine(player, "settings.json")))!.AsObject();
        Assert.Equal((Slot, EffectsVolume), (written["lastCharacter"]!.GetValue<string>(), written["effectsVolume"]!.GetValue<double>()));
        Assert.Equal(Statventure.Records.RecordSerializer.ToJson(new SaveGame { Name = Name }).TrimEnd(), File.ReadAllText(characterFile).ReplaceLineEndings("\n").TrimEnd());

        // The mod: its info and an entry of a kind the game doesn't know; the game's check leaves it out naming the entry.
        var mod = Path.Combine(files, ModFileName);
        var writer = new PackWriter();
        writer.AddRecord(ModInfo.Path, new ModInfo { Name = "Odd Charms", Author = "Vatrix", Version = "0.1", Description = "Charms of a kind no game knows." });
        writer.AddRecord("charms/lucky", new Charm { Title = "Lucky charm" });
        writer.Write(mod);
        var report = ModCheck.Check(mod, [], GameMods.GameKinds);
        Assert.Equal(["charms/lucky: a record of type e2e.charm, which isn't a record type known here"], report.Problems.Select(p => p.ToString()));
        Assert.Equal("Odd Charms", report.Info?.Name);
    }
}
