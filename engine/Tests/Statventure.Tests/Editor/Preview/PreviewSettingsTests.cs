using System.Text;
using Statventure.Editor.Preview;
using Statventure.Render3D;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Preview;

/// <summary>
/// The preview's own files in the editor's <c>editor/</c> folder (G6): the stand-ins per cutscene (P34), read tolerantly and
/// written whole; and the preview's palettes (P32, Q6): a distinct colour per slot, the user's where given.
/// </summary>
public class PreviewSettingsTests
{
    [Fact]
    public void TheStandInsAreWrittenWholeAndReadBack()
    {
        using var folder = new StorageFolder();
        var all = new Dictionary<string, Dictionary<string, StandIn>>
        {
            ["world/cutscenes/b"] = new() { ["king"] = new StandIn("models/figure", "models/acting"), ["glow"] = new StandIn(Colours: new Dictionary<string, string> { ["ember"] = "#ffeedd", ["dim"] = "#000000" }) },
            ["world/cutscenes/a"] = new() { ["spot"] = StandIn.None },
        };
        Assert.True(StandInsFile.Write(folder.Root, all));
        Assert.Equal("""
            {
              "version": 1,
              "cutscenes": {
                "world/cutscenes/a": {
                  "spot": {}
                },
                "world/cutscenes/b": {
                  "glow": {
                    "colours": {
                      "dim": "#000000",
                      "ember": "#ffeedd"
                    }
                  },
                  "king": {
                    "model": "models/figure",
                    "clips": "models/acting"
                  }
                }
              }
            }

            """.Replace("\r\n", "\n"), File.ReadAllText(StandInsFile.File(folder.Root)));
        var read = StandInsFile.Read(folder.Root);
        Assert.Equal(["world/cutscenes/a", "world/cutscenes/b"], read.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(new StandIn("models/figure", "models/acting"), read["world/cutscenes/b"]["king"]);
        Assert.Equal(new Dictionary<string, string> { ["ember"] = "#ffeedd", ["dim"] = "#000000" }, read["world/cutscenes/b"]["glow"].Colours);
        Assert.Equal(StandIn.None, read["world/cutscenes/a"]["spot"]);
    }

    [Fact]
    public void TheStandInsFileIsReadTolerantly()
    {
        using var folder = new StorageFolder();
        void Text(string text) => folder.Write("editor/" + StandInsFile.FileName, Encoding.UTF8.GetBytes(text));
        Assert.Empty(StandInsFile.Read(folder.Root));
        Text("not json");
        Assert.Empty(StandInsFile.Read(folder.Root));
        Text("""{ "version": 2, "cutscenes": { "a": { "x": { "model": "m" } } } }""");
        Assert.Empty(StandInsFile.Read(folder.Root));
        Text("""{ "version": 1, "cutscenes": [] }""");
        Assert.Empty(StandInsFile.Read(folder.Root));
        // What isn't of its kind is left out, the rest kept.
        Text("""
            { "version": 1, "cutscenes": {
                "a": { "x": { "model": 3, "clips": "c" }, "y": "z", "p": { "colours": { "s": "#12345", "t": "#aBcDeF", "u": 4 } } },
                "b": [] } }
            """);
        var read = StandInsFile.Read(folder.Root);
        Assert.Equal(["a"], read.Keys);
        Assert.Equal(new StandIn(null, "c"), read["a"]["x"]);
        Assert.False(read["a"].ContainsKey("y"));
        Assert.Equal(new Dictionary<string, string> { ["t"] = "#aBcDeF" }, read["a"]["p"].Colours);
    }

    [Fact]
    public void ThePreviewsPalettesGiveEachSlotADistinctColourUnlessTheUsersOwn()
    {
        Assert.Equal([("a", "#d77a50"), ("b", "#5f9fd3"), ("c", "#8fc85a")], PreviewColours.For(["a", "b", "a", "c"]));
        Assert.Equal([("a", "#d77a50"), ("b", "#123456"), ("c", "#8fc85a")], PreviewColours.For(["a", "b", "c"], new Dictionary<string, string> { ["b"] = "#123456" }));
        // Past the list, round again.
        var many = PreviewColours.For(Enumerable.Range(0, PreviewColours.Distinct.Length + 1).Select(i => $"s{i}"));
        Assert.Equal(PreviewColours.Distinct.Distinct().Count(), PreviewColours.Distinct.Length);
        Assert.Equal(PreviewColours.Distinct[0], many[^1].Colour);
        var palette = PreviewColours.Palette([("a", "#ff0000")]);
        Assert.Equal(Colors.LinearFromHex("#ff0000"), palette.Slots["a"].Lit);
        Assert.Equal("#a0b1c2", PreviewColours.Read(" A0B1C2 "));
        Assert.Equal("#a0b1c2", PreviewColours.Read("#a0b1c2"));
        Assert.Null(PreviewColours.Read("#a0b1c"));
        Assert.Null(PreviewColours.Read("#a0b1cg"));
        Assert.Null(PreviewColours.Read("violet"));
        Assert.True(PreviewColours.IsHex("#A0b1C2"));
        Assert.False(PreviewColours.IsHex("a0b1c2"));
        Assert.False(PreviewColours.IsHex("#a0b1c23"));
    }
}
