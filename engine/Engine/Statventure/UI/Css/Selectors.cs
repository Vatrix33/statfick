using System.Text.RegularExpressions;
using Statventure.UI.Dom;

namespace Statventure.UI.Css;

public enum Combinator
{
    Descendant,
    Child,
    Adjacent,
    Sibling,
}

public enum PseudoElement
{
    None,
    Before,
    After,
    Placeholder,
    Selection,
    /// <summary>::marker, and ::-webkit-details-marker (a summary's marker).</summary>
    Marker,
    // The parts of form controls (Blink's user-agent shadow elements), styled through their host.
    ProgressInnerElement,
    ProgressBar,
    ProgressValue,
    InnerSpinButton,
    SearchCancelButton,
    SliderContainer,
    SliderRunnableTrack,
    SliderThumb,
    FileUploadButton,
    /// <summary>Any other ::-webkit-* pseudo-element: valid (the rule stays, as in Blink) but it matches nothing.</summary>
    UnknownWebkit,
}

public readonly record struct Specificity(int Ids, int Classes, int Types) : IComparable<Specificity>
{
    public int CompareTo(Specificity other) =>
        Ids != other.Ids ? Ids.CompareTo(other.Ids) : Classes != other.Classes ? Classes.CompareTo(other.Classes) : Types.CompareTo(other.Types);

    public static Specificity operator +(Specificity a, Specificity b) => new(a.Ids + b.Ids, a.Classes + b.Classes, a.Types + b.Types);
    public static bool operator >(Specificity a, Specificity b) => a.CompareTo(b) > 0;
    public static bool operator <(Specificity a, Specificity b) => a.CompareTo(b) < 0;
}

public enum AttributeMatch
{
    Exists,
    Equals,
    StartsWith,
    Contains,
    EndsWith,
    Word,
    DashPrefix,
}

public sealed record AttributeTest(string Name, AttributeMatch Match, string Value, bool IgnoreCase);

public enum PseudoClassKind
{
    Hover,
    Active,
    Focus,
    FocusVisible,
    FocusWithin,
    Disabled,
    Enabled,
    Checked,
    FirstChild,
    LastChild,
    OnlyChild,
    NthChild,
    NthLastChild,
    NthOfType,
    FirstOfType,
    LastOfType,
    Empty,
    Root,
    Not,
    Is,
    Where,
    Has,
    PlaceholderShown,
    AnyLink,
    // Form states.
    Indeterminate,
    Required,
    Optional,
    ReadOnly,
    ReadWrite,
    Valid,
    Invalid,
    UserValid,
    UserInvalid,
    InRange,
    OutOfRange,
    Default,
    Open,
    /// <summary>Pseudo-classes browsers know that mean nothing in the engine: never match.</summary>
    Unsupported,
    /// <summary>Not a pseudo-class at all (the selector is invalid).</summary>
    Unknown,
}

public sealed record PseudoClass(PseudoClassKind Kind, SelectorList? Arguments = null, int A = 0, int B = 0, bool HasChildCombinator = false);

/// <summary>One compound selector: a type, an id, classes, attribute tests and pseudo-classes, all of which must match.</summary>
public sealed class CompoundSelector
{
    public string? Tag;
    public string? Id;
    public List<string> Classes = [];
    public List<AttributeTest> Attributes = [];
    public List<PseudoClass> PseudoClasses = [];

    public Specificity Specificity
    {
        get
        {
            var s = new Specificity(Id is null ? 0 : 1, Classes.Count + Attributes.Count, Tag is null || Tag == "*" ? 0 : 1);
            foreach (var pseudo in PseudoClasses)
                s += pseudo.Kind switch
                {
                    PseudoClassKind.Where => default,
                    PseudoClassKind.Not or PseudoClassKind.Is or PseudoClassKind.Has => pseudo.Arguments?.MaxSpecificity ?? default,
                    _ => new Specificity(0, 1, 0),
                };
            return s;
        }
    }
}

/// <summary>A complex selector: compounds joined by combinators, plus an optional pseudo-element.</summary>
public sealed class Selector
{
    /// <summary>Compounds from left to right; Combinators[i] joins Compounds[i] and Compounds[i + 1].</summary>
    public List<CompoundSelector> Compounds = [];
    public List<Combinator> Combinators = [];
    public PseudoElement PseudoElement;
    public string Text = "";

    public Specificity Specificity
    {
        get
        {
            var s = default(Specificity);
            foreach (var compound in Compounds)
                s += compound.Specificity;
            if (PseudoElement != PseudoElement.None)
                s += new Specificity(0, 0, 1);
            return s;
        }
    }

    public override string ToString() => Text;
}

public sealed class SelectorList(List<Selector> selectors)
{
    public List<Selector> Selectors { get; } = selectors;
    public Specificity MaxSpecificity => Selectors.Count == 0 ? default : Selectors.Max(s => s.Specificity);

    /// <summary>Does any selector in the list match (as a whole, without pseudo-elements)?</summary>
    public bool Matches(UiElement element)
    {
        foreach (var selector in Selectors)
            if (selector.PseudoElement == PseudoElement.None && SelectorMatcher.Matches(selector, element))
                return true;
        return false;
    }

    public override string ToString() => string.Join(", ", Selectors.Select(s => s.Text));
}

/// <summary>Parses selector lists (Selectors Level 4 subset).</summary>
public static class SelectorParser
{
    public static SelectorList? ParseList(string text) => ParseList(CssParser.Trim(CssTokenizer.Tokenize(text)));

    /// <summary>A list of selectors; null if any selector is invalid (the whole rule is dropped, as in CSS).</summary>
    public static SelectorList? ParseList(List<CssToken> tokens)
    {
        var selectors = new List<Selector>();
        foreach (var part in CssParser.SplitOnCommas(tokens))
        {
            var selector = Parse(part);
            if (selector is null)
                return null;
            selectors.Add(selector);
        }
        return selectors.Count == 0 ? null : new SelectorList(selectors);
    }

    static Selector? Parse(List<CssToken> tokens)
    {
        var selector = new Selector { Text = string.Concat(tokens.Select(t => t.ToString())).Trim() };
        var compound = new CompoundSelector();
        var compoundStarted = false;
        Combinator? pending = null;
        var i = 0;
        void Finish()
        {
            if (!compoundStarted)
                return;
            if (selector.Compounds.Count > 0)
                selector.Combinators.Add(pending ?? Combinator.Descendant);
            selector.Compounds.Add(compound);
            compound = new CompoundSelector();
            compoundStarted = false;
            pending = null;
        }
        while (i < tokens.Count)
        {
            var token = tokens[i];
            switch (token.Type)
            {
                case CssTokenType.Whitespace:
                    Finish();
                    i++;
                    continue;
                case CssTokenType.Delim when token.Value is ">" or "+" or "~":
                    Finish();
                    pending = token.Value switch { ">" => Combinator.Child, "+" => Combinator.Adjacent, _ => Combinator.Sibling };
                    i++;
                    continue;
                case CssTokenType.Ident:
                    compound.Tag = token.Value.ToLowerInvariant();
                    break;
                case CssTokenType.Delim when token.Value == "*":
                    compound.Tag = "*";
                    break;
                case CssTokenType.Hash:
                    compound.Id = token.Value;
                    break;
                case CssTokenType.Delim when token.Value == ".":
                    if (i + 1 >= tokens.Count || tokens[i + 1].Type != CssTokenType.Ident)
                        return null;
                    compound.Classes.Add(tokens[++i].Value);
                    break;
                case CssTokenType.LeftBracket:
                {
                    var inner = new List<CssToken>();
                    for (i++; i < tokens.Count && tokens[i].Type != CssTokenType.RightBracket; i++)
                        if (tokens[i].Type != CssTokenType.Whitespace)
                            inner.Add(tokens[i]);
                    var test = ParseAttribute(inner);
                    if (test is null)
                        return null;
                    compound.Attributes.Add(test);
                    break;
                }
                case CssTokenType.Colon:
                {
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == CssTokenType.Colon)
                    {
                        i += 2;
                        if (i >= tokens.Count)
                            return null;
                        selector.PseudoElement = PseudoElementNamed(tokens[i].Value);
                        if (selector.PseudoElement == PseudoElement.None)
                            return null;
                        break;
                    }
                    i++;
                    if (i >= tokens.Count)
                        return null;
                    var name = tokens[i];
                    if (name.Type == CssTokenType.Ident)
                    {
                        var legacy = PseudoElementNamed(name.Value);
                        if (legacy is PseudoElement.Before or PseudoElement.After)
                        {
                            selector.PseudoElement = legacy;
                            break;
                        }
                        var kind = SimplePseudoClass(name.Value);
                        if (kind == PseudoClassKind.Unknown)
                            return null; // an unknown pseudo-class invalidates the rule, as in browsers
                        compound.PseudoClasses.Add(new PseudoClass(kind));
                    }
                    else if (name.Type == CssTokenType.Function)
                    {
                        var argument = new List<CssToken>();
                        var depth = 1;
                        for (i++; i < tokens.Count; i++)
                        {
                            if (tokens[i].Type is CssTokenType.Function or CssTokenType.LeftParen)
                                depth++;
                            else if (tokens[i].Type == CssTokenType.RightParen && --depth == 0)
                                break;
                            argument.Add(tokens[i]);
                        }
                        var pseudo = FunctionalPseudoClass(name.Value, CssParser.Trim(argument));
                        if (pseudo is null)
                            return null;
                        compound.PseudoClasses.Add(pseudo);
                    }
                    else
                        return null;
                    break;
                }
                default:
                    return null;
            }
            compoundStarted = true;
            i++;
        }
        Finish();
        if (selector.Compounds.Count == 0)
        {
            if (selector.PseudoElement == PseudoElement.None)
                return null;
            selector.Compounds.Add(new CompoundSelector { Tag = "*" });
        }
        return selector;
    }

    static PseudoElement PseudoElementNamed(string name) => name.ToLowerInvariant() switch
    {
        "before" => PseudoElement.Before,
        "after" => PseudoElement.After,
        "placeholder" or "-webkit-input-placeholder" => PseudoElement.Placeholder,
        "selection" => PseudoElement.Selection,
        "marker" or "-webkit-details-marker" => PseudoElement.Marker,
        "-webkit-progress-inner-element" => PseudoElement.ProgressInnerElement,
        "-webkit-progress-bar" => PseudoElement.ProgressBar,
        "-webkit-progress-value" => PseudoElement.ProgressValue,
        "-webkit-inner-spin-button" => PseudoElement.InnerSpinButton,
        "-webkit-search-cancel-button" => PseudoElement.SearchCancelButton,
        "-webkit-slider-container" => PseudoElement.SliderContainer,
        "-webkit-slider-runnable-track" => PseudoElement.SliderRunnableTrack,
        "-webkit-slider-thumb" => PseudoElement.SliderThumb,
        "-webkit-file-upload-button" or "file-selector-button" => PseudoElement.FileUploadButton,
        var other when other.StartsWith("-webkit-", StringComparison.Ordinal) => PseudoElement.UnknownWebkit,
        _ => PseudoElement.None,
    };

    static PseudoClassKind SimplePseudoClass(string name) => name.ToLowerInvariant() switch
    {
        "hover" => PseudoClassKind.Hover,
        "active" => PseudoClassKind.Active,
        "focus" => PseudoClassKind.Focus,
        "focus-visible" => PseudoClassKind.FocusVisible,
        "focus-within" => PseudoClassKind.FocusWithin,
        "disabled" => PseudoClassKind.Disabled,
        "enabled" => PseudoClassKind.Enabled,
        "checked" => PseudoClassKind.Checked,
        "first-child" => PseudoClassKind.FirstChild,
        "last-child" => PseudoClassKind.LastChild,
        "only-child" => PseudoClassKind.OnlyChild,
        "first-of-type" => PseudoClassKind.FirstOfType,
        "last-of-type" => PseudoClassKind.LastOfType,
        "empty" => PseudoClassKind.Empty,
        "root" => PseudoClassKind.Root,
        "placeholder-shown" => PseudoClassKind.PlaceholderShown,
        "any-link" or "link" or "-webkit-any-link" => PseudoClassKind.AnyLink,
        "indeterminate" => PseudoClassKind.Indeterminate,
        "required" => PseudoClassKind.Required,
        "optional" => PseudoClassKind.Optional,
        "read-only" => PseudoClassKind.ReadOnly,
        "read-write" => PseudoClassKind.ReadWrite,
        "valid" => PseudoClassKind.Valid,
        "invalid" => PseudoClassKind.Invalid,
        "user-valid" => PseudoClassKind.UserValid,
        "user-invalid" => PseudoClassKind.UserInvalid,
        "in-range" => PseudoClassKind.InRange,
        "out-of-range" => PseudoClassKind.OutOfRange,
        "default" => PseudoClassKind.Default,
        "open" => PseudoClassKind.Open,
        // Known to browsers but not meaningful here: they never match.
        "visited" or "target" or "fullscreen" or "modal" or "autofill" or "-webkit-autofill" or "defined"
            or "scope" or "popover-open" or "closed" => PseudoClassKind.Unsupported,
        _ => PseudoClassKind.Unknown,
    };

    static readonly Regex NthPattern = new(@"^\s*(?:(?<odd>odd)|(?<even>even)|(?<a>[+-]?\d*)n\s*(?:(?<sign>[+-])\s*(?<b>\d+))?|(?<only>[+-]?\d+))\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static PseudoClass? FunctionalPseudoClass(string name, List<CssToken> argument)
    {
        switch (name)
        {
            case "not":
            case "is":
            case "matches":
            case "-webkit-any":
            case "where":
            {
                var list = ParseList(argument);
                if (list is null)
                    return null;
                return new PseudoClass(name switch { "not" => PseudoClassKind.Not, "where" => PseudoClassKind.Where, _ => PseudoClassKind.Is }, list);
            }
            case "has":
            {
                // Relative selectors: "> .x" (children) or ".x" (descendants).
                var child = argument.Count > 0 && argument[0].IsDelim('>');
                var list = ParseList(CssParser.Trim(child ? argument.GetRange(1, argument.Count - 1) : argument));
                return list is null ? null : new PseudoClass(PseudoClassKind.Has, list, HasChildCombinator: child);
            }
            case "nth-child":
            case "nth-last-child":
            case "nth-of-type":
            {
                var match = NthPattern.Match(string.Concat(argument.Select(t => t.ToString())));
                if (!match.Success)
                    return null;
                int a, b;
                if (match.Groups["odd"].Success)
                    (a, b) = (2, 1);
                else if (match.Groups["even"].Success)
                    (a, b) = (2, 0);
                else if (match.Groups["only"].Success)
                    (a, b) = (0, int.Parse(match.Groups["only"].Value));
                else
                {
                    var aText = match.Groups["a"].Value;
                    a = aText is "" or "+" ? 1 : aText == "-" ? -1 : int.Parse(aText);
                    b = match.Groups["b"].Success ? int.Parse(match.Groups["b"].Value) * (match.Groups["sign"].Value == "-" ? -1 : 1) : 0;
                }
                var kind = name switch { "nth-last-child" => PseudoClassKind.NthLastChild, "nth-of-type" => PseudoClassKind.NthOfType, _ => PseudoClassKind.NthChild };
                return new PseudoClass(kind, A: a, B: b);
            }
            default:
                return new PseudoClass(PseudoClassKind.Unsupported);
        }
    }

    static AttributeTest? ParseAttribute(List<CssToken> inner)
    {
        if (inner.Count == 0 || inner[0].Type != CssTokenType.Ident)
            return null;
        var name = inner[0].Value.ToLowerInvariant();
        if (inner.Count == 1)
            return new AttributeTest(name, AttributeMatch.Exists, "", false);
        var at = 1;
        var match = AttributeMatch.Equals;
        if (inner[at].Type == CssTokenType.Delim && inner[at].Value != "=")
        {
            match = inner[at].Value switch
            {
                "^" => AttributeMatch.StartsWith,
                "*" => AttributeMatch.Contains,
                "$" => AttributeMatch.EndsWith,
                "~" => AttributeMatch.Word,
                "|" => AttributeMatch.DashPrefix,
                _ => AttributeMatch.Equals,
            };
            at++;
        }
        if (at >= inner.Count || !inner[at].IsDelim('='))
            return null;
        at++;
        if (at >= inner.Count)
            return null;
        var value = inner[at].Type == CssTokenType.Number ? inner[at].ToString() : inner[at].Value;
        var ignoreCase = at + 1 < inner.Count && inner[at + 1].IsIdent("i");
        return new AttributeTest(name, match, value, ignoreCase);
    }
}

/// <summary>Matches selectors against elements, right to left.</summary>
public static class SelectorMatcher
{
    public static bool Matches(Selector selector, UiElement element) => MatchFrom(selector, selector.Compounds.Count - 1, element);

    static bool MatchFrom(Selector selector, int index, UiElement element)
    {
        if (!MatchesCompound(selector.Compounds[index], element))
            return false;
        if (index == 0)
            return true;
        switch (selector.Combinators[index - 1])
        {
            case Combinator.Child:
                return element.ParentElement is { } parent && MatchFrom(selector, index - 1, parent);
            case Combinator.Descendant:
                for (var ancestor = element.ParentElement; ancestor is not null; ancestor = ancestor.ParentElement)
                    if (MatchFrom(selector, index - 1, ancestor))
                        return true;
                return false;
            case Combinator.Adjacent:
                return element.PreviousElementSibling is { } previous && MatchFrom(selector, index - 1, previous);
            default:
                for (var sibling = element.PreviousElementSibling; sibling is not null; sibling = sibling.PreviousElementSibling)
                    if (MatchFrom(selector, index - 1, sibling))
                        return true;
                return false;
        }
    }

    public static bool MatchesCompound(CompoundSelector compound, UiElement element)
    {
        if (compound.Tag is { } tag && tag != "*" && tag != element.Tag)
            return false;
        if (compound.Id is { } id && id != element.Id)
            return false;
        foreach (var cls in compound.Classes)
            if (!element.HasClass(cls))
                return false;
        foreach (var test in compound.Attributes)
            if (!MatchesAttribute(test, element))
                return false;
        foreach (var pseudo in compound.PseudoClasses)
            if (!MatchesPseudo(pseudo, element))
                return false;
        return true;
    }

    static bool MatchesAttribute(AttributeTest test, UiElement element)
    {
        var value = element.GetAttribute(test.Name);
        if (value is null)
            return false;
        var comparison = test.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return test.Match switch
        {
            AttributeMatch.Exists => true,
            AttributeMatch.Equals => string.Equals(value, test.Value, comparison),
            AttributeMatch.StartsWith => test.Value.Length > 0 && value.StartsWith(test.Value, comparison),
            AttributeMatch.EndsWith => test.Value.Length > 0 && value.EndsWith(test.Value, comparison),
            AttributeMatch.Contains => test.Value.Length > 0 && value.Contains(test.Value, comparison),
            AttributeMatch.Word => ContainsWord(value, test.Value, comparison),
            AttributeMatch.DashPrefix => string.Equals(value, test.Value, comparison) || value.StartsWith(test.Value + "-", comparison),
            _ => false,
        };
    }

    static bool ContainsWord(string value, string word, StringComparison comparison)
    {
        if (word.Length == 0)
            return false;
        var start = 0;
        while (start < value.Length)
        {
            while (start < value.Length && char.IsWhiteSpace(value[start]))
                start++;
            var end = start;
            while (end < value.Length && !char.IsWhiteSpace(value[end]))
                end++;
            if (end - start == word.Length && string.Compare(value, start, word, 0, word.Length, comparison) == 0)
                return true;
            start = end;
        }
        return false;
    }

    static int SiblingsBefore(UiElement element, bool sameType)
    {
        var count = 0;
        for (var sibling = element.PreviousElementSibling; sibling is not null; sibling = sibling.PreviousElementSibling)
            if (!sameType || sibling.Tag == element.Tag)
                count++;
        return count;
    }

    static int SiblingsAfter(UiElement element, bool sameType)
    {
        var count = 0;
        for (var sibling = element.NextElementSibling; sibling is not null; sibling = sibling.NextElementSibling)
            if (!sameType || sibling.Tag == element.Tag)
                count++;
        return count;
    }

    static bool IsEmpty(UiElement element)
    {
        // Comments don't count (Blink's Element::IsEmpty… : only elements and text with data do).
        foreach (var child in element.Children)
            if (child is not (UiText { Text.Length: 0 } or UiComment))
                return false;
        return true;
    }

    static bool HasMatchingDescendant(UiElement element, SelectorList selectors, bool childrenOnly)
    {
        foreach (var child in element.Children)
        {
            if (child is not UiElement childElement)
                continue;
            if (selectors.Matches(childElement) || (!childrenOnly && HasMatchingDescendant(childElement, selectors, false)))
                return true;
        }
        return false;
    }

    static bool MatchesNth(int a, int b, int position)
    {
        if (a == 0)
            return position == b;
        var n = (position - b) / (double)a;
        return n >= 0 && Math.Abs(n - Math.Round(n)) < 1e-9;
    }

    static bool MatchesPseudo(PseudoClass pseudo, UiElement element)
    {
        switch (pseudo.Kind)
        {
            case PseudoClassKind.Hover: return element.IsHovered;
            case PseudoClassKind.Active: return element.IsActive;
            // Focus shows only while the window has it (Blink's IsFrameFocused).
            case PseudoClassKind.Focus: return element.IsFocused && element.Document?.WindowFocused != false;
            case PseudoClassKind.FocusVisible: return element.IsFocused && element.FocusVisible && element.Document?.WindowFocused != false;
            case PseudoClassKind.FocusWithin:
                return element.Document is { WindowFocused: true, FocusedElement: { } focused } && (focused == element || focused.IsDescendantOf(element));
            case PseudoClassKind.Disabled: return element.IsDisabled;
            case PseudoClassKind.Enabled: return element.IsFormControl && !element.IsDisabled;
            case PseudoClassKind.Checked: return element.IsChecked;
            case PseudoClassKind.FirstChild: return element.PreviousElementSibling is null && element.ParentElement is not null;
            case PseudoClassKind.LastChild: return element.NextElementSibling is null && element.ParentElement is not null;
            case PseudoClassKind.OnlyChild: return element.PreviousElementSibling is null && element.NextElementSibling is null && element.ParentElement is not null;
            case PseudoClassKind.FirstOfType: return SiblingsBefore(element, sameType: true) == 0;
            case PseudoClassKind.LastOfType: return SiblingsAfter(element, sameType: true) == 0;
            case PseudoClassKind.NthChild: return element.ParentElement is not null && MatchesNth(pseudo.A, pseudo.B, SiblingsBefore(element, sameType: false) + 1);
            case PseudoClassKind.NthLastChild: return element.ParentElement is not null && MatchesNth(pseudo.A, pseudo.B, SiblingsAfter(element, sameType: false) + 1);
            case PseudoClassKind.NthOfType: return element.ParentElement is not null && MatchesNth(pseudo.A, pseudo.B, SiblingsBefore(element, sameType: true) + 1);
            case PseudoClassKind.Empty: return IsEmpty(element);
            case PseudoClassKind.Root: return element.ParentElement is null && element.Document?.Root == element;
            case PseudoClassKind.Not: return !pseudo.Arguments!.Matches(element);
            case PseudoClassKind.Is:
            case PseudoClassKind.Where: return pseudo.Arguments!.Matches(element);
            case PseudoClassKind.Has:
                return HasMatchingDescendant(element, pseudo.Arguments!, childrenOnly: pseudo.HasChildCombinator);
            case PseudoClassKind.PlaceholderShown: return element.ShowsPlaceholder;
            case PseudoClassKind.AnyLink: return element.Tag is "a" or "area" && element.HasAttribute("href");
            case PseudoClassKind.Indeterminate: return element.MatchesIndeterminate;
            case PseudoClassKind.Required: return element.IsRequiredControl;
            case PseudoClassKind.Optional: return element.IsOptionalControl;
            case PseudoClassKind.ReadOnly: return !element.IsReadWrite;
            case PseudoClassKind.ReadWrite: return element.IsReadWrite;
            case PseudoClassKind.Valid: return element.MatchesValidity(valid: true);
            case PseudoClassKind.Invalid: return element.MatchesValidity(valid: false);
            case PseudoClassKind.UserValid: return element.MatchesUserValidity(valid: true);
            case PseudoClassKind.UserInvalid: return element.MatchesUserValidity(valid: false);
            case PseudoClassKind.InRange: return element.MatchesRange(inRange: true);
            case PseudoClassKind.OutOfRange: return element.MatchesRange(inRange: false);
            case PseudoClassKind.Default: return element.MatchesDefault;
            case PseudoClassKind.Open: return (element.Tag is "details" or "dialog" && element.HasAttribute("open")) || (element.Tag == "select" && element.Document?.Input.OpenSelectElement == element);
            default: return false;
        }
    }
}
