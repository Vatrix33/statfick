using System.Text;
using System.Text.Json;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>A race file the old game refuses (its own message, as <c>applyRaceText</c> and <c>normalizeRaceDefinition</c> throw it), or one the converter can't carry over.</summary>
sealed class OldRaceFileException(string message) : Exception(message);

/// <summary>
/// The converter from the old game's race file (<c>old-game/objects/races.json</c>) to the <c>race</c> entries of
/// <c>data.pak</c> (GM2-P4; Docs/Game/Hero.md, "How it works", step 1): the file read as <c>applyRaceText</c> reads it
/// and each race as <c>normalizeRaceDefinition</c> makes it (<c>old-game/scripts/app.js</c> 2926–2954, 3155–3172), with
/// JavaScript's own rules for what a value is (a string, an object, truthy), refusing what they refuse with their
/// messages; the id the entry's name, the accent the old code's <c>raceAccent</c> table (2956–2958), the order the
/// file's (10, 20, …; Q43), the seal <c>objects/race-&lt;name&gt;.svg</c> as the emblem's name (Q44). What the port's
/// records can't hold as the old game held it the converter refuses with its own message rather than guess: an id or a
/// passive's id, condition or description that isn't text, an effect without a <c>type</c> of the union's, a field of an
/// effect its type doesn't have or of another kind of value, a seal that isn't <c>objects/race-&lt;name&gt;.svg</c>.
/// <see cref="RaceDataTests"/> holds the committed JSON forms (<c>Assets/Data/data/races/</c>) to its output and writes
/// them with <c>STATVENTURE_RACE_DATA=write</c>.
/// </summary>
static class OldRaceFile
{
    /// <summary><c>raceAccent</c>'s table (<c>app.js</c> 2957): the races with a colour of their own.</summary>
    public static IReadOnlyDictionary<string, string> Accents { get; } = new Dictionary<string, string>
    {
        ["human"] = "#c7683f", ["elf"] = "#728577", ["dark-elf"] = "#77607e", ["dwarf"] = "#8b684e", ["hobbit"] = "#858b6c",
        ["lizardman"] = "#66806a", ["half-beast"] = "#9a6a3f", ["goliath"] = "#7d7a72", ["goblin"] = "#76904f", ["impid"] = "#a14f61",
        ["merfolk"] = "#4c9a8a",
    };

    /// <summary><c>raceAccent(id)</c>: the table's colour, else <see cref="Race.DefaultAccent"/>.</summary>
    public static string AccentOf(string id) => Accents.TryGetValue(id, out var accent) ? accent : Race.DefaultAccent;

    /// <summary>The races of a race file's text, in its order (<c>applyRaceText</c>: an array, each race normalized, no id twice).</summary>
    public static IReadOnlyList<RaceEntry> Convert(string text)
    {
        using var json = JsonDocument.Parse(text);
        if (json.RootElement.ValueKind != JsonValueKind.Array)
            throw new OldRaceFileException("The race catalogue must be a JSON array");
        var races = json.RootElement.EnumerateArray().Select((raw, index) => Normalize(raw, index)).ToList();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var race in races)
            if (!ids.Add(race.Id))
                throw new OldRaceFileException($"Duplicate race id: {race.Id}");
        return races;
    }

    /// <summary>
    /// <c>normalizeRaceDefinition(raw, index)</c>: the race at <paramref name="index"/> (from 0) of a file, as the port's
    /// record and its id, or the old message ("Race &lt;index + 1&gt;: …").
    /// </summary>
    public static RaceEntry Normalize(JsonElement raw, int index)
    {
        string Where(string message) => $"Race {index + 1}: {message}";
        OldRaceFileException Invalid(string message) => new(Where(message));
        if (raw.ValueKind != JsonValueKind.Object)
            throw Invalid("expected a race object");
        var name = Member(raw, "name");
        if (name?.ValueKind != JsonValueKind.String || JsValues.Trim(name.Value.GetString()!).Length == 0)
            throw Invalid("name must be a nonempty string");
        var id = Slugify(Text(Truthy(Member(raw, "id")) ? Member(raw, "id") : name, Where("an id that isn't text")));
        if (id.Length == 0)
            throw Invalid("id must be a nonempty string");
        var given = Member(raw, "statModifiers") is { } m && Truthy(m) && m.ValueKind is JsonValueKind.Object or JsonValueKind.Array ? m : (JsonElement?)null;
        var modifiers = new Dictionary<string, double>();
        foreach (var key in AttributeKeys.All)
        {
            var value = given?.ValueKind == JsonValueKind.Object ? Member(given.Value, key) : null;
            if (value is null || value.Value.ValueKind == JsonValueKind.Null)
                modifiers[key] = 0;
            else if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDouble(out var number) && double.IsFinite(number))
                modifiers[key] = number;
            else
                throw Invalid($"{key} modifier must be a number");
        }
        var passive = Member(raw, "passive");
        if (!Truthy(passive) || passive!.Value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array) || Member(passive.Value, "name")?.ValueKind != JsonValueKind.String)
            throw Invalid("passive must be an object with a name");
        var p = passive.Value;
        var passiveName = Member(p, "name")!.Value;
        var effects = Member(p, "effects") is { ValueKind: JsonValueKind.Array } list
            ? list.EnumerateArray().Where(e => e.ValueKind is JsonValueKind.Object or JsonValueKind.Array).Select((e, i) => EffectOf(e, Where($"effect {i + 1}"))).ToList()
            : [];
        var icon = Member(raw, "icon") is { ValueKind: JsonValueKind.String } s && JsValues.Trim(s.GetString()!).Length > 0 ? JsValues.Trim(s.GetString()!) : null;
        var race = new Race
        {
            Name = JsValues.Trim(name.Value.GetString()!),
            Playable = Member(raw, "playable")?.ValueKind != JsonValueKind.False,
            StatModifiers = new RaceModifiers
            {
                Strength = modifiers[AttributeKeys.Strength], Vitality = modifiers[AttributeKeys.Vitality], Mind = modifiers[AttributeKeys.Mind],
                Dexterity = modifiers[AttributeKeys.Dexterity], Intelligence = modifiers[AttributeKeys.Intelligence], Wisdom = modifiers[AttributeKeys.Wisdom],
                Speed = modifiers[AttributeKeys.Speed], Luck = modifiers[AttributeKeys.Luck],
            },
            Passive = new RacePassive
            {
                Id = Slugify(Text(Truthy(Member(p, "id")) ? Member(p, "id") : passiveName, Where("a passive's id that isn't text"))),
                Name = JsValues.Trim(passiveName.GetString()!),
                Condition = Truthy(Member(p, "condition")) ? Text(Member(p, "condition"), Where("a passive's condition that isn't text")) : RacePassive.AlwaysActive,
                Description = Truthy(Member(p, "description")) ? Text(Member(p, "description"), Where("a passive's description that isn't text")) : "",
                Effects = effects,
            },
            Accent = AccentOf(id),
            Emblem = icon is null ? null : EmblemOf(icon, Where($"a seal the converter can't name: {icon}")),
            Order = (index + 1) * 10,
        };
        return new RaceEntry(id, race);
    }

    /// <summary>
    /// <c>slugify(value)</c> (<c>app.js</c> 2218–2220): JavaScript's lower case, trimmed, every run of characters other
    /// than a–z and 0–9 one "-", none at either end.
    /// </summary>
    public static string Slugify(string value)
    {
        var lower = JsValues.Trim(JsValues.ToLowerCase(value));
        var slug = new StringBuilder(lower.Length);
        var run = false;
        foreach (var c in lower)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(c);
                run = false;
            }
            else if (!run)
            {
                slug.Append('-');
                run = true;
            }
        }
        var text = slug.ToString();
        if (text.StartsWith('-'))
            text = text[1..];
        if (text.EndsWith('-'))
            text = text[..^1];
        return text;
    }

    /// <summary>The emblem's name of a seal's path, <c>objects/race-&lt;name&gt;.svg</c> (Q44).</summary>
    static string EmblemOf(string icon, string problem) =>
        icon.StartsWith("objects/race-", StringComparison.Ordinal) && icon.EndsWith(".svg", StringComparison.Ordinal) && icon.Length > "objects/race-.svg".Length
            ? icon["objects/race-".Length..^".svg".Length]
            : throw new OldRaceFileException(problem);

    /// <summary>
    /// An effect of a passive as its subtype: its <c>type</c> one of the union's, each other member a field of that
    /// subtype (null as not given), of the field's kind of value; the record made through the serializer's JSON form.
    /// </summary>
    static Effect EffectOf(JsonElement raw, string where)
    {
        if (raw.ValueKind != JsonValueKind.Object || Member(raw, "type") is not { ValueKind: JsonValueKind.String } typeName)
            throw new OldRaceFileException($"{where}: an effect without a type (the converter reads type, not kind)");
        if (!Effect.Types.TryGetValue(typeName.GetString()!, out var type))
            throw new OldRaceFileException($"{where}: an effect type the port doesn't have: {typeName.GetString()}");
        var shape = RecordShape.Of(type);
        var form = new Dictionary<string, object?>();
        foreach (var member in raw.EnumerateObject())
        {
            if (member.Name == "type" || member.Value.ValueKind == JsonValueKind.Null)
                continue;
            var field = shape.Field(member.Name) is { } f && f.JsonName == member.Name ? f : null;
            if (field is null)
                throw new OldRaceFileException($"{where}: {typeName.GetString()} has no field {member.Name}");
            var kind = field.Value.Kind == ValueKind.Nullable ? field.Value.Element!.Kind : field.Value.Kind;
            var fits = kind switch
            {
                ValueKind.Float64 => member.Value.ValueKind == JsonValueKind.Number && member.Value.TryGetDouble(out var n) && double.IsFinite(n),
                ValueKind.String => member.Value.ValueKind == JsonValueKind.String,
                ValueKind.List => member.Value.ValueKind == JsonValueKind.Array && member.Value.EnumerateArray().All(e => e.ValueKind == JsonValueKind.String),
                _ => false,
            };
            if (!fits)
                throw new OldRaceFileException($"{where}: {typeName.GetString()}'s {member.Name} isn't the kind of value the port holds there: {member.Value.GetRawText()}");
            form[member.Name] = member.Value.Clone();
        }
        var name = RecordShape.Of(type).Name;
        var text = JsonSerializer.Serialize(new Dictionary<string, object?> { ["$types"] = new Dictionary<string, int> { [name] = 1 } }
            .Concat(form).ToDictionary(e => e.Key, e => e.Value));
        return (Effect)RecordSerializer.FromJson(type, Encoding.UTF8.GetBytes(text));
    }

    /// <summary>The member <paramref name="name"/> of an object (an array has none of these), or null when it isn't there (JavaScript's undefined).</summary>
    static JsonElement? Member(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var member) ? member : null;

    /// <summary>Whether JavaScript counts the value as true: not undefined, null, false, 0, NaN or "".</summary>
    static bool Truthy(JsonElement? value) => value is { } v && v.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.False or JsonValueKind.Undefined => false,
        JsonValueKind.Number => v.TryGetDouble(out var n) && n != 0 && !double.IsNaN(n),
        JsonValueKind.String => v.GetString()!.Length > 0,
        _ => true,
    };

    /// <summary>A text value as it is; anything else the converter's refusal (the old game took it through JavaScript's String or failed on it).</summary>
    static string Text(JsonElement? value, string problem) =>
        value is { ValueKind: JsonValueKind.String } text ? text.GetString()! : throw new OldRaceFileException(problem);

    /// <summary>The races as their entries' JSON forms (the committed files' bytes), by id.</summary>
    public static IReadOnlyDictionary<string, byte[]> Forms(IEnumerable<RaceEntry> races) =>
        races.ToDictionary(r => r.Id, r => RecordSerializer.ToJsonUtf8(typeof(Race), r.Race), StringComparer.Ordinal);
}
