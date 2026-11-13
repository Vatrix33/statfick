using Statventure.Game.Session;
using Statventure.Storage;

namespace Statventure.Game.Tests;

/// <summary>
/// The slot's file named from the character's name (Q1; Docs/Game/Foundation.md, "The slot's file" and "How it works",
/// step 5): "Ser Alrik" is <c>ser-alrik</c>, a second one <c>ser-alrik-2</c>, and each edge case of the rule as decided.
/// </summary>
public class SlotNamesTests
{
    /// <summary>The slot for <paramref name="name"/> with <paramref name="taken"/> already there.</summary>
    static string Slot(string name, params string[] taken) => SlotNames.Free(name, taken.Contains);

    [Theory]
    [InlineData("Ser Alrik", "ser-alrik")]
    [InlineData("Character 1", "character-1")]
    [InlineData("SER ALRIK", "ser-alrik")]
    [InlineData("  Ser   Alrik!! ", "ser-alrik")]
    [InlineData("--Ser--Alrik--", "ser-alrik")]
    [InlineData("Ser_Alrik.the.Bold", "ser-alrik-the-bold")]
    [InlineData("Ser Alrik, 2nd of Vale", "ser-alrik-2nd-of-vale")]
    [InlineData("d'Arc", "d-arc")]
    [InlineData("007", "007")]
    public void LettersAndDigitsAreKeptAndEveryOtherRunIsOneDash(string name, string slot) => Assert.Equal(slot, Slot(name));

    [Theory]
    [InlineData("Élise", "elise")]
    [InlineData("Ŝon Ñúñez", "son-nunez")]
    [InlineData("Zoë Brontë", "zoe-bronte")]
    [InlineData("Ångström", "angstrom")]
    [InlineData("Kelvin", "kelvin")]
    [InlineData("Élise", "elise")]
    public void AccentsAreTakenOffAsTheEditorsImportNamesDo(string name, string slot) => Assert.Equal(slot, Slot(name));

    [Theory]
    [InlineData("Weiß", "wei")]
    [InlineData("Ærwyn", "rwyn")]
    [InlineData("Bjørn", "bj-rn")]
    [InlineData("Влад 3", "3")]
    [InlineData("Ærø", "r")]
    public void LettersOutsideAToZAreOtherCharacters(string name, string slot) => Assert.Equal(slot, Slot(name));

    [Theory]
    [InlineData("日本")]
    [InlineData("!!!")]
    [InlineData("Æ")]
    [InlineData("🜂")]
    [InlineData("_._")]
    [InlineData("")]
    public void ANameThatKeepsNothingIsCharacter(string name)
    {
        Assert.Equal("character", SlotNames.Fallback);
        Assert.Equal(SlotNames.Fallback, Slot(name));
    }

    [Fact]
    public void ATakenSlotGetsTheNextNumber()
    {
        Assert.Equal("ser-alrik-2", Slot("Ser Alrik", "ser-alrik"));
        Assert.Equal("ser-alrik-3", Slot("Ser Alrik", "ser-alrik", "ser-alrik-2"));
        // The first free one: a gap is filled.
        Assert.Equal("ser-alrik-2", Slot("Ser Alrik", "ser-alrik", "ser-alrik-3"));
        // Only the stem's own numbers: another name that meets them takes its turn.
        Assert.Equal("ser-alrik-2-2", Slot("Ser Alrik 2", "ser-alrik-2"));
        Assert.Equal("character-2", Slot("!!!", "character"));
        Assert.Equal("character-1-2", Slot("Character 1", "character-1"));
    }

    [Fact]
    public void NamesDifferingOnlyInCaseOrAccentsShareTheirStem()
    {
        Assert.Equal("elise-2", Slot("ELISE", "elise"));
        Assert.Equal("elise-2", Slot("Élise", "elise"));
        Assert.Equal("ser-alrik-2", Slot("ser alrik", "ser-alrik"));
    }

    [Theory]
    [InlineData("Con", "con-2")]
    [InlineData("AUX", "aux-2")]
    [InlineData("nul", "nul-2")]
    [InlineData("Prn!", "prn-2")]
    [InlineData("COM1", "com1-2")]
    [InlineData("lpt9", "lpt9-2")]
    [InlineData("Com 1", "com-1")]
    [InlineData("Con.", "con-2")]
    [InlineData("Con Man", "con-man")]
    [InlineData("Lpt", "lpt")]
    [InlineData("Aux.txt", "aux-txt")]
    public void AWindowsDeviceNameStartsAtTwo(string name, string slot)
    {
        Assert.Equal(slot, Slot(name));
        Assert.True(SaveSlots.IsValidName(slot));
    }

    [Fact]
    public void ADeviceNamesNumbersGoOnFromTwo() => Assert.Equal("con-3", Slot("Con", "con-2"));

    [Fact]
    public void AVeryLongNameIsCutToTheSlotNamesLimit()
    {
        // A character's name has at most 80 characters, and its stem never more: 80 letters are kept whole.
        var eighty = new string('a', 80);
        Assert.Equal(eighty, Slot(eighty));
        Assert.Equal(eighty + "-2", Slot(eighty, eighty));
        // The rule itself on any name: cut so the slot with its number has at most 100 characters (the candidates first,
        // so a rule that never gives a slot name fails here rather than looking for one forever).
        var long150 = new string('b', 150);
        Assert.Equal(new string('b', 100), SlotNames.Candidate(SlotNames.Stem(long150), 1));
        Assert.Equal(new string('b', 97) + "-10", SlotNames.Candidate(SlotNames.Stem(long150), 10));
        Assert.Equal(new string('b', 100), Slot(long150));
        Assert.Equal(new string('b', 98) + "-2", Slot(long150, new string('b', 100)));
        // A '-' left at the cut is taken off: 99 letters, a space and more, cut after the dash.
        var dashAtCut = new string('c', 99) + " dd";
        Assert.Equal(new string('c', 99), Slot(dashAtCut));
        Assert.Equal(new string('c', 98) + "-2", Slot(dashAtCut, new string('c', 99)));
        Assert.All(new[] { Slot(long150), Slot(dashAtCut), Slot(new string('x', 99) + "!y") }, s => Assert.True(SaveSlots.IsValidName(s), s));
    }
}
