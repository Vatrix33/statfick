using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Screens.Races;

namespace Statventure.Game.Tests;

/// <summary>
/// The words of the race carousel and the character maker (GM2 step 8): the old race list's modifiers as its
/// <c>formatValue</c> writes them (the rows' values computed by the old function in Node), and <c>selectRace</c>'s refusals
/// in the old game's words as its test client wrote them (the oracle's <c>sheet-actions</c> chooseRace cases).
/// </summary>
public class HeroTextsTests
{
    [Theory]
    [InlineData(2.0, "2", "+2")]
    [InlineData(-3.0, "-3", "-3")]
    [InlineData(0.0, "0", "+0")]
    [InlineData(-0.0, "0", "+0")]
    [InlineData(0.25, "0.3", "+0.3")]
    [InlineData(-0.25, "-0.2", "-0.2")]
    [InlineData(0.05, "0.1", "+0.1")]
    [InlineData(-0.05, "0", "0")]
    [InlineData(1.05, "1.1", "+1.1")]
    [InlineData(2.45, "2.5", "+2.5")]
    [InlineData(-2.45, "-2.4", "-2.4")]
    [InlineData(12.34, "12.3", "+12.3")]
    [InlineData(0.04, "0", "+0")]
    [InlineData(-1e-9, "0", "0")]
    [InlineData(100.0, "100", "+100")]
    [InlineData(7.95, "8", "+8")]
    public void AModifierIsWrittenAsTheOldListWritesIt(double value, string formatted, string modifier)
    {
        Assert.Equal(formatted, HeroTexts.FormatValue(value));
        Assert.Equal(modifier, HeroTexts.Modifier(value));
    }

    [Fact]
    public void TheRefusalsAreTheOldGamesWordsForTheCasesItRefused()
    {
        var reference = OldGameReference.Load("sheet-actions");
        var refused = 0;
        foreach (var (input, output) in reference.Cases)
        {
            if (input.GetProperty("action").GetProperty("kind").GetString() != "chooseRace" || !output.TryGetProperty("status", out var status)
                || status.GetProperty("state").GetString() != "is-error")
                continue;
            var sheet = ActorRules.Settle(SheetCases.Sheet(input.GetProperty("sheet")), SheetCases.Context(input));
            var choice = ActorRules.ChooseRace(sheet, SheetCases.Context(input), input.GetProperty("action").GetProperty("race").GetString()!, SheetCases.DevMode(input));
            Assert.NotNull(choice.Refused);
            Assert.Equal(status.GetProperty("text").GetString(), HeroTexts.Refusal(choice.Refused.Value));
            refused++;
        }
        // Both refusals are among the old cases.
        Assert.True(refused >= 2, $"{refused} refused cases");
        Assert.Equal("Enable Dev Mode to select a non-playable race", HeroTexts.Refusal(RaceRefusal.NotPlayable));
        Assert.Equal("A chosen race can only be changed in Dev Mode", HeroTexts.Refusal(RaceRefusal.Locked));
        Assert.Throws<ArgumentOutOfRangeException>(() => HeroTexts.Refusal((RaceRefusal)7));
    }

    [Fact]
    public void TheScreensWords()
    {
        var human = new Race { Name = "Human", Passive = new RacePassive { Name = "Rulers of the World" } };
        var goblin = human with { Name = "Goblin", Playable = false };
        Assert.Equal(("Playable", "Selected", "Non-playable · Dev Mode"), (HeroTexts.Kind(human, false), HeroTexts.Kind(human, true), HeroTexts.Kind(goblin, false)));
        Assert.Equal(("Choose Human", "Race locked"), (HeroTexts.Choose(human, false), HeroTexts.Choose(human, true)));
        Assert.Equal(("Begin as Human", "Use this look"), (HeroTexts.Begin(human, false), HeroTexts.Begin(human, true)));
        Assert.Equal("HUMAN · APPEARANCE", HeroTexts.MakerEyebrow(human));
        Assert.Equal("No looks for the Human yet", HeroTexts.NoLooks(human));
        var look = new LookHero { Race = "human", Sex = "female" };
        Assert.Equal("Human · Female · Strong · Test Ranger", HeroTexts.Summary(human, look, "strong", "Test Ranger"));
        Assert.Equal("Human · Female · Strong", HeroTexts.Summary(human, look, "strong", null));
        Assert.Equal(["STR", "VIT", "MND", "DEX", "INT", "WIS", "SPD", "LCK"], HeroTexts.Attributes.Select(a => a.Short));
        Assert.Equal(["Face", "Brows", "Hair", "Beard", "Crest", "Ears", "Tail", "Marks", "Head piece"], LookNames.Categories.Select(HeroTexts.Category));
        Assert.Equal(["Skin", "Hair", "Hair ties", "Eyes", "Brows", "Lips", "Cloth", "Trim", "Trousers", "Crest", "Markings", "Claws", "Eye whites", "Leather", "Buckles"],
            LookNames.Slots.Select(HeroTexts.Slot));
        Assert.Equal(("Odd", "Odd", ""), (HeroTexts.Category("odd"), HeroTexts.Slot("odd"), HeroTexts.Capital("")));
    }
}
