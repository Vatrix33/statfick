using System.Text;
using Style_ = Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>A node of the UI tree: an element or a run of text.</summary>
public abstract class UiNode
{
    public UiElement? Parent { get; internal set; }
    public UiDocument? Document { get; internal set; }
    /// <summary>The neighbouring nodes in the parent's children.</summary>
    public UiNode? PreviousSibling { get; internal set; }
    public UiNode? NextSibling { get; internal set; }

    public UiElement? ParentElement => Parent;

    public bool IsDescendantOf(UiElement ancestor)
    {
        for (var node = Parent; node is not null; node = node.Parent)
            if (node == ancestor)
                return true;
        return false;
    }

    internal virtual void Attach(UiDocument? document)
    {
        Document = document;
    }
}

/// <summary>Text inside an element.</summary>
public sealed class UiText : UiNode
{
    string _text;

    public UiText(string text)
    {
        _text = text;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
                return;
            _text = value;
            Parent?.Touch();
            Document?.TextChanged(Parent);
        }
    }

    public override string ToString() => _text;
}

/// <summary>
/// A UI element, modelled on the HTML DOM so the game's stylesheets apply as they are: a tag,
/// attributes (id, class, data-*, aria-*, style), children, and interaction states for the
/// pseudo-classes (hover, active, focus, disabled, checked).
/// </summary>
public partial class UiElement : UiNode
{
    static readonly HashSet<string> FormControls = ["button", "input", "select", "textarea", "option", "optgroup", "fieldset"];

    readonly List<UiNode> _children = [];
    readonly Dictionary<string, string> _attributes = new(StringComparer.Ordinal);
    readonly HashSet<string> _classes = new(StringComparer.Ordinal);
    bool _hovered, _active, _focused, _focusVisible;

    public string Tag { get; }
    public IReadOnlyList<UiNode> Children => _children;
    public IReadOnlyDictionary<string, string> Attributes => _attributes;

    /// <summary>Set by the style system: the element's computed style (with animations applied).</summary>
    public Style.ComputedStyle? Style { get; internal set; }
    /// <summary>The computed styles before animations and transitions, with the pseudo-elements'.</summary>
    public Style.ElementStyles? Styles { get; internal set; }
    /// <summary>The ::before and ::after styles shown (with animations), or null when they generate nothing.</summary>
    public Style.ComputedStyle? BeforeStyle { get; internal set; }
    public Style.ComputedStyle? AfterStyle { get; internal set; }
    // Box invalidation: this element's boxes are made again / its children's boxes are collected
    // again / something below needs one of these.
    internal bool BoxNeedsRebuild, ChildBoxesNeedRebuild, DescendantBoxesNeedRebuild;

    /// <summary>Set by layout: the element's principal box (null when it generates none).</summary>
    public Layout.LayoutBox? Box { get; internal set; }

    float _scrollLeft, _scrollTop;

    /// <summary>
    /// Scroll position of a scroll container (kept across layouts, clamped to what can scroll). On
    /// the root element it is the document's (the viewport's) scroll position, as in browsers.
    /// Setting it scrolls as the element's scroll-behavior says (smoothly or at once).
    /// </summary>
    public float ScrollLeft
    {
        get => LayoutScroll.X / Zoom;
        set
        {
            if (Document is { } document)
                document.LayoutScrollTo(this, new System.Numerics.Vector2(value * document.Zoom, LayoutScroll.Y));
            else
                _scrollLeft = value;
        }
    }

    public float ScrollTop
    {
        get => LayoutScroll.Y / Zoom;
        set
        {
            if (Document is { } document)
                document.LayoutScrollTo(this, new System.Numerics.Vector2(LayoutScroll.X, value * document.Zoom));
            else
                _scrollTop = value;
        }
    }

    float Zoom => Document?.Zoom ?? 1;

    /// <summary>The scroll position in device pixels (the document's for the root).</summary>
    internal System.Numerics.Vector2 LayoutScroll =>
        Document is { } document && document.Root == this ? document.Layout.ViewportScroll : new(_scrollLeft, _scrollTop);

    /// <summary>Sets the stored offsets (the document clamps and reports the change).</summary>
    internal void SetScrollOffsets(float left, float top)
    {
        _scrollLeft = left;
        _scrollTop = top;
    }

    // Style invalidation: this element needs its style recomputed / some descendant does /
    // its whole subtree must be recomputed.
    internal bool NeedsStyle, ChildNeedsStyle, SubtreeNeedsStyle;

    List<Style.PreparedDeclaration>? _preparedInline;
    List<Css.CssDeclaration>? _inlineDeclarations;
    bool _inlineParsed;

    /// <summary>Bumped when one of the element's attributes changes (for caches of what attributes give, like SVG shapes).</summary>
    internal int AttributeVersion;
    /// <summary>Bumped when anything in the element's subtree changes: attributes, text, children.</summary>
    internal int SubtreeVersion;

    internal void Touch()
    {
        for (UiElement? element = this; element is not null; element = element.Parent)
            element.SubtreeVersion++;
    }

    /// <summary>The style attribute's declarations as written.</summary>
    List<Css.CssDeclaration> InlineDeclarations => _inlineDeclarations ??= Css.CssParser.ParseInlineStyle(GetAttribute("style") ?? "");

    /// <summary>The style attribute, parsed and prepared for the cascade.</summary>
    internal List<Style.PreparedDeclaration>? PreparedInlineStyle
    {
        get
        {
            if (!_inlineParsed)
            {
                _inlineParsed = true;
                var text = GetAttribute("style");
                _preparedInline = string.IsNullOrWhiteSpace(text) ? null : Style_.Stylesheet.Prepare(Css.CssParser.ParseInlineStyle(text));
            }
            return _preparedInline;
        }
    }

    /// <summary>Sets one property in the style attribute (element.style.setProperty); null removes it.</summary>
    public void SetStyle(string property, string? value)
    {
        var declarations = Css.CssParser.ParseInlineStyle(GetAttribute("style") ?? "");
        declarations.RemoveAll(d => d.Property == (property.StartsWith("--") ? property : property.ToLowerInvariant()));
        var text = new StringBuilder();
        foreach (var declaration in declarations)
            text.Append(declaration.Property).Append(": ").Append(declaration.ValueText).Append(declaration.Important ? " !important" : "").Append("; ");
        if (value is not null)
            text.Append(property).Append(": ").Append(value).Append(';');
        var result = text.ToString().Trim();
        SetAttribute("style", result.Length == 0 ? null : result);
    }

    /// <summary>A property's value in the style attribute, or null.</summary>
    public string? GetStyle(string property)
    {
        var declarations = InlineDeclarations;
        if (declarations.Count == 0)
            return null;
        var name = property.StartsWith("--", StringComparison.Ordinal) ? property : property.ToLowerInvariant();
        for (var i = declarations.Count - 1; i >= 0; i--)
            if (declarations[i].Property == name)
                return declarations[i].ValueText;
        return null;
    }

    public UiElement(string tag)
    {
        Tag = tag.ToLowerInvariant();
    }

    // ---------- Tree ----------

    public IEnumerable<UiElement> ElementChildren => _children.OfType<UiElement>();

    public T Append<T>(T child) where T : UiNode
    {
        InsertBefore(child, null);
        return child;
    }

    public UiElement Append(string tag, string? className = null, string? text = null)
    {
        var element = new UiElement(tag);
        if (className is not null)
            element.SetAttribute("class", className);
        if (text is not null)
            element.TextContent = text;
        return Append(element);
    }

    public UiText AppendText(string text) => Append(new UiText(text));

    public void InsertBefore(UiNode child, UiNode? reference)
    {
        child.Parent?.RemoveChild(child);
        var index = reference is null ? _children.Count : _children.IndexOf(reference);
        if (index < 0)
            throw new ArgumentException("the reference node is not a child of this element");
        _children.Insert(index, child);
        Touch();
        child.Parent = this;
        child.PreviousSibling = index > 0 ? _children[index - 1] : null;
        child.NextSibling = index + 1 < _children.Count ? _children[index + 1] : null;
        if (child.PreviousSibling is { } before)
            before.NextSibling = child;
        if (child.NextSibling is { } after)
            after.PreviousSibling = child;
        child.Attach(Document);
        foreach (var (option, select) in OptionsIn(child))
            select.OptionInserted(option);
        Document?.ChildrenChanged(this, child, inserted: true, child.NextSibling);
    }

    /// <summary>The options a subtree brings into or takes out of a select (an option, or an optgroup's options), with the select.</summary>
    static List<(UiElement Option, UiElement Select)> OptionsIn(UiNode child)
    {
        var options = new List<(UiElement, UiElement)>();
        if (child is not UiElement element)
            return options;
        IEnumerable<UiElement> candidates = element.Tag == "option" ? [element] : element.Tag == "optgroup" ? element.Children.OfType<UiElement>().Where(o => o.Tag == "option") : [];
        foreach (var option in candidates)
            if (option.SelectOwner is { } select)
                options.Add((option, select));
        return options;
    }

    public void RemoveChild(UiNode child)
    {
        var options = OptionsIn(child).Select(o => (o.Option, o.Select, WasSelected: o.Select.SelectedOption == o.Option)).ToList();
        if (!_children.Remove(child))
            return;
        Touch();
        var next = child.NextSibling;
        if (child.PreviousSibling is { } before)
            before.NextSibling = child.NextSibling;
        if (child.NextSibling is { } after)
            after.PreviousSibling = child.PreviousSibling;
        child.PreviousSibling = child.NextSibling = null;
        child.Parent = null;
        Document?.NodeRemoved(child);
        child.Attach(null);
        foreach (var (option, select, wasSelected) in options)
            select.OptionRemoved(option, wasSelected);
        Document?.ChildrenChanged(this, child, inserted: false, next);
    }

    public void ClearChildren()
    {
        foreach (var child in _children.ToList())
            RemoveChild(child);
    }

    public void Remove() => Parent?.RemoveChild(this);

    internal override void Attach(UiDocument? document)
    {
        base.Attach(document);
        if (document is not null && _pendingToggle is not null)
            PostToggleTask();
        foreach (var child in _children)
            child.Attach(document);
    }

    public int ElementIndex
    {
        get
        {
            var index = 0;
            for (var sibling = PreviousElementSibling; sibling is not null; sibling = sibling.PreviousElementSibling)
                index++;
            return index;
        }
    }

    public UiElement? PreviousElementSibling
    {
        get
        {
            for (var node = PreviousSibling; node is not null; node = node.PreviousSibling)
                if (node is UiElement element)
                    return element;
            return null;
        }
    }

    public UiElement? NextElementSibling
    {
        get
        {
            for (var node = NextSibling; node is not null; node = node.NextSibling)
                if (node is UiElement element)
                    return element;
            return null;
        }
    }

    public IEnumerable<UiElement> ElementSiblingsBefore()
    {
        if (Parent is null)
            yield break;
        foreach (var sibling in Parent._children)
        {
            if (sibling == this)
                yield break;
            if (sibling is UiElement element)
                yield return element;
        }
    }

    public IEnumerable<UiElement> ElementSiblingsAfter()
    {
        for (var sibling = NextElementSibling; sibling is not null; sibling = sibling.NextElementSibling)
            yield return sibling;
    }

    /// <summary>Descendant elements in document order.</summary>
    public IEnumerable<UiElement> Descendants()
    {
        var stack = new Stack<(UiElement Element, int Next)>();
        var element = this;
        var next = 0;
        while (true)
        {
            var children = element._children;
            while (next < children.Count && children[next] is not UiElement)
                next++;
            if (next < children.Count)
            {
                var child = (UiElement)children[next];
                yield return child;
                stack.Push((element, next + 1));
                element = child;
                next = 0;
                continue;
            }
            if (stack.Count == 0)
                yield break;
            (element, next) = stack.Pop();
        }
    }

    // ---------- Attributes ----------

    public string? Id
    {
        get => GetAttribute("id");
        set => SetAttribute("id", value);
    }

    public string ClassName
    {
        get => GetAttribute("class") ?? "";
        set => SetAttribute("class", value);
    }

    public string? GetAttribute(string name) => _attributes.TryGetValue(name, out var value) ? value : null;

    public bool HasAttribute(string name) => _attributes.ContainsKey(name);

    public void SetAttribute(string name, string? value)
    {
        name = name.ToLowerInvariant();
        if (value is null)
        {
            RemoveAttribute(name);
            return;
        }
        if (_attributes.TryGetValue(name, out var old) && old == value)
            return;
        _attributes[name] = value;
        AttributeVersion++;
        Touch();
        if (name == "style")
        {
            _inlineParsed = false;
            _inlineDeclarations = null;
        }
        if (name == "class")
        {
            _classes.Clear();
            foreach (var cls in value.Split((char[])[' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries))
                _classes.Add(cls);
        }
        FormAttributeChanged(name);
        Document?.AttributeChanged(this, name, old);
    }

    public void RemoveAttribute(string name)
    {
        name = name.ToLowerInvariant();
        if (!_attributes.Remove(name, out var old))
            return;
        AttributeVersion++;
        Touch();
        if (name == "style")
        {
            _inlineParsed = false;
            _inlineDeclarations = null;
        }
        if (name == "class")
            _classes.Clear();
        FormAttributeChanged(name);
        Document?.AttributeChanged(this, name, old);
    }

    public void ToggleAttribute(string name, bool on)
    {
        if (on)
            SetAttribute(name, GetAttribute(name) ?? "");
        else
            RemoveAttribute(name);
    }

    public bool HasClass(string name) => _classes.Contains(name);

    public IReadOnlyCollection<string> Classes => _classes;
    internal HashSet<string> ClassSet => _classes;
    internal Dictionary<string, string> AttributeMap => _attributes;

    public void AddClass(string name)
    {
        if (!_classes.Contains(name))
            ClassName = (ClassName + " " + name).Trim();
    }

    public void RemoveClass(string name)
    {
        if (_classes.Contains(name))
            ClassName = string.Join(" ", _classes.Where(c => c != name));
    }

    public void ToggleClass(string name, bool? on = null)
    {
        if (on ?? !HasClass(name))
            AddClass(name);
        else
            RemoveClass(name);
    }

    public string? InlineStyle
    {
        get => GetAttribute("style");
        set => SetAttribute("style", value);
    }

    public string TextContent
    {
        get
        {
            var text = new StringBuilder();
            void Walk(UiNode node)
            {
                if (node is UiText t)
                    text.Append(t.Text);
                else if (node is UiElement e)
                    foreach (var child in e._children)
                        Walk(child);
            }
            Walk(this);
            return text.ToString();
        }
        set
        {
            if (_children.Count == 1 && _children[0] is UiText single)
            {
                single.Text = value;
                return;
            }
            ClearChildren();
            if (value.Length > 0)
                AppendText(value);
        }
    }

    // ---------- States ----------

    public bool IsFormControl => FormControls.Contains(Tag);
    public bool IsDisabled
    {
        get
        {
            if (!IsFormControl)
                return false;
            if (HasAttribute("disabled"))
                return true;
            for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
                if (ancestor.Tag == "fieldset" && ancestor.HasAttribute("disabled"))
                    return true;
            return false;
        }
    }
    /// <summary>:placeholder-shown: a text field with a placeholder and nothing typed.</summary>
    public bool ShowsPlaceholder => IsTextField && HasAttribute("placeholder") && EditorText.Length == 0;
    public bool Hidden
    {
        get => HasAttribute("hidden");
        set => ToggleAttribute("hidden", value);
    }

    /// <summary>The inert attribute: the element and its subtree can't be hit, selected or focused.</summary>
    public bool Inert
    {
        get => HasAttribute("inert");
        set => ToggleAttribute("inert", value);
    }

    public IEnumerable<UiElement> Ancestors()
    {
        for (var node = Parent; node is not null; node = node.Parent)
            yield return node;
    }

    static readonly Css.PseudoClassKind[] HoverStates = [Css.PseudoClassKind.Hover];
    static readonly Css.PseudoClassKind[] ActiveStates = [Css.PseudoClassKind.Active];
    static readonly Css.PseudoClassKind[] FocusStates = [Css.PseudoClassKind.Focus, Css.PseudoClassKind.FocusVisible, Css.PseudoClassKind.FocusWithin];
    static readonly Css.PseudoClassKind[] FocusVisibleStates = [Css.PseudoClassKind.FocusVisible];

    public bool IsHovered
    {
        get => _hovered;
        internal set => SetState(ref _hovered, value, HoverStates);
    }

    public bool IsActive
    {
        get => _active;
        internal set => SetState(ref _active, value, ActiveStates);
    }

    public bool IsFocused
    {
        get => _focused;
        internal set => SetState(ref _focused, value, FocusStates);
    }

    public bool FocusVisible
    {
        get => _focusVisible;
        internal set => SetState(ref _focusVisible, value, FocusVisibleStates);
    }

    void SetState(ref bool field, bool value, Css.PseudoClassKind[] states)
    {
        if (field == value)
            return;
        field = value;
        Document?.StateChanged(this, states);
    }

    // ---------- Events ----------

    internal EventListeners? Listeners;

    /// <summary>Calls <paramref name="listener"/> for events of this type reaching the element (in the capture phase with <paramref name="capture"/>).</summary>
    public void AddEventListener(string type, Action<UiEvent> listener, bool capture = false) =>
        (Listeners ??= new()).Add(type, listener, listener, capture);

    /// <summary>A listener for one kind of event (e.g. <c>AddEventListener&lt;UiMouseEvent&gt;("click", ...)</c>).</summary>
    public void AddEventListener<T>(string type, Action<T> listener, bool capture = false) where T : UiEvent =>
        (Listeners ??= new()).Add(type, listener, e =>
        {
            if (e is T typed)
                listener(typed);
        }, capture);

    public void RemoveEventListener(string type, Delegate listener, bool capture = false) => Listeners?.Remove(type, listener, capture);

    /// <summary>Dispatches an event to this element (capture, target, bubble); false if a listener prevented its default.</summary>
    public bool DispatchEvent(UiEvent e) => UiDocument.Dispatch(e, this, Document);

    // ---------- Focus ----------

    /// <summary>The tabindex attribute (null when absent or not an integer).</summary>
    public int? TabIndex => int.TryParse(GetAttribute("tabindex"), out var index) ? index : null;

    /// <summary>
    /// Can the element take focus (HTML's focusable areas): a tabindex, links, enabled form
    /// controls, a details' summary; and it must be rendered.
    /// </summary>
    public bool IsFocusable
    {
        get
        {
            // ComputedStyle::IsFocusable: rendered, visible and not inert.
            if (Box is null || Style is not { Visibility: Style_.Visibility.Visible, IsInert: false })
                return false;
            if (TabIndex is not null)
                return true;
            return Tag switch
            {
                "a" or "area" => HasAttribute("href"),
                "button" or "select" or "textarea" => !IsDisabled,
                "input" => !IsDisabled && !string.Equals(GetAttribute("type"), "hidden", StringComparison.OrdinalIgnoreCase),
                "summary" => Parent?.Tag == "details" && Parent.ElementChildren.FirstOrDefault(e => e.Tag == "summary") == this,
                _ => false,
            };
        }
    }

    /// <summary>Focuses the element if it can take focus.</summary>
    public void Focus() => Document?.Input.Focus(this, FocusOrigin.Script);

    /// <summary>Takes focus away from the element if it has it.</summary>
    public void Blur()
    {
        if (Document is { } document && document.FocusedElement == this)
            document.Input.Focus(null, FocusOrigin.Script);
    }

    public override string ToString()
    {
        var text = new StringBuilder("<").Append(Tag);
        if (Id is { } id)
            text.Append(" id=\"").Append(id).Append('"');
        if (ClassName.Length > 0)
            text.Append(" class=\"").Append(ClassName).Append('"');
        return text.Append('>').ToString();
    }
}
