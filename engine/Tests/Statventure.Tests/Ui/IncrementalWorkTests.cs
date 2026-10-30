using System.IO.Compression;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// Updates do only the work a change needs (on the game's character sheet): nothing for nothing,
/// styles without layout for changes that don't reach layout, a repaint alone for paint-only
/// changes, and the boxes on the way to a change for a local layout change. (That the results are
/// right is <see cref="IncrementalUpdateTests"/>' part.)
/// </summary>
public class IncrementalWorkTests
{
    static readonly Lazy<JsonElement> Sheet = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "game-ui.json.gz");
        using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        var root = JsonDocument.Parse(stream).RootElement;
        return root.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == "sheet").Clone();
    });

    static UiDocument Build()
    {
        var document = GameLayoutTests.Build(Sheet.Value);
        document.Painted();
        Assert.False(document.IsDirty);
        return document;
    }

    static (long Styles, long Layouts) Work(UiDocument document) => (document.Styles.StylesComputed, document.Layout.BoxesLaidOut);

    static UiElement Button(UiDocument document) => document.QuerySelectorAll("button").First(b => b.Box is not null);

    [Fact]
    public void AnUpdateWithoutChangesDoesNothing()
    {
        var document = Build();
        var before = Work(document);
        document.Update();
        Assert.Equal(before, Work(document));
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void AttributesNoRuleUsesChangeNothing()
    {
        var document = Build();
        var before = Work(document);
        Button(document).SetAttribute("data-unused", "1");
        document.Root.SetAttribute("data-unused", "1");
        document.Update();
        Assert.Equal(before, Work(document));
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void AClassNoRuleUsesNeitherLaysOutNorRepaints()
    {
        var document = Build();
        var before = Work(document);
        Button(document).AddClass("no-rule-has-this-class");
        document.Update();
        // The game's [class*="item-rarity-"] rules look at every class change of the element.
        Assert.InRange(Work(document).Styles - before.Styles, 0, 10);
        Assert.Equal(before.Layouts, Work(document).Layouts);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void AClassRulesUseRestylesWhatTheRulesReach()
    {
        var document = Build();
        var before = Work(document);
        Button(document).AddClass("is-selected");
        document.Update();
        Assert.InRange(Work(document).Styles - before.Styles, 1, 100);
    }

    [Fact]
    public void ARestyleOfEverythingKeepsLayoutWhenNothingChanged()
    {
        var document = Build();
        var before = Work(document);
        document.AddStylesheet(".no-element-has-this-class { color: red; }", "unused");
        document.Update();
        var after = Work(document);
        Assert.True(after.Styles - before.Styles > 500);
        Assert.Equal(before.Layouts, after.Layouts);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void APaintOnlyChangeRepaintsWithoutLayout()
    {
        var document = Build();
        var before = Work(document);
        Button(document).SetAttribute("style", "color: rgb(200, 10, 10); background: rgb(10, 20, 30); box-shadow: 0 0 4px black");
        document.Update();
        Assert.Equal(before.Layouts, Work(document).Layouts);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void ScrollingRepaintsWithoutLayout()
    {
        var document = Build();
        var before = Work(document);
        document.Root.ScrollTop = 120;
        document.Update();
        Assert.Equal(before, Work(document));
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void ATextChangeLaysOutTheBoxesOnItsWay()
    {
        var document = Build();
        var text = document.Body.Descendants().Where(e => e.Box is not null).SelectMany(e => e.Children.OfType<UiText>()).First(t => t.Text.Trim().Length > 3);
        var before = Work(document);
        text.Text += " and more";
        document.Update();
        var laidOut = Work(document).Layouts - before.Layouts;
        Assert.InRange(laidOut, 1, 60);
        // Only :empty looks at text: at most the text's element is restyled.
        Assert.InRange(Work(document).Styles - before.Styles, 0, 1);
    }

    [Fact]
    public void ALocalSizeChangeLaysOutTheBoxesOnItsWay()
    {
        var document = Build();
        var before = Work(document);
        Button(document).SetAttribute("style", "padding-left: 31px");
        document.Update();
        Assert.InRange(Work(document).Layouts - before.Layouts, 1, 80);
    }
}
