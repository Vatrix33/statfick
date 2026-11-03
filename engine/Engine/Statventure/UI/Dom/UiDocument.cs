using System.Numerics;
using Statventure.Packs;
using Statventure.Text;
using Statventure.UI.Css;
using Statventure.UI.Layout;
using Statventure.UI.Paint;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>The element a point hits, and the box there.</summary>
public readonly record struct HitResult(UiElement Element, LayoutBox Box)
{
    /// <summary>The transform the box was painted with (viewport units): its inverse maps the point into the box's untransformed space.</summary>
    public Matrix3x2 Transform { get; init; } = Matrix3x2.Identity;
    /// <summary>For text: the line item hit, and its rectangle (untransformed; it starts at the item's first glyph).</summary>
    public LineItem? TextItem { get; init; }
    public Core.RectF TextRect { get; init; }
}

/// <summary>
/// A UI document: the element tree (html with a body), its stylesheets, and the style and layout
/// state derived from them. Changes to the tree, attributes and interaction states invalidate
/// styles and layout; <see cref="Update"/> brings everything up to date.
/// </summary>
public sealed partial class UiDocument
{
    readonly Dictionary<Stylesheet, bool> _authorSheets = [];
    int _styleVersion = -1;
    // What the next update has to redo: the box tree, layout, painting.
    bool _needsBoxes = true, _needsLayout = true, _needsPaint = true;
    Vector2 _viewport = new(1280, 720);
    bool _reducedMotion;
    float _rootFontSize = 16;
    BoxBuilder? _builder;

    public UiElement Root { get; }
    public UiElement Body { get; }
    public StyleEngine Styles { get; } = new();
    public AnimationEngine Animations { get; }
    /// <summary>The window has the system's focus (else :focus matches nothing and the caret hides).</summary>
    public bool WindowFocused { get; private set; } = true;

    /// <summary>
    /// The window gained or lost the system's focus (Blink's FocusController::SetFocused): leaving,
    /// the focused element commits (a text field's change), gets blur and focusout, then the
    /// document blur; coming back, the
    /// document gets focus, then the element focus and focusin. The focused element stays.
    /// </summary>
    internal void SetWindowFocused(bool focused)
    {
        if (WindowFocused == focused)
            return;
        WindowFocused = focused;
        var element = FocusedElement is { } f && f.Document == this ? f : null;
        if (element is not null)
            StateChanged(element, [PseudoClassKind.Focus, PseudoClassKind.FocusVisible, PseudoClassKind.FocusWithin]);
        if (!focused && element is not null)
        {
            // Blink's SetFocused(false): a text field commits (change) before the blur.
            element.FocusChanged(false);
            element.DispatchEvent(new UiFocusEvent("blur", bubbles: false) { IsTrusted = true });
            if (FocusedElement == element)
                element.DispatchEvent(new UiFocusEvent("focusout", bubbles: true) { IsTrusted = true });
        }
        // The window's own focus and blur are plain Events, not FocusEvents (Reference/visibility-results.json).
        DispatchEvent(new UiEvent(focused ? "focus" : "blur", bubbles: false, cancelable: false) { IsTrusted = true });
        if (focused && element is not null && FocusedElement == element)
        {
            element.DispatchEvent(new UiFocusEvent("focus", bubbles: false) { IsTrusted = true });
            if (FocusedElement == element)
                element.DispatchEvent(new UiFocusEvent("focusin", bubbles: true) { IsTrusted = true });
        }
        Repaint();
    }

    /// <summary>The colour under the page (a browser's white; transparent to show what is behind the UI).</summary>
    public Core.Color BaseBackground { get; set; } = Core.Color.FromBytes(255, 255, 255);
    public FontLibrary Fonts { get; }
    public LayoutEngine Layout { get; }
    public UiElement? FocusedElement { get; internal set; }
    /// <summary>Input handling: pointer, keyboard, focus (fed by the window host, or by tests).</summary>
    public UiInput Input { get; }
    /// <summary>The viewport in device pixels (what layout and painting work in, the size to render at).</summary>
    public Vector2 ViewportSize => _viewport;

    float _deviceScale = 1, _pageZoom = 1;
    Vector2 _viewportUnits = new(1280, 720);

    /// <summary>Device pixels per window unit (the screen's scale; Chromium's device scale factor).</summary>
    public float DeviceScale => _deviceScale;
    /// <summary>The page zoom (Chromium's zoom factor: 1.2 to the power of the zoom level).</summary>
    public float PageZoom => _pageZoom;
    /// <summary>
    /// Device pixels per CSS px: the device scale times the page zoom (Blink's effective zoom, by which
    /// every length is computed). Layout, hit testing and painting work in device pixels; the DOM
    /// (event coordinates, geometry, scroll offsets) in CSS px.
    /// </summary>
    public float Zoom => _deviceScale * _pageZoom;
    /// <summary>Box-tree constructs handled approximately in the last layout (see <see cref="BoxBuilder.Diagnostics"/>).</summary>
    public IReadOnlyList<string> LayoutDiagnostics => _builder?.Diagnostics ?? [];

    public UiDocument(FontLibrary fonts)
    {
        Fonts = fonts;
        Animations = new AnimationEngine(Styles);
        Root = new UiElement("html");
        Root.Attach(this);
        Body = Root.Append("body");
        Styles.Add(UserAgentStyles.Sheet);
        Styles.SetEnvironment(new MediaEnvironment(_viewport.X, _viewport.Y));
        Layout = new LayoutEngine(new LayoutContext { Fonts = fonts, ViewportWidth = _viewport.X, ViewportHeight = _viewport.Y });
        Input = new UiInput(this);
    }

    // ---------- Events ----------

    internal EventListeners? Listeners;

    /// <summary>Calls <paramref name="listener"/> for events reaching the document (every bubbling event, and document events such as resize).</summary>
    public void AddEventListener(string type, Action<UiEvent> listener, bool capture = false) =>
        (Listeners ??= new()).Add(type, listener, listener, capture);

    public void AddEventListener<T>(string type, Action<T> listener, bool capture = false) where T : UiEvent =>
        (Listeners ??= new()).Add(type, listener, e =>
        {
            if (e is T typed)
                listener(typed);
        }, capture);

    public void RemoveEventListener(string type, Delegate listener, bool capture = false) => Listeners?.Remove(type, listener, capture);

    /// <summary>Dispatches an event to the document itself.</summary>
    public bool DispatchEvent(UiEvent e) => Dispatch(e, null, this);

    /// <summary>
    /// The DOM's dispatch (Blink's EventDispatcher): the path (the target, its ancestors, the
    /// document) is fixed first; then capture from the document down, the target, and bubbling
    /// back up when the event bubbles. Real clicks and presses don't reach a disabled control or
    /// its ancestors. A click runs its activation target's activation (a checkbox toggles before
    /// the listeners and is restored if they cancel); then, unless cancelled, the default handlers
    /// (labels, buttons, summaries ...) run from the target up. Returns false when a listener
    /// prevented the default action.
    /// </summary>
    internal static bool Dispatch(UiEvent e, UiElement? target, UiDocument? document)
    {
        e.Target = target;
        if (document is not null)
            e.TimeStamp = document.Animations.Time;
        var path = new List<UiElement>();
        for (var element = target; element is not null; element = element.Parent)
            path.Add(element);
        // MouseEvent::DispatchEvent: the path ends below the first disabled form control.
        var truncated = false;
        if (e.IsTrusted && e is UiMouseEvent && e.Type is "click" or "mousedown" or "mouseup" or "dblclick")
        {
            var disabled = path.FindIndex(p => p.IsDisabled);
            if (disabled >= 0)
            {
                if (disabled == 0)
                    return true;
                path.RemoveRange(disabled, path.Count - disabled);
                truncated = true;
            }
        }
        // The activation target: the target, or (for a bubbling click) its nearest ancestor with activation behavior.
        var isClick = e is UiMouseEvent && e.Type == "click";
        UiElement? activation = null;
        if (isClick)
            for (var i = 0; i < path.Count && (i == 0 || e.Bubbles); i++)
                if (path[i].HasActivationBehavior)
                {
                    activation = path[i];
                    break;
                }
        var state = activation?.LegacyPreActivationBehavior();
        void At(object current, EventListeners? listeners, EventPhase phase)
        {
            if (listeners is null)
                return;
            e.Phase = phase;
            e.CurrentTarget = current;
            listeners.Invoke(e, document?.EventLoop);
        }
        var listenersAtDocument = document is not null && !truncated;
        if (target is null)
        {
            if (document is not null)
                At(document, document.Listeners, EventPhase.AtTarget);
        }
        else
        {
            if (listenersAtDocument)
                At(document!, document!.Listeners, EventPhase.Capturing);
            for (var i = path.Count - 1; i >= 1 && !e.PropagationStopped; i--)
                At(path[i], path[i].Listeners, EventPhase.Capturing);
            if (!e.PropagationStopped)
                At(target, target.Listeners, EventPhase.AtTarget);
            if (e.Bubbles)
            {
                for (var i = 1; i < path.Count && !e.PropagationStopped; i++)
                    At(path[i], path[i].Listeners, EventPhase.Bubbling);
                if (!e.PropagationStopped && listenersAtDocument)
                    At(document!, document!.Listeners, EventPhase.Bubbling);
            }
        }
        e.Phase = EventPhase.None;
        e.CurrentTarget = null;
        e.PropagationStopped = e.ImmediatePropagationStopped = false;
        if (isClick && activation is not null)
            activation.RunActivationBehavior(e, state);
        // Default handlers: trusted events and clicks, from the target up until one handles it.
        if (target is not null && !e.DefaultPrevented && !e.DefaultHandled && (e.IsTrusted || isClick))
            for (var i = 0; i < path.Count && (i == 0 || e.Bubbles); i++)
            {
                path[i].DefaultEventHandler(e);
                if (e.DefaultHandled || e.DefaultPrevented)
                    break;
            }
        return !e.DefaultPrevented;
    }

    // ---------- Stylesheets and environment ----------

    VirtualFileSystem? _files;
    // The faces each author sheet's @font-face rules added to the font library (Q24).
    readonly Dictionary<Stylesheet, List<FontFace>> _sheetFaces = [];

    /// <summary>
    /// The game's data the document reads (Docs/Assets.md, "UI"; S7-1, S7-4; the game sets
    /// <c>Application.Data</c>): every url (a stylesheet's, the markup's) is a data path in it from its root.
    /// With one, the @font-face rules of the author sheets are honoured: each face's font is read from it
    /// and added to <see cref="Fonts"/> when its sheet is added (or when this is set), and given back when
    /// the sheet is removed; a font that isn't there or doesn't load is an error naming the sheet (a
    /// <see cref="DataFileException"/> naming the pack and entry of a compiled sheet, an
    /// <see cref="InvalidDataException"/> naming one parsed from text), and nothing changes. Without one,
    /// @font-face rules are ignored (the fonts are the library's). The host reads images and SVG text from it.
    /// </summary>
    public VirtualFileSystem? Files
    {
        get => _files;
        set
        {
            if (ReferenceEquals(value, _files))
                return;
            var added = new Dictionary<Stylesheet, List<FontFace>>();
            try
            {
                if (value is not null)
                    foreach (var sheet in _authorSheets.Keys)
                        added[sheet] = AcquireFaces(sheet, value);
            }
            catch
            {
                foreach (var faces in added.Values)
                    ReleaseFaces(faces);
                throw;
            }
            foreach (var faces in _sheetFaces.Values)
                ReleaseFaces(faces);
            _sheetFaces.Clear();
            foreach (var (sheet, faces) in added)
                _sheetFaces[sheet] = faces;
            _files = value;
            InvalidateLayout();
        }
    }

    public Stylesheet AddStylesheet(string css, string name = "")
    {
        var sheet = Stylesheet.Parse(css, name);
        AddStylesheet(sheet);
        return sheet;
    }

    /// <summary>Adds an author sheet; with <see cref="Files"/>, its @font-face fonts are loaded first (an error leaves the document as it was).</summary>
    public void AddStylesheet(Stylesheet sheet)
    {
        if (_files is not null && !_sheetFaces.ContainsKey(sheet))
        {
            var faces = AcquireFaces(sheet, _files);
            _sheetFaces[sheet] = faces;
            if (faces.Count > 0)
                InvalidateLayout();
        }
        Styles.Add(sheet);
        _authorSheets[sheet] = true;
    }

    public void RemoveStylesheet(Stylesheet sheet)
    {
        Styles.Remove(sheet);
        _authorSheets.Remove(sheet);
        if (_sheetFaces.Remove(sheet, out var faces))
        {
            ReleaseFaces(faces);
            if (faces.Count > 0)
                InvalidateLayout();
        }
    }

    /// <summary>The faces of a sheet's @font-face rules, read from <paramref name="files"/> (all or none: a failure gives back those already taken).</summary>
    List<FontFace> AcquireFaces(Stylesheet sheet, VirtualFileSystem files)
    {
        var faces = new List<FontFace>();
        try
        {
            foreach (var rule in sheet.FontFaces)
            {
                FontFaceDescriptors descriptors;
                try
                {
                    descriptors = FontFaceDescriptors.Of(rule);
                }
                catch (InvalidDataException problem)
                {
                    throw SheetProblem(sheet, problem.Message, problem);
                }
                byte[] data;
                try
                {
                    data = files.ReadAllBytes(descriptors.Source);
                }
                catch (Exception problem) when (problem is IOException or ArgumentException)
                {
                    throw SheetProblem(sheet, $"the @font-face font '{descriptors.Source}' of {descriptors.Family} can't be read: {problem.Message}", problem);
                }
                try
                {
                    faces.Add(Fonts.AcquireSheetFace(data, descriptors.Family, descriptors.Weight, descriptors.Italic, descriptors.WeightMax));
                }
                catch (Exception problem) when (problem is not OutOfMemoryException)
                {
                    throw SheetProblem(sheet, $"the @font-face font '{descriptors.Source}' of {descriptors.Family} doesn't load: {problem.Message}", problem);
                }
            }
        }
        catch
        {
            ReleaseFaces(faces);
            throw;
        }
        return faces;
    }

    void ReleaseFaces(List<FontFace> faces)
    {
        foreach (var face in faces)
            Fonts.ReleaseSheetFace(face);
    }

    /// <summary>
    /// A problem with a sheet's fonts, naming the sheet: a <see cref="DataFileException"/> naming its pack and
    /// entry (or loose file) when it was loaded from the data, else an <see cref="InvalidDataException"/> naming it.
    /// </summary>
    static Exception SheetProblem(Stylesheet sheet, string problem, Exception inner)
    {
        if (sheet.LoadedFrom is { } loaded)
        {
            try
            {
                return loaded.Files.Problem(loaded.Path, problem, inner);
            }
            catch (Exception gone) when (gone is ObjectDisposedException or FileNotFoundException)
            {
                // The data it came from is gone (disposed, or reloaded without it): its path names it.
                return new InvalidDataException($"the stylesheet '{loaded.Path}': {problem}", inner);
            }
        }
        return new InvalidDataException($"the stylesheet '{sheet.Name}': {problem}", inner);
    }

    /// <summary>The viewport size in window units (DIPs; CSS px at zoom 1) and the reduced-motion preference.</summary>
    public void SetViewport(float width, float height, bool reducedMotion = false)
    {
        if (_viewportUnits == new Vector2(width, height) && reducedMotion == _reducedMotion)
            return;
        _viewportUnits = new Vector2(width, height);
        _reducedMotion = reducedMotion;
        ApplyViewport();
    }

    /// <summary>
    /// The screen's scale (device pixels per window unit) and the page zoom. Both zoom the page as
    /// Chromium does (zoom-for-DSF): lengths are computed in device pixels and layout runs there.
    /// </summary>
    public void SetZoom(float deviceScale, float pageZoom = 1)
    {
        if (deviceScale == _deviceScale && pageZoom == _pageZoom)
            return;
        _deviceScale = deviceScale;
        _pageZoom = pageZoom;
        ApplyViewport();
    }

    void ApplyViewport()
    {
        var zoom = Zoom;
        // Layout's viewport in whole device pixels (a window's framebuffer, a frame's snapped rect);
        // media queries see CSS px.
        _viewport = new Vector2(MathF.Round(_viewportUnits.X * _deviceScale, MidpointRounding.AwayFromZero), MathF.Round(_viewportUnits.Y * _deviceScale, MidpointRounding.AwayFromZero));
        var zoomChanged = Styles.Environment.Zoom != zoom;
        Styles.SetEnvironment(new MediaEnvironment(_viewport.X / zoom, _viewport.Y / zoom, _reducedMotion, zoom, _deviceScale));
        if (zoomChanged)
            _rootFontSize = Styles.Initial.SpecifiedFontSize;
        Layout.Context.ViewportWidth = _viewport.X;
        Layout.Context.ViewportHeight = _viewport.Y;
        Layout.Context.DeviceScale = _deviceScale;
        // Viewport units: everything is recomputed.
        InvalidateStyle(Root, subtree: true);
        _needsLayout = true;
        Repaint();
    }

    // ---------- Queries ----------

    public UiElement CreateElement(string tag) => new(tag);

    public IEnumerable<UiElement> AllElements()
    {
        yield return Root;
        foreach (var element in Root.Descendants())
            yield return element;
    }

    public UiElement? GetElementById(string id) => AllElements().FirstOrDefault(e => e.Id == id);

    public UiElement? QuerySelector(string selector) => QuerySelectorAll(selector).FirstOrDefault();

    public IEnumerable<UiElement> QuerySelectorAll(string selector)
    {
        var list = SelectorParser.ParseList(selector) ?? throw new ArgumentException("invalid selector: " + selector);
        return AllElements().Where(list.Matches);
    }

    // ---------- Invalidation ----------

    internal void InvalidateStyle(UiElement element, bool subtree)
    {
        element.NeedsStyle = true;
        if (subtree)
            element.SubtreeNeedsStyle = true;
        for (var ancestor = element.Parent; ancestor is not null && !ancestor.ChildNeedsStyle; ancestor = ancestor.Parent)
            ancestor.ChildNeedsStyle = true;
    }

    /// <summary>Text changed: the boxes of <paramref name="element"/>'s children are rebuilt.</summary>
    internal void TextChanged(UiElement? element)
    {
        if (element is null)
            return;
        // :empty counts text.
        var features = Styles.Features;
        InvalidateReach(element, features.Empty);
        if (features.HasAny)
            InvalidateHas(element, includeSelf: true);
        RecollectChildren(element);
        Repaint();
    }

    /// <summary>A textarea's text changed: its scrollable overflow (and so its scrollbars) is laid out again.</summary>
    internal void TextAreaTextChanged(UiElement textarea)
    {
        RelayoutBoxes(textarea);
        Repaint();
    }

    /// <summary>A child was added (<paramref name="inserted"/>) or removed; <paramref name="next"/> is the node after where it is or was.</summary>
    internal void ChildrenChanged(UiElement parent, UiNode child, bool inserted, UiNode? next)
    {
        var features = Styles.Features;
        if (inserted && child is UiElement element)
            InvalidateStyle(element, subtree: true);
        // :empty on the parent; positions (:first-child, :nth-child ...) of every child; what sibling
        // combinators see after the change; :has() arguments that may now match or not.
        InvalidateReach(parent, features.Empty);
        if (features.Structural != Reach.None)
            foreach (var sibling in parent.ElementChildren)
                InvalidateReach(sibling, features.Structural);
        if (features.SiblingCombinators)
            for (var node = next; node is not null; node = node.NextSibling)
                if (node is UiElement following)
                    InvalidateStyle(following, subtree: true);
        if (features.UsesHas && (features.HasAny || (child is UiElement changed && TouchesHas(changed, features))))
            InvalidateHas(parent, includeSelf: true);
        RecollectChildren(parent);
        Repaint();
    }

    /// <summary>Restyles what a change on <paramref name="element"/> reaches.</summary>
    void InvalidateReach(UiElement element, Reach reach)
    {
        if ((reach & (Reach.Self | Reach.Descendants)) != 0)
            InvalidateStyle(element, subtree: (reach & Reach.Descendants) != 0);
        if ((reach & Reach.Siblings) != 0)
            for (var sibling = element.NextElementSibling; sibling is not null; sibling = sibling.NextElementSibling)
                InvalidateStyle(sibling, subtree: true);
    }

    /// <summary>Something a :has() argument looks at changed at <paramref name="element"/>: its ancestors may match differently.</summary>
    void InvalidateHas(UiElement element, bool includeSelf = false)
    {
        var reach = Styles.Features.HasReach;
        for (var ancestor = includeSelf ? element : element.Parent; ancestor is not null; ancestor = ancestor.Parent)
            InvalidateReach(ancestor, reach);
    }

    /// <summary>Does anything in the subtree have what a :has() argument looks at?</summary>
    static bool TouchesHas(UiElement element, RuleFeatures features)
    {
        if (features.HasTags.Contains(element.Tag) || (element.Id is { } id && features.HasIds.Contains(id)))
            return true;
        foreach (var cls in element.ClassSet)
            if (features.HasClasses.Contains(cls))
                return true;
        foreach (var name in element.AttributeMap.Keys)
            if (features.HasAttributes.Contains(name))
                return true;
        foreach (var child in element.Children)
            if (child is UiElement childElement && TouchesHas(childElement, features))
                return true;
        return false;
    }

    /// <summary>Something the layout reads changed outside styles and the tree (images, fonts): everything is laid out again.</summary>
    public void InvalidateLayout()
    {
        static void Mark(LayoutBox box)
        {
            box.Invalidate();
            box.ChildNeedsLayout = true;
            if (box.Marker is { } marker)
                Mark(marker);
            foreach (var child in box.Children)
                Mark(child);
        }
        Mark(Layout.Viewport);
        _needsLayout = true;
        Repaint();
    }

    // ---------- Boxes and layout ----------

    /// <summary>The element's own boxes are made again (its parent collects its child boxes again).</summary>
    void RebuildBoxes(UiElement element)
    {
        element.BoxNeedsRebuild = true;
        if (element.Parent is { } parent)
            RecollectChildren(parent);
        else
            element.DescendantBoxesNeedRebuild = true;
        _needsBoxes = true;
    }

    /// <summary>The element collects its child boxes again (text, children or generated content changed).</summary>
    void RecollectChildren(UiElement element)
    {
        // Without a box of its own (display: contents) its children's boxes sit among its parent's.
        var target = element;
        while (target.Parent is not null && target.Style is { Display: Display.Contents } && !BoxBuilder.IsReplaced(target))
            target = target.Parent;
        target.ChildBoxesNeedRebuild = true;
        for (var e = target; e is not null; e = e.Parent)
            e.DescendantBoxesNeedRebuild = true;
        _needsBoxes = true;
    }

    /// <summary>The element's boxes are laid out again: its own and those made from its style (anonymous boxes, text, markers, pseudo-elements).</summary>
    void RelayoutBoxes(UiElement element)
    {
        if (element.Box is not { } box)
        {
            // display: contents: its text sits in an ancestor's boxes.
            if (element.Style is { Display: Display.Contents })
                RecollectChildren(element);
            return;
        }
        void Mark(LayoutBox parent)
        {
            foreach (var child in parent.Children)
                if (child.Element is null || child.Element == element)
                {
                    child.Invalidate();
                    Mark(child);
                }
        }
        box.MarkNeedsLayout();
        Mark(box);
        if (box.Marker is { } marker)
        {
            marker.Invalidate();
            Mark(marker);
        }
        _needsLayout = true;
    }

    /// <summary>Something only painting reads changed (scroll offsets, control values).</summary>
    public void InvalidatePaint() => Repaint();

    internal void Repaint()
    {
        _needsPaint = true;
        VisualVersion++;
    }

    // ---------- Hit testing ----------

    readonly List<HitRegion> _hitRegions = [];
    int _hitVersion = -1;
    UiPainter? _hitPainter;

    /// <summary>Bumped whenever what the document shows changes (hit regions recorded before are stale).</summary>
    internal int VisualVersion { get; private set; }

    /// <summary>The painter's hit regions of the document as painted at <paramref name="version"/>.</summary>
    internal void SetHitRegions(List<HitRegion> regions, int version)
    {
        _hitRegions.Clear();
        _hitRegions.AddRange(regions);
        _hitVersion = version;
    }

    /// <summary>
    /// The element at a point of the viewport, as painted (CSS paint order, transforms, clips,
    /// pointer-events, visibility): the topmost box's element (text counts as its element,
    /// generated content as its element's); the root element where nothing else is. Styles and
    /// layout are brought up to date first, as browsers do.
    /// </summary>
    public HitResult? HitTest(Vector2 point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= _viewport.X || point.Y >= _viewport.Y)
            return null;
        UpdateLayout();
        // The viewport's own scrollbars aren't any element's.
        var viewport = Layout.Viewport;
        if (point.X >= viewport.Width - viewport.VerticalScrollbar || point.Y >= viewport.Height - viewport.HorizontalScrollbar)
            return null;
        if (_hitVersion != VisualVersion)
        {
            _hitPainter ??= new UiPainter(this);
            _hitPainter.RecordHitRegions();
        }
        for (var i = _hitRegions.Count - 1; i >= 0; i--)
        {
            var region = _hitRegions[i];
            if (!region.Contains(point))
                continue;

            // Inside an inline SVG its shapes decide (and parts of its overflow are empty).
            if (region.Box is { Type: BoxType.Replaced, Element.Tag: "svg" } svg)
            {
                _hitPainter ??= new UiPainter(this);
                if (Matrix3x2.Invert(region.Transform, out var inverse) && _hitPainter.HitSvg(svg, Vector2.Transform(point, inverse)) is { } shape)
                    return new HitResult(shape, svg);
                continue;
            }
            if (ElementOf(region.Box) is { } element)
                return new HitResult(element, region.Box) { Transform = region.Transform, TextItem = region.Item, TextRect = region.Rect };
        }
        return Root.Box is { } root ? new HitResult(Root, root) : null;
    }

    /// <summary>For diagnostics: the hit regions at a point, topmost first.</summary>
    internal IEnumerable<HitRegion> HitRegionsAt(Vector2 point)
    {
        HitTest(point);
        for (var i = _hitRegions.Count - 1; i >= 0; i--)
            if (_hitRegions[i].Contains(point) || HitRegion.Inside(_hitRegions[i].Rect, default, point))
                yield return _hitRegions[i];
    }

    /// <summary>The element a box shows: its own, a text box's parent element, else the nearest ancestor's.</summary>
    static UiElement? ElementOf(LayoutBox box)
    {
        for (var b = box; b is not null; b = b.Parent)
        {
            if (b.Element is { } element)
                return element;
            if (b.TextNode?.Parent is { } parent)
                return parent;
        }
        return null;
    }

    internal void NodeRemoved(UiNode node)
    {
        if (node is UiElement removed)
        {
            Animations.Forget(removed);
            ForgetScrolling(removed);
            foreach (var descendant in removed.Descendants())
            {
                Animations.Forget(descendant);
                ForgetScrolling(descendant);
            }
        }
        if (FocusedElement is { } focused && (focused == node || (node is UiElement element && focused.IsDescendantOf(element))))
            FocusedElement = null;
        // Its boxes go with it (its parent collects its child boxes again).
        if (node is UiElement gone)
            BoxBuilder.ClearBoxes(gone);
        Repaint();
    }

    internal void AttributeChanged(UiElement element, string name, string? oldValue)
    {
        // Restyle what selectors looking at this attribute (its classes, its id, the states it decides) reach.
        var features = Styles.Features;
        var reach = features.Attributes.GetValueOrDefault(name);
        var has = features.HasAttributes.Contains(name);
        switch (name)
        {
            case "class":
            {
                var old = new HashSet<string>((oldValue ?? "").Split((char[])[' ', '\t', '\n', '\r', '\f'], StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
                foreach (var cls in element.ClassSet)
                    if (!old.Remove(cls))
                    {
                        reach |= features.Classes.GetValueOrDefault(cls);
                        has |= features.HasClasses.Contains(cls);
                    }
                foreach (var cls in old)
                {
                    reach |= features.Classes.GetValueOrDefault(cls);
                    has |= features.HasClasses.Contains(cls);
                }
                break;
            }
            case "id":
                foreach (var id in new[] { oldValue, element.Id })
                    if (id is not null)
                    {
                        reach |= features.Ids.GetValueOrDefault(id);
                        has |= features.HasIds.Contains(id);
                    }
                break;
            case "style":
                reach |= Reach.Self;
                break;
        }
        // Presentational hints (width and height, an SVG element's transform) are part of its own style.
        if (StyleEngine.IsPresentationAttribute(element, name))
            reach |= Reach.Self;
        foreach (var state in RuleFeatures.StatesOf(name))
        {
            reach |= features.States.GetValueOrDefault(state);
            has |= features.HasStates.Contains(state);
        }
        // A disabled fieldset disables the controls inside it.
        if (name == "disabled" && element.Tag == "fieldset" && reach != Reach.None)
            reach |= Reach.Descendants;
        InvalidateReach(element, reach);
        if (has)
            InvalidateHas(element);
        // Attributes reach boxes and layout through styles, except these (input types, details open,
        // list starts, image sources, sizes ...) and those generated content shows.
        // (Those that only reach rendering through styles repaint when the styles say so.)
        if (!StyleOnlyAttribute(name) || UsedByContent(element.BeforeStyle, name) || UsedByContent(element.AfterStyle, name))
        {
            RebuildBoxes(element);
            Repaint();
        }
    }

    static bool StyleOnlyAttribute(string name) =>
        name is "class" or "id" or "style" or "title" or "role" or "tabindex" or "lang" or "dir" or "draggable" or "transform" or "inert"
        || name.StartsWith("data-", StringComparison.Ordinal) || name.StartsWith("aria-", StringComparison.Ordinal);

    static bool UsedByContent(ComputedStyle? style, string name) =>
        style?.Content is { } content && content.Any(item => item.Attribute == name);

    /// <summary>An interaction state of the element changed (hover, active, focus ...).</summary>
    internal void StateChanged(UiElement element, PseudoClassKind[] states)
    {
        var features = Styles.Features;
        var reach = Reach.None;
        var has = false;
        foreach (var state in states)
        {
            var stateReach = features.States.GetValueOrDefault(state);
            has |= features.HasStates.Contains(state);
            // :focus-within holds for the focused element's ancestors too.
            if (state == PseudoClassKind.FocusWithin && stateReach != Reach.None)
                for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
                    InvalidateReach(ancestor, stateReach);
            reach |= stateReach;
        }
        InvalidateReach(element, reach);
        if (has)
            InvalidateHas(element);
        // Controls paint their states themselves.
        Repaint();
    }

    // ---------- Update ----------

    /// <summary>True when styles, layout or painting are out of date, or animations are running (the next frame differs).</summary>
    public bool IsDirty => NeedsStyleOrLayout || _needsPaint || Animations.HasActive || ScrollingPending || Input.NeedsFrames || _bubble is not null || ProgressAnimating || _tasks.Count > 0 || _lateTasks.Count > 0 || CaretBlinking || _frameCallbacks.Count > 0 || _resizeObservationsScheduled;

    bool NeedsStyleOrLayout => _needsBoxes || _needsLayout || Root.NeedsStyle || Root.ChildNeedsStyle || Styles.Version != _styleVersion || Animations.NeedsTick;

    /// <summary>Called by whoever paints the document once it has painted the current state.</summary>
    public void Painted() => _needsPaint = false;

    /// <summary>Brings styles and layout up to date (as browsers do before answering geometry and scroll questions).</summary>
    public void UpdateLayout()
    {
        if (NeedsStyleOrLayout)
        {
            // Style and layout only (as a browser's forced layout): tasks and timers wait for the frame.
            var wasInFrameSteps = _inFrameSteps;
            _inFrameSteps = true;
            try
            {
                Update();
            }
            finally
            {
                _inFrameSteps = wasInFrameSteps;
            }
        }
        else if (_stickyDirty)
            UpdateStickyOffsets();
    }

    /// <summary>Recomputes what changed at the current animation time.</summary>
    public void Update() => Update(Animations.Time);

    /// <summary>
    /// A frame at <paramref name="time"/> (seconds): timers due before it, the frame's steps (input,
    /// scrolling, the animation timing update, resize, scroll, media query and animation events,
    /// frame callbacks), then styles, animations, the box tree and layout, then resize observers
    /// (which may lay out again). Called again during the frame (and by forced layouts) it only
    /// brings styles and layout up to date. A hidden document gets no frame: only its tasks and
    /// throttled timers run; an unloaded one does nothing.
    /// </summary>
    public void Update(double time)
    {
        if (Unloaded)
            return;
        if (Hidden && !_inFrameSteps)
        {
            UpdateHidden(time);
            return;
        }
        Animations.Advance(time);
        if (_inFrameSteps)
        {
            UpdateStyleAndLayout(time);
            return;
        }
        _inFrameSteps = true;
        try
        {
            // Timers due before the frame run first, each at its time.
            EventLoop.RunUntil(time);
            RunTasks(late: false);
            _animationFrameTime = time;
            _inRendering = true;
            Input.Advance(time);
            AdvanceScrolling(time);
            // Blink's animation timing update queues animation and transition events before the
            // frame's scroll steps; they're dispatched after them. A microtask checkpoint follows
            // the update (DocumentAnimations::UpdateAnimationTimingForAnimationFrame).
            Animations.UpdateTiming(time);
            AdvanceFrameClients(time);
            EventLoop.PerformMicrotaskCheckpoint();
            DispatchResizeEvent();
            DispatchScrollEvents();
            NotifyMediaQueryLists();
            DispatchAnimationEvents();
            DispatchFrameClientEvents();
            RunFrameCallbacks(time);
            // selectionchange comes after the frame's scroll events (Chromium renders right after input, its tasks wait).
            RunTasks(late: true);
            UpdateStyleAndLayout(time);
            RunResizeObservers(time);
            // After paint Blink commits pending animations (their start time is this frame's).
            Animations.CommitPending(TimelineTime);
        }
        finally
        {
            _inRendering = false;
            _inFrameSteps = false;
        }
        // The frame is a task: microtasks it queued run now.
        EventLoop.PerformMicrotaskCheckpoint();
    }

    /// <summary>Styles, animations at <paramref name="time"/>, the box tree and layout, and what follows layout (popups, focus fixup, hover).</summary>
    void UpdateStyleAndLayout(double time)
    {
        UpdateStyles();
        var changed = Animations.Tick(time, (element, pseudo) => pseudo switch
        {
            "before" => element.BeforeStyle,
            "after" => element.AfterStyle,
            _ => element.Style,
        });
        var restyle = false;
        foreach (var (element, pseudo, visible, layout, inherited) in changed)
        {
            // Paint-only values were written in place; layout values need a new layout; inherited ones
            // reach the children through their styles.
            Repaint();
            if (layout)
                RelayoutBoxes(element);
            if (inherited && pseudo.Length == 0)
            {
                // A new instance: children's styles are shared by their parent's style instance, and
                // this one's values changed.
                element.Style = visible.Clone();
                foreach (var child in element.ElementChildren)
                    InvalidateStyle(child, subtree: true);
                restyle = true;
            }
            if (inherited && pseudo.Length > 0)
                RelayoutBoxes(element);
        }
        if (restyle)
            UpdateStyles();
        if (_needsBoxes)
        {
            _builder ??= new BoxBuilder(Layout.Viewport);
            _builder.Build(Root);
            _needsBoxes = false;
            _needsLayout = true;
        }
        if (_needsLayout)
        {
            Layout.Layout();
            _needsLayout = false;
            // After layout Blink dirties the hover state (PerformPostLayoutTasks: a fake mouse move).
            _hoverDirty = true;
            Repaint();
            ClampScrollOffsets();
            CollectStickyBoxes();
        }
        if (_stickyDirty)
            UpdateStickyOffsets();
        if (_bubble is not null)
        {
            PlaceValidationBubble();
            if (_needsLayout || Root.NeedsStyle || Root.ChildNeedsStyle)
                Update(time);
        }
        if (Input.OpenSelectElement is { } select && Input.SelectPopup is { } popup)
        {
            PlaceSelectPopup(select, popup);
            if (_needsLayout || Root.NeedsStyle || Root.ChildNeedsStyle)
                Update(time);
            Input.ScrollPopupHighlightIntoView();
        }
        if (Input.Tooltip is { } tooltip)
        {
            PlaceTooltip(tooltip, Input.TooltipAnchor);
            UpdateLayout();
        }
        if (_refreshedVersion != VisualVersion || _hoverDirty)
        {
            _refreshedVersion = VisualVersion;
            // Focus fixup (Document::ClearFocusedElementIfNeeded): a focused element that can no
            // longer take focus (hidden, disabled, inert) is blurred by a task, with blur and
            // focusout, if it still can't then.
            if (FocusedElement is { IsFocusable: false } && !_focusFixupScheduled)
            {
                _focusFixupScheduled = true;
                EventLoop.PostTask(() =>
                {
                    _focusFixupScheduled = false;
                    UpdateLayout();
                    if (FocusedElement is { IsFocusable: false } still)
                        still.Blur();
                });
            }
            // What is under a still pointer may have changed. :hover follows only after layout and
            // scrolling (Blink's hover state is dirtied there, not by paint-only changes such as
            // pointer-events or inert); the cursor follows any change (its update timer).
            Input.Refresh(rehover: _hoverDirty);
            _hoverDirty = false;
            if (Root.NeedsStyle || Root.ChildNeedsStyle)
                Update(time);
        }
    }

    int _refreshedVersion = -1;
    bool _focusFixupScheduled;
    /// <summary>Layout or a scroll changed what's under the pointer (Blink's MarkHoverStateDirty).</summary>
    bool _hoverDirty;
    bool _inFrameSteps;

    ComputedStyle? ForgetPseudo(UiElement element, string pseudo, ComputedStyle? style)
    {
        Animations.Forget(element, pseudo);
        return style;
    }

    /// <summary>The animation and transition events the frame's timing update (and cancellations since the last frame) queued.</summary>
    void DispatchAnimationEvents()
    {
        foreach (var (target, e) in Animations.TakeEvents())
            if (target is UiElement element)
                element.DispatchEvent(e);
            else if (target is UiAnimation animation)
                animation.Dispatch(e);
    }

    readonly List<Action> _tasks = [];
    readonly List<Action> _lateTasks = [];

    /// <summary>Runs <paramref name="task"/> at the start of the next frame (as a browser task: details' toggle events and such).</summary>
    internal void PostTask(Action task) => _tasks.Add(task);

    /// <summary>Runs <paramref name="task"/> at the next frame after the other tasks (selectionchange comes after select).</summary>
    internal void PostLateTask(Action task) => _lateTasks.Add(task);

    void RunTasks(bool late)
    {
        foreach (var queue in late ? new[] { _lateTasks } : [_tasks])
        {
            if (queue.Count == 0)
                continue;
            var due = queue.ToArray();
            queue.Clear();
            foreach (var task in due)
                try
                {
                    task();
                }
                catch (Exception exception)
                {
                    Statventure.Core.Log.Error("UI", $"A UI task failed: {exception}");
                }
        }
    }

    void UpdateStyles()
    {
        if (Styles.Version != _styleVersion)
        {
            _styleVersion = Styles.Version;
            InvalidateStyle(Root, subtree: true);
        }
        if (!Root.NeedsStyle && !Root.ChildNeedsStyle && !Root.SubtreeNeedsStyle)
            return;
        Styles.BeginTreePass();
        try
        {
            Restyle(Root, null, null, force: false);
        }
        finally
        {
            Styles.EndTreePass();
        }
    }

    /// <summary>Records what an element's new styles require (<paramref name="pseudo"/>: its ::before or ::after style).</summary>
    void Note(UiElement element, StyleChange change, bool pseudo = false)
    {
        switch (change)
        {
            case StyleChange.Box when pseudo:
                RecollectChildren(element);
                break;
            case StyleChange.Box:
                RebuildBoxes(element);
                break;
            case StyleChange.Layout:
                RelayoutBoxes(element);
                break;
        }
        if (change != StyleChange.None)
            Repaint();
    }

    void Restyle(UiElement element, ComputedStyle? parent, ComputedStyle? layoutParent, bool force)
    {
        var recompute = force || element.NeedsStyle || element.SubtreeNeedsStyle || element.Style is null;
        var shownBefore = element.Style is { Display: not Display.None };
        var forceChildren = force || element.SubtreeNeedsStyle;
        if (recompute)
        {
            var old = element.Style;
            var oldBefore = element.BeforeStyle;
            var oldAfter = element.AfterStyle;
            var styles = Styles.Compute(element, parent, layoutParent, _rootFontSize);
            // Unchanged styles keep their instances (so the children's shared styles still apply);
            // parts, placeholders and selections follow their own rules.
            if (element.Styles is { } previous && previous != styles && StyleDifference.Identical(previous.Style, styles.Style)
                && StyleDifference.Identical(previous.Before, styles.Before) && StyleDifference.Identical(previous.After, styles.After)
                && previous.Important.SetEquals(styles.Important))
            {
                if (previous.SameLaterRules(styles))
                    styles = previous;
                else
                {
                    styles = new ElementStyles
                    {
                        Style = previous.Style,
                        Before = previous.Before,
                        After = previous.After,
                        Important = previous.Important,
                        PlaceholderSource = styles.PlaceholderSource,
                        SelectionSource = styles.SelectionSource,
                        PartSource = styles.PartSource,
                        LaterRules = styles.LaterRules,
                    };
                    Repaint();
                }
            }
            element.Styles = styles;
            // Animations and transitions start, continue or stop here; the style shown carries their values.
            element.Style = Animations.Restyled(element, "", old, styles.Style, parent ?? Styles.Initial, _rootFontSize, styles.Important);
            // Pseudo-elements of an element that isn't rendered aren't either: their animations are cancelled.
            var rendered = element.Style.Display != Display.None;
            element.BeforeStyle = styles.Before is { } before && rendered ? Animations.Restyled(element, "before", element.BeforeStyle, before, element.Style, _rootFontSize, ElementStyles.EmptySet) : ForgetPseudo(element, "before", styles.Before);
            element.AfterStyle = styles.After is { } after && rendered ? Animations.Restyled(element, "after", element.AfterStyle, after, element.Style, _rootFontSize, ElementStyles.EmptySet) : ForgetPseudo(element, "after", styles.After);
            Note(element, StyleDifference.Compare(old, element.Style));
            Note(element, StyleDifference.Compare(oldBefore, element.BeforeStyle), pseudo: true);
            Note(element, StyleDifference.Compare(oldAfter, element.AfterStyle), pseudo: true);
            if (element == Root && styles.Style.SpecifiedFontSize != _rootFontSize)
            {
                // rem units everywhere change.
                _rootFontSize = styles.Style.SpecifiedFontSize;
                forceChildren = true;
            }
            if (old is null || !old.AffectsChildrenEquals(element.Style))
                forceChildren = true;
        }
        element.NeedsStyle = false;
        element.SubtreeNeedsStyle = false;
        var style = element.Style!;
        var childLayoutParent = style.Display == Display.Contents ? layoutParent : style;
        if (style.Display == Display.None)
        {
            // Nothing inside display: none is rendered: its styles wait until it is shown (and nothing animates).
            // (Descendants of an element that was already hidden have no styles, unless something was invalidated in there.)
            if (shownBefore || element.ChildNeedsStyle)
            {
                foreach (var descendant in element.Descendants())
                {
                    if (descendant.Style is not null)
                        Animations.Forget(descendant);
                    descendant.Style = null;
                    descendant.Styles = null;
                    descendant.BeforeStyle = descendant.AfterStyle = null;
                    descendant.NeedsStyle = descendant.SubtreeNeedsStyle = descendant.ChildNeedsStyle = false;
                }
            }
            element.ChildNeedsStyle = false;
            return;
        }
        if (forceChildren || element.ChildNeedsStyle)
        {
            Styles.PushAncestor(element);
            foreach (var child in element.Children)
                if (child is UiElement childElement)
                    Restyle(childElement, style, childLayoutParent, forceChildren);
            Styles.PopAncestor(element);
        }
        element.ChildNeedsStyle = false;
    }
}
