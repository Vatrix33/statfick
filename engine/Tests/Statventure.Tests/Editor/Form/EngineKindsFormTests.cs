using System.Diagnostics;
using Statventure.Packs;
using Statventure.Records;
using Xunit.Abstractions;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// Every engine kind in the form (M11-2: no editor code per kind): a record of each, its required values given,
/// shown with a row per field and edited through UI events, the draft built to the expected record; every record of
/// the repo's audio.pak shown as read.
/// </summary>
public class EngineKindsFormTests(ITestOutputHelper output)
{
    /// <summary>Every engine record type a draft can be of (the plain ones and the unions' subtypes).</summary>
    public static TheoryData<string> Kinds()
    {
        var data = new TheoryData<string>();
        foreach (var (name, type) in RecordTypes.Engine.ByName)
            if (!RecordShape.Of(type).IsUnion)
                data.Add(name);
        return data;
    }

    /// <summary>A value for a required field: zero, false, an empty text, the first member, one element of a list, a filled record.</summary>
    static DraftValue Filler(ValueShape shape) => shape.Kind switch
    {
        ValueKind.Bool => DraftValue.Of(false),
        ValueKind.String => DraftValue.Of(""),
        ValueKind.Enum => DraftValue.Of(shape.Members[0].Value),
        ValueKind.Bytes => DraftValue.Of(Array.Empty<byte>()),
        ValueKind.Nullable => DraftValue.Null,
        ValueKind.List => new DraftList([Filler(shape.Element!)]),
        ValueKind.Map or ValueKind.Pairs => DraftMap.Empty,
        ValueKind.Record => Filled(shape.Record!),
        ValueKind.Union => Filled(shape.Record!.Subtypes[0]),
        _ => DraftValue.Of(Activator.CreateInstance(shape.Type)!),
    };

    static RecordDraft Filled(RecordShape shape)
    {
        var blank = RecordDraft.Blank(shape);
        foreach (var field in shape.Fields.Where(f => f.Required))
            blank = blank.With(field, Filler(field.Value));
        return blank;
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EveryEngineKindIsShownAndEdited(string name)
    {
        var shape = RecordShape.Of(RecordTypes.Engine.Find(name)!);
        var build = Filled(shape).Build();
        // A kind whose own type refuses the filler's values (a curve needs keys) is shown from its built default.
        Assert.True(build.Built, string.Join("; ", build.Problems));
        using var editor = FormTesting.Open(FormTesting.Entry("kinds/entry", build.Value!));
        var entry = editor.Session.Selected!;
        foreach (var field in shape.Fields)
            Assert.Equal(field.DisplayName, editor.Row("$." + field.JsonName).ElementChildren.First().ElementChildren.First(e => e.HasClass("name")).TextContent);
        if (shape.Fields.Count == 0)
            return;
        var (path, expected) = Edit(editor, shape);
        var want = entry.SavedDraft!.With(path, expected).Build();
        var got = entry.Build!;
        Assert.Equal(2, entry.History.Count);
        if (!want.Built)
        {
            // An element added to a list of records with values to give: the same problems at the same paths.
            Assert.Equal(want.Problems.Select(p => p.ToString()), got.Problems.Select(p => p.ToString()));
            return;
        }
        Assert.True(got.Built, string.Join("; ", got.Problems));
        Assert.Equal(RecordSerializer.ToBinary(shape.Type, want.Value!), RecordSerializer.ToBinary(shape.Type, got.Value!));
        Assert.NotEqual(RecordSerializer.ToBinary(shape.Type, build.Value!), RecordSerializer.ToBinary(shape.Type, got.Value!));
    }

    /// <summary>One edit by UI events of the first field that takes one (a number, a text, a bool, an enum, a nullable set, a list grown); gives its path and the value expected there.</summary>
    static (RecordPath Path, DraftValue Value) Edit(EditorHarness editor, RecordShape shape)
    {
        // A single value first; a list grown only when the kind has none.
        foreach (var field in shape.Fields.Where(f => !f.IsBulk).OrderBy(f => f.Value.Kind == ValueKind.List ? 1 : 0))
        {
            var path = RecordPath.Root.Field(field.JsonName);
            var row = editor.Row(path.ToString());
            var value = field.Value;
            if (value.IsNumber)
            {
                editor.Enter(row.Control("input", "number"), "3");
                return (path, DraftValue.Of(Convert.ChangeType(3, value.Type)));
            }
            if (value.Kind == ValueKind.String && field.Choices is null)
            {
                editor.Enter(row.Control("input", "text"), "edited");
                return (path, DraftValue.Of("edited"));
            }
            if (value.Kind == ValueKind.Bool)
            {
                var box = row.Control("input", "checkbox");
                var was = box.Checked;
                editor.Press(box);
                return (path, DraftValue.Of(!was));
            }
            if (value.Kind == ValueKind.Enum && value.Members.Count > 1)
            {
                var select = row.Control("select");
                var other = value.Members.First(m => m.DisplayName != select.SelectedOption!.TextContent);
                editor.Choose(select, other.DisplayName);
                return (path, DraftValue.Of(other.Value));
            }
            if (value.Kind == ValueKind.Nullable && value.Element!.Kind is ValueKind.String or ValueKind.Float64 or ValueKind.Float32 or ValueKind.Int32
                && editor.Session.Selected!.Draft![field] == DraftValue.Null)
            {
                editor.Press(row.Control("input", "checkbox"));
                return (path, value.Element.Kind == ValueKind.String ? DraftValue.Of("") : DraftValue.Of(Convert.ChangeType(0, value.Element.Type)));
            }
            if (value.Kind == ValueKind.List && !ValueEditorIsFixed(value.Element!))
            {
                var list = (DraftList)editor.Session.Selected!.Draft![field];
                editor.Press(row.Button("Add"));
                return (path, list.Insert(list.Count, Statventure.Editor.Form.ValueText.Start(value.Element!)));
            }
        }
        throw new InvalidOperationException($"{shape.Name} has no field the test edits");
    }

    static bool ValueEditorIsFixed(ValueShape element) => element.Kind is ValueKind.Bytes or ValueKind.Quaternion or ValueKind.Matrix4x4;

    [Fact]
    public void EveryRecordOfTheReposAudioPackShowsAsRead()
    {
        var records = FormTesting.AudioPack();
        using var editor = FormTesting.Open(records.Select(r => FormTesting.Entry(r.Key, r.Value)));
        var slowest = (Path: "", Time: 0.0);
        foreach (var entry in editor.Session.Entries)
        {
            var clock = Stopwatch.StartNew();
            editor.Session.Select(entry);
            editor.Frame();
            var time = clock.Elapsed.TotalMilliseconds;
            if (time > slowest.Time)
                slowest = (entry.Path, time);
            Assert.True(entry.Builds);
            Assert.Equal(RecordSerializer.ToBinary(entry.Kind!.Type, entry.Saved!), RecordSerializer.ToBinary(entry.Kind.Type, entry.LastBuilt!));
            foreach (var field in entry.Kind.Fields)
                editor.Row("$." + field.JsonName);
            Assert.Equal(entry.Path, editor.Document.QuerySelector(".form-path")!.TextContent);
            Assert.Empty(editor.Document.QuerySelectorAll(".problem"));
        }
        Assert.Equal(records.Count, editor.Session.Entries.Count);
        Assert.True(records.Count >= 118);
        output.WriteLine($"{records.Count} records shown; the slowest {slowest.Path}, {slowest.Time:F0} ms to show and lay out");
    }
}
