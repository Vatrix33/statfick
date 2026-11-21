// The figure mode's scene (GM2-P14; Docs/Game/Hero.md, "How it works", step 3a): a figure (a body .glb, its garments on
// its bones, its items on its sockets) as the game shows one, an Actor on a node of a SceneView in a View3D, in a look's
// numbers (FigureLook), playing its clips in turn, seen in a view. The views and their framing are render.py's
// (Tools/Models/humanoid/render.py), so "three-quarter" is the same view in Blender's renders and here; and the scene
// at any moment is written as the scene file Blender's harness renders (render_scenes.py's format, as
// Render3DReferenceTests writes its scenes), which is how the engine's picture is held to Blender's.
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Graphics;
using Statventure.Render3D;
using Statventure.Scene;

namespace Statventure.Sandbox.Figure;

/// <summary>What the figure mode shows: the figure's files, its look, the view, the picture's size and the framing's numbers (render.py's).</summary>
public sealed record FigureSpec
{
    public required string Body { get; init; }
    public IReadOnlyList<string> Garments { get; init; } = [];
    /// <summary>Items as (.glb, socket bone).</summary>
    public IReadOnlyList<(string Model, string Socket)> Items { get; init; } = [];
    /// <summary>A clip library (.glb) played by bone names; null: the body's own clips.</summary>
    public string? Clips { get; init; }
    /// <summary>The clips to play in turn, by name (--play); null: every clip of the library once, in its order.</summary>
    public IReadOnlyList<FigurePlay>? Play { get; init; }
    public required FigureLook Look { get; init; }
    /// <summary>front, three-quarter, side, back, close-up, or a turn in degrees.</summary>
    public string View { get; init; } = "three-quarter";
    public int Width { get; init; } = 600;
    public int Height { get; init; } = 800;
    /// <summary>How far the camera looks down, in degrees.</summary>
    public float Elevation { get; init; } = 5;
    /// <summary>Room around the figure in a full view, a share of its size on each side.</summary>
    public float Margin { get; init; } = 0.06f;
    /// <summary>The height framed (metres); null: the figure's with the margin, a close-up 0.3 of the figure's.</summary>
    public float? Extent { get; init; }
    /// <summary>The bone a close-up frames; null: "head", else the figure's top.</summary>
    public string? Focus { get; init; }
    /// <summary>A perspective camera's vertical field of view in degrees; null: orthographic.</summary>
    public float? Perspective { get; init; }
}

/// <summary>render.py's views and framing (Tools/Models/humanoid/render.py, view_spec): the same numbers on both sides.</summary>
public static class FigureFraming
{
    /// <summary>Each view's turn of the figure about Y in degrees (its front, glTF's +Z, turned toward the screen's right).</summary>
    public static readonly IReadOnlyDictionary<string, float> Turns = new Dictionary<string, float>
    {
        ["front"] = 0, ["three-quarter"] = 45, ["side"] = 90, ["back"] = 180, ["close-up"] = 45,
    };

    /// <summary>The views in order (the keys 1–5).</summary>
    public static readonly IReadOnlyList<string> Views = ["front", "three-quarter", "side", "back", "close-up"];

    public static float TurnOf(string view) =>
        Turns.TryGetValue(view, out var turn) ? turn
        : float.TryParse(view, NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees) ? degrees
        : throw new ArgumentException($"no view {view} (front, three-quarter, side, back, close-up or degrees)");

    /// <summary>The figure's surface points at rest (model space, then <paramref name="placement"/>): each mesh's positions where the rest pose puts them (<see cref="ModelData.RestPoints"/>).</summary>
    public static List<Vector3> RestPoints(ModelData model, Matrix4x4 placement) => model.RestPoints(placement);

    /// <summary>A bone's middle at rest (model space): halfway to its first child, or 0.1 m up its own Y without one; null without that bone.</summary>
    public static Vector3? FocusPoint(ModelData model, string bone)
    {
        var index = -1;
        for (var i = 0; i < model.Nodes.Count && index < 0; i++)
            if (model.Nodes[i].Name == bone)
                index = i;
        if (index < 0)
            return null;
        var world = model.WorldMatrices();
        var head = world[index].Translation;
        var children = model.Nodes[index].Children;
        var tail = children.Count > 0 ? world[children[0]].Translation : Vector3.Transform(new Vector3(0, 0.2f, 0), world[index]);
        return Vector3.Lerp(head, tail, 0.5f);
    }

    /// <summary>The camera for a view (render.py's view_spec): the figure at rest turned by the view's turn, framed whole, or a close-up on the focus.</summary>
    public static Camera CameraFor(FigureSpec spec, string view, IReadOnlyList<Vector3> restPoints, Vector3? focus)
    {
        var rotation = Matrix4x4.CreateRotationY(TurnOf(view) * MathF.PI / 180);
        var turned = restPoints.Select(p => Vector3.Transform(p, rotation)).ToList();
        Vector3 min = turned.Aggregate(Vector3.Min), max = turned.Aggregate(Vector3.Max);
        var size = max - min;
        var aspect = (float)spec.Width / spec.Height;
        Vector3 target;
        float extent;
        if (view == "close-up")
        {
            target = focus is { } f ? Vector3.Transform(f, rotation) : new Vector3((min.X + max.X) / 2, max.Y - 0.15f * size.Y, (min.Z + max.Z) / 2);
            extent = spec.Extent ?? 0.3f * size.Y;
        }
        else
        {
            target = (min + max) / 2;
            extent = spec.Extent ?? MathF.Max(size.Y, size.X / aspect) * (1 + 2 * spec.Margin);
        }
        var reach = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        var elevation = spec.Elevation * MathF.PI / 180;
        var camera = new Camera { Target = target, Up = Vector3.UnitY, Near = 0.1f, OrthographicHeight = extent, VerticalFieldOfView = 40 * MathF.PI / 180 };
        float distance;
        if (spec.Perspective is { } fov)
        {
            camera.Projection = Projection.Perspective;
            camera.VerticalFieldOfView = fov * MathF.PI / 180;
            distance = extent / 2 / MathF.Tan(camera.VerticalFieldOfView / 2);
        }
        else
        {
            camera.Projection = Projection.Orthographic;
            distance = reach + 5;
        }
        camera.Far = MathF.Max(50, distance + reach + 10);
        camera.Position = target + distance * new Vector3(0, MathF.Sin(elevation), MathF.Cos(elevation));
        return camera;
    }
}

/// <summary>
/// One turn of the figure mode's play list (--play, GM2 step 4b): a clip played <see cref="Times"/> times in a row, and
/// optionally another clip laid over it (<see cref="Over"/>: a layer above that replaces only the channels its clip keys,
/// Blender's NLA rule; a stance loop of the arms over the idle, GM2-P21), looping on its own time for as long.
/// </summary>
public sealed record FigurePlay(string Clip, int Times = 1, string? Over = null)
{
    /// <summary>
    /// A play list as the command line gives it: turns split by commas, each <c>clip[+over][:times]</c> ("idle:2",
    /// "idle+hold-bow", "swing:3"); an <see cref="ArgumentException"/> names what is wrong.
    /// </summary>
    public static IReadOnlyList<FigurePlay> Parse(string text)
    {
        var list = new List<FigurePlay>();
        foreach (var turn in text.Split(','))
        {
            var (name, times) = (turn, 1);
            if (turn.LastIndexOf(':') is var colon and >= 0)
            {
                name = turn[..colon];
                if (!int.TryParse(turn[(colon + 1)..], System.Globalization.NumberStyles.None, CultureInfo.InvariantCulture, out times) || times < 1)
                    throw new ArgumentException($"--play {turn}: the times after ':' must be a whole number from 1");
            }
            var parts = name.Split('+');
            if (parts.Length > 2 || parts.Any(p => p.Length == 0))
                throw new ArgumentException($"--play {turn}: <clip>[+<clip over it>][:times]");
            list.Add(new FigurePlay(parts[0], times, parts.Length == 2 ? parts[1] : null));
        }
        return list;
    }
}

/// <summary>
/// Clips in turn on one clock (an NLA layer of strips end to end, played round, and a layer above for the clips laid
/// over them): which clip acts at a time and when, as the stack's layer picks it.
/// </summary>
public sealed class FigureClips
{
    /// <summary>Every clip once, in its order.</summary>
    public FigureClips(IReadOnlyList<AnimationClip> clips) : this(clips.Select(c => (c, 1, (AnimationClip?)null)).ToList())
    {
    }

    /// <summary>A play list over a library's clips (null: every clip once, in the library's order); a name the library hasn't is refused.</summary>
    public FigureClips(IReadOnlyList<AnimationClip> library, IReadOnlyList<FigurePlay>? play, string where) : this(play is null
        ? library.Select(c => (c, 1, (AnimationClip?)null)).ToList()
        : play.Select(p => (Find(library, p.Clip, where), p.Times, p.Over is { } over ? Find(library, over, where) : null)).ToList())
    {
    }

    FigureClips(List<(AnimationClip Clip, int Times, AnimationClip? Over)> turns)
    {
        Clips = turns.Select(t => t.Clip).ToArray();
        Repeats = turns.Select(t => t.Times).ToArray();
        Overs = turns.Select(t => t.Over).ToArray();
        var start = 0.0;
        Starts = turns.Select((t, i) =>
        {
            var at = start;
            start += TurnLength(i);
            return at;
        }).ToArray();
        Cycle = start;
    }

    static AnimationClip Find(IReadOnlyList<AnimationClip> library, string name, string where) =>
        library.FirstOrDefault(c => c.Name == name) ?? throw new ArgumentException($"no clip {name} in {where}");

    /// <summary>Each turn's clip.</summary>
    public IReadOnlyList<AnimationClip> Clips { get; }
    /// <summary>How many times each turn plays its clip in a row.</summary>
    public IReadOnlyList<int> Repeats { get; }
    /// <summary>The clip laid over each turn's, or null.</summary>
    public IReadOnlyList<AnimationClip?> Overs { get; }
    /// <summary>Each turn's start on the cycle.</summary>
    public IReadOnlyList<double> Starts { get; }
    /// <summary>All the turns once, in seconds (0 without clips).</summary>
    public double Cycle { get; }

    /// <summary>A clip's length on the cycle: its duration, a still clip a frame.</summary>
    public static double Length(AnimationClip clip) => Math.Max(clip.Duration, 1.0 / 60);

    /// <summary>A turn's length: its clip's times its repeats.</summary>
    public double TurnLength(int turn) => Length(Clips[turn]) * Repeats[turn];

    /// <summary>A playing time on the cycle (played round, before 0 too).</summary>
    public double Wrap(double time) => Cycle <= 0 ? 0 : time - Math.Floor(time / Cycle) * Cycle;

    /// <summary>
    /// The turn acting at a playing time and its clip's time: a turn from its start to its end, ends included (the stack's
    /// rule: at a meeting the earlier at its end), its clip's time as the strip's (AnimationStrip.ClipTimeAt: the time
    /// round the clip, its end at the turn's very end).
    /// </summary>
    public (int Clip, double Time) At(double time)
    {
        if (Clips.Count == 0)
            return (-1, 0);
        var t = Wrap(time);
        for (var i = 0; i < Clips.Count; i++)
            if (t <= Starts[i] + TurnLength(i) + AnimationStrip.Epsilon)
            {
                var into = t - Starts[i];
                if (Math.Abs(into - TurnLength(i)) <= AnimationStrip.Epsilon)
                    return (i, Clips[i].Duration);
                var round = into % Length(Clips[i]);
                return (i, Math.Min(Length(Clips[i]) - round <= AnimationStrip.Epsilon ? 0 : round, Clips[i].Duration));
            }
        return (Clips.Count - 1, Clips[^1].Duration);
    }

    /// <summary>The layer that plays the turns' clips so on a skeleton.</summary>
    public AnimationLayer Layer(Skeleton skeleton)
    {
        var layer = new AnimationLayer();
        for (var i = 0; i < Clips.Count; i++)
            layer.Strips.Add(new AnimationStrip(new ClipBinding(Clips[i], skeleton)) { Start = Starts[i], Repeat = Repeats[i] });
        return layer;
    }

    /// <summary>
    /// The layer above that plays the clips laid over the turns (null: none is): each over its turn, from its start, round
    /// for the turn's length, and nothing outside it (so the turns without one show their own clip whole).
    /// </summary>
    public AnimationLayer? OverLayer(Skeleton skeleton)
    {
        if (Overs.All(o => o is null))
            return null;
        var layer = new AnimationLayer();
        for (var i = 0; i < Clips.Count; i++)
            if (Overs[i] is { } over)
                layer.Strips.Add(new AnimationStrip(new ClipBinding(over, skeleton))
                {
                    Start = Starts[i], Repeat = TurnLength(i) / Length(over), Extrapolation = Extrapolation.Nothing,
                });
        return layer;
    }

    /// <summary>A turn's name: its clip's, with the one laid over it after a "+" (as --play names it).</summary>
    public string NameOf(int turn) => Overs[turn] is { } over ? $"{Clips[turn].Name}+{over.Name}" : Clips[turn].Name;
}

/// <summary>The figure in its scene: models loaded, the actor posed at a playing time, turned for the view, framed, in the look.</summary>
public sealed class FigureView : IDisposable
{
    readonly List<Model> _models = [];
    readonly SceneNode _node;
    readonly List<Vector3> _restPoints = [];
    readonly Vector3? _focus;
    double _time;
    float _turn;

    public FigureView(GpuDevice device, FigureSpec spec)
    {
        Spec = spec;
        Body = Load(device, spec.Body);
        var skeleton = new Skeleton(Body.Data);
        var clips = spec.Clips is { } library ? AnimationClip.AllOf(ModelData.Load(library)) : AnimationClip.AllOf(Body.Data);
        Clips = new FigureClips(clips, spec.Play, Path.GetFileName(spec.Clips ?? spec.Body));
        var stack = new AnimationStack(skeleton);
        stack.Layers.Add(Clips.Layer(skeleton));
        if (Clips.OverLayer(skeleton) is { } over)
            stack.Layers.Add(over);
        Actor = new Actor(Body, new Animator(stack)) { Palette = spec.Look.Palette };
        foreach (var path in spec.Garments)
        {
            var garment = Load(device, path);
            Actor.Garments.Add(new ActorGarment(garment, new Garment(garment.Data, skeleton)));
        }
        foreach (var (path, socketName) in spec.Items)
            Actor.Items.Add(new ActorItem(Load(device, path), new Socket(skeleton, socketName)));
        // The figure stands by its soles on its node (GM2-P22), as the game's figures do; the view frames it as it stands.
        Actor.Ground = Actor.GroundOf(Body.Data, Actor.Garments.Select(g => g.Model.Data));
        Lift = Matrix4x4.CreateTranslation(0, -Actor.Ground, 0);
        _restPoints.AddRange(FigureFraming.RestPoints(Body.Data, Lift));
        foreach (var garment in Actor.Garments)
            _restPoints.AddRange(FigureFraming.RestPoints(garment.Model.Data, Lift));
        var rest = Body.Data.WorldMatrices();
        foreach (var item in Actor.Items)
            _restPoints.AddRange(FigureFraming.RestPoints(item.Model.Data, item.Socket.ItemTransform(rest, Lift)));
        _focus = FigureFraming.FocusPoint(Body.Data, spec.Focus ?? "head") is { } point ? Vector3.Transform(point, Lift) : null;
        if (spec.Focus is { } focus && _focus is null)
            throw new ArgumentException($"{spec.Body} has no bone {focus} to frame");
        View = new View3D(device) { Background = spec.Look.BackgroundLinear, Sun = spec.Look.Sun, Style = spec.Look.Style };
        // The view's clock is the figure mode's own: the scene holds the figures (rate 0), the mode seeks them.
        Scene = new SceneView(View) { Rate = 0 };
        _node = Scene.Root.Add("figure");
        _node.Attach(Actor);
        ShowView(spec.View);
        Seek(0);
    }

    Model Load(GpuDevice device, string path)
    {
        var model = Model.Load(device, path);
        _models.Add(model);
        return model;
    }

    public FigureSpec Spec { get; }
    public Model Body { get; }
    public Actor Actor { get; }
    /// <summary>The figure raised so it stands by its soles on its node (<see cref="Actor.Ground"/>; identity when its origin is its ground).</summary>
    public Matrix4x4 Lift { get; }
    public FigureClips Clips { get; }
    public View3D View { get; }
    public SceneView Scene { get; }

    /// <summary>The playing time (seconds on the clips' cycle, played round).</summary>
    public double Time => _time;

    /// <summary>The view shown (front, three-quarter, side, back, close-up, or a turn in degrees).</summary>
    public string ViewName { get; private set; } = "";

    /// <summary>Shows a view: the figure turned by its turn, the camera framing it.</summary>
    public void ShowView(string view)
    {
        View.Camera = FigureFraming.CameraFor(Spec, view, _restPoints, _focus);
        ViewName = view;
        Turn = FigureFraming.TurnOf(view);
    }

    /// <summary>The figure's turn about Y in degrees (the view's, then the keys'); the camera stays the view's.</summary>
    public float Turn
    {
        get => _turn;
        set
        {
            _turn = value;
            _node.SetLocal(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, value * MathF.PI / 180), Vector3.One);
        }
    }

    /// <summary>Poses the figure at a playing time.</summary>
    public void Seek(double time)
    {
        _time = time;
        Actor.Animator.Seek(Clips.Wrap(time));
        Scene.Advance(0);
    }

    /// <summary>The clip shown and its time, for the caption and the dump's index.</summary>
    public string Describe()
    {
        var (clip, at) = Clips.At(_time);
        return clip < 0 ? "at rest (no clips)" : string.Create(CultureInfo.InvariantCulture, $"{Clips.NameOf(clip)} {at:0.000} s");
    }

    /// <summary>
    /// The scene as Blender's harness reads it (render_scenes.py; Render3DReferenceTests' scene file): the camera, the
    /// look, and the figure as one group at the actor's pose (each joint's deformation in model space, the garments
    /// posed by the same joints by name, the items at their sockets), its files relative to <paramref name="modelFolder"/>.
    /// </summary>
    public object SceneOf(string name, string modelFolder, float scale = 1)
    {
        static float[] V(Vector3 v) => [v.X, v.Y, v.Z];
        static float[][] Columns(Matrix4x4 m) => [[m.M11, m.M12, m.M13, m.M14], [m.M21, m.M22, m.M23, m.M24], [m.M31, m.M32, m.M33, m.M34], [m.M41, m.M42, m.M43, m.M44]];
        string File(string path) => Path.GetRelativePath(modelFolder, Path.GetFullPath(path)).Replace('\\', '/');
        var data = Body.Data;
        var placement = Actor.Placement;
        var world = data.WorldMatrices(Actor.Animator.Pose());
        var rest = data.WorldMatrices();
        var pose = new Dictionary<string, float[]>();
        foreach (var skin in data.Skins)
            foreach (var joint in skin.Joints)
            {
                Matrix4x4.Invert(rest[joint], out var back);
                var m = back * world[joint];
                pose[data.Nodes[joint].Name] = [m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44];
            }
        var anchor = V(placement.Translation);
        var models = new List<object> { Model(File(Spec.Body), placement, pose) };
        models.AddRange(Spec.Garments.Select(g => Model(File(g), placement, pose)));
        models.AddRange(Spec.Items.Select((item, i) => Model(File(item.Model), Actor.Items[i].Socket.ItemTransform(world, placement), null)));
        object Model(string file, Matrix4x4 matrix, Dictionary<string, float[]>? jointPose) => new
        {
            file, matrix = Columns(matrix), scenery = false, pose = jointPose, group = "figure", anchor, objects = (bool?)null, clipTime = (double?)null,
            palette = (Dictionary<string, string>?)null,
        };
        var camera = View.Camera;
        var look = Spec.Look;
        var style = look.Style;
        return new
        {
            name,
            width = Spec.Width,
            height = Spec.Height,
            background = look.Background,
            camera = new
            {
                projection = camera.Projection.ToString().ToLowerInvariant(),
                position = V(camera.Position),
                target = V(camera.Target),
                up = V(camera.Up),
                fieldOfViewDegrees = camera.VerticalFieldOfView * 180 / MathF.PI,
                orthographicHeight = camera.OrthographicHeight,
                near = camera.Near,
                far = camera.Far,
            },
            objects = Array.Empty<object>(),
            models,
            palette = look.Colours,
            shadows = look.Shadows,
            sun = new { direction = V(look.Sun.Direction), strength = look.Sun.Strength, angle = look.Sun.Angle },
            style = new
            {
                ink = Hex(style.Ink),
                shadowMix = style.ShadowMix, bandEdge = style.BandEdge,
                outlineWidth = style.OutlineWidth * scale, innerWidth = style.InnerWidth * scale, creaseAngle = style.CreaseAngle,
                hatchSpacing = style.HatchSpacing, hatchWidth = style.HatchWidth, hatchAngle = style.HatchAngle, hatchInk = style.HatchInk,
                glowWide = style.GlowWide, glowCore = style.GlowCore, glowBoost = style.GlowBoost,
            },
            scale,
            terrain = (object?)null,
            time = 0,
            particles = Array.Empty<object>(),
        };
    }

    /// <summary>An sRGB colour (0–1, as <see cref="Colors.FromHex"/> gives it) as "#rrggbb", back to the look file's text.</summary>
    public static string Hex(Vector4 srgb) => $"#{(int)MathF.Round(srgb.X * 255):x2}{(int)MathF.Round(srgb.Y * 255):x2}{(int)MathF.Round(srgb.Z * 255):x2}";

    /// <summary>A scene file holding these scenes (render_scenes.py's format), the models' files relative to <paramref name="modelFolder"/>, written relative to the file's folder.</summary>
    public static string SceneFile(IEnumerable<object> scenes, string modelFolder, string fileFolder) =>
        JsonSerializer.Serialize(new
        {
            modelFolder = Path.GetRelativePath(fileFolder, modelFolder).Replace('\\', '/'),
            terrainFolder = ".",
            shapes = new Dictionary<string, object>(),
            scenes,
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n";

    public void Dispose()
    {
        View.Dispose();
        foreach (var model in _models)
            model.Dispose();
    }
}
