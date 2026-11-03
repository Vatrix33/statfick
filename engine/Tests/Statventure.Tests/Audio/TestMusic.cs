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

    /// <summary>The 55 themes, 34 instruments, 21 percussion voices and 7 rooms of the committed pack.</summary>
    public static MusicLibrary Library => s_library.Value;
}
