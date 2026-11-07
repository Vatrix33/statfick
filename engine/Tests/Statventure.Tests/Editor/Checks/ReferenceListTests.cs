using Statventure.Audio;
using Statventure.Editor.References;
using Statventure.Editor.Session;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Records;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Checks.CheckTesting;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// The reference field's list (M11-11): a field naming another entry opens, while it has the focus, a list of the
/// entries of the mark's kind its rule names (the game's dimmed), filtered by its text, flagging a text that names
/// none; Down, Up, Enter, a click and Escape; the mark's condition, a map's key and a name not given yet; driven
/// through the UI's input.
/// </summary>
public class ReferenceListTests
{
    static EditorHarness Parts() => FormTesting.Open([
        FormTesting.Entry("parts/arm", new Part { Name = "leg" }),
        FormTesting.Entry("parts/leg", new Part { Name = "arm" }),
        FormTesting.Entry("parts/tail", new Part { Name = "arm" }, EntrySource.Game),
        FormTesting.Entry("creatures/_grim", new Creature { Name = "Grim" }),
        FormTesting.Entry("creatures/_bold", new Creature { Name = "Bold" }),
    ]);

    static UiElement NameField(EditorHarness editor) => editor.Row("$.name").Control("input", "text");

    static string[] Options(EditorHarness editor) =>
        [.. editor.References().List!.ElementChildren.Where(e => e.HasClass("reference-option")).Select(e => e.TextContent)];

    static string? Flag(EditorHarness editor) =>
        editor.References().List!.ElementChildren.FirstOrDefault(e => e.HasClass("reference-flag"))?.TextContent;

    [Fact]
    public void FocusOpensTheListOfTheEntriesTheFieldCanName()
    {
        using var editor = Parts();
        var field = NameField(editor);
        Assert.Null(editor.References().List);
        editor.Press(field);
        var list = editor.References().List!;
        Assert.Same(field, editor.References().Field);
        Assert.Equal(["arm", "leg", "tail"], Options(editor));
        Assert.Null(Flag(editor));
        // In its row under the row's line, at the field's left and as wide; the game's entry dimmed; each with its path as tooltip.
        var row = editor.Row("$.name");
        Assert.Same(row, list.Parent);
        Assert.Same(row.ElementChildren.ElementAt(1), list);
        Assert.True(row.HasClass("reference-open"));
        Assert.Equal("true", field.GetAttribute("aria-expanded"));
        Assert.Equal("listbox", list.GetAttribute("role"));
        var options = list.ElementChildren.Where(e => e.HasClass("reference-option")).ToList();
        Assert.Equal([false, false, true], options.Select(o => o.HasClass("game")));
        // The mod's own copy of a game entry isn't dimmed.
        var tail = editor.Session.At("parts/tail");
        editor.Session.Commit(tail, tail.Draft!.With("Weight", DraftValue.Of(3.0)));
        editor.Frame();
        Assert.Equal([false, false, false], list.ElementChildren.Where(e => e.HasClass("reference-option")).Select(o => o.HasClass("game")));
        Assert.Equal("parts/tail", options[2].GetAttribute("title"));
        var at = field.GetBoundingClientRect();
        var line = row.ElementChildren.First().GetBoundingClientRect();
        var box = list.GetBoundingClientRect();
        Assert.Equal(at.X, box.X, 0.5);
        Assert.Equal(at.Width, box.Width, 0.5);
        Assert.Equal(line.Y + line.Height + 2, box.Y, 0.5);
        // The rows below make room for it.
        var weight = editor.Row("$.weight").GetBoundingClientRect();
        Assert.True(weight.Y >= box.Y + box.Height);
        // Leaving the field closes it.
        editor.Press(editor.Row("$.weight").Control("input", "number"));
        Assert.Null(editor.References().List);
        Assert.False(row.HasClass("reference-open"));
        Assert.Null(field.GetAttribute("aria-expanded"));
        Assert.Empty(row.Descendants().Where(e => e.HasClass("reference-list")));
        // Leaving it for somewhere outside the form (the entry list's filter): closed too.
        editor.Press(field);
        Assert.NotNull(editor.References().List);
        editor.Click(editor.View.Part<Statventure.Editor.Entries.EntryList>().Filter);
        Assert.NotSame(field, editor.Document.FocusedElement);
        Assert.Null(editor.References().List);
    }

    /// <summary>
    /// Opened, it offers every name, whatever the field holds; typing filters it (a name holding the text, ignoring case);
    /// a text that names no entry is flagged on its last line, typed or not.
    /// </summary>
    [Fact]
    public void TypingFiltersItAndFlagsANameThatIsntAnEntry()
    {
        using var editor = Parts();
        var field = NameField(editor);
        editor.Session.Commit(editor.Session.Selected!, editor.Session.Selected!.Draft!.With("Name", DraftValue.Of("wing")));
        editor.Session.Checks.Run();
        editor.Frame();
        var row = editor.Row("$.name");
        Assert.Equal(["no body part \"wing\" (parts/wing)"], row.Problems());
        editor.Press(field);
        Assert.Equal(["arm", "leg", "tail"], Options(editor));
        Assert.Equal("\"wing\" names no body part", Flag(editor));
        // In the row under its line, before its problem.
        Assert.Equal(["line", "reference-list", "problem"], row.ElementChildren.Select(c => c.ClassName));
        editor.Type(field, "A");
        Assert.Equal(["arm", "tail"], Options(editor));
        Assert.Equal("\"A\" names no body part", Flag(editor));
        Assert.True(editor.References().List!.ElementChildren.Last().HasClass("reference-flag"));
        editor.Type(field, "leg");
        Assert.Equal(["leg"], Options(editor));
        Assert.Null(Flag(editor));
        editor.Type(field, "wing");
        Assert.Empty(Options(editor));
        Assert.Equal("\"wing\" names no body part", Flag(editor));
        // Nothing typed: every name.
        editor.Key("Backspace", "Backspace");
        editor.Key("a", "KeyA", KeyModifiers.Control);
        editor.Key("Backspace", "Backspace");
        Assert.Equal(["arm", "leg", "tail"], Options(editor));
        // The text is only typed, not committed.
        Assert.Equal("wing", editor.Built<Part>().Name);
    }

    /// <summary>
    /// Down and Up move through it (scrolled to show the one moved to); Enter picks it, committed as the field commits
    /// (one step of the history, undone by Undo), the list closed; Enter with none moved to commits the text typed.
    /// </summary>
    [Fact]
    public void KeysMoveThroughItAndEnterPicks()
    {
        using var editor = Parts();
        var field = NameField(editor);
        editor.Press(field);
        var refs = editor.References();
        Assert.Equal(-1, refs.Active);
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(2, refs.Active);
        editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal(1, refs.Active);
        // With a modifier the key is the field's (Shift+Down selects text): the list doesn't move.
        editor.Key("ArrowDown", "ArrowDown", KeyModifiers.Shift);
        Assert.Equal(1, refs.Active);
        var options = refs.List!.ElementChildren.Where(e => e.HasClass("reference-option")).ToList();
        Assert.Equal([false, true, false], options.Select(o => o.HasClass("active")));
        Assert.Equal(["false", "true", "false"], options.Select(o => o.GetAttribute("aria-selected")));
        var steps = editor.Session.Selected!.History.Count;
        editor.Key("Enter", "Enter");
        Assert.Null(refs.List);
        Assert.Equal("leg", field.Value);
        Assert.Equal("leg", editor.Built<Part>().Name);
        Assert.Equal(steps, editor.Session.Selected!.History.Count);
        // The same name: no commit. Another: one step.
        editor.Type(field, "t");
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("Enter", "Enter");
        Assert.Equal("tail", editor.Built<Part>().Name);
        Assert.Equal(steps + 1, editor.Session.Selected!.History.Count);
        Assert.Same(field, editor.Document.FocusedElement);
        editor.Undo();
        Assert.Equal("leg", editor.Built<Part>().Name);
        // Enter with nothing moved to: the text as typed, the list closed.
        editor.Type(field, "wing");
        Assert.NotNull(refs.List);
        editor.Key("Enter", "Enter");
        Assert.Null(refs.List);
        Assert.Equal("wing", editor.Built<Part>().Name);
    }

    [Fact]
    public void AClickPicksAndKeepsTheFocus()
    {
        using var editor = Parts();
        var field = NameField(editor);
        editor.Press(field);
        var tail = editor.References().List!.ElementChildren.First(e => e.TextContent == "tail");
        editor.Click(tail);
        Assert.Equal("tail", editor.Built<Part>().Name);
        Assert.Same(field, editor.Document.FocusedElement);
        Assert.Null(editor.References().List);
    }

    /// <summary>Moving through the list leaves the field's caret where it was (Down and Up aren't the field's then); so does Down opening it.</summary>
    [Fact]
    public void MovingLeavesTheCaret()
    {
        using var editor = Parts();
        var field = NameField(editor);
        editor.Type(field, "ar");
        editor.Key("Home", "Home");
        Assert.Equal(0, field.SelectionStart);
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(0, editor.References().Active);
        Assert.Equal(0, field.SelectionStart);
        editor.Key("Escape", "Escape");
        editor.Key("ArrowDown", "ArrowDown");
        Assert.NotNull(editor.References().List);
        Assert.Equal(0, field.SelectionStart);
    }

    /// <summary>Escape closes it (the text kept, nothing committed); typing (filtering) or Down (every name) opens it again.</summary>
    [Fact]
    public void EscapeClosesAndTypingOrDownOpensAgain()
    {
        using var editor = Parts();
        var field = NameField(editor);
        editor.Type(field, "ar");
        editor.Key("Escape", "Escape");
        Assert.Null(editor.References().List);
        Assert.Equal("ar", field.Value);
        Assert.Same(field, editor.Document.FocusedElement);
        Assert.Equal("leg", editor.Built<Part>().Name);
        editor.Key("m", "KeyM");
        Assert.Equal(["arm"], Options(editor));
        // Down opens it with every name (nothing typed since), none moved to yet.
        editor.Key("Escape", "Escape");
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(["arm", "leg", "tail"], Options(editor));
        Assert.Equal(-1, editor.References().Active);
    }

    /// <summary>The "set" box of a nullable name, in the reference's row: no list (only its text field has one).</summary>
    [Fact]
    public void OnlyTheTextFieldOfAReferenceRow()
    {
        const string vale = "audio/themes/alder-vale";
        using var editor = OpenMod(w => w.AddRecord(vale, Game<ThemeSpec>(vale)));
        editor.Session.Select(editor.Session.At(vale));
        editor.Frame();
        var box = editor.Row("$.room").Control("input", "checkbox");
        box.Focus();
        editor.Frame();
        Assert.Same(box, editor.Document.FocusedElement);
        Assert.Null(editor.References().List);
        editor.Row("$.room").Control("input", "text").Focus();
        editor.Frame();
        Assert.NotNull(editor.References().List);
    }

    /// <summary>A field no mark holds for has no list: a number, a text without a reference, a reference whose condition names nothing.</summary>
    [Fact]
    public void OnlyAReferenceFieldHasAList()
    {
        using var editor = FormTesting.Open(FormTesting.Entry("kinds/one", FormScreens.Sample()));
        editor.Press(editor.Row("$.int32").Control("input", "number"));
        Assert.Null(editor.References().List);
        editor.Press(editor.Row("$.motto").Control("input", "text"));
        Assert.Null(editor.References().List);
        // The sample's motto ("none") names nothing with its text.
        editor.Press(editor.Row("$.text").Control("input", "text"));
        Assert.Null(editor.References().List);
        editor.Press(editor.Row("$.names[0]").Control("input", "text"));
        Assert.NotNull(editor.References().List);
    }

    /// <summary>The mark's condition as the draft has it, a map's key, and a name not given yet.</summary>
    [Fact]
    public void ConditionsKeysAndNamesNotGivenYet()
    {
        var sample = FormScreens.Sample();
        sample = new Kinds
        {
            Text = "Grim", Motto = "creature", Part = sample.Part, Pairs = [new("arm", "hand")], Names = ["Grim"],
        };
        using var editor = FormTesting.Open([
            FormTesting.Entry("kinds/one", sample),
            FormTesting.Entry("creatures/_grim", new Creature { Name = "Grim" }),
            FormTesting.Entry("parts/arm", new Part { Name = "arm" }),
            FormTesting.Entry("spare-parts/bolt", new Part { Name = "arm" }),
        ]);
        var text = editor.Row("$.text").Control("input", "text");
        editor.Press(text);
        Assert.Equal(["Grim"], Options(editor));
        editor.Enter(editor.Row("$.motto").Control("input", "text"), "piece");
        editor.Press(text);
        Assert.Equal(["bolt"], Options(editor));
        // A map's key: the pairs' keys name parts; their values nothing.
        var key = editor.Row("$.pairs[\"arm\"]").Own().First();
        Assert.True(key.Parent!.HasClass("key"));
        editor.Press(key);
        Assert.Equal(["arm"], Options(editor));
        Assert.True(editor.Row("$.pairs[\"arm\"]").HasClass("reference-open"));
        Assert.Equal(key.GetBoundingClientRect().X, editor.References().List!.GetBoundingClientRect().X, 0.5);
        var value = editor.Row("$.pairs[\"arm\"]").Own().First(e => e.Tag == "input" && e.Parent!.HasClass("controls"));
        Assert.NotSame(key, value);
        editor.Press(value);
        Assert.Null(editor.References().List);
        // A required name not given yet still has its list.
        var entry = editor.Session.Selected!;
        editor.Session.Commit(entry, entry.Draft!.With("Text", DraftValue.Missing));
        editor.Frame();
        editor.Press(editor.Row("$.text").Control("input", "text"));
        Assert.Equal(["bolt"], Options(editor));
        var steps = entry.History.Count;
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("Enter", "Enter");
        Assert.Equal("bolt", editor.Built<Kinds>().Text);
        // One commit (the field's own Enter, which gives a value not given yet, finds it given).
        Assert.Equal(steps + 1, entry.History.Count);
    }

    /// <summary>
    /// It follows the session: entries added while it is open join it; its field gone with its row (its list's element
    /// removed) closes it; another entry selected closes it.
    /// </summary>
    [Fact]
    public void ItFollowsTheSession()
    {
        using var editor = Parts();
        editor.Press(NameField(editor));
        editor.Session.Add("parts/wing", RecordDraft.From(new Part { Name = "arm" }));
        editor.Frame();
        Assert.Equal(["arm", "leg", "tail", "wing"], Options(editor));
        editor.Session.Select(editor.Session.At("parts/leg"));
        editor.Frame();
        Assert.Null(editor.References().List);
        // A list's element taken out: its rows are made again, the field the list was for is gone.
        var kinds = editor.Session.Add("kinds/one", RecordDraft.From(new Kinds { Text = "x", Part = new Part { Name = "x" }, Names = ["Grim", "Bold"] }));
        editor.Session.Select(kinds);
        editor.Frame();
        editor.Press(editor.Row("$.names[1]").Control("input", "text"));
        Assert.NotNull(editor.References().List);
        editor.Session.Commit(kinds, kinds.Draft!.With(RecordPath.Root.Field("names"), new DraftList([DraftValue.Of("Grim")])));
        editor.Frame();
        Assert.Null(editor.References().List);
        Assert.Null(editor.References().Field);
    }

    /// <summary>A kind with no entry to name: its list says so.</summary>
    [Fact]
    public void AKindWithNoEntriesSaysSo()
    {
        using var editor = FormTesting.Open([FormTesting.Entry("parts/arm", new Part { Name = "", Shade = Shade.Dark })]);
        editor.Press(NameField(editor));
        Assert.Empty(Options(editor));
        Assert.Equal("No creature in the data", Flag(editor));
    }

    /// <summary>More names than it shows at once: moving through them scrolls the list to show the one moved to.</summary>
    [Fact]
    public void MovingScrollsTheList()
    {
        var entries = Enumerable.Range(0, ReferenceListPart.Rows + 4).Select(i => FormTesting.Entry($"parts/p{i:00}", new Part { Name = "p00" })).ToList();
        using var editor = FormTesting.Open(entries);
        editor.Press(NameField(editor));
        var list = editor.References().List!;
        Assert.Equal(ReferenceListPart.Rows + 4, Options(editor).Length);
        Assert.Equal(0, list.ScrollTop);
        for (var i = 0; i < ReferenceListPart.Rows + 2; i++)
            editor.Key("ArrowDown", "ArrowDown");
        var active = list.ElementChildren.ElementAt(editor.References().Active);
        Assert.True(list.ScrollTop > 0);
        var box = list.GetBoundingClientRect();
        var at = active.GetBoundingClientRect();
        Assert.True(at.Y + at.Height <= box.Y + box.Height + 0.5, $"{at} isn't shown in {box}");
        // Back up to the first: scrolled to show it, at the scrollport's top (the list's padding above it).
        for (var i = 0; i < ReferenceListPart.Rows + 2; i++)
            editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal(0, editor.References().Active);
        box = list.GetBoundingClientRect();
        at = list.ElementChildren.First().GetBoundingClientRect();
        Assert.Equal(box.Y + list.ClientTop, at.Y, 0.5);
        Assert.Equal(2, list.ScrollTop, 0.5);
    }

    /// <summary>A read-only entry's field takes no focus, so no list.</summary>
    [Fact]
    public void ARead_onlyEntryHasNone()
    {
        using var editor = FormTesting.Open([FormTesting.Entry("parts/arm", new Part { Name = "leg" }), FormTesting.Entry("parts/leg", new Part { Name = "arm" })], writable: false);
        var field = NameField(editor);
        Assert.True(field.HasAttribute("disabled"));
        editor.Press(field);
        Assert.Null(editor.References().List);
    }

    /// <summary>
    /// The game's own data in a mod: a theme's room lists the game's rooms (dimmed); a hit layer's instrument the
    /// percussion voices, another layer's the instruments; the drums' keys the percussion voices.
    /// </summary>
    [Fact]
    public void TheGamesEntriesInAMod()
    {
        static IEnumerable<LayerSpec> Layers(ThemeSpec t) => t.Sections.Values.SelectMany(s => s.Layers ?? []);
        var vale = FormTesting.AudioPack().First(p => p.Value is ThemeSpec t && Layers(t).Any(l => l.Type == "hit")
            && Layers(t).Any(l => l.Drums is { Count: > 0 }) && Layers(t).Any(l => l.Type is "pad" or "bass" or "mel" or "arp")).Key;
        using var editor = OpenMod(w => w.AddRecord(vale, Game<ThemeSpec>(vale)));
        var session = editor.Session;
        session.Select(session.At(vale));
        editor.Frame();
        editor.Press(editor.Row("$.room").Control("input", "text"));
        Assert.Equal(["cathedral", "cavern", "chamber", "forest", "hall", "mountain", "open"], Options(editor));
        Assert.All(editor.References().List!.ElementChildren.Where(e => e.HasClass("reference-option")), o => Assert.True(o.HasClass("game")));
        var theme = (ThemeSpec)session.Selected!.Build!.Value!;
        string Layer(Func<LayerSpec, bool> type) => theme.Sections
            .SelectMany(s => (s.Value.Layers ?? []).Select((l, i) => (Path: $"$.sections[\"{s.Key}\"].layers[{i}]", Layer: l)))
            .First(l => type(l.Layer)).Path;
        editor.Press(editor.Row(Layer(l => l.Type == "hit") + ".instrument").Control("input", "text"));
        Assert.Contains("taiko", Options(editor));
        Assert.DoesNotContain("harp", Options(editor));
        editor.Press(editor.Row(Layer(l => l.Type is "pad" or "bass" or "mel" or "arp") + ".instrument").Control("input", "text"));
        Assert.Contains("harp", Options(editor));
        Assert.DoesNotContain("taiko", Options(editor));
        // The drums' keys: percussion voices.
        var drums = Layer(l => l.Drums is { Count: > 0 });
        var drum = theme.Sections.SelectMany(s => s.Value.Layers ?? []).First(l => l.Drums is { Count: > 0 }).Drums!.First().Key;
        editor.Press(editor.Row($"{drums}.drums[\"{drum}\"]").Own().First());
        Assert.Contains("taiko", Options(editor));
        Assert.DoesNotContain("harp", Options(editor));
    }
}
