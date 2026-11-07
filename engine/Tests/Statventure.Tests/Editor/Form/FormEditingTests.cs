using System.Numerics;
using Statventure.Editor.Session;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Records;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// Every field shape of <c>test.kinds</c> shown and edited through UI events (P11, P12): each edit one commit, the
/// draft built to the expected record; text that doesn't read stays in its field, marked, and isn't committed;
/// lists, maps, records and unions changed by their own controls; bulk and read-only values never changed.
/// </summary>
public class FormEditingTests
{
    static EditorHarness Open(Kinds? value = null, bool readOnly = false, bool writable = true) =>
        FormTesting.Open([FormTesting.Entry("kinds/sample", value ?? FormScreens.Sample(), readOnly: readOnly)], writable: writable);

    static Kinds Built(EditorHarness editor) => editor.Built<Kinds>();

    static int Steps(EditorHarness editor) => editor.Session.Selected!.History.Count;

    static UiElement Name(UiElement row) => row.ElementChildren.First().ElementChildren.First(e => e.HasClass("name"));

    static string Text(UiElement row, string className) => row.ElementChildren.First().Descendants().First(e => e.HasClass(className)).TextContent;

    [Fact]
    public void EveryFieldHasARowWithItsNameHelpAndUnit()
    {
        using var editor = Open();
        foreach (var field in RecordShape.Of<Kinds>().Fields)
            Assert.Equal(field.DisplayName, Name(editor.Row("$." + field.JsonName)).TextContent);
        Assert.Equal("A flag.", Name(editor.Row("$.flag")).GetAttribute("title"));
        Assert.Null(Name(editor.Row("$.int8")).GetAttribute("title"));
        Assert.Equal("kg", Text(editor.Row("$.part.weight"), "unit"));
        // Records, lists and maps are groups (open), leaves lines.
        Assert.Equal("details", editor.Row("$.part").Tag);
        Assert.True(editor.Row("$.part").HasAttribute("open"));
        Assert.Equal("details", editor.Row("$.ints").Tag);
        Assert.Equal("details", editor.Row("$.scores").Tag);
        Assert.Equal("div", editor.Row("$.flag").Tag);
        Assert.Equal("div", editor.Row("$.ints[0]").Tag);
        Assert.Equal("0", Name(editor.Row("$.ints[0]")).TextContent);
        Assert.Equal("1", Name(editor.Row("$.ints[1]")).TextContent);
        Assert.Equal("2 elements", Text(editor.Row("$.ints"), "count"));
        Assert.Equal("1 element", Text(editor.Row("$.doubles"), "count"));
        Assert.Equal("no elements", Text(editor.Row("$.floats"), "count"));
        Assert.Equal("1 entry", Text(editor.Row("$.scores"), "count"));
        Assert.Equal("no entries", Text(editor.Row("$.byNumber"), "count"));
        // Nested rows have their paths: a map's values by their keys, a union's fields as the subtype's.
        Assert.Equal("gold", editor.Row("$.scores[\"gold\"]").Own().First().Value);
        Assert.Equal("ink", editor.Row("$.shape.tint").Control("input", "text").Value);
        Assert.Equal("2", editor.Row("$.shape.radius").Control("input", "number").Value);
        Assert.Equal("Circle", editor.Row("$.shape").Control("select").SelectedOption!.TextContent);
        Assert.Equal("arm", editor.Row("$.pairs[\"arm\"]").Own().First().Value);
        Assert.Equal("Pale", editor.Row("$.byShade[\"pale\"]").Own().First().SelectedOption!.TextContent);
    }

    [Fact]
    public void BulkAndReadOnlyValuesShowWithoutControls()
    {
        using var editor = Open();
        Assert.Equal("4 bytes", Text(editor.Row("$.bytes"), "summary"));
        Assert.Equal("none", Text(editor.Row("$.maybeBytes"), "summary"));
        Assert.Equal("2 points", Text(editor.Row("$.points"), "summary"));
        Assert.Equal("0 parts", Text(editor.Row("$.parts"), "summary"));
        Assert.Equal("(1, 0, 0, 0; 0, 1, 0, 0; 0, 0, 1, 0; 0, 0, 0, 1)", Text(editor.Row("$.matrix"), "summary"));
        foreach (var path in new[] { "$.bytes", "$.maybeBytes", "$.points", "$.parts", "$.matrix", "$.rotation" })
            Assert.Empty(editor.Row(path).Own());
        // A list of matrices can't grow either (only imported models have them, P11).
        Assert.Empty(editor.Row("$.matrices").Own());
        // An edit elsewhere builds them as the very objects read (P10, P21).
        var sample = (Kinds)editor.Session.Selected!.Saved!;
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        var built = Built(editor);
        Assert.Same(sample.Bytes, built.Bytes);
        Assert.Same(sample.Points, built.Points);
        Assert.Same(sample.Parts, built.Parts);
    }

    [Fact]
    public void ColoursRoundToTheNearestOf255Steps()
    {
        Assert.Equal("#80ff00", Statventure.Editor.Form.ValueText.Hex([0.5f, 0.999f, -1f]));
        Assert.Equal("#00000080", Statventure.Editor.Form.ValueText.Hex([float.NaN, 0, 0, 0.502f]));
    }

    [Fact]
    public void ACheckboxCommitsABool()
    {
        using var editor = Open();
        var box = editor.Row("$.flag").Control("input", "checkbox");
        Assert.True(box.Checked);
        editor.Press(box);
        Assert.False(Built(editor).Flag);
        Assert.Equal(2, Steps(editor));
        editor.Press(editor.Row("$.bools[0]").Control("input", "checkbox"));
        Assert.Equal([false], Built(editor).Bools);
    }

    public static TheoryData<string, string, object> Integers => new()
    {
        { "int8", "-128", (sbyte)-128 },
        { "uInt8", "199", (byte)199 },
        { "int16", "-32768", (short)-32768 },
        { "uInt16", "65535", (ushort)65535 },
        { "int32", "-2147483648", int.MinValue },
        { "uInt32", "4294967295", uint.MaxValue },
        { "int64", "-9223372036854775808", long.MinValue },
        { "uInt64", "18446744073709551615", ulong.MaxValue },
    };

    [Theory]
    [MemberData(nameof(Integers))]
    public void NumberFieldsCommitEachIntegerKindOnEnter(string field, string typed, object expected)
    {
        using var editor = Open();
        var input = editor.Row("$." + field).Control("input", "number");
        Assert.Equal("1", input.GetAttribute("step"));
        editor.Enter(input, typed);
        Assert.Equal(expected, RecordShape.Of<Kinds>().Field(field)!.Get(Built(editor)));
        Assert.Equal(typed, input.Value);
        Assert.Equal(2, Steps(editor));
    }

    [Fact]
    public void FloatsCommitAndShowTheirShortestForm()
    {
        using var editor = Open();
        var single = editor.Row("$.float32").Control("input", "number");
        Assert.Equal("any", single.GetAttribute("step"));
        editor.Enter(single, "0.1");
        Assert.Equal(0.1f, Built(editor).Float32);
        Assert.Equal("0.1", single.Value);
        var dbl = editor.Row("$.float64").Control("input", "number");
        editor.Enter(dbl, "1e-7");
        Assert.Equal(1e-7, Built(editor).Float64);
        Assert.Equal("1E-07", dbl.Value);
        Assert.Equal(3, Steps(editor));
        // The same value typed another way is no commit; the field shows it as the form writes it.
        editor.Enter(single, "0.10");
        Assert.Equal(3, Steps(editor));
        Assert.Equal("0.1", single.Value);
        Assert.False(single.HasClass("bad"));
        // The type's and the limits' ends bound the field (the slider's ends are the limits, M11-10).
        var bytes = editor.Row("$.uInt8").Control("input", "number");
        Assert.Equal(("1", "200"), (bytes.GetAttribute("min"), bytes.GetAttribute("max")));
        Assert.Equal(("-128", "127"), (editor.Row("$.int8").Control("input", "number").GetAttribute("min"), editor.Row("$.int8").Control("input", "number").GetAttribute("max")));
        Assert.Equal(("-1", "1"), (single.GetAttribute("min"), single.GetAttribute("max")));
        Assert.Null(dbl.GetAttribute("min"));
        var slider = editor.Row("$.float32").Control("input", "range");
        Assert.Equal(("-1", "1", "any"), (slider.GetAttribute("min"), slider.GetAttribute("max"), slider.GetAttribute("step")));
        // The slider follows the value and stays out of the tab order (the field is there).
        Assert.Equal("0.1", slider.Value);
        Assert.Equal("-1", slider.GetAttribute("tabindex"));
        Assert.Equal(("1", "200", "1"), (editor.Row("$.uInt8").Control("input", "range").GetAttribute("min"), editor.Row("$.uInt8").Control("input", "range").GetAttribute("max"), editor.Row("$.uInt8").Control("input", "range").GetAttribute("step")));
        // No slider without two finite limits.
        Assert.DoesNotContain(editor.Row("$.float64").Own(), e => e.InputType == "range");
        Assert.DoesNotContain(editor.Row("$.part.weight").Own(), e => e.InputType == "range");
    }

    [Fact]
    public void TextThatDoesntReadStaysMarkedAndIsntCommitted()
    {
        using var editor = Open();
        var entry = editor.Session.Selected!;
        var draft = entry.Draft;
        var row = editor.Row("$.int8");
        var input = row.Control("input", "number");
        void Refused(string typed, string problem)
        {
            editor.Type(input, typed);
            if (typed.Length == 0)
                editor.Key("Backspace", "Backspace");
            editor.Key("Enter", "Enter");
            Assert.Same(draft, entry.Draft);
            Assert.Equal(1, Steps(editor));
            Assert.True(input.HasClass("bad"));
            Assert.Equal([problem], row.Problems());
            Assert.Equal(typed, input.EditorText);
        }
        Refused("300", "\"300\" isn't a whole number from -128 to 127");
        Refused("1.5", "\"1.5\" isn't a whole number");
        Refused("", "needs a number");
        var single = editor.Row("$.float32");
        editor.Enter(single.Control("input", "number"), "1e");
        Assert.Equal(["\"1e\" isn't a number"], single.Problems());
        editor.Enter(single.Control("input", "number"), "1e39");
        Assert.Equal(["\"1e39\" is too large for this field"], single.Problems());
        Assert.Same(draft, entry.Draft);
        // A commit elsewhere leaves the text that doesn't read where it is, marked.
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        Assert.Equal("1e39", single.Control("input", "number").EditorText);
        Assert.Equal(["\"1e39\" is too large for this field"], single.Problems());
        Assert.True(row.HasClass("has-problem"));
        // Text that reads clears the mark and commits.
        editor.Enter(input, "-5");
        Assert.False(input.HasClass("bad"));
        Assert.Empty(row.Problems());
        Assert.False(row.HasClass("has-problem"));
        Assert.Equal(-5, Built(editor).Int8);
    }

    [Fact]
    public void LeavingAFieldCommitsAndArrowKeysCommitAStep()
    {
        using var editor = Open();
        var input = editor.Row("$.int32").Control("input", "number");
        editor.Type(input, "77");
        Assert.Equal(1, Steps(editor));
        editor.Key("Tab", "Tab");
        Assert.Equal(77, Built(editor).Int32);
        Assert.Equal(2, Steps(editor));
        var shorts = editor.Row("$.int16").Control("input", "number");
        editor.Press(shorts);
        editor.Key("ArrowUp", "ArrowUp");
        Assert.Equal(301, Built(editor).Int16);
        Assert.Equal(3, Steps(editor));
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(299, Built(editor).Int16);
        Assert.Equal(5, Steps(editor));
    }

    [Fact]
    public void ASliderDragIsOneStep()
    {
        using var editor = Open();
        var row = editor.Row("$.uInt8");
        var slider = row.Control("input", "range");
        var field = row.Control("input", "number");
        slider.ScrollIntoView(ScrollAlign.Center);
        editor.Frame();
        var box = slider.GetBoundingClientRect();
        var start = editor.Middle(slider) with { X = box.X + box.Width * 0.2f };
        editor.Input.PointerMove(start);
        editor.Input.PointerDown(start, MouseButton.Left);
        editor.Frame();
        foreach (var share in new[] { 0.4f, 0.6f, 0.8f })
        {
            editor.Input.PointerMove(start with { X = box.X + box.Width * share });
            editor.Frame();
            // The field follows the slider; nothing is committed while it moves.
            Assert.Equal(slider.Value, field.Value);
            Assert.Equal(1, Steps(editor));
        }
        editor.Input.PointerUp(start with { X = box.X + box.Width * 0.8f }, MouseButton.Left);
        editor.Frame();
        Assert.Equal(2, Steps(editor));
        Assert.Equal(byte.Parse(slider.Value), Built(editor).UInt8);
        Assert.InRange(Built(editor).UInt8, 140, 180);
    }

    [Fact]
    public void TextFieldsAndColourStrings()
    {
        using var editor = Open();
        editor.Enter(editor.Row("$.motto").Control("input", "text"), "Fear no tails");
        Assert.Equal("Fear no tails", Built(editor).Motto);
        var note = editor.Row("$.note");
        var swatch = note.ElementChildren.First().Descendants().First(e => e.HasClass("swatch"));
        Assert.Equal("#d77a50", swatch.GetStyle("background-color"));
        editor.Enter(note.Control("input", "text"), "#336699");
        Assert.Equal("#336699", Built(editor).Note);
        Assert.Equal("#336699", swatch.GetStyle("background-color"));
        // Text that would end the swatch's declaration is the value, but never goes into its style (nor stays the old colour).
        editor.Enter(note.Control("input", "text"), "red; width: 99px");
        Assert.Equal("red; width: 99px", Built(editor).Note);
        Assert.Null(swatch.GetStyle("background-color"));
        Assert.Null(swatch.GetStyle("width"));
        // A required text not given takes the empty text on Enter.
        var entry = editor.Session.Selected!;
        editor.Session.Commit(entry, entry.Draft!.With("text", DraftValue.Missing));
        var text = editor.Row("$.text");
        Assert.Equal(["needs a value"], text.Problems());
        Assert.Equal("", text.Control("input", "text").Value);
        editor.Press(text.Control("input", "text"));
        editor.Key("Enter", "Enter");
        Assert.Equal("", Built(editor).Text);
        Assert.Empty(text.Problems());
    }

    [Fact]
    public void SelectsChooseChoicesAndMembers()
    {
        using var editor = Open();
        var shade = editor.Row("$.shade").Control("select");
        Assert.Equal(["Pale", "Dark", "Golden"], shade.Options.Select(o => o.TextContent));
        editor.Choose(shade, "Golden");
        Assert.Equal(Shade.Golden, Built(editor).Shade);
        editor.Choose(editor.Row("$.huge").Control("select"), "Small");
        Assert.Equal(Huge.Small, Built(editor).Huge);
        // A nullable enum: set (its first member), then chosen.
        var pace = editor.Row("$.pace");
        Assert.Equal("none", Text(pace, "none"));
        editor.Press(pace.Control("input", "checkbox"));
        Assert.Equal(Pace.Slow, Built(editor).Pace);
        editor.Choose(pace.Control("select"), "Fast");
        Assert.Equal(Pace.Fast, Built(editor).Pace);
        // Strings with choices: the empty one by its name.
        var word = editor.Row("$.words[0]").Control("select");
        Assert.Equal(["a", "(empty)", "β"], word.Options.Select(o => o.TextContent));
        editor.Choose(word, "(empty)");
        Assert.Equal([""], Built(editor).Words);
        Assert.Equal(6, Steps(editor));
    }

    [Fact]
    public void MembersShowByTheirDisplayNames()
    {
        using var editor = FormTesting.Open(FormTesting.Entry("cues/stop", new Statventure.Scene.AudioCue(1, Statventure.Scene.AudioCueKind.StopMusic)));
        var kind = editor.Row("$.kind").Control("select");
        Assert.Equal(["Effect", "Music", "Stop music"], kind.Options.Select(o => o.TextContent));
        Assert.Equal("Stop music", kind.SelectedOption!.TextContent);
        editor.Choose(kind, "Music");
        Assert.Equal(Statventure.Scene.AudioCueKind.Music, editor.Built<Statventure.Scene.AudioCue>().Kind);
    }

    [Fact]
    public void ValuesThatArentChoicesOrMembersShowAsSuch()
    {
        var sample = FormScreens.Sample();
        using var editor = Open(new Kinds { Text = "t", Part = sample.Part, Words = ["zz"], Shade = (Shade)3 });
        // Not touched, it isn't changed, though its record can't be written (a value that isn't a member).
        Assert.False(editor.Session.Selected!.IsChanged);
        Assert.False(editor.Session.HasUnsavedWork);
        Assert.Equal(["a", "(empty)", "β", "zz (not a choice)"], editor.Row("$.words[0]").Control("select").Options.Select(o => o.TextContent));
        Assert.Equal(3, editor.Row("$.words[0]").Control("select").SelectedIndex);
        Assert.Equal(["Pale", "Dark", "Golden", "3 (not a member)"], editor.Row("$.shade").Control("select").Options.Select(o => o.TextContent));
        Assert.Equal(["3 isn't a member of Shade"], editor.Row("$.shade").Problems());
        editor.Choose(editor.Row("$.shade").Control("select"), "Dark");
        Assert.Equal(["Pale", "Dark", "Golden"], editor.Row("$.shade").Control("select").Options.Select(o => o.TextContent));
    }

    [Fact]
    public void NullablesAreSetAndUnset()
    {
        using var editor = Open();
        var count = editor.Row("$.count");
        editor.Press(count.Control("input", "checkbox"));
        Assert.Null(Built(editor).Count);
        Assert.Equal("none", Text(count, "none"));
        editor.Press(count.Control("input", "checkbox"));
        Assert.Equal(0, Built(editor).Count);
        editor.Enter(count.Control("input", "number"), "12");
        Assert.Equal(12, Built(editor).Count);
        // A nullable record starts as its blank draft: its required name not given.
        editor.Press(editor.Row("$.maybePart").Control("input", "checkbox"));
        Assert.Equal(["needs a value"], editor.Row("$.maybePart.name").Problems());
        Assert.False(editor.Session.Selected!.Builds);
        editor.Enter(editor.Row("$.maybePart.name").Control("input", "text"), "Tail");
        Assert.Equal(new Part { Name = "Tail" }, Built(editor).MaybePart);
        // A nullable union starts as its first subtype's blank; a nullable list empty.
        editor.Press(editor.Row("$.maybeShape").Control("input", "checkbox"));
        Assert.Equal(["needs a value"], editor.Row("$.maybeShape.radius").Problems());
        editor.Enter(editor.Row("$.maybeShape.radius").Control("input", "number"), "4");
        Assert.Equal(new Circle { Radius = 4 }, Built(editor).MaybeShape);
        editor.Press(editor.Row("$.maybeInts").Control("input", "checkbox"));
        Assert.Equal([], Built(editor).MaybeInts!);
        editor.Press(editor.Row("$.maybeInts").Button("Add"));
        Assert.Equal([0], Built(editor).MaybeInts!);
        editor.Press(editor.Row("$.maybeInts").Control("input", "checkbox"));
        Assert.Null(Built(editor).MaybeInts);
        Assert.Empty(editor.Row("$.maybeInts").ElementChildren.Skip(1).SelectMany(c => c.Descendants()).Where(e => e.Tag == "input"));
        editor.Press(editor.Row("$.maybeRotation").Control("input", "checkbox"));
        Assert.Equal(Quaternion.Identity, Built(editor).MaybeRotation);
    }

    [Fact]
    public void VectorsTakeTheirComponentsAndColoursTheirHex()
    {
        using var editor = Open();
        var v2 = editor.Row("$.v2").Own().Where(e => e.InputType == "number").ToList();
        Assert.Equal(["1", "2"], v2.Select(i => i.Value));
        Assert.Equal(["X", "Y"], v2.Select(i => i.GetAttribute("title")));
        editor.Enter(v2[1], "5.5");
        Assert.Equal(new Vector2(1, 5.5f), Built(editor).V2);
        var v3 = editor.Row("$.v3").Control("input", "text");
        Assert.Equal("#d67a4f", v3.Value);
        editor.Enter(v3, "#ff0080");
        Assert.Equal(new Vector3(1, 0, 128 / 255f), Built(editor).V3);
        var v4 = editor.Row("$.v4").Control("input", "text");
        Assert.Equal("#336699ff", v4.Value);
        editor.Enter(v4, "#00ff0080");
        Assert.Equal(new Vector4(0, 1, 0, 128 / 255f), Built(editor).V4);
        editor.Enter(v4, "#0000ff");
        Assert.Equal(new Vector4(0, 0, 1, 1), Built(editor).V4);
        var steps = Steps(editor);
        editor.Enter(v3, "#ff008080");
        Assert.Equal(["\"#ff008080\" isn't a colour (#rrggbb)"], editor.Row("$.v3").Problems());
        editor.Enter(v3, "#12");
        Assert.Equal(["\"#12\" isn't a colour (#rrggbb)"], editor.Row("$.v3").Problems());
        editor.Enter(v4, "red");
        Assert.Equal(["\"red\" isn't a colour (#rrggbb or #rrggbbaa)"], editor.Row("$.v4").Problems());
        Assert.Equal(steps, Steps(editor));
        var swatch = editor.Row("$.v4").ElementChildren.First().Descendants().First(e => e.HasClass("swatch"));
        Assert.Equal("#0000ffff", swatch.GetStyle("background-color"));
    }

    [Fact]
    public void ListsAddMoveAndRemoveTheirElements()
    {
        using var editor = Open();
        var ints = editor.Row("$.ints");
        editor.Press(ints.Button("Add"));
        Assert.Equal([2, -3, 0], Built(editor).Ints);
        Assert.True(editor.Row("$.ints[0]").Button("Up").HasAttribute("disabled"));
        Assert.True(editor.Row("$.ints[2]").Button("Down").HasAttribute("disabled"));
        Assert.False(editor.Row("$.ints[1]").Button("Down").HasAttribute("disabled"));
        editor.Press(editor.Row("$.ints[2]").Button("Up"));
        Assert.Equal([2, 0, -3], Built(editor).Ints);
        editor.Press(editor.Row("$.ints[0]").Button("Down"));
        Assert.Equal([0, 2, -3], Built(editor).Ints);
        editor.Press(editor.Row("$.ints[1]").Button("Remove"));
        Assert.Equal([0, -3], Built(editor).Ints);
        editor.Enter(editor.Row("$.ints[1]").Control("input", "number"), "4");
        Assert.Equal([0, 4], Built(editor).Ints);
        Assert.Equal(6, Steps(editor));
        // Elements of every kind start as their kind's start: a list, a union's first subtype, a record, a member, a text.
        editor.Press(editor.Row("$.jagged[0]").Button("Add"));
        Assert.Equal([1.0, 2, 0], Built(editor).Jagged[0]);
        editor.Press(editor.Row("$.jagged").Button("Add"));
        Assert.Equal(2, Built(editor).Jagged.Length);
        Assert.Empty(Built(editor).Jagged[1]);
        editor.Press(editor.Row("$.paces").Button("Add"));
        Assert.Equal([Pace.Slow], Built(editor).Paces);
        editor.Press(editor.Row("$.gaps").Button("Add"));
        Assert.Equal([""], Built(editor).Gaps);
        editor.Press(editor.Row("$.empties").Button("Add"));
        Assert.Single(Built(editor).Empties);
        editor.Press(editor.Row("$.names").Button("Add"));
        editor.Enter(editor.Row("$.names[1]").Control("input", "text"), "Bold");
        Assert.Equal(["Grim", "Bold"], Built(editor).Names);
        editor.Press(editor.Row("$.floatList").Button("Add"));
        editor.Enter(editor.Row("$.floatList[0]").Control("input", "number"), "2.5");
        Assert.Equal([2.5f], Built(editor).FloatList);
        editor.Press(editor.Row("$.maybeParts").Button("Add"));
        Assert.Equal(["needs a value"], editor.Row("$.maybeParts[0].name").Problems());
        editor.Press(editor.Row("$.maybeParts[0]").Control("input", "checkbox"));
        Assert.Equal([null], Built(editor).MaybeParts);
        editor.Press(editor.Row("$.shapes").Button("Add"));
        Assert.Equal(["needs a value"], editor.Row("$.shapes[0].radius").Problems());
        editor.Enter(editor.Row("$.shapes[0].radius").Control("input", "number"), "1");
        Assert.Equal([new Circle { Radius = 1 }], Built(editor).Shapes);
    }

    [Fact]
    public void MapsTakeKeysAndValues()
    {
        using var editor = Open();
        editor.Press(editor.Row("$.scores").Button("Add"));
        Assert.Equal(new Dictionary<string, int> { ["gold"] = 3, [""] = 0 }, Built(editor).Scores);
        var key = editor.Row("$.scores[\"\"]").Own().First();
        editor.Enter(key, "silver");
        editor.Enter(editor.Row("$.scores[\"silver\"]").Control("input", "number"), "5");
        Assert.Equal(new Dictionary<string, int> { ["gold"] = 3, ["silver"] = 5 }, Built(editor).Scores);
        // A key there twice is the map's problem; the draft doesn't build until it is told apart.
        editor.Enter(editor.Row("$.scores[\"silver\"]").Own().First(), "gold");
        Assert.Equal(["the key gold is there twice"], editor.Row("$.scores").Problems());
        Assert.False(editor.Session.Selected!.Builds);
        editor.Press(editor.Row("$.scores").Button("Add"));
        editor.Press(editor.Row("$.scores").Button("Add"));
        var keys = editor.Row("$.scores").Descendants().Where(e => e.HasClass("key")).Select(k => k.Descendants().First(e => e.Tag == "input").Value).ToList();
        Assert.Equal(["gold", "gold", "", "new"], keys);
        // Dictionaries don't move their entries; pairs do.
        Assert.DoesNotContain(editor.Row("$.scores[\"gold\"]").Own(), b => b.TextContent == "Up");
        editor.Press(editor.Row("$.pairs").Button("Add"));
        editor.Press(editor.Row("$.pairs[\"\"]").Button("Up"));
        Assert.Equal([new("", ""), new("arm", "hand")], Built2(editor).Pairs);
        // Integer keys count on from the largest, members take the first not used.
        editor.Press(editor.Row("$.byNumber").Button("Add"));
        editor.Press(editor.Row("$.byNumber").Button("Add"));
        Assert.Equal([0, 1], Built2(editor).ByNumber.Keys);
        editor.Enter(editor.Row("$.byNumber[\"1\"]").Own().First(), "7");
        Assert.Equal([0, 7], Built2(editor).ByNumber.Keys);
        editor.Press(editor.Row("$.byNumber").Button("Add"));
        Assert.Equal([0, 7, 8], Built2(editor).ByNumber.Keys);
        Assert.Equal([""], Built2(editor).ByNumber.Values.Distinct());
        editor.Press(editor.Row("$.byShade").Button("Add"));
        Assert.Equal([Shade.Pale, Shade.Dark], Built2(editor).ByShade.Keys);
        editor.Choose(editor.Row("$.byShade[\"dark\"]").Own().First(), "Golden");
        Assert.Equal([Shade.Pale, Shade.Golden], Built2(editor).ByShade.Keys);
        editor.Press(editor.Row("$.sparse").Button("Add"));
        Assert.Equal(["needs a value"], editor.Row("$.sparse[\"0\"].name").Problems());
        editor.Press(editor.Row("$.ordered").Control("input", "checkbox"));
        editor.Press(editor.Row("$.ordered").Button("Add"));
        editor.Press(editor.Row("$.shapesByName").Button("Add"));
        editor.Choose(editor.Row("$.shapesByName[\"\"]").Control("select"), "Dot");
        editor.Press(editor.Row("$.shadePairs").Button("Add"));
        editor.Enter(editor.Row("$.shadePairs[\"pale\"]").Control("input", "number"), "9");
        editor.Enter(editor.Row("$.sparse[\"0\"].name").Control("input", "text"), "Fin");
        var built = Built2(editor);
        Assert.Equal(new Part { Name = "Fin" }, built.Sparse[0]);
        Assert.Equal(0, built.Ordered![""]);
        Assert.Equal(new Dot(), built.ShapesByName[""]);
        Assert.Equal([new(Shade.Pale, 9)], built.ShadePairs);
        editor.Press(editor.Row("$.byNumber[\"7\"]").Button("Remove"));
        Assert.Equal([0, 8], Built2(editor).ByNumber.Keys);

        // The scores' twin keys still keep it from building; the rest is read as the map that builds.
        static Kinds Built2(EditorHarness editor)
        {
            var draft = editor.Session.Selected!.Draft!;
            var scores = (DraftMap)draft["scores"];
            var fixedDraft = draft.With("scores", new DraftMap(scores.Entries.Select((e, i) => e with { Key = $"k{i}" })));
            var build = fixedDraft.Build();
            Assert.True(build.Built, string.Join("; ", build.Problems));
            return (Kinds)build.Value!;
        }
    }

    [Fact]
    public void RecordsAndUnionsEditTheirFields()
    {
        using var editor = Open();
        editor.Enter(editor.Row("$.part.name").Control("input", "text"), "Bold");
        editor.Enter(editor.Row("$.part.weight").Control("input", "number"), "3.5");
        editor.Enter(editor.Row("$.joint.a").Control("input", "number"), "7");
        editor.Press(editor.Row("$.point.label").Control("input", "checkbox"));
        editor.Enter(editor.Row("$.point.label").Control("input", "text"), "here");
        var built = Built(editor);
        Assert.Equal(new Part { Name = "Bold", Weight = 3.5 }, built.Part);
        Assert.Equal(new Joint(7, 2), built.Joint);
        Assert.Equal(new Point(1, 2, Label: "here"), built.Point);
        // Switching a union's subtype keeps the fields of the same name and kind (P11): the tint stays.
        editor.Enter(editor.Row("$.shape.tint").Control("input", "text"), "ember");
        editor.Choose(editor.Row("$.shape").Control("select"), "Square");
        Assert.Equal(new Square { Tint = "ember" }, Built(editor).Shape);
        Assert.Throws<InvalidOperationException>(() => editor.Row("$.shape.radius"));
        editor.Press(editor.Row("$.shape.inner").Control("input", "checkbox"));
        editor.Enter(editor.Row("$.shape.inner.radius").Control("input", "number"), "2");
        Assert.Equal(new Square { Tint = "ember", Inner = new Circle { Radius = 2 } }, Built(editor).Shape);
        editor.Choose(editor.Row("$.shape").Control("select"), "Dot");
        Assert.Equal(new Dot { Tint = "ember" }, Built(editor).Shape);
    }

    [Fact]
    public void ASwitchKeepsOnlyFieldsOfTheSameKind()
    {
        // Subtypes sharing the name "value" with different kinds of value: the number isn't carried into the text.
        using var editor = FormTesting.Open(FormTesting.Entry("switches/one",
            new Statventure.Tests.Records.Variants.SwitchHolder { Switch = new Statventure.Tests.Records.Variants.SwitchCount { Tag = "t", Value = 5 } }));
        editor.Choose(editor.Row("$.switch").Control("select"), "Switch name");
        Assert.Equal(new Statventure.Tests.Records.Variants.SwitchName { Tag = "t" }, editor.Built<Statventure.Tests.Records.Variants.SwitchHolder>().Switch);
        Assert.Equal("", editor.Row("$.switch.value").Control("input", "text").Value);
    }

    [Fact]
    public void ARequiredRecordNotGivenIsMade()
    {
        using var editor = Open();
        var made = editor.Session.Add("kinds/new", RecordDraft.Blank(typeof(Kinds)));
        editor.Session.Select(made);
        editor.Frame();
        var part = editor.Row("$.part");
        Assert.Equal(["needs a value"], part.Problems());
        Assert.Equal(["needs a value"], editor.Row("$.text").Problems());
        Assert.True(part.HasClass("has-problem"));
        editor.Press(part.Button("Make"));
        Assert.Equal(["needs a value"], editor.Row("$.part.name").Problems());
        Assert.Empty(editor.Row("$.part").Problems());
        Assert.Throws<InvalidOperationException>(() => editor.Row("$.part").Button("Make"));
        editor.Enter(editor.Row("$.part.name").Control("input", "text"), "Arm");
        editor.Enter(editor.Row("$.text").Control("input", "text"), "t");
        Assert.Equal(new Part { Name = "Arm" }, Built(editor).Part);
    }

    [Fact]
    public void ReadOnlyEntriesShowEveryControlDisabled()
    {
        foreach (var (readOnly, writable) in new[] { (true, true), (false, false) })
        {
            using var editor = Open(readOnly: readOnly, writable: writable);
            var controls = editor.Document.Body.Descendants().Where(e => e.IsDescendantOf(editor.Document.QuerySelector(".form-rows")!) && e.Tag is "input" or "select").ToList();
            Assert.True(controls.Count > 50);
            Assert.All(controls, c => Assert.True(c.IsDisabled));
            Assert.Empty(editor.Document.QuerySelectorAll(".form-rows button"));
            Assert.Equal("read-only", editor.Document.QuerySelector(".form-state")!.TextContent);
            Assert.False(editor.View.Commands.CanRun(Statventure.Editor.Shell.EditorCommand.Undo));
        }
    }

    [Fact]
    public void ProblemsShowUnderTheirRowsAndThoseOfTheWholeEntryAtTheTop()
    {
        using var editor = Open();
        var entry = editor.Session.Selected!;
        editor.Session.AddCheck((_, _) =>
        [
            new RecordProblem(RecordPath.Root, "is odd as a whole"),
            new RecordProblem(RecordPath.Root.Field("ints"), "has too few"),
            new RecordProblem(RecordPath.Root.Field("ints").Index(1), "is negative"),
            new RecordProblem(RecordPath.Root.Field("part").Field("name"), "isn't on the roster"),
            new RecordProblem(RecordPath.Root.Field("points").Index(1), "is far away"),
            new RecordProblem(RecordPath.Root.Field("scores").Key("gold"), "is too much"),
        ]);
        editor.Session.Recheck(entry);
        Assert.Equal(["is odd as a whole"], editor.Document.QuerySelectorAll(".form-problems .problem").Select(p => p.TextContent));
        Assert.Equal(["is negative"], editor.Row("$.ints[1]").Problems());
        Assert.Equal(["isn't on the roster"], editor.Row("$.part.name").Problems());
        // A path without a row of its own shows under its nearest row above (a bulk field's element).
        Assert.Equal(["is far away"], editor.Row("$.points").Problems());
        Assert.Equal(["is too much"], editor.Row("$.scores[\"gold\"]").Problems());
        // A group's problem is under its summary, before its rows.
        Assert.Equal(["has too few"], editor.Row("$.ints").Problems());
        Assert.Equal(["problem", "items"], editor.Row("$.ints").ElementChildren.Skip(1).Select(c => c.ClassName));
        Assert.True(editor.Row("$.part.name").HasClass("has-problem"));
        Assert.False(editor.Row("$.part").HasClass("has-problem"));
        // Shown again after every change; gone when the check stops giving them.
        editor.Session.AddCheck((_, _) => [new RecordProblem(RecordPath.Root.Field("flag"), "is set")]);
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        Assert.Equal(["is set"], editor.Row("$.flag").Problems());
        Assert.Equal(["is negative"], editor.Row("$.ints[1]").Problems());
    }
}
