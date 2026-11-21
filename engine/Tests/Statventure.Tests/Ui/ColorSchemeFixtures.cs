namespace Statventure.Tests.Ui;

/// <summary>
/// A page of the colour scheme's rules (GM2's colour-scheme task, 2026-11-21): <c>color-scheme</c> (its grammar,
/// inheritance, <c>only</c>, the used scheme, the root's initial colour and canvas), the system colours and
/// <c>light-dark()</c>, and everything the native look switches by scheme (fields, spin buttons, text areas, selects,
/// buttons, checkboxes, radios, ranges, progress bars, placeholders, focus rings, disabled states, scrollbars).
/// Chromium 152 paints each at 1 and 1.65 (Golden/ui-&lt;name&gt;@1.png and @1.65.png) and reports the
/// <see cref="Computed"/> properties' values of every element with an id (Reference/scaled/ui-&lt;name&gt;@&lt;scale&gt;.styles.json),
/// with Tools/Scripts/make-scaled-paint-references.sh 1 1 color-scheme-fixtures.json and 1.65 1 color-scheme-fixtures.json
/// (the list is written by <see cref="ColorSchemeReferenceTests"/>). Most pages come as a light and a dark twin, the
/// same markup under <c>:root { color-scheme: light }</c> and <c>dark</c>. <see cref="Tolerance"/> is the limit of the
/// kind of page: the paint fixtures' 12 levels, the native controls' 40 (the "controls" fixture's).
/// </summary>
public sealed record ColorSchemeFixture(string Name, int Width, int Height, string Css, string Html, string[] Computed, int Tolerance = 12, bool WindowFocused = true)
{
    public PaintFixture Paint => new(Name, Width, Height, Css, Html, Tolerance: Tolerance, WindowFocused: WindowFocused);
}

public static class ColorSchemeFixtures
{
    static readonly string[] ColourProperties = ["color", "background-color", "border-top-color", "outline-color", "color-scheme"];

    /// <summary>The system colours the engine knows (and Blink's link colours), each a swatch of its own.</summary>
    static readonly string[] Keywords =
    [
        "Canvas", "CanvasText", "LinkText", "VisitedText", "ActiveText", "ButtonFace", "ButtonText", "ButtonBorder", "Field",
        "FieldText", "GrayText", "Mark", "MarkText", "SelectedItem", "SelectedItemText", "Highlight", "HighlightText",
        "-webkit-link", "-webkit-activelink",
    ];

    /// <summary>color-scheme's grammar: each value on an element under a light parent (an invalid one inherits light).</summary>
    static readonly string[] Grammar =
    [
        "normal", "light", "dark", "light dark", "dark light", "only dark", "dark only", "only light", "only", "only only",
        "light only dark", "foo", "foo dark", "normal dark", "DARK", "dark dark", "light foo", "Only Dark", "inherit", "initial",
        "unset", "default", "dark default", "light, dark", "dark normal", "dark 2", "\"dark\"", "only dark only", "dark only light", "dark initial",
    ];

    static string Swatches() => string.Concat(Keywords.Select(k =>
        $"<i id=\"k-{k.TrimStart('-')}\" style=\"background-color: {k}; color: {k}\"></i>"));

    static string GrammarRows() => string.Concat(Grammar.Select((g, i) =>
        $"<b id=\"g{i}\" style='color-scheme: {g}; color: CanvasText; background-color: Canvas'></b>"));

    const string ColoursCss = """
        body { padding: 6px; font: 13px/16px Lato; }
        .row { display: flex; flex-wrap: wrap; gap: 3px; margin-bottom: 6px; align-items: center; }
        i, b { display: inline-block; width: 14px; height: 14px; border: 1px solid #888; }
        @media (prefers-color-scheme: dark) { #pref { background-color: rgb(1, 1, 1); } }
        @media (prefers-color-scheme: light) { #pref { background-color: rgb(2, 2, 2); } }
        dialog, fieldset { box-sizing: border-box; width: 60px; height: 26px; margin: 0; padding: 2px 4px; }
        dialog { position: static; }
        /* (Groove and the fieldset's own width aren't the scheme's: a solid border in a box of its own.) */
        fieldset { display: block; border-style: solid; }
        .slot { width: 64px; height: 26px; }
        input:not([type]), select { box-sizing: border-box; width: 48px; height: 26px; }
        textarea { box-sizing: border-box; width: 60px; height: 26px; }
        input, select, button, textarea { font: inherit; }
        * { caret-color: transparent; }
        """;

    /// <summary>
    /// The colours a scheme gives: every system colour as a swatch (its background and colour), light-dark() (as a
    /// colour, in color-mix(), nested, and four invalid forms that leave the colour inherited), the root's initial colour
    /// (inherited text), color: initial, a link, a focused field's ring colour, the user agent's colours on a dialog, a
    /// fieldset, disabled fields, buttons and a select, a range's colour (that row's values only: it is hidden, the controls
    /// pages paint them), the preferred scheme's media query and
    /// color-scheme: light dark, color-scheme's grammar, and a system colour inherited as its colour into the other
    /// scheme. The root has no background: the canvas is the scheme's.
    /// </summary>
    static ColorSchemeFixture Colours(string scheme) => new($"color-scheme-colors-{scheme}", 460, 240, $":root {{ color-scheme: {scheme} }}\n" + ColoursCss, $"""
        <div class="row">{Swatches()}</div>
        <div class="row">
          <i id="ld" style="color: light-dark(rgb(1, 2, 3), rgb(4, 5, 6)); background-color: light-dark(#c33, #3c3)"></i>
          <i id="ld-mix" style="color: color-mix(in srgb, light-dark(rgb(0, 0, 0), rgb(200, 200, 200)) 50%, rgb(100, 100, 100)); background-color: color-mix(in srgb, light-dark(#00f, #ff0), Canvas)"></i>
          <i id="ld-nested" style="color: light-dark(light-dark(rgb(9, 9, 9), rgb(8, 8, 8)), rgb(7, 7, 7)); background-color: light-dark(Field, ButtonFace)"></i>
          <i id="ld-one" style="color: light-dark(red)"></i>
          <i id="ld-three" style="color: light-dark(red, blue, green)"></i>
          <i id="ld-length" style="color: light-dark(red, 4px)"></i>
          <i id="ld-two" style="color: light-dark(red blue, green)"></i>
          <i id="pref"></i>
          <i id="both" style="color-scheme: light dark; background-color: Canvas; color: CanvasText"></i>
          <span id="text">Text</span> <span id="initial" style="color: initial">initial</span> <a id="link" href="#x">link</a>
          <input id="focus" value="ring" size="4" data-focus>
          <svg width="40" height="14"><rect width="20" height="14" fill="CanvasText"/><rect x="20" width="20" height="14" fill="light-dark(#c33, #3c3)"/></svg>
        </div>
        <div class="row" style="visibility: hidden">
          <div class="slot"><dialog id="dialog" open>dialog</dialog></div>
          <div class="slot"><fieldset id="fieldset">set</fieldset></div>
          <input id="field-off" value="off" size="3" disabled>
          <textarea id="area-off" rows="1" cols="4" disabled>off</textarea>
          <button id="button-off" disabled>off</button>
          <select id="select-off" disabled style="background-color: transparent"><option>off</option></select>
          <input id="range" type="range" style="width: 60px">
        </div>
        <div class="row" style="color-scheme: light">{GrammarRows()}</div>
        <div class="row">
          <div id="outer" style="color-scheme: dark; color: CanvasText; background-color: Canvas; padding: 2px 4px">dark
            <span id="inner" style="color-scheme: light; background-color: Canvas; padding: 0 4px">light <span id="inner-text">kept</span></span>
          </div>
        </div>
        """, ColourProperties);

    const string ControlsCss = """
        body { padding: 6px; }
        input, select, button, textarea { font: inherit; }
        .row { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; margin-bottom: 10px; }
        * { caret-color: transparent; }
        .accent { accent-color: #6f8f68; }
        .pale { accent-color: #dfe8f0; }
        .deep { accent-color: #1e2a20; }
        .faint { accent-color: rgba(0, 120, 60, 0.5); }
        .big { width: 18px; height: 18px; }
        .wide { width: 200px; height: 28px; }
        .styled { padding: 9px; border: 1px solid #b9a98f; border-radius: 3px; background: #f4ecdd; color: #2d241b; }
        .painted { background: #c96; border-style: solid; }
        /* (A resizer's grip isn't the scheme's.) */
        textarea { box-sizing: border-box; width: 130px; height: 40px; resize: none; }
        """;

    /// <summary>
    /// The native controls of a scheme, unfocused: checkboxes and radios (checked, indeterminate, disabled, with an accent,
    /// a pale and a deep accent that flip the scheme, a translucent accent made opaque over the scheme's canvas), ranges,
    /// progress bars, fields (number, search, text, a placeholder, disabled, read-only), selects (themed, author styled,
    /// disabled), buttons (themed, disabled, input buttons, one with an author background and so the user agent's
    /// ButtonBorder border), text areas.
    /// </summary>
    static ColorSchemeFixture Controls(string scheme) => new($"color-scheme-controls-{scheme}", 720, 400, $":root {{ color-scheme: {scheme} }}\n" + ControlsCss, """
        <div class="row">
          <input type="checkbox"><input type="checkbox" checked><input type="checkbox" data-indeterminate><input type="checkbox" disabled><input type="checkbox" disabled checked>
          <input type="checkbox" checked class="accent"><input type="checkbox" checked class="big pale"><input type="checkbox" checked class="big deep"><input type="checkbox" checked class="big faint"><input type="checkbox" class="big">
          <input type="radio"><input type="radio" checked><input type="radio" disabled checked><input type="radio" checked class="pale"><input type="radio" checked class="deep"><input type="radio" class="big">
        </div>
        <div class="row">
          <input type="range"><input type="range" value="0"><input type="range" class="accent wide" value="70"><input type="range" class="pale" value="30"><input type="range" class="deep" value="60"><input type="range" disabled value="30">
        </div>
        <div class="row">
          <progress value="0.3"></progress><progress value="0.8" class="accent"></progress><progress value="0.5" class="pale"></progress><progress value="0.5" class="deep"></progress><progress value="1"></progress>
        </div>
        <div class="row">
          <input type="number" value="12" id="number"><input type="search" value="cave" id="search"><input type="text" value="Hello world" id="field">
          <input type="text" placeholder="Amount"><input type="text" value="off" disabled>
        </div>
        <div class="row">
          <select id="select"><option>First option</option><option>Second</option></select>
          <select class="styled"><option>Steady</option><option selected>Swift battle pace</option></select>
          <select disabled><option>Disabled</option></select>
          <button>Button</button><button disabled>Off</button><input type="submit" value="Submit"><input type="reset" value="Reset" disabled>
          <button class="painted" id="painted">Painted</button>
        </div>
        <div class="row">
          <textarea id="area">Some text in an area</textarea><textarea disabled>Off area</textarea><textarea placeholder="Notes"></textarea>
        </div>
        """, ["color", "border-top-color", "outline-color", "color-scheme"], Tolerance: 40);

    /// <summary>A focused control of a scheme in a focused window: its focus ring, a number field's spin button, a search field's cancel button.</summary>
    static ColorSchemeFixture Focus(string scheme, string what, string html) => new($"color-scheme-focus-{what}-{scheme}", 300, 60,
        $":root {{ color-scheme: {scheme} }}\n" + ControlsCss, $"""<div class="row">{html}</div>""", ["outline-color", "color-scheme"], Tolerance: 40);

    const string ScrollersCss = """
        .s { position: absolute; width: 150px; height: 100px; overflow: auto; border: 1px solid #888; }
        .tall { height: 600px; }
        .wide { width: 600px; height: 40px; }
        .huge { width: 500px; height: 500px; }
        .colored { scrollbar-color: #85673f #d5c39b; }
        .thin { scrollbar-width: thin; }
        .page { height: 900px; }
        """;

    /// <summary>
    /// Scrollbars of a scheme: a vertical bar (at its top, its end), a horizontal one, both with the corner, a thin one,
    /// scrollbar-color (the same in both schemes), a resizer; and the root's bars (the page is taller than the viewport).
    /// </summary>
    static ColorSchemeFixture Scrollbars(string scheme) => new($"color-scheme-scrollbars-{scheme}", 700, 360, $":root {{ color-scheme: {scheme} }}\n" + ScrollersCss, """
        <div class="page">
        <div class="s" style="left: 10px; top: 10px" id="vertical"><div class="tall"></div></div>
        <div class="s" style="left: 180px; top: 10px" data-scroll-top="1000"><div class="tall"></div></div>
        <div class="s" style="left: 350px; top: 10px; overflow-y: hidden" data-scroll-left="100"><div class="wide"></div></div>
        <div class="s" style="left: 10px; top: 130px" data-scroll-left="80" data-scroll-top="120"><div class="huge"></div></div>
        <div class="s thin" style="left: 180px; top: 130px" data-scroll-top="150"><div class="tall"></div></div>
        <div class="s colored" style="left: 350px; top: 130px" data-scroll-top="150"><div class="huge"></div></div>
        <div class="s" style="left: 10px; top: 250px; resize: both"><div class="huge"></div></div>
        <div class="s" style="left: 180px; top: 250px; overflow: scroll"><div style="height: 50px"></div></div>
        </div>
        """, ["color-scheme"]);

    /// <summary>
    /// The scheme changing inside the page: a dark root with a light section and in it a dark one, and the controls,
    /// scrollers, system colours and inherited text of each; color-scheme: light dark (the preferred scheme's) and
    /// only light.
    /// </summary>
    public static readonly ColorSchemeFixture Nested = new("color-scheme-nested", 640, 300, """
        :root { color-scheme: dark }
        body { padding: 6px; font: 13px/16px Lato; }
        input, select, button { font: inherit; }
        * { caret-color: transparent; }
        .box { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; padding: 6px; margin-bottom: 6px; border: 1px solid ButtonBorder; }
        .light { color-scheme: light; }
        .dark { color-scheme: dark; }
        .both { color-scheme: light dark; }
        .only { color-scheme: only light; }
        .canvas { background-color: Canvas; color: CanvasText; }
        .s { width: 90px; height: 50px; overflow: auto; }
        .s > div { height: 200px; width: 200px; }
        """, """
        <div class="box" id="root-box">root text <input type="checkbox" checked><input type="text" value="dark field" size="8"><button>Dark</button><div class="s"><div></div></div>
          <div class="box light canvas" id="light-box">light text <input type="checkbox" checked><input type="radio" checked><input type="text" value="light" size="5"><select><option>Light</option></select><div class="s"><div></div></div>
            <div class="box dark canvas" id="dark-box">dark again <input type="checkbox"><input type="range" style="width: 60px"><button>Dark</button><div class="s"><div></div></div></div>
          </div>
          <div class="box both canvas" id="both-box">light dark <input type="checkbox" checked><input type="text" value="field" size="5"></div>
          <div class="box only canvas" id="only-box">only light <input type="checkbox" checked><button>Light</button></div>
        </div>
        """, ["color", "background-color", "border-top-color", "color-scheme"], Tolerance: 40);

    public static readonly ColorSchemeFixture[] All =
    [
        Colours("light"), Colours("dark"),
        Controls("light"), Controls("dark"),
        Focus("light", "number", """<input type="number" value="12" id="focused" data-focus>"""),
        Focus("dark", "number", """<input type="number" value="12" id="focused" data-focus>"""),
        Focus("light", "button", """<button id="focused" data-focus>Button</button><input type="checkbox" checked class="accent">"""),
        Focus("dark", "button", """<button id="focused" data-focus>Button</button><input type="checkbox" checked class="accent">"""),
        Scrollbars("light"), Scrollbars("dark"),
        Nested,
    ];
}
