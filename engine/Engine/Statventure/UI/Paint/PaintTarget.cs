using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Text;
using Statventure.UI.Layout;
using Statventure.UI.Style;
using Statventure.UI.Paint.Compositing;
using CanvasRenderer = Statventure.Canvas.Canvas;
using CanvasPath = Statventure.Canvas.Path;

namespace Statventure.UI.Paint;

/// <summary>
/// Where a box takes pointer input: a (rounded) rectangle in the space of <see cref="Transform"/>,
/// within the clip that applied when it was painted (in viewport units).
/// </summary>
public readonly record struct HitRegion(LayoutBox Box, RectF Rect, CornerRadii Radii, Matrix3x2 Transform, RectF Clip, CornerRadii ClipRadii, bool Text = false, LineItem? Item = null)
{
    /// <summary>
    /// Does the point hit the region? As in Blink, a box is hit where the 1×1 square from the point
    /// (HitTestLocation) meets it; text where its pixel-snapped rectangle contains the point.
    /// </summary>
    public bool Contains(Vector2 point)
    {
        if (!Intersects(Clip, ClipRadii, point, Matrix3x2.Identity) || !Matrix3x2.Invert(Transform, out var inverse))
            return false;
        if (Box.Type == BoxType.Text || Text)
        {
            static float Round(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
            return Inside(RectF.FromEdges(Round(Rect.Left), Round(Rect.Top), Round(Rect.Right), Round(Rect.Bottom)), CornerRadii.Zero, Vector2.Transform(point, inverse));
        }
        return Intersects(Rect, Radii, point, inverse);
    }

    /// <summary>Does the unit square at <paramref name="point"/>, mapped by <paramref name="toLocal"/>, intersect the rounded rectangle?</summary>
    static bool Intersects(RectF rect, CornerRadii radii, Vector2 point, Matrix3x2 toLocal)
    {
        Span<Vector2> corners = [point, point + new Vector2(1, 0), point + new Vector2(0, 1), point + Vector2.One];
        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        for (var i = 0; i < 4; i++)
        {
            corners[i] = Vector2.Transform(corners[i], toLocal);
            min = Vector2.Min(min, corners[i]);
            max = Vector2.Max(max, corners[i]);
        }
        if (min.X >= rect.Right || max.X <= rect.Left || min.Y >= rect.Bottom || max.Y <= rect.Top)
            return false;
        if (radii.IsZero)
            return true;
        // Rounded: the square misses only if it lies wholly in a corner's box and its point nearest
        // the corner's centre is outside the ellipse. Blink tests rounded borders pixel-snapped.
        static float Snap(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
        rect = RectF.FromEdges(Snap(rect.Left), Snap(rect.Top), Snap(rect.Right), Snap(rect.Bottom));
        radii = radii.Fit(rect.Size);
        static bool Misses(Vector2 min, Vector2 max, Vector2 center, Vector2 radius, bool left, bool top)
        {
            if (radius.X <= 0 || radius.Y <= 0)
                return false;
            var beyondX = left ? max.X <= center.X : min.X >= center.X;
            var beyondY = top ? max.Y <= center.Y : min.Y >= center.Y;
            if (!beyondX || !beyondY)
                return false;
            var nearest = new Vector2(left ? max.X : min.X, top ? max.Y : min.Y);
            return ((nearest - center) / radius).LengthSquared() > 1;
        }
        return !Misses(min, max, new Vector2(rect.Left + radii.TopLeft.X, rect.Top + radii.TopLeft.Y), radii.TopLeft, true, true)
            && !Misses(min, max, new Vector2(rect.Right - radii.TopRight.X, rect.Top + radii.TopRight.Y), radii.TopRight, false, true)
            && !Misses(min, max, new Vector2(rect.Right - radii.BottomRight.X, rect.Bottom - radii.BottomRight.Y), radii.BottomRight, false, false)
            && !Misses(min, max, new Vector2(rect.Left + radii.BottomLeft.X, rect.Bottom - radii.BottomLeft.Y), radii.BottomLeft, true, false);
    }

    /// <summary>Is the point inside the rectangle with these corner radii (half-open on the right and bottom)?</summary>
    public static bool Inside(RectF rect, CornerRadii radii, Vector2 p)
    {
        if (p.X < rect.Left || p.Y < rect.Top || p.X >= rect.Right || p.Y >= rect.Bottom)
            return false;
        if (radii.IsZero)
            return true;
        static bool OutsideCorner(Vector2 p, Vector2 center, Vector2 radius, bool left, bool top)
        {
            if (radius.X <= 0 || radius.Y <= 0)
                return false;
            if ((left ? p.X >= center.X : p.X <= center.X) || (top ? p.Y >= center.Y : p.Y <= center.Y))
                return false;
            var d = (p - center) / radius;
            return d.LengthSquared() > 1;
        }
        return !OutsideCorner(p, new Vector2(rect.Left + radii.TopLeft.X, rect.Top + radii.TopLeft.Y), radii.TopLeft, true, true)
            && !OutsideCorner(p, new Vector2(rect.Right - radii.TopRight.X, rect.Top + radii.TopRight.Y), radii.TopRight, false, true)
            && !OutsideCorner(p, new Vector2(rect.Right - radii.BottomRight.X, rect.Bottom - radii.BottomRight.Y), radii.BottomRight, false, false)
            && !OutsideCorner(p, new Vector2(rect.Left + radii.BottomLeft.X, rect.Bottom - radii.BottomLeft.Y), radii.BottomLeft, true, false);
    }
}

/// <summary>
/// What the painter paints on: the canvas when one is given (otherwise nothing is drawn), plus the
/// hit regions of everything painted, in paint order. It follows transforms and clips itself (in
/// viewport units), so the painter's traversal also serves hit testing without a GPU. Parts painted
/// muted (opacity 0) take input but draw nothing.
/// </summary>
internal sealed class PaintTarget
{
    CanvasRenderer? _canvas;
    /// <summary>Recording paint chunks alone (the analysis pass): everything is "drawn", nothing reaches a canvas.</summary>
    bool _recordOnly;
    float _scale = 1;
    int _muted;
    Matrix3x2 _transform = Matrix3x2.Identity;
    RectF _clip;
    CornerRadii _clipRadii;
    readonly Stack<Matrix3x2> _transforms = new();
    readonly Stack<(RectF, CornerRadii)> _clips = new();
    /// <summary>Each pushed clip's own screen rect, and whether it is rounded (the clip ops a display item records).</summary>
    readonly List<(RectF Rect, bool Rounded)> _clipOps = [];
    readonly Stack<bool> _layers = new();

    public readonly List<HitRegion> Regions = [];

    /// <summary>Draws are made (to a canvas, or recorded as paint chunks; not hit-region recording only, not muted).</summary>
    public bool Drawing => (_canvas is not null || _recordOnly) && _muted == 0;

    /// <summary>Draws reach a canvas.</summary>
    public bool Draws => _canvas is not null && _muted == 0;

    /// <summary>There is a canvas to make textures on.</summary>
    public bool HasCanvas => _canvas is not null;

    public float Scale => _canvas?.Scale ?? _scale;

    public Matrix3x2 Transform => _transform;

    /// <param name="recordOnly">Draw nothing on <paramref name="canvas"/> (only its scale counts): the paint chunks' analysis pass.</param>
    public void Begin(CanvasRenderer? canvas, Vector2 viewport, bool recordOnly = false)
    {
        _canvas = recordOnly ? null : canvas;
        _recordOnly = recordOnly;
        _scale = canvas?.Scale ?? 1f;
        _chunks = null;
        _verdicts = null;
        _state = null;
        _item = null;
        _muted = 0;
        _transform = Matrix3x2.Identity;
        _clip = new RectF(0, 0, viewport.X, viewport.Y);
        _clipRadii = CornerRadii.Zero;
        _transforms.Clear();
        _clips.Clear();
        _clipOps.Clear();
        _layers.Clear();
        Regions.Clear();
    }

    /// <summary>The following draws are input-only (opacity 0) until <see cref="Unmute"/>.</summary>
    public void Mute() => _muted++;

    public void Unmute() => _muted--;

    int _noHits;

    /// <summary>The following draws take no input (outside list markers) until <see cref="TakeHits"/>.</summary>
    public void IgnoreHits() => _noHits++;

    public void TakeHits() => _noHits--;

    // ---------- Hit regions ----------

    /// <param name="borderBox">The box's border box when it takes input elsewhere (an inline SVG's overflowing shapes): its hit test data is still its border box.</param>
    public void Hit(LayoutBox box, RectF rect, CornerRadii radii, bool text = false, LineItem? item = null, RectF? borderBox = null, bool takesInput = true)
    {
        // Blink records every visible box's border box as hit test data in its chunk.
        if (!text && box.Type != BoxType.Text && _noHits == 0)
            RecordHitTestRect(borderBox ?? rect, HitTestOpaquenessOf(box, radii));
        if (!takesInput || _noHits > 0 || _clip.Width <= 0 || _clip.Height <= 0 || rect.Width <= 0 || rect.Height <= 0)
            return;
        Regions.Add(new HitRegion(box, rect, radii, _transform, _clip, _clipRadii, text, item));
    }

    // ---------- Paint chunks (Blink's PaintChunker) ----------

    List<PaintChunk>? _chunks;
    /// <summary>The draw pass: the analysis pass's chunks (same sequence), whose verdicts decide LCD text.</summary>
    List<PaintChunk>? _verdicts;
    PropertyState? _state;
    LayoutBox? _stateOwner;
    bool _forceNewChunk;
    int _suppressed;
    Item? _item;

    /// <summary>A display item being recorded: its visual rect, what it knows to be opaque, a background colour candidate, text.</summary>
    sealed class Item
    {
        public RectF Visual;
        /// <summary>The visual rect is the box's (its ink overflow), not the drawings' bounds.</summary>
        public bool FixedVisual;
        public RectF Opaque;
        public int Ops;
        /// <summary>The clips pushed before the item began (property tree clips, not the item's own ops).</summary>
        public int ClipDepth;
        /// <summary>The opaqueness analysis has stopped (a restore, a rounded clip, or past 8 ops).</summary>
        public bool Stopped;
        public Color? BackgroundColor;
        public float BackgroundArea;
        public bool AnalyzeFirstOpOnly;
        public bool SeenOp;
        /// <summary>Something was drawn (an item with nothing drawn isn't recorded).</summary>
        public bool Drew;
        public bool HasText;
    }

    /// <summary>The chunks recorded since <see cref="BeginChunks"/>.</summary>
    public List<PaintChunk>? Chunks => _chunks;

    /// <summary>Records paint chunks from here on; in the draw pass, <paramref name="verdicts"/> are the analysis pass's.</summary>
    public void BeginChunks(List<PaintChunk>? verdicts = null)
    {
        _chunks = [];
        _verdicts = verdicts;
        _state = null;
        _forceNewChunk = false;
        _item = null;
    }

    /// <summary>The following drawings are in this property tree state, painted for this box.</summary>
    public void UseState(PropertyState state, LayoutBox owner)
    {
        _state = state;
        _stateOwner = owner;
    }

    /// <summary>The following drawings are drawn but not recorded again (their chunks were recorded where Blink paints them).</summary>
    public void SuppressChunks() => _suppressed++;

    public void ResumeChunks() => _suppressed--;

    /// <summary>The page's device pixel ratio (PaintLayerPainter's sparsity limit scales with it).</summary>
    public float DevicePixelRatio { get; set; } = 1;

    /// <summary>
    /// A stacked layer's painting begins (PaintLayerPainter's subsequence, which starts and ends
    /// with new chunks): when its properties differ from the last chunk's, it has two or more
    /// descendant layers, its hit testing differs from the current chunk's, or joining the last
    /// chunk would leave too much of it empty. Returns whether it made the subsequence.
    /// </summary>
    public bool BeginLayer(PropertyState? borderBox, RectF visualRect, bool transparentToHitTest, bool twoDescendantLayers)
    {
        if (_chunks is null || _suppressed > 0)
            return false;
        var last = _chunks.Count > 0 ? _chunks[^1] : null;
        var create = last is not null && borderBox is { } state && last.State != state;
        create |= twoDescendantLayers;
        if (!create && last is not null)
        {
            var willCreateNewChunk = _forceNewChunk || _state != last.State;
            var currentTransparent = !willCreateNewChunk && !Gfx.IsEmpty(last.Bounds) && last.HitTestOpaqueness == HitTestOpaqueness.Transparent;
            if (transparentToHitTest != currentTransparent)
                create = true;
            else
            {
                static float Area(RectF r) => MathF.Max(0, r.Width) * MathF.Max(0, r.Height);
                var visual = RectF.FromEdges(MathF.Floor(visualRect.Left), MathF.Floor(visualRect.Top), MathF.Ceiling(visualRect.Right), MathF.Ceiling(visualRect.Bottom));
                var merged = Gfx.Union(last.Bounds, visual);
                create = Area(merged) > 10000 * DevicePixelRatio * DevicePixelRatio + Area(last.Bounds) + Area(visual);
            }
        }
        if (create)
            _forceNewChunk = true;
        return create;
    }

    /// <summary>The layer's subsequence ends: the next drawing starts a new chunk.</summary>
    public void EndLayer(bool created)
    {
        if (created && _chunks is not null && _suppressed == 0)
            _forceNewChunk = true;
    }

    /// <summary>When set, drawings in the current item don't count as opaque or as a background colour (the background goes into the scrolling contents).</summary>
    public bool NotOpaque { get; set; }

    PaintChunk? CurrentChunk(bool create)
    {
        if (_chunks is null || _state is not { } state || _suppressed > 0)
            return null;
        if (!create)
            return _chunks.Count > 0 && _chunks[^1].State == state && !_forceNewChunk ? _chunks[^1] : null;
        if (_chunks.Count == 0 || _forceNewChunk || _chunks[^1].State != state || _chunks[^1].Kind != ChunkKind.Normal)
        {
            _chunks.Add(new PaintChunk { State = state, Owner = _stateOwner });
            _forceNewChunk = false;
            if (_verdicts is not null && _canvas is not null)
                _canvas.TextLcd = _chunks.Count <= _verdicts.Count && _verdicts[_chunks.Count - 1].LcdText;
        }
        return _chunks[^1];
    }

    RectF ToScreen(RectF rect) => _transform.IsIdentity ? rect : rect.Transform(_transform);

    /// <summary>A box's hit test rect joins the current chunk's bounds (making the chunk if needed).</summary>
    /// <summary>ObjectPainter::GetHitTestOpaqueness: transparent without pointer events, mixed with rounded corners.</summary>
    static HitTestOpaqueness HitTestOpaquenessOf(LayoutBox box, CornerRadii radii) =>
        box.Style.UsedPointerEvents == PointerEvents.None || box.Style.Visibility != Visibility.Visible ? HitTestOpaqueness.Transparent
        : !radii.IsZero ? HitTestOpaqueness.Mixed : HitTestOpaqueness.Opaque;

    void RecordHitTestRect(RectF rect, HitTestOpaqueness opaqueness = HitTestOpaqueness.Opaque)
    {
        if (rect.Width <= 0 || rect.Height <= 0 || CurrentChunk(true) is not { } chunk)
            return;
        static float Round(float v) => MathF.Round(v, MidpointRounding.AwayFromZero);
        var screen = ToScreen(rect);
        chunk.UnionBounds(RectF.FromEdges(Round(screen.Left), Round(screen.Top), Round(screen.Right), Round(screen.Bottom)), opaqueness);
    }

    /// <summary>Starts a display item (one box's decorations, one text fragment…); drawings until <see cref="EndItem"/> belong to it.</summary>
    /// <param name="visualRect">The item's visual rect (a box's ink overflow rect) when not its drawings' bounds.</param>
    public void BeginItem(bool boxDecoration = false, RectF? visualRect = null) =>
        _item = new Item
        {
            AnalyzeFirstOpOnly = !boxDecoration, Visual = visualRect is { } v ? ToScreen(v) : RectF.Empty, FixedVisual = visualRect is not null,
            ClipDepth = _clipOps.Count,
        };

    /// <summary>
    /// One op of the current item that isn't a drawing (DrawingDisplayItem::CalculateRectKnownToBeOpaqueForRecord
    /// counts it toward its 8 ops; a restore or a rounded clip ends the analysis).
    /// </summary>
    void RecordOp(bool stops)
    {
        if (_item is not { } item)
            return;
        item.SeenOp = true;
        if (++item.Ops > 8 || stops)
            item.Stopped = true;
    }

    public void EndItem()
    {
        if (_item is not { } item)
            return;
        _item = null;
        AddItem(item);
    }

    void AddItem(Item item)
    {
        if (!item.Drew || Gfx.IsEmpty(item.Visual) || _muted > 0 || CurrentChunk(true) is not { } chunk)
            return;
        var visual = RectF.FromEdges(MathF.Floor(item.Visual.Left), MathF.Floor(item.Visual.Top), MathF.Ceiling(item.Visual.Right), MathF.Ceiling(item.Visual.Bottom));
        chunk.UnionBounds(visual, HitTestOpaqueness.Transparent);
        chunk.DrawableBounds = Gfx.Union(chunk.DrawableBounds, visual);
        // PaintChunker::ProcessBackgroundColorCandidate.
        if (item.BackgroundColor is { } color)
        {
            var minArea = 0.5f * chunk.Bounds.Width * chunk.Bounds.Height;
            if (item.BackgroundArea >= chunk.BackgroundColorArea || item.BackgroundArea >= minArea)
            {
                if (chunk.BackgroundColorArea >= minArea && color.A < 1 && chunk.BackgroundColor is { } under)
                {
                    chunk.BackgroundColorArea = item.BackgroundArea;
                    chunk.BackgroundColor = Layerizer.Over(color, under);
                }
                else
                {
                    chunk.BackgroundColor = color;
                    chunk.BackgroundColorArea = item.BackgroundArea;
                }
            }
        }
        chunk.RectKnownToBeOpaque = Gfx.MaximumCoveredRect(chunk.RectKnownToBeOpaque, item.Opaque);
        if (item.HasText)
        {
            chunk.HasText = true;
            if (chunk.TextKnownToBeOnOpaqueBackground)
                chunk.TextKnownToBeOnOpaqueBackground = Gfx.Contains(chunk.RectKnownToBeOpaque, visual);
        }
    }

    /// <summary>
    /// One drawing of the current item (or an item of its own): its screen bounds, and for an opaque
    /// fill its known-opaque rect (DrawingDisplayItem::CalculateRectKnownToBeOpaque: up to 8 ops,
    /// rounded rects inset by 0.3 × radius) and background colour (the first plain fill).
    /// </summary>
    void Record(RectF bounds, Brush? fill = null, CornerRadii radii = default, RectF? fillRect = null, bool text = false)
    {
        if (_chunks is null || _suppressed > 0)
            return;
        var own = _item is null;
        var item = _item ?? new Item { AnalyzeFirstOpOnly = true };
        if (!item.FixedVisual)
            item.Visual = Gfx.Union(item.Visual, ToScreen(bounds));
        item.HasText |= text;
        var axisAligned = _transform.M12 == 0 && _transform.M21 == 0;
        if (++item.Ops > 8)
            item.Stopped = true;
        if (fill is not null && !NotOpaque && axisAligned)
        {
            var area = ToScreen(fillRect ?? bounds);
            var solid = !fill.IsGradient;
            var opaque = solid ? fill.Color.A >= 1 : fill.Stops!.All(stop => stop.Color.A >= 1);
            if (opaque && !item.Stopped)
            {
                var opaqueRect = radii.IsZero ? area : RectF.FromEdges(
                    area.Left + MathF.Max(radii.TopLeft.X, radii.BottomLeft.X) * 0.3f, area.Top + MathF.Max(radii.TopLeft.Y, radii.TopRight.Y) * 0.3f,
                    area.Right - MathF.Max(radii.TopRight.X, radii.BottomRight.X) * 0.3f, area.Bottom - MathF.Max(radii.BottomLeft.Y, radii.BottomRight.Y) * 0.3f);
                opaqueRect = RectF.FromEdges(MathF.Ceiling(opaqueRect.Left), MathF.Ceiling(opaqueRect.Top), MathF.Floor(opaqueRect.Right), MathF.Floor(opaqueRect.Bottom));
                item.Opaque = Gfx.MaximumCoveredRect(item.Opaque, opaqueRect);
                // Within the item's visual rect and the clips it pushed.
                var clip = item.FixedVisual ? Enclosed(item.Visual) : (RectF?)null;
                for (var i = item.ClipDepth; i < _clipOps.Count; i++)
                    clip = clip is { } c ? Gfx.Intersect(c, Enclosed(_clipOps[i].Rect)) : Enclosed(_clipOps[i].Rect);
                if (clip is { } limit)
                    item.Opaque = Gfx.Intersect(item.Opaque, limit);
            }
            // DrawingDisplayItem::BackgroundColor: the first plain fill (past other ops in box decorations; not limited like the opaqueness).
            if (solid && item.BackgroundColor is null && !(item.AnalyzeFirstOpOnly && item.SeenOp))
            {
                item.BackgroundColor = fill.Color;
                item.BackgroundArea = area.Width * area.Height;
            }
        }
        item.SeenOp = true;
        item.Drew = true;
        if (own)
            AddItem(item);
    }

    /// <summary>gfx::ToEnclosedRect: the whole pixels inside a rect.</summary>
    static RectF Enclosed(RectF r) => RectF.FromEdges(MathF.Ceiling(r.Left), MathF.Ceiling(r.Top), MathF.Floor(r.Right), MathF.Floor(r.Bottom));

    /// <summary>A box's scroll hit test (a chunk of its own: a composited scroller's layer).</summary>
    public void RecordScrollHitTest(PropertyState state, TransformNode scrollTranslation, LayoutBox owner, RectF rect)
    {
        if (_chunks is null || _suppressed > 0)
            return;
        _chunks.Add(new PaintChunk { State = state, Owner = owner, Kind = ChunkKind.ScrollHitTest, ScrollTranslation = scrollTranslation, Bounds = rect });
        _forceNewChunk = true;
    }

    /// <summary>A scrollbar (a chunk of its own: with raster-inducing scroll every scrollbar is a layer).</summary>
    public void RecordScrollbar(PropertyState state, TransformNode? scrollTranslation, LayoutBox owner, RectF rect, bool opaque)
    {
        if (_chunks is null || _suppressed > 0 || rect.Width <= 0 || rect.Height <= 0)
            return;
        _chunks.Add(new PaintChunk
        {
            State = state, Owner = owner, Kind = ChunkKind.Scrollbar, ScrollTranslation = scrollTranslation,
            Bounds = rect, DrawableBounds = rect, RectKnownToBeOpaque = opaque ? rect : RectF.Empty,
        });
        _forceNewChunk = true;
    }

    /// <summary>Content drawn by its own layer (a canvas element).</summary>
    public void RecordForeign(PropertyState state, LayoutBox owner, RectF rect)
    {
        if (_chunks is null || _suppressed > 0)
            return;
        _chunks.Add(new PaintChunk { State = state, Owner = owner, Kind = ChunkKind.Foreign, Bounds = rect, DrawableBounds = rect });
        _forceNewChunk = true;
    }

    /// <summary>
    /// What Blink paints of a scroller in its scrolling contents (the engine draws the background in
    /// the border box): the background when it goes there, and the contents' hit test rect. Recorded, not drawn.
    /// </summary>
    public void RecordScrollingBackground(PropertyState state, LayoutBox owner, RectF rect, Brush? fill)
    {
        var previous = (_state, _stateOwner);
        UseState(state, owner);
        if (fill is not null)
        {
            BeginItem(boxDecoration: true);
            Record(rect, fill, fillRect: rect);
            EndItem();
        }
        RecordHitTestRect(rect);
        (_state, _stateOwner) = previous;
    }

    // ---------- State (followed here, and on the canvas) ----------

    public void PushTransform(Matrix3x2 transform)
    {
        _transforms.Push(_transform);
        _transform = transform * _transform;
        _canvas?.PushTransform(transform);
    }

    public void PopTransform()
    {
        _transform = _transforms.Pop();
        _canvas?.PopTransform();
    }

    /// <summary>
    /// As <see cref="CanvasRenderer.PushClip"/>: the transformed rectangle's bounds intersected with
    /// the current clip. Hit testing clips to <paramref name="hitRect"/> when given (the unsnapped
    /// rectangle, as Blink hit-tests overflow clips in layout units).
    /// </summary>
    public void PushClip(RectF rect, CornerRadii radii = default, RectF? hitRect = null)
    {
        _clips.Push((_clip, _clipRadii));
        var bounds = (hitRect ?? rect).Transform(_transform);
        var clipped = _clip.Intersect(bounds);
        if (!radii.IsZero)
        {
            var scale = MathF.Sqrt(MathF.Abs(_transform.M11 * _transform.M22 - _transform.M12 * _transform.M21));
            _clipRadii = radii.Fit((hitRect ?? rect).Size).Scale(scale);
        }
        else if (clipped != _clip)
            _clipRadii = CornerRadii.Zero;
        _clip = clipped;
        _clipOps.Add((bounds, !radii.IsZero));
        // In a display item: a save and a clip op (a rounded clip ends its opaqueness analysis).
        if (_item is not null)
        {
            RecordOp(false);
            RecordOp(!radii.IsZero);
        }
        _canvas?.PushClip(rect, radii);
    }

    public void PopClip()
    {
        (_clip, _clipRadii) = _clips.Pop();
        _clipOps.RemoveAt(_clipOps.Count - 1);
        // A restore in a display item ends its opaqueness analysis.
        if (_item is { } item && _clipOps.Count >= item.ClipDepth)
            RecordOp(true);
        _canvas?.PopClip();
    }

    public void PushLayer(LayerOptions options)
    {
        var drawn = Draws;
        _layers.Push(drawn);
        if (drawn)
            _canvas!.PushLayer(options);
    }

    public void PopLayer()
    {
        if (_layers.Pop())
            _canvas!.PopLayer();
    }

    readonly Stack<(Matrix3x2 Transform, RectF Clip, CornerRadii ClipRadii, Item? Item, bool Drawn)> _tiles = new();

    /// <summary>
    /// Draws until <see cref="PopTile"/> go into a tile texture (<see cref="CanvasRenderer.PushTile"/>,
    /// a background layer's pattern), in its pixels: they aren't display items or hit regions here,
    /// only the tile's one draw is (<see cref="DrawTile"/>). Without a canvas nothing is drawn.
    /// </summary>
    public void PushTile(int pixelWidth, int pixelHeight)
    {
        var drawn = Draws;
        _tiles.Push((_transform, _clip, _clipRadii, _item, drawn));
        _item = null;
        _suppressed++;
        _noHits++;
        _transform = Matrix3x2.CreateScale(1 / Scale);
        _clip = new RectF(0, 0, pixelWidth, pixelHeight);
        _clipRadii = CornerRadii.Zero;
        if (drawn)
            _canvas!.PushTile(pixelWidth, pixelHeight);
    }

    /// <summary>The tile's texture (null without a canvas): see <see cref="CanvasRenderer.PopTile"/>.</summary>
    public GpuImage? PopTile()
    {
        var (transform, clip, clipRadii, item, drawn) = _tiles.Pop();
        (_transform, _clip, _clipRadii, _item) = (transform, clip, clipRadii, item);
        _suppressed--;
        _noHits--;
        return drawn ? _canvas!.PopTile() : null;
    }

    /// <summary>A tile's texture repeated over a rect (premultiplied, with the repeat sampler): one drawing of the rect.</summary>
    public void DrawTile(GpuImage? tile, RectF rect, RectF uv)
    {
        Record(rect);
        if (Draws && tile is not null)
            _canvas!.DrawImage(tile, rect, uv, sampler: SamplerKind.LinearRepeat, premultiplied: true);
    }

    // ---------- Drawing (only with a canvas) ----------

    public void DrawBox(RectF rect, Brush? background, CornerRadii radii, Border border)
    {
        Record(rect, background, radii);
        if (Draws)
            _canvas!.DrawBox(rect, background, radii, border);
    }

    public void DrawBox(RectF rect, Brush? background, CornerRadii radii, BorderSides sides)
    {
        Record(rect, background, radii);
        if (Draws)
            _canvas!.DrawBox(rect, background, radii, sides);
    }

    public void FillRect(RectF rect, Color color)
    {
        Record(rect, new Brush { Color = color });
        if (Draws)
            _canvas!.FillRect(rect, color);
    }

    public void DrawBoxShadow(RectF rect, CornerRadii radii, BoxShadow shadow)
    {
        var reach = shadow.Blur * 1.5f + MathF.Max(0, shadow.Spread);
        // Blink draws a shadow between a save with a clip and a restore, which ends the item's opaqueness analysis.
        RecordOp(false);
        RecordOp(!radii.IsZero);
        Record(rect.Offset(shadow.Offset).Inflate(reach));
        RecordOp(true);
        if (Draws)
            _canvas!.DrawBoxShadow(rect, radii, shadow);
    }

    public void DrawBackdropBlur(RectF rect, CornerRadii radii, float blurRadius)
    {
        // (No drawing for Blink: a backdrop filter is its effect node's, which makes its layer draw content.)
        if (Draws)
            _canvas!.DrawBackdropBlur(rect, radii, blurRadius);
    }

    public void DrawImage(int textureIndex, RectF rect, RectF? uv = null, CornerRadii radii = default, Color? tint = null, SamplerKind sampler = SamplerKind.LinearClamp)
    {
        Record(rect);
        if (Draws)
            _canvas!.DrawImage(textureIndex, rect, uv, radii, tint, sampler);
    }

    public void DrawImage(GpuImage image, RectF rect, RectF? uv = null, CornerRadii radii = default, Color? tint = null, SamplerKind sampler = SamplerKind.LinearClamp)
    {
        Record(rect);
        if (Draws)
            _canvas!.DrawImage(image, rect, uv, radii, tint, sampler);
    }

    public void FillPath(CanvasPath path, Brush brush, FillRule rule = FillRule.NonZero)
    {
        Record(path.Bounds);
        if (Draws)
            _canvas!.FillPath(path, brush, rule);
    }

    public void StrokePath(CanvasPath path, Brush brush, StrokeStyle stroke)
    {
        Record(path.Bounds.Inflate(stroke.Width / 2));
        if (Draws)
            _canvas!.StrokePath(path, brush, stroke);
    }

    public void DrawGlyphs(GlyphRun run, Vector2 origin, Color color)
    {
        Record(GlyphBounds(run, origin), text: true);
        if (Draws)
            _canvas!.DrawGlyphs(run, origin, color);
    }

    public void DrawText(TextLayout layout, Vector2 position, Color? color = null)
    {
        foreach (var line in layout.Lines)
            foreach (var run in line.Runs)
                Record(GlyphBounds(run, position + new Vector2(0, line.Baseline)), text: true);
        if (Draws)
            _canvas!.DrawText(layout, position, color);
    }

    /// <summary>A text run's box: its advance from the font's ascent to its descent (a text fragment's ink rect, roughly).</summary>
    static RectF GlyphBounds(GlyphRun run, Vector2 origin)
    {
        var face = run.Font.Face;
        var size = run.Style.Size;
        var ascent = face.Ascent * size / face.UnitsPerEm;
        var descent = face.Descent * size / face.UnitsPerEm;
        float left = 0, right = run.Width;
        foreach (var glyph in run.Glyphs)
        {
            left = MathF.Min(left, glyph.X);
            right = MathF.Max(right, glyph.X + size * 0.6f);
        }
        return RectF.FromEdges(origin.X + left, origin.Y - ascent, origin.X + right, origin.Y + descent);
    }

    /// <summary>A texture on the canvas's device (null when only recording hit regions).</summary>
    public GpuImage? CreateTexture(int width, int height, ReadOnlySpan<byte> rgba) => _canvas?.CreateTexture(width, height, rgba);
}
