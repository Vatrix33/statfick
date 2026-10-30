namespace Statventure.Tests.Ui;

/// <summary>
/// A page painted by the engine and by the game's browser (Tools/Scripts/make-paint-references.sh
/// screenshots it into Golden/ui-<name>.png). Elements with data-scroll-top / data-scroll-left are
/// scrolled there first, then the document to <see cref="ScrollX"/>, <see cref="ScrollY"/>;
/// data-indeterminate makes a checkbox indeterminate, data-focus focuses an element. Fixtures
/// with text compare with the text milestone's tolerance (Chromium's glyph antialiasing differs
/// by a few levels). The browser's test window doesn't have the system's focus, so fixtures that
/// focus something are drawn with the window unfocused.
/// </summary>
public sealed record PaintFixture(string Name, int Width, int Height, string Css, string Html, int ScrollX = 0, int ScrollY = 0, int Tolerance = 12, bool WindowFocused = true);

public static class PaintFixtures
{
    const string Boxes = """
        .s { position: absolute; width: 150px; height: 100px; overflow: auto; border: 1px solid #333; background: #fff; }
        .tall { height: 600px; background: linear-gradient(#cde, #edc); }
        .wide { width: 600px; height: 40px; background: linear-gradient(90deg, #cde, #edc); }
        .huge { width: 500px; height: 500px; background: linear-gradient(135deg, #cde, #edc); }
        .colored { scrollbar-color: #85673f #d5c39b; }
        .thin { scrollbar-width: thin; }
        """;

    public static readonly PaintFixture[] All =
    [
        new("scrollbars", 700, 360, Boxes, """
            <div class="s" style="left: 10px; top: 10px"><div class="tall"></div></div>
            <div class="s" style="left: 180px; top: 10px" data-scroll-top="200"><div class="tall"></div></div>
            <div class="s" style="left: 350px; top: 10px" data-scroll-top="1000"><div class="tall"></div></div>
            <div class="s" style="left: 520px; top: 10px; height: 40px"><div class="tall"></div></div>
            <div class="s thin" style="left: 10px; top: 130px" data-scroll-top="150"><div class="tall"></div></div>
            <div class="s colored" style="left: 180px; top: 130px" data-scroll-top="150"><div class="tall"></div></div>
            <div class="s colored thin" style="left: 350px; top: 130px" data-scroll-top="150"><div class="tall"></div></div>
            <div class="s" style="left: 520px; top: 130px; overflow-y: hidden" data-scroll-left="100"><div class="wide"></div></div>
            <div class="s" style="left: 10px; top: 250px" data-scroll-left="80" data-scroll-top="120"><div class="huge"></div></div>
            <div class="s colored" style="left: 180px; top: 250px" data-scroll-left="80" data-scroll-top="120"><div class="huge"></div></div>
            <div class="s thin" style="left: 350px; top: 250px; overflow-x: scroll; overflow-y: hidden"><div class="wide"></div></div>
            <div class="s" style="left: 520px; top: 250px; overflow: scroll"><div style="height: 50px"></div></div>
            """),
        new("viewport-scrollbar", 400, 300, """
            html { scrollbar-color: #85673f #d5c39b; scrollbar-width: thin; }
            body { background: linear-gradient(#fff, #bbb); }
            .page { height: 900px; margin: 0 20px; border-left: 4px solid #456; }
            """, """<div class="page"></div>""" + "\n"),
        // position: sticky in scroll containers: the companion sheet's nav (top: -25px against the
        // scroller's padding), headers pushed out by the end of their section, a bottom footer, a
        // left column, margins, both insets at once, and nesting.
        new("sticky", 700, 360, """
            .s { position: absolute; width: 150px; height: 150px; overflow: auto; border: 1px solid #333; background: #fff; scrollbar-width: thin; }
            .pad { padding: 25px 28px 32px; }
            .nav { position: sticky; top: -25px; height: 20px; margin: 0 -28px; background: #c76; }
            .block { height: 40px; margin-top: 8px; background: linear-gradient(#cde, #9ab); }
            .section { height: 120px; background: #eee; border-top: 2px solid #999; }
            .head { position: sticky; top: 0; height: 24px; background: #58a; }
            .foot { position: sticky; bottom: 0; height: 18px; background: #a85; }
            .wide { width: 500px; height: 300px; display: flex; background: linear-gradient(90deg, #eee, #ccc); }
            .col { position: sticky; left: 0; width: 30px; flex: none; background: #7a5; }
            .margin { position: sticky; top: 10px; margin-top: 20px; height: 30px; background: #a5a; }
            .both { position: sticky; top: 20px; bottom: 20px; height: 40px; background: #5aa; }
            .outer { position: sticky; top: 0; padding: 4px; background: #caa; }
            .inner { position: sticky; top: 10px; height: 20px; background: #533; }
            """, """
            <div class="s pad" style="left: 10px; top: 10px" data-scroll-top="100"><div class="nav"></div><div class="block"></div><div class="block"></div><div class="block"></div><div class="block"></div><div class="block"></div><div class="block"></div></div>
            <div class="s" style="left: 180px; top: 10px" data-scroll-top="100"><div class="section"><div class="head"></div></div><div class="section"><div class="head"></div></div><div class="section"><div class="head"></div></div></div>
            <div class="s" style="left: 350px; top: 10px" data-scroll-top="40"><div class="block"></div><div class="block"></div><div class="block"></div><div class="block"></div><div class="block"></div><div class="foot"></div><div class="block"></div></div>
            <div class="s" style="left: 520px; top: 10px" data-scroll-left="80" data-scroll-top="30"><div class="wide"><div class="col"></div></div></div>
            <div class="s" style="left: 10px; top: 190px" data-scroll-top="140"><div class="block"></div><div style="height: 120px; background: #ddd"><div class="margin"></div></div><div style="height: 400px"></div></div>
            <div class="s" style="left: 180px; top: 190px" data-scroll-top="60"><div style="height: 60px"></div><div style="height: 100px; background: #ddd"><div class="both"></div></div><div style="height: 300px"></div></div>
            <div class="s" style="left: 350px; top: 190px" data-scroll-top="90"><div style="height: 50px"></div><div style="height: 200px; background: #ddd"><div class="outer"><div style="height: 40px; background: #eaa"></div><div class="inner"></div></div></div><div style="height: 300px"></div></div>
            <div class="s" style="left: 520px; top: 190px; height: 60px" data-scroll-top="20"><div class="both"></div><div style="height: 200px"></div></div>
            """),
        // Sticky against the document's viewport, which is scrolled.
        new("sticky-viewport", 400, 300, """
            body { margin: 0; }
            .bar { position: sticky; top: 8px; height: 30px; margin: 0 20px; background: #47a; }
            .chunk { height: 120px; margin: 10px 20px; background: linear-gradient(#ddd, #999); }
            .end { position: sticky; bottom: 0; height: 24px; background: #a74; }
            """, """<div class="chunk"></div><div class="bar"></div><div class="chunk"></div><div class="chunk"></div><div class="end"></div><div class="chunk"></div><div class="chunk"></div>""", ScrollY: 150),
        // Form controls with their native look, unfocused.
        new("controls", 720, 420, ControlsCss, """
            <div class="row">
              <input type="checkbox"><input type="checkbox" checked><input type="checkbox" data-indeterminate><input type="checkbox" disabled><input type="checkbox" disabled checked>
              <input type="checkbox" checked class="accent"><input type="checkbox" checked class="big accent"><input type="checkbox" class="big">
              <input type="radio"><input type="radio" checked><input type="radio" disabled checked><input type="radio" checked class="accent"><input type="radio" class="big">
            </div>
            <div class="row">
              <input type="range"><input type="range" value="0"><input type="range" value="100"><input type="range" class="accent wide" value="70"><input type="range" disabled value="30">
            </div>
            <div class="row">
              <progress value="0.3"></progress><progress value="0.8" class="accent"></progress><progress value="0"></progress><progress value="1"></progress>
              <progress value="0.45" class="bar"></progress><progress value="0.6" class="bar mana"></progress>
            </div>
            <div class="row">
              <input type="number" value="12"><input type="number" value="3" style="appearance: textfield"><input type="search" value="cave"><input type="text" value="Hello world">
              <input type="text" placeholder="Amount"><input type="text" value="off" disabled>
            </div>
            <div class="row">
              <select><option>First option</option><option>Second</option></select>
              <select class="styled"><option>Steady</option><option selected>Swift battle pace</option></select>
              <select disabled><option>Disabled</option></select>
              <button>Button</button><button disabled>Off</button><input type="submit" value="Submit">
            </div>
            <div class="row">
              <details><summary>Closed details</summary><p>Hidden</p></details>
              <details open><summary>Open details</summary><span>Shown</span></details>
              <details class="nomarker"><summary>No marker</summary></details>
            </div>
            """, Tolerance: 40),
        // Focused controls in a window without the system's focus: no spin buttons, cancel button or focus ring.
        // Text decorations: line styles, thickness, offsets and positions, decorating boxes across
        // font sizes, propagation stopped by an inline-block, ink skipping, shadows.
        new("decorations", 720, 540, """
            body { margin: 0; padding: 8px; font: 16px Lato; }
            p { margin: 0 0 7px; }
            """, """
            <p><span style="text-decoration: underline">Underline gyp</span> <span style="text-decoration: overline">Overline gyp</span> <span style="text-decoration: line-through">Line through gyp</span></p>
            <p style="text-decoration: underline double">Double underline gyp</p>
            <p style="text-decoration: underline dotted">Dotted underline gyp</p>
            <p style="text-decoration: underline dashed">Dashed underline gyp</p>
            <p style="text-decoration: underline wavy #c33">Wavy underline gyp</p>
            <p style="text-decoration: underline 3px; text-underline-offset: 3px">Thick offset gyp</p>
            <p style="text-decoration: underline; text-underline-offset: 3px">Offset of three pixels gyp</p>
            <p style="text-decoration: underline; text-underline-position: under">Under the em box gyp</p>
            <p style="text-decoration: underline; text-underline-position: from-font">From the font gyp</p>
            <p style="text-decoration: underline; text-decoration-skip-ink: none">No ink skipping gyp</p>
            <p style="text-decoration: underline">Base <span style="font-size: 26px">big gyp</span> <span style="font-size: 11px">small gyp</span></p>
            <p>Plain <span style="text-decoration: underline; font-size: 24px">big <span style="font-size: 12px">small gyp</span></span></p>
            <p style="text-decoration: underline red">Red <span style="text-decoration: overline blue">both gyp</span> <span style="display: inline-block">inline-block gyp</span></p>
            <p style="text-decoration: underline; text-shadow: 2px 2px #f80">Shadow gyp</p>
            <p style="text-decoration: underline overline line-through dotted 4px #367">Thick dots gyp</p>
            <p style="text-decoration: underline dashed 4px">Thick dashes gyp</p>
            <p style="text-decoration: underline wavy 3px #c33; text-decoration-thickness: 3px">Thick wave gyp</p>
            """, Tolerance: 40),
        new("controls-focus-number", 300, 60, ControlsCss, """<div class="row"><input type="number" value="12" data-focus></div>""", Tolerance: 40, WindowFocused: false),
        new("controls-focus-search", 300, 60, ControlsCss, """<div class="row"><input type="search" value="cave" data-focus></div>""", Tolerance: 40, WindowFocused: false),
        new("controls-focus-checkbox", 300, 60, ControlsCss, """<div class="row"><input type="checkbox" checked data-focus><input type="range" value="40" style="margin-left: 20px"></div>""", Tolerance: 40, WindowFocused: false),
        // Inline SVG transforms: the transform attribute as CSS transform's presentation attribute
        // (a rotation about a point, overridden by a rule, matrix and skew, nested groups, use with x
        // and y), CSS transforms on SVG elements against their transform-box (fill-box, view-box from
        // 0 0) and transform-origin (0 0 by default), the individual transform properties, and the
        // UA sheet's overflow clip of inline svg next to overflow: visible. Path edges are antialiased
        // differently from Skia (user decision 2026-10-25: the canvas keeps its own path rasterizer),
        // so the tolerance covers edge pixels; a misplaced shape still differs in thousands of pixels.
        new("svg-transforms", 460, 260, """
            body { margin: 0; padding: 10px; }
            .row { display: flex; gap: 10px; margin-bottom: 10px; }
            svg { width: 100px; height: 100px; background: #e8e4dc; }
            .flat { transform: none; }
            .spin { transform-box: fill-box; transform-origin: 50% 50%; transform: rotate(-25deg); }
            .half { transform-origin: 50% 50%; transform: scale(0.5); }
            .props { transform-box: fill-box; transform-origin: center; scale: 1.5 0.75; translate: 6px 2px; rotate: 20deg; }
            .shift { transform-box: fill-box; transform: translate(50%, -25%); }
            .visible { overflow: visible; }
            """, """
            <div class="row">
              <svg viewBox="0 0 40 40"><rect x="10" y="10" width="20" height="20" fill="#2b6" transform="rotate(30 20 20)"/></svg>
              <svg viewBox="0 0 40 40"><rect class="flat" x="10" y="10" width="20" height="20" fill="#b62" transform="rotate(45 20 20)"/><g transform="translate(4 4) scale(0.5)"><circle cx="10" cy="10" r="6" fill="#26b"/></g></svg>
              <svg viewBox="0 0 40 40"><rect class="spin" x="6" y="12" width="28" height="10" fill="#a3c" stroke="#222" stroke-width="1.5"/></svg>
              <svg viewBox="0 0 40 40"><rect class="half" x="0" y="0" width="40" height="40" fill="#c90"/><circle class="props" cx="12" cy="28" r="5" fill="#09c"/></svg>
            </div>
            <div class="row">
              <svg viewBox="0 0 40 40"><g transform="matrix(1 0.2 0 1 2 0)"><rect x="4" y="4" width="16" height="16" fill="#6a3" transform="skewX(20)"/></g><circle cx="38" cy="20" r="8" fill="#c33"/></svg>
              <svg viewBox="0 0 40 40" class="visible"><circle cx="38" cy="20" r="8" fill="#c33"/><path class="shift" d="M4 30 L16 30 L10 38 Z" fill="#36a"/></svg>
              <svg viewBox="0 0 40 40" style="margin-left: 10px"><defs><rect id="tile" width="10" height="6" fill="#875"/></defs><use href="#tile" x="6" y="4" transform="rotate(20)"/><use href="#tile" x="20" y="24"/></svg>
            </div>
            """, Tolerance: 60),
    ];

    const string ControlsCss = """
        body { margin: 0; padding: 6px; }
        input, select, button, textarea { font: inherit; }
        .row { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; margin-bottom: 10px; }
        * { caret-color: transparent; }
        .accent { accent-color: #6f8f68; }
        .big { width: 18px; height: 18px; }
        .wide { width: 200px; height: 28px; }
        .bar { width: 120px; height: 7px; border-radius: 4px; appearance: none; background: rgba(40, 30, 20, 0.12); }
        .bar::-webkit-progress-bar { border-radius: 4px; background: rgba(40, 30, 20, 0.12); }
        .bar::-webkit-progress-value { border-radius: 4px; background: linear-gradient(#8fd88a, #3f9a4a); }
        .mana::-webkit-progress-value { background: linear-gradient(#8fc4ff, #3f7fd8); }
        .styled { padding: 9px; border: 1px solid #b9a98f; border-radius: 3px; background: #f4ecdd; color: #2d241b; }
        .nomarker > summary::-webkit-details-marker { display: none; }
        """;
}
