using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Records;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// The session's entries without a document (Docs/Editor.md, "How it works", step 9, "The session's entries"): the
/// set shown (the pack's over the game's), the selection, commits with each entry's own history (M11-7), what
/// counts as changed, the problems, and New, Duplicate, Rename, Remove and Revert with what they report.
/// </summary>
public class SessionEntriesTests
{
    static EditorSession Session(bool writable = true) =>
        new(GameData.Repo(EditorHarness.EngineFolder)) { Types = FormTesting.Types, Writable = writable };

    static Creature Grim => new() { Name = "Grim", Speed = 2 };

    static EditorEntry Record(string path, object record, EntrySource source = EntrySource.Pack, bool readOnly = false) =>
        EditorEntry.ForRecord(path, record, source, readOnly);

    static RecordDraft Faster(EditorEntry entry, double speed) => entry.Draft!.With("speed", DraftValue.Of(speed));

    [Fact]
    public void EntriesShowByPathWithThePacksOverTheGames()
    {
        var session = Session();
        var changes = new List<EntriesChange>();
        session.EntriesChanged += changes.Add;
        var gameB = Record("creatures/b", Grim, EntrySource.Game);
        var packB = Record("creatures/b", Grim with { Speed = 3 });
        var a = Record("creatures/a", Grim, EntrySource.Game);
        var file = EditorEntry.ForFile("ui/fonts/x.ttf", 1234, EntrySource.Game);
        var bad = EditorEntry.ForUnreadable("zz/broken", "a damaged content", EntrySource.Pack);
        session.ShowEntries([gameB, file, packB, a, bad]);
        Assert.Equal(["creatures/a", "creatures/b", "ui/fonts/x.ttf", "zz/broken"], session.Entries.Select(e => e.Path));
        Assert.Same(packB, session.Entry("creatures/b"));
        Assert.Same(gameB, session.Overridden("creatures/b"));
        Assert.Null(session.Overridden("creatures/a"));
        Assert.Null(session.Entry("creatures/c"));
        Assert.Equal(EntriesChangeKind.Shown, Assert.Single(changes).Kind);
        // Files and unreadable entries are shown, never edited.
        Assert.True(file.ReadOnly);
        Assert.Equal("File", file.KindName);
        Assert.Equal(1234, file.FileSize);
        Assert.Null(file.Draft);
        Assert.True(bad.ReadOnly);
        Assert.Equal("Unreadable", bad.KindName);
        Assert.Equal("a damaged content", bad.Reason);
        Assert.False(session.CanEdit(file));
        Assert.False(session.CanEdit(bad));
        Assert.Equal("Creature", a.KindName);
        Assert.Equal("b", packB.Name);
        // The same path twice from one place, a new entry, or a path the rule refuses can't be shown.
        Assert.Throws<ArgumentException>(() => session.ShowEntries([Record("creatures/a", Grim), Record("creatures/a", Grim)]));
        Assert.Throws<ArgumentException>(() => session.ShowEntries([Record("Creatures/A", Grim)]));
        Assert.Throws<ArgumentException>(() => EditorEntry.ForRecord("x", Grim, EntrySource.New));
        Assert.Throws<ArgumentException>(() => EditorEntry.ForFile("x", 1, EntrySource.New));
        Assert.Throws<ArgumentException>(() => EditorEntry.ForUnreadable("x", "why", EntrySource.New));
        var other = Session();
        var made = other.Add("creatures/n", RecordDraft.Blank(typeof(Creature)));
        Assert.Throws<ArgumentException>(() => session.ShowEntries([made]));
    }

    [Fact]
    public void SelectingShowsAnEntryOfTheSessionOrNone()
    {
        var session = Session();
        var a = Record("creatures/a", Grim);
        var b = Record("creatures/b", Grim);
        session.ShowEntries([a, b]);
        var changes = 0;
        session.SelectionChanged += () => changes++;
        session.Select(b);
        Assert.Same(b, session.Selected);
        session.Select(b);
        Assert.Equal(1, changes);
        session.Select(null);
        Assert.Null(session.Selected);
        Assert.Equal(2, changes);
        Assert.Throws<ArgumentException>(() => session.Select(Record("creatures/c", Grim)));
        // Showing a new set clears the selection.
        session.Select(a);
        session.ShowEntries([b]);
        Assert.Null(session.Selected);
    }

    [Fact]
    public void EachEntryKeepsItsOwnHistory()
    {
        var session = Session();
        var a = Record("creatures/a", Grim);
        var b = Record("creatures/b", Grim);
        session.ShowEntries([a, b]);
        var changed = new List<EditorEntry>();
        session.EntryChanged += changed.Add;
        Assert.True(session.Commit(a, Faster(a, 3)));
        Assert.True(session.Commit(a, Faster(a, 4)));
        Assert.True(session.Commit(b, Faster(b, 5)));
        Assert.False(session.Commit(b, b.Draft!));
        Assert.Equal([a, a, b], changed);
        Assert.Equal(3, a.History.Count);
        Assert.Equal(2, a.HistoryPosition);
        Assert.True(session.Undo(a));
        Assert.Equal(3, ((Creature)a.Build!.Value!).Speed);
        // Undo touches only the entry it is given (M11-7).
        Assert.Equal(5, ((Creature)b.Build!.Value!).Speed);
        Assert.True(session.Undo(a));
        Assert.False(session.Undo(a));
        Assert.Same(a.SavedDraft, a.Draft);
        Assert.True(session.Redo(a));
        Assert.True(a.CanRedo);
        // A commit drops what could be redone.
        Assert.True(session.Commit(a, Faster(a, 9)));
        Assert.False(a.CanRedo);
        Assert.False(session.Redo(a));
        Assert.Equal([2.0, 3, 9], a.History.Select(d => (double)((DraftLeaf)d["speed"]).Value));
    }

    [Fact]
    public void OnlyEntriesThatCanBeEditedTakeCommits()
    {
        var session = Session(writable: false);
        var a = Record("creatures/a", Grim);
        var locked = Record("creatures/b", Grim, EntrySource.Game, readOnly: true);
        var game = Record("creatures/c", Grim, EntrySource.Game);
        session.ShowEntries([a, locked, game]);
        // Nothing is editable while the open pack takes no changes.
        Assert.False(session.CanEdit(a));
        Assert.Throws<InvalidOperationException>(() => session.Commit(a, Faster(a, 3)));
        Assert.Throws<InvalidOperationException>(() => session.Undo(a));
        Assert.Throws<InvalidOperationException>(() => session.Redo(a));
        session.Writable = true;
        Assert.True(session.CanEdit(a));
        // A game entry is editable in a mod (its copy, P7) unless it is read-only.
        Assert.True(session.CanEdit(game));
        Assert.False(session.CanEdit(locked));
        Assert.Throws<InvalidOperationException>(() => session.Commit(locked, Faster(locked, 3)));
        Assert.Throws<ArgumentException>(() => session.Commit(Record("creatures/x", Grim), Faster(a, 3)));
        // A draft of another kind isn't the entry's.
        Assert.Throws<ArgumentException>(() => session.Commit(a, RecordDraft.Blank(typeof(Part))));
    }

    [Fact]
    public void AnEntryIsChangedWhenItsRecordDiffersFromTheSavedOne()
    {
        var session = Session();
        var a = Record("creatures/a", Grim);
        var b = Record("creatures/b", Grim);
        session.ShowEntries([a, b]);
        var saved = a.Draft;
        Assert.False(a.IsChanged);
        Assert.False(session.HasUnsavedEntries);
        Assert.False(session.HasUnsavedWork);
        session.Commit(a, Faster(a, 3));
        Assert.True(a.IsChanged);
        Assert.True(session.HasUnsavedEntries);
        Assert.True(session.HasUnsavedWork);
        // An edit taken back by another isn't a change.
        session.Commit(a, Faster(a, 2));
        Assert.NotSame(saved, a.Draft);
        Assert.False(a.IsChanged);
        Assert.False(session.HasUnsavedWork);
        // Nor is the saved draft reached by undo; a draft that doesn't build is.
        session.Commit(a, a.Draft!.With("name", DraftValue.Missing));
        Assert.True(a.IsChanged);
        session.Undo(a);
        session.Undo(a);
        session.Undo(a);
        Assert.Same(saved, a.Draft);
        Assert.False(a.IsChanged);
        Assert.False(b.IsChanged);
    }

    [Fact]
    public void ProblemsAreTheBuildsWhileItDoesntBuildElseTheChecks()
    {
        var session = Session();
        var a = Record("creatures/a", Grim);
        session.ShowEntries([a]);
        Assert.True(a.Builds);
        Assert.Empty(a.Problems);
        var built = a.LastBuilt;
        session.Commit(a, a.Draft!.With("name", DraftValue.Missing));
        Assert.False(a.Builds);
        Assert.Equal(["needs a value (at $.name)"], a.Problems.Select(p => p.ToString()));
        // The last value that built stays (the JSON tab and the preview show it).
        Assert.Same(built, a.LastBuilt);
        session.Undo(a);
        var checks = 0;
        session.AddCheck((entry, value) =>
        {
            checks++;
            return ((Creature)value).Speed > 1 ? [new RecordProblem(RecordPath.Root.Field("speed"), $"{entry.Path} is too fast")] : [];
        });
        Assert.Equal(["creatures/a is too fast (at $.speed)"], a.Problems.Select(p => p.ToString()));
        session.Commit(a, Faster(a, 1));
        Assert.Empty(a.Problems);
        Assert.NotSame(built, a.LastBuilt);
        // Checks run on drafts that build only.
        var before = checks;
        session.Commit(a, a.Draft!.With("name", DraftValue.Missing));
        Assert.Equal(before, checks);
        Assert.Equal(["needs a value (at $.name)"], a.Problems.Select(p => p.ToString()));
        var reported = new List<EditorEntry>();
        session.EntryChanged += reported.Add;
        session.Recheck(a);
        Assert.Equal([a], reported);
    }

    [Fact]
    public void PathProblemsAreTheRuleATakenPathAndFolderClashes()
    {
        var session = Session();
        var a = Record("creatures/a", Grim);
        session.ShowEntries([a, Record("creatures/b", Grim)]);
        Assert.Null(session.PathProblem("creatures/c"));
        Assert.Equal("there is already an entry at creatures/b", session.PathProblem("creatures/b"));
        Assert.Null(session.PathProblem("creatures/a", a));
        Assert.Equal("creatures is the folder of creatures/a", session.PathProblem("creatures"));
        Assert.Equal("creatures/a is an entry, so it can't be a folder", session.PathProblem("creatures/a/c"));
        Assert.NotNull(session.PathProblem("Creatures/C"));
        Assert.NotNull(session.PathProblem(""));
    }

    [Fact]
    public void NewAndDuplicateAddNewEntries()
    {
        var session = Session();
        var a = Record("creatures/a", Grim);
        session.ShowEntries([a]);
        var changes = new List<EntriesChange>();
        session.EntriesChanged += changes.Add;
        var blank = session.Add("creatures/n", RecordDraft.Blank(typeof(Creature)));
        Assert.Equal(EntrySource.New, blank.Source);
        Assert.Null(blank.SavedPath);
        Assert.True(blank.IsChanged);
        Assert.Equal(["needs a value (at $.name)"], blank.Problems.Select(p => p.ToString()));
        Assert.Equal(["creatures/a", "creatures/n"], session.Entries.Select(e => e.Path));
        Assert.True(session.HasUnsavedWork);
        // A copy of the entry's draft as it is now, built or not.
        session.Commit(a, Faster(a, 7));
        var copy = session.Duplicate(a, "creatures/b");
        Assert.Same(a.Draft, copy.Draft);
        Assert.Equal(7, ((Creature)copy.Build!.Value!).Speed);
        Assert.False(copy.CanUndo);
        Assert.Equal(["creatures/a", "creatures/b", "creatures/n"], session.Entries.Select(e => e.Path));
        Assert.Equal([(EntriesChangeKind.Added, blank), (EntriesChangeKind.Added, copy)], changes.Select(c => (c.Kind, c.Entry!)));
        Assert.Throws<ArgumentException>(() => session.Add("creatures/a", RecordDraft.Blank(typeof(Creature))));
        Assert.Throws<InvalidOperationException>(() => session.Duplicate(EditorEntry.ForFile("f", 1, EntrySource.Pack), "f2"));
        session.Writable = false;
        Assert.False(session.CanAdd);
        Assert.Throws<InvalidOperationException>(() => session.Add("creatures/z", RecordDraft.Blank(typeof(Creature))));
    }

    [Fact]
    public void RenameMovesAnEntryWithItsHistory()
    {
        var session = Session();
        var game = Record("creatures/b", Grim, EntrySource.Game);
        var copy = Record("creatures/b", Grim with { Speed = 4 });
        var a = Record("creatures/a", Grim);
        session.ShowEntries([a, game, copy, Record("creatures/g", Grim, EntrySource.Game)]);
        session.Commit(a, Faster(a, 3));
        var changes = new List<EntriesChange>();
        session.EntriesChanged += changes.Add;
        session.Rename(a, "creatures/z");
        Assert.Equal("creatures/z", a.Path);
        Assert.Equal("creatures/a", a.SavedPath);
        Assert.True(a.IsRenamed);
        Assert.True(a.CanUndo);
        Assert.Equal(["creatures/b", "creatures/g", "creatures/z"], session.Entries.Select(e => e.Path));
        Assert.Equal((EntriesChangeKind.Renamed, a, "creatures/a"), (changes[0].Kind, changes[0].Entry, changes[0].OldPath));
        // The pack's copy renamed: the game's entry shows again at its path.
        session.Rename(copy, "creatures/c");
        Assert.Same(game, session.Entry("creatures/b"));
        Assert.Equal((EntriesChangeKind.Added, game), (changes[^1].Kind, changes[^1].Entry));
        // Game entries can't be renamed (P7); a path taken or refused can't be the new one.
        Assert.False(session.CanRenameOrRemove(game));
        Assert.Throws<InvalidOperationException>(() => session.Rename(game, "creatures/q"));
        Assert.Throws<ArgumentException>(() => session.Rename(a, "creatures/b"));
        Assert.Throws<ArgumentException>(() => session.Rename(a, "Creatures/Q"));
        // A new entry renamed has no saved path to keep.
        var made = session.Add("creatures/m", RecordDraft.Blank(typeof(Creature)));
        session.Rename(made, "creatures/n");
        Assert.False(made.IsRenamed);
        session.Rename(made, "creatures/n");
        // A rename alone, of an entry not changed, is unsaved work.
        var still = Record("creatures/s", Grim);
        session.ShowEntries([still]);
        Assert.False(session.HasUnsavedEntries);
        session.Rename(still, "creatures/t");
        Assert.False(still.IsChanged);
        Assert.True(session.HasUnsavedEntries);
        session.Writable = false;
        Assert.False(session.CanRenameOrRemove(a));
    }

    [Fact]
    public void RemoveTakesAnEntryOutAndListsThePacksForSaving()
    {
        var session = Session();
        var game = Record("creatures/b", Grim, EntrySource.Game);
        var copy = Record("creatures/b", Grim with { Speed = 4 });
        var a = Record("creatures/a", Grim);
        session.ShowEntries([a, game, copy]);
        var made = session.Add("creatures/n", RecordDraft.Blank(typeof(Creature)));
        session.Select(a);
        var changes = new List<EntriesChange>();
        session.EntriesChanged += changes.Add;
        session.Remove(a);
        Assert.Null(session.Selected);
        Assert.Equal([a], session.Removed);
        Assert.True(session.HasUnsavedWork);
        Assert.Equal((EntriesChangeKind.Removed, a, "creatures/a"), (changes[0].Kind, changes[0].Entry, changes[0].OldPath));
        // A new entry just goes; the pack's copy goes and the game's shows again.
        session.Remove(made);
        Assert.Equal([a], session.Removed);
        session.Remove(copy);
        Assert.Equal([a, copy], session.Removed);
        Assert.Same(game, session.Entry("creatures/b"));
        Assert.Throws<InvalidOperationException>(() => session.Remove(game));
        Assert.Equal(["creatures/b"], session.Entries.Select(e => e.Path));
        // A removal alone is unsaved work; a new set shown starts with none removed.
        var plain = Record("creatures/p", Grim);
        session.ShowEntries([plain]);
        Assert.Empty(session.Removed);
        Assert.False(session.HasUnsavedEntries);
        session.Remove(plain);
        Assert.True(session.HasUnsavedEntries);
        Assert.True(session.HasUnsavedWork);
    }

    [Fact]
    public void AGameEntryShownAgainHasTheChecksProblems()
    {
        var session = Session();
        var game = Record("creatures/b", Grim, EntrySource.Game);
        var copy = Record("creatures/b", Grim with { Speed = 4 });
        session.ShowEntries([game, copy]);
        session.AddCheck((entry, _) => [new RecordProblem(RecordPath.Root, $"{entry.Source} is checked")]);
        Assert.Equal(["Pack is checked"], copy.Problems.Select(p => p.Message));
        session.Remove(copy);
        Assert.Equal(["Game is checked"], game.Problems.Select(p => p.Message));
    }

    [Fact]
    public void RevertDropsTheModsCopyOfAGameEntry()
    {
        var session = Session();
        var game = Record("creatures/b", Grim, EntrySource.Game);
        var copy = Record("creatures/b", Grim with { Speed = 4 });
        var edited = Record("creatures/c", Grim, EntrySource.Game);
        var own = Record("creatures/a", Grim);
        session.ShowEntries([own, game, copy, edited]);
        // A copy: the pack's entry over the game's, or a game entry changed in this session.
        Assert.True(session.IsModCopy(copy));
        Assert.False(session.IsModCopy(edited));
        Assert.False(session.IsModCopy(own));
        Assert.False(session.IsModCopy(game));
        session.Commit(edited, Faster(edited, 6));
        session.Commit(edited, Faster(edited, 7));
        Assert.True(session.IsModCopy(edited));
        Assert.False(session.CanRevert(own));
        Assert.Throws<InvalidOperationException>(() => session.Revert(own));
        // A changed game entry goes back to the game's draft, its history cleared.
        var changed = new List<EditorEntry>();
        session.EntryChanged += changed.Add;
        Assert.Same(edited, session.Revert(edited));
        Assert.Same(edited.SavedDraft, edited.Draft);
        Assert.Single(edited.History);
        Assert.False(edited.CanUndo);
        Assert.False(edited.CanRedo);
        Assert.Equal([edited], changed);
        Assert.False(session.IsModCopy(edited));
        // The pack's copy goes (for saving) and the game's shows again, selected if the copy was, its history cleared.
        session.Commit(copy, Faster(copy, 8));
        session.Select(copy);
        var changes = new List<EntriesChange>();
        session.EntriesChanged += changes.Add;
        Assert.Same(game, session.Revert(copy));
        Assert.Same(game, session.Entry("creatures/b"));
        Assert.Same(game, session.Selected);
        Assert.Equal([copy], session.Removed);
        Assert.Equal([(EntriesChangeKind.Removed, copy), (EntriesChangeKind.Added, game)], changes.Select(c => (c.Kind, c.Entry!)));
        Assert.Single(game.History);
        Assert.Null(session.Overridden("creatures/b"));
        Assert.False(session.IsModCopy(game));
        // Not while the pack takes no changes.
        session.Commit(game, Faster(game, 9));
        session.Writable = false;
        Assert.True(session.IsModCopy(game));
        Assert.False(session.CanRevert(game));
    }

    [Fact]
    public void NewOffersEveryPlainKindByName()
    {
        var session = Session();
        var kinds = session.NewKinds();
        Assert.All(kinds, k => Assert.False(k.IsUnion));
        Assert.Equal(kinds.Select(k => k.DisplayName).Order(StringComparer.Ordinal), kinds.Select(k => k.DisplayName));
        Assert.Contains(kinds, k => k.Name == "test.creature");
        Assert.Contains(kinds, k => k.Name == "audio.effect");
        Assert.Contains(kinds, k => k.Name == "test.circle");
        Assert.DoesNotContain(kinds, k => k.Name == "test.shape");
        Assert.Equal(FormTesting.Types.ByName.Values.Count(t => !RecordShape.Of(t).IsUnion), kinds.Count);
    }

    [Fact]
    public void NewLeavesOutAKindMarkedNotAPackKind()
    {
        // The save (save.game) is marked so: its values are save slots and character files, never entries (the user's
        // Q26 of GM1). Two kinds alike but for the mark: the one marked isn't offered, the other is.
        var types = new Dictionary<string, Type>(FormTesting.Types.ByName)
        {
            ["variant.not-a-pack-kind"] = typeof(Statventure.Tests.Records.Variants.NotAPackKindVariant),
            ["variant.a-pack-kind"] = typeof(Statventure.Tests.Records.Variants.APackKindVariant),
        };
        var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { Types = new Statventure.Packs.RecordTypes(types), Writable = true };
        var kinds = session.NewKinds();
        Assert.Contains(kinds, k => k.Name == "variant.a-pack-kind");
        Assert.DoesNotContain(kinds, k => k.Name == "variant.not-a-pack-kind");
        Assert.Equal(types.Count(t => !RecordShape.Of(t.Value).IsUnion) - 1, kinds.Count);
        Assert.Equal((false, true), (RecordShape.Of<Statventure.Tests.Records.Variants.NotAPackKindVariant>().IsPackKind,
            RecordShape.Of<Statventure.Tests.Records.Variants.APackKindVariant>().IsPackKind));
    }
}
