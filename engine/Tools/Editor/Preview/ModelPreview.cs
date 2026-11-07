using System.Numerics;
using Statventure.Animation;
using Statventure.Editor.Session;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>
/// A model's preview (P33): the model in its own colours, instanced in the scene (each top-level object a model of its
/// own, its cameras and sun on their nodes, as a stage is); its clips listed, played, paused, looped and moved along a
/// time slider; the turnable camera framing its rest pose (drag, wheel, Reset view); a stage's cameras to look through
/// (its sun lights it when it has one). A valid change shows the new model at once, the clip, its time and the camera
/// kept where they still exist (P35).
/// </summary>
sealed class ModelPreview(PreviewPart part, EditorEntry entry) : PreviewView(part, entry, PreviewKind.Model)
{
    /// <summary>The camera select's value for the turnable camera.</summary>
    public const string FreeCamera = "";

    /// <summary>The clip select's value for none (the rest pose).</summary>
    public const string NoClip = "";

    ModelData? _last;
    Model? _model;
    ModelInstance? _instance;
    SceneNode? _node;
    AnimationClip[] _clips = [];
    ClipBinding? _binding;
    AnimationStack? _rest;
    UiElement _surface = null!, _cameras = null!, _clipSelect = null!, _play = null!, _loop = null!, _slider = null!, _time = null!;
    string _camera = FreeCamera;

    protected override bool HasLast => _last is not null;

    /// <summary>The model shown: the last valid (null before one).</summary>
    public ModelData? Data => _last;

    /// <summary>The instance in the scene (null without a 3D view).</summary>
    public ModelInstance? Instance => _instance;

    /// <summary>The clip chosen (<see cref="NoClip"/>: the rest pose).</summary>
    public string Clip { get; private set; } = NoClip;

    /// <summary>Seconds into the clip.</summary>
    public double Time { get; private set; }

    public bool Playing { get; private set; }

    public bool Loop { get; private set; } = true;

    /// <summary>The camera looked through: a camera node's name, or <see cref="FreeCamera"/>.</summary>
    public string Camera => _camera;

    protected override void BuildControls()
    {
        _surface = Surface(Root);
        var view = Root.Append("div", "preview-buttons");
        Button(view, "reset-view", "Reset view", () => Part.Viewport?.Orbit.Reset()).SetAttribute("title", "The camera back where it started");
        _cameras = view.Append("select");
        _cameras.SetAttribute("data-preview", "camera");
        _cameras.SetAttribute("title", "Look through one of the model's cameras, or the camera you turn");
        _cameras.AddEventListener("change", _ => LookThrough(_cameras.Value));
        var clip = Line(Root, "Clip", "One of the model's clips (its animations from Blender)");
        _clipSelect = clip.Append("select");
        _clipSelect.SetAttribute("data-preview", "clip");
        _clipSelect.AddEventListener("change", _ => Choose(_clipSelect.Value));
        _play = Button(clip, "play", "Play", () => { if (Playing) Pause(); else Play(); });
        var loop = clip.Append("label", "preview-check");
        _loop = loop.Append("input");
        _loop.SetAttribute("type", "checkbox");
        _loop.SetAttribute("data-preview", "loop");
        _loop.Checked = Loop;
        _loop.AddEventListener("change", _ => Loop = _loop.Checked);
        loop.AppendText("Loop");
        var time = Line(Root, "Time", "Where the clip is");
        _slider = time.Append("input", "preview-slider");
        _slider.SetAttribute("type", "range");
        _slider.SetAttribute("data-preview", "time");
        _slider.SetAttribute("min", "0");
        _slider.SetAttribute("step", "0.01");
        _slider.SetAttribute("max", "0");
        _slider.Value = "0";
        // (A range's change always follows its input: input is enough.)
        _slider.AddEventListener("input", _ => Seek(_slider.ValueAsNumber));
        _time = time.Append("span", "preview-value", Seconds(0));
        Controls();
    }

    protected override string? Use(object value)
    {
        var data = (ModelData)value;
        try
        {
            data.Validate();
        }
        catch (InvalidDataException problem)
        {
            return $"The model isn't valid ({problem.Message})";
        }
        Model? model = null;
        if (Part.Viewport is { } viewport && Part.Gpu is { } gpu)
        {
            try
            {
                model = Model.Create(gpu, data);
            }
            catch (Exception problem) when (problem is ArgumentException or InvalidDataException or InvalidOperationException)
            {
                return $"The model can't be drawn ({problem.Message})";
            }
            var first = _last is null;
            if (first)
                _node = viewport.Begin(_surface);
            var old = _model;
            // The instance's nodes on a node of their own, which goes with them.
            _instance?.Node?.Remove();
            _model = model;
            _instance = _node!.Add("model").Instance(model);
            _rest = new AnimationStack(_instance.Skeleton);
            viewport.Scene.Sun = _instance.Sun;
            if (first)
                viewport.Orbit.Frame(Bounds(data, out var radius), radius);
            // The old model's buffers go once nothing draws it (the scene drawn again without it first).
            viewport.Scene.Refresh();
            old?.Dispose();
        }
        _last = data;
        _clips = AnimationClip.AllOf(data);
        if (Clip != NoClip && !_clips.Any(c => c.Name == Clip))
            Clip = NoClip;
        if (_camera != FreeCamera && !CameraNames(data).Contains(_camera))
            _camera = FreeCamera;
        Bind();
        Controls();
        LookThrough(_camera);
        return null;
    }

    /// <summary>The names of the nodes that carry the model's cameras, in file order.</summary>
    static IEnumerable<string> CameraNames(ModelData data) => data.Nodes.Where(n => n.Camera >= 0).Select(n => n.Name);

    /// <summary>The centre and radius of a ball round the model at rest (its meshes' points where their nodes are).</summary>
    public static Vector3 Bounds(ModelData data, out float radius)
    {
        var worlds = data.WorldMatrices();
        var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (var n = 0; n < data.Nodes.Count; n++)
        {
            if (data.Nodes[n].Mesh is var mesh and >= 0 && mesh < data.Meshes.Count)
                foreach (var primitive in data.Meshes[mesh].Primitives)
                    foreach (var point in primitive.Positions)
                    {
                        // A skinned mesh stands where its joints put it (glTF ignores its node): its bind pose.
                        var placed = primitive.Joints is null ? Vector3.Transform(point, worlds[n]) : point;
                        (min, max) = (Vector3.Min(min, placed), Vector3.Max(max, placed));
                    }
        }
        if (min.X > max.X)
        {
            radius = 1;
            return Vector3.Zero;
        }
        radius = Math.Max((max - min).Length() / 2, 0.01f);
        return (min + max) / 2;
    }

    /// <summary>Looks through the camera on the node <paramref name="name"/>, or turns the free camera (<see cref="FreeCamera"/>).</summary>
    public void LookThrough(string name)
    {
        _camera = name;
        if (Part.Viewport is { } viewport)
            viewport.Scene.Camera = name == FreeCamera ? null : _instance?.Cameras.FirstOrDefault(c => c.Node?.Name == name);
        Controls();
    }

    /// <summary>Chooses a clip (<see cref="NoClip"/>: the rest pose) from its start, not playing.</summary>
    public void Choose(string clip)
    {
        Clip = _clips.Any(c => c.Name == clip) ? clip : NoClip;
        Time = 0;
        Playing = false;
        Bind();
        Apply();
        Controls();
    }

    public void Play()
    {
        if (Clip == NoClip)
            return;
        if (Time >= Duration)
            Time = 0;
        Playing = true;
        Controls();
    }

    public void Pause()
    {
        Playing = false;
        Controls();
    }

    /// <summary>Moves the clip to <paramref name="time"/> seconds (within it).</summary>
    public void Seek(double time)
    {
        if (!double.IsFinite(time))
            return;
        Time = Math.Clamp(time, 0, Duration);
        Apply();
        Controls(slider: false);
    }

    /// <summary>The clip's length in seconds (0 for none).</summary>
    public double Duration => _clips.FirstOrDefault(c => c.Name == Clip)?.Duration ?? 0;

    public override void Frame(double seconds)
    {
        if (Part.Viewport is { } viewport)
            viewport.ApplyOrbit();
        if (!Playing || Clip == NoClip)
            return;
        var duration = Duration;
        Time += seconds;
        if (Time >= duration)
        {
            if (Loop && duration > 0)
                Time %= duration;
            else
            {
                Time = duration;
                Playing = false;
            }
        }
        Apply();
        Controls();
    }

    void Bind() => _binding = _instance is { } instance && Clip != NoClip ? instance.Clip(Clip) : null;

    /// <summary>The instance's nodes at the clip's time, or at rest without a clip.</summary>
    void Apply()
    {
        if (_instance is not { } instance)
            return;
        if (_binding is { } binding)
            instance.Apply(binding, Time);
        else if (_rest is { } rest)
            instance.Apply(rest, 0);
    }

    /// <summary>The controls as the state is: the cameras and clips listed, play or pause, the time.</summary>
    void Controls(bool slider = true)
    {
        Options(_cameras, [(FreeCamera, "Free camera"), .. (_last is { } data ? CameraNames(data) : []).Select(n => (n, n))], _camera);
        _cameras.ToggleAttribute("disabled", _last is null || !CameraNames(_last).Any());
        Options(_clipSelect, [(NoClip, "(none)"), .. _clips.Select(c => (c.Name, c.Name))], Clip);
        _clipSelect.ToggleAttribute("disabled", _clips.Length == 0);
        _play.TextContent = Playing ? "Pause" : "Play";
        _play.ToggleAttribute("disabled", Clip == NoClip);
        _slider.SetAttribute("max", Number(Duration));
        _slider.ToggleAttribute("disabled", Clip == NoClip);
        if (slider)
            _slider.Value = Number(Time);
        _time.TextContent = Seconds(Time);
    }

    /// <summary>A select's options as given (made again only when they differ), <paramref name="selected"/> chosen.</summary>
    static void Options(UiElement select, IReadOnlyList<(string Value, string Text)> options, string selected)
    {
        var now = select.ElementChildren.Select(o => (o.GetAttribute("value") ?? "", o.TextContent)).ToList();
        if (!now.SequenceEqual(options))
        {
            select.ClearChildren();
            foreach (var (value, text) in options)
                select.Append("option", null, text).SetAttribute("value", value);
        }
        select.Value = selected;
    }

    public override bool Space() => false;

    public override void Dispose()
    {
        Part.Viewport?.End();
        _model?.Dispose();
    }
}
