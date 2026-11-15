using Statventure.Game.Sounds;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests.Sounds;

/// <summary>
/// The game's sounds' names: GM1's eight interface sounds (GM1-11, Q13; Docs/Game/Foundation.md, "The sounds"), final
/// since GM1 step 2, and GM2's seven for the hero (Q38, GM2 step 12; Docs/Game/Hero.md, "How it works", step 12).
/// </summary>
public class SoundNamesTests
{
    [Fact]
    public void TheEightNamesAndWhereTheirPatchesAre()
    {
        Assert.Equal(
            ["ui-press", "ui-toggle", "ui-open", "ui-close", "game-saved", "game-opened", "ui-refused", "game-deleted"],
            [SoundNames.Press, SoundNames.Toggle, SoundNames.Open, SoundNames.Close, SoundNames.Saved, SoundNames.Opened, SoundNames.Refused, SoundNames.Deleted]);
        Assert.Equal("audio/effects/ui-press", SoundNames.PatchPath(SoundNames.Press));
    }

    /// <summary>GM2's seven, in Q38's order (a race chosen, a maker choice changed, the random roll, a point spent, a level up, a respec, the hero confirmed).</summary>
    [Fact]
    public void TheHerosSevenNames()
    {
        Assert.Equal(
            ["race-chosen", "maker-choice", "maker-random", "point-spent", "level-up", "respec", "hero-confirmed"],
            [SoundNames.RaceChosen, SoundNames.MakerChoice, SoundNames.MakerRandom, SoundNames.PointSpent, SoundNames.LevelUp, SoundNames.Respec, SoundNames.HeroConfirmed]);
        Assert.Equal(["race-chosen", "maker-choice", "maker-random", "point-spent", "level-up", "respec", "hero-confirmed"], SoundNames.Hero);
        Assert.Equal("audio/effects/hero-confirmed", SoundNames.PatchPath(SoundNames.HeroConfirmed));
    }

    /// <summary>All of them: GM1's eight in its design's order, then GM2's seven; no name twice.</summary>
    [Fact]
    public void AllIsTheFifteen()
    {
        Assert.Equal(
            ["ui-press", "ui-toggle", "ui-open", "ui-close", "game-saved", "game-opened", "ui-refused", "game-deleted",
             "race-chosen", "maker-choice", "maker-random", "point-spent", "level-up", "respec", "hero-confirmed"],
            SoundNames.All);
        Assert.Equal(SoundNames.All.Count, SoundNames.All.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>The committed <c>audio.pak</c> (GM1 step 4's and GM2 step 12's patches) holds a record at each name's patch path, and no other effect.</summary>
    [Fact]
    public void TheCommittedSoundPackHasAPatchForEachName()
    {
        using var pack = PackFile.Open(Path.Combine(TestPaths.Packs, "audio.pak"));
        var effects = pack.Entries.Where(e => e.Path.StartsWith("audio/effects/", StringComparison.Ordinal)).ToList();
        Assert.Equal(SoundNames.All.Select(SoundNames.PatchPath).Order(StringComparer.Ordinal), effects.Select(e => e.Path).Order(StringComparer.Ordinal));
        Assert.All(effects, e => Assert.True(e.IsRecord, e.Path));
    }
}
