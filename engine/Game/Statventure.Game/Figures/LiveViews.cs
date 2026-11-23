using System.Numerics;
using Statventure.Animation;
using Statventure.Core;
using Statventure.Game.Data.Looks;
using Statventure.Game.Screens.Races;
using Statventure.Game.Ui;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Game.Figures;

// The live 3D views of the game's hero screens (GM2 step 11; Docs/Game/Hero.md, "How it works", step 11): the hero's
// figure in the sheet's portrait panel, the shown race's figure in the race carousel and the look being made in the
// character maker, each standing relaxed (the figures' state machine, `stand` looped: the user's Q57) in the portraits'
// fixed three-quarter view (Q31, Q41), in the game's look, on its surface's ground; and the carousel's one live emblem,
// rocking slowly from side to side (Q36, Q67). A screen keeps only the views it shows (GM2-P16 as amended), each let go
// when it hides.

/// <summary>
/// The emblems' live view (Q36, Q67; GM2 step 11's view of step 10's emblems): the race's emblem model, by the race's
/// <c>emblem</c> name (Q44), rocking slowly from side to side about its upright axis in the carousel's medallion (the
/// user's Q67, 2026-11-20, changing Q36's "slowly turning": about <see cref="Sway"/>° to each side and back, so the motif
/// is always readable and its back never seen; one whole sway, right, left and back, every <see cref="SecondsPerSway"/>
/// seconds: the user's Q78, 2026-11-22). The model is step 10's at <see cref="Emblems.ModelPath"/>
/// (<c>models/humanoid/emblems/&lt;emblem&gt;</c> in <c>models.pak</c>), the medallion's face toward the figures' front
/// (glTF's +Z, the viewer at a sway of 0), its upright Y, its middle at its origin (the view rocks it about the upright
/// line through its box's middle); its colours the race's by step 10's rule (<see cref="LiveHeroViews.EmblemPalette"/>,
/// <see cref="Emblems.PaletteOf(Statventure.Packs.VirtualFileSystem, string)"/>). Eased as a sine (slowest at each side),
/// toward the screen's right first (<see cref="Direction"/>: +1, the right-hand turn about +Y, as the figures'
/// three-quarter turn), seen by step 10's pictures' camera (<see cref="Emblems.PictureCamera"/>: about 5° up, the
/// medallion with a small margin; the user's Q80, 2026-11-22, so the live emblem at rest shows its relief as the picture
/// beside it in the strip; a square surface, as the medallion's), in the game's look under a light of its own from the front
/// (<see cref="SunOf"/>: the user's Q79, 2026-11-22, "Lit from the front"), on the medallion's own ground (the view
/// transparent). Without a model the medallion shows the emblem's picture (step 10's), or the ✦ without one.
/// </summary>
public static class LiveEmblems
{
    /// <summary>How far it rocks to each side, in degrees (the user's Q67: about 35).</summary>
    public const double Sway = 35;

    /// <summary>One whole sway's length in seconds, right, left and back (the user's Q78: 8 s).</summary>
    public const double SecondsPerSway = 8;

    /// <summary>+1: toward the screen's right first (the right-hand turn about +Y); −1 the other way.</summary>
    public const int Direction = 1;

    /// <summary>Its turn in degrees after <paramref name="seconds"/> of rocking from face-on: a sine of <see cref="Sway"/>° over <see cref="SecondsPerSway"/>.</summary>
    public static double AngleAt(double seconds) => Direction * Sway * Math.Sin(2 * Math.PI * seconds / SecondsPerSway);

    /// <summary>
    /// Where its own light comes from (the user's Q79, 2026-11-22, "Lit from the front"): degrees from straight ahead
    /// toward the screen's left, and up. Measured (Docs/Game/Hero.md, "How it works", step 11): of the lights that keep
    /// every face of the nine emblems that looks at the viewer (its normal within 20° of the medallion's front) in the lit
    /// band through the whole sway with a margin of at least 0.05 of the band's diffuse light, the nearest to the game's
    /// sun (46.8° left, 28.7° up), which lights step 10's pictures in the strip beside it: 19.1° from it.
    /// </summary>
    public const float LightAzimuth = 25, LightElevation = 30;

    /// <summary>The direction toward its light (unit; glTF's axes: +X the screen's right, +Y up, +Z toward the viewer).</summary>
    public static Vector3 LightDirection
    {
        get
        {
            var (azimuth, elevation) = (LightAzimuth * MathF.PI / 180, LightElevation * MathF.PI / 180);
            return new Vector3(-MathF.Sin(azimuth) * MathF.Cos(elevation), MathF.Sin(elevation), MathF.Cos(azimuth) * MathF.Cos(elevation));
        }
    }

    /// <summary>The emblem's own light: the game's sun (its strength and angle, the look's) from <see cref="LightDirection"/>; the game's sun stays the figures'.</summary>
    public static SunLight SunOf(SunLight game) => game with { Direction = LightDirection };
}

/// <summary>
/// The game's <see cref="IHeroViews"/> (GM2 step 11), one per screen (<see cref="For"/>): each surface it is asked to fill
/// gets a <see cref="View3D"/> shown through the window's <see cref="SurfaceViews"/>, its scene a frame client of the
/// document. <b>A figure</b> (<see cref="ShowFigure"/>) is the look's <see cref="HeroFigure"/> (GM2 step 5's builder) on
/// a node turned for the portraits (<see cref="Portraits"/>), playing the figures' state machine
/// (<c>models/humanoid/machines/figure</c>, <c>stand</c> looped: Q57) over the family's clip library
/// (<c>models/humanoid/clips</c>), in the game's style and sun (<see cref="GameStyle"/>; the sun as
/// <see cref="Portraits.Light"/> says), on the surface's computed <c>background-color</c> (the sheet's
/// <c>--portrait-ground</c>, the carousel's and the maker's panel's), framed as render.py frames a figure for the
/// surface's laid-out size. Shown again with the same look nothing is built; with other colours on the same body and parts
/// the palette changes in place; with another body or parts the figure is built again into the same view, its clock
/// carried on, so neither the old figure nor an empty view shows between. A look the data can't build (a mod gone under
/// a slot's save: <see cref="LookRefusedException"/>, or a model that doesn't read) is logged and shows nothing; a null
/// look shows nothing. <b>The emblem</b> (<see cref="ShowEmblem"/>): see <see cref="LiveEmblems"/>. <b>Paused</b> (taken
/// off the document's frames, so nothing moves or renders, the last picture still shown) while a game window covers the
/// screen or the game's window is hidden, and taken up again where it stopped; <b>released</b> (<see cref="Hide"/>: the
/// view, its 3D target and the figure's models given back) when the screen lets the surface go.
/// </summary>
public sealed class LiveHeroViews : IHeroViews, IDisposable
{
    /// <summary>The humanoid family's clip library (GM2 step 4b).</summary>
    public const string ClipsPath = "models/humanoid/clips";

    /// <summary>The figures' state machine (GM2 step 4b: <c>stand</c> looped, Q57).</summary>
    public const string MachinePath = "models/humanoid/machines/figure";

    readonly SurfaceViews _surfaces;
    readonly GameWindows? _windows;
    readonly Func<VirtualFileSystem?> _data;
    readonly Dictionary<UiElement, LiveView> _views = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<UiElement, LookHero> _refused = new(ReferenceEqualityComparer.Instance);
    readonly Action<UiElement> _covered;
    readonly Action<UiEvent> _visibility;
    FigureAnimation? _animation;
    (VirtualFileSystem Data, RenderStyle Style)? _style;
    bool _disposed;

    /// <param name="device">The GPU the views render on.</param>
    /// <param name="surfaces">The window's surface views (its <see cref="UiHost.Views"/>).</param>
    /// <param name="document">The game's document: its frames move the views, its visibility pauses them.</param>
    /// <param name="windows">The game's windows (one open covers the screen and pauses its views); null: none.</param>
    /// <param name="data">The game's data the figures, the clips and the emblems are read from (the screen's).</param>
    /// <param name="light">The portraits' facing and light (<see cref="Portraits.Light"/>; the tests give each).</param>
    public LiveHeroViews(GpuDevice device, SurfaceViews surfaces, UiDocument document, GameWindows? windows, Func<VirtualFileSystem?> data,
        PortraitLight light = Portraits.Light)
    {
        Device = device;
        _surfaces = surfaces;
        Document = document;
        _windows = windows;
        _data = data;
        Light = light;
        _covered = _ => Follow();
        _visibility = _ => Follow();
        if (windows is not null)
        {
            windows.Opened += _covered;
            windows.Closed += _covered;
        }
        document.AddEventListener("visibilitychange", _visibility);
    }

    /// <summary>
    /// The views for a screen of the game (<paramref name="parts"/>): live views on its window when it has one (the
    /// program), else <see cref="NoHeroViews"/> (a document without a window: the tests' screens).
    /// </summary>
    public static IHeroViews For(GameParts parts, Func<VirtualFileSystem?> data) =>
        parts.Ui.Host is { } host ? new LiveHeroViews(parts.Game.Gpu, host.Views, parts.Ui.Document, parts.Ui.Windows, data) : NoHeroViews.Instance;

    internal GpuDevice Device { get; }
    internal UiDocument Document { get; }
    internal PortraitLight Light { get; }

    /// <summary>The surfaces with a live view now.</summary>
    public IReadOnlyCollection<UiElement> Live => _views.Keys;

    /// <summary>Whether the views are paused: a game window covers the screen, or the game's window is hidden.</summary>
    public bool Paused => Document.Hidden || _windows?.Top is not null;

    /// <summary>The view in <paramref name="surface"/>, if any (the tests' look at it).</summary>
    internal LiveView? ViewIn(UiElement surface) => _views.GetValueOrDefault(surface);

    public void ShowFigure(UiElement surface, string race, LookHero? look)
    {
        if (_disposed)
            return;
        if (look is null || _data() is not { } data)
        {
            Hide(surface);
            return;
        }
        // The figure is its choices; where the look started (the maker's display) changes nothing of it.
        look = look with { Preset = null };
        // Refused already while the screen shows (asked again on every save of the hero): said once.
        if (_refused.GetValueOrDefault(surface) == look)
            return;
        _refused.Remove(surface);
        var shown = _views.GetValueOrDefault(surface) as LiveFigure;
        if (shown?.Look == look)
            return;
        try
        {
            if (shown?.Look is { } before && before.Body == look.Body && SameParts(before, look))
            {
                // The same models in other colours: the palette in place.
                shown.Recolour(HeroFigure.Resolve(data, look).Colours, look);
                return;
            }
            var figure = HeroFigure.Build(Device, data, look);
            if (shown is not null)
            {
                shown.Use(figure, look, AnimationOf(data));
                return;
            }
            Release(surface);
            shown = new LiveFigure(this, surface);
            shown.Use(figure, look, AnimationOf(data));
            Add(surface, shown);
        }
        catch (Exception problem) when (problem is LookRefusedException or DataFileException)
        {
            Log.Warning("Figures", $"#{surface.Id}: the {race}'s look can't be shown ({problem.Message}): the view stays empty");
            Release(surface);
            _refused[surface] = look;
        }
    }

    static bool SameParts(LookHero a, LookHero b) =>
        a.Parts.Count == b.Parts.Count && a.Parts.All(p => b.Parts.TryGetValue(p.Key, out var part) && part == p.Value);

    public bool ShowEmblem(UiElement surface, string race, string? emblem)
    {
        if (_disposed)
            return false;
        if (emblem is null || _data() is not { } data || !data.Exists(Emblems.ModelPath(emblem)))
        {
            Hide(surface);
            return false;
        }
        if (_views.GetValueOrDefault(surface) is LiveEmblem { } shown && shown.Emblem == emblem)
            return true;
        Model model;
        try
        {
            model = Model.Load(Device, data, Emblems.ModelPath(emblem));
        }
        catch (DataFileException problem)
        {
            Log.Warning("Figures", $"#{surface.Id}: the {race}'s emblem {emblem} can't be shown ({problem.Message}): its placeholder stays");
            Hide(surface);
            return false;
        }
        Release(surface);
        Add(surface, new LiveEmblem(this, surface, emblem, model, EmblemPalette?.Invoke(data, race)));
        return true;
    }

    /// <summary>
    /// The colours of a race's emblem (the data and the race's id; null, or a null palette: the model's own): GM2 step
    /// 10's rule, <see cref="Emblems.PaletteOf(VirtualFileSystem, string)"/> (its six <c>emblem.*</c> slots filled from the
    /// race's accent colour).
    /// </summary>
    public Func<VirtualFileSystem, string, Palette?>? EmblemPalette { get; set; } = Emblems.PaletteOf;

    public void Hide(UiElement surface)
    {
        _refused.Remove(surface);
        Release(surface);
    }

    /// <summary>The view in <paramref name="surface"/> let go: off the window's surfaces and the document's frames, its GPU resources and models given back.</summary>
    void Release(UiElement surface)
    {
        if (!_views.Remove(surface, out var view))
            return;
        _surfaces.Hide(surface);
        if (view.Running)
            Document.RemoveFrameClient(view);
        view.Dispose();
    }

    void Add(UiElement surface, LiveView view)
    {
        _views[surface] = view;
        _surfaces.Show(surface, view.View);
        view.Fit();
        Follow();
    }

    /// <summary>Every view on the document's frames while nothing covers the screens, off them while something does.</summary>
    void Follow()
    {
        var paused = Paused;
        foreach (var view in _views.Values)
        {
            if (view.Running == !paused)
                continue;
            if (paused)
                Document.RemoveFrameClient(view);
            else
                // From where it stopped: a frame client's first frame moves it by nothing.
                Document.AddFrameClient(view);
            view.Running = !paused;
        }
    }

    /// <summary>The game's style in the data (a broken one, a mod's, logged: the engine's own look then).</summary>
    internal RenderStyle StyleOf(VirtualFileSystem? data)
    {
        if (data is null)
            return new RenderStyle();
        if (_style is { } cached && cached.Data == data)
            return cached.Style;
        RenderStyle style;
        try
        {
            style = GameStyle.Load(data);
        }
        catch (DataFileException problem)
        {
            Log.Warning("Figures", $"the game's look can't be read ({problem.Message}): the views take the engine's own");
            style = new RenderStyle();
        }
        _style = (data, style);
        return style;
    }

    internal RenderStyle Style => StyleOf(_data());

    /// <summary>The figures' state machine and clip library in the data (read once; without them, or broken, the figures stand at rest, logged).</summary>
    FigureAnimation AnimationOf(VirtualFileSystem data)
    {
        if (_animation is { } cached && cached.Data == data)
            return cached;
        StateMachineDefinition? machine = null;
        AnimationClip[] clips = [];
        try
        {
            if (!data.Exists(MachinePath) || !data.Exists(ClipsPath))
                Log.Warning("Figures", $"no {MachinePath} or {ClipsPath} in the data: the figures stand at rest");
            else
            {
                machine = StateMachineDefinition.Load(data, MachinePath);
                // With each clip's keyed body (its contacts, GM2-P37), read from the same data.
                clips = AnimationClip.AllOf(data, ClipsPath);
                var missing = machine.Layers.SelectMany(l => l.States).Select(s => s.Clip).OfType<string>().Where(c => clips.All(clip => clip.Name != c)).ToList();
                if (missing.Count > 0)
                {
                    Log.Warning("Figures", $"{MachinePath} plays {string.Join(", ", missing)}, which {ClipsPath} hasn't: the figures stand at rest");
                    machine = null;
                }
            }
        }
        catch (DataFileException problem)
        {
            Log.Warning("Figures", $"the figures' state machine or clips can't be read ({problem.Message}): the figures stand at rest");
            machine = null;
        }
        return (_animation = new FigureAnimation(data, machine, clips));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        foreach (var surface in _views.Keys.ToList())
            Hide(surface);
        _disposed = true;
        if (_windows is not null)
        {
            _windows.Opened -= _covered;
            _windows.Closed -= _covered;
        }
        Document.RemoveEventListener("visibilitychange", _visibility);
    }
}

/// <summary>The figures' state machine and the clips it plays, read from one data (none: the figures stand at rest).</summary>
sealed record FigureAnimation(VirtualFileSystem Data, StateMachineDefinition? Machine, IReadOnlyList<AnimationClip> Clips);

/// <summary>
/// One live view in a surface: its <see cref="View3D"/> and <see cref="SceneView"/>, a frame client of the document while it
/// runs. Each frame it takes its surface's ground (its computed <c>background-color</c>, linear and premultiplied; a
/// transparent one leaves the view transparent) and moves on; after each layout that changed the surface's size (a
/// resize observer), it frames its content for the size the view's picture will have (the content box in device
/// pixels, rounded, as <see cref="SurfaceViews"/> renders it).
/// </summary>
abstract class LiveView : IUiFrameClient, IDisposable
{
    readonly UiResizeObserver _observer;
    (int Width, int Height) _size;

    protected LiveView(LiveHeroViews owner, UiElement surface, string name)
    {
        Owner = owner;
        Surface = surface;
        var style = owner.Style;
        View = new View3D(owner.Device) { Style = style.Cel, Sun = SunOf(style) };
        Scene = new SceneView(View);
        Node = Scene.Root.Add(name);
        _observer = new UiResizeObserver(owner.Document, _ => Fit());
        _observer.Observe(surface);
    }

    protected LiveHeroViews Owner { get; }
    public UiElement Surface { get; }
    public View3D View { get; }
    public SceneView Scene { get; }
    public SceneNode Node { get; }

    /// <summary>Whether it is on the document's frames (not paused).</summary>
    public bool Running { get; set; }

    /// <summary>The size it is framed for (its picture's), (0, 0) before its surface is laid out.</summary>
    public (int Width, int Height) Size => _size;

    /// <summary>The sun it takes from the game's style.</summary>
    protected virtual SunLight SunOf(RenderStyle style) => style.SunLight;

    /// <summary>Its ground, then its framing when its surface's size changed.</summary>
    public void Fit()
    {
        Ground();
        if (Surface.Box is not { } box)
            return;
        var size = ((int)MathF.Round(box.ContentWidth), (int)MathF.Round(box.ContentHeight));
        if (size.Item1 <= 0 || size.Item2 <= 0 || size == _size)
            return;
        _size = size;
        Frame();
    }

    /// <summary>The camera for <see cref="Size"/>.</summary>
    protected abstract void Frame();

    /// <summary>Moves on by <paramref name="seconds"/>.</summary>
    protected abstract void Advance(double seconds);

    void Ground()
    {
        var colour = Surface.Style?.BackgroundColor ?? Color.Transparent;
        Vector4? ground = colour.A <= 0 ? null : GroundOf(colour);
        if (ground != View.Background)
            View.Background = ground;
    }

    /// <summary>A CSS colour (sRGB, straight alpha) as a view's background (linear, premultiplied).</summary>
    internal static Vector4 GroundOf(Color colour)
    {
        var linear = Colors.ToLinear(new Vector4(colour.R, colour.G, colour.B, 1));
        return new Vector4(linear.X * colour.A, linear.Y * colour.A, linear.Z * colour.A, colour.A);
    }

    void IUiFrameClient.AdvanceFrame(double seconds)
    {
        Ground();
        Advance(seconds);
    }

    void IUiFrameClient.DispatchFrameEvents(UiEventLoop loop) => ((IUiFrameClient)Scene).DispatchFrameEvents(loop);

    public virtual void Dispose()
    {
        _observer.Disconnect();
        View.Dispose();
    }
}

/// <summary>
/// A look's figure in a portrait (the sheet's, the carousel's, the maker's): the figure on the node, turned as
/// <see cref="Portraits.Light"/> says, its own actor of the built figure's model, garments and items with an animator
/// playing the figures' state machine; built again into the same view when the look's models change, its clock carried
/// on.
/// </summary>
sealed class LiveFigure : LiveView
{
    HeroFigure? _figure;
    List<Vector3> _points = [];

    public LiveFigure(LiveHeroViews owner, UiElement surface) : base(owner, surface, "figure") =>
        Node.SetLocal(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, Portraits.TurnOf(owner.Light) * MathF.PI / 180), Vector3.One);

    /// <summary>The look shown (its preset left out).</summary>
    public LookHero? Look { get; private set; }

    /// <summary>The figure shown.</summary>
    public HeroFigure? Figure => _figure;

    /// <summary>The actor on the node (the figure's model, garments, items and palette, with the state machine's animator).</summary>
    public Actor? Actor { get; private set; }

    protected override SunLight SunOf(RenderStyle style) => Portraits.SunOf(style.SunLight, Owner.Light);

    /// <summary>Shows <paramref name="figure"/> (the look <paramref name="look"/>) in place of the figure shown, on its clock.</summary>
    public void Use(HeroFigure figure, LookHero look, FigureAnimation animation)
    {
        var time = Actor?.Animator.Time ?? 0;
        if (Actor is not null)
            Node.Detach(Actor);
        _figure?.Dispose();
        _figure = figure;
        var built = figure.Actor;
        var stack = built.Animator.Stack;
        var machine = animation.Machine is { } definition
            ? new StateMachine(definition, stack, clip => new ClipBinding(animation.Clips.First(c => c.Name == clip), figure.Skeleton))
            : null;
        // Standing by its soles on the node as the builder set it (GM2-P22, "the figure's ground").
        var actor = new Actor(built.Model, new Animator(stack, machine)) { Palette = built.Palette, Ground = built.Ground };
        actor.Garments.AddRange(built.Garments);
        actor.Items.AddRange(built.Items);
        actor.Animator.Seek(time);
        Actor = actor;
        Look = look;
        Node.Attach(actor);
        _points = Portraits.RestPoints(actor);
        if (Size.Width > 0)
            Frame();
        Scene.Advance(0);
    }

    /// <summary>The same models in other colours: the palette of <paramref name="colours"/> (the look <paramref name="look"/>).</summary>
    public void Recolour(IReadOnlyDictionary<string, string> colours, LookHero look)
    {
        Actor!.Palette = new Palette(colours);
        Look = look;
        Scene.Refresh();
    }

    protected override void Frame() => View.Camera = Portraits.CameraFor(_points, Portraits.TurnOf(Owner.Light), Size.Width, Size.Height);

    protected override void Advance(double seconds) => Scene.Advance(seconds);

    public override void Dispose()
    {
        base.Dispose();
        _figure?.Dispose();
        _figure = null;
    }
}

/// <summary>
/// A race's emblem rocking in the carousel's medallion (<see cref="LiveEmblems"/>): its model (in its race's palette, if
/// given) on a node placed so the middle of its box is on the rocking node's axis, swayed about +Y, framed straight on
/// round its bounding sphere.
/// </summary>
sealed class LiveEmblem : LiveView
{
    readonly Model _model;
    double _seconds;

    public LiveEmblem(LiveHeroViews owner, UiElement surface, string emblem, Model model, Palette? palette = null) : base(owner, surface, "emblem")
    {
        Emblem = emblem;
        _model = model;
        var points = model.Data.RestPoints(Matrix4x4.Identity);
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        var middle = points.Count == 0 ? Vector3.Zero : (min + max) / 2;
        var placed = Node.Add("model");
        placed.SetLocal(-middle, Quaternion.Identity, Vector3.One);
        placed.Attach(new PlacedModel(model) { Palette = palette });
        Turn();
    }

    /// <summary>The emblem's name.</summary>
    public string Emblem { get; }

    /// <summary>Its own light from the front (Q79), not the game's sun.</summary>
    protected override SunLight SunOf(RenderStyle style) => LiveEmblems.SunOf(style.SunLight);

    /// <summary>How long it has rocked, in seconds.</summary>
    public double Seconds => _seconds;

    /// <summary>Its turn now, in degrees (0 face-on, positive toward the screen's right).</summary>
    public double Angle => LiveEmblems.AngleAt(_seconds);

    void Turn() => Node.SetLocal(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(Angle * Math.PI / 180)), Vector3.One);

    /// <summary>The tests' and the approval pictures' moment: rocked for <paramref name="seconds"/> from its start.</summary>
    internal void Seek(double seconds)
    {
        _seconds = seconds;
        Turn();
        Scene.Advance(0);
    }

    /// <summary>Step 10's pictures' camera (the user's Q80): the same for any size of its square surface.</summary>
    protected override void Frame() => View.Camera = Emblems.PictureCamera();

    protected override void Advance(double seconds)
    {
        _seconds += seconds;
        Turn();
        Scene.Advance(seconds);
    }

    public override void Dispose()
    {
        base.Dispose();
        _model.Dispose();
    }
}
