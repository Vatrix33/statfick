namespace Statventure.Tests.Ui;

/// <summary>
/// One input action: the pointer (move, down, up, wheel), a key (keydown, keyup; keys that type
/// also send their character), a pause (wait), or what a script does to an element (focus, blur,
/// click, reset, requestSubmit, select, stepUp, stepDown, set a property).
/// </summary>
public sealed record InputStep(string Kind)
{
    public float X { get; init; }
    public float Y { get; init; }
    public string? Button { get; init; }
    public string? Key { get; init; }
    public string[]? Modifiers { get; init; }
    public float Dx { get; init; }
    public float Dy { get; init; }
    public int Ms { get; init; }
    public string? Target { get; init; }
    public string? Property { get; init; }
    public string? Value { get; init; }
    public string? Name { get; init; }
}

/// <summary>
/// A page and the input played into it, in the engine and in Chromium 152 as real input
/// (Tools/Scripts/make-input-references.sh writes what Chromium saw to Reference/input-results.json).
/// The events listed in the body's data-log (all of them when absent) are logged at the document;
/// elements with data-prevent cancel the events it names.
/// </summary>
public sealed record InputFixture(string Name, int Width, int Height, string Css, string Html, InputStep[] Steps);

public static class InputFixtures
{
    public const string BaseCss = """
        body { margin: 0; font: 16px Lato; }
        input, select, button, textarea { font: inherit; }
        .a { position: absolute; margin: 0; box-sizing: border-box; }
        """;

    static InputStep[] Move(float x, float y) => [new("move") { X = x, Y = y }];

    static InputStep[] Click(float x, float y) =>
        [new("move") { X = x, Y = y }, new("down") { X = x, Y = y }, new("up") { X = x, Y = y }];

    static InputStep[] Press(string key, params string[] modifiers) =>
        [new("keydown") { Key = key, Modifiers = modifiers.Length > 0 ? modifiers : null }, new("keyup") { Key = key, Modifiers = modifiers.Length > 0 ? modifiers : null }];

    static InputStep[] KeyDown(string key) => [new("keydown") { Key = key }];

    static InputStep[] KeyUp(string key) => [new("keyup") { Key = key }];

    static InputStep[] Wait(int ms) => [new("wait") { Ms = ms }];

    static InputStep[] Down(float x, float y) => [new("down") { X = x, Y = y }];

    static InputStep[] ShiftClick(float x, float y) =>
        [new("move") { X = x, Y = y }, new("down") { X = x, Y = y, Modifiers = ["Shift"] }, new("up") { X = x, Y = y, Modifiers = ["Shift"] }];

    static InputStep[] Up(float x, float y) => [new("up") { X = x, Y = y }];

    static InputStep[] Wheel(float x, float y, float dy) => [new("wheel") { X = x, Y = y, Dy = dy }];

    /// <summary>The states (pseudo-classes, values, focus) at this point.</summary>
    static InputStep[] Snapshot() => [new("snapshot")];

    /// <summary>A picture of the page now (Golden/input-&lt;fixture&gt;-&lt;name&gt;.png from Chromium).</summary>
    static InputStep[] Screenshot(string name) => [new("screenshot") { Name = name }];

    static InputStep[] WindowBlur() => [new("windowBlur")];

    static InputStep[] WindowFocus() => [new("windowFocus")];

    static InputStep[] Call(string kind, string target) => [new(kind) { Target = target }];

    static InputStep[] Set(string target, string property, string value) => [new("set") { Target = target, Property = property, Value = value }];

    static InputStep[] Steps(params InputStep[][] parts) => parts.SelectMany(p => p).ToArray();

    /// <summary>Types the text as keys (capitals with Shift, as a keyboard sends them).</summary>
    static InputStep[] Type(string text) => text.SelectMany(c => char.IsUpper(c) ? Press(c.ToString(), "Shift") : Press(c.ToString())).ToArray();

    public static readonly InputFixture[] All =
    [
        // How selections and the caret look: a field's caret and selected word, the page's
        // selection over two paragraphs (with a background and a tall line), the inactive window's
        // colours, the author's ::selection colours, the caret in an empty field with a placeholder.
        new("selection-paint", 400, 240, """
            p { position: absolute; left: 10px; margin: 0; }
            #p3::selection, #u::selection { color: #f6ecd2; background: #7a2a1d; }
            """, """
            <input id="t" class="a" type="text" value="alpha beta gamma" style="left: 10px; top: 10px; width: 300px; height: 30px">
            <input id="e" class="a" type="text" placeholder="Name" style="left: 10px; top: 50px; width: 140px; height: 30px">
            <input id="u" class="a" type="text" value="custom colours" style="left: 160px; top: 50px; width: 150px; height: 30px">
            <p id="p1" style="top: 100px; background: #fe8">Some words to select here.</p>
            <p id="p2" style="top: 130px; color: #a33; line-height: 30px">Second paragraph line.</p>
            <p id="p3" style="top: 180px">Author colours here.</p>
            """, Steps(
                Click(59, 25), Screenshot("caret"), Wait(500), Click(59, 25), Click(59, 25), Screenshot("word"), WindowBlur(), Wait(100),
                Screenshot("word-inactive"), WindowFocus(), Wait(500), Down(66, 110), Move(100, 125), Move(80, 145), Up(80, 145),
                Screenshot("page"), WindowBlur(), Wait(100), Screenshot("page-inactive"), WindowFocus(), Wait(500),
                Click(50, 190), Click(50, 190), Screenshot("author-page"), Wait(500), Click(200, 65), Click(200, 65), Screenshot("author-field"),
                Wait(500), Click(60, 65), Screenshot("empty-caret"))),

        // Resizers: a press on one (not cancelled) resizes instead of focusing; moves set the width
        // and height in the style attribute; resize: vertical changes only the height.
        new("resize", 400, 300, "", """
            <textarea id="a" class="a" data-size style="left: 10px; top: 10px; width: 150px; height: 40px">some text</textarea>
            <div id="d" class="a" data-size style="left: 10px; top: 120px; width: 100px; height: 60px; overflow: auto; resize: vertical; border: 2px solid #888; padding: 3px"><div style="height: 200px"></div></div>
            """, Steps(
                Move(155, 45), Down(155, 45), Move(200, 80), Move(231, 101), Up(231, 101), Snapshot(), Screenshot("textarea"),
                Move(100, 170), Down(100, 170), Move(150, 230), Up(150, 230), Snapshot(), Screenshot("scroller"))),

        // Scrolling text fields: the caret is kept in view when the user moves it (a one-line field's
        // editor sideways, a textarea's box, then the page), not when a script sets the value; page
        // keys move a textarea's caret a page and put it at the top of every scroller.
        new("field-scroll", 400, 300, "", """
            <input id="t" class="a" type="text" value="the quick brown fox jumps over the lazy dog" style="left: 10px; top: 10px; width: 120px; height: 26px">
            <textarea id="a" class="a" rows="3" style="left: 10px; top: 50px; width: 200px">line one
            line two
            line three
            line four
            line five
            line six
            line seven
            line eight
            line nine
            line ten
            line eleven
            line twelve</textarea>
            <div style="height: 1500px"></div>
            """, Steps(
                Click(70, 23), Snapshot(), Press("End"), Snapshot(), Press("Home"), Snapshot(),
                Set("#t", "value", "another quite long value for the field"), Snapshot(), Press("ArrowRight"), Snapshot(),
                Click(100, 60), Snapshot(), Screenshot("textarea"), Press("PageDown"), Snapshot(), Press("PageDown"), Snapshot(), Screenshot("textarea-paged"),
                Press("PageUp"), Snapshot(),
                Press("ArrowDown"), Press("ArrowDown"), Press("ArrowDown"), Snapshot(), Press("End", "Control"), Snapshot(),
                Press("PageUp", "Shift"), Snapshot(), Click(70, 23), Press("PageDown"), Wait(400), Snapshot())),

        // Decorations under a selection (Blink's highlight overlay: the selected part's lines in the
        // selection colour) and in fields (with a shadow, in a placeholder).
        new("decoration-selection", 420, 200, """
            p { position: absolute; left: 10px; margin: 0; }
            """, """
            <p id="p1" style="top: 10px; text-decoration: underline wavy #c33">Underlined text to select gyp</p>
            <p id="p2" style="top: 45px; text-decoration: underline overline line-through; text-shadow: 1px 1px #f80">Struck text with shadow gyp</p>
            <input id="t" class="a" type="text" value="underlined field gyp" style="left: 10px; top: 90px; width: 260px; height: 30px; text-decoration: underline; text-shadow: 1px 1px #8af">
            <input id="e" class="a" type="text" placeholder="Placeholder gyp" style="left: 10px; top: 140px; width: 200px; height: 30px; text-decoration: underline">
            """, Steps(
                Screenshot("plain"), Down(60, 20), Move(120, 40), Move(150, 55), Up(150, 55), Screenshot("page"), WindowBlur(), Wait(100),
                Screenshot("page-inactive"), WindowFocus(), Wait(500), Click(110, 105), Click(110, 105), Screenshot("field"))),

        // Focus rings (outline: auto) as Tab moves through: a themed button, checkbox and radio
        // (their native radii and the 2 px offset), a link wrapped over two lines and a box whose
        // child and text overflow it (the ring goes around the union), author radii and offsets,
        // a styled button and a select.
        new("focus-rings", 420, 260, """
            p { position: absolute; margin: 0; }
            .box { position: absolute; width: 60px; height: 30px; background: #eef; }
            """, """
            <button id="b" class="a" style="left: 10px; top: 10px; width: 80px; height: 30px">Button</button>
            <input id="c" class="a" type="checkbox" style="left: 110px; top: 18px">
            <input id="r" class="a" type="radio" style="left: 150px; top: 18px">
            <select id="s" class="a" style="left: 190px; top: 10px; width: 90px; height: 30px"><option>One</option><option>Two</option></select>
            <button id="sb" class="a" style="left: 300px; top: 10px; width: 90px; height: 30px; border: 1px solid #888; border-radius: 8px; background: #ddd">Styled</button>
            <p style="left: 10px; top: 70px; width: 130px">Some text <a id="l" href="#x">a link that wraps</a> end.</p>
            <div id="o" class="box" tabindex="0" style="left: 170px; top: 70px"><div style="width: 100px; height: 12px; background: #fcc"></div></div>
            <div id="t" class="box" tabindex="0" style="left: 300px; top: 70px; white-space: nowrap">Overflowing text</div>
            <div id="rd" class="box" tabindex="0" style="left: 10px; top: 150px; border: 2px solid #888; border-radius: 10px"></div>
            <p style="left: 120px; top: 155px">An <span id="sp" tabindex="0" style="outline-offset: 4px">offset span</span> here.</p>
            <div id="z" class="box" tabindex="0" style="left: 300px; top: 150px; outline-offset: -6px"></div>
            """, Steps(
                Press("Tab"), Screenshot("button"), Press("Tab"), Screenshot("checkbox"), Press("Tab"), Screenshot("radio"),
                Press("Tab"), Screenshot("select"), Press("Tab"), Screenshot("styled-button"), Press("Tab"), Screenshot("link"),
                Press("Tab"), Screenshot("overflow-child"), Press("Tab"), Screenshot("overflow-text"), Press("Tab"), Screenshot("radius"),
                Press("Tab"), Screenshot("offset-span"), Press("Tab"), Screenshot("negative-offset"))),

        // Selecting in a field with the pointer: a press puts the caret, a drag selects, a press in
        // the selection collapses on release, double and triple presses select a word and all,
        // Shift extends, a backward drag selects backward.
        new("field-mouse", 400, 200, "", """
            <input id="t" class="a" type="text" value="alpha beta gamma" style="left: 10px; top: 10px; width: 300px; height: 30px">
            """, Steps(
                Click(27, 25), Snapshot(), Wait(500), Down(27, 25), Move(70, 25), Move(108, 25), Snapshot(), Up(108, 25), Snapshot(),
                Wait(500), Click(45, 25), Snapshot(), Wait(500), Click(59, 25), Click(59, 25), Snapshot(), Wait(500),
                Click(59, 25), Click(59, 25), Click(59, 25), Snapshot(), Wait(500), Click(27, 25), ShiftClick(108, 25), Snapshot(),
                Wait(500), Click(150, 25), Wait(500), Down(108, 25), Move(60, 25), Move(27, 25), Up(27, 25), Snapshot())),

        // Selecting page text: a press puts a caret, a drag selects across paragraphs, double and
        // triple presses select a word and the paragraph, Shift extends, Ctrl+A selects all.
        new("page-select", 400, 220, """
            p { position: absolute; left: 10px; margin: 0; }
            """, """
            <p id="p1" style="top: 100px">Some words to select here.</p>
            <p id="p2" style="top: 130px">Second paragraph line.</p>
            """, Steps(
                Click(66, 110), Snapshot(), Wait(500), Down(66, 110), Move(100, 125), Move(66, 140), Snapshot(), Up(66, 140), Snapshot(),
                Wait(500), Click(140, 110), Click(140, 110), Snapshot(), Wait(500), Click(140, 110), Click(140, 110), Click(140, 110), Snapshot(),
                Wait(500), Click(66, 110), ShiftClick(66, 140), Snapshot(), Wait(500), Click(300, 190), Snapshot(),
                Press("a", "Control"), Snapshot())),

        // Typing: beforeinput, the change, input; Backspace and Delete; caret moves; Shift extends;
        // Home and End; Ctrl+A; typing over a selection.
        new("edit-basic", 400, 200, "", """
            <input id="t" class="a" type="text" style="left: 10px; top: 10px; width: 250px; height: 26px">
            """, Steps(
                Click(100, 23), Type("Hello"), Press("Backspace"), Press("ArrowLeft"), Press("ArrowLeft"), Type("x"), Snapshot(),
                Press("Delete"), Press("Home"), Press("ArrowRight", "Shift"), Press("ArrowRight", "Shift"), Snapshot(), Type("J"),
                Press("End"), Press("ArrowLeft", "Shift"), Press("Backspace"), Press("a", "Control"), Snapshot(), Type("w"),
                Press("ArrowLeft"), Press("ArrowRight", "Shift"), Press("ArrowLeft"), Snapshot(), Call("blur", "#t"))),

        // Words: Ctrl+arrows move by words, Ctrl+Backspace and Ctrl+Delete delete them (punctuation, numbers, apostrophes).
        new("edit-words", 500, 200, "", """
            <input id="t" class="a" type="text" value="Don't stop: 3.14 apples, (big) foo-bar  end" style="left: 10px; top: 10px; width: 450px; height: 26px">
            """, Steps(
                Call("focus", "#t"), Press("Home"), Press("ArrowRight", "Control"), Snapshot(), Press("ArrowRight", "Control"), Snapshot(),
                Press("ArrowRight", "Control"), Snapshot(), Press("ArrowRight", "Control"), Snapshot(), Press("ArrowRight", "Control"), Snapshot(),
                Press("ArrowRight", "Control"), Snapshot(), Press("ArrowRight", "Control"), Snapshot(), Press("ArrowRight", "Control"), Snapshot(),
                Press("ArrowRight", "Control"), Snapshot(), Press("ArrowLeft", "Control"), Snapshot(), Press("ArrowLeft", "Control"), Snapshot(),
                Press("ArrowLeft", "Control"), Snapshot(), Press("ArrowLeft", "Control"), Snapshot(), Press("ArrowLeft", "Control", "Shift"), Snapshot(),
                Press("End"), Press("Backspace", "Control"), Snapshot(), Press("Backspace", "Control"), Snapshot(), Press("Home"),
                Press("Delete", "Control"), Snapshot(), Press("Delete", "Control"), Snapshot(), Press("Backspace", "Control", "Shift"))),

        // maxlength stops typing at the limit; a number field drops what isn't part of a number.
        new("edit-limits", 400, 200, "", """
            <input id="t" class="a" type="text" maxlength="3" style="left: 10px; top: 10px; width: 250px; height: 26px">
            <input id="n" class="a" type="number" style="left: 10px; top: 60px; width: 250px; height: 26px">
            """, Steps(
                Click(100, 23), Type("abcd"), Click(100, 73), Type("1a2.-e"), Press("Backspace"), Type("5"), Call("blur", "#n"))),

        // Undo and redo (Ctrl+Z, Ctrl+Shift+Z, Ctrl+Y); typing runs undo as one step.
        new("edit-undo", 400, 200, "", """
            <input id="t" class="a" type="text" style="left: 10px; top: 10px; width: 250px; height: 26px">
            """, Steps(
                Click(100, 23), Type("one"), Press(" "), Type("two"), Press("Backspace"), Press("Backspace"), Press("z", "Control"), Snapshot(),
                Press("z", "Control"), Snapshot(), Press("z", "Control", "Shift"), Snapshot(), Press("y", "Control"), Snapshot(), Press("z", "Control"),
                Press("z", "Control"), Press("z", "Control"), Snapshot())),

        // Focus and the selection: Tab selects all; script focus keeps the old selection; a click puts the caret there.
        // inert: clicks go through an inert subtree to what's under it, Tab and focus() skip it, a
        // press in its text starts no selection; a focused element that becomes inert loses focus.
        new("inert", 400, 200, "", """
            <div id="base" class="a" style="left: 0; top: 0; width: 400px; height: 200px"></div>
            <div id="menu" inert style="position: absolute; left: 10px; top: 10px; width: 200px; height: 130px">
              <button id="inner" class="a" style="left: 0; top: 0; width: 80px; height: 30px">In</button>
              <input id="inner-field" class="a" type="text" value="inside" style="left: 0; top: 40px; width: 150px; height: 26px">
              <p id="words" class="a" style="left: 0; top: 80px">Inert words here</p>
            </div>
            <button id="out" class="a" style="left: 250px; top: 10px; width: 80px; height: 30px">Out</button>
            <input id="field" class="a" type="text" style="left: 250px; top: 60px; width: 120px; height: 26px">
            """, Steps(
                Click(30, 20), Snapshot(), Press("Tab"), Snapshot(), Call("focus", "#inner-field"), Snapshot(),
                Down(20, 98), Move(150, 98), Up(150, 98), Snapshot(),
                Set("#menu", "inert", "false"), Click(30, 20), Snapshot(), Call("focus", "#inner-field"), Snapshot(),
                Set("#menu", "inert", "true"), Snapshot(), Press("Tab"), Snapshot(), Move(30, 20), Snapshot())),

        new("edit-focus", 400, 200, "", """
            <input id="a" class="a" type="text" value="alpha" style="left: 10px; top: 10px; width: 250px; height: 26px">
            <input id="b" class="a" type="text" value="bravo" style="left: 10px; top: 60px; width: 250px; height: 26px">
            <textarea id="c" class="a" style="left: 10px; top: 110px; width: 250px; height: 60px">charlie</textarea>
            """, Steps(
                Snapshot(), Call("focus", "#a"), Snapshot(), Press("Tab"), Snapshot(), Press("Tab"), Snapshot(), Press("Tab", "Shift"), Snapshot(),
                Click(15, 73), Snapshot(), Call("focus", "#a"), Snapshot())),

        // While the main button is held, :active stays on what was pressed and :hover only on what
        // is in that chain; the click goes to the common ancestor; a cancelled mousedown still activates.
        new("press-hover", 400, 200, """
            .box { position: absolute; width: 100px; height: 40px; background: #ddd; }
            """, """
            <div id="outer" style="position: absolute; left: 0; top: 0; width: 400px; height: 200px">
              <div id="a" class="box" style="left: 10px; top: 10px"></div>
              <div id="b" class="box" style="left: 150px; top: 10px"></div>
              <button id="c" class="a" data-prevent="mousedown" style="left: 10px; top: 100px; width: 80px; height: 30px">C</button>
            </div>
            """, Steps(
                Move(20, 20), Down(20, 20), Snapshot(), Move(160, 20), Snapshot(), Up(160, 20), Snapshot(),
                Move(20, 110), Down(20, 110), Snapshot(), Up(20, 110))),

        // The number field's spin button: a press steps, a hold repeats (250 ms, then every 50 ms),
        // the release commits; the half under the pointer decides; the wheel steps a focused
        // field; ArrowUp/ArrowDown step and commit; limits stop it.
        new("number-spin", 400, 200, "", """
            <input id="n" class="a" type="number" value="4" min="0" max="20" style="left: 10px; top: 10px; width: 150px; height: 26px">
            <input id="e" class="a" type="number" style="left: 10px; top: 60px; width: 150px; height: 26px">
            <input id="m" class="a" type="number" step="0.5" min="1" max="3" value="2.75" style="left: 10px; top: 110px; width: 150px; height: 26px">
            """, Steps(
                Move(150, 16), Down(150, 16), Wait(400), Up(150, 16), Move(150, 31), Down(150, 31), Up(150, 31),
                Wheel(80, 23, 100), Wheel(80, 23, -100), Press("ArrowUp"), Press("ArrowDown"),
                Move(150, 66), Down(150, 66), Up(150, 66), Move(150, 116), Down(150, 116), Up(150, 116),
                Press("ArrowUp"), Press("ArrowUp"), Press("ArrowDown"), Move(300, 180))),

        // The range slider: a press on the track jumps there (input), dragging follows the pointer,
        // the release commits (change); keys step, page and jump to the ends.
        new("range", 400, 200, "", """
            <input id="r" class="a" type="range" min="0" max="100" value="50" style="left: 10px; top: 10px; width: 200px; height: 20px">
            <input id="s" class="a" type="range" min="0" max="10" step="any" value="5" style="left: 10px; top: 60px; width: 200px; height: 20px">
            """, Steps(
                Move(50, 20), Down(50, 20), Up(50, 20), Move(42, 20), Down(42, 20), Move(80, 20), Move(150, 25), Snapshot(),
                Move(400, 100), Up(400, 100), Press("ArrowLeft"), Press("PageDown"), Press("Home"), Press("ArrowDown"), Press("End"),
                Move(110, 70), Down(110, 70), Up(110, 70), Press("ArrowRight"), Press("PageUp"))),

        // A closed select: arrows, page keys, Home and End move the selection past disabled options
        // (input, change); letters type ahead (a repeated letter cycles).
        new("select-keys", 400, 200, "", """
            <select id="s" class="a" style="left: 10px; top: 10px; width: 150px; height: 26px">
              <option>Apple</option><option disabled>Banana</option><option>Cherry</option><option>Coconut</option>
              <option>Date</option><option>Elder</option><option>Fig</option>
            </select>
            """, Steps(
                Call("focus", "#s"), Press("ArrowDown"), Press("ArrowRight"), Press("ArrowUp"), Press("ArrowLeft"), Press("End"),
                Press("ArrowDown"), Press("Home"), Press("PageDown"), Press("PageUp"), Press("c"), Wait(1100), Press("c"), Press("c"),
                Wait(1100), Press("d"), Press("a"), Call("blur", "#s"))),

        // The list opened by keys: the page sees no keys while it is open; Enter picks; Escape
        // after arrows keeps the arrowed option; Escape alone changes nothing.
        new("select-popup-keys", 400, 300, "", """
            <select id="s" class="a" style="left: 10px; top: 10px; width: 150px; height: 26px">
              <option>One</option><option>Two</option><option>Three</option><option>Four</option>
            </select>
            """, Steps(
                Call("focus", "#s"), Press("Enter"), Snapshot(), Press("ArrowDown"), Press("ArrowDown"), Press("Enter"), Snapshot(),
                Press(" "), Press("ArrowDown"), Press("Escape"), Press("ArrowDown", "Alt"), Snapshot(), Press("Escape"), Press("F4"),
                Press("ArrowUp"), Press("Tab"), Wait(50))),

        // The list opened by a press: :open while open; a press elsewhere closes it and reaches the
        // page; a press on the select toggles it.
        new("select-popup-press", 400, 300, "", """
            <select id="s" class="a" style="left: 10px; top: 10px; width: 150px; height: 26px">
              <option>One</option><option>Two</option><option>Three</option>
            </select>
            <button id="b" class="a" style="left: 250px; top: 250px; width: 80px; height: 30px">B</button>
            """, Steps(
                Click(50, 23), Snapshot(), Click(280, 260), Snapshot(), Click(50, 23), Click(50, 23),
                Click(50, 23), Press("ArrowDown"), Click(280, 260))),

        // The search field's cancel button empties it (input, then search); so does Escape.
        new("search-cancel", 400, 200, "", """
            <input id="q" class="a" type="search" value="abc" style="left: 10px; top: 10px; width: 200px; height: 26px">
            """, Steps(
                Click(60, 23), Click(198, 23), Set("#q", "value", "xyz"), Press("Escape"), Press("Escape"), Move(300, 180))),

        // Checkboxes: a click toggles before the listeners and fires input and change; a cancelled
        // click is undone; labels pass clicks on (and focus the control).
        new("checkbox", 400, 200, """
            label { display: block; }
            """, """
            <input id="c1" class="a" type="checkbox" style="left: 10px; top: 10px; width: 20px; height: 20px">
            <input id="c2" class="a" type="checkbox" checked data-prevent="click" style="left: 40px; top: 10px; width: 20px; height: 20px">
            <input id="c3" class="a" type="checkbox" required style="left: 70px; top: 10px; width: 20px; height: 20px">
            <label id="l1" class="a" style="left: 10px; top: 50px; width: 150px; height: 24px">Wrapped <input id="c4" type="checkbox"></label>
            <label id="l2" for="c5" class="a" style="left: 10px; top: 90px; width: 150px; height: 24px">For c5</label>
            <input id="c5" class="a" type="checkbox" style="left: 200px; top: 90px; width: 20px; height: 20px">
            <input id="c6" class="a" type="checkbox" style="left: 100px; top: 10px; width: 20px; height: 20px">
            """, Steps(
                Click(20, 20), Click(20, 20), Click(50, 20), Click(80, 20), Click(80, 20),
                Click(30, 62), Click(30, 102), Set("#c6", "indeterminate", "true"), Click(110, 20), Move(300, 180))),

        // Radios: a click checks one of the group (the old one gets no events); arrows move through
        // the group and check; a cancelled click restores the old radio.
        new("radio", 400, 200, "", """
            <input id="r1" class="a" type="radio" name="g" style="left: 10px; top: 10px; width: 20px; height: 20px">
            <input id="r2" class="a" type="radio" name="g" style="left: 40px; top: 10px; width: 20px; height: 20px">
            <input id="r3" class="a" type="radio" name="g" style="left: 70px; top: 10px; width: 20px; height: 20px">
            <input id="r4" class="a" type="radio" name="h" data-prevent="click" style="left: 100px; top: 10px; width: 20px; height: 20px">
            <input id="r5" class="a" type="radio" name="h" checked style="left: 130px; top: 10px; width: 20px; height: 20px">
            """, Steps(
                Click(20, 20), Click(50, 20), Press("ArrowRight"), Press("ArrowDown"), Press("ArrowLeft"),
                Press("ArrowUp"), Press(" "), Click(110, 20), Move(300, 180))),

        // Space and Enter on a focused checkbox: Space toggles on release, Enter submits the form implicitly.
        new("checkbox-keys", 400, 200, "", """
            <form id="f" data-prevent="submit">
              <input id="c" class="a" type="checkbox" style="left: 10px; top: 10px; width: 20px; height: 20px">
              <button id="b" class="a" style="left: 10px; top: 50px; width: 80px; height: 30px">Send</button>
            </form>
            """, Steps(
                Call("focus", "#c"), Press(" "), KeyDown(" "), Call("blur", "#c"), KeyUp(" "),
                Call("focus", "#c"), Press("Enter"))),

        // Details: the summary toggles them (asynchronous toggle events, coalesced), not when the
        // click is on a control inside the summary; Enter and Space on the summary.
        new("details", 400, 300, """
            summary { height: 24px; }
            details { position: absolute; left: 10px; width: 300px; }
            """, """
            <details id="d1" style="top: 10px"><summary id="s1">First <span id="sp">inner</span> <button id="sb" style="width: 50px; height: 20px">B</button></summary><p>Body</p></details>
            <details id="d2" style="top: 150px" open><summary id="s2">Second</summary><p>Body</p></details>
            """, Steps(
                Click(20, 20), Click(20, 20), Click(80, 22), Click(140, 22),
                Call("focus", "#s2"), Press("Enter"), Press(" "), Wait(50),
                Set("#d2", "open", "true"), Set("#d2", "open", "false"), Set("#d2", "open", "true"), Wait(50))),

        // Buttons: Space presses (keyup clicks), Enter clicks on keypress; disabled buttons get no
        // click (nor their ancestors); a span inside a disabled button gets it, its ancestors don't.
        new("buttons", 400, 200, "", """
            <div id="wrap">
              <button id="b1" class="a" style="left: 10px; top: 10px; width: 80px; height: 30px">One</button>
              <button id="b2" class="a" disabled style="left: 100px; top: 10px; width: 80px; height: 30px">Two</button>
              <button id="b3" class="a" disabled style="left: 190px; top: 10px; width: 120px; height: 30px"><span id="sp" style="display: inline-block; width: 60px; height: 20px">Three</span></button>
              <input id="i1" class="a" type="button" value="Four" style="left: 10px; top: 60px; width: 80px; height: 30px">
            </div>
            """, Steps(
                Click(50, 25), Press(" "), Press("Enter"), Click(140, 25), Click(230, 25),
                Call("focus", "#i1"), Press(" "), Press("Enter"), Call("click", "#b1"))),

        // Forms: a submit with an invalid field shows the validation (invalid events, focus, no
        // submit); a valid one fires submit with its submitter; reset restores the defaults.
        new("form-submit", 400, 300, "", """
            <form id="f" data-prevent="submit">
              <input id="t" class="a" type="text" required style="left: 10px; top: 10px; width: 150px; height: 26px">
              <input id="n" class="a" type="number" min="1" max="5" value="3" style="left: 10px; top: 50px; width: 150px; height: 26px">
              <input id="c" class="a" type="checkbox" checked style="left: 10px; top: 90px; width: 20px; height: 20px">
              <button id="s" class="a" style="left: 10px; top: 130px; width: 80px; height: 30px">Send</button>
              <button id="r" class="a" type="reset" style="left: 100px; top: 130px; width: 80px; height: 30px">Reset</button>
            </form>
            """, Steps(
                Click(140, 145), Set("#t", "value", "hello"), Set("#n", "value", "9"), Click(50, 145),
                Set("#n", "value", "4"), Click(50, 145), Click(20, 100), Click(140, 145), Move(300, 280))),

        // Implicit submission: Enter in a field clicks the form's first submit button; without one
        // the form submits only if that is its one field; Enter in a search field fires search.
        new("implicit-submission", 400, 300, "", """
            <form id="f1" data-prevent="submit">
              <input id="t1" class="a" type="text" style="left: 10px; top: 10px; width: 150px; height: 26px">
              <button id="b1" class="a" type="button" style="left: 200px; top: 10px; width: 60px; height: 26px">Btn</button>
              <button id="s1" class="a" style="left: 270px; top: 10px; width: 60px; height: 26px">Go</button>
            </form>
            <form id="f2" data-prevent="submit">
              <input id="t2" class="a" type="search" style="left: 10px; top: 60px; width: 150px; height: 26px">
            </form>
            <form id="f3" data-prevent="submit">
              <input id="t3" class="a" type="text" style="left: 10px; top: 110px; width: 150px; height: 26px">
              <input id="t4" class="a" type="text" style="left: 200px; top: 110px; width: 150px; height: 26px">
            </form>
            <form id="f4" data-prevent="submit">
              <input id="t5" class="a" type="text" style="left: 10px; top: 160px; width: 150px; height: 26px">
              <button id="s5" class="a" disabled style="left: 200px; top: 160px; width: 60px; height: 26px">Off</button>
            </form>
            """, Steps(
                Click(80, 23), Press("Enter"), Click(80, 73), Press("Enter"), Wait(30), Click(80, 123), Press("Enter"),
                Click(80, 173), Press("Enter"))),

        // CSS animation and transition events: iterations and ends, a transition cancelled during
        // its delay, one reversed part way (cancelled, the way back shortened), a negative delay, an
        // animation removed while running, one on ::before, one hidden by display: none, one on an
        // element taken out of the document (its events don't reach the document).
        new("animation-events", 420, 220, """
            @keyframes grow { from { width: 80px; } to { width: 90px; } }
            .box { position: absolute; width: 80px; height: 40px; background: #ccc; }
            #both { left: 10px; top: 10px; transition: color 240ms linear; }
            #both:hover { color: rgb(0, 0, 255); animation: grow 100ms 3; }
            #delayed { left: 110px; top: 10px; transition: color 300ms linear 1s; }
            #delayed:hover { color: rgb(0, 0, 255); }
            #rev { left: 210px; top: 10px; transition: color 1s linear; }
            #rev:hover { color: rgb(0, 0, 255); }
            #loop { left: 310px; top: 10px; }
            #loop:hover { animation: grow 300ms infinite -0.6s; }
            #pseudo { left: 10px; top: 80px; }
            #pseudo::before { content: ""; display: block; width: 10px; height: 10px; }
            #pseudo:hover::before { animation: grow 100ms; }
            #go { left: 110px; top: 80px; }
            #hider { left: 210px; top: 80px; }
            #victim { left: 310px; top: 80px; }
            #go:focus ~ #victim, #go:focus ~ #holder > #kid { animation: grow 2s infinite; }
            #hider:hover ~ #victim { display: none; }
            #holder { left: 10px; top: 150px; }
            #neg { left: 110px; top: 150px; transition: color 400ms linear -100ms; }
            #neg:hover { color: rgb(0, 0, 255); }
            """, """
            <div id="both" class="box"></div>
            <div id="delayed" class="box"></div>
            <div id="rev" class="box"></div>
            <div id="loop" class="box"></div>
            <div id="pseudo" class="box"></div>
            <button id="go" class="box">Go</button>
            <div id="hider" class="box"></div>
            <div id="victim" class="box"></div>
            <div id="holder" class="box"><div id="kid" style="height: 10px"></div></div>
            <div id="neg" class="box"></div>
            <span hidden data-log="animationstart animationiteration animationend animationcancel transitionrun transitionstart transitionend transitioncancel"></span>
            """, Steps(
                Move(50, 30), Wait(500), Move(150, 30), Wait(100), Move(250, 30), Wait(380), Move(350, 30), Wait(480),
                Move(50, 100), Wait(300), Click(150, 100), Wait(300), Move(250, 100), Wait(200),
                Set("#holder", "innerHTML", ""), Wait(100), Move(150, 170), Wait(500))),
    ];
}
