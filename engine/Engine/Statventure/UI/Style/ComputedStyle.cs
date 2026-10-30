using System.Numerics;
using Statventure.Core;
using Statventure.UI.Css;

namespace Statventure.UI.Style;

public enum Display { None, Block, Inline, InlineBlock, Flex, InlineFlex, Grid, InlineGrid, Contents, ListItem, WebkitBox }
public enum PositionKind { Static, Relative, Absolute, Fixed, Sticky }
public enum BoxSizing { ContentBox, BorderBox }
public enum Overflow { Visible, Hidden, Clip, Scroll, Auto }
public enum FlexDirection { Row, RowReverse, Column, ColumnReverse }
public enum FlexWrap { NoWrap, Wrap, WrapReverse }
public enum Alignment { Auto, Normal, Stretch, Start, End, FlexStart, FlexEnd, Center, Baseline, LastBaseline, SpaceBetween, SpaceAround, SpaceEvenly, Left, Right, SelfStart, SelfEnd }
public enum Visibility { Visible, Hidden, Collapse }
public enum PointerEvents { Auto, None }
public enum TextAlignment { Start, Left, Center, Right, Justify, End }
public enum TextOverflow { Clip, Ellipsis }
public enum DecorationStyle { Solid, Double, Dotted, Dashed, Wavy }
/// <summary>The resize property (auto computes to both, as Chrome makes text areas resizable).</summary>
public enum ResizeKind { None, Both, Horizontal, Vertical, Block, Inline }
public enum TextDecorationSkipInk { Auto, None, All }
/// <summary>text-underline-position: auto (none set), from-font, under, left, right.</summary>
[Flags]
public enum UnderlinePosition { Auto = 0, FromFont = 1, Under = 2, Left = 4, Right = 8 }
public enum DecorationThicknessKind { Auto, FromFont, Length }

/// <summary>text-decoration-thickness: auto, from-font, or a length (a percentage is of the font size).</summary>
public readonly record struct DecorationThickness(DecorationThicknessKind Kind, Length Length)
{
    public static readonly DecorationThickness Auto = new(DecorationThicknessKind.Auto, Length.Px(0));
}

/// <summary>
/// A decoration a box's text draws (Blink's AppliedTextDecoration): the lines one decorating box
/// set, with its style, its resolved colour, thickness and underline offset.
/// </summary>
public sealed record AppliedDecoration(bool Underline, bool Overline, bool LineThrough, DecorationStyle Style, Color Color, DecorationThickness Thickness, Length UnderlineOffset);
public enum VerticalAlignKind { Baseline, Middle, Top, Bottom, TextTop, TextBottom, Sub, Super, Length }
public enum ObjectFit { Fill, Contain, Cover, None, ScaleDown }
/// <summary>transform-box: the reference box of transform and transform-origin (SVG elements use the fill, stroke or view box).</summary>
public enum TransformBox { ContentBox, BorderBox, FillBox, StrokeBox, ViewBox }
public enum CssBlendMode { Normal, Multiply, Screen, Overlay, Darken, Lighten, ColorDodge, ColorBurn, HardLight, SoftLight, Difference, Exclusion, Hue, Saturation, Color, Luminosity, PlusLighter }
public enum GridAutoFlow { Row, Column, RowDense, ColumnDense }
public enum AnimationDirection { Normal, Reverse, Alternate, AlternateReverse }
public enum AnimationFillMode { None, Forwards, Backwards, Both }
/// <summary>A border or outline line style; Auto is outline-style: auto (the focus ring), which borders don't take.</summary>
public enum BorderLineStyle { None, Hidden, Solid, Dashed, Dotted, Double, Groove, Ridge, Inset, Outset, Auto }
public enum BackgroundRepeat { Repeat, RepeatX, RepeatY, NoRepeat, Space, Round }
public enum UserSelect { Auto, None, Text, All }

/// <summary>CSS UI 4's interactivity: inert makes the subtree inert (the inert attribute sets it from the UA sheet).</summary>
public enum Interactivity { Auto, Inert }
public enum ScrollBehavior { Auto, Smooth }

/// <summary>overscroll-behavior: whether scrolling chains on to the next scroller at the end of this one.</summary>
public enum OverscrollBehavior { Auto, Contain, None }
public enum LineHeightKind { Normal, Number, Length }

public readonly record struct LineHeight(LineHeightKind Kind, float Value)
{
    public static readonly LineHeight Normal = new(LineHeightKind.Normal, 0);
}

public readonly record struct Corner(Length X, Length Y);

/// <summary>A CSS easing function: keywords as cubic Béziers, or steps().</summary>
public sealed record TimingFunction(float X1, float Y1, float X2, float Y2, int Steps = 0, bool JumpStart = false)
{
    public static readonly TimingFunction Linear = new(0, 0, 1, 1);
    public static readonly TimingFunction Ease = new(0.25f, 0.1f, 0.25f, 1f);
    public static readonly TimingFunction EaseIn = new(0.42f, 0f, 1f, 1f);
    public static readonly TimingFunction EaseOut = new(0f, 0f, 0.58f, 1f);
    public static readonly TimingFunction EaseInOut = new(0.42f, 0f, 0.58f, 1f);

    /// <summary>The eased progress at input progress t (clamped to 0..1).</summary>
    public float Apply(float t) => (float)Evaluate(Math.Clamp(t, 0, 1), before: false);

    /// <summary>
    /// The eased progress at <paramref name="x"/>, which may lie outside 0..1 (Web Animations'
    /// keyframe easing after an overshooting effect easing): gfx's CubicBezier::Solve (a Bézier
    /// extends along its end gradients) and StepsTimingFunction::GetValue (<paramref name="before"/>
    /// is the left limit at a step).
    /// </summary>
    public double Evaluate(double x, bool before)
    {
        if (Steps > 0)
        {
            double steps = Steps;
            var step = Math.Floor(steps * x + (JumpStart ? 1 : 0));
            if (before && steps * x - Math.Floor(steps * x) == 0)
                step -= 1;
            if (x >= 0 && step < 0)
                step = 0;
            if (x <= 1 && step > Steps)
                step = Steps;
            return step / Steps;
        }
        if (this == Linear || (X1 == Y1 && X2 == Y2))
            return x;
        return Beziers.GetOrAdd((X1, Y1, X2, Y2), k => new CubicBezier(k.X1, k.Y1, k.X2, k.Y2)).Solve(x);
    }

    // (Not a field: a record's equality would include it.)
    static readonly System.Collections.Concurrent.ConcurrentDictionary<(float X1, float Y1, float X2, float Y2), CubicBezier> Beziers = new();

    /// <summary>gfx::CubicBezier: Newton's method from spline samples, then bisection; outside 0..1 the end gradients.</summary>
    sealed class CubicBezier
    {
        const double Epsilon = 1e-7;
        const int SplineSamples = 11;
        const int MaxNewtonIterations = 4;
        readonly double _ax, _bx, _cx, _ay, _by, _cy, _startGradient, _endGradient;
        readonly double[] _samples = new double[SplineSamples];

        public CubicBezier(double p1x, double p1y, double p2x, double p2y)
        {
            _cx = 3.0 * p1x;
            _bx = 3.0 * (p2x - p1x) - _cx;
            _ax = 1.0 - _cx - _bx;
            _cy = 3.0 * p1y;
            _by = 3.0 * (p2y - p1y) - _cy;
            _ay = 1.0 - _cy - _by;
            if (p1x > 0)
                _startGradient = p1y / p1x;
            else if (p1y == 0 && p2x > 0)
                _startGradient = p2y / p2x;
            else if (p1y == 0 && p2y == 0)
                _startGradient = 1;
            else
                _startGradient = 0;
            if (p2x < 1)
                _endGradient = (p2y - 1) / (p2x - 1);
            else if (p2y == 1 && p1x < 1)
                _endGradient = (p1y - 1) / (p1x - 1);
            else if (p2y == 1 && p1y == 1)
                _endGradient = 1;
            else
                _endGradient = 0;
            const double delta = 1.0 / (SplineSamples - 1);
            for (var i = 0; i < SplineSamples; i++)
                _samples[i] = SampleX(i * delta);
        }

        double SampleX(double t) => ((_ax * t + _bx) * t + _cx) * t;
        double SampleY(double t) => ((_ay * t + _by) * t + _cy) * t;
        double DerivativeX(double t) => (3.0 * _ax * t + 2.0 * _bx) * t + _cx;

        public double Solve(double x)
        {
            if (x < 0.0)
                return _startGradient * x;
            if (x > 1.0)
                return 1.0 + _endGradient * (x - 1.0);
            return SampleY(SolveX(x));
        }

        double SolveX(double x)
        {
            double t0 = 0, t1 = 0, t2 = x, x2 = 0;
            const double delta = 1.0 / (SplineSamples - 1);
            for (var i = 1; i < SplineSamples; i++)
                if (x <= _samples[i])
                {
                    t1 = delta * i;
                    t0 = t1 - delta;
                    t2 = t0 + (t1 - t0) * (x - _samples[i - 1]) / (_samples[i] - _samples[i - 1]);
                    break;
                }
            for (var i = 0; i < MaxNewtonIterations; i++)
            {
                x2 = SampleX(t2) - x;
                if (Math.Abs(x2) < Epsilon)
                    return t2;
                var d2 = DerivativeX(t2);
                if (Math.Abs(d2) < Epsilon)
                    break;
                t2 -= x2 / d2;
            }
            if (Math.Abs(x2) < Epsilon)
                return t2;
            while (t0 < t1)
            {
                x2 = SampleX(t2);
                if (Math.Abs(x2 - x) < Epsilon)
                    return t2;
                if (x > x2)
                    t0 = t2;
                else
                    t1 = t2;
                t2 = (t1 + t0) * .5;
            }
            return t2;
        }
    }
}

public sealed record TransitionSpec(string Property, float Duration, TimingFunction Timing, float Delay);

/// <summary>A cursor image: its url and, when given, its hotspot (in image pixels).</summary>
public sealed record CursorImage(string Url, System.Numerics.Vector2? Hotspot);

public sealed record AnimationSpec(string Name, float Duration, TimingFunction Timing, float Delay, float IterationCount, AnimationDirection Direction, AnimationFillMode FillMode, bool Paused);

public sealed record ShadowValue(float OffsetX, float OffsetY, float Blur, float Spread, Color Color, bool Inset);

public readonly record struct ColorStop(Color Color, Length? Position);

public enum ImageKind { None, Url, LinearGradient, RadialGradient }

/// <summary>A background image: a url or a gradient (geometry resolved at paint time against the box).</summary>
public sealed record ImageValue
{
    public ImageKind Kind { get; init; }
    public string Url { get; init; } = "";
    public bool Repeating { get; init; }
    public ColorStop[] Stops { get; init; } = [];
    /// <summary>Linear: the angle in degrees (0 = to top), or a corner direction.</summary>
    public float Angle { get; init; } = 180f;
    public bool CornerRight { get; init; }
    public bool CornerBottom { get; init; }
    public bool UsesCorner { get; init; }
    /// <summary>Radial: circle (else ellipse), size keyword or explicit radii, centre.</summary>
    public bool Circle { get; init; }
    public string SizeKeyword { get; init; } = "farthest-corner";
    public Length RadiusX { get; init; } = Length.Auto;
    public Length RadiusY { get; init; } = Length.Auto;
    public Length CenterX { get; init; } = Length.Pct(50);
    public Length CenterY { get; init; } = Length.Pct(50);

    public static readonly ImageValue None = new();
}

public readonly record struct BackgroundSize(Length X, Length Y, string Keyword)
{
    public static readonly BackgroundSize Auto = new(Length.Auto, Length.Auto, "");
}

public readonly record struct BackgroundPosition(Length X, Length Y)
{
    public static readonly BackgroundPosition TopLeft = new(Length.Pct(0), Length.Pct(0));
}

public sealed record BackgroundLayer(ImageValue Image, Length SizeX, Length SizeY, string SizeKeyword, Length PositionX, Length PositionY, BackgroundRepeat RepeatX, BackgroundRepeat RepeatY)
{
    public bool Fixed { get; init; }

    public static BackgroundLayer Of(ImageValue image) => new(image, Length.Auto, Length.Auto, "", Length.Pct(0), Length.Pct(0), BackgroundRepeat.Repeat, BackgroundRepeat.Repeat);
}

public enum TransformKind { Translate, Scale, Rotate, Skew, Matrix }

public sealed record TransformFunction(TransformKind Kind, Length X, Length Y, float A = 0, float B = 0, float[]? Matrix = null)
{
    /// <summary>The 2D matrix for a box of this size (translate percentages refer to the box).</summary>
    public Matrix3x2 ToMatrix(Vector2 boxSize) => Kind switch
    {
        TransformKind.Translate => Matrix3x2.CreateTranslation(X.ResolveOr(boxSize.X, 0), Y.ResolveOr(boxSize.Y, 0)),
        TransformKind.Scale => Matrix3x2.CreateScale(A, B),
        TransformKind.Rotate => Matrix3x2.CreateRotation(A * MathF.PI / 180f),
        TransformKind.Skew => Matrix3x2.CreateSkew(A * MathF.PI / 180f, B * MathF.PI / 180f),
        _ => new Matrix3x2(Matrix![0], Matrix[1], Matrix[2], Matrix[3], Matrix[4], Matrix[5]),
    };
}

public enum FilterFunctionKind { Blur, DropShadow, Brightness, Contrast, Saturate, Grayscale, Sepia, HueRotate, Invert, Opacity }

public sealed record FilterFunction(FilterFunctionKind Kind, float Amount, float OffsetX = 0, float OffsetY = 0, Color Color = default);

public enum TrackKind { Length, Fraction, MinMax, Auto, MinContent, MaxContent, FitContent }

public sealed record TrackSize(TrackKind Kind, Length Length = default, float Fraction = 0, TrackSize? Min = null, TrackSize? Max = null)
{
    public static readonly TrackSize AutoTrack = new(TrackKind.Auto);
}

/// <summary>A grid template: tracks, with an optional auto-fill/auto-fit repetition inserted at <see cref="RepeatIndex"/>.</summary>
public sealed record GridTemplate(TrackSize[] Tracks, string[][] LineNames, TrackSize[]? AutoRepeat = null, bool AutoFit = false, int RepeatIndex = 0)
{
    public static readonly GridTemplate None = new([], []);
    public bool IsNone => Tracks.Length == 0 && AutoRepeat is null;
}

public readonly record struct GridLine(int Line, int Span, string? Name)
{
    public static readonly GridLine Auto = new(0, 0, null);
    public bool IsAuto => Line == 0 && Span == 0 && Name is null;
}

public sealed record ContentItem(string Text, string? Attribute = null);

public sealed record ClipInset(Length Top, Length Right, Length Bottom, Length Left, Corner[] Radii);

/// <summary>
/// An element's computed style: every property the engine supports, typed. Percentages that depend
/// on layout stay as <see cref="Length"/> percentages; everything else is resolved to pixels.
/// </summary>
public sealed class ComputedStyle
{
    // Box
    public Display Display = Display.Inline;
    public PositionKind Position = PositionKind.Static;
    public BoxSizing BoxSizing = BoxSizing.ContentBox;
    public Length Width = Length.Auto, Height = Length.Auto;
    public Length MinWidth = Length.Auto, MinHeight = Length.Auto;
    public Length MaxWidth = Length.None, MaxHeight = Length.None;
    public Length MarginTop, MarginRight, MarginBottom, MarginLeft;
    public Length PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;
    public float BorderTopWidth = 3, BorderRightWidth = 3, BorderBottomWidth = 3, BorderLeftWidth = 3;
    public BorderLineStyle BorderTopStyle, BorderRightStyle, BorderBottomStyle, BorderLeftStyle;
    public Color? BorderTopColor, BorderRightColor, BorderBottomColor, BorderLeftColor;
    public Corner RadiusTopLeft, RadiusTopRight, RadiusBottomRight, RadiusBottomLeft;
    public Length Top = Length.Auto, Right = Length.Auto, Bottom = Length.Auto, Left = Length.Auto;
    public float AspectRatio;
    public int? ZIndex;
    public Overflow OverflowX = Overflow.Visible, OverflowY = Overflow.Visible;

    // Flex and grid
    public FlexDirection FlexDirection = FlexDirection.Row;
    public FlexWrap FlexWrap = FlexWrap.NoWrap;
    public float FlexGrow;
    public float FlexShrink = 1;
    public Length FlexBasis = Length.Auto;
    public Alignment JustifyContent = Alignment.Normal;
    public Alignment AlignItems = Alignment.Normal;
    public Alignment AlignSelf = Alignment.Auto;
    public Alignment AlignContent = Alignment.Normal;
    public Alignment JustifyItems = Alignment.Normal;
    public Alignment JustifySelf = Alignment.Auto;
    public int Order;
    public Length RowGap = Length.None, ColumnGap = Length.None;
    public GridTemplate GridTemplateColumns = GridTemplate.None;
    public GridTemplate GridTemplateRows = GridTemplate.None;
    public string[]? GridTemplateAreas;
    public GridAutoFlow GridAutoFlow = GridAutoFlow.Row;
    public TrackSize[] GridAutoColumns = [TrackSize.AutoTrack];
    public TrackSize[] GridAutoRows = [TrackSize.AutoTrack];
    public GridLine GridColumnStart = GridLine.Auto, GridColumnEnd = GridLine.Auto;
    public GridLine GridRowStart = GridLine.Auto, GridRowEnd = GridLine.Auto;

    // Text (inherited)
    public Color Color = Color.Black;
    public string[] FontFamilies = ["serif"];
    /// <summary>The computed font size (device pixels: <see cref="SpecifiedFontSize"/> times the zoom).</summary>
    public float FontSize = 16;
    /// <summary>The font size in CSS px before the zoom (Blink's SpecifiedFontSize): em, rem and relative font sizes refer to it.</summary>
    public float SpecifiedFontSize = 16;
    /// <summary>Device pixels per CSS px for this element (Blink's EffectiveZoom): lengths here are already zoomed.</summary>
    public float Zoom = 1;
    /// <summary>Text is positioned at subpixels on this screen (Chromium's FontRenderStyle; changes how font metrics round on Linux).</summary>
    public bool SubpixelPositioning;
    public int FontWeight = 400;
    public bool FontItalic;
    public LineHeight LineHeight = LineHeight.Normal;
    public float LetterSpacing;
    public float WordSpacing;
    public TextAlignment TextAlign = TextAlignment.Start;
    public Text.TextTransform TextTransform;
    public Text.WhiteSpace WhiteSpace = Text.WhiteSpace.Normal;
    public Text.OverflowWrap OverflowWrap = Text.OverflowWrap.Normal;
    public ShadowValue[] TextShadows = [];
    public Visibility Visibility = Visibility.Visible;
    public PointerEvents PointerEvents = PointerEvents.Auto;
    public string Cursor = "auto";
    /// <summary>The cursor's images before its keyword (cursor: url(...) x y, ...), tried in order.</summary>
    public CursorImage[] CursorImages = [];
    public UserSelect UserSelect = UserSelect.Auto;
    public Interactivity Interactivity = Interactivity.Auto;
    /// <summary>
    /// Blink's IsCSSInert: interactivity: inert was applied here or on an ancestor (auto below
    /// doesn't undo it). Inert content isn't hit, selected or focused.
    /// </summary>
    public bool IsInert;

    /// <summary>ComputedStyle::UsedPointerEvents: none when inert.</summary>
    public PointerEvents UsedPointerEvents => IsInert ? PointerEvents.None : PointerEvents;

    /// <summary>ComputedStyle::UsedUserSelect: none when inert.</summary>
    public UserSelect UsedUserSelect => IsInert ? UserSelect.None : UserSelect;
    public Color? AccentColor;
    public Color? CaretColor;
    public bool TabularNumbers;
    public UnderlinePosition TextUnderlinePosition;
    public TextDecorationSkipInk TextDecorationSkipInk;
    /// <summary>scrollbar-color (null: the platform's).</summary>
    public Color? ScrollbarThumb, ScrollbarTrack;
    public string ListStyleType = "disc";
    public bool ListStyleInside;

    // Text (not inherited)
    public TextOverflow TextOverflow = TextOverflow.Clip;
    public ResizeKind Resize;
    public bool Underline, LineThrough, Overline;
    public Color? TextDecorationColor;
    public DecorationStyle TextDecorationStyle;
    public DecorationThickness TextDecorationThickness = DecorationThickness.Auto;
    public Length TextUnderlineOffset = Length.Auto;
    /// <summary>text-decoration-line: blink (it draws nothing, but makes the box a decorating box).</summary>
    public bool DecorationBlink;
    /// <summary>The decorations propagated from the layout parent (Blink's base text decoration data; none where propagation stops).</summary>
    public AppliedDecoration[] BaseDecorations = [];
    /// <summary>The decorations in effect on this box's text: the propagated ones, then its own (set by <see cref="Finish"/>).</summary>
    public AppliedDecoration[] AppliedDecorations = [];

    /// <summary>The box sets decoration lines (Blink's IsDecoratingBox).</summary>
    public bool IsDecoratingBox => Underline || Overline || LineThrough || DecorationBlink;
    public int LineClamp;
    public VerticalAlignKind VerticalAlign = VerticalAlignKind.Baseline;
    public Length VerticalAlignLength;

    // Visual
    public float Opacity = 1;
    public Color BackgroundColor = Color.Transparent;
    public ImageValue[] BackgroundImages = [ImageValue.None];
    public BackgroundSize[] BackgroundSizes = [BackgroundSize.Auto];
    public BackgroundPosition[] BackgroundPositions = [BackgroundPosition.TopLeft];
    public (BackgroundRepeat X, BackgroundRepeat Y)[] BackgroundRepeats = [(BackgroundRepeat.Repeat, BackgroundRepeat.Repeat)];
    /// <summary>background-attachment per layer: fixed layers are positioned against the viewport.</summary>
    public bool[] BackgroundFixed = [false];
    /// <summary>The background layers that have an image, bottom layer last (built by <see cref="Finish"/>).</summary>
    public BackgroundLayer[] Backgrounds = [];
    public ShadowValue[] BoxShadows = [];
    public TransformFunction[] Transform = [];
    public Length TransformOriginX = Length.Pct(50), TransformOriginY = Length.Pct(50);
    public TransformBox TransformBox = TransformBox.ViewBox;
    public Length TranslateX, TranslateY;
    public float ScaleX = 1, ScaleY = 1;
    public float Rotate;
    public FilterFunction[] Filters = [];
    public FilterFunction[] BackdropFilters = [];
    public CssBlendMode MixBlendMode = CssBlendMode.Normal;
    public bool Isolate;
    /// <summary>will-change: the property names (empty for auto).</summary>
    public string[] WillChange = [];
    public ClipInset? ClipPath;
    public float OutlineWidth = 3;
    public BorderLineStyle OutlineStyle = BorderLineStyle.None;
    public Color? OutlineColor;
    public float OutlineOffset;
    public ObjectFit ObjectFit = ObjectFit.Fill;
    /// <summary>appearance: auto, none, textfield, menulist-button ...</summary>
    public string Appearance = "auto";
    /// <summary>An author (not the user agent) set a background or border property (Blink then drops a control's native look).</summary>
    public bool HasAuthorBackground, HasAuthorBorder;
    /// <summary>An author set the border radius (always so for elements other than controls), so a focus ring follows the radii.</summary>
    public bool HasAuthorBorderRadius;

    /// <summary>Some corner is rounded.</summary>
    public bool HasBorderRadius => !(RadiusTopLeft.X.IsZero && RadiusTopLeft.Y.IsZero && RadiusTopRight.X.IsZero && RadiusTopRight.Y.IsZero
        && RadiusBottomRight.X.IsZero && RadiusBottomRight.Y.IsZero && RadiusBottomLeft.X.IsZero && RadiusBottomLeft.Y.IsZero);
    public bool AppearanceNone => Appearance == "none";
    public ScrollBehavior ScrollBehavior = ScrollBehavior.Auto;
    public string ScrollbarWidth = "auto";
    public ContentItem[]? Content;
    /// <summary>clip: rect(top, right, bottom, left) for absolutely positioned boxes; null edges are auto.</summary>
    public float?[]? ClipRect;
    /// <summary>scroll-margin: the area around the box that scrolling into view also reveals.</summary>
    public float ScrollMarginTop, ScrollMarginRight, ScrollMarginBottom, ScrollMarginLeft;
    /// <summary>scroll-padding of a scroller: the edges of its scrollport that scrolling into view keeps clear (auto is 0).</summary>
    public Length ScrollPaddingTop, ScrollPaddingRight, ScrollPaddingBottom, ScrollPaddingLeft;
    public OverscrollBehavior OverscrollBehaviorX, OverscrollBehaviorY;
    public bool ScrollbarGutterStable;

    // Animation: the longhand lists, combined by Finish.
    public string[] TransitionProperty = ["all"];
    public float[] TransitionDuration = [0];
    public TimingFunction[] TransitionTiming = [TimingFunction.Ease];
    public float[] TransitionDelay = [0];
    public string[] AnimationName = ["none"];
    public float[] AnimationDuration = [0];
    public TimingFunction[] AnimationTiming = [TimingFunction.Ease];
    public float[] AnimationDelay = [0];
    public float[] AnimationIterationCount = [1];
    public AnimationDirection[] AnimationDirectionList = [AnimationDirection.Normal];
    public AnimationFillMode[] AnimationFillModeList = [AnimationFillMode.None];
    public bool[] AnimationPaused = [false];
    public TransitionSpec[] Transitions = [];
    public AnimationSpec[] Animations = [];

    /// <summary>Custom properties (--name), inherited, as unparsed token lists.</summary>
    public Dictionary<string, List<CssToken>> Custom = [];

    public ComputedStyle Clone()
    {
        var copy = (ComputedStyle)MemberwiseClone();
        return copy;
    }

    /// <summary>
    /// Combines the list-valued longhands after the cascade: background layers follow the images,
    /// transitions follow transition-property and animations follow animation-name, with the other
    /// lists repeated to that length (CSS Backgrounds 3 §2.2, CSS Transitions §2).
    /// </summary>
    public void Finish()
    {
        AppliedDecorations = IsDecoratingBox
            ? [.. BaseDecorations, new AppliedDecoration(Underline, Overline, LineThrough, TextDecorationStyle, TextDecorationColor ?? Color, TextDecorationThickness, TextUnderlineOffset)]
            : BaseDecorations;
        var layers = new List<BackgroundLayer>();
        for (var i = 0; i < BackgroundImages.Length; i++)
        {
            var image = BackgroundImages[i];
            if (image.Kind == ImageKind.None)
                continue;
            var size = BackgroundSizes[i % BackgroundSizes.Length];
            var position = BackgroundPositions[i % BackgroundPositions.Length];
            var repeat = BackgroundRepeats[i % BackgroundRepeats.Length];
            layers.Add(new BackgroundLayer(image, size.X, size.Y, size.Keyword, position.X, position.Y, repeat.X, repeat.Y) { Fixed = BackgroundFixed[i % BackgroundFixed.Length] });
        }
        Backgrounds = layers.ToArray();

        var transitions = new List<TransitionSpec>();
        for (var i = 0; i < TransitionProperty.Length; i++)
        {
            var property = TransitionProperty[i];
            if (property == "none")
                continue;
            transitions.Add(new TransitionSpec(property, TransitionDuration[i % TransitionDuration.Length], TransitionTiming[i % TransitionTiming.Length], TransitionDelay[i % TransitionDelay.Length]));
        }
        Transitions = transitions.Count == 1 && transitions[0] is { Property: "all", Duration: <= 0, Delay: <= 0 } ? [] : transitions.ToArray();

        var animations = new List<AnimationSpec>();
        for (var i = 0; i < AnimationName.Length; i++)
        {
            var name = AnimationName[i];
            if (name == "none")
                continue;
            animations.Add(new AnimationSpec(name, AnimationDuration[i % AnimationDuration.Length], AnimationTiming[i % AnimationTiming.Length],
                AnimationDelay[i % AnimationDelay.Length], AnimationIterationCount[i % AnimationIterationCount.Length],
                AnimationDirectionList[i % AnimationDirectionList.Length], AnimationFillModeList[i % AnimationFillModeList.Length],
                AnimationPaused[i % AnimationPaused.Length]));
        }
        Animations = animations.ToArray();
    }

    /// <summary>
    /// Do children computed from this style come out the same as from <paramref name="other"/>?
    /// (Inherited properties, custom properties, and the display type that blockifies flex and grid items.)
    /// </summary>
    public bool AffectsChildrenEquals(ComputedStyle other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (Color != other.Color || FontSize != other.FontSize || FontWeight != other.FontWeight || FontItalic != other.FontItalic
            || LineHeight != other.LineHeight || LetterSpacing != other.LetterSpacing || WordSpacing != other.WordSpacing
            || TextAlign != other.TextAlign || TextTransform != other.TextTransform || WhiteSpace != other.WhiteSpace
            || OverflowWrap != other.OverflowWrap || Visibility != other.Visibility || PointerEvents != other.PointerEvents || Interactivity != other.Interactivity || IsInert != other.IsInert
            || Cursor != other.Cursor || !CursorImages.AsSpan().SequenceEqual(other.CursorImages) || UserSelect != other.UserSelect || AccentColor != other.AccentColor || CaretColor != other.CaretColor
            || TabularNumbers != other.TabularNumbers || ListStyleType != other.ListStyleType || ListStyleInside != other.ListStyleInside
            || ScrollbarThumb != other.ScrollbarThumb || ScrollbarTrack != other.ScrollbarTrack
            || TextUnderlinePosition != other.TextUnderlinePosition || TextDecorationSkipInk != other.TextDecorationSkipInk
            || Display != other.Display)
            return false;
        if (!ReferenceEquals(AppliedDecorations, other.AppliedDecorations) && !AppliedDecorations.SequenceEqual(other.AppliedDecorations))
            return false;
        if (!ReferenceEquals(FontFamilies, other.FontFamilies) && !FontFamilies.SequenceEqual(other.FontFamilies))
            return false;
        if (!ReferenceEquals(TextShadows, other.TextShadows) && !TextShadows.SequenceEqual(other.TextShadows))
            return false;
        if (ReferenceEquals(Custom, other.Custom))
            return true;
        if (Custom.Count != other.Custom.Count)
            return false;
        foreach (var (name, value) in Custom)
            if (!other.Custom.TryGetValue(name, out var otherValue) || !(ReferenceEquals(value, otherValue) || value.SequenceEqual(otherValue)))
                return false;
        return true;
    }

    // Helpers for layout and painting.
    public bool IsOutOfFlow => Position is PositionKind.Absolute or PositionKind.Fixed;
    public bool IsFlexContainer => Display is Display.Flex or Display.InlineFlex;
    public bool IsGridContainer => Display is Display.Grid or Display.InlineGrid;
    public bool IsInlineLevel => Display is Display.Inline or Display.InlineBlock or Display.InlineFlex or Display.InlineGrid;
    public bool IsScrollContainer => OverflowX is Overflow.Hidden or Overflow.Scroll or Overflow.Auto or Overflow.Clip || OverflowY is Overflow.Hidden or Overflow.Scroll or Overflow.Auto or Overflow.Clip;
    /// <summary>Blink's IsOverflowValueScrollableX/Y: the computed overflow of an axis is hidden, scroll or auto (not visible or clip).</summary>
    public bool IsOverflowValueScrollableX => OverflowX is Overflow.Hidden or Overflow.Scroll or Overflow.Auto;
    public bool IsOverflowValueScrollableY => OverflowY is Overflow.Hidden or Overflow.Scroll or Overflow.Auto;
    public bool HasTransform => Transform.Length > 0 || TranslateX.IsValue && !TranslateX.IsZero || TranslateY.IsValue && !TranslateY.IsZero || ScaleX != 1 || ScaleY != 1 || Rotate != 0;

    public float BorderTop => BorderTopStyle is BorderLineStyle.None or BorderLineStyle.Hidden ? 0 : BorderTopWidth;
    public float BorderRight => BorderRightStyle is BorderLineStyle.None or BorderLineStyle.Hidden ? 0 : BorderRightWidth;
    public float BorderBottom => BorderBottomStyle is BorderLineStyle.None or BorderLineStyle.Hidden ? 0 : BorderBottomWidth;
    public float BorderLeft => BorderLeftStyle is BorderLineStyle.None or BorderLineStyle.Hidden ? 0 : BorderLeftWidth;

    /// <summary>Creates a stacking context (z-index on a positioned box, opacity, transforms, filters ...).</summary>
    public bool CreatesStackingContext =>
        (Position != PositionKind.Static && ZIndex is not null) || Position is PositionKind.Fixed or PositionKind.Sticky
        || Opacity < 1 || HasTransform || Filters.Length > 0 || BackdropFilters.Length > 0 || MixBlendMode != CssBlendMode.Normal
        || Isolate || ClipPath is not null || WillChangeCreatesStackingContext;

    /// <summary>will-change names this property.</summary>
    public bool WillChangeProperty(string name) => Array.IndexOf(WillChange, name) >= 0;

    /// <summary>will-change names a property that makes a stacking context (Blink's WillChangeProperties stacking context set).</summary>
    public bool WillChangeCreatesStackingContext => WillChange.Length > 0 && WillChange.Any(p => p is "opacity" or "transform" or "translate" or "rotate" or "scale"
        or "perspective" or "transform-style" or "filter" or "backdrop-filter" or "mix-blend-mode" or "isolation" or "clip-path" or "mask" or "mask-image" or "offset-path");

    /// <summary>will-change names a transform-related property or filter: the box contains positioned and fixed descendants.</summary>
    public bool WillChangeContainsFixed => WillChange.Length > 0 && WillChange.Any(p => p is "transform" or "translate" or "rotate" or "scale" or "perspective" or "transform-style" or "filter" or "backdrop-filter" or "offset-path");

    /// <summary>The text style for the text layout engine.</summary>
    public Text.TextStyle ToTextStyle(Color? colorOverride = null) => new()
    {
        Families = FontFamilies,
        Size = FontSize,
        Weight = FontWeight,
        Italic = FontItalic,
        Color = colorOverride ?? Color,
        LetterSpacing = LetterSpacing,
        Transform = TextTransform,
        Underline = Underline,
        LineHeight = LineHeight.Kind switch
        {
            LineHeightKind.Number => LineHeight.Value * FontSize,
            LineHeightKind.Length => LineHeight.Value,
            _ => null,
        },
    };
}
