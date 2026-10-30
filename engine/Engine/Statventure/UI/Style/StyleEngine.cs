using Statventure.UI.Css;
using Statventure.UI.Dom;

namespace Statventure.UI.Style;

/// <summary>The computed styles of an element and of its pseudo-elements.</summary>
public sealed class ElementStyles
{
    public required ComputedStyle Style { get; init; }
    public ComputedStyle? Before { get; init; }
    public ComputedStyle? After { get; init; }
    /// <summary>Longhands whose winning declaration is !important (animations don't override them).</summary>
    public IReadOnlySet<string> Important { get; init; } = EmptySet;

    // ::placeholder and ::selection are computed when first asked for.
    internal Func<ComputedStyle?>? PlaceholderSource, SelectionSource;
    ComputedStyle? _placeholder, _selection;

    public ComputedStyle? Placeholder => PlaceholderSource is { } source ? _placeholder ??= source() : null;
    public ComputedStyle? Selection => SelectionSource is { } source ? _selection ??= source() : null;

    // The parts of form controls (and ::marker), computed when first asked for.
    internal Func<PseudoElement, ComputedStyle?, ComputedStyle>? PartSource;

    /// <summary>The rules behind the styles computed later (parts, ::placeholder, ::selection): equal rules, equal styles.</summary>
    internal object[] LaterRules { get; init; } = [];

    internal bool SameLaterRules(ElementStyles other)
    {
        if (LaterRules.Length != other.LaterRules.Length)
            return false;
        for (var i = 0; i < LaterRules.Length; i++)
            if (!ReferenceEquals(LaterRules[i], other.LaterRules[i]))
                return false;
        return true;
    }
    Dictionary<PseudoElement, ComputedStyle>? _parts;

    /// <summary>
    /// The style of a part of a form control (Blink's shadow elements, e.g. ::-webkit-progress-value)
    /// or of ::marker. Parts inherit along Blink's shadow tree: the slider thumb from the track from
    /// the container, the progress value from the bar from the inner element, the rest from the host.
    /// </summary>
    public ComputedStyle Part(PseudoElement part)
    {
        if (_parts?.TryGetValue(part, out var cached) == true)
            return cached;
        var parent = part switch
        {
            PseudoElement.ProgressBar => Part(PseudoElement.ProgressInnerElement),
            PseudoElement.ProgressValue => Part(PseudoElement.ProgressBar),
            PseudoElement.SliderRunnableTrack => Part(PseudoElement.SliderContainer),
            PseudoElement.SliderThumb => Part(PseudoElement.SliderRunnableTrack),
            _ => Style,
        };
        var style = PartSource?.Invoke(part, parent) ?? parent;
        (_parts ??= [])[part] = style;
        return style;
    }

    internal static readonly HashSet<string> EmptySet = [];
}

/// <summary>A @keyframes rule ready for animation: frames sorted by offset.</summary>
public sealed class KeyframesDefinition
{
    public sealed record Frame(float Offset, List<PreparedDeclaration> Declarations, TimingFunction? Timing);

    public required string Name { get; init; }
    public required List<Frame> Frames { get; init; }
    /// <summary>Every longhand some frame sets.</summary>
    public required HashSet<string> Properties { get; init; }
}

/// <summary>
/// Resolves styles: indexes the stylesheets' rules (for the current media environment), matches them
/// against elements, runs the cascade (origins, !important, specificity, order, the style attribute),
/// substitutes custom properties and computes every property's value.
/// </summary>
public sealed class StyleEngine
{
    sealed class Entry
    {
        public required Selector Selector;
        public required Stylesheet.Rule Rule;
        public required Specificity Specificity;
        public required long Order;
        public required StyleOrigin Origin;
        /// <summary>Tags, ids and classes some ancestor must have (for the ancestor filter); null if none.</summary>
        public int[]? AncestorHashes;
        /// <summary>The match it was last tested in (entries sit in several buckets).</summary>
        public int Stamp;
    }

    sealed class Index
    {
        public readonly Dictionary<string, List<Entry>> ById = new(StringComparer.Ordinal);
        public readonly Dictionary<string, List<Entry>> ByClass = new(StringComparer.Ordinal);
        public readonly Dictionary<string, List<Entry>> ByTag = new(StringComparer.Ordinal);
        public readonly Dictionary<string, List<Entry>> ByAttribute = new(StringComparer.Ordinal);
        public readonly List<Entry> Root = [];
        public readonly List<Entry> Universal = [];
    }

    readonly List<Stylesheet> _sheets = [];
    Index? _index;
    Dictionary<string, KeyframesDefinition> _keyframes = new(StringComparer.Ordinal);
    MediaEnvironment _environment = new(1280, 720);
    string _mediaKey = "";

    /// <summary>Bumped whenever the rule set changes: every element must be restyled.</summary>
    public int Version { get; private set; }
    RuleFeatures _features = new();

    /// <summary>What the current rules' selectors look at (for restyling only what a change can affect).</summary>
    public RuleFeatures Features
    {
        get
        {
            EnsureIndex();
            return _features;
        }
    }

    /// <summary>Some rule uses :has(), so changes inside an element can restyle it.</summary>
    public bool UsesHas { get; private set; }
    public IReadOnlyList<Stylesheet> Sheets => _sheets;
    public MediaEnvironment Environment => _environment;

    ComputedStyle? _initial;

    /// <summary>Whether text is positioned at subpixels on the current screen.</summary>
    bool SubpixelPositioning => Text.FontRenderSettings.System.SubpixelPositioningAt(_environment.DeviceScale);

    /// <summary>
    /// The initial style at the current zoom (the root's parent): Properties.Initial with the medium
    /// font size zoomed (Blink's initial font-size is the medium keyword, computed with the zoom).
    /// </summary>
    public ComputedStyle Initial
    {
        get
        {
            if (_initial is null)
            {
                if (_environment.Zoom == 1 && !SubpixelPositioning)
                    _initial = Properties.Initial;
                else
                {
                    _initial = Properties.Initial.Clone();
                    _initial.FontSize *= _environment.Zoom;
                    _initial.Zoom = _environment.Zoom;
                    _initial.SubpixelPositioning = SubpixelPositioning;
                    _initial.Finish();
                }
            }
            return _initial;
        }
    }

    public void Add(Stylesheet sheet)
    {
        _sheets.Add(sheet);
        Invalidate();
    }

    public void Remove(Stylesheet sheet)
    {
        if (_sheets.Remove(sheet))
            Invalidate();
    }

    void Invalidate()
    {
        _index = null;
        _shared.Clear();
        Version++;
    }

    /// <summary>Sets the viewport and preferences; the rule set is rebuilt if any media query changes.</summary>
    public void SetEnvironment(MediaEnvironment environment)
    {
        // Viewport units: shared styles were computed for the old viewport.
        if (environment != _environment)
            _shared.Clear();
        var zoomed = environment.Zoom != _environment.Zoom || environment.DeviceScale != _environment.DeviceScale;
        _environment = environment;
        if (zoomed)
            _initial = null;
        var key = MediaKey();
        // Every length is computed with the zoom: a new zoom restyles everything.
        if (key != _mediaKey || zoomed)
            Invalidate();
    }

    string MediaKey()
    {
        var bits = new System.Text.StringBuilder();
        foreach (var sheet in _sheets)
        {
            foreach (var rule in sheet.Rules)
                if (rule.Media is { } media)
                    bits.Append(media.Matches(_environment) ? '1' : '0');
            foreach (var (_, media) in sheet.Keyframes)
                if (media is not null)
                    bits.Append(media.Matches(_environment) ? '1' : '0');
        }
        return bits.ToString();
    }

    Index EnsureIndex()
    {
        if (_index is not null)
            return _index;
        var index = new Index();
        var keyframes = new Dictionary<string, KeyframesDefinition>(StringComparer.Ordinal);
        UsesHas = false;
        var features = new RuleFeatures();
        for (var s = 0; s < _sheets.Count; s++)
        {
            var sheet = _sheets[s];
            foreach (var rule in sheet.Rules)
            {
                if (rule.Media is { } media && !media.Matches(_environment))
                    continue;
                if (rule.Declarations.Count == 0)
                    continue;
                foreach (var selector in rule.Source.Selectors.Selectors)
                {
                    var entry = new Entry
                    {
                        Selector = selector,
                        Rule = rule,
                        Specificity = selector.Specificity,
                        Order = ((long)s << 32) | (uint)rule.Source.Order,
                        Origin = sheet.Origin,
                    };
                    if (selector.Text.Contains(":has(", StringComparison.Ordinal))
                        UsesHas = true;
                    features.Add(selector);
                    entry.AncestorHashes = AncestorHashes(selector);
                    var keys = new List<(int Kind, string Key)>();
                    if (!SubjectKeys(selector.Compounds[^1], keys))
                        index.Universal.Add(entry);
                    else
                        foreach (var (kind, key) in keys.Distinct())
                            switch (kind)
                            {
                                case IdKey: Bucket(index.ById, key).Add(entry); break;
                                case ClassKey: Bucket(index.ByClass, key).Add(entry); break;
                                case TagKey: Bucket(index.ByTag, key).Add(entry); break;
                                case AttributeKey: Bucket(index.ByAttribute, key).Add(entry); break;
                                default: index.Root.Add(entry); break;
                            }
                }
            }
            foreach (var (rule, media) in sheet.Keyframes)
            {
                if (media is not null && !media.Matches(_environment))
                    continue;
                keyframes[rule.Name] = PrepareKeyframes(rule);
            }
        }
        _keyframes = keyframes;
        _mediaKey = MediaKey();
        _features = features;
        return _index = index;
    }

    const int IdKey = 0, ClassKey = 1, TagKey = 2, AttributeKey = 3, RootKey = 4;

    /// <summary>
    /// Something every element the compound matches has (an id, a class, a tag, an attribute, being
    /// the root), or for :is() and :where(), one such thing per alternative. False if there is none.
    /// </summary>
    static bool SubjectKeys(CompoundSelector compound, List<(int, string)> keys)
    {
        if (compound.Id is { } id)
            keys.Add((IdKey, id));
        else if (compound.Classes.Count > 0)
            keys.Add((ClassKey, compound.Classes[0]));
        else if (compound.Tag is { } tag && tag != "*")
            keys.Add((TagKey, tag));
        else if (compound.Attributes.Count > 0)
            keys.Add((AttributeKey, compound.Attributes[0].Name));
        else if (compound.PseudoClasses.Any(p => p.Kind == PseudoClassKind.Root))
            keys.Add((RootKey, ""));
        else
        {
            foreach (var pseudo in compound.PseudoClasses)
            {
                if (pseudo.Kind is not (PseudoClassKind.Is or PseudoClassKind.Where) || pseudo.Arguments is not { } arguments)
                    continue;
                var alternatives = new List<(int, string)>();
                if (arguments.Selectors.All(alternative => alternative.PseudoElement == PseudoElement.None && SubjectKeys(alternative.Compounds[^1], alternatives)))
                {
                    keys.AddRange(alternatives);
                    return true;
                }
            }
            return false;
        }
        return true;
    }

    static int Hash(int kind, string value) => (value.GetHashCode() * 31) ^ (kind * 0x5BD1E995);

    /// <summary>
    /// What ancestors must have: the compounds followed by a descendant or child combinator (a compound
    /// followed by a sibling combinator is the sibling of the subject or of an ancestor, as Blink's
    /// SelectorFilter skips it).
    /// </summary>
    static int[]? AncestorHashes(Selector selector)
    {
        var hashes = new List<int>();
        for (var i = 0; i < selector.Combinators.Count; i++)
        {
            if (selector.Combinators[i] is not (Combinator.Descendant or Combinator.Child))
                continue;
            var compound = selector.Compounds[i];
            if (compound.Tag is { } tag && tag != "*")
                hashes.Add(Hash(TagKey, tag));
            if (compound.Id is { } id)
                hashes.Add(Hash(IdKey, id));
            foreach (var cls in compound.Classes)
                hashes.Add(Hash(ClassKey, cls));
        }
        return hashes.Count == 0 ? null : hashes.Distinct().ToArray();
    }

    static List<Entry> Bucket(Dictionary<string, List<Entry>> buckets, string key)
    {
        if (!buckets.TryGetValue(key, out var list))
            buckets[key] = list = [];
        return list;
    }

    static KeyframesDefinition PrepareKeyframes(CssKeyframesRule rule)
    {
        var frames = new List<KeyframesDefinition.Frame>();
        var properties = new HashSet<string>(StringComparer.Ordinal);
        foreach (var keyframe in rule.Frames)
        {
            // !important declarations in keyframes are ignored (CSS Animations §4).
            var declarations = Stylesheet.Prepare(keyframe.Declarations.Where(d => !d.Important).ToList());
            TimingFunction? timing = null;
            var timingDeclaration = keyframe.Declarations.LastOrDefault(d => d.Property == "animation-timing-function");
            if (timingDeclaration is not null)
                timing = Properties.ParseTiming(new ValueReader(timingDeclaration.Value));
            declarations.RemoveAll(d => d.Property.StartsWith("animation", StringComparison.Ordinal) || d.Property.StartsWith("transition", StringComparison.Ordinal));
            foreach (var declaration in declarations)
                properties.Add(declaration.Property);
            foreach (var offset in keyframe.Offsets)
                frames.Add(new KeyframesDefinition.Frame(offset, declarations, timing));
        }
        frames.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        return new KeyframesDefinition { Name = rule.Name, Frames = frames, Properties = properties };
    }

    public KeyframesDefinition? Keyframes(string name)
    {
        EnsureIndex();
        return _keyframes.GetValueOrDefault(name);
    }

    // ---------- Matching ----------

    readonly List<Entry> _matched = [];
    int _stamp;

    static int CascadeOrder(Entry a, Entry b)
    {
        var c = a.Specificity.CompareTo(b.Specificity);
        return c != 0 ? c : a.Order.CompareTo(b.Order);
    }

    /// <summary>Sorts the matched rules by specificity, then order (insertion sort: the lists are short).</summary>
    void SortMatched()
    {
        for (var i = 1; i < _matched.Count; i++)
        {
            var entry = _matched[i];
            var j = i - 1;
            while (j >= 0 && CascadeOrder(_matched[j], entry) > 0)
            {
                _matched[j + 1] = _matched[j];
                j--;
            }
            _matched[j + 1] = entry;
        }
    }

    void Match(UiElement element, Index index)
    {
        _matched.Clear();
        _stamp++;
        if (element.Id is { } id && index.ById.TryGetValue(id, out var byId))
            Test(byId, element);
        foreach (var cls in element.ClassSet)
            if (index.ByClass.TryGetValue(cls, out var byClass))
                Test(byClass, element);
        if (index.ByTag.TryGetValue(element.Tag, out var byTag))
            Test(byTag, element);
        if (index.ByAttribute.Count > 0)
            foreach (var name in element.AttributeMap.Keys)
                if (index.ByAttribute.TryGetValue(name, out var byAttribute))
                    Test(byAttribute, element);
        if (element.Parent is null)
            Test(index.Root, element);
        Test(index.Universal, element);
        SortMatched();
    }

    void Test(List<Entry> entries, UiElement element)
    {
        foreach (var entry in entries)
        {
            if (entry.Stamp == _stamp)
                continue;
            entry.Stamp = _stamp;
            if (_filterActive && entry.AncestorHashes is { } hashes && !_filter.MayContainAll(hashes))
                continue;
            if (SelectorMatcher.Matches(entry.Selector, element))
                _matched.Add(entry);
        }
    }

    // ---------- Ancestor filter ----------

    readonly AncestorFilter _filter = new();
    bool _filterActive;

    /// <summary>
    /// Starts a top-down pass over the tree: while it runs, every element computed has had its
    /// ancestors pushed (<see cref="PushAncestor"/>), so selectors needing ancestors that aren't there are skipped quickly.
    /// </summary>
    public void BeginTreePass()
    {
        _filter.Clear();
        _filterActive = true;
    }

    public void EndTreePass()
    {
        _filterActive = false;
        _filter.Clear();
    }

    public void PushAncestor(UiElement element)
    {
        if (!_filterActive)
            return;
        _filter.Add(Hash(TagKey, element.Tag), 1);
        if (element.Id is { } id)
            _filter.Add(Hash(IdKey, id), 1);
        foreach (var cls in element.ClassSet)
            _filter.Add(Hash(ClassKey, cls), 1);
    }

    public void PopAncestor(UiElement element)
    {
        if (!_filterActive)
            return;
        _filter.Add(Hash(TagKey, element.Tag), -1);
        if (element.Id is { } id)
            _filter.Add(Hash(IdKey, id), -1);
        foreach (var cls in element.ClassSet)
            _filter.Add(Hash(ClassKey, cls), -1);
    }

    /// <summary>A counting Bloom filter of the current ancestors' tags, ids and classes (as in WebKit's SelectorFilter).</summary>
    sealed class AncestorFilter
    {
        const int Bits = 13;
        const int Mask = (1 << Bits) - 1;
        readonly ushort[] _counts = new ushort[1 << Bits];

        public void Clear() => Array.Clear(_counts);

        public void Add(int hash, int delta)
        {
            _counts[hash & Mask] = (ushort)(_counts[hash & Mask] + delta);
            _counts[(hash >> 16) & Mask] = (ushort)(_counts[(hash >> 16) & Mask] + delta);
        }

        public bool MayContainAll(int[] hashes)
        {
            foreach (var hash in hashes)
                if (_counts[hash & Mask] == 0 || _counts[(hash >> 16) & Mask] == 0)
                    return false;
            return true;
        }
    }

    /// <summary>The winning declaration for each property, given the matched rules for one (pseudo-)element.</summary>
    static Dictionary<string, PreparedDeclaration> Cascade(IEnumerable<Entry> matched, List<PreparedDeclaration>? inline, HashSet<string>? important, List<PreparedDeclaration>? hints = null, HashSet<string>? authored = null)
    {
        var winners = new Dictionary<string, PreparedDeclaration>(StringComparer.Ordinal);
        var entries = matched as IList<Entry> ?? matched.ToList();
        void Pass(StyleOrigin origin, bool isImportant)
        {
            foreach (var entry in entries)
                if (entry.Origin == origin)
                    foreach (var declaration in entry.Rule.Declarations)
                        if (declaration.Important == isImportant)
                            Set(declaration, origin != StyleOrigin.UserAgent);
        }
        void Inline(bool isImportant)
        {
            if (inline is null)
                return;
            foreach (var declaration in inline)
                if (declaration.Important == isImportant)
                    Set(declaration, true);
        }
        void Set(PreparedDeclaration declaration, bool author = true)
        {
            winners[declaration.Property] = declaration;
            if (authored is not null)
            {
                if (author && declaration.Keyword != "revert")
                    authored.Add(declaration.Property);
                else
                    authored.Remove(declaration.Property);
            }
            if (declaration.Important)
                important?.Add(declaration.Property);
            else
                important?.Remove(declaration.Property);
        }
        // Normal declarations: user agent, author, style attribute; then !important: author,
        // style attribute, user agent (CSS Cascade 4 §6.2).
        Pass(StyleOrigin.UserAgent, false);
        // Presentational hints (width/height attributes) sit below every author rule.
        if (hints is not null)
            foreach (var declaration in hints)
                Set(declaration);
        Pass(StyleOrigin.Author, false);
        Inline(false);
        Pass(StyleOrigin.Author, true);
        Inline(true);
        Pass(StyleOrigin.UserAgent, true);
        return winners;
    }

    // ---------- Computing ----------

    /// <summary>How many elements' styles have been computed (matched; shared or not): the work restyling does.</summary>
    public long StylesComputed { get; private set; }

    /// <summary>
    /// Computes an element's styles. <paramref name="parent"/> is the parent element's style (null for
    /// the root) and <paramref name="layoutParent"/> that of the nearest ancestor that generates a box
    /// (for blockification inside flex and grid containers). Elements with the same matched rules,
    /// style attribute and parent style share one result.
    /// </summary>
    public ElementStyles Compute(UiElement element, ComputedStyle? parent, ComputedStyle? layoutParent, float rootFontSize)
    {
        StylesComputed++;
        var index = EnsureIndex();
        Match(element, index);
        var isRoot = element.Parent is null;
        var hints = PresentationalHints(element);
        // Elements with the same rules, style attribute and parent style get the same styles (shared);
        // an SVG element's transform hint is shared by its text, so it can be part of the key.
        var transformHint = hints is [{ Property: "transform" } only] ? only : null;
        var shareable = !isRoot && (hints is null || transformHint is not null);
        if (shareable)
        {
            _probe.Set(_matched, element.GetAttribute("style"), parent, Blockifies(layoutParent), element.Tag is "input" or "textarea", rootFontSize,
                layoutParent?.AppliedDecorations, IsOutermostSvg(element), transformHint);
            if (_shared.TryGetValue(_probe, out var shared))
                return shared;
        }
        var normal = new List<Entry>();
        List<Entry>? before = null, after = null, placeholder = null, selection = null, parts = null;
        foreach (var entry in _matched)
        {
            switch (entry.Selector.PseudoElement)
            {
                case PseudoElement.None: normal.Add(entry); break;
                case PseudoElement.Before: (before ??= []).Add(entry); break;
                case PseudoElement.After: (after ??= []).Add(entry); break;
                case PseudoElement.Placeholder: (placeholder ??= []).Add(entry); break;
                case PseudoElement.Selection: (selection ??= []).Add(entry); break;
                case PseudoElement.UnknownWebkit: break;
                default: (parts ??= []).Add(entry); break;
            }
        }
        var important = new HashSet<string>(StringComparer.Ordinal);
        var authored = element.IsFormControl || element.Tag is "progress" or "meter" ? new HashSet<string>(StringComparer.Ordinal) : null;
        var winners = Cascade(normal, element.PreparedInlineStyle, important, hints, authored);
        var style = ComputeStyle(winners, parent, layoutParent, isRoot, isRoot ? 16 : rootFontSize);
        // An outermost svg element stops decorations too.
        if (IsOutermostSvg(element))
        {
            style.BaseDecorations = [];
            style.Finish();
        }
        if (authored is { Count: > 0 })
        {
            // What makes Blink drop a control's native look (LayoutTheme::IsControlStyled).
            style.HasAuthorBackground = authored.Overlaps(AuthorBackgroundProperties);
            style.HasAuthorBorder = authored.Overlaps(AuthorBorderProperties);
            style.HasAuthorBorderRadius = authored.Overlaps(AuthorRadiusProperties);
        }
        else if (authored is null)
        {
            // The user agent rounds only controls' corners: any other element's radius is the author's.
            style.HasAuthorBorderRadius = true;
        }
        var pseudoRootFontSize = isRoot ? style.SpecifiedFontSize : rootFontSize;
        ComputedStyle? Box(List<Entry>? entries)
        {
            if (entries is null)
                return null;
            // ::before and ::after only generate boxes with content (initially normal, which is none for them).
            var pseudoWinners = Cascade(entries, null, null);
            if (!pseudoWinners.TryGetValue("content", out var content) || content.Keyword is "initial" or "unset" or "revert"
                || (content.Keyword is null && !content.HasVar && CssParser.Trim(content.Value) is [{ Type: CssTokenType.Ident } only] && only.Value.ToLowerInvariant() is "none" or "normal"))
                return null;
            var pseudo = ComputeStyle(pseudoWinners, style, style, false, pseudoRootFontSize, pseudoBox: true);
            return pseudo.Content is null ? null : pseudo;
        }
        Func<ComputedStyle?>? Later(List<Entry>? entries) =>
            entries is null ? null : () => ComputeStyle(Cascade(entries, null, null), style, style, false, pseudoRootFontSize);
        var result = new ElementStyles
        {
            Style = style,
            Before = Box(before),
            After = Box(after),
            Important = important.Count > 0 ? important : ElementStyles.EmptySet,
            PlaceholderSource = element.Tag is "input" or "textarea" ? Later(placeholder) : null,
            // Highlight pseudos take decorations only by inheritance (no propagation from the layout parent).
            SelectionSource = selection is null ? null : () => ComputeStyle(Cascade(selection, null, null), style, style, false, pseudoRootFontSize, highlight: true),
            LaterRules = [.. new[] { placeholder, selection, parts }.SelectMany(list => list ?? []).SelectMany(e => new object[] { e.Selector, e.Rule })],
            // A part no rule styles still has its own (initial) values for what doesn't inherit.
            PartSource = (part, parentStyle) =>
            {
                var entries = parts?.Where(e => e.Selector.PseudoElement == part).ToList() ?? [];
                return ComputeStyle(Cascade(entries, null, null), parentStyle ?? style, parentStyle ?? style, false, pseudoRootFontSize);
            },
        };
        if (shareable)
        {
            if (_shared.Count >= MaxShared)
                _shared.Clear();
            _shared[_probe.Copy()] = result;
        }
        return result;
    }

    static readonly HashSet<string> AuthorBackgroundProperties =
    [
        "background-attachment", "background-clip", "background-color", "background-image", "background-origin",
        "background-position", "background-position-x", "background-position-y", "background-size",
    ];

    static readonly HashSet<string> AuthorBorderProperties =
    [
        "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
        "border-top-style", "border-right-style", "border-bottom-style", "border-left-style",
        "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
        "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius",
        "border-image-outset", "border-image-repeat", "border-image-slice", "border-image-source", "border-image-width",
    ];

    static readonly HashSet<string> AuthorRadiusProperties =
    [
        "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius", "border-bottom-left-radius",
    ];

    static bool IsOutermostSvg(UiElement element) => element.Tag == "svg" && element.Parent?.Tag != "svg";

    static bool Blockifies(ComputedStyle? layoutParent) => layoutParent is { IsFlexContainer: true } or { IsGridContainer: true };

    // ---------- Sharing ----------

    const int MaxShared = 50000;
    readonly Dictionary<SharingKey, ElementStyles> _shared = [];
    readonly SharingKey _probe = new();

    /// <summary>What an element's styles depend on besides the rules: the key under which they are shared.</summary>
    sealed class SharingKey : IEquatable<SharingKey>
    {
        IReadOnlyList<Entry> _entries = [];
        string? _inline;
        ComputedStyle? _parent;
        bool _blockify, _textControl, _outermostSvg;
        float _rootFontSize;
        AppliedDecoration[]? _layoutDecorations;
        PreparedDeclaration? _hint;
        int _hash;

        public void Set(IReadOnlyList<Entry> entries, string? inline, ComputedStyle? parent, bool blockify, bool textControl, float rootFontSize,
            AppliedDecoration[]? layoutDecorations, bool outermostSvg, PreparedDeclaration? hint)
        {
            _hint = hint;
            _entries = entries;
            _inline = inline;
            _parent = parent;
            _blockify = blockify;
            _textControl = textControl;
            _rootFontSize = rootFontSize;
            _layoutDecorations = layoutDecorations;
            _outermostSvg = outermostSvg;
            var hash = new HashCode();
            foreach (var entry in entries)
                hash.Add(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(entry));
            hash.Add(inline);
            hash.Add(parent is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(parent));
            hash.Add(blockify);
            hash.Add(textControl);
            hash.Add(rootFontSize);
            hash.Add(hint is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(hint));
            _hash = hash.ToHashCode();
        }

        public SharingKey Copy()
        {
            var copy = new SharingKey();
            copy._entries = _entries.ToArray();
            copy._inline = _inline;
            copy._parent = _parent;
            copy._blockify = _blockify;
            copy._textControl = _textControl;
            copy._rootFontSize = _rootFontSize;
            copy._layoutDecorations = _layoutDecorations;
            copy._outermostSvg = _outermostSvg;
            copy._hint = _hint;
            copy._hash = _hash;
            return copy;
        }

        public bool Equals(SharingKey? other)
        {
            if (other is null || other._hash != _hash || !ReferenceEquals(other._parent, _parent) || other._inline != _inline
                || other._blockify != _blockify || other._textControl != _textControl || other._rootFontSize != _rootFontSize
                || !ReferenceEquals(other._layoutDecorations, _layoutDecorations) || other._outermostSvg != _outermostSvg || !ReferenceEquals(other._hint, _hint)
                || other._entries.Count != _entries.Count)
                return false;
            for (var i = 0; i < _entries.Count; i++)
                if (!ReferenceEquals(other._entries[i], _entries[i]))
                    return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is SharingKey key && Equals(key);

        public override int GetHashCode() => _hash;
    }

    /// <summary>The SVG elements whose transform attribute is a presentation attribute (Blink's SVGTransformableElement).</summary>
    static readonly HashSet<string> SvgTransformable =
        ["svg", "g", "use", "image", "text", "tspan", "textpath", "a", "switch", "foreignobject", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "clippath"];

    /// <summary>Whether an element is an SVG element: an svg, or inside one (not in a foreignObject's content).</summary>
    internal static bool IsSvgElement(UiElement element)
    {
        if (element.Tag == "svg")
            return true;
        for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor.Tag == "foreignobject")
                return false;
            if (ancestor.Tag == "svg")
                return true;
        }
        return false;
    }

    /// <summary>Whether an attribute of the element is a presentational hint (a change restyles the element).</summary>
    internal static bool IsPresentationAttribute(UiElement element, string name) => name switch
    {
        "width" or "height" => element.Tag is "img" or "svg",
        "display" or "visibility" or "opacity" => element.Tag == "svg",
        "transform" => SvgTransformable.Contains(element.Tag) && IsSvgElement(element),
        _ => false,
    };

    static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PreparedDeclaration?> TransformHints = new(StringComparer.Ordinal);

    /// <summary>
    /// A transform attribute as a transform declaration: one instance per attribute text (as Blink's
    /// presentation attribute cache), so elements with the same attribute can share styles.
    /// </summary>
    static PreparedDeclaration? TransformHint(string transform)
    {
        if (TransformHints.TryGetValue(transform, out var kept))
            return kept;
        if (TransformHints.Count > 4096)
            TransformHints.Clear();
        var tokens = CssParser.Trim(CssTokenizer.Tokenize(SvgTransformList.CssText(SvgTransformList.Parse(transform))));
        return TransformHints.GetOrAdd(transform, tokens.Count > 0 && Stylesheet.Validate("transform", tokens) ? new PreparedDeclaration { Property = "transform", Value = tokens } : null);
    }

    /// <summary>
    /// HTML presentational hints: the width and height attributes of img (pixels) and of svg
    /// (lengths, as SVG presentation attributes), and the transform attribute of SVG elements
    /// (a presentation attribute of CSS transform, SVGTransformList::CssValue).
    /// </summary>
    static List<PreparedDeclaration>? PresentationalHints(UiElement element)
    {
        List<PreparedDeclaration>? hints = null;
        if (element.GetAttribute("transform") is { } transform && SvgTransformable.Contains(element.Tag) && IsSvgElement(element)
            && TransformHint(transform) is { } transformHint)
            hints = [transformHint];
        if (element.Tag is not ("img" or "svg"))
            return hints;
        // SVG presentation attributes the layout cares about.
        if (element.Tag == "svg")
            foreach (var name in new[] { "display", "visibility", "opacity" })
                if (element.GetAttribute(name) is { } presentation && CssParser.Trim(CssTokenizer.Tokenize(presentation)) is { Count: > 0 } presentationTokens && Stylesheet.Validate(name, presentationTokens))
                    (hints ??= []).Add(new PreparedDeclaration { Property = name, Value = presentationTokens });
        foreach (var name in new[] { "width", "height" })
        {
            if (element.GetAttribute(name) is not { } value || value.Trim().Length == 0)
                continue;
            var text = value.Trim();
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                text += "px";
            else if (element.Tag == "img" && !text.EndsWith('%'))
                continue;
            var tokens = CssParser.Trim(CssTokenizer.Tokenize(text));
            if (!Stylesheet.Validate(name, tokens))
                continue;
            (hints ??= []).Add(new PreparedDeclaration { Property = name, Value = tokens });
        }
        return hints;
    }

    /// <summary>The computed style for an anonymous box (it inherits everything inheritable, nothing else).</summary>
    public static ComputedStyle Anonymous(ComputedStyle parent, Display display)
    {
        var style = new ComputedStyle();
        foreach (var property in Properties.Inherited)
            property.Copy(style, parent);
        style.Zoom = parent.Zoom;
        style.SubpixelPositioning = parent.SubpixelPositioning;
        style.Custom = parent.Custom;
        style.Display = display;
        style.BaseDecorations = PropagatedDecorations(style, parent);
        style.Finish();
        return style;
    }

    /// <summary>The units at the current zoom: specified (unzoomed) font sizes, the viewport in CSS px, and the zoom.</summary>
    UnitContext Units(float specifiedFontSize, float rootSpecifiedFontSize, float lineHeight = 0) =>
        new(specifiedFontSize, rootSpecifiedFontSize, _environment.Width, _environment.Height, lineHeight, _environment.Zoom);

    ComputedStyle ComputeStyle(Dictionary<string, PreparedDeclaration> winners, ComputedStyle? parent, ComputedStyle? layoutParent, bool isRoot, float rootFontSize, bool pseudoBox = false, bool highlight = false)
    {
        var initial = Initial;
        var parentStyle = parent ?? initial;
        var style = new ComputedStyle { Zoom = _environment.Zoom, SubpixelPositioning = SubpixelPositioning };
        foreach (var property in Properties.Inherited)
            property.Copy(style, parentStyle);
        style.Custom = parentStyle.Custom;

        // Custom properties: cascade, then substitute var() among them.
        Dictionary<string, List<CssToken>>? raw = null;
        foreach (var (name, declaration) in winners)
        {
            if (!declaration.IsCustom)
                continue;
            if (raw is null)
            {
                raw = new Dictionary<string, List<CssToken>>(StringComparer.Ordinal);
                style.Custom = new Dictionary<string, List<CssToken>>(parentStyle.Custom, StringComparer.Ordinal);
            }
            switch (declaration.Keyword)
            {
                case "inherit" or "unset" or "revert":
                    if (parentStyle.Custom.TryGetValue(name, out var inherited))
                        style.Custom[name] = inherited;
                    else
                        style.Custom.Remove(name);
                    break;
                case "initial":
                    style.Custom.Remove(name);
                    break;
                default:
                    if (declaration.HasVar)
                    {
                        raw[name] = declaration.Value;
                        style.Custom.Remove(name);
                    }
                    else
                        style.Custom[name] = declaration.Value;
                    break;
            }
        }
        if (raw is { Count: > 0 })
        {
            var custom = style.Custom;
            var resolving = new HashSet<string>(StringComparer.Ordinal);
            var failed = new HashSet<string>(StringComparer.Ordinal);
            List<CssToken>? Resolve(string name)
            {
                if (custom.TryGetValue(name, out var done))
                    return done;
                if (!raw.TryGetValue(name, out var value) || failed.Contains(name))
                    return null;
                if (!resolving.Add(name))
                {
                    // A cycle: every property in it is invalid at computed-value time.
                    failed.Add(name);
                    return null;
                }
                var substituted = Substitute(value, custom, Resolve);
                resolving.Remove(name);
                if (substituted is null || failed.Contains(name))
                {
                    failed.Add(name);
                    return null;
                }
                custom[name] = substituted;
                return substituted;
            }
            foreach (var name in raw.Keys)
                Resolve(name);
        }

        var context = new StyleContext
        {
            Units = Units(parentStyle.SpecifiedFontSize, rootFontSize),
            Parent = parentStyle,
            Style = style,
        };
        Dictionary<string, List<(string, List<CssToken>)>?>? expansions = null;

        void Apply(Properties.Property property)
        {
            if (!winners.TryGetValue(property.Name, out var declaration))
            {
                if (!property.Inherited)
                    return; // already initial
                return; // already inherited
            }
            var tokens = declaration.Value;
            var keyword = declaration.Keyword;
            if (keyword is null && declaration.PendingShorthand is { } shorthand)
            {
                var substituted = Substitute(declaration.Value, style.Custom, null);
                if (substituted is null)
                    keyword = "unset";
                else if (WideKeyword(substituted) is { } wide)
                    keyword = wide;
                else
                {
                    expansions ??= [];
                    var key = shorthand + "\u0001" + string.Concat(declaration.Value.Select(t => t.ToString()));
                    if (!expansions.TryGetValue(key, out var expanded))
                        expansions[key] = expanded = Properties.Expand(shorthand, CssParser.Trim(substituted));
                    var longhand = expanded?.FirstOrDefault(e => e.Item1 == property.Name);
                    if (longhand is not { Item2: { } value })
                        keyword = "unset";
                    else
                    {
                        tokens = value;
                        keyword = WideKeyword(value);
                    }
                }
            }
            else if (keyword is null && declaration.HasVar)
            {
                var substituted = Substitute(declaration.Value, style.Custom, null);
                if (substituted is null)
                    keyword = "unset";
                else
                {
                    tokens = CssParser.Trim(substituted);
                    keyword = WideKeyword(tokens);
                }
            }
            if (keyword is null)
            {
                if (property.Parse(new ValueReader(tokens), context))
                    return;
                // Invalid at computed-value time: the property is unset.
                keyword = "unset";
            }
            switch (keyword)
            {
                case "inherit":
                    property.Copy(style, parentStyle);
                    break;
                case "initial":
                    property.Copy(style, initial);
                    break;
                default:
                    property.Copy(style, property.Inherited ? parentStyle : initial);
                    break;
            }
        }

        // font-size first (em and the other font-relative units depend on it), then color
        // (currentColor), then line-height (lh), then everything else.
        if (Properties.Get("font-size") is { } fontSize)
            Apply(fontSize);
        context.Units = context.Units with { FontSize = style.SpecifiedFontSize };
        if (isRoot)
            context.Units = context.Units with { RootFontSize = style.SpecifiedFontSize };
        Apply(Properties.Get("color")!);
        Apply(Properties.Get("line-height")!);
        context.Units = context.Units with { LineHeight = style.LineHeight.Kind switch
        {
            LineHeightKind.Number => style.LineHeight.Value * style.FontSize,
            LineHeightKind.Length => style.LineHeight.Value,
            _ => style.FontSize * 1.2f,
        } };
        foreach (var property in Properties.Ordered)
            if (property.Name is not ("font-size" or "color" or "line-height") && winners.ContainsKey(property.Name))
                Apply(property);

        // Blockification (CSS Display 3 §2.7): the root, absolutely positioned boxes and flex/grid items.
        if (isRoot || style.IsOutOfFlow || layoutParent is { IsFlexContainer: true } or { IsGridContainer: true })
            style.Display = Blockify(style.Display);
        if (style.OutlineStyle == BorderLineStyle.None)
            style.OutlineWidth = 0;
        style.BaseDecorations = PropagatedDecorations(style, highlight ? null : layoutParent);
        style.Finish();
        return style;
    }

    /// <summary>
    /// The decorations a box takes from its layout parent (StyleAdjuster's base text decoration data):
    /// the parent's in effect, unless the box stops their propagation (atomic inlines and out-of-flow
    /// boxes, StopPropagateTextDecorations).
    /// </summary>
    internal static AppliedDecoration[] PropagatedDecorations(ComputedStyle style, ComputedStyle? layoutParent) =>
        layoutParent is null || StopsDecorations(style) ? [] : layoutParent.AppliedDecorations;

    static bool StopsDecorations(ComputedStyle style) =>
        style.Display is Display.InlineBlock or Display.InlineFlex or Display.InlineGrid || style.Position is PositionKind.Absolute or PositionKind.Fixed;

    static Display Blockify(Display display) => display switch
    {
        Display.Inline or Display.InlineBlock => Display.Block,
        Display.InlineFlex => Display.Flex,
        Display.InlineGrid => Display.Grid,
        _ => display,
    };

    static string? WideKeyword(List<CssToken> value)
    {
        var trimmed = CssParser.Trim(value);
        if (trimmed.Count == 1 && trimmed[0].Type == CssTokenType.Ident)
            return trimmed[0].Value.ToLowerInvariant() switch
            {
                "inherit" => "inherit",
                "initial" => "initial",
                "unset" => "unset",
                "revert" or "revert-layer" => "revert",
                _ => null,
            };
        return null;
    }

    /// <summary>
    /// Replaces var() references with custom property values (or fallbacks); null when a reference
    /// can't be resolved (the declaration is then invalid at computed-value time).
    /// </summary>
    public static List<CssToken>? Substitute(List<CssToken> tokens, IReadOnlyDictionary<string, List<CssToken>> custom, Func<string, List<CssToken>?>? resolve)
    {
        if (!Stylesheet.ContainsVar(tokens))
            return tokens;
        var output = new List<CssToken>(tokens.Count + 8);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Type != CssTokenType.Function || token.Value != "var")
            {
                output.Add(token);
                continue;
            }
            var arguments = new List<CssToken>();
            var depth = 1;
            for (i++; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Type is CssTokenType.Function or CssTokenType.LeftParen)
                    depth++;
                else if (t.Type == CssTokenType.RightParen && --depth == 0)
                    break;
                arguments.Add(t);
            }
            arguments = CssParser.Trim(arguments);
            if (arguments.Count == 0 || arguments[0].Type != CssTokenType.Ident || !arguments[0].Value.StartsWith("--", StringComparison.Ordinal))
                return null;
            var name = arguments[0].Value;
            var value = resolve is not null ? resolve(name) : custom.TryGetValue(name, out var found) ? found : null;
            if (value is null)
            {
                var comma = -1;
                var d = 0;
                for (var j = 1; j < arguments.Count; j++)
                {
                    if (arguments[j].Type is CssTokenType.Function or CssTokenType.LeftParen)
                        d++;
                    else if (arguments[j].Type == CssTokenType.RightParen)
                        d--;
                    else if (d == 0 && arguments[j].Type == CssTokenType.Comma)
                    {
                        comma = j;
                        break;
                    }
                }
                if (comma < 0)
                    return null;
                value = Substitute(CssParser.Trim(arguments.GetRange(comma + 1, arguments.Count - comma - 1)), custom, resolve);
                if (value is null)
                    return null;
            }
            // Substituted values never merge with their neighbours into one token.
            if (output.Count > 0 && output[^1].Type != CssTokenType.Whitespace && value.Count > 0)
                output.Add(CssToken.Space);
            output.AddRange(value);
            if (i + 1 < tokens.Count && tokens[i + 1].Type != CssTokenType.Whitespace && tokens[i + 1].Type is not (CssTokenType.Comma or CssTokenType.RightParen))
                output.Add(CssToken.Space);
        }
        return output;
    }

    /// <summary>
    /// Applies one declaration to a style (keyframe values): the value is computed in the element's
    /// context. False if the value doesn't apply.
    /// </summary>
    public bool ApplyDeclaration(ComputedStyle style, ComputedStyle parent, PreparedDeclaration declaration, float rootFontSize)
    {
        var property = Properties.Get(declaration.Property);
        if (property is null)
            return false;
        var tokens = declaration.Value;
        if (declaration.PendingShorthand is { } shorthand)
        {
            var substituted = Substitute(declaration.Value, style.Custom, null);
            var longhand = substituted is null ? null : Properties.Expand(shorthand, CssParser.Trim(substituted))?.FirstOrDefault(e => e.Item1 == property.Name);
            if (longhand is not { Item2: { } value })
                return false;
            tokens = value;
        }
        else if (declaration.HasVar)
        {
            var substituted = Substitute(declaration.Value, style.Custom, null);
            if (substituted is null)
                return false;
            tokens = CssParser.Trim(substituted);
        }
        switch (WideKeyword(tokens))
        {
            case "inherit":
                property.Copy(style, parent);
                return true;
            case "initial":
                property.Copy(style, Initial);
                return true;
            case not null:
                property.Copy(style, property.Inherited ? parent : Initial);
                return true;
        }
        var lineHeight = style.LineHeight.Kind switch
        {
            LineHeightKind.Number => style.LineHeight.Value * style.FontSize,
            LineHeightKind.Length => style.LineHeight.Value,
            _ => style.FontSize * 1.2f,
        };
        var context = new StyleContext
        {
            Units = Units(property.Name == "font-size" ? parent.SpecifiedFontSize : style.SpecifiedFontSize, rootFontSize, lineHeight),
            Parent = parent,
            Style = style,
        };
        return property.Parse(new ValueReader(tokens), context);
    }
}
