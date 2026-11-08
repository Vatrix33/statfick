using Statventure.Audio;
using Statventure.Editor.Saving;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// Saving driven as the user drives it (Docs/Editor.md, "How it works", step 11): a scratch folder holding the player's
/// folder (<c>player/</c>, its mods), files to import and a scratch copy of the repo's packs for the dev setup
/// (<c>repo/Assets/Packs/</c>); mods written there, opened over the repo's packs (the harness's dev setup) or the copy;
/// what a saved pack holds read back entry by entry, as stored.
/// </summary>
static class SavingTesting
{
    /// <summary>A room of the game's audio.pak (a mod's copy of it is a game entry changed).</summary>
    public const string GameRoom = "audio/rooms/hall";

    public sealed class Scratch : IDisposable
    {
        public readonly TempFolder Folder = new();

        public string this[string relative] => Folder[relative];

        /// <summary>The player's game folder.</summary>
        public string Player => Folder["player"];

        /// <summary>The player's mods folder.</summary>
        public string Mods => Folder["player/mods"];

        /// <summary>A mod in the player's mods folder holding what <paramref name="fill"/> writes.</summary>
        public string Mod(Action<PackWriter> fill, string name = "my-mod.pak")
        {
            var writer = new PackWriter();
            fill(writer);
            var file = Folder[$"player/mods/{name}"];
            Directory.CreateDirectory(Mods);
            writer.Write(file);
            return file;
        }

        /// <summary>A mod made by hand (<see cref="RawPack"/>), each entry stored as given.</summary>
        public string Mod(RawPack pack, string name = "my-mod.pak") => Folder.Write($"player/mods/{name}", pack.Build());

        /// <summary>A session over the repo's packs (the dev setup), the player's folder the scratch one, its checks started, <paramref name="pack"/> open.</summary>
        public EditorSession Session(string pack, RecordTypes? types = null)
        {
            var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { PlayerFolder = Player };
            if (types is not null)
                session.Types = types;
            _ = session.Checks;
            session.Open(pack);
            return session;
        }

        /// <summary>The editor over the repo's packs, the player's folder the scratch one, <paramref name="pack"/> open (none: nothing open).</summary>
        public EditorHarness Editor(string? pack = null, float width = 1000, float height = 640)
        {
            var editor = new EditorHarness(width, height);
            editor.Session.PlayerFolder = Player;
            if (pack is not null)
                editor.Session.Open(pack);
            editor.Frame();
            return editor;
        }

        /// <summary>A scratch copy of the repo's packs (<c>repo/Assets/Packs/</c>) as the dev setup's game data.</summary>
        public GameData Repo()
        {
            foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            {
                var copy = Folder[$"repo/Assets/Packs/{Path.GetFileName(pack)}"];
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(pack, copy, overwrite: true);
            }
            return GameData.Repo(Folder["repo"]);
        }

        public void Dispose() => Folder.Dispose();
    }

    public static SavingPart Saving(this EditorHarness editor) => editor.View.Part<SavingPart>();

    public static ModInfoForm ModInfoForm(this EditorHarness editor) => editor.View.Part<ModInfoForm>();

    /// <summary>What each entry of a pack file holds: its flags, its content's hash and its stored bytes as they are in the file.</summary>
    public static Dictionary<string, (PackEntryFlags Flags, string Hash, string Stored)> Stored(string file)
    {
        var bytes = File.ReadAllBytes(file);
        using var pack = PackFile.Open(file);
        return pack.Entries.ToDictionary(e => e.Path,
            e => (e.Flags, e.HashHex, Convert.ToHexStringLower(bytes.AsSpan((int)e.Offset, (int)e.StoredLength))), StringComparer.Ordinal);
    }

    /// <summary>What each entry of a pack file holds: whether a record, and its content.</summary>
    public static Dictionary<string, (bool Record, string Content)> Contents(string file)
    {
        using var pack = PackFile.Open(file);
        return pack.Entries.ToDictionary(e => e.Path, e => (e.IsRecord, Convert.ToHexStringLower(pack.Read(e))), StringComparer.Ordinal);
    }

    /// <summary>A record's binary form in the current versions, as text.</summary>
    public static string Binary<T>(T value) where T : notnull => Convert.ToHexStringLower(RecordSerializer.ToBinary(value));

    /// <summary>The repo's audio.pak's room at <paramref name="path"/>.</summary>
    public static RoomSpec GameRoomSpec(string path = GameRoom) => (RoomSpec)Form.FormTesting.AudioPack()[path];
}
