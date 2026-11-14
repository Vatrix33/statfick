using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;
using Statventure.Tests.Records.Variants;
using Statventure.UI.Css;

namespace Statventure.Tests.Records;

/// <summary>
/// The checks of a record don't walk what can't report anything (GM1 step 6c; <see cref="RecordShape.ChecksAnything"/>,
/// <see cref="ValueShape.ChecksBelow"/>), and every problem is the one the walk into every record gives
/// (<see cref="RecordChecks.CheckEveryRecord"/>, the walk as it was), path for path, word for word and in the same order:
/// over every record of the committed packs, alone and with the data and a lookup, and over shapes of the tests' own whose
/// checks sit only deep down (in a list of maps of unions, in one arm of a union only, in a recursive shape, through a
/// nullable and through pairs, under a union with a check of its own, under a type that isn't sealed).
/// </summary>
public class RecordChecksPruningTests
{
    sealed class Lookup(Dictionary<string, string> kinds) : IReferenceLookup
    {
        public string? KindAt(string path) => kinds.GetValueOrDefault(path);
    }

    /// <summary>The problems of both walks, which must be the same; the pruned walk's.</summary>
    static IReadOnlyList<string> Same(object record, VirtualFileSystem? data = null, IReferenceLookup? references = null)
    {
        var every = RecordChecks.CheckEveryRecord(record, data, references);
        var pruned = RecordChecks.Check(record, data, references);
        Assert.True(every.Select(p => (p.Path, p.Message)).SequenceEqual(pruned.Select(p => (p.Path, p.Message))),
            $"{record.GetType().Name}: every record [{string.Join(" | ", every)}] but pruned [{string.Join(" | ", pruned)}]");
        return [.. pruned.Select(p => p.ToString())];
    }

    [Fact]
    public void EveryCommittedRecordGetsTheSameProblems()
    {
        using var data = new VirtualFileSystem(ModCheckTesting.Game);
        var records = new List<(string Path, object Value)>();
        var kinds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in data.List())
            if (ModCheck.GameRecord(data, path, RecordTypes.Engine) is { } value)
            {
                records.Add((path, value));
                kinds[path] = RecordTypes.NameOf(value.GetType());
            }
        Assert.True(records.Count > 120, $"{records.Count} records");
        var lookup = new Lookup(kinds);
        foreach (var (path, value) in records)
        {
            Same(value);
            Same(value, data, lookup);
        }
        // What the walk passes by: the stylesheet's rules, declarations and tokens (its own check reads them itself).
        var sheet = RecordShape.Of<StylesheetRecord>();
        Assert.True(sheet.ChecksAnything);
        Assert.False(sheet.Field("rules")!.Value.ChecksBelow);
        Assert.False(RecordShape.Of<CssTokenRecord>().ChecksAnything);
        Assert.False(RecordShape.Of<StyleRuleRecord>().ChecksAnything);
    }

    static PruneTree Tree(PruneLeaf? leaf, params PruneTree[] children) => new() { Leaf = leaf, Children = children };

    [Fact]
    public void ChecksDeepDownAreFoundAsTheWalkIntoEverythingFindsThem()
    {
        var quiet = new PruneQuiet { Name = "q", Below = [new PruneQuiet { Below = [new PruneQuiet()] }] };
        var deep = new PruneDeep
        {
            Groups =
            [
                new Dictionary<string, PruneArm> { ["a"] = new PruneQuietArm { Text = "x" }, ["b"] = new PruneLoudArm { Level = 2 } },
                new Dictionary<string, PruneArm> { ["c"] = new PruneLoudArm { Level = 0.5 }, ["d"] = new PruneUnlistedArm { Note = "n" } },
            ],
            Pairs =
            [
                new("p", Tree(null, Tree(null, Tree(new PruneLeaf { Limited = new() { Amount = 2 } })))),
                new("q", Tree(new PruneLeaf { Chosen = new() { Colour = "green" }, Naming = new() { Names = "nobody" } })),
            ],
            Checked = new PruneCheckedArm(),
            Open = new PruneOpenChild { Name = "kid" },
            Quiet = [quiet],
        };
        var lookup = new Lookup(new() { ["prune/quiet/someone"] = "variant.prune-quiet" });
        Assert.Equal(
            [
                "too loud (2) (at $.groups[0][\"b\"].level)",
                "unlisted: n (at $.groups[1][\"d\"])",
                "must be 0 or more and at most 1 (it is 2) (at $.pairs[\"p\"].children[0].children[0].leaf.limited.amount)",
                "\"green\" isn't one of the choices: red, blue (at $.pairs[\"q\"].leaf.chosen.colour)",
                "no tag (at $.checked)",
                "the child kid (at $.open)",
            ],
            Same(deep));
        // With a lookup: the reference deep down too.
        Assert.Contains("no prune quiet \"nobody\" (prune/quiet/nobody) (at $.pairs[\"q\"].leaf.naming.names)", Same(deep, references: lookup));
        // Each alone, as the root.
        Assert.Equal(["must be 0 or more and at most 1 (it is 2) (at $.children[0].children[0].leaf.limited.amount)"],
            Same(Tree(null, Tree(null, Tree(new PruneLeaf { Limited = new() { Amount = 2 } })))));
        Assert.Equal(["too loud (3) (at $.level)"], Same(new PruneLoudArm { Level = 3 }));
        Assert.Equal(["too loud (4) (at $.groups[0][\"b\"].level)", "unlisted: m (at $.groups[1][\"u\"])"],
            Same(new PruneGroups
            {
                Groups =
                [
                    new Dictionary<string, PruneArm> { ["a"] = new PruneQuietArm(), ["b"] = new PruneLoudArm { Level = 4 } },
                    new Dictionary<string, PruneArm> { ["u"] = new PruneUnlistedArm { Note = "m" } },
                ],
            }));
        Assert.Empty(Same(new PruneGroups { Groups = [new Dictionary<string, PruneArm> { ["a"] = new PruneQuietArm() }] }));
        Assert.Equal(["the child kid (at $.item)"], Same(new PruneOpenHolder { Item = new PruneOpenChild { Name = "kid" } }));
        Assert.Empty(Same(new PruneOpenHolder { Item = new PruneOpenBase { Name = "base" } }));
        Assert.Equal(["no tag"], Same(new PruneCheckedArm()));
        Assert.Equal(["the child kid"], Same(new PruneOpenChild { Name = "kid" }));
        Assert.Empty(Same(new PruneDeep { Groups = [new Dictionary<string, PruneArm> { ["a"] = new PruneQuietArm() }], Quiet = [quiet] }));
        Assert.Empty(Same(quiet));
        Assert.Empty(Same(new PruneOpenBase { Name = "base" }));
        // What the shapes say: nothing below a recursive quiet shape; a deep check found through every kind of holder.
        Assert.False(RecordShape.Of<PruneQuiet>().ChecksAnything);
        Assert.False(RecordShape.Of<PruneQuietArm>().ChecksAnything);
        Assert.False(RecordShape.Of<PruneOpenBase>().ChecksAnything);
        Assert.True(RecordShape.Of<PruneTree>().ChecksAnything);
        Assert.True(RecordShape.Of<PruneDeep>().ChecksAnything);
        Assert.True(RecordShape.Of<PruneCheckedArm>().ChecksAnything);
        Assert.False(RecordShape.Of<PruneTree>().Field("quiet")!.Value.ChecksBelow);
        Assert.True(RecordShape.Of<PruneDeep>().Field("open")!.Value.ChecksBelow);
    }

    /// <summary>A mark that doesn't fit below: both walks meet it and throw the same.</summary>
    [Fact]
    public void AShapeThatCantBeReadBelowIsWalkedAndThrowsAsBefore()
    {
        var broken = new PruneBroken { Bad = new LimitsOnText { A = "a" } };
        var every = Assert.Throws<InvalidOperationException>(() => RecordChecks.CheckEveryRecord(broken));
        var pruned = Assert.Throws<InvalidOperationException>(() => RecordChecks.Check(broken));
        Assert.Equal(every.Message, pruned.Message);
        Assert.Equal("LimitsOnText.A: [Limits] is for a field of numbers (or lists or maps of them), not string", pruned.Message);
        // Nothing there: nothing walked into, by either.
        Assert.Empty(Same(new PruneBroken()));
    }
}
