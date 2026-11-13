using Statventure.Game.Sounds;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests.Sounds;

/// <summary>The interface sounds' names (GM1-11, Q13; Docs/Game/Foundation.md, "The sounds"): final since step 2.</summary>
public class SoundNamesTests
{
    [Fact]
    public void TheEightNamesAndWhereTheirPatchesAre()
    {
        Assert.Equal(["ui-press", "ui-toggle", "ui-open", "ui-close", "game-saved", "game-opened", "ui-refused", "game-deleted"], SoundNames.All);
        Assert.Equal(
            ["ui-press", "ui-toggle", "ui-open", "ui-close", "game-saved", "game-opened", "ui-refused", "game-deleted"],
            [SoundNames.Press, SoundNames.Toggle, SoundNames.Open, SoundNames.Close, SoundNames.Saved, SoundNames.Opened, SoundNames.Refused, SoundNames.Deleted]);
        Assert.Equal("audio/effects/ui-press", SoundNames.PatchPath(SoundNames.Press));
    }

    /// <summary>The committed <c>audio.pak</c> (step 4's patches) holds a record at each name's patch path, and no other effect.</summary>
    [Fact]
    public void TheCommittedSoundPackHasAPatchForEachName()
    {
        using var pack = PackFile.Open(Path.Combine(TestPaths.Packs, "audio.pak"));
        var effects = pack.Entries.Where(e => e.Path.StartsWith("audio/effects/", StringComparison.Ordinal)).ToList();
        Assert.Equal(SoundNames.All.Select(SoundNames.PatchPath).Order(StringComparer.Ordinal), effects.Select(e => e.Path).Order(StringComparer.Ordinal));
        Assert.All(effects, e => Assert.True(e.IsRecord, e.Path));
    }
}
