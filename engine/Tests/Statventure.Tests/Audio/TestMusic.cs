using Statventure.Audio;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>The game's music as the committed audio.pak holds it (engine/Assets/Packs/; Docs/Assets.md, step 6), loaded once for the audio tests.</summary>
internal static class TestMusic
{
    public static string Pack => Path.Combine(TestPaths.Packs, "audio.pak");

    static readonly Lazy<MusicLibrary> s_library = new(() =>
    {
        using var data = new VirtualFileSystem([Mount.Pack(Pack)]);
        return MusicLibrary.Load(data);
    });

    /// <summary>The 55 themes, 34 instruments, 21 percussion voices and 7 rooms of the committed pack, and the game's 8 sound effects.</summary>
    public static MusicLibrary Library => s_library.Value;
}

/// <summary>
/// The game's sound effects (GM1 step 4; Docs/Game/Foundation.md, "The sounds"; Docs/Audio.md, "The game's sound
/// effects"): the eight interface sounds the user approved by ear, their sources the JSON forms in
/// engine/Assets/Audio/effects/, packed into audio.pak by make-music-data.sh as audio/effects/&lt;name&gt;.
/// </summary>
internal static class GameSounds
{
    /// <summary>Q13's eight, in its order (the game names them in Game/Statventure.Game/Sounds/SoundNames, GM1 step 2).</summary>
    public static readonly string[] Names = ["ui-press", "ui-toggle", "ui-open", "ui-close", "game-saved", "game-opened", "ui-refused", "game-deleted"];

    /// <summary>engine/Assets/Audio/effects/.</summary>
    public static string Folder => Path.GetFullPath(Path.Combine(TestPaths.Packs, "..", "Audio", "effects"));

    /// <summary>The source of <paramref name="name"/>: its patch's JSON form.</summary>
    public static string Source(string name) => File.ReadAllText(Path.Combine(Folder, name + ".json"));

    /// <summary>How long any of them may sound, from the play to its last sample (Q14: each under 0.3 s).</summary>
    public const double MaxSeconds = 0.3;
}
