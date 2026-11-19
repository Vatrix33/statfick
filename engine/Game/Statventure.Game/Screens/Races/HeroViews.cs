using Statventure.Game.Data.Looks;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Races;

/// <summary>
/// The 3D views of the race carousel and the character maker (Q28, Q36, Q41; GM2-P16): the race's figure or the hero's
/// look in a <c>surface</c> element (idling in a fixed three-quarter view, Q41), and the carousel's live, slowly turning
/// emblem of the race being chosen (Q36). The screens say what each surface shows and when it stops showing; building and
/// releasing the views (a look's figure, the emblem's model, pausing while hidden, the memory and per-frame cost) is GM2
/// step 11's. Until it lands the screens have <see cref="NoHeroViews"/>: the figure panels stay empty and the emblem shows
/// its placeholder (✦ in the race's colour). Stand-ins, never shipped (GP-3): Docs/Game/Hero.md, "How it works", step 8.
/// </summary>
public interface IHeroViews
{
    /// <summary>
    /// The figure of <paramref name="look"/> (null: the race <paramref name="race"/> has no look to show, an enemy-only race
    /// or a race with no body) in <paramref name="surface"/>, in place of what it showed.
    /// </summary>
    void ShowFigure(UiElement surface, string race, LookHero? look);

    /// <summary>The race's emblem (<paramref name="emblem"/>, its name; null: none) live in <paramref name="surface"/>; whether a view shows there now (else the placeholder stays).</summary>
    bool ShowEmblem(UiElement surface, string race, string? emblem);

    /// <summary>Nothing shown in <paramref name="surface"/> any more (its screen hid): the view released.</summary>
    void Hide(UiElement surface);
}

/// <summary>No views yet (until GM2 step 11): every surface stays as its sheet draws it.</summary>
public sealed class NoHeroViews : IHeroViews
{
    public static NoHeroViews Instance { get; } = new();

    public void ShowFigure(UiElement surface, string race, LookHero? look)
    {
    }

    public bool ShowEmblem(UiElement surface, string race, string? emblem) => false;

    public void Hide(UiElement surface)
    {
    }
}
