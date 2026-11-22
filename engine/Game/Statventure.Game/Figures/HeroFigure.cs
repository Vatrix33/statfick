using Statventure.Animation;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Scene;

namespace Statventure.Game.Figures;

// The game's figures built from looks (GM2 step 5, GM2-P8; Docs/Game/Hero.md, "How it works", step 5): a hero's look
// (look.hero, its choices) becomes one Actor at run time, as the humanoid scripts' assemble.py makes the same look in
// Blender (Tools/Models/humanoid/assemble.py), against which its pictures are held.

/// <summary>
/// Gear a figure wears beside its look (GM3's equipment; GM2's sample sword, shield and chain shirt in the tests): garments
/// made on the look's body (bound to its bones by name, as the look's parts are), items on its sockets, and the colours of
/// the gear's own palette slots (metal, wood, grip: never a look's slot, GM2-P7, since one actor palette colours figure and
/// gear alike).
/// </summary>
public sealed record FigureGear
{
    /// <summary>Garments: models in the game's data (<c>models/…</c>), made on the look's body.</summary>
    public IReadOnlyList<string> Garments { get; init; } = [];

    /// <summary>Rigid items, each a model in the game's data on a socket bone of the body (<c>socket.hand.R</c> …).</summary>
    public IReadOnlyList<FigureItem> Items { get; init; } = [];

    /// <summary>The gear's palette slots' colours ("#rrggbb"), by slot.</summary>
    public IReadOnlyDictionary<string, string> Colours { get; init; } = new Dictionary<string, string>();
}

/// <summary>A rigid item: its model in the game's data and the socket bone it sits on.</summary>
public sealed record FigureItem(string Model, string Socket);

/// <summary>A part of a look as the figure draws it: its category, its name, and its model for the look's body (none: a part drawn as nothing, "None", "Bald").</summary>
public sealed record FigurePart(string Category, string Part, string? Model);

/// <summary>
/// What a look comes to in the game's data (<see cref="HeroFigure.Resolve"/>): the body's model, each part chosen with its
/// model for that body, the gear's garments and items, and the palette's colours by slot ("#rrggbb": the look's, each the
/// race's swatch of the name it chose, then the gear's).
/// </summary>
public sealed record FigurePlan(string Body, IReadOnlyList<FigurePart> Parts, IReadOnlyList<string> Garments, IReadOnlyList<FigureItem> Items,
    IReadOnlyDictionary<string, string> Colours)
{
    /// <summary>Every model the figure draws bound to the body's bones: the parts' (in the maker's order of categories), then the gear's garments.</summary>
    public IEnumerable<string> GarmentModels => Parts.Where(p => p.Model is not null).Select(p => p.Model!).Concat(Garments);
}

/// <summary>A look the game's data can't build: everything it names that isn't there (GM2-P18: a look's models are references, refused when missing).</summary>
public sealed class LookRefusedException(IReadOnlyList<string> problems)
    : Exception("the look can't be built: " + string.Join("; ", problems))
{
    public IReadOnlyList<string> Problems { get; } = problems;
}

/// <summary>
/// A hero's figure built from its look (GM2-P8): one <see cref="Actor"/> of the look's body model (its skeleton and its
/// plain outfit), every part chosen made on that body as an <see cref="ActorGarment"/> (bound to its bones by name: no
/// refit, a part is fitted to the bodies its models name), the gear's garments likewise and its rigid items on their
/// sockets as <see cref="ActorItem"/>s, the palette made of the look's colours (each a swatch of the race's by its name,
/// into its slot) and the gear's. The actor stands by its soles (GM2-P22, "the figure's ground"): its
/// <see cref="Actor.Ground"/> is the lowest point at rest of its body and its garments (<see cref="Actor.GroundOf"/>; the
/// human bodies' boots 24 mm under their origin), so the node it hangs on is where its soles are, and a clip's hips' place
/// follows the body's legs (<see cref="ClipBinding.Scale"/>). The actor's animator has no clips yet (the screens add
/// theirs, GM2 step 11); a new figure is built when the look or the gear changes (a new body means a new actor). A look
/// that names anything the data
/// hasn't (a race, a body, a part, a model, a swatch: a mod switched off under a slot's save) is refused with what is
/// missing (<see cref="LookRefusedException"/>), never drawn with a piece left out; what it leaves out it may leave out
/// (a category the body offers that it doesn't choose draws nothing: a mod adding a category mustn't refuse older heroes).
/// The figure owns its models: disposing it frees them.
/// </summary>
public sealed class HeroFigure : IDisposable
{
    readonly List<Model> _models;

    HeroFigure(FigurePlan plan, Actor actor, Skeleton skeleton, List<Model> models)
    {
        Plan = plan;
        Actor = actor;
        Skeleton = skeleton;
        _models = models;
    }

    /// <summary>What the look came to in the data.</summary>
    public FigurePlan Plan { get; }

    /// <summary>The figure, to attach to a scene node: the node is where its soles stand (<see cref="Actor.Ground"/>).</summary>
    public Actor Actor { get; }

    /// <summary>The body's skeleton (its clips are bound to it by bone names).</summary>
    public Skeleton Skeleton { get; }

    /// <summary>The models the figure draws, the body's first, then the garments' and the items' as the plan lists them.</summary>
    public IReadOnlyList<Model> Models => _models;

    /// <summary>
    /// What <paramref name="look"/> (with <paramref name="gear"/>) comes to in <paramref name="data"/>, or a
    /// <see cref="LookRefusedException"/> naming all it names that the data hasn't: the body and its model, each part and
    /// its model for the body (a part with models but none for the look's body can't be drawn on it), the race and each
    /// swatch the colours name, the gear's models. Reads the records only, no model.
    /// </summary>
    public static FigurePlan Resolve(VirtualFileSystem data, LookHero look, FigureGear? gear = null)
    {
        gear ??= new FigureGear();
        var problems = new List<string>();
        void Model(string path, string whose)
        {
            if (!data.Exists(path))
                problems.Add($"{whose}: no model \"{path}\"");
        }
        var body = LookData.Body(data, look.Body);
        if (body is null)
            problems.Add($"no look body \"{look.Body}\" ({LookNames.BodiesFolder}/{look.Body})");
        else
            Model(body.Model, $"the body \"{look.Body}\"");
        var parts = new List<FigurePart>();
        // A part's model is the one fitted to the look's body and its face (a mark is made for each face, Q65).
        var face = LookData.FaceOf(look);
        foreach (var (category, name) in look.Parts.OrderBy(p => Order(p.Key)).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            if (LookData.Part(data, name) is not { } part)
            {
                problems.Add($"no look part \"{name}\" ({LookNames.PartsFolder}/{name})");
                continue;
            }
            if (part.Models.Count == 0)
            {
                parts.Add(new FigurePart(category, name, null));
                continue;
            }
            if (part.ModelFor(look.Body, face) is not { } model)
            {
                // Without the body its fit is moot: the body is named missing already.
                if (body is not null)
                    problems.Add(!part.Models.Any(m => m.Bodies.Contains(look.Body, StringComparer.Ordinal)) ? $"\"{name}\" has no model for the body \"{look.Body}\""
                        : face is null ? $"\"{name}\" has no model for the body \"{look.Body}\" without a face"
                        : $"\"{name}\" has no model for the face \"{face}\" on the body \"{look.Body}\"");
                continue;
            }
            Model(model.Model, $"\"{name}\"");
            parts.Add(new FigurePart(category, name, model.Model));
        }
        var colours = new Dictionary<string, string>(StringComparer.Ordinal);
        if (look.Colours.Count > 0)
        {
            if (LookData.RaceOf(data, look.Race) is not { } race)
                problems.Add($"no race \"{look.Race}\" ({RaceCatalogue.Folder}/{look.Race}) for the colours");
            else
                foreach (var (slot, name) in look.Colours.OrderBy(c => c.Key, StringComparer.Ordinal))
                {
                    if (race.Swatches.FirstOrDefault(s => s.Slot == slot && s.Name == name) is { } swatch)
                        colours[slot] = swatch.Colour;
                    else
                        problems.Add($"the {look.Race} has no {slot} colour \"{name}\"");
                }
        }
        foreach (var garment in gear.Garments)
            Model(garment, "a garment");
        foreach (var item in gear.Items)
            Model(item.Model, $"an item on {item.Socket}");
        foreach (var (slot, colour) in gear.Colours.OrderBy(c => c.Key, StringComparer.Ordinal))
        {
            if (LookNames.Slots.Contains(slot, StringComparer.Ordinal))
                throw new ArgumentException($"the gear's colour for \"{slot}\": that slot is a look's (the gear's slots are its own, GM2-P7)", nameof(gear));
            if (!RenderStyle.IsColour(colour))
                throw new ArgumentException($"the gear's colour for \"{slot}\": \"{colour}\" isn't a colour (#rrggbb)", nameof(gear));
            colours[slot] = colour;
        }
        if (problems.Count > 0)
            throw new LookRefusedException(problems);
        return new FigurePlan(body!.Model, parts, gear.Garments, gear.Items, colours);
    }

    /// <summary>A category's place in the maker's order (one that isn't a category after them all).</summary>
    static int Order(string category)
    {
        for (var i = 0; i < LookNames.Categories.Count; i++)
            if (LookNames.Categories[i] == category)
                return i;
        return int.MaxValue;
    }

    /// <summary>
    /// The figure of <paramref name="look"/> (with <paramref name="gear"/>) from <paramref name="data"/>, its models made
    /// on <paramref name="device"/>: <see cref="Resolve"/>'s refusal, a model that doesn't read (the data's
    /// <see cref="DataFileException"/>) or an item on a socket the body hasn't (an <see cref="ArgumentException"/>) leave
    /// nothing made.
    /// </summary>
    public static HeroFigure Build(GpuDevice device, VirtualFileSystem data, LookHero look, FigureGear? gear = null)
    {
        var plan = Resolve(data, look, gear);
        var models = new List<Model>();
        try
        {
            Model Load(string path)
            {
                var model = Render3D.Model.Load(device, data, path);
                models.Add(model);
                return model;
            }
            var body = Load(plan.Body);
            var garments = plan.GarmentModels.Select(Load).ToList();
            // The body wears its parts and garments: with its skin they meet the ground a clip's contacts keep (GM2-P37), so
            // a clip bound to this skeleton keeps them on this figure as it is dressed; a new look or gear is a new figure.
            var skeleton = new Skeleton(body.Data, garments.Select(g => g.Data));
            var actor = new Actor(body, new Animator(new AnimationStack(skeleton))) { Palette = new Palette(plan.Colours) };
            foreach (var garment in garments)
                actor.Garments.Add(new ActorGarment(garment, new Garment(garment.Data, skeleton)));
            foreach (var item in plan.Items)
            {
                var model = Load(item.Model);
                actor.Items.Add(new ActorItem(model, new Socket(skeleton, item.Socket)));
            }
            // It stands by its soles (GM2-P22): the lowest point at rest of the body and what it wears on its bones.
            actor.Ground = Actor.GroundOf(body.Data, actor.Garments.Select(g => g.Model.Data));
            return new HeroFigure(plan, actor, skeleton, models);
        }
        catch
        {
            foreach (var model in models)
                model.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var model in _models)
            model.Dispose();
        _models.Clear();
    }
}
