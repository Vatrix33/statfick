namespace Statventure.Tests.Ui;

/// <summary>
/// A paint fixture of one of the rules today's old stylesheet brought (GM1 step 1a): registered custom properties
/// (<c>@property</c>), <c>-webkit-text-fill-color</c> and <c>background-clip: text</c>. Chromium 152 paints it at 1 and
/// 1.65 (Golden/ui-&lt;name&gt;@1.png and @1.65.png) and reports the <see cref="Computed"/> properties' computed values
/// of every element with an id (Reference/scaled/ui-&lt;name&gt;@&lt;scale&gt;.styles.json), with
/// Tools/Scripts/make-scaled-paint-references.sh 1 1 stylesheet-rule-fixtures.json and 1.65 1 stylesheet-rule-fixtures.json.
/// <see cref="Trigger"/> is a class added to the body once the page has been styled (to start transitions).
/// </summary>
public sealed record StylesheetRuleFixture(string Name, int Width, int Height, string Css, string Html, string[] Computed, string? Trigger = null)
{
    public PaintFixture Paint => new(Name, Width, Height, Css, Html);
}

public static class StylesheetRuleFixtures
{
    /// <summary>
    /// Registered custom properties: the descriptors (syntax with spaces, a later rule winning, an invalid one not, an
    /// unknown descriptor, names and keywords in capitals), rules Chromium ignores (each property then unregistered),
    /// initial values of every kind of colour, inheritance and its absence, values invalid at computed-value time,
    /// CSS-wide keywords, var() substituting the computed value, currentcolor kept, keyframes smooth (linear and the
    /// game's ease-in-out), from the underlying value, discrete for a value that isn't a colour and for an unregistered
    /// property, a transition of a registered property and none of an unregistered one or under "all".
    /// </summary>
    public static readonly StylesheetRuleFixture PropertyRule = new("property-rule", 640, 300, """
        body { font: 13px/16px Lato; color: #222; background: #fff; }
        @property --tone { syntax: "<color>"; inherits: true; initial-value: #14b351; }
        @property --flat { syntax: "<color>"; inherits: false; initial-value: rgb(10 20 30 / 50%); }
        @property --named { syntax: " <color> "; inherits: true; initial-value: rebeccapurple; }
        @property --cur { syntax: "<color>"; inherits: true; initial-value: currentcolor; }
        @property --mix { syntax: "<color>"; inherits: true; initial-value: color-mix(in srgb, red 25%, blue); }
        @property --hsl { syntax: "<color>"; inherits: false; initial-value: hsl(30deg 60% 40%); }
        @property --clear { syntax: "<color>"; inherits: true; initial-value: transparent; }
        @property --twice { syntax: "<color>"; inherits: true; initial-value: #c00; }
        @property --twice { syntax: "<color>"; inherits: false; initial-value: #00c; }
        @property --kept { syntax: "<color>"; inherits: true; initial-value: #0c0; }
        @property --kept { syntax: "<color>"; inherits: true; initial-value: 4px; }
        @property --extra-descriptor { syntax: "<color>"; inherits: true; initial-value: #123456; color: red; }
        @property --upper { SYNTAX: "<color>"; INHERITS: FALSE; INITIAL-VALUE: #234; }
        @property --bad-syntax { syntax: "<colour>"; inherits: true; initial-value: red; }
        @property --bad-case { syntax: "<Color>"; inherits: true; initial-value: red; }
        @property --unquoted { syntax: <color>; inherits: true; initial-value: red; }
        @property --no-initial { syntax: "<color>"; inherits: true; }
        @property --no-inherits { syntax: "<color>"; initial-value: red; }
        @property --bad-inherits { syntax: "<color>"; inherits: yes; initial-value: red; }
        @property --bad-initial { syntax: "<color>"; inherits: true; initial-value: 12px; }
        @property --var-initial { syntax: "<color>"; inherits: true; initial-value: var(--tone); }
        @property --important { syntax: "<color>"; inherits: true; initial-value: red !important; }
        @property --two-colors { syntax: "<color>"; inherits: true; initial-value: red blue; }
        @property --junk junk { syntax: "<color>"; inherits: true; initial-value: red; }
        @property no-dashes { syntax: "<color>"; inherits: true; initial-value: red; }
        .row { display: flex; flex-wrap: wrap; margin: 2px 4px; }
        .sw { width: 26px; height: 14px; margin: 2px; border: 3px solid #000; }
        .a { --tone: #8a3cf0; --flat: hsl(200 50% 50%); }
        .b { --flat: red; }
        .c { --tone: 12px; --flat: nope; --named: var(--missing); }
        .e { --flat: inherit; }
        .g { --tone: initial; }
        .h { --flat: unset; }
        .ref { --ref: var(--tone); color: var(--tone); border-color: color-mix(in srgb, var(--tone) 50%, #000); }
        .u { --bad-syntax: #c33; --bad-case: blue; --no-initial: #36c; }
        .cc { color: #36c; --cur: currentcolor; border-color: var(--cur); }
        .cc .sw { color: #c63; border-color: var(--cur); background: var(--cur); }
        @keyframes tone { 0%, 100% { --tone: #14b351; } 50% { --tone: #8a3cf0; } }
        @keyframes flat { from { --flat: #f00; } to { --flat: #00f; } }
        @keyframes part { 50% { --tone: #8a3cf0; } }
        @keyframes bad { from { --tone: 12px; } to { --tone: #8a3cf0; } }
        @keyframes width { from { --w: 10px; } to { --w: 40px; } }
        .t { animation: tone 4s linear infinite paused; border-color: var(--tone); }
        .t .sw { background: var(--tone); border-color: color-mix(in srgb, var(--tone) 68%, #000); }
        #t1 { animation-delay: -1s; }
        #t2 { animation-delay: -3.2s; }
        #t3 { animation-timing-function: ease-in-out; animation-delay: -1.3s; }
        #f1 { animation: flat 2s linear paused -0.5s; background: var(--flat); }
        #f1 .sw { background: var(--flat); }
        #p1 { --tone: #c00; animation: part 2s linear paused -0.5s; background: var(--tone); }
        #b1 { animation: bad 1s linear paused -0.3s; background: var(--tone); }
        #b2 { animation: bad 1s linear paused -0.7s; background: var(--tone); }
        #w1 { animation: width 1s linear paused -0.3s; width: var(--w); }
        #w2 { animation: width 1s linear paused -0.7s; width: var(--w); }
        .tr { --tone: #14b351; transition: --tone 10000s linear -2500s; background: var(--tone); }
        .tr-all { --tone: #14b351; transition: all 10000s linear -2500s; background: var(--tone); }
        .tr-un { --w: 10px; transition: --w 10000s linear -2500s; width: var(--w); }
        body.go :is(.tr, .tr-all) { --tone: #8a3cf0; }
        body.go .tr-un { --w: 40px; }
        """, """
        <div class="row">
        <div class="sw" id="tone" style="background: var(--tone)"></div><div class="sw" id="flat" style="background: var(--flat)"></div>
        <div class="sw" id="named" style="background: var(--named)"></div><div class="sw" id="cur" style="color: #a52; background: var(--cur)"></div>
        <div class="sw" id="mix" style="background: var(--mix)"></div><div class="sw" id="hsl" style="background: var(--hsl)"></div>
        <div class="sw" id="clear" style="background: var(--clear, #f0f)"></div><div class="sw" id="twice" style="background: var(--twice)"></div>
        <div class="sw" id="kept" style="background: var(--kept)"></div><div class="sw" id="extra" style="background: var(--extra-descriptor)"></div>
        <div class="sw" id="upper" style="background: var(--upper)"></div><div class="sw" id="ignored" style="background: var(--bad-syntax, #888)"></div>
        <div class="sw" id="no-initial" style="background: var(--no-initial, #999)"></div><div class="sw" id="var-initial" style="background: var(--var-initial, #aaa)"></div>
        </div>
        <div class="row">
        <div class="a" id="a"><div class="sw" id="a-child" style="background: var(--tone); border-color: var(--flat)"></div></div>
        <div class="b"><div class="sw" id="b-child" style="background: var(--flat)"></div></div>
        <div class="a"><div class="sw c" id="c" style="background: var(--tone); border-color: var(--flat)"></div></div>
        <div class="a"><div class="sw e" id="e" style="background: var(--flat)"></div></div>
        <div class="a"><div class="sw g" id="g" style="background: var(--tone)"></div></div>
        <div class="b"><div class="sw h" id="h" style="background: var(--flat)"></div></div>
        <div class="a"><div class="sw ref" id="ref" style="background: var(--ref)"></div></div>
        <div class="u" id="u"><div class="sw" id="u-child" style="background: var(--bad-syntax); border-color: var(--no-initial)"></div></div>
        <div class="cc" id="cc"><div class="sw" id="cc-child"></div></div>
        </div>
        <div class="row">
        <div class="t" id="t1"><div class="sw" id="t1-child"></div></div>
        <div class="t" id="t2"><div class="sw" id="t2-child"></div></div>
        <div class="t" id="t3"><div class="sw" id="t3-child"></div></div>
        <div class="sw" id="f1"><div class="sw" id="f1-child"></div></div>
        <div class="sw" id="p1"></div><div class="sw" id="b1"></div><div class="sw" id="b2"></div>
        <div class="sw" id="w1"></div><div class="sw" id="w2"></div>
        <div class="sw tr" id="tr"></div><div class="sw tr-all" id="tr-all"></div><div class="sw tr-un" id="tr-un"></div>
        </div>
        """,
        ["--tone", "--flat", "--named", "--cur", "--mix", "--hsl", "--clear", "--twice", "--kept", "--extra-descriptor", "--upper",
         "--bad-syntax", "--bad-case", "--unquoted", "--no-initial", "--no-inherits", "--bad-inherits", "--bad-initial", "--var-initial",
         "--important", "--two-colors", "--junk", "--ref", "--w", "color", "background-color", "border-top-color"],
        Trigger: "go");

    /// <summary>
    /// <c>-webkit-text-fill-color</c>: a fill over the text's colour, a transparent one under a shadow of
    /// <c>currentcolor</c> (which stays the colour), a translucent one, inherited as a colour, the initial
    /// <c>currentcolor</c> following each element's colour, an underline taking the fill and one of its own colour,
    /// a line-through taking its decorating box's fill, a text field's value, and the fill animated; currentcolor set
    /// (inherited as the keyword), an underline's own currentcolor (the fill), an input button's, a select's and a button's text,
    /// and the page's selection from a filled paragraph into one whose background is clipped to its text (the unselected
    /// glyphs keep the fill, the underlined ones through Blink's overlay path; no selection in the text mask).
    /// </summary>
    public static readonly StylesheetRuleFixture TextFillColor = new("text-fill-color", 520, 404, """
        body { font: 18px/24px Lato; color: #333; background: #fff; }
        .p { margin: 2px 8px; }
        @keyframes fill { from { -webkit-text-fill-color: #c33; } to { -webkit-text-fill-color: #33c; } }
        """, """
        <p class="p" id="red" style="-webkit-text-fill-color: #c33">Fill red over the ink</p>
        <p class="p" id="clear" style="-webkit-text-fill-color: transparent; text-shadow: 1px 1px 0 currentcolor">Transparent with a shadow</p>
        <p class="p" id="half" style="-webkit-text-fill-color: rgba(0, 0, 200, 0.5)">A half-transparent fill</p>
        <div class="p" id="inherit" style="-webkit-text-fill-color: #36c">Inherited, <span id="inherit-span" style="color: #c63">the fill wins</span></div>
        <div class="p" id="current" style="color: #2a7">Current, <span id="current-span" style="color: #c63">the colour follows</span></div>
        <p class="p" id="under" style="-webkit-text-fill-color: #36c; text-decoration: underline">An underline takes the fill</p>
        <p class="p" id="under2" style="-webkit-text-fill-color: #36c; text-decoration: underline #c33">One of its own colour</p>
        <div class="p" id="through" style="-webkit-text-fill-color: #a3c; text-decoration: line-through">Decorating <span id="through-span" style="-webkit-text-fill-color: #3a3">box's fill</span></div>
        <div class="p"><input id="field" value="Field text" style="-webkit-text-fill-color: #c33; font: 16px Lato"></div>
        <p class="p" id="anim" style="animation: fill 1s linear paused -0.5s">An animated fill</p>
        <div class="p" id="keyword" style="color: #c33; -webkit-text-fill-color: currentcolor">Keyword, <span id="keyword-span" style="color: #33c">the colour follows</span></div>
        <p class="p" id="under3" style="-webkit-text-fill-color: #36c; text-decoration: underline currentcolor">Its own currentcolor</p>
        <div class="p"><input type="button" id="ibutton" value="Input" style="-webkit-text-fill-color: #c33; font: 16px Lato"> <select id="select" style="-webkit-text-fill-color: #33c; font: 16px Lato"><option>Select</option></select> <button id="button" style="-webkit-text-fill-color: #3a3; font: 16px Lato">Button</button></div>
        <p class="p" id="sel" tabindex="-1" data-focus data-select-from="22" style="-webkit-text-fill-color: #c33; outline: none; text-decoration: underline">Some selected words in the fill</p>
        <p class="p" id="sel-clip" data-select-to="7" style="background: linear-gradient(90deg, #14b351, #8a3cf0); background-clip: text; -webkit-text-fill-color: transparent">Clipped text selected too</p>
        """,
        ["-webkit-text-fill-color", "color"]);

    /// <summary>
    /// <c>background-clip: text</c>: the game's Exotic wave on a block (at rest and moved by its animation, a tile's
    /// edge inside the box) and the Sparkling tag on an inline box in a flex row (at a time of its shimmer), a colour
    /// alone through text, a translucent fill over its clip, a rounded box with a border and padding, a box whose text
    /// is in a block child, a relatively placed and a translucent span and an underline, two layers (the top one
    /// clipped to text, the colour under it not), a text field's value (the sheet's stat inputs, with line-height: normal:
    /// the engine places a field's text 1 px low at 1 when its line-height is under the font's, mask or not, 1b's), and the
    /// property's computed values with the <c>background</c> shorthand and the <c>-webkit-</c> alias; then an inline block's text, text
    /// shadows and a decoration's (none in the mask), a list's marker (inside: the engine places an outside one apart
    /// from Chromium, mask or not), an input button's, a select's and a text area's text, the colour
    /// through text under a translucent image of the border box (the bottom layer's clip), and an inline box's mask
    /// holding only its own text where other text overlaps it; a text field focused with a selection (none in the mask;
    /// a number field takes none from a script), a box with a wide opaque border, and an opaque colour through the text
    /// of a composited layer at whole pixels (the save layer ends the display item's opaqueness analysis:
    /// <see cref="StylesheetRuleReferenceTests.ATextClipMakesNothingKnownOpaque"/>; Chromium draws its text grayscale).
    /// </summary>
    public static readonly StylesheetRuleFixture BackgroundClipText = new("background-clip-text", 560, 372, """
        body { font: 700 26px/32px Lato; color: #333; background: #fdf8ee; }
        .wave { background: linear-gradient(90deg, #14b351, #8a3cf0, #14b351) 0 50% / 200% 100%; -webkit-background-clip: text; background-clip: text; -webkit-text-fill-color: transparent; }
        @keyframes exotic-wave { from { background-position: 0% 50%; } to { background-position: 200% 50%; } }
        .moving { animation: exotic-wave 3.2s linear infinite paused -2s; }
        .k { display: inline-block; margin: 2px 6px; vertical-align: top; }
        .enc { margin: 0 6px; padding: 0; list-style: none; width: 240px; }
        .enc li { display: flex; justify-content: space-between; padding-left: 9px; border-left: 3px solid #d9a92e; font: 16px/22px Lato; color: #333; }
        .enc span { color: #8a4a1a; font: 13px Lato; }
        .sparkling-tag { color: #b47c10; font-weight: 700; background: linear-gradient(100deg, #9a6a08 20%, #ffe9a0 45%, #c8901c 60%, #9a6a08 80%) 0 0 / 250% 100%; -webkit-background-clip: text; background-clip: text; -webkit-text-fill-color: transparent; animation: sparkling-shimmer 2.4s linear infinite paused -0.6s; }
        @keyframes sparkling-shimmer { from { background-position: 100% 0; } to { background-position: -150% 0; } }
        """, """
        <div class="k wave" id="wave">Exotic wave</div><div class="k wave moving" id="moving">Exotic wave</div>
        <ul class="enc"><li><div><b class="sparkling-tag" id="spark">Sparkling</b> Ghoul</div><span>×2</span></li></ul>
        <div class="k" id="color-only" style="background-color: #c33; background-clip: text; color: transparent">Colour alone</div>
        <div class="k wave" id="visible" style="-webkit-text-fill-color: rgba(0, 0, 0, 0.3)">Fill over</div>
        <div class="k wave" id="framed" style="border: 3px solid #333; border-radius: 8px; padding: 2px 10px">Framed</div>
        <div class="k wave" id="nested"><p style="margin: 0">Block child</p><span style="position: relative; top: 4px">offset</span> <span style="opacity: 0.3">faint</span> <u>line</u> <span style="display: inline-block">block</span></div>
        <div class="k" id="layers" style="background: linear-gradient(90deg, #c33, #33c), #ffd480; background-clip: text, border-box; -webkit-text-fill-color: transparent">Two layers</div>
        <input class="k wave" id="stat" type="number" value="42" style="font: 700 26px Lato; width: 80px; border: 0; border-bottom: 2px solid #333; padding: 0; appearance: textfield">
        <ol class="k wave" id="list" style="padding: 0; list-style-position: inside"><li>Marker</li></ol>
        <input type="button" class="k wave" id="ibutton" value="Input" style="font: inherit; width: 90px; height: 36px; border: 2px solid #777; padding: 0"><select class="k wave" id="select" style="font: inherit; width: 110px; height: 36px"><option>Select</option></select>
        <textarea class="k wave" id="area" style="font: inherit; width: 90px; height: 34px">Area</textarea>
        <div class="k" id="under" style="background: linear-gradient(90deg, rgba(204, 51, 51, 0.4), rgba(51, 51, 204, 0.4)), #2a7 text; -webkit-text-fill-color: transparent">Colour under</div>
        <div class="k" id="overlap"><b class="sparkling-tag" style="animation: none">Overlap</b><span style="margin-left: -50px; -webkit-text-fill-color: transparent">hidden</span></div>
        <div class="k wave" id="thick" style="border: 14px solid #333">Thick</div>
        <input class="k wave" id="field" type="text" value="Selected" data-focus data-select="2 6" style="font: 700 26px Lato; width: 130px; border: 0; border-bottom: 2px solid #333; padding: 0; outline: none"><div class="k wave" id="shadow" style="position: absolute; left: 6px; top: 326px; margin: 0; text-shadow: 0 -8px 0 rgba(0, 0, 0, 0.25)">Shadow <u style="text-decoration-thickness: 6px">underlined line</u></div><div class="k" id="layer" style="position: absolute; left: 300px; top: 280px; margin: 0; will-change: opacity; background-color: #fff; background-clip: text; -webkit-text-fill-color: transparent">White <span style="-webkit-text-fill-color: #333">grey text</span></div>
        <div id="sh1" style="background-clip: text; background: red"></div><div id="sh2" style="background: red text"></div>
        <div id="sh3" style="background: red content-box"></div><div id="sh4" style="background: red padding-box content-box"></div>
        <div id="sh5" style="background: linear-gradient(red, blue), red; background-clip: text, padding-box"></div><div id="sh6" style="-webkit-background-clip: text"></div>
        """,
        ["background-clip", "-webkit-background-clip", "-webkit-text-fill-color"]);

    /// <summary>
    /// <c>background-clip: text</c> on the root, whose background the view paints: over the whole canvas, not through
    /// the text (BoxBackgroundPaintContext's clip for the view is the border box).
    /// </summary>
    public static readonly StylesheetRuleFixture BackgroundClipTextView = new("background-clip-text-view", 200, 60, """
        html { background: linear-gradient(90deg, #c33, #33c); -webkit-background-clip: text; background-clip: text; }
        body { font: 700 26px/32px Lato; color: #333; }
        """, """
        <div id="view">The view</div>
        """,
        ["background-clip", "background-color"]);

    /// <summary>
    /// A text's y rounded against its parent's offset (TextFragmentPainter's ComputeBoxGeometry; found with
    /// <see cref="BackgroundClipText"/> at 1.65): smaller text in spans that have a box fragment (a background) and in
    /// culled ones (none), the backgrounds colours and images, raised or lowered by fractions of a pixel, on lines of
    /// several heights, so the spans' and the
    /// lines' tops fall on different fractions of a device pixel.
    /// </summary>
    public static readonly StylesheetRuleFixture TextParentRounding = new("text-parent-rounding", 420, 152, """
        body { font: 16px Lato; color: #222; background: #fff; }
        .l { margin: 0 6px; }
        .bg { background: #fde8b0; }
        .img { background: linear-gradient(#fde8b0, #fde8b0); }
        .a { vertical-align: 0.4px; } .b { vertical-align: -0.3px; } .c { vertical-align: 0.7px; } .d { vertical-align: 0.55px; }
        """, """
        <div class="l" style="line-height: 21.3px">Line <span class="bg a" id="bg" style="font-size: 11px">eleven bg</span> <span class="a" id="culled" style="font-size: 11px">eleven</span> <span class="bg c" style="font-size: 13px">thirteen bg</span> <span class="c" style="font-size: 13px">thirteen</span></div>
        <div class="l" style="line-height: 22.7px">Line <span class="bg b" style="font-size: 9px">nine bg</span> <span class="b" style="font-size: 9px">nine</span> <span class="bg d" style="font-size: 15px">fifteen bg</span> <span class="d" style="font-size: 15px">fifteen</span></div>
        <div class="l" style="line-height: 24.1px">Line <span class="bg c" style="font-size: 12px">twelve bg</span> <span class="c" style="font-size: 12px">twelve</span> <span class="bg a" style="font-size: 14px">fourteen bg</span> <span class="a" style="font-size: 14px">fourteen</span></div>
        <div class="l" style="line-height: 20.6px">Line <span class="img d" style="font-size: 10px">ten bg</span> <span class="d" style="font-size: 10px">ten</span> <span class="img b" style="font-size: 11.5px">eleven and a half bg</span> <span class="b" style="font-size: 11.5px">and a half</span></div>
        <div class="l" style="line-height: 23.4px">Line <span class="img a" style="font-size: 13.5px">thirteen and a half bg</span> <span class="a" style="font-size: 13.5px">thirteen and a half</span></div>
        <div class="l" style="line-height: 25.3px">Line <span class="img c" style="font-size: 12.5px">twelve and a half bg</span> <span class="c" style="font-size: 12.5px">twelve and a half</span></div>
        """,
        ["background-color"]);

    public static readonly StylesheetRuleFixture[] All = [PropertyRule, TextFillColor, BackgroundClipText, BackgroundClipTextView, TextParentRounding];
}
