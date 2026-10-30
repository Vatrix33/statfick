using Statventure.UI.Css;

namespace Statventure.UI.Style;

/// <summary>Which elements a change on one element can restyle.</summary>
[Flags]
public enum Reach
{
    None = 0,
    /// <summary>The element itself.</summary>
    Self = 1,
    /// <summary>Its descendants.</summary>
    Descendants = 2,
    /// <summary>Its following siblings and their descendants.</summary>
    Siblings = 4,
}

/// <summary>
/// What the rules' selectors look at, and how far a change of each reaches (Blink's invalidation
/// sets, simplified): a class, id, attribute or state that no selector names restyles nothing; one
/// only subjects name restyles the element; one in an ancestor position its subtree; one before a
/// sibling combinator the following siblings. Changes to what :has() arguments look at restyle the
/// ancestors.
/// </summary>
public sealed class RuleFeatures
{
    public readonly Dictionary<string, Reach> Classes = new(StringComparer.Ordinal);
    public readonly Dictionary<string, Reach> Ids = new(StringComparer.Ordinal);
    /// <summary>Attribute names (lowercase).</summary>
    public readonly Dictionary<string, Reach> Attributes = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<PseudoClassKind, Reach> States = [];
    /// <summary>Position pseudo-classes (:first-child, :nth-child ...): how far a child's change of position reaches.</summary>
    public Reach Structural;
    /// <summary>:empty: how far a change of an element's emptiness reaches.</summary>
    public Reach Empty;
    public bool SiblingCombinators;

    // Inside :has() arguments: a change of these anywhere restyles the ancestors, with HasReach.
    public readonly HashSet<string> HasClasses = new(StringComparer.Ordinal);
    public readonly HashSet<string> HasIds = new(StringComparer.Ordinal);
    public readonly HashSet<string> HasAttributes = new(StringComparer.OrdinalIgnoreCase);
    public readonly HashSet<string> HasTags = new(StringComparer.Ordinal);
    public readonly HashSet<PseudoClassKind> HasStates = [];
    /// <summary>A :has() argument matches elements by something other than these (any tree change can matter).</summary>
    public bool HasAny;
    public Reach HasReach;

    public bool UsesHas => HasReach != Reach.None;

    static readonly HashSet<PseudoClassKind> Positional =
    [
        PseudoClassKind.FirstChild, PseudoClassKind.LastChild, PseudoClassKind.OnlyChild, PseudoClassKind.NthChild,
        PseudoClassKind.NthLastChild, PseudoClassKind.NthOfType, PseudoClassKind.FirstOfType, PseudoClassKind.LastOfType,
    ];

    public void Add(Selector selector) => Add(selector, Reach.Self, inHas: false);

    void Add(Selector selector, Reach outer, bool inHas)
    {
        var count = selector.Compounds.Count;
        for (var i = 0; i < count; i++)
        {
            var reach = i == count - 1 ? outer : outer & (Reach.Descendants | Reach.Siblings);
            for (var j = i; j < count - 1; j++)
                reach |= selector.Combinators[j] is Combinator.Descendant or Combinator.Child ? Reach.Descendants : Reach.Siblings;
            Add(selector.Compounds[i], reach, inHas);
        }
        if (selector.Combinators.Any(c => c is Combinator.Adjacent or Combinator.Sibling))
            SiblingCombinators = true;
    }

    void Add(CompoundSelector compound, Reach reach, bool inHas)
    {
        if (inHas)
        {
            var named = false;
            if (compound.Tag is { } tag && tag != "*")
            {
                HasTags.Add(tag);
                named = true;
            }
            if (compound.Id is { } id)
            {
                HasIds.Add(id);
                named = true;
            }
            foreach (var cls in compound.Classes)
            {
                HasClasses.Add(cls);
                named = true;
            }
            foreach (var attribute in compound.Attributes)
            {
                HasAttributes.Add(attribute.Name);
                named = true;
            }
            if (!named && compound.PseudoClasses.Count == 0)
                HasAny = true;
        }
        else
        {
            if (compound.Id is { } id)
                Or(Ids, id, reach);
            foreach (var cls in compound.Classes)
                Or(Classes, cls, reach);
            foreach (var attribute in compound.Attributes)
                Or(Attributes, attribute.Name, reach);
        }
        foreach (var pseudo in compound.PseudoClasses)
        {
            switch (pseudo.Kind)
            {
                case PseudoClassKind.Is or PseudoClassKind.Where or PseudoClassKind.Not:
                    foreach (var argument in pseudo.Arguments?.Selectors ?? [])
                        Add(argument, reach, inHas);
                    break;
                case PseudoClassKind.Has:
                    if (!inHas)
                        HasReach |= reach;
                    foreach (var argument in pseudo.Arguments?.Selectors ?? [])
                        Add(argument, Reach.Self, inHas: true);
                    break;
                case PseudoClassKind.Root or PseudoClassKind.Unsupported or PseudoClassKind.Unknown:
                    break;
                case PseudoClassKind.Empty:
                    if (inHas)
                        HasAny = true;
                    else
                        Empty |= reach;
                    break;
                default:
                    if (Positional.Contains(pseudo.Kind))
                    {
                        if (inHas)
                            HasAny = true;
                        else
                            Structural |= reach;
                    }
                    else if (inHas)
                        HasStates.Add(pseudo.Kind);
                    else
                        States[pseudo.Kind] = States.GetValueOrDefault(pseudo.Kind) | reach;
                    break;
            }
        }
    }

    static void Or(Dictionary<string, Reach> map, string key, Reach reach) => map[key] = map.GetValueOrDefault(key) | reach;

    /// <summary>The pseudo-classes an attribute decides.</summary>
    public static PseudoClassKind[] StatesOf(string attribute) => attribute switch
    {
        "checked" => [PseudoClassKind.Checked, PseudoClassKind.Default, PseudoClassKind.Indeterminate, .. Validity],
        "selected" => [PseudoClassKind.Checked, PseudoClassKind.Default, .. Validity],
        "disabled" => [PseudoClassKind.Disabled, PseudoClassKind.Enabled, PseudoClassKind.ReadOnly, PseudoClassKind.ReadWrite, .. Validity],
        "value" => [PseudoClassKind.PlaceholderShown, PseudoClassKind.Indeterminate, PseudoClassKind.InRange, PseudoClassKind.OutOfRange, .. Validity],
        "placeholder" => [PseudoClassKind.PlaceholderShown],
        "href" => [PseudoClassKind.AnyLink],
        "required" => [PseudoClassKind.Required, PseudoClassKind.Optional, .. Validity],
        "readonly" => [PseudoClassKind.ReadOnly, PseudoClassKind.ReadWrite, .. Validity],
        "contenteditable" => [PseudoClassKind.ReadOnly, PseudoClassKind.ReadWrite],
        "min" or "max" or "step" => [PseudoClassKind.InRange, PseudoClassKind.OutOfRange, .. Validity],
        "maxlength" or "minlength" or "pattern" or "multiple" or "size" or "name" or "form" => [.. Validity],
        "open" => [PseudoClassKind.Open],
        "type" =>
        [
            PseudoClassKind.Checked, PseudoClassKind.Disabled, PseudoClassKind.Enabled, PseudoClassKind.PlaceholderShown, PseudoClassKind.Indeterminate,
            PseudoClassKind.Required, PseudoClassKind.Optional, PseudoClassKind.ReadOnly, PseudoClassKind.ReadWrite, PseudoClassKind.InRange,
            PseudoClassKind.OutOfRange, PseudoClassKind.Default, .. Validity,
        ],
        _ => [],
    };

    static readonly PseudoClassKind[] Validity = [PseudoClassKind.Valid, PseudoClassKind.Invalid, PseudoClassKind.UserValid, PseudoClassKind.UserInvalid];
}
