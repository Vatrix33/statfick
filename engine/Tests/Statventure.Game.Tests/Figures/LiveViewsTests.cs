using System.Numerics;
using Statventure.Animation;
using Statventure.Core;
using Statventure.Game.Data.Looks;
using Statventure.Game.Figures;
using Statventure.Game.Random;
using Statventure.Game.Screens.Races;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The live views of the hero screens (GM2 step 11; Docs/Game/Hero.md, "How it works", step 11; Q31, Q36, Q41, Q57,
/// GM2-P16 as amended): what each screen shows live and when, the figure standing as the figures' state machine says, in
/// the portraits' view and framing, the game's look and the surface's ground; built again on every change of the maker
/// without a flash; paused while covered or hidden and going on where it stopped; let go with its screen and its memory
/// given back; the emblem's live view on a stand-in model of the tests'.
/// </summary>
[Collection("GPU")]
public class LiveViewsTests(GpuFixture gpu)
{
    /// <summary>
    /// The live views' data in <c>Reference/live-view-data/</c> (the human race and the tests' two presets) is what
    /// <see cref="LiveViewData.OverlayFiles"/> makes of the shipped data (written with STATVENTURE_LIVE_VIEWS=write), and
    /// nothing else.
    /// </summary>
    [Fact]
    public void TheLiveViewsDataIsUpToDate()
    {
        var files = LiveViewData.OverlayFiles().ToList();
        if (Environment.GetEnvironmentVariable(LiveViewData.WriteVariable) == "write")
            foreach (var (path, json) in files)
            {
                var file = Path.Combine(LiveViewData.Overlay, path);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, json);
            }
        foreach (var (path, json) in files)
        {
            var file = Path.Combine(LiveViewData.Overlay, path);
            Assert.True(File.Exists(file) && File.ReadAllText(file) == json, $"{path} isn't what the shipped data makes: run the game's tests with {LiveViewData.WriteVariable}=write");
        }
        Assert.Equal(files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Directory.GetFiles(LiveViewData.Overlay, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(LiveViewData.Overlay, f).Replace('\\', '/')).Order(StringComparer.Ordinal));
    }

    static LiveFigure FigureIn(LiveHeroViews views, UiElement surface) =>
        Assert.IsType<LiveFigure>(views.ViewIn(surface));

    /// <summary>
    /// The sheet shows the open hero's figure in its portrait panel, standing: its look, the portraits' turn, the game's
    /// style and sun, the panel's ground, framed for the view's size; it moves with the document's frames as the figures'
    /// state machine plays `stand`; it is let go when the character is left.
    /// </summary>
    [Fact]
    public void TheSheetShowsTheHeroStandingAndLetsItGoWhenItHides()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        var look = LiveViewData.ManLook();
        s.ReadyHero(look);
        var surface = s.Surface.Sheet;
        Assert.Equal(["hero-view"], s.Live);
        var live = FigureIn(s.Sheet, surface);
        Assert.Same(live.View, s.Surfaces.ViewOf(surface));
        Assert.Equal(look, live.Look);
        var actor = live.Actor!;
        // The look's figure (the builder's plan), its actor the built one's model, garments, palette and ground: standing
        // by its soles on the node (GM2-P22: the human's boots 24 mm under the body's origin).
        var plan = HeroFigure.Resolve(data.Data, look);
        Assert.Equal(plan.GarmentModels.Prepend(plan.Body), live.Figure!.Plan.GarmentModels.Prepend(live.Figure.Plan.Body));
        Assert.Equal(["models/humanoid/bodies/human-male-average", "models/humanoid/parts/face/human-male-1/human-male-average"], live.Figure.Plan.GarmentModels.Prepend(live.Figure.Plan.Body).Take(2));
        Assert.Same(live.Figure.Actor.Model, actor.Model);
        Assert.Equal(live.Figure.Actor.Garments, actor.Garments);
        Assert.Equal(LiveViewData.Swatch("skin", look.Colours["skin"]), Hex(actor.Palette!.Slots["skin"].Lit));
        Assert.Equal(live.Figure.Actor.Ground, actor.Ground);
        Assert.InRange(actor.Ground, -0.025f, -0.023f);
        // The portraits' view (the user's Q68: the front turned to the screen's left, −45°, in the game's own sun), the
        // game's style, the panel's ground (#2a1f1a, --portrait-ground), framed as render.py frames the figure.
        Assert.Equal(PortraitLight.FacingLeft, Portraits.Light);
        var style = GameStyle.Load(data.Data);
        Assert.Equal(Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI / 4), live.Node.Rotation);
        Assert.Equal(style.Cel, live.View.Style);
        Assert.Equal(style.SunLight, live.View.Sun);
        Assert.Equal(Colors.LinearFromHex("#2a1f1a"), live.View.Background);
        var box = surface.Box!;
        Assert.Equal(((int)MathF.Round(box.ContentWidth), (int)MathF.Round(box.ContentHeight)), live.Size);
        AssertCamera(Portraits.CameraFor(Portraits.RestPoints(actor), -45, live.Size.Width, live.Size.Height), live.View.Camera);
        // Standing: the figures' machine in `stand`, the pose `stand`'s at the clock's time, the clock the frames'.
        Assert.Equal("stand", actor.Animator.Machine!.StateOf("base"));
        var before = actor.Animator.Time;
        for (var i = 0; i < 30; i++)
            s.H.Frame();
        Assert.Equal(before + 30 / 60.0, actor.Animator.Time, 6);
        var stand = new ClipBinding(AnimationClip.AllOf(data.Data, LiveHeroViews.ClipsPath).Single(c => c.Name == "stand"), live.Figure.Skeleton);
        var (shown, expected) = (actor.Model.Data.WorldMatrices(actor.Animator.Pose()), actor.Model.Data.WorldMatrices(stand.Pose(actor.Animator.Time % stand.Clip.Duration)));
        for (var n = 0; n < shown.Length; n++)
            Assert.True(Vector3.Distance(shown[n].Translation, expected[n].Translation) < 1e-5f, $"{actor.Model.Data.Nodes[n].Name} isn't where stand puts it");
        // It renders when it moved, into the surface's picture.
        s.Render();
        Assert.NotNull(s.Surfaces.Texture(surface));
        var count = live.View.RenderCount;
        s.H.Frame();
        s.Render();
        Assert.Equal(count + 1, live.View.RenderCount);
        // A save of the hero shows the same look: nothing built again, not even its palette; nor where the look started
        // (the maker's display).
        var figure = live.Figure;
        var palette = actor.Palette;
        s.S.Session.Change(save => save with { Hero = save.Hero with { Experience = save.Hero.Experience + 1 } });
        s.H.Frame();
        Assert.Same(figure, FigureIn(s.Sheet, surface).Figure);
        Assert.Same(palette, actor.Palette);
        s.Sheet.ShowFigure(surface, "human", look with { Preset = LiveViewData.Man });
        Assert.Same(figure, FigureIn(s.Sheet, surface).Figure);
        Assert.Same(palette, actor.Palette);
        // Left: the view let go, off the document's frames.
        s.S.Session.Leave();
        Assert.Empty(s.Live);
        Assert.Null(s.Surfaces.ViewOf(surface));
        Assert.Null(live.Figure);
        var stopped = actor.Animator.Time;
        s.H.Wait(0.2);
        Assert.Equal(stopped, actor.Animator.Time);
    }

    /// <summary>
    /// The palette changes in place only when the body and every part stay: other parts on the same body, or another body
    /// with the same parts (another build, which the human's face and hair fit too: step 9a's), build the figure again; a
    /// look the data then can't build lets the shown figure go (no stale figure stays).
    /// </summary>
    [Fact]
    public void OtherPartsOrAnotherBodyBuildAgainAndARefusalLetsTheFigureGo()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        var look = LiveViewData.ManLook();
        s.ReadyHero(look);
        var surface = s.Surface.Sheet;
        var live = FigureIn(s.Sheet, surface);
        var first = live.Figure!;
        // Other parts on the same body: the hair taken off.
        var bald = look with { Parts = new Dictionary<string, string>(look.Parts) { ["hair"] = "hair-none" } };
        s.Sheet.ShowFigure(surface, "human", bald);
        Assert.NotSame(first, live.Figure);
        Assert.Equal(first.Plan.GarmentModels.Count() - 1, live.Figure!.Plan.GarmentModels.Count());
        // Another body with the same parts: the strong build.
        var before = live.Figure;
        s.Sheet.ShowFigure(surface, "human", bald with { Body = "human-male-strong" });
        Assert.NotSame(before, live.Figure);
        Assert.Equal("models/humanoid/bodies/human-male-strong", live.Figure!.Plan.Body);
        Assert.Same(live, s.Sheet.ViewIn(surface));
        // A look the data can't build: the figure shown is let go, the panel its ground.
        s.Sheet.ShowFigure(surface, "human", look with { Parts = new Dictionary<string, string>(look.Parts) { ["hair"] = "hair-gone" } });
        Assert.Empty(s.Live);
        Assert.Null(s.Surfaces.ViewOf(surface));
    }

    /// <summary>A CSS colour as a view's ground: linear and premultiplied by its alpha (a transparent one: none, the medallion's).</summary>
    [Fact]
    public void ASurfacesGroundIsItsColourLinearAndPremultiplied()
    {
        var ground = LiveView.GroundOf(new Statventure.Core.Color(1, 0.5f, 0.2f, 0.5f));
        Assert.Equal(new Vector4(0.5f, Colors.ToLinear(0.5f) * 0.5f, Colors.ToLinear(0.2f) * 0.5f, 0.5f), ground);
        Assert.Equal(Colors.LinearFromHex("#2a1f1a"), LiveView.GroundOf(new Statventure.Core.Color(0x2a / 255f, 0x1f / 255f, 0x1a / 255f)));
    }

    static string Hex(Vector4 linear)
    {
        var srgb = Colors.ToSrgb(linear);
        return $"#{(int)MathF.Round(srgb.X * 255):x2}{(int)MathF.Round(srgb.Y * 255):x2}{(int)MathF.Round(srgb.Z * 255):x2}";
    }

    static void AssertCamera(Camera expected, Camera actual)
    {
        Assert.Equal(expected.Projection, actual.Projection);
        Assert.True(Vector3.Distance(expected.Position, actual.Position) < 1e-5f && Vector3.Distance(expected.Target, actual.Target) < 1e-5f,
            $"camera at {actual.Position} on {actual.Target}, expected {expected.Position} on {expected.Target}");
        Assert.Equal(expected.OrthographicHeight, actual.OrthographicHeight, 5);
        Assert.Equal((expected.Near, expected.Far, expected.Up), (actual.Near, actual.Far, actual.Up));
    }

    /// <summary>A Dev Mode hero of an enemy-only race has no look: the sheet's panel shows its ground alone.</summary>
    [Fact]
    public void AHeroWithNoLookShowsNoFigure()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        s.H.Settings.DevMode = true;
        s.ReadyHero(null, race: "goblin");
        Assert.True(s.S.Sheet!.Showing);
        Assert.Empty(s.Live);
    }

    /// <summary>
    /// The carousel shows the shown race's first preset standing and its emblem rocking (step 10's model); a race with no
    /// look shows no figure, its emblem all the same; taking a race lets both go and the maker shows the look.
    /// </summary>
    [Fact]
    public void TheCarouselShowsTheRacesFigureAndItsEmblemAndLetsThemGo()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        s.S.NewHero();
        s.H.Frame();
        var (_, races, emblem, maker) = s.Surface;
        Assert.Equal(["races-emblem-view", "races-figure"], s.Live);
        Assert.Equal(LiveViewData.Presets[LiveViewData.Man].Look, FigureIn(s.Races, races).Look);
        Assert.Equal(Colors.LinearFromHex("#0f0a07"), FigureIn(s.Races, races).View.Background);
        var turning = Assert.IsType<LiveEmblem>(s.Races.ViewIn(emblem));
        Assert.Equal("human", turning.Emblem);
        Assert.True(s.H.Q("#races-emblem").HasClass("is-live"));
        // The medallion's view is transparent over its own ground.
        Assert.Null(turning.View.Background);
        // The elf: no body (its figure comes with its race's step), its emblem live (step 10's).
        s.S.Key("ArrowRight");
        Assert.Equal("elf", s.S.Races.Shown);
        Assert.Equal(["races-emblem-view"], s.Live);
        Assert.Equal("elf", Assert.IsType<LiveEmblem>(s.Races.ViewIn(emblem)).Emblem);
        Assert.True(s.H.Q("#races-emblem").HasClass("is-live"));
        Assert.Null(s.Surfaces.ViewOf(races));
        s.S.Key("ArrowLeft");
        Assert.Equal(["races-emblem-view", "races-figure"], s.Live);
        // Taken: the carousel's let go, the maker's shown.
        s.S.Click(s.H.Q("#races-choose"));
        Assert.Equal(["maker-figure"], s.Live);
        Assert.Null(s.Surfaces.ViewOf(races));
        Assert.Null(s.Surfaces.ViewOf(emblem));
        Assert.Equal(LiveViewData.Presets[LiveViewData.Man].Look, FigureIn(s.Maker, maker).Look);
    }

    /// <summary>
    /// The maker's figure follows every change in the same view, never a frame without it: a colour changes the palette in
    /// place (no model loaded again); a preset, the sex and the random roll build the figure again, its clock carried on;
    /// the view's draw items are the new figure's at once (the next picture is it).
    /// </summary>
    [Fact]
    public void TheMakersFigureFollowsEveryChangeWithoutAFlash()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data, random: new GameRandom(8));
        s.S.NewHero();
        s.S.Click(s.H.Q("#races-choose"));
        Assert.Equal(["maker-figure"], s.Live);
        var surface = s.Surface.Maker;
        var live = FigureIn(s.Maker, surface);
        var view = live.View;
        var figure = live.Figure;
        for (var i = 0; i < 20; i++)
            s.H.Frame();
        var clock = live.Actor!.Animator.Time;
        Assert.True(clock > 0.3);
        // Each change is drawn at once: the view the surface shows holds the new figure's items before any frame.
        void Drawn()
        {
            Assert.Same(view, s.Surfaces.ViewOf(surface));
            Assert.NotEmpty(view.Items);
            Assert.All(view.Items, item => Assert.Same(live.Actor, item.Group));
        }
        // A click on a control (wherever the choices' list has scrolled it), checked before the frame after it.
        void Press(UiElement control, Action check)
        {
            control.Click();
            check();
            s.H.Frame();
        }
        UiElement Option(string row, string value) => s.S.Row(row).Descendants().First(e => e.GetAttribute("data-value") == value);
        // A colour: the same figure, its palette.
        Press(s.S.Row("colour:cloth").Descendants().First(e => e.HasClass("maker-row-head")), () => { });
        Assert.NotEqual("Rust", live.Look!.Colours["cloth"]);
        Press(Option("colour:cloth", "Rust"), Drawn);
        Assert.Same(figure, live.Figure);
        Assert.Equal(LiveViewData.Swatch("cloth", "Rust"), Hex(live.Actor.Palette!.Slots["cloth"].Lit));
        Assert.Equal("Rust", live.Look!.Colours["cloth"]);
        // A preset of the other sex: built again in the same view, on its clock.
        clock = live.Actor.Animator.Time;
        Press(s.H.Q($"#maker-presets [data-preset=\"{LiveViewData.Woman}\"]"), () =>
        {
            Assert.NotSame(figure, live.Figure);
            Assert.Equal(clock, live.Actor.Animator.Time);
            Drawn();
        });
        Assert.Equal("models/humanoid/bodies/human-female-average", live.Figure!.Plan.Body);
        Assert.Equal(clock + 1 / 60.0, live.Actor.Animator.Time, 6);
        Assert.Equal(LiveViewData.Presets[LiveViewData.Woman].Look, live.Look);
        // Framed again for the new figure's own surface.
        AssertCamera(Portraits.CameraFor(Portraits.RestPoints(live.Actor), Portraits.TurnOf(Portraits.Light), live.Size.Width, live.Size.Height), live.View.Camera);
        // The sex.
        figure = live.Figure;
        Press(Option("sex", "male"), Drawn);
        Assert.NotSame(figure, live.Figure);
        Assert.Equal("models/humanoid/bodies/human-male-average", live.Figure!.Plan.Body);
        // The build (step 9a's three a sex): another body, framed again for it.
        figure = live.Figure;
        Press(Option("build", "strong"), Drawn);
        Assert.NotSame(figure, live.Figure);
        Assert.Equal("models/humanoid/bodies/human-male-strong", live.Figure!.Plan.Body);
        AssertCamera(Portraits.CameraFor(Portraits.RestPoints(live.Actor), Portraits.TurnOf(Portraits.Light), live.Size.Width, live.Size.Height), live.View.Camera);
        // The random roll: whatever it rolled is shown.
        Press(s.H.Q("#maker-random"), Drawn);
        Assert.Equal(s.S.Maker.Look! with { Preset = null }, live.Look);
        // Opening a row changes nothing shown.
        figure = live.Figure;
        Press(s.S.Row("part:hair").Descendants().First(e => e.HasClass("maker-row-head")), () => { });
        Assert.Same(figure, live.Figure);
        // Begun: let go.
        Press(s.H.Q("#maker-begin"), () => { });
        Assert.Equal(["hero-view"], s.Live);
        Assert.Null(s.Surfaces.ViewOf(surface));
    }

    /// <summary>
    /// The views pause while a game window covers the screen (off the document's frames: nothing moves or renders, the last
    /// picture still shown) and go on where they stopped when it closes; the same while the game's window is hidden.
    /// </summary>
    [Fact]
    public void TheViewsPauseWhileCoveredOrHiddenAndGoOnWhereTheyStopped()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        s.S.NewHero();
        var (_, races, emblem, _) = s.Surface;
        var figure = FigureIn(s.Races, races);
        var turning = Assert.IsType<LiveEmblem>(s.Races.ViewIn(emblem));
        s.H.Frame();
        s.Render();
        var (time, angle, renders) = (figure.Actor!.Animator.Time, turning.Seconds, figure.View.RenderCount);
        // A window opens over the carousel.
        var layer = s.H.Layer("covering");
        s.H.Ui.Windows.Open(layer);
        Assert.True(s.Races.Paused);
        Assert.False(figure.Running);
        for (var i = 0; i < 10; i++)
            s.H.Frame();
        s.Render();
        Assert.Equal(time, figure.Actor.Animator.Time);
        Assert.Equal(angle, turning.Seconds);
        Assert.Equal(renders, figure.View.RenderCount);
        Assert.NotNull(s.Surfaces.Texture(races));
        // Closed: from where they stopped (the first frame moves them by nothing).
        s.H.Ui.Windows.Close(layer);
        Assert.False(s.Races.Paused);
        s.H.Frame();
        Assert.Equal(time, figure.Actor.Animator.Time);
        s.H.Frame();
        Assert.Equal(time + 1 / 60.0, figure.Actor.Animator.Time, 6);
        Assert.Equal(angle + 1 / 60.0, turning.Seconds, 6);
        // The game's window hidden (minimized) and shown again.
        time = figure.Actor.Animator.Time;
        s.H.Document.SetHidden(true);
        Assert.True(s.Races.Paused);
        s.H.Wait(1);
        s.H.Document.SetHidden(false);
        s.H.Frame();
        s.H.Frame();
        Assert.Equal(time + 1 / 60.0, figure.Actor.Animator.Time, 6);
        // A view shown while paused waits too (the carousel's keys are the window's then: the views asked directly).
        s.H.Ui.Windows.Open(layer);
        s.Races.Hide(races);
        s.Races.ShowFigure(races, "human", LiveViewData.Presets[LiveViewData.Man].Look);
        var again = FigureIn(s.Races, races);
        Assert.NotSame(figure, again);
        Assert.False(again.Running);
        time = again.Actor!.Animator.Time;
        s.H.Wait(0.2);
        Assert.Equal(time, again.Actor.Animator.Time);
        s.H.Ui.Windows.Close(layer);
        Assert.True(again.Running);
    }

    /// <summary>
    /// At most the views each screen shows (GM2-P16 as amended), through the new hero's flow: the carousel two, the maker
    /// one, the sheet one; none once the character is left, and the GPU memory they held given back.
    /// </summary>
    [Fact]
    public void EachScreenKeepsOnlyTheViewsItShowsAndTheirMemoryIsGivenBack()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        // Once through first: what a first view leaves behind (the frames' upload buffers, the pipelines) is no view's.
        s.S.NewHero();
        s.Render();
        s.S.Session.Leave();
        s.Render();
        var baseline = Allocated();
        s.S.NewHero();
        s.Render();
        Assert.Equal(["races-emblem-view", "races-figure"], s.Live);
        var held = Allocated() - baseline;
        // Each view's target, the figure's and the emblem's models; no shadow atlas (nothing in a portrait or the medallion
        // receives shadows: GM2-P33's atlas made only for a shadow pass).
        Assert.True(held > 8L << 20, $"the carousel's two views hold {held >> 20} MiB");
        Assert.All(new[] { s.Surface.Races, s.Surface.Emblem }, surface => Assert.False(s.Races.ViewIn(surface)!.View.Target!.HasShadowMap));
        // Stepping to a race without looks and back makes the views again: still nothing kept of the first ones.
        s.S.Key("ArrowRight");
        s.S.Key("ArrowLeft");
        s.Render();
        Assert.Equal(held, Allocated() - baseline);
        s.S.Click(s.H.Q("#races-choose"));
        s.Render();
        Assert.Equal(["maker-figure"], s.Live);
        // A figure built again in the maker's view gives back the old one's models.
        s.H.Q($"#maker-presets [data-preset=\"{LiveViewData.Woman}\"]").Click();
        s.H.Frame();
        s.Render();
        s.S.Click(s.H.Q("#maker-begin"));
        s.Render();
        Assert.Equal(["hero-view"], s.Live);
        s.S.Session.Leave();
        s.Render();
        Assert.Empty(s.Live);
        var left = Allocated() - baseline;
        Assert.True(left == 0, $"{left / 1024.0:F0} KiB not given back");
    }

    long Allocated()
    {
        gpu.Device.WaitIdle();
        gpu.Device.Collect();
        Brutal.VulkanApi.Vma.VmaAllocatorEx.CalculateStatistics(gpu.Device.Allocator, out var statistics);
        return (long)statistics.Total.Statistics.AllocationBytes;
    }

    /// <summary>The view follows its surface's size: framed again for it after the layout that changed it.</summary>
    [Fact]
    public void TheViewIsFramedAgainWhenItsSurfaceChangesSize()
    {
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        s.S.NewHero();
        s.S.Click(s.H.Q("#races-choose"));
        var live = FigureIn(s.Maker, s.Surface.Maker);
        var wide = live.Size;
        // Under 1080 px the maker's portrait is 460 px tall (maker.css).
        s.H.Document.SetViewport(1000, 950);
        s.H.Frame();
        var box = s.Surface.Maker.Box!;
        Assert.Equal(((int)MathF.Round(box.ContentWidth), (int)MathF.Round(box.ContentHeight)), live.Size);
        Assert.NotEqual(wide, live.Size);
        AssertCamera(Portraits.CameraFor(Portraits.RestPoints(live.Actor!), Portraits.TurnOf(Portraits.Light), live.Size.Width, live.Size.Height), live.View.Camera);
    }

    /// <summary>The portraits frame a figure as the sandbox's figure mode (render.py) frames the same view.</summary>
    [Theory]
    [InlineData(45, 240, 440)]
    [InlineData(45, 278, 408)]
    [InlineData(-45, 396, 726)]
    [InlineData(45, 600, 200)]
    public void ThePortraitsFrameAsTheFigureModeDoes(float turn, int width, int height)
    {
        using var data = new LiveViewData();
        // With gear on (GM3's): a garment of it and two items on their sockets frame with the figure.
        var gear = new FigureGear
        {
            Garments = ["models/humanoid/gear/chain-shirt/human-male-average"],
            Items = [new FigureItem("models/humanoid/gear/sword", "socket.hand.R"), new FigureItem("models/humanoid/gear/shield", "socket.shield.L")],
        };
        using var figure = HeroFigure.Build(gpu.Device, data.Data, LiveViewData.ManLook(), gear);
        // As the figure mode frames it: standing by its soles (its model space raised by its ground, FigureView's Lift).
        var lift = Matrix4x4.CreateTranslation(0, -figure.Actor.Ground, 0);
        Assert.NotEqual(Matrix4x4.Identity, lift);
        var points = FigureFraming.RestPoints(figure.Actor.Model.Data, lift);
        foreach (var garment in figure.Actor.Garments)
            points.AddRange(FigureFraming.RestPoints(garment.Model.Data, lift));
        var rest = figure.Actor.Model.Data.WorldMatrices();
        foreach (var item in figure.Actor.Items)
            points.AddRange(FigureFraming.RestPoints(item.Model.Data, item.Socket.ItemTransform(rest, lift)));
        Assert.Equal(2, figure.Actor.Items.Count);
        Assert.Equal(points, Portraits.RestPoints(figure.Actor));
        var view = turn == 45 ? "three-quarter" : turn.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var spec = new FigureSpec { Body = "", Look = GameStyleTests.Look, View = view, Width = width, Height = height };
        AssertCamera(FigureFraming.CameraFor(spec, view, points, null), Portraits.CameraFor(points, turn, width, height));
    }

    /// <summary>
    /// The question put to the user (Docs/Game/Hero.md, step 11): each way turns the figure and lights it as it says: (a)
    /// the front to the screen's right in the game's sun, (b) the same with the sun mirrored left to right, (c) the front
    /// to the screen's left in the game's sun.
    /// </summary>
    [Theory]
    [InlineData(PortraitLight.AsPlanned, 45, false)]
    [InlineData(PortraitLight.SunMirrored, 45, true)]
    [InlineData(PortraitLight.FacingLeft, -45, false)]
    public void EachPortraitLightTurnsAndLightsAsItSays(PortraitLight light, float turn, bool mirrored)
    {
        Assert.Equal(turn, Portraits.TurnOf(light));
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data, light: light);
        s.ReadyHero(LiveViewData.ManLook());
        var live = FigureIn(s.Sheet, s.Surface.Sheet);
        Assert.Equal(Quaternion.CreateFromAxisAngle(Vector3.UnitY, turn * MathF.PI / 180), live.Node.Rotation);
        var sun = GameStyle.Load(data.Data).SunLight;
        Assert.Equal(mirrored ? sun with { Direction = new Vector3(-sun.Direction.X, sun.Direction.Y, sun.Direction.Z) } : sun, live.View.Sun);
        AssertCamera(Portraits.CameraFor(Portraits.RestPoints(live.Actor!), turn, live.Size.Width, live.Size.Height), live.View.Camera);
        // The emblem keeps its own light from the front (Q79) whatever the portraits take: the game's sun's strength and
        // angle from 25° left and 30° up.
        s.S.Session.Leave();
        s.S.NewHero();
        var own = Assert.IsType<LiveEmblem>(s.Races.ViewIn(s.Surface.Emblem)).View.Sun;
        Assert.Equal(sun with { Direction = LiveEmblems.LightDirection }, own);
        Assert.Equal(new Vector3(-MathF.Sin(25 * MathF.PI / 180) * MathF.Cos(30 * MathF.PI / 180), 0.5f, MathF.Cos(25 * MathF.PI / 180) * MathF.Cos(30 * MathF.PI / 180)), own.Direction);
    }

    /// <summary>
    /// The user's Q79 ("Lit from the front"): through the whole sway, at every frame of it (60 a second over its 8 s), the
    /// face of each of step 10's nine emblems stays in the lit band. The measure: every vertex of the emblem's model whose
    /// normal at rest is within 20° of the medallion's front (the field, the tops of the motif's outlines and of the rim:
    /// the faces that look at the viewer), turned by the sway, lit by the engine's own rule (lit where the light's diffuse,
    /// its strength / π times the normal's cosine to it, reaches the look's band edge) with a margin of 0.05 of that light;
    /// under the game's sun (the figures') the same faces drop into the shade band on the way to the right.
    /// </summary>
    [Fact]
    public void TheLiveEmblemsFaceStaysLitThroughTheSway()
    {
        using var data = EmblemReferenceTests.Data();
        var style = GameStyle.Load(data);
        var (band, diffuse) = (style.BandEdge, style.SunLight.Strength / MathF.PI);
        var front = MathF.Cos(20 * MathF.PI / 180);
        // Every vertex normal at rest that looks at the viewer, of each emblem.
        var emblems = EmblemReferenceTests.Races(data).Select(r =>
        {
            var model = ModelData.Load(Path.Combine(RaceOracle.Engine, "Assets", "Models", Emblems.ModelPath(r.Race.Emblem!)["models/".Length..] + ".glb"));
            var world = model.WorldMatrices();
            var normals = new List<Vector3>();
            for (var n = 0; n < model.Nodes.Count; n++)
                if (model.Nodes[n].Mesh >= 0)
                    foreach (var primitive in model.Meshes[model.Nodes[n].Mesh].Primitives)
                        normals.AddRange(primitive.Normals.Select(normal => Vector3.Normalize(Vector3.TransformNormal(normal, world[n]))).Where(normal => normal.Z >= front));
            return (Emblem: r.Race.Emblem!, Normals: normals);
        }).ToList();
        Assert.Equal(9, emblems.Count);
        Assert.All(emblems, e => Assert.True(e.Normals.Count > 100, e.Emblem));
        // The least light over the sway's frames above the band edge (negative: in the shade band), and where.
        (double Margin, string Where) Least(Vector3 light)
        {
            var least = (Margin: double.MaxValue, Where: "");
            foreach (var (emblem, normals) in emblems)
                for (var frame = 0; frame <= 480; frame++)
                {
                    var angle = LiveEmblems.AngleAt(frame / 60.0);
                    var turn = Matrix4x4.CreateRotationY((float)(angle * Math.PI / 180));
                    foreach (var normal in normals)
                    {
                        var margin = diffuse * Math.Max(Vector3.Dot(Vector3.TransformNormal(normal, turn), light), 0) - band;
                        if (margin < least.Margin)
                            least = (margin, $"{emblem} at {angle:0.0}°");
                    }
                }
            return least;
        }
        var lit = Least(Vector3.Normalize(LiveEmblems.LightDirection));
        Assert.True(lit.Margin >= 0.05, $"the emblem's own light: the least light {lit.Margin:0.000} above the band edge ({lit.Where})");
        var game = Least(Vector3.Normalize(style.SunLight.Direction));
        Assert.True(game.Margin < 0, $"under the game's sun the faces would stay lit too ({game.Margin:0.000}, {game.Where})");
        // At both ends of the sway and face-on, as Q79 says.
        Assert.Equal(35, LiveEmblems.AngleAt(2), 9);
        Assert.Equal(-35, LiveEmblems.AngleAt(6), 9);
    }

    /// <summary>
    /// The emblem rocks from side to side (the user's Q67): about 35° to each side and back as a sine, one whole sway every
    /// 8 s (the user's Q78), slowest at each side, toward the screen's right first, never further (its back never seen),
    /// about the upright line through its middle, seen by step 10's pictures' camera (Q80); step 10's model of the race's
    /// emblem in its race's colours by step 10's rule; shown only for an emblem whose model is in the data.
    /// </summary>
    [Fact]
    public void TheEmblemRocksSlowlyAndShowsOnlyWithItsModel()
    {
        Assert.Equal(8, LiveEmblems.SecondsPerSway);
        Assert.Equal(0, LiveEmblems.AngleAt(0), 9);
        Assert.Equal(35, LiveEmblems.AngleAt(2), 9);
        Assert.Equal(0, LiveEmblems.AngleAt(4), 9);
        Assert.Equal(-35, LiveEmblems.AngleAt(6), 9);
        Assert.Equal(35 * Math.Sin(Math.PI / 4), LiveEmblems.AngleAt(1), 9);
        Assert.Equal(35 * Math.Sin(Math.PI / 4), LiveEmblems.AngleAt(9), 9);
        Assert.All(Enumerable.Range(0, 801).Select(i => LiveEmblems.AngleAt(i / 100.0)), angle => Assert.InRange(angle, -35, 35));
        using var data = new LiveViewData();
        using var s = new LiveScreens(gpu.Device, data);
        s.S.NewHero();
        var turning = Assert.IsType<LiveEmblem>(s.Races.ViewIn(s.Surface.Emblem));
        for (var i = 0; i < 61; i++)
            s.H.Frame();
        Assert.Equal(1, turning.Seconds, 6);
        Assert.Equal(Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)(35 * Math.Sin(Math.PI / 4) * Math.PI / 180)), turning.Node.Rotation);
        // Step 10's model of the human's emblem, in the human's colours by step 10's rule (its six slots from the accent).
        var shownModel = (PlacedModel)turning.Node.Children.Single().Attachments.Single();
        var human = Statventure.Records.RecordSerializer.FromJson<Statventure.Game.Data.Races.Race>(
            File.ReadAllBytes(Path.Combine(LiveViewData.Overlay, "data", "races", "human.json")));
        Assert.Equal(Emblems.Colours(human.Accent).OrderBy(c => c.Key, StringComparer.Ordinal),
            Emblems.Rule.Select(r => KeyValuePair.Create(r.Slot, Hex(shownModel.Palette!.Slots[r.Slot].Lit))).OrderBy(c => c.Key, StringComparer.Ordinal));
        Assert.Equal(6, shownModel.Palette!.Slots.Count);
        // Shown again for the same emblem: the same view, rocking on.
        Assert.True(s.Races.ShowEmblem(s.Surface.Emblem, "human", "human"));
        Assert.Same(turning, s.Races.ViewIn(s.Surface.Emblem));
        Assert.Equal(1, turning.Seconds, 6);
        // Its middle on the turning axis, its whole sphere in the picture at any turn.
        var model = ModelData.Load(Path.Combine(RaceOracle.Engine, "Assets", "Models", Emblems.ModelPath("human")["models/".Length..] + ".glb"));
        Assert.Equal(model.Nodes.Count, shownModel.Model.Data.Nodes.Count);
        var points = model.RestPoints(Matrix4x4.Identity);
        Vector3 min = points.Aggregate(Vector3.Min), max = points.Aggregate(Vector3.Max);
        var middle = (min + max) / 2;
        var placed = turning.Node.Children.Single();
        Assert.Equal(-middle, placed.Translation);
        // Seen by step 10's pictures' camera (the user's Q80): about 5° up, the medallion whole with a small margin at any turn.
        var camera = turning.View.Camera;
        AssertCamera(Emblems.PictureCamera(), camera);
        Assert.Equal(5, MathF.Atan2(camera.Position.Y, camera.Position.Z) * 180 / MathF.PI, 0);
        var radius = points.Max(p => Vector3.Distance(p, middle));
        Assert.True(2 * radius < camera.OrthographicHeight, "the medallion's sphere inside the picture");
        // The race's palette through the seam, asked with the race's id for each emblem shown.
        var asked = new List<string>();
        var palette = new Palette([KeyValuePair.Create("emblem.rim", "#bb8d3c")]);
        s.Races.EmblemPalette = (_, race) =>
        {
            asked.Add(race);
            return palette;
        };
        s.S.Key("ArrowRight");
        s.S.Key("ArrowLeft");
        Assert.Equal(["elf", "human"], asked);
        var coloured = Assert.IsType<LiveEmblem>(s.Races.ViewIn(s.Surface.Emblem));
        Assert.Same(palette, ((PlacedModel)coloured.Node.Children.Single().Attachments.Single()).Palette);
        // No palette: the model's own colours.
        s.Races.EmblemPalette = null;
        s.S.Key("ArrowRight");
        Assert.Null(((PlacedModel)Assert.IsType<LiveEmblem>(s.Races.ViewIn(s.Surface.Emblem)).Node.Children.Single().Attachments.Single()).Palette);
        // A race whose emblem has a name of its own (a mod's; the tests' shapes as its model): the model by the emblem's
        // name, the colours by the race's id.
        using var renamed = new LiveViewData(emblems: false, extra: new Dictionary<string, object>
        {
            ["data/races/human"] = human with { Emblem = "test-medallion" },
            [Emblems.ModelPath("test-medallion")] = ModelData.Load(LiveViewData.StandInEmblem),
        });
        using var u = new LiveScreens(gpu.Device, renamed);
        var askedFor = new List<string>();
        u.Races.EmblemPalette = (_, race) =>
        {
            askedFor.Add(race);
            return null;
        };
        u.S.NewHero();
        Assert.Equal(["human"], askedFor);
        Assert.Equal("test-medallion", Assert.IsType<LiveEmblem>(u.Races.ViewIn(u.Surface.Emblem)).Emblem);
        // No model for the emblem's name, or no emblem: no view, the placeholder stays.
        using var none = new LiveViewData(emblems: false);
        using var t = new LiveScreens(gpu.Device, none);
        t.S.NewHero();
        Assert.Equal(["races-figure"], t.Live);
        Assert.False(t.H.Q("#races-emblem").HasClass("is-live"));
        Assert.False(t.Races.ShowEmblem(t.Surface.Emblem, "goblin", null));
    }

    /// <summary>
    /// The carousel's medallion shows one thing at a time (step 10's picture, this step's live emblem, the old ✦): a race
    /// whose emblem's model is in the data its live emblem, the picture and the ✦ not drawn (<c>.is-live</c>); a race whose
    /// emblem has pictures but no model (a mod's, or a document whose views can't be live) its picture; a race with no
    /// emblem the ✦. Held in every frame as the window's host draws it (the document's update, then the views' render
    /// before the paint): exactly one of the three is drawn, never two and never none, as the carousel opens, while the
    /// emblem rocks, from race to race (one with an emblem to one without and back, Dev Mode), under a window, and on data
    /// without the emblems' models.
    /// </summary>
    [Fact]
    public void TheMedallionDrawsThePictureOrTheLiveEmblemNeverBothOrNeither()
    {
        static string Drawn(LiveScreens s)
        {
            var picture = s.H.Q("#races-emblem-picture");
            var shown = new List<string>();
            if (picture.Box is { ContentWidth: > 0 } && picture.GetAttribute("src") is { Length: > 0 })
                shown.Add("picture");
            if (s.Surfaces.Texture(s.Surface.Emblem) is not null)
                shown.Add("live");
            if (s.H.Q("#races-emblem .races-emblem-mark").Box is not null)
                shown.Add("✦");
            return string.Join(" and ", shown);
        }
        static void Frames(LiveScreens s, int count, string expected, string when)
        {
            for (var i = 0; i < count; i++)
            {
                s.H.Frame();
                s.Render();
                Assert.True(Drawn(s) == expected, $"{when}, frame {i}: {(Drawn(s) is { Length: > 0 } drawn ? drawn : "nothing")} drawn, not {expected}");
            }
        }
        using var data = new LiveViewData();
        using (var s = new LiveScreens(gpu.Device, data))
        {
            s.H.Settings.DevMode = true;
            s.S.NewHero();
            Frames(s, 30, "live", "the carousel opened on the human");
            s.S.Key("ArrowRight");
            Assert.Equal("elf", s.S.Races.Shown);
            Frames(s, 5, "live", "on to the elf");
            s.S.Click(s.H.Q("#races-strip .races-strip-item[data-race=\"goblin\"]"));
            Frames(s, 5, "✦", "to the goblin (no emblem, Dev Mode)");
            s.S.Click(s.H.Q("#races-strip .races-strip-item[data-race=\"hobbit\"]"));
            Frames(s, 5, "live", "back to the hobbit");
            var layer = s.H.Layer("covering");
            s.H.Ui.Windows.Open(layer);
            Frames(s, 5, "live", "under a window (paused: its last picture)");
            s.H.Ui.Windows.Close(layer);
            Frames(s, 5, "live", "the window closed");
        }
        using var none = new LiveViewData(emblems: false);
        using (var t = new LiveScreens(gpu.Device, none))
        {
            t.S.NewHero();
            Frames(t, 5, "picture", "no emblem models: the human");
            t.S.Key("ArrowRight");
            Frames(t, 5, "picture", "no emblem models: the elf");
        }
    }

    /// <summary>
    /// What a mod could break under the views shows the figure all the same: a figures' machine naming a clip the library
    /// hasn't leaves it standing at rest, and a broken game's look gives the views the engine's own look.
    /// </summary>
    [Fact]
    public void ABrokenMachineOrStyleLeavesTheFigureAtRestInTheEnginesLook()
    {
        var machine = new StateMachineBuilder("figure").Layer("base", Animation.BlendMode.Replace, "stand", l => l.State("stand", "gone", loop: true)).Build();
        using var data = new LiveViewData(extra: new Dictionary<string, object>
        {
            [LiveHeroViews.MachinePath] = machine,
            [GameStyle.Path] = new RenderStyle { Ink = "nothing" },
        });
        using var s = new LiveScreens(gpu.Device, data);
        s.ReadyHero(LiveViewData.ManLook());
        var live = FigureIn(s.Sheet, s.Surface.Sheet);
        Assert.Null(live.Actor!.Animator.Machine);
        s.H.Wait(0.3);
        Assert.Equal(live.Actor.Model.Data.WorldMatrices(), live.Actor.Model.Data.WorldMatrices(live.Actor.Animator.Pose()));
        Assert.Equal(new RenderStyle().Cel, live.View.Style);
        Assert.Equal(new RenderStyle().SunLight, live.View.Sun);
    }

    /// <summary>A data without the figures' machine or clips: the figure still shows, standing at rest.</summary>
    [Fact]
    public void WithoutTheFiguresMachineAFigureStandsAtRest()
    {
        using var data = new LiveViewData(machine: false);
        using var s = new LiveScreens(gpu.Device, data);
        s.ReadyHero(LiveViewData.ManLook());
        var live = FigureIn(s.Sheet, s.Surface.Sheet);
        Assert.Null(live.Actor!.Animator.Machine);
        s.H.Wait(0.5);
        Assert.True(live.Actor.Animator.Time > 0.4);
        var rest = live.Actor.Model.Data.WorldMatrices();
        Assert.Equal(rest, live.Actor.Model.Data.WorldMatrices(live.Actor.Animator.Pose()));
    }
}
