using Statventure.Editor.References;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Records;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Checks.CheckTesting;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// Renaming or removing an entry lists the pack's entries that refer to it and offers to update them in the same step
/// (M11-11, P19; the manager's decision Q-A, 2026-11-07): a rename's "Update them to the new name", ticked; a removal's
/// "Point them at:" another entry of the kind, "Leave them" by default; left ones show their problems once the checks
/// across entries run (P18).
/// </summary>
public class ReferrersTests
{
    /// <summary>
    /// A part arm named by a part, a kinds entry (a map's key and a nested part), itself, and the game's tail (not listed);
    /// a part the parts' mark can't name (in spare-parts) and a creature in the parts' folder, neither offered.
    /// </summary>
    static EditorHarness Open() => FormTesting.Open([
        FormTesting.Entry("parts/arm", new Part { Name = "arm" }),
        FormTesting.Entry("parts/leg", new Part { Name = "arm" }),
        FormTesting.Entry("parts/wing", new Part { Name = "leg" }),
        FormTesting.Entry("spare-parts/bolt", new Part { Name = "x" }),
        FormTesting.Entry("parts/beast", new Creature { Name = "Beast" }),
        FormTesting.Entry("parts/tail", new Part { Name = "arm" }, EntrySource.Game),
        FormTesting.Entry("kinds/one", new Kinds { Text = "x", Part = new Part { Name = "leg" }, MaybePart = new Part { Name = "arm" }, Pairs = [new("leg", "1"), new("arm", "2")] }),
    ]);

    static UiElement? Referrers(EditorHarness editor) =>
        editor.View.Dialogs.Top!.Content.Descendants().FirstOrDefault(e => e.HasClass("referrers"));

    static string[] Listed(EditorHarness editor) =>
        [.. Referrers(editor)!.Descendants().Where(e => e.Tag == "li").Select(e => e.TextContent)];

    static string Name(EditorHarness editor, string path) => ((Part)editor.Session.At(path).Build!.Value!).Name;

    [Fact]
    public void ARenameUpdatesTheReferrersInTheSameStep()
    {
        using var editor = Open();
        var arm = editor.Session.At("parts/arm");
        var renamed = editor.Ask(a => a.Rename(arm));
        Assert.Equal("3 entries refer to it:", Referrers(editor)!.Descendants().First(e => e.Tag == "p").TextContent);
        Assert.Equal(["kinds/one", "parts/arm", "parts/leg"], Listed(editor));
        var update = Referrers(editor)!.Descendants().First(e => e.Tag == "input");
        Assert.True(update.Checked);
        Assert.Contains("Update them to the new name", Referrers(editor)!.TextContent);
        var leg = editor.Session.At("parts/leg");
        var steps = leg.History.Count;
        editor.Enter(editor.DialogField("input"), "limb");
        Assert.True(editor.Done(renamed));
        Assert.Equal("parts/limb", arm.Path);
        Assert.Equal("limb", Name(editor, "parts/leg"));
        Assert.Equal("limb", Name(editor, "parts/limb"));
        var one = (Kinds)editor.Session.At("kinds/one").Build!.Value!;
        Assert.Equal(("limb", "leg"), (one.MaybePart!.Name, one.Part.Name));
        Assert.Equal([new("leg", "1"), new("limb", "2")], one.Pairs);
        // The game's entry isn't changed; each referrer's update is one step of its own history.
        Assert.Equal("arm", Name(editor, "parts/tail"));
        Assert.Equal(steps + 1, leg.History.Count);
        Assert.Equal("Pointed 3 entries that referred to parts/arm at parts/limb", editor.View.Status);
        editor.Session.Undo(leg);
        Assert.Equal("arm", Name(editor, "parts/leg"));
    }

    /// <summary>Unticked, the referrers are left as they were, and show their problems once the checks across entries run.</summary>
    [Fact]
    public void ARenameCanLeaveThem()
    {
        using var editor = Open();
        var arm = editor.Session.At("parts/arm");
        var renamed = editor.Ask(a => a.Rename(arm));
        editor.Press(Referrers(editor)!.Descendants().First(e => e.Tag == "input"));
        editor.Press(editor.DialogField("input"));
        editor.Enter(editor.DialogField("input"), "limb");
        Assert.True(editor.Done(renamed));
        Assert.Equal("arm", Name(editor, "parts/leg"));
        Assert.Equal("arm", Name(editor, "parts/limb"));
        editor.Pause();
        Assert.Equal(["no body part \"arm\" (parts/arm) (at $.name)"], editor.Session.At("parts/leg").Shown());
        // The game's tail names arm too: shown, though not listed (a mod can't change it).
        Assert.Equal(["no body part \"arm\" (parts/arm) (at $.name)"], editor.Session.At("parts/tail").Shown());
    }

    /// <summary>A removal offers "Point them at:" the kind's other entries, "Leave them" first and chosen.</summary>
    [Fact]
    public void ARemovalLeavesThemOrPointsThemAtAnother()
    {
        using var editor = Open();
        var arm = editor.Session.At("parts/arm");
        var removed = editor.Ask(a => a.Remove(arm));
        // It isn't its own referrer once removed.
        Assert.Equal(["kinds/one", "parts/leg"], Listed(editor));
        var select = Referrers(editor)!.Descendants().First(e => e.Tag == "select");
        Assert.Equal(["Leave them", "leg", "tail", "wing"], select.Options.Select(o => o.TextContent));
        Assert.Equal(0, select.SelectedIndex);
        var status = editor.View.Status;
        editor.Press(editor.DialogButton("Remove"));
        Assert.True(editor.Done(removed));
        Assert.Equal("arm", Name(editor, "parts/leg"));
        // Left: nothing pointed anywhere, nothing said of it.
        Assert.Equal(status, editor.View.Status);
        // Another: pointed at it.
        var leg = editor.Session.At("parts/leg");
        var wing = editor.Session.At("parts/wing");
        removed = editor.Ask(a => a.Remove(leg));
        Assert.Equal(["kinds/one", "parts/wing"], Listed(editor));
        select = Referrers(editor)!.Descendants().First(e => e.Tag == "select");
        editor.Choose(select, "tail");
        editor.Press(editor.DialogButton("Remove"));
        Assert.True(editor.Done(removed));
        Assert.Equal("tail", Name(editor, "parts/wing"));
        var one = (Kinds)editor.Session.At("kinds/one").Build!.Value!;
        Assert.Equal("tail", one.Part.Name);
        Assert.Equal([new("tail", "1"), new("arm", "2")], one.Pairs);
        Assert.Equal("Pointed 2 entries that referred to parts/leg at parts/tail", editor.View.Status);
        Assert.Equal(EntrySource.Pack, wing.Source);
    }

    /// <summary>Only a rename and a removal list the referrers (a duplicate doesn't change what they name).</summary>
    [Fact]
    public void ADuplicateListsNone()
    {
        using var editor = Open();
        var made = editor.Ask(a => a.Duplicate(editor.Session.At("parts/arm")));
        Assert.Null(Referrers(editor));
        editor.Press(editor.DialogButton("Cancel"));
        Assert.Null(editor.Done(made));
    }

    /// <summary>Cancelled: nothing changes; an entry no one names gets no list; a name its mark can't give the new path is left and counted.</summary>
    [Fact]
    public void CancelledNoneAndNamesThatCantFollow()
    {
        using var editor = FormTesting.Open([
            FormTesting.Entry("spare-parts/bolt", new Part { Name = "x" }),
        FormTesting.Entry("parts/beast", new Creature { Name = "Beast" }),
            FormTesting.Entry("kinds/one", new Kinds { Text = "bolt", Motto = "part", Part = new Part { Name = "x" } }),
            FormTesting.Entry("parts/bolt", new Part { Name = "y" }),
        ]);
        var bolt = editor.Session.At("spare-parts/bolt");
        var renamed = editor.Ask(a => a.Rename(bolt));
        Assert.Equal(["kinds/one"], Listed(editor));
        Assert.Equal("1 entry refers to it:", Referrers(editor)!.Descendants().First(e => e.Tag == "p").TextContent);
        editor.Press(editor.DialogButton("Cancel"));
        Assert.False(editor.Done(renamed));
        Assert.Equal("bolt", ((Kinds)editor.Session.At("kinds/one").Build!.Value!).Text);
        // No referrer: nothing added.
        var removed = editor.Ask(a => a.Remove(editor.Session.At("parts/bolt")));
        Assert.Null(Referrers(editor));
        editor.Press(editor.DialogButton("Cancel"));
        Assert.False(editor.Done(removed));
        // Out of the spare parts' folder: the text's mark can't name it.
        renamed = editor.Ask(a => a.Rename(bolt));
        editor.Enter(editor.DialogField("input"), "parts/nut");
        Assert.True(editor.Done(renamed));
        Assert.Equal("bolt", ((Kinds)editor.Session.At("kinds/one").Build!.Value!).Text);
        Assert.Equal("Pointed 0 entries that referred to spare-parts/bolt at parts/nut; 1 name can't name it and stay as they were", editor.View.Status);
    }

    /// <summary>
    /// More than <see cref="ReferrersPart.Listed"/> referrers: the first named, the rest counted; a referrer that can't be
    /// edited (the pack read-only meanwhile) isn't changed.
    /// </summary>
    [Fact]
    public void ManyReferrersAreCounted()
    {
        var entries = new List<EditorEntry> { FormTesting.Entry("parts/arm", new Part { Name = "x" }) };
        for (var i = 0; i < ReferrersPart.Listed + 3; i++)
            entries.Add(FormTesting.Entry($"parts/p{i:00}", new Part { Name = "arm" }));
        using var editor = FormTesting.Open(entries);
        Assert.Equal(8, ReferrersPart.Listed);
        var renamed = editor.Ask(a => a.Rename(editor.Session.At("parts/arm")));
        var listed = Listed(editor);
        Assert.Equal(ReferrersPart.Listed + 1, listed.Length);
        Assert.Equal("parts/p00", listed[0]);
        Assert.Equal("and 3 more", listed[^1]);
        Assert.Equal($"{ReferrersPart.Listed + 3} entries refer to it:", Referrers(editor)!.Descendants().First(e => e.Tag == "p").TextContent);
        editor.Enter(editor.DialogField("input"), "limb");
        Assert.True(editor.Done(renamed));
        Assert.All(editor.Session.Entries.Where(e => e.Path.StartsWith("parts/p", StringComparison.Ordinal)), e => Assert.Equal("limb", ((Part)e.Build!.Value!).Name));
    }
}
