using Statventure.Editor.Import;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records.Variants;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// Import refuses a record of a kind marked as no pack's (the user's Q28 of GM1, 2026-11-14; the mark of Q26, today the
/// save's): the button's and a drop's files alike, through the refusal Import has for any file it can't take, so nothing
/// in the editor makes such an entry (New entry leaves the kind out, Q26). A kind alike but not marked imports as before.
/// The tests' two variant kinds stand for the save (the data library's kinds can't share the tests' process).
/// </summary>
public class ImportNotAPackKindTests
{
    /// <summary>The engine's kinds and the two variants: one marked as no pack's, one not.</summary>
    static RecordTypes Kinds() => new(new Dictionary<string, Type>(RecordTypes.Engine.ByName)
    {
        ["variant.not-a-pack-kind"] = typeof(NotAPackKindVariant),
        ["variant.a-pack-kind"] = typeof(APackKindVariant),
    });

    const string Refused = "Ser Alrik.json can't be imported: a not a pack kind variant (variant.not-a-pack-kind) isn't something a mod can hold.";

    [Fact]
    public void ARecordOfAKindNoPackHoldsIsRefusedByTheButtonAndADrop()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        editor.Session.Types = Kinds();
        var save = scratch.Write("downloads/Ser Alrik.json", RecordSerializer.ToJson(new NotAPackKindVariant { Name = "Ser Alrik" }));
        editor.ClickImport(save);
        Assert.Equal(("Can't import", Refused), (editor.Title(), editor.Message()));
        editor.Answer("OK");
        editor.Drop(save);
        Assert.Equal(("Can't import", Refused), (editor.Title(), editor.Message()));
        editor.Answer("OK");
        Assert.DoesNotContain(editor.Session.Entries, e => e.Source == EntrySource.New);
        Assert.False(editor.Session.HasUnsavedWork);

        // A kind alike, not marked: imported as any record's JSON form.
        var other = scratch.Write("downloads/Ser Alrik Two.json", RecordSerializer.ToJson(new APackKindVariant { Name = "Ser Alrik" }));
        editor.ClickImport(other);
        Assert.StartsWith("Ser Alrik Two.json imports as ", editor.Message());
        editor.ImportAt("notes/ser-alrik-two");
        Assert.Equal(new APackKindVariant { Name = "Ser Alrik" }, editor.Session.Entry("notes/ser-alrik-two")!.Build!.Value);
    }

    /// <summary>The session's import refuses it wherever a record's JSON form is read: the path's suggestion and the plan.</summary>
    [Fact]
    public void TheSessionsImportRefusesItAtEveryStep()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var session = editor.Session;
        session.Types = Kinds();
        var source = session.ImportSourceOf(scratch.Write("downloads/Ser Alrik.json", RecordSerializer.ToJson(new NotAPackKindVariant { Name = "Ser Alrik" })));
        const string why = "a not a pack kind variant (variant.not-a-pack-kind) isn't something a mod can hold";
        Assert.Equal(why, Assert.Throws<ImportException>(() => session.SuggestImportPath(source)).Message);
        Assert.Equal(why, Assert.Throws<ImportException>(() => session.PlanImport(source, "notes/ser-alrik")).Message);
        Assert.Equal(why, EditorSession.NotAPackKindProblem(RecordShape.Of<NotAPackKindVariant>()));
        Assert.Null(EditorSession.NotAPackKindProblem(RecordShape.Of<APackKindVariant>()));
        // "an" before a vowel.
        Assert.Equal("an unpacked variant (variant.unpacked) isn't something a mod can hold", EditorSession.NotAPackKindProblem(RecordShape.Of<UnpackedVariant>()));
    }
}
