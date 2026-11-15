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

    /// <summary>The figure's surface points at rest (model space): each mesh's positions where the rest pose puts them.</summary>
    public static List<Vector3> RestPoints(ModelData model, Matrix4x4 placement)
    {
        var world = model.WorldMatrices();
        var points = new List<Vector3>();
        for (var n = 0; n < model.Nodes.Count; n++)
        {
            var node = model.Nodes[n];
            if (node.Mesh < 0)
                continue;
            foreach (var primitive in model.Meshes[node.Mesh].Primitives)
                for (var v = 0; v < primitive.Positions.Length; v++)
                {
                    var p = primitive.Positions[v];
                    if (node.Skin >= 0 && primitive.Joints is { } joints && primitive.Weights is { } weights)
                    {
                        // Skinned: placed by its joints (glTF ignores the mesh node's own transform).
                        var skin = model.Skins[node.Skin];
                        var (j, w) = (joints[v], weights[v]);
                        var sum = Vector3.Zero;
                        foreach (var (joint, weight) in new[] { (j.J0, w.X), (j.J1, w.Y), (j.J2, w.Z), (j.J3, w.W) })
                            if (weight != 0)
                                sum += weight * Vector3.Transform(p, skin.InverseBindMatrices[joint] * world[skin.Joints[joint]]);
                        points.Add(Vector3.Transform(sum, placement));
                    }
                    else
                        points.Add(Vector3.Transform(p, world[n] * placement));
                }
        }
        return points;
    }

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

/// <summary>Clips in turn on one clock (an NLA layer of strips end to end, played round): which clip acts at a time and when, as the stack's layer picks it.</summary>
public sealed class FigureClips
{
    public FigureClips(IReadOnlyList<AnimationClip> clips)
    {
        Clips = clips;
        var start = 0.0;
        Starts = clips.Select(c =>
        {
            var at = start;
            start += Length(c);
            return at;
        }).ToArray();
        Cycle = start;
    }

    public IReadOnlyList<AnimationClip> Clips { get; }
    /// <summary>Each clip's start on the cycle.</summary>
    public IReadOnlyList<double> Starts { get; }
    /// <summary>All the clips once, in seconds (0 without clips).</summary>
    public double Cycle { get; }

    /// <summary>A clip's length on the cycle: its duration, a still clip a frame.</summary>
    public static double Length(AnimationClip clip) => Math.Max(clip.Duration, 1.0 / 60);

    /// <summary>A playing time on the cycle (played round, before 0 too).</summary>
    public double Wrap(double time) => Cycle <= 0 ? 0 : time - Math.Floor(time / Cycle) * Cycle;

    /// <summary>The clip acting at a playing time and its time in it: a clip from its start to its end, ends included (the stack's rule: at a meeting the earlier at its end).</summary>
    public (int Clip, double Time) At(double time)
    {
        if (Clips.Count == 0)
            return (-1, 0);
        var t = Wrap(time);
        for (var i = 0; i < Clips.Count; i++)
            if (t <= Starts[i] + Length(Clips[i]) + AnimationStrip.Epsilon)
                return (i, Math.Min(t - Starts[i], Clips[i].Duration));
        return (Clips.Count - 1, Clips[^1].Duration);
    }

    /// <summary>The layer that plays them so on a skeleton.</summary>
    public AnimationLayer Layer(Skeleton skeleton)
    {
        var layer = new AnimationLayer();
        for (var i = 0; i < Clips.Count; i++)
            layer.Strips.Add(new AnimationStrip(new ClipBinding(Clips[i], skeleton)) { Start = Starts[i] });
        return layer;
    }
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
        Clips = new FigureClips(clips);
        var stack = new AnimationStack(skeleton);
        stack.Layers.Add(Clips.Layer(skeleton));
        Actor = new Actor(Body, new Animator(stack)) { Palette = spec.Look.Palette };
        _restPoints.AddRange(FigureFraming.RestPoints(Body.Data, Matrix4x4.Identity));
        foreach (var path in spec.Garments)
        {
            var garment = Load(device, path);
            Actor.Garments.Add(new ActorGarment(garment, new Garment(garment.Data, skeleton)));
            _restPoints.AddRange(FigureFraming.RestPoints(garment.Data, Matrix4x4.Identity));
        }
        var rest = Body.Data.WorldMatrices();
        foreach (var (path, socketName) in spec.Items)
        {
            var item = Load(device, path);
            var socket = new Socket(skeleton, socketName);
            Actor.Items.Add(new ActorItem(item, socket));
            _restPoints.AddRange(FigureFraming.RestPoints(item.Data, socket.ItemTransform(rest, Matrix4x4.Identity)));
        }
        _focus = FigureFraming.FocusPoint(Body.Data, spec.Focus ?? "head");
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
        return clip < 0 ? "at rest (no clips)" : $"{Clips.Clips[clip].Name} {at:0.000} s";
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
        var placement = _node.World;
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
