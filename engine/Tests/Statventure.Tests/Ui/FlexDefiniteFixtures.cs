namespace Statventure.Tests.Ui;

/// <summary>
/// A layout fixture of when a column flexbox's item has a definite height (GM1 step 1b: the game's equipment slots, a
/// <c>flex: 1</c> column flexbox inside another of indefinite height). Chromium 152 measures each at 1 and 1.65
/// (Reference/scaled/layout-flexdef-&lt;name&gt;@&lt;scale&gt;.json, Tools/PaintReference with <c>--measure=layout</c>).
/// </summary>
public sealed record FlexDefiniteFixture(string Name, int Width, int Height, string Css, string Html);

public static class FlexDefiniteFixtures
{
    const string Tall = "<i>one</i><i>two</i><i>three</i><i>four</i><i>five</i><i>six</i>";

    /// <summary>The game's equipment slot reduced: a grid row, the slot, its content and the control, each a column flexbox.</summary>
    const string SlotCss = """
        body { font: 16px/20px Lato; }
        .row { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; width: 520px; }
        .slot { display: flex; flex-direction: column; justify-content: space-between; min-height: 164px; padding: 15px; overflow: hidden; border: 1px solid #888; }
        .content { position: relative; min-height: 66px; display: flex; flex: 1; flex-direction: column; }
        .control { display: flex; flex: 1; flex-direction: column; justify-content: flex-start; gap: 5px; min-height: 108px; padding: 12px; border: 1px solid #444; font: inherit; }
        .name { overflow: hidden; }
        i { display: block; }
        """;

    public static readonly FlexDefiniteFixture[] All =
    [
        // The equipment slot: a button control (the game's) and a div one.
        new("slot", 560, 520, SlotCss, $"""
            <div class="row" id="row"><article class="slot" id="slot"><span>Primary hand</span><div class="content" id="content"><button class="control" id="control"><span class="name" id="name">A long name</span><span>meta</span><span id="stats">{Tall}</span></button></div></article>
            <article class="slot" id="slot2"><span>Off hand</span><div class="content" id="content2"><div class="control" id="control2"><span class="name">Short</span><span id="stats2">{Tall}{Tall}</span></div></div></article></div>
            """),
        // The same slot under a block (no grid), and with no min-height on the slot.
        new("slot-block", 400, 520, SlotCss + ".plain .slot { min-height: 0; }", $"""
            <div style="width: 300px"><article class="slot" id="slot"><div class="content" id="content"><div class="control" id="control"><span class="name">Name</span><span id="stats">{Tall}</span></div></div></article></div>
            <div style="width: 300px" class="plain"><article class="slot" id="slot2"><div class="content" id="content2"><div class="control" id="control2"><span class="name">Name</span><span id="stats2">{Tall}</span></div></div></article></div>
            """),
        // A 0 % basis in a column flexbox of auto height (with a min-height too), of a definite height, and inside an item
        // flexed in a definite one; the columns side by side in a row flexbox (items of auto height there).
        new("column-basis", 520, 400, """
            body { font: 16px/20px Lato; }
            .cols { display: flex; align-items: flex-start; gap: 8px; }
            .col { display: flex; flex-direction: column; width: 120px; border: 1px solid #888; }
            .grow { flex: 1; }
            .min { min-height: 60px; }
            .pin { min-height: 30px; }
            i { display: block; }
            """, $"""
            <div class="cols"><div class="col" id="auto"><div class="grow" id="auto-item">{Tall}</div></div>
            <div class="col min" id="min"><div class="grow" id="min-item">{Tall}</div></div>
            <div class="col" id="fixed" style="height: 200px"><div class="grow" id="fixed-item">{Tall}</div><div id="fixed-other">x</div></div>
            <div class="col" id="outer" style="height: 300px"><div class="col grow" id="inner" style="width: auto"><div class="grow pin" id="inner-item">{Tall}</div><div id="inner-other">x</div></div></div></div>
            <div class="col" id="nested"><div class="col grow" id="nested-inner" style="width: auto"><div class="grow pin" id="nested-item">{Tall}</div></div></div>
            """),
    ];
}
