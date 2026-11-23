using Statventure.Game.Data.Looks;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Races;

/// <summary>
/// The 3D views of the race carousel and the character maker (Q28, Q36, Q41; GM2-P16): the race's figure or the hero's
/// look in a <c>surface</c> element (standing in a fixed three-quarter view, Q41, Q57), and the carousel's live emblem of
/// the race being chosen, rocking slowly from side to side (Q36, Q67, Q78). The screens say what each surface shows and
/// when it stops showing; building and releasing the views (a look's figure, the emblem's model, pausing while covered or
/// hidden) is GM2 step 11's <see cref="Figures.LiveHeroViews"/>, which the screens (the sheet's too) take on the game's
/// window; a document without a window (the tests' screens) has <see cref="NoHeroViews"/>: the figure panels stay empty
/// and the medallion shows what it shows without a live view, the emblem's picture (GM2 step 10's) or, for a race with no
/// emblem, the ✦ in the race's colour. Docs/Game/Hero.md, "How it works", steps 8, 10 and 11.
/// </summary>
public interface IHeroViews
{
    /// <summary>
    /// The figure of <paramref name="look"/> (null: the race <paramref name="race"/> has no look to show, an enemy-only race
    /// or a race with no body) in <paramref name="surface"/>, in place of what it showed.
    /// </summary>
    void ShowFigure(UiElement surface, string race, LookHero? look);

    /// <summary>The race's emblem (<paramref name="emblem"/>, its name; null: none) live in <paramref name="surface"/>; whether a view shows there now (else the medallion keeps its picture or its ✦).</summary>
    bool ShowEmblem(UiElement surface, string race, string? emblem);

    /// <summary>Nothing shown in <paramref name="surface"/> any more (its screen hid): the view released.</summary>
    void Hide(UiElement surface);
}

/// <summary>No views (a document without a window): every surface stays as its sheet draws it.</summary>
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
