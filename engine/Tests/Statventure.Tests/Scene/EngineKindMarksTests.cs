using System.Text;
using Statventure.Animation;
using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Animation;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using Statventure.Tests.Ui;
using Statventure.UI.Css;

namespace Statventure.Tests.Scene;

/// <summary>
/// The marks and checks of the engine's scene, model, particle, terrain, state machine and stylesheet kinds
/// (M11 step 6; Docs/Editor.md, "The engine's kinds: checks and marks"): every kind's names, folders, help
/// texts, units, limits, colours, bulk fields and references as recorded in Data/Records/Marks (a changed
/// mark fails until the file is written again, as a layout does); every declared limit one the kind's own
/// check already holds, so the game refuses nothing it loaded before (G4); everything that loads today
/// keeping every check; and the reference marks resolving through a lookup.
/// </summary>
public class EngineKindMarksTests
{
    /// <summary>The kinds this step marks, by record name: their nested record types are described with them.</summary>
    public static readonly Dictionary<string, Type> Kinds = new()
    {
        ["scene.sequence"] = typeof(Sequence), ["scene.curve"] = typeof(FCurve), ["particle.effect"] = typeof(ParticleEffect),
        ["model"] = typeof(ModelData), ["terrain"] = typeof(TerrainData), ["animation.state-machine"] = typeof(StateMachineDefinition),
        ["ui.stylesheet"] = typeof(StylesheetRecord),
    };

    public static TheoryData<string> KindNames => [.. Kinds.Keys];

    static string MarksFile(string kind) => Path.Combine(RecordFixtures.Folder, "Marks", kind + ".txt");

    /// <summary>
    /// A kind's shape as tools see it, with every nested record type it holds (a union's subtypes after it): each
    /// type's record name, display name, folder and rule, help; each field's JSON and display names and marks.
    /// </summary>
    internal static string Describe(Type root)
    {
        var text = new StringBuilder();
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>([root]);
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
                continue;
            var shape = RecordShape.Of(type);
            text.Append($"{shape.Name} \"{shape.DisplayName}\"");
            if (shape.Folder != null)
                text.Append($" in {shape.Folder} by {shape.Rule.GetType().Name}");
            text.Append('\n');
            if (shape.Help != null)
                text.Append($"  help: {shape.Help}\n");
            foreach (var subtype in shape.Subtypes)
                queue.Enqueue(subtype.Type);
            foreach (var field in shape.Fields)
            {
                var marks = new List<string>();
                if (field.Unit != null)
                    marks.Add($"unit {field.Unit}");
                if (field.Limits is { } limits)
                    marks.Add($"limits {limits.Describe()}");
                if (field.Choices is { } choices)
                    marks.Add($"choices {string.Join(", ", choices)}");
                if (field.IsColour)
                    marks.Add("colour");
                if (field.IsBulk)
                    marks.Add($"bulk \"{field.BulkSummary ?? "{0}"}\"");
                foreach (var reference in field.References)
                    marks.Add($"references {reference.Kind.Name} in {reference.Folder} by {reference.Rule.GetType().Name}{(reference.Keys ? " (keys)" : "")}"
                        + (reference.When is { } when ? $" when {when.Name} is {string.Join(" or ", reference.Is)}" : ""));
                text.Append($"  {field.JsonName} \"{field.DisplayName}\"{(marks.Count > 0 ? ": " + string.Join("; ", marks) : "")}\n");
                if (field.Help != null)
                    text.Append($"    help: {field.Help}\n");
                foreach (var held in RecordTypesIn(field.Value))
                    queue.Enqueue(held);
            }
        }
        return text.ToString();
    }

    static IEnumerable<Type> RecordTypesIn(ValueShape shape) => shape.Kind switch
    {
        ValueKind.Record or ValueKind.Union => [shape.Record!.Type],
        ValueKind.Nullable or ValueKind.List or ValueKind.Map or ValueKind.Pairs => RecordTypesIn(shape.Element!),
        _ => [],
    };

    /// <summary>Each kind's marks are as recorded in Data/Records/Marks/&lt;kind&gt;.txt (written, when missing, by a run with STATVENTURE_RECORD_FIXTURES=1).</summary>
    [Theory]
    [MemberData(nameof(KindNames))]
    public void TheMarksAreAsRecorded(string kind)
    {
        var described = Describe(Kinds[kind]);
        var file = MarksFile(kind);
        if (!File.Exists(file))
        {
            Assert.True(RecordFixtures.Recording, $"no marks file {file}: run the tests with STATVENTURE_RECORD_FIXTURES=1 to write it");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, described);
        }
        Assert.Equal(File.ReadAllText(file).Replace("\r", ""), described);
    }

    /// <summary>
    /// The help texts this step gives (M11-10, the kinds with a preview first): every field of a particle effect and of
    /// a cutscene's records (the sequence, its roles, spans, strips, tracks, cues and curves) has one, and every kind has one.
    /// </summary>
    [Fact]
    public void ParticleEffectsAndCutscenesHaveHelpOnEveryField()
    {
        foreach (var shape in Reachable(typeof(Sequence)).Concat(Reachable(typeof(ParticleEffect))))
        {
            Assert.True(shape.Help != null, $"{shape.Name} has no help");
            foreach (var field in shape.Fields)
                Assert.True(field.Help != null, $"{field} has no help");
        }
        foreach (var kind in Kinds.Values)
            Assert.NotNull(RecordShape.Of(kind).Help);
    }

    /// <summary>The shapes of a kind and of every record type it holds.</summary>
    static IEnumerable<RecordShape> Reachable(Type root)
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>([root]);
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
                continue;
            var shape = RecordShape.Of(type);
            yield return shape;
            foreach (var subtype in shape.Subtypes)
                queue.Enqueue(subtype.Type);
            foreach (var field in shape.Fields)
                foreach (var held in RecordTypesIn(field.Value))
                    queue.Enqueue(held);
        }
    }

    // ---------- G4: only limits the kinds' own checks already hold ----------

    /// <summary>Valid samples that hold every field with a limit of these kinds (a strip, a span, every cue, every shape and spawn, a sun, a grid).</summary>
    static readonly Dictionary<string, Func<object>> Samples = new()
    {
        ["the every-field sequence"] = () => SequenceRecordTests.EveryField(),
        ["the test sequence"] = () => TestSequence.Build(),
        ["a spark in a cone"] = () => ParticleTests.Full(),
        ["a ring in a sphere"] = () => ParticleTests.Defaults() with { Shape = new ParticleShape.Ring(0.25f), Spawn = new ParticleSpawn.Sphere(0.5f) },
        ["a puff"] = () => ParticleTests.Defaults() with { Shape = new ParticleShape.Puff(0.5f) },
        ["a shard"] = () => ParticleTests.Defaults() with { Shape = new ParticleShape.Shard(0.5f) },
        ["the stage sample"] = () => StageRecordSamples.Stage(),
        ["the small terrain"] = () => TerrainSamples.Small(),
    };

    public static TheoryData<string> SampleNames => [.. Samples.Keys];

    /// <summary>Every field with a limit in a record, with its path (a limit on a list's numbers isn't declared by these kinds).</summary>
    static IEnumerable<(RecordPath Path, FieldShape Field)> Limited(object record, RecordPath at)
    {
        foreach (var field in RecordShape.Of(record.GetType()).Fields)
        {
            var path = at.Field(field.JsonName);
            var value = field.Get(record);
            if (field.Limits != null)
            {
                Assert.True(field.Value.Kind is not (ValueKind.List or ValueKind.Map or ValueKind.Pairs), $"{field}: a limit on a list");
                yield return (path, field);
            }
            if (field.Value.HoldsRecords)
                foreach (var (child, where) in Held(field.Value, value, path))
                    foreach (var found in Limited(child, where))
                        yield return found;
        }
    }

    static IEnumerable<(object Value, RecordPath Path)> Held(ValueShape shape, object? value, RecordPath path)
    {
        if (value == null)
            yield break;
        if (shape.Kind == ValueKind.Nullable)
            foreach (var found in Held(shape.Element!, value, path))
                yield return found;
        else if (shape.Kind == ValueKind.List)
        {
            var items = (System.Collections.IList)(value is System.Collections.IList list ? list : ((System.Collections.IEnumerable)value).Cast<object>().ToList());
            for (var i = 0; i < items.Count; i++)
                foreach (var found in Held(shape.Element!, items[i], path.Index(i)))
                    yield return found;
        }
        else if (shape.Kind is ValueKind.Record or ValueKind.Union)
            yield return (value, path);
    }

    /// <summary>Values just outside a limit: below its minimum (the minimum itself when that is excluded), above its maximum, and NaN for a float.</summary>
    static IEnumerable<object> Outside(FieldShape field)
    {
        var limits = field.Limits!;
        var number = field.Value.Kind == ValueKind.Nullable ? field.Value.Element! : field.Value;
        var values = new List<double>();
        if (!double.IsNegativeInfinity(limits.Min))
            values.Add(limits.MinExclusive ? limits.Min : limits.Min - 1);
        if (!double.IsPositiveInfinity(limits.Max))
            values.Add(limits.MaxExclusive ? limits.Max : limits.Max + 1);
        if (number.IsFloat)
            values.Add(double.NaN);
        return values.Select(v => number.Kind switch
        {
            ValueKind.Float32 => (object)(float)v,
            ValueKind.Float64 => v,
            ValueKind.Int32 => (int)v,
            _ => throw new InvalidOperationException($"{field}: a limit on a {number}"),
        });
    }

    /// <summary>
    /// G4 (the user's answer: the game enforces declared limits too, and in M11 only limits its checks already hold
    /// are declared): every value just outside a declared limit, put into a valid sample, is refused by the kind's
    /// own rules already, first and at the same field (or the record can't even be made: a curve's key), so the
    /// limit refuses nothing the game loaded before and changes no message it logs.
    /// </summary>
    [Theory]
    [MemberData(nameof(SampleNames))]
    public void EveryDeclaredLimitIsHeldByTheKindsOwnCheck(string name)
    {
        var sample = Samples[name]();
        Assert.Empty(RecordChecks.Check(sample));
        var limited = Limited(sample, RecordPath.Root).ToList();
        Assert.NotEmpty(limited);
        var draft = RecordDraft.From(sample);
        foreach (var (path, field) in limited)
            foreach (var value in Outside(field))
            {
                var build = draft.With(path, DraftValue.Of(value)).Build();
                if (!build.Built)
                {
                    // A curve refuses its key (the only record here that can't hold a broken value).
                    var refused = Assert.Single(build.Problems);
                    Assert.StartsWith(refused.Path + ".keys[", path.ToString());
                    continue;
                }
                var problems = RecordChecks.Check(build.Value!);
                var at = problems.Where(p => p.Path == path).ToList();
                Assert.True(at.Any(p => p.Message.StartsWith("must be ", StringComparison.Ordinal)), $"{name}: {path} = {value}: no limit problem");
                Assert.True(at.Any(p => !p.Message.StartsWith("must be ", StringComparison.Ordinal)), $"{name}: {path} = {value}: not refused by the kind's own check: {string.Join("; ", problems)}");
                Assert.False(problems[0].Message.StartsWith("must be ", StringComparison.Ordinal), $"{name}: {path} = {value}: the limit's words first");
            }
    }

    /// <summary>Which fields have limits, so a limit added or dropped shows here as in the marks files: every one this step declares.</summary>
    [Fact]
    public void TheLimitedFieldsAreTheseAndTheSamplesReachThemAll()
    {
        var declared = Kinds.Values.SelectMany(Reachable).Distinct().SelectMany(s => s.Fields).Where(f => f.Limits != null).Select(f => f.ToString()).Order().ToList();
        var reached = Samples.Values.SelectMany(make => Limited(make(), RecordPath.Root)).Select(l => l.Field.ToString()).Distinct().Order().ToList();
        Assert.Equal(declared, reached);
        Assert.Equal(34, reached.Count);
    }

    // ---------- What loads today ----------

    /// <summary>
    /// Everything of these kinds that loads today keeps every check: each kind's fixtures, the test models and the
    /// test terrain as imported, the proof's particle effects, the test sequence, the state machines, the compiled
    /// copy of the game's stylesheet, the reference scenes' effects and the committed packs' records of these kinds
    /// (ui.pak's stylesheet).
    /// (The proof's sequence against its data: <see cref="SequenceDataCheckTests"/>.)
    /// </summary>
    [Fact]
    public void EverythingThatLoadsTodayKeepsTheChecks()
    {
        var checkedCount = 0;
        void Keeps(object record, string what)
        {
            Assert.True(RecordChecks.Check(record).Count == 0, $"{what}: {string.Join("; ", RecordChecks.Check(record))}");
            checkedCount++;
        }
        foreach (var (type, label, extension) in RecordFixtures.All().Where(f => Kinds.ContainsKey(f.Type) && f.Extension == ".rec"))
            Keeps(RecordSerializer.FromBinary(Kinds[type], RecordFixtures.Read(type, label, extension)), $"the fixture {type}/{label}");
        foreach (var name in TestModelPack.Names)
            Keeps(GltfImport.Load(TestModelPack.SourceOf(name)), $"the test model {name}");
        Keeps(TerrainData.Load(TestTerrainPack.Description), "the test terrain");
        var (mist, embers) = JiruanIntro.Effects();
        Keeps(mist, "the proof's mist");
        Keeps(embers, "the proof's embers");
        Keeps(TestSequence.Build(), "the test sequence");
        Keeps(StateMachineTests.Humanoid(), "the humanoid machine");
        Keeps(StateMachineRecordTests.Full(), "the full machine");
        Keeps(GameStylesheets.Files.Load<StylesheetRecord>(GameStylesheets.Path), "the game's stylesheet");
        foreach (var effect in Scenes3D.All.SelectMany(s => s.Particles).Select(e => e.Effect).Distinct())
            Keeps(effect, "a reference scene's effect");
        Assert.True(checkedCount > 40, $"{checkedCount}");
        // The committed packs' records of these kinds (since GM1 step 1a ui.pak's stylesheet, ui/styles/game) keep them too.
        var inPacks = 0;
        foreach (var file in Directory.GetFiles(TestPaths.Packs, "*.pak"))
        {
            using var pack = PackFile.Open(file);
            foreach (var entry in pack.Entries.Where(e => e.IsRecord))
            {
                var bytes = pack.Read(entry);
                if (Kinds.TryGetValue(RecordSerializer.RootTypeName(bytes), out var kind))
                {
                    Keeps(RecordSerializer.FromBinary(kind, bytes), $"{Path.GetFileName(file)}'s {entry.Path}");
                    inPacks++;
                }
            }
        }
        Assert.True(inPacks >= 1, "no committed pack holds a record of these kinds");
    }

    // ---------- References ----------

    /// <summary>The record kinds at paths, as a lookup for the reference marks (M11-11).</summary>
    sealed class Lookup(Dictionary<string, string> kinds) : IReferenceLookup
    {
        public string? KindAt(string path) => kinds.GetValueOrDefault(path);
    }

    /// <summary>
    /// The references marked (M11-11): a sequence's stages are models by their whole path one level below
    /// models/stages, a particle cue's effect a particle effect by its whole path below models/effects; each checked
    /// through a lookup with the generic words (beside the sequence's own data check, which the editor shows first, P16);
    /// an audio cue's name a sound effect while it plays one, a theme while it switches the music (by the music's names,
    /// step 5's rule), nothing while it stops the music.
    /// </summary>
    [Fact]
    public void TheReferencesResolveThroughALookup()
    {
        var sequence = new Sequence
        {
            Name = "refs", Duration = 2, Roles = [new("pivot", RoleKind.Node)], Stages = ["models/stages/arena"],
            Tracks =
            [
                new ParticleTrack { Cues = [new(0, "models/effects/rift/mist", "pivot")] },
                new AudioTrack { Cues = [new(0, AudioCueKind.Effect, "bigBoom"), new(1, AudioCueKind.Music, "great-imp-jiruan"), new(2, AudioCueKind.StopMusic)] },
            ],
        };
        Assert.Empty(RecordChecks.Check(sequence, references: new Lookup(new()
        {
            ["models/stages/arena"] = "model", ["models/effects/rift/mist"] = "particle.effect", ["audio/effects/big_boom"] = "audio.effect",
            ["audio/themes/great-imp-jiruan"] = "audio.theme",
        })));
        Assert.Equal([
                "$.stages[0]: no model \"models/stages/arena\" (models/stages/arena)",
                "$.tracks[0].cues[0].effect: models/effects/rift/mist isn't a particle effect: it holds a model record",
                "$.tracks[1].cues[0].name: no sound effect \"bigBoom\" (audio/effects/big_boom)",
                "$.tracks[1].cues[1].name: audio/themes/great-imp-jiruan isn't a theme: it holds a audio.effect record",
            ],
            RecordChecks.Check(sequence, references: new Lookup(new()
            {
                ["models/effects/rift/mist"] = "model", ["audio/themes/great-imp-jiruan"] = "audio.effect",
            })).Select(p => $"{p.Path}: {p.Message}"));
        var stages = RecordShape.Of<Sequence>().Field("stages")!.References.Single();
        Assert.Equal(("models/stages/arena", "models/stages/arena"), (stages.PathOf("models/stages/arena"), stages.NameOf("models/stages/arena")));
        Assert.Null(stages.PathOf("arena"));
        var name = RecordShape.Of<AudioCue>().Field("name")!;
        Assert.Equal(("audio.effect", "audio/effects/big_boom"), (name.ReferenceFor(AudioCueKind.Effect)!.Kind.Name, name.ReferenceFor(AudioCueKind.Effect)!.PathOf("bigBoom")));
        Assert.Equal(("audio.theme", "audio/themes/combat.alder-vale"), (name.ReferenceFor(AudioCueKind.Music)!.Kind.Name, name.ReferenceFor(AudioCueKind.Music)!.PathOf("combat@alder-vale")));
        Assert.Null(name.ReferenceFor(AudioCueKind.StopMusic));
    }

    /// <summary>Bulk fields keep their objects in a draft and show as their summaries (P21): a model's meshes, a terrain's samples, a sheet's rules.</summary>
    [Fact]
    public void BulkFieldsShowTheirSummaries()
    {
        var model = GltfImport.Load(TestModelPack.SourceOf("jiruan"));
        var meshes = RecordShape.Of<ModelData>().Field("meshes")!;
        Assert.Equal($"{model.Meshes.Count} meshes", meshes.Summarize(model.Meshes));
        Assert.Same(model.Meshes, ((DraftLeaf)RecordDraft.From(model)["meshes"]).Value);
        var terrain = TerrainSamples.Small();
        Assert.Equal($"{terrain.HeightSamples.Length:N0} height samples", RecordShape.Of<TerrainData>().Field("heightSamples")!.Summarize(terrain.HeightSamples));
        var sheet = GameStylesheets.Files.Load<StylesheetRecord>(GameStylesheets.Path);
        Assert.Equal($"{sheet.Rules.Count:N0} style rules", RecordShape.Of<StylesheetRecord>().Field("rules")!.Summarize(sheet.Rules));
        Assert.Equal("a matrix, as imported", RecordShape.Of<ModelNode>().Field("local")!.Summarize(model.Nodes[0].Local));
    }
}
