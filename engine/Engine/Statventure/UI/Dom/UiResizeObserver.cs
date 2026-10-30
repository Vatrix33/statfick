using Statventure.Core;
using Statventure.UI.Layout;

namespace Statventure.UI.Dom;

/// <summary>The box a resize observation watches (ResizeObserverBoxOptions).</summary>
public enum ResizeObserverBox
{
    ContentBox,
    BorderBox,
    DevicePixelContentBox,
}

/// <summary>A ResizeObserverSize: inline and block size (CSS px; device pixels for the device-pixel content box).</summary>
public readonly record struct UiResizeObserverSize(float InlineSize, float BlockSize);

/// <summary>
/// A ResizeObserverEntry, made when its observer delivers: the target's content rect (x and y are
/// the padding's left and top) and its three boxes' sizes, all at the time of delivery; zero for a
/// target without a box (and for inline boxes).
/// </summary>
public sealed class UiResizeObserverEntry
{
    internal UiResizeObserverEntry(UiElement target)
    {
        Target = target;
        if (target.Box is { } box && UiResizeObserver.HasBoxGeometry(box))
        {
            var zoom = box.Style.Zoom;
            ContentRect = new RectF(
                UiResizeObserver.Adjust(box.Padding.Left, zoom), UiResizeObserver.Adjust(box.Padding.Top, zoom),
                UiResizeObserver.Adjust(box.ContentWidth, zoom), UiResizeObserver.Adjust(box.ContentHeight, zoom));
            ContentBoxSize = UiResizeObserver.ZoomAdjustedBox(ResizeObserverBox.ContentBox, box);
            BorderBoxSize = UiResizeObserver.ZoomAdjustedBox(ResizeObserverBox.BorderBox, box);
            DevicePixelContentBoxSize = UiResizeObserver.ZoomAdjustedBox(ResizeObserverBox.DevicePixelContentBox, box);
        }
        else if (target.Document?.SvgBoundingBox(target) is { } bbox)
        {
            // An SVG element: its bounding box (SVGGraphicsElement::GetBBox) for both CSS boxes.
            ContentRect = new RectF(0, 0, bbox.Size.X, bbox.Size.Y);
            ContentBoxSize = BorderBoxSize = new UiResizeObserverSize(bbox.Size.X, bbox.Size.Y);
            DevicePixelContentBoxSize = UiResizeObserver.SnappedDevicePixelContentBox(
                LayoutUnit.Truncate(bbox.Size.X * bbox.Zoom), LayoutUnit.Truncate(bbox.Size.Y * bbox.Zoom), bbox.Origin);
        }
    }

    public UiElement Target { get; }
    public RectF ContentRect { get; }
    public UiResizeObserverSize ContentBoxSize { get; }
    public UiResizeObserverSize BorderBoxSize { get; }
    public UiResizeObserverSize DevicePixelContentBoxSize { get; }
}

/// <summary>
/// A ResizeObserver, as Blink 152's: observed elements are reported, after the frame's layout,
/// when the size of their observed box differs from the last one reported (always at first, even a
/// box of 0×0); the callback runs once per observer with the entries in the order the elements
/// were observed. Delivery goes deeper each round (an element is reported again in the same frame
/// only when it's deeper in the tree than the shallowest reported in the previous round); what's
/// left gives the window an error event and waits for the next frame. The target keeps its
/// observer alive, as in Blink.
/// </summary>
public sealed class UiResizeObserver
{
    readonly UiDocument _document;
    readonly Action<IReadOnlyList<UiResizeObserverEntry>, UiResizeObserver> _callback;
    // In the order they were made (Blink's observations_ is an insertion-ordered set).
    readonly List<Observation> _observations = [];
    readonly List<Observation> _active = [];
    bool _skipped;

    internal sealed class Observation(UiElement target, ResizeObserverBox box)
    {
        public readonly UiElement Target = target;
        public readonly ResizeObserverBox Box = box;
        // Something impossible at first, so the first gathering always reports the target.
        public UiResizeObserverSize Size = new(-1, -1);
    }

    /// <summary>new ResizeObserver(callback) in <paramref name="document"/>'s window.</summary>
    public UiResizeObserver(UiDocument document, Action<IReadOnlyList<UiResizeObserverEntry>, UiResizeObserver> callback)
    {
        _document = document;
        _callback = callback;
        document.AddResizeObserver(this);
    }

    /// <summary>new ResizeObserver(callback) for a callback that doesn't take the observer.</summary>
    public UiResizeObserver(UiDocument document, Action<IReadOnlyList<UiResizeObserverEntry>> callback)
        : this(document, (entries, _) => callback(entries))
    {
    }

    /// <summary>observe: watches <paramref name="target"/>'s <paramref name="box"/> (observing it again with another box starts over).</summary>
    public void Observe(UiElement target, ResizeObserverBox box = ResizeObserverBox.ContentBox)
    {
        var observers = target.ResizeObservations ??= [];
        if (observers.TryGetValue(this, out var existing))
        {
            if (existing.Box == box)
                return;
            _observations.Remove(existing);
            _active.Remove(existing);
            observers.Remove(this);
        }
        var observation = new Observation(target, box);
        _observations.Add(observation);
        observers[this] = observation;
        _document.ScheduleResizeObservations();
    }

    /// <summary>unobserve.</summary>
    public void Unobserve(UiElement target)
    {
        if (target.ResizeObservations is not { } observers || !observers.Remove(this, out var observation))
            return;
        _observations.Remove(observation);
        _active.Remove(observation);
    }

    /// <summary>disconnect: stops watching every target.</summary>
    public void Disconnect()
    {
        var observations = _observations.ToArray();
        _observations.Clear();
        foreach (var observation in observations)
            observation.Target.ResizeObservations?.Remove(this);
        ClearObservations();
    }

    internal bool HasObservations => _observations.Count > 0;

    /// <summary>The targets whose size changed, deeper than <paramref name="deeperThan"/> (shallower ones are skipped); returns the shallowest depth gathered.</summary>
    internal int GatherObservations(int deeperThan)
    {
        var shallowest = DepthBottom;
        foreach (var observation in _observations)
        {
            if (TargetSize(observation) == observation.Size)
                continue;
            var depth = Depth(observation.Target);
            if (depth > deeperThan)
            {
                _active.Add(observation);
                shallowest = Math.Min(shallowest, depth);
            }
            else
                _skipped = true;
        }
        return shallowest;
    }

    internal bool SkippedObservations => _skipped;

    /// <summary>Calls back with the gathered targets' entries (the sizes they report are now the observed ones).</summary>
    internal void DeliverObservations()
    {
        if (_active.Count == 0)
            return;
        var entries = new List<UiResizeObserverEntry>(_active.Count);
        foreach (var observation in _active)
        {
            observation.Size = TargetSize(observation);
            entries.Add(new UiResizeObserverEntry(observation.Target));
        }
        var loop = _document.EventLoop;
        loop.EnterScript();
        try
        {
            _callback(entries, this);
        }
        catch (Exception exception)
        {
            Log.Error("UI", $"A resize observer failed: {exception}");
        }
        finally
        {
            loop.ExitScript();
            ClearObservations();
        }
    }

    internal void ClearObservations()
    {
        _active.Clear();
        _skipped = false;
    }

    internal const int DepthBottom = int.MaxValue;

    /// <summary>The number of elements from the target up to the root (the target included).</summary>
    static int Depth(UiElement target)
    {
        var depth = 0;
        for (UiNode? node = target; node is UiElement; node = node.Parent)
            depth++;
        return depth;
    }

    /// <summary>ResizeObservation::ComputeTargetSize: the observed box in LayoutUnits; zero without a box (or for an inline box).</summary>
    UiResizeObserverSize TargetSize(Observation observation)
    {
        var target = observation.Target;
        if (target.Document != _document)
            return default;
        if (target.Box is { } box)
        {
            if (!HasBoxGeometry(box))
                return default;
            var size = ZoomAdjustedBox(observation.Box, box);
            return new UiResizeObserverSize(LayoutUnit.Truncate(size.InlineSize), LayoutUnit.Truncate(size.BlockSize));
        }
        if (_document.SvgBoundingBox(target) is { } bbox)
        {
            var size = observation.Box == ResizeObserverBox.DevicePixelContentBox
                ? SnappedDevicePixelContentBox(LayoutUnit.Truncate(bbox.Size.X * bbox.Zoom), LayoutUnit.Truncate(bbox.Size.Y * bbox.Zoom), bbox.Origin)
                : new UiResizeObserverSize(bbox.Size.X, bbox.Size.Y);
            return new UiResizeObserverSize(LayoutUnit.Truncate(size.InlineSize), LayoutUnit.Truncate(size.BlockSize));
        }
        return default;
    }

    /// <summary>A LayoutBox in Blink (inline boxes and text aren't).</summary>
    internal static bool HasBoxGeometry(LayoutBox box) => box.Type is not (BoxType.Inline or BoxType.Text or BoxType.LineBreak);

    /// <summary>AdjustForAbsoluteZoom::AdjustLayoutUnit: a LayoutUnit over the zoom, as a LayoutUnit.</summary>
    internal static float Adjust(float value, float zoom) => zoom == 1 ? LayoutUnit.Truncate(value) : LayoutUnit.Truncate(LayoutUnit.Truncate(value) / zoom);

    /// <summary>ResizeObserverUtilities::ComputeZoomAdjustedBox.</summary>
    internal static UiResizeObserverSize ZoomAdjustedBox(ResizeObserverBox kind, LayoutBox box)
    {
        var zoom = box.Style.Zoom;
        return kind switch
        {
            ResizeObserverBox.ContentBox => new(Adjust(box.ContentWidth, zoom), Adjust(box.ContentHeight, zoom)),
            ResizeObserverBox.BorderBox => new(Adjust(box.Width, zoom), Adjust(box.Height, zoom)),
            _ => SnappedDevicePixelContentBox(LayoutUnit.Truncate(box.ContentWidth), LayoutUnit.Truncate(box.ContentHeight), PaintOffset(box)),
        };
    }

    /// <summary>
    /// ComputeSnappedDevicePixelContentBox: the content size in device pixels, snapped as it's painted
    /// from the box's paint offset (SnapSizeToPixel).
    /// </summary>
    internal static UiResizeObserverSize SnappedDevicePixelContentBox(float width, float height, System.Numerics.Vector2 paintOffset) =>
        new(SnapSizeToPixel(width, LayoutUnit.Truncate(paintOffset.X)), SnapSizeToPixel(height, LayoutUnit.Truncate(paintOffset.Y)));

    /// <summary>The box's offset in its transform space (FirstFragment().PaintOffset()): its position in the document, scroll offsets and transforms aside.</summary>
    static System.Numerics.Vector2 PaintOffset(LayoutBox box)
    {
        var position = new System.Numerics.Vector2(box.X, box.Y) + box.RelativeOffset + box.StickyOffset;
        for (var container = box.Container; container is not null; container = container.Container)
            position += new System.Numerics.Vector2(container.X, container.Y) + container.RelativeOffset + container.StickyOffset;
        return position;
    }

    /// <summary>Blink's SnapSizeToPixel: the size's far edge rounded less the near edge rounded, at least 1 for a size above 4/64.</summary>
    static int SnapSizeToPixel(float size, float location)
    {
        var fraction = Raw(location) % 64;
        var result = RoundRaw(fraction + Raw(size)) - RoundRaw(fraction);
        if (result == 0 && MathF.Abs(size) > 4 * LayoutUnit.Epsilon)
            return size > 0 ? 1 : -1;
        return result;

        static int Raw(float value) => (int)MathF.Round(value * 64);
        // LayoutUnit::Round: half up (toward +∞).
        static int RoundRaw(int raw) => (raw + 32) >> 6;
    }
}
