using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Statventure.Game.Figures;
using Statventure.Packs;
using Statventure.Render3D;

namespace Statventure.Game.Tests;

/// <summary>
/// The emblems' colours (the manager's GM2-P31, 2026-11-20): an emblem's six slots filled from its race's accent by one rule,
/// the game's (<see cref="Emblems.Colours"/>) and the scripts' (<c>Tools/Models/humanoid/emblems/palette.py</c>) alike: the
/// colours the user approved the medallions in, half to even where a mix falls on a half, the same constants and rule on
/// both sides, the scenes Blender rendered coloured by it, and the live emblem's seam (<see cref="Emblems.PaletteOf(VirtualFileSystem, string)"/>,
/// GM2 step 11's) filling all six from the game's data.
/// </summary>
public class EmblemPaletteTests
{
    /// <summary>The colours of the approval renders (the manager's scenes, 2026-11-20): the field, the accent and the inlays by race; the rim and the metal gold, the light parts bone.</summary>
    static readonly Dictionary<string, (string Accent, string Field, string AccentParts, string Cut)> Approved = new()
    {
        ["human"] = ("#c7683f", "#633622", "#cc7b53", "#2b1b12"),
        ["elf"] = ("#728577", "#3f423a", "#869381", "#231e18"),
        ["dark-elf"] = ("#77607e", "#41333c", "#8a7586", "#231a18"),
        ["dwarf"] = ("#8b684e", "#493628", "#9a7b5f", "#251b14"),
        ["hobbit"] = ("#858b6c", "#474535", "#969878", "#251e16"),
        ["lizardman"] = ("#66806a", "#3a4034", "#7c8f76", "#221d16"),
        ["half-beast"] = ("#9a6a3f", "#503722", "#a77d53", "#271b12"),
        ["goliath"] = ("#7d7a72", "#443e37", "#8f8a7d", "#241c17"),
        ["merfolk"] = ("#4c9a8a", "#2f4b42", "#67a490", "#1f201a"),
    };

    public static TheoryData<string> Races => new(Approved.Keys);

    /// <summary>Each race's emblem in the colours the user approved its medallion in, from the accent the game's data gives the race.</summary>
    [Theory]
    [MemberData(nameof(Races))]
    public void TheRuleGivesTheApprovedColours(string emblem)
    {
        using var data = EmblemReferenceTests.Data();
        var race = EmblemReferenceTests.Races(data).Single(r => r.Race.Emblem == emblem).Race;
        var (accent, field, parts, cut) = Approved[emblem];
        Assert.Equal(accent, race.Accent);
        Assert.Equal(new Dictionary<string, string>
        {
            ["emblem.rim"] = "#bb8d3c", ["emblem.field"] = field, ["emblem.high"] = "#e1d3ad", ["emblem.metal"] = "#bb8d3c",
            ["emblem.accent"] = parts, ["emblem.cut"] = cut,
        }, Emblems.Colours(race.Accent));
    }

    /// <summary>Every race of the game's data with an emblem is one the user approved, and every approved one is a race's.</summary>
    [Fact]
    public void TheApprovedEmblemsAreTheRacesEmblems()
    {
        using var data = EmblemReferenceTests.Data();
        Assert.Equal(Approved.Keys.Order(StringComparer.Ordinal), EmblemReferenceTests.Races(data).Select(r => r.Race.Emblem!).Order(StringComparer.Ordinal));
    }

    /// <summary>A mix per channel a + (b − a)·t rounded half to even (Python's round, which coloured the approval renders), its edges and a malformed accent refused.</summary>
    [Fact]
    public void AMixRoundsHalfToEvenAndAnAccentIsAColour()
    {
        Assert.Equal(2, Emblems.Mix(1, 2, 0.5));     // 1.5
        Assert.Equal(2, Emblems.Mix(2, 3, 0.5));     // 2.5
        Assert.Equal(4, Emblems.Mix(3, 4, 0.5));     // 3.5
        Assert.Equal(99, Emblems.Mix(199, 26, 0.58)); // 98.66
        Assert.Equal(0, Emblems.Mix(0, 255, 0));
        Assert.Equal(255, Emblems.Mix(0, 255, 1));
        Assert.Equal("#bb8d3c", Emblems.Colours("#000000")["emblem.rim"]);
        Assert.Equal(("#e1d3ad", "#faf7f0"), (Emblems.Colours("#FFFFFF")["emblem.high"], Emblems.Colours("#FFFFFF")["emblem.accent"]));
        Assert.Throws<ArgumentException>(() => Emblems.Colours("c7683f"));
        Assert.Throws<ArgumentException>(() => Emblems.Colours("#c7683"));
        Assert.Throws<ArgumentException>(() => Emblems.Colours("#c7683g"));
        Assert.Throws<ArgumentException>(() => Emblems.Colours("xc7683f"));
    }

    /// <summary>The six slots, in the scripts' order (look.py's EMBLEM_SLOTS), every one filled.</summary>
    [Fact]
    public void TheRuleFillsTheSixSlotsOfTheScripts()
    {
        var look = File.ReadAllText(Path.Combine(RaceOracle.Engine, "Tools", "Models", "humanoid", "look.py"));
        var match = Regex.Match(look, @"^EMBLEM_SLOTS = \[(.*)\]$", RegexOptions.Multiline);
        Assert.True(match.Success, "look.py has no EMBLEM_SLOTS list");
        var slots = Regex.Matches(match.Groups[1].Value, "\"([^\"]*)\"").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(slots, Emblems.Rule.Select(r => r.Slot));
        Assert.Equal(slots, Emblems.Colours("#c7683f").Keys);
    }

    /// <summary>
    /// The scripts' rule (palette.py) is the game's: its gold, bone and ink, and each slot's source, target and amount, as
    /// that file writes them.
    /// </summary>
    [Fact]
    public void TheScriptsRuleIsTheGames()
    {
        var script = File.ReadAllText(Path.Combine(RaceOracle.Engine, "Tools", "Models", "humanoid", "emblems", "palette.py"));
        string Constant(string name) => Regex.Match(script, $"^{name} = \"(#[0-9a-f]{{6}})\"$", RegexOptions.Multiline) is { Success: true } m
            ? m.Groups[1].Value : throw new InvalidOperationException($"palette.py has no {name}");
        Assert.Equal((Emblems.Gold, Emblems.Bone, Emblems.Ink), (Constant("GOLD"), Constant("BONE"), Constant("INK")));
        var named = new Dictionary<string, string> { ["GOLD"] = Emblems.Gold, ["BONE"] = Emblems.Bone, ["INK"] = Emblems.Ink, ["\"accent\""] = "accent", ["None"] = "" };
        var rule = Regex.Matches(script, @"^    ""(emblem\.[a-z]+)"": \((GOLD|BONE|""accent""), (INK|BONE|None), ([0-9.]+)\),$", RegexOptions.Multiline)
            .Select(m => (m.Groups[1].Value, named[m.Groups[2].Value], named[m.Groups[3].Value] is { Length: > 0 } t ? t : null, double.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture)))
            .ToList();
        Assert.Equal(Emblems.Rule.Select(r => (r.Slot, r.From, r.Toward, r.Amount)), rule);
    }

    /// <summary>The scenes Blender rendered the emblems from are coloured by the rule (palette.py checks the same from its side: make-humanoid.sh check).</summary>
    [Fact]
    public void BlendersScenesAreColouredByTheRule()
    {
        using var data = EmblemReferenceTests.Data();
        using var scenes = JsonDocument.Parse(File.ReadAllText(EmblemReferenceTests.ScenesPath));
        foreach (var (_, race) in EmblemReferenceTests.Races(data))
            foreach (var (suffix, _) in EmblemReferenceTests.Grounds)
            {
                var name = EmblemReferenceTests.SceneName(race.Emblem!, suffix);
                var scene = scenes.RootElement.GetProperty("scenes").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
                Assert.Equal(Emblems.Colours(race.Accent), scene.GetProperty("palette").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!));
            }
    }

    /// <summary>
    /// The live emblem's seam (GM2 step 11's view takes its colours through a function of the game's data and a race's id):
    /// every race's six slots, linear as a palette holds them, from its own accent; a race the data hasn't refused.
    /// </summary>
    [Fact]
    public void ThePaletteOfARaceInTheGamesDataFillsTheSixSlots()
    {
        using var data = EmblemReferenceTests.Data();
        foreach (var (id, race) in Statventure.Game.Data.Races.RaceCatalogue.Load(data))
        {
            var palette = Emblems.PaletteOf(data, id);
            Assert.Equal(Emblems.Rule.Select(r => r.Slot).Order(StringComparer.Ordinal), palette.Slots.Keys.Order(StringComparer.Ordinal));
            foreach (var (slot, colour) in Emblems.Colours(race.Accent))
            {
                Assert.Equal(Colors.LinearFromHex(colour), palette.Slots[slot].Lit);
                Assert.Null(palette.Slots[slot].Shadow);
            }
        }
        Assert.NotEqual(Emblems.PaletteOf(data, "human").Slots["emblem.field"].Lit, Emblems.PaletteOf(data, "elf").Slots["emblem.field"].Lit);
        Assert.Throws<ArgumentException>(() => Emblems.PaletteOf(data, "no-such-race"));
    }
}
