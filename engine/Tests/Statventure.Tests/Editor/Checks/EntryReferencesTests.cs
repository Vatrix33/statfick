using Statventure.Editor.References;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Records;
using static Statventure.Tests.Editor.Checks.CheckTesting;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// References in drafts (M11-11; <see cref="EntryReferences"/>): every name a mark holds for, in nested records, lists,
/// maps' values and keys, through nullables and unions, by each mark's condition (read from the draft); what a draft's
/// text field at a path names; pointing names at another entry; the entries a mark can name; the session's lookup.
/// </summary>
public class EntryReferencesTests
{
    static Kinds Sample() => new()
    {
        Text = "Grim",
        Motto = "creature",
        Names = ["Grim", "Bold"],
        Parts = new Dictionary<string, Part> { ["bulk"] = new() { Name = "hidden" } },
        Pairs = [new("arm", "hand"), new("leg", "foot")],
        ByNumber = new() { [1] = "kinds/sets/a", [2] = null },
        Ordered = new() { ["tail"] = 1 },
        Sparse = new() { [7] = new Part { Name = "wing" } },
        Part = new Part { Name = "Grim", Shade = Shade.Dark },
        MaybePart = new Part { Name = "arm" },
        MaybeParts = [null, new Part { Name = "leg" }],
    };

    static string Text(ReferenceAt r) => $"{r.Path} {r.Name} -> {r.Target ?? "-"} ({r.Reference.Kind.Name}{(r.Map is null ? "" : ", key")})";

    [Fact]
    public void EveryReferenceInADraftIsFoundInFieldOrder()
    {
        Assert.Equal(
        [
            "$.text Grim -> creatures/_grim (test.creature)",
            "$.names[0] Grim -> creatures/_grim (test.creature)",
            "$.names[1] Bold -> creatures/_bold (test.creature)",
            "$.pairs[\"arm\"] arm -> parts/arm (test.part, key)",
            "$.pairs[\"leg\"] leg -> parts/leg (test.part, key)",
            "$.byNumber[\"1\"] kinds/sets/a -> kinds/sets/a (test.kinds)",
            "$.ordered[\"tail\"] tail -> parts/tail (test.part, key)",
            "$.sparse[\"7\"].name wing -> parts/wing (test.part)",
            "$.part.name Grim -> creatures/_grim (test.creature)",
            "$.maybePart.name arm -> parts/arm (test.part)",
            "$.maybeParts[1].name leg -> parts/leg (test.part)",
        ], EntryReferences.In(RecordDraft.From(Sample())).Select(Text));
        // The conditions as the draft has them: another motto names a spare part, or nothing; a pale part a part.
        var draft = RecordDraft.From(Sample());
        var spare = EntryReferences.In(draft.With("Motto", DraftValue.Of("piece")).With("Text", DraftValue.Of("bolt"))).First();
        Assert.Equal("$.text bolt -> spare-parts/bolt (test.part)", Text(spare));
        Assert.DoesNotContain(EntryReferences.In(draft.With("Motto", DraftValue.Of("none"))), r => r.Path.ToString() == "$.text");
        var pale = draft.With(RecordPath.Root.Field("part").Field("shade"), DraftValue.Of(Shade.Pale));
        Assert.Equal("$.part.name Grim -> - (test.part)", Text(EntryReferences.In(pale).Single(r => r.Path.ToString() == "$.part.name")));
        // A name the rule can't store names no path.
        var bad = EntryReferences.In(draft.With(RecordPath.Root.Field("names").Index(0), DraftValue.Of("a/b"))).First(r => r.Path.ToString() == "$.names[0]");
        Assert.Null(bad.Target);
    }

    /// <summary>
    /// The engine's kinds: through a union's subtype (a cutscene's audio track) by a condition on an enum (the cue's
    /// kind); and records in a map with enum keys, at the key's JSON name.
    /// </summary>
    [Fact]
    public void ThroughUnionsAndEnumKeys()
    {
        var cutscene = Cutscene(Sound("boom"), new Statventure.Scene.AudioCue(1, Statventure.Scene.AudioCueKind.Music, "combat@alder-vale"), new Statventure.Scene.AudioCue(2, Statventure.Scene.AudioCueKind.StopMusic));
        Assert.Equal(
        [
            "$.tracks[0].cues[0].name boom -> audio/effects/boom (audio.effect)",
            "$.tracks[0].cues[1].name combat@alder-vale -> audio/themes/combat.alder-vale (audio.theme)",
        ], EntryReferences.In(RecordDraft.From(cutscene)).Select(Text));
        var shades = new Records.Variants.PartsByShade { Parts = new() { [Shade.Pale] = new() { Name = "arm" }, [Shade.Golden] = new() { Name = "Grim", Shade = Shade.Golden } } };
        Assert.Equal(
        [
            "$.parts[\"pale\"].name arm -> parts/arm (test.part)",
            "$.parts[\"golden\"].name Grim -> creatures/_grim (test.creature)",
        ], EntryReferences.In(RecordDraft.From(shades)).Select(Text));
    }

    /// <summary>A draft that doesn't build is read too (its references are where the form shows them).</summary>
    [Fact]
    public void ADraftThatDoesntBuildIsReadToo()
    {
        var draft = RecordDraft.From(Sample()).With("Part", DraftValue.Missing);
        Assert.False(draft.Build().Built);
        Assert.Contains(EntryReferences.In(draft), r => r.Path.ToString() == "$.names[1]");
        Assert.DoesNotContain(EntryReferences.In(draft), r => r.Path.ToString() == "$.part.name");
    }

    /// <summary>
    /// What a form's text field at a row's path names: a value or (as asked) a map's key; a required name not given yet
    /// (its name empty, naming nothing yet); nothing where no mark holds.
    /// </summary>
    [Fact]
    public void TheReferenceAtATextFieldsPath()
    {
        var draft = RecordDraft.From(Sample());
        Assert.Equal("test.creature", EntryReferences.At(draft, "$.names[1]", key: false)!.Reference.Kind.Name);
        Assert.Equal("test.part", EntryReferences.At(draft, "$.pairs[\"arm\"]", key: true)!.Reference.Kind.Name);
        Assert.Null(EntryReferences.At(draft, "$.pairs[\"arm\"]", key: false));
        Assert.Null(EntryReferences.At(draft, "$.names[1]", key: true));
        Assert.Null(EntryReferences.At(draft, "$.motto", key: false));
        Assert.Null(EntryReferences.At(draft, "$.byNumber[\"2\"]", key: false));
        var missing = draft.With("Text", DraftValue.Missing);
        var at = EntryReferences.At(missing, "$.text", key: false)!;
        Assert.Equal(("", (string?)null, "test.creature"), (at.Name, at.Target, at.Reference.Kind.Name));
        Assert.DoesNotContain(EntryReferences.In(missing), r => r.Path.ToString() == "$.text");
    }

    /// <summary>
    /// Names pointed at another entry: a string written again, a map's key replaced in its place (the order kept); a
    /// name whose mark's rule can't name the new path is left and counted.
    /// </summary>
    [Fact]
    public void NamesArePointedAtAnotherEntry()
    {
        var draft = RecordDraft.From(Sample());
        var arm = EntryReferences.In(draft).Where(r => r.Target == "parts/arm").ToList();
        Assert.Equal(2, arm.Count);
        var (moved, left) = EntryReferences.PointAt(draft, arm, "parts/limb");
        Assert.Equal(0, left);
        var built = (Kinds)moved.Build().Value!;
        Assert.Equal([new("limb", "hand"), new("leg", "foot")], built.Pairs);
        Assert.Equal("limb", built.MaybePart!.Name);
        Assert.Equal(Sample().Names, built.Names);
        var grim = EntryReferences.In(draft).Where(r => r.Target == "creatures/_grim").ToList();
        (moved, left) = EntryReferences.PointAt(draft, grim, "creatures/_gob");
        Assert.Equal(0, left);
        built = (Kinds)moved.Build().Value!;
        Assert.Equal(("Gob", "Gob", "Gob"), (built.Text, built.Names[0], built.Part.Name));
        // The creatures' rule can't name a path outside its folder: left as they were.
        (moved, left) = EntryReferences.PointAt(draft, grim, "parts/gob");
        Assert.Equal(3, left);
        Assert.Same(draft, moved);
    }

    /// <summary>The entries a mark can name: the session's records of its kind its rule names (the test rule takes folders below its own), by name; other kinds, files and paths it can't name left out.</summary>
    [Fact]
    public void TheEntriesAMarkCanName()
    {
        using var editor = FormTesting.Open([
            FormTesting.Entry("creatures/_grim", new Creature { Name = "Grim" }),
            FormTesting.Entry("creatures/_bold", new Creature { Name = "Bold" }, EntrySource.Game),
            FormTesting.Entry("creatures/old/_gob", new Creature { Name = "Gob" }),
            FormTesting.Entry("beasts/_gob", new Creature { Name = "Gob" }),
            FormTesting.Entry("creatures/_imp", new Part { Name = "imp" }),
            EditorEntry.ForFile("creatures/notes", 3, EntrySource.Pack),
        ]);
        var session = editor.Session;
        var reference = RecordShape.Of(typeof(Kinds)).Field("names")!.References[0];
        Assert.Equal([("Bold", "creatures/_bold"), ("Grim", "creatures/_grim"), ("old/Gob", "creatures/old/_gob")],
            EntryReferences.Candidates(session, reference).Select(c => (c.Name, c.Entry.Path)));
        // The session's lookup: a record's kind; nothing for a file or a path without an entry.
        var lookup = new SessionReferences(session);
        Assert.Equal("test.creature", lookup.KindAt("creatures/_bold"));
        Assert.Equal("test.part", lookup.KindAt("creatures/_imp"));
        Assert.Null(lookup.KindAt("creatures/notes"));
        Assert.Null(lookup.KindAt("creatures/_none"));
    }

    /// <summary>
    /// The entries naming one (P19): the pack's own, new ones and the mod's copies, each with its references to it; the
    /// game's own entries aren't (a mod can't change them); a reference to another kind at the same path isn't one.
    /// </summary>
    [Fact]
    public void TheEntriesNamingOne()
    {
        using var editor = FormTesting.Open([
            FormTesting.Entry("parts/arm", new Part { Name = "arm" }),
            FormTesting.Entry("parts/leg", new Part { Name = "arm" }),
            FormTesting.Entry("parts/wing", new Part { Name = "arm" }, EntrySource.Game),
            FormTesting.Entry("parts/tail", new Part { Name = "arm" }, EntrySource.Game),
            FormTesting.Entry("kinds/one", Sample()),
            EditorEntry.ForFile("parts/notes", 4, EntrySource.Pack),
        ]);
        var session = editor.Session;
        var tail = session.At("parts/tail");
        session.Change(tail, "Weight", 3.0);
        Assert.True(session.IsModCopy(tail));
        var made = session.Add("parts/new", RecordDraft.From(new Part { Name = "arm" }));
        var referrers = EntryReferences.To(session, session.At("parts/arm"));
        Assert.Equal(["kinds/one", "parts/arm", "parts/leg", "parts/new", "parts/tail"], referrers.Select(r => r.Entry.Path));
        Assert.Equal(["$.pairs[\"arm\"]", "$.maybePart.name"], referrers[0].References.Select(r => r.Path.ToString()));
        Assert.True(EntryReferences.IsThePacks(session, made));
        Assert.False(EntryReferences.IsThePacks(session, session.At("parts/wing")));
        Assert.Empty(EntryReferences.To(session, session.At("kinds/one")));
        // A name of another kind pointing at an entry's path isn't a reference to it: a part at creatures/_x, a creature "X" named.
        var x = session.Add("creatures/_x", RecordDraft.From(new Part { Name = "x" }));
        session.Add("kinds/two", RecordDraft.From(new Kinds { Text = "x", Part = new Part { Name = "x" }, Names = ["X"] }));
        Assert.Contains(EntryReferences.In(session.At("kinds/two").Draft!), r => r.Target == "creatures/_x");
        Assert.Empty(EntryReferences.To(session, x));
    }
}
