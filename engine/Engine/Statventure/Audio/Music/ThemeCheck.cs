using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Audio;

/// <summary>
/// The instruments, percussion voices and rooms a theme is checked against (<see cref="ThemeCheck"/>): a
/// library's, or the game's data's, found by the music's name rule.
/// </summary>
internal sealed class MusicParts
{
    readonly Func<string, InstrumentSpec?> _instrument;
    readonly Func<string, bool> _percussion;
    readonly Func<string, bool> _room;

    MusicParts(Func<string, InstrumentSpec?> instrument, Func<string, bool> percussion, Func<string, bool> room)
    {
        _instrument = instrument;
        _percussion = percussion;
        _room = room;
    }

    /// <summary>The parts a library has (or a library being loaded has read).</summary>
    public static MusicParts Of(IReadOnlyDictionary<string, InstrumentSpec> instruments, IReadOnlyDictionary<string, PercussionSpec> percussion,
        IReadOnlyDictionary<string, RoomSpec> rooms) =>
        new(name => instruments.GetValueOrDefault(name), percussion.ContainsKey, rooms.ContainsKey);

    /// <summary>
    /// The parts in <paramref name="data"/> at the music's paths (an instrument read once, however often it is
    /// named); an instrument entry that doesn't read as one is the file system's <see cref="DataFileException"/>.
    /// </summary>
    public static MusicParts Of(VirtualFileSystem data)
    {
        var read = new Dictionary<string, InstrumentSpec?>(StringComparer.Ordinal);
        bool Has(string folder, string name) => MusicNames.ToPathName(name) is { } stored && data.Exists(folder + "/" + stored);
        InstrumentSpec? Instrument(string name)
        {
            if (!read.TryGetValue(name, out var instrument))
                read[name] = instrument = Has(MusicNames.Instruments, name) ? data.Load<InstrumentSpec>(MusicNames.PathOf(MusicNames.Instruments, name)) : null;
            return instrument;
        }
        return new(Instrument, name => Has(MusicNames.Percussion, name), name => Has(MusicNames.Rooms, name));
    }

    /// <summary>The instrument called <paramref name="name"/>, or null.</summary>
    public InstrumentSpec? Instrument(string name) => _instrument(name);

    public bool HasPercussion(string name) => _percussion(name);

    public bool HasRoom(string name) => _room(name);
}

/// <summary>
/// A theme against the composer, without playing it (M11 step 5, G5; Docs/Audio.md, "Checks and marks"): what
/// today fails only when the music is compiled (all themes, the first time one is asked for) or played (on the
/// sound thread), each at the path of what it names, in the words the score and the composer fail with. In
/// order: the room (today's rule at load), the score as <see cref="Score.Compile"/> resolves it (the mode, the
/// meter, each section the form plays: its chords, melody and layers' notes; the form and its loop point), then
/// each of those sections' layers by the composer's rules for its type (<see cref="ComposerNames"/>): its type,
/// the instrument or percussion voices it plays and whether they are there and sound, its texture, pattern and
/// scale degrees, and a held chord with none before it where the layer reads chords. Sections the form doesn't
/// play aren't checked (the composer never reads them). A layer is checked by what its type reads, not by
/// whether a note of it is reached (a pattern of rests, bars it never plays in). The score is compiled once; only
/// when that fails is it walked part by part to find where.
/// </summary>
internal static class ThemeCheck
{
    /// <summary>Every problem of <paramref name="theme"/>; without <paramref name="parts"/> the room and the names its layers play aren't checked.</summary>
    public static List<RecordProblem> Problems(ThemeSpec theme, MusicParts? parts) => Problems(theme, parts, out _);

    /// <summary>
    /// Every problem of <paramref name="theme"/> (see <see cref="Problems(ThemeSpec, MusicParts?)"/>), and the
    /// theme compiled when its score has none (else null).
    /// </summary>
    public static List<RecordProblem> Problems(ThemeSpec theme, MusicParts? parts, out CompiledTheme? compiled)
    {
        var check = new Check(theme, parts);
        check.Run();
        compiled = check.Compiled;
        return check.Problems;
    }

    /// <summary>A layer's place: its section's (or like's) layers and its index; a path made only for a problem.</summary>
    readonly record struct LayerAt(RecordPath Layers, int Index)
    {
        public RecordPath Field(string name) => Layers.Index(Index).Field(name);
    }

    sealed class Check(ThemeSpec theme, MusicParts? parts)
    {
        readonly List<RecordProblem> _room = [], _score = [], _play = [];
        /// <summary>Each part of the score compiled, by where it is written (a like's part, taken by several sections, compiles once).</summary>
        readonly Dictionary<RecordPath, object?> _parts = [];
        /// <summary>The compiled score's sections by name, when it compiled.</summary>
        Dictionary<string, CompiledSection>? _sections;
        double[]? _scale;
        double? _steps;

        public CompiledTheme? Compiled { get; private set; }

        /// <summary>The room's, the score's, then the layers' problems, each once.</summary>
        public List<RecordProblem> Problems => [.. _room.Concat(_score).Concat(_play).Distinct()];

        static RecordPath Root => RecordPath.Root;

        public void Run()
        {
            if (parts != null && MusicLibrary.RoomProblem(theme, parts.HasRoom) is { } room)
                _room.Add(new(Root.Field(nameof(ThemeSpec.Room)), room));
            Exception? failure = null;
            try
            {
                Compiled = Audio.Score.Compile(theme);
                _scale = Compiled.Scale;
                _sections = [];
                foreach (var (section, _) in Compiled.Timeline)
                    _sections.TryAdd(section.Name, section);
            }
            catch (Exception problem)
            {
                failure = problem;
            }
            Walk(score: failure != null);
            // What the walk doesn't know of still fails the compile; it reports that much.
            if (failure != null && _score.Count == 0)
                _score.Add(new(Root, failure.Message));
        }

        /// <summary>
        /// The sections the form plays, each once, in its order, and their layers; with <paramref name="score"/> (it
        /// didn't compile) also the score's parts as <see cref="Audio.Score.Compile"/> meets them, to find where it fails.
        /// </summary>
        void Walk(bool score)
        {
            var id = theme.Id;
            if (score)
            {
                if (!Audio.Score.Modes.TryGetValue(theme.Mode, out _scale))
                    _score.Add(new(Root.Field(nameof(ThemeSpec.Mode)), "Unknown mode " + theme.Mode));
                if (theme.Meter.Length < 2)
                    _score.Add(new(Root.Field(nameof(ThemeSpec.Meter)), $"{id}: the meter needs two numbers, the beats in a bar and the steps in a beat"));
                else
                    _steps = (double)theme.Meter[0] * theme.Meter[1];
            }
            var resolved = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < theme.Form.Length; i++)
            {
                var name = theme.Form[i];
                if (!theme.Sections.ContainsKey(name))
                    _score.Add(new(Root.Field(nameof(ThemeSpec.Form)).Index(i), $"{id}: missing section {name}"));
                else if (resolved.Add(name))
                    Section(name, score);
            }
            if (!score)
                return;
            if (theme.Form.Length == 0)
                _score.Add(new(Root.Field(nameof(ThemeSpec.Form)), $"{id}: the form names no section"));
            else if (theme.Loop is { } loop && (loop < 0 || loop >= theme.Form.Length))
                _score.Add(new(Root.Field(nameof(ThemeSpec.Loop)), $"{id}: its loop point {loop} isn't a place in the form (0 to {theme.Form.Length - 1})"));
        }

        static RecordPath In(string section) => Root.Field(nameof(ThemeSpec.Sections)).Key(section);

        /// <summary>
        /// A section as the score resolves it ({ ...like, ...section }, its layers its own or else the like's), each
        /// part at the path of the section that gives it.
        /// </summary>
        void Section(string name, bool score)
        {
            var id = theme.Id;
            var raw = theme.Sections[name];
            var like = raw.Like != null ? theme.Sections.GetValueOrDefault(raw.Like) : null;
            // Where a part comes from: the section, or (only when it sets none and the like does) the like.
            RecordPath From(object? own) => own != null ? In(name) : In(raw.Like!);
            var chordsText = raw.Chords ?? like?.Chords;
            var layers = raw.Layers ?? like?.Layers ?? [];
            var layersPath = (raw.Layers == null && like?.Layers == null ? In(name) : From(raw.Layers)).Field(nameof(SectionSpec.Layers));
            List<ChordEntry>? chords = _sections?.GetValueOrDefault(name)?.Chords;
            if (score)
            {
                if (chordsText == null)
                    _score.Add(new(In(name).Field(nameof(SectionSpec.Chords)), $"{id}/{name}: no chords"));
                else if (_scale != null && _steps is { } steps)
                {
                    var at = From(raw.Chords).Field(nameof(SectionSpec.Chords));
                    if (Audio.Score.Bars(chordsText) is ["%", ..])
                        _score.Add(new(at, $"{id}/{name}: its first bar is \"%\", which repeats the bar before it"));
                    else
                        chords = Part(at, () => Audio.Score.CompileChords(chordsText, _scale, steps));
                }
                if ((raw.Melody ?? like?.Melody) is { Length: > 0 } melody && _scale != null && _steps is { } melodySteps)
                    Part(From(raw.Melody).Field(nameof(SectionSpec.Melody)), () => Audio.Score.CompileLine(melody, _scale, melodySteps, $"{id}/{name}/mel"));
                for (var index = 0; index < layers.Length; index++)
                    if (layers[index].Notes is { Length: > 0 } notes && _scale != null && _steps is { } lineSteps)
                        Part(layersPath.Index(index).Field(nameof(LayerSpec.Notes)), () => Audio.Score.CompileLine(notes, _scale, lineSteps, $"{id}/{name}/layer{index}"));
            }
            for (var index = 0; index < layers.Length; index++)
                Layer(layers[index], new LayerAt(layersPath, index));
            // A chord line opening with "." holds no chord; a layer reading the chords there fails.
            if (chords != null && chords.Any(entry => entry.Chord is null) && layers.Any(ReadsChords))
                _play.Add(new(From(raw.Chords).Field(nameof(SectionSpec.Chords)), $"{id}: a step plays a held chord with no chord before it"));
        }

        /// <summary>The part of the score written at <paramref name="path"/>, compiled the first time a section takes it.</summary>
        T? Part<T>(RecordPath path, Func<T> compile) where T : class
        {
            if (!_parts.TryGetValue(path, out var part))
            {
                try
                {
                    part = compile();
                }
                catch (FormatException problem)
                {
                    _score.Add(new(path, problem.Message));
                    part = null;
                }
                _parts[path] = part;
            }
            return (T?)part;
        }

        /// <summary>Whether a layer's notes read the section's chords (the composer's ChordPitch or voicing).</summary>
        static bool ReadsChords(LayerSpec layer) =>
            ComposerNames.ChordLayers.Contains(layer.Type)
            || layer.Type == "drums" && (layer.Drums ?? []).Any(drum => ComposerNames.TunedDrums.Contains(drum.Key))
            || layer.Type == "hit" && layer.Instrument == "timp" && layer.Degree is not { Length: > 0 };

        /// <summary>A layer by the composer's rules for its type (<see cref="Performance.Step"/>).</summary>
        void Layer(LayerSpec layer, LayerAt at)
        {
            var type = layer.Type;
            if (!ComposerNames.LayerTypes.Contains(type))
            {
                _play.Add(new(at.Field(nameof(LayerSpec.Type)), "Unknown layer " + type));
                return;
            }
            if (ComposerNames.PatternLayers.Contains(type) && layer.Pattern is not { Length: > 0 })
                _play.Add(new(at.Field(nameof(LayerSpec.Pattern)), $"{theme.Id}: a {type} layer without a pattern"));
            if (ComposerNames.InstrumentLayers.Contains(type)
                && InstrumentProblem(type == "drips" ? layer.Instrument is { Length: > 0 } drip ? drip : ComposerNames.DripsInstrument : layer.Instrument) is { } instrument)
                _play.Add(new(at.Field(nameof(LayerSpec.Instrument)), instrument));
            switch (type)
            {
                case "arp" or "bass" or "pulse" when layer.Doublings != null:
                    // layer.dbl.i of an array is undefined in the old script.
                    _play.Add(new(at.Field(nameof(LayerSpec.Doublings)), "Unknown instrument undefined"));
                    break;
                case "mel" or "line":
                    for (var i = 0; i < (layer.Doublings?.Length ?? 0); i++)
                        if (InstrumentProblem(layer.Doublings![i].Instrument) is { } doubling)
                            _play.Add(new(at.Field(nameof(LayerSpec.Doublings)).Index(i).Field(nameof(DoublingSpec.Instrument)), doubling));
                    if (layer.Harmony is { } harmony && InstrumentProblem(harmony.Instrument) is { } harmonyProblem)
                        _play.Add(new(at.Field(nameof(LayerSpec.Harmony)).Field(nameof(HarmonySpec.Instrument)), harmonyProblem));
                    break;
                case "drums":
                    foreach (var (name, _) in layer.Drums ?? [])
                        if (PercussionProblem(name) is { } drum)
                            _play.Add(new(at.Field(nameof(LayerSpec.Drums)).Key(name), drum));
                    break;
                case "hit":
                    if (PercussionProblem(layer.Instrument) is { } hit)
                        _play.Add(new(at.Field(nameof(LayerSpec.Instrument)), hit));
                    if (layer.Degree is { Length: > 0 } degree && DegreeProblem(degree) is { } tuning)
                        _play.Add(new(at.Field(nameof(LayerSpec.Degree)), tuning));
                    break;
                case "drone":
                    var degrees = layer.Degrees ?? [];
                    for (var i = 0; i < degrees.Length; i++)
                        if (DegreeProblem(degrees[i]) is { } held)
                            _play.Add(new(at.Field(nameof(LayerSpec.Degrees)).Index(i), held));
                    break;
                case "tex" when layer.Kind == null || !Performance.Textures.ContainsKey(layer.Kind):
                    _play.Add(new(at.Field(nameof(LayerSpec.Kind)), "Unknown texture " + (layer.Kind ?? "undefined")));
                    break;
            }
        }

        /// <summary>What is wrong with an instrument a layer plays (not there, or making no sound), or null; nothing without the data.</summary>
        string? InstrumentProblem(string? name)
        {
            if (parts == null)
                return null;
            InstrumentSpec? instrument;
            try
            {
                instrument = name != null ? parts.Instrument(name) : null;
            }
            catch (IOException problem)
            {
                return problem.Message;
            }
            return instrument == null ? "Unknown instrument " + (name ?? "undefined")
                : !ComposerNames.MakesSound(instrument) ? $"Instrument {name} of kind {instrument.Kind} makes no sound"
                : null;
        }

        /// <summary>What is wrong with a percussion voice a layer plays (not there), or null; nothing without the data.</summary>
        string? PercussionProblem(string? name) =>
            parts != null && (name == null || !parts.HasPercussion(name)) ? "Unknown percussion " + (name ?? "undefined") : null;

        /// <summary>What is wrong with a scale degree a layer tunes to, or null (nothing while the scale is unknown).</summary>
        string? DegreeProblem(string degree)
        {
            if (_scale == null)
                return null;
            try
            {
                Audio.Score.Degree(degree, _scale);
                return null;
            }
            catch (FormatException problem)
            {
                return problem.Message;
            }
        }
    }
}
