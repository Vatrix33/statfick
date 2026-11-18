using Statventure.Packs;
using Statventure.Render3D;

namespace Statventure.Game.Figures;

/// <summary>
/// The game's look in its data (GM2 step 5; the user's Q12, GM2-P11): one <see cref="RenderStyle"/> entry, the look the
/// user chose ("Ember, hearth", GM2 step 3b; Tools/Models/humanoid/looks/ember-hearth.json), which every game view takes
/// (its <see cref="RenderStyle.Cel"/> as the view's style, its <see cref="RenderStyle.SunLight"/> as its sun) and a mod
/// restyles by an entry of its own at the same path.
/// </summary>
public static class GameStyle
{
    /// <summary>The entry's path: <c>render/styles/game</c>, in data.pak (Assets/Data/render/styles/game.json).</summary>
    public const string Path = "render/styles/game";

    /// <summary>The game's style in <paramref name="data"/>, checked (<see cref="RenderStyle.Load"/>).</summary>
    public static RenderStyle Load(VirtualFileSystem data) => RenderStyle.Load(data, Path);
}
