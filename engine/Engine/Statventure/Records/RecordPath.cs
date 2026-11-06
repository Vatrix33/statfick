using System.Text;
using System.Text.Json;

namespace Statventure.Records;

/// <summary>What a segment of a <see cref="RecordPath"/> steps into.</summary>
public enum RecordPathKind
{
    /// <summary>A record's field, by its JSON name: <c>.layers</c>.</summary>
    Field,
    /// <summary>A list's element: <c>[2]</c>.</summary>
    Index,
    /// <summary>A map's value, by its key's text as the JSON form writes it: <c>["kick"]</c>.</summary>
    Key,
}

/// <summary>One step of a <see cref="RecordPath"/>: a field's JSON name, a list index, or a map key's text.</summary>
public readonly record struct RecordPathSegment(RecordPathKind Kind, string? Name, int Index)
{
    public override string ToString() => Kind switch
    {
        RecordPathKind.Field => "." + Name,
        RecordPathKind.Index => $"[{Index}]",
        _ => $"[{JsonSerializer.Serialize(Name)}]",
    };
}

/// <summary>
/// A place in a record (P20): <c>$</c> is the record itself (the whole entry), then fields by their JSON
/// names, list indices and map keys, written as <see cref="RecordFormatException.Path"/> writes them
/// (<c>$.layers[0].length</c>, <c>$.drums["kick"]</c>). A union's value adds no step (its fields are
/// the subtype's), nor does a nullable one. Immutable; equal when their text is.
/// </summary>
public sealed class RecordPath : IEquatable<RecordPath>
{
    readonly RecordPath? _parent;
    readonly RecordPathSegment _segment;
    string? _text;

    RecordPath() { }

    RecordPath(RecordPath parent, RecordPathSegment segment)
    {
        _parent = parent;
        _segment = segment;
        Depth = parent.Depth + 1;
    }

    /// <summary>The record itself.</summary>
    public static RecordPath Root { get; } = new();

    public bool IsRoot => _parent == null;

    /// <summary>The number of segments.</summary>
    public int Depth { get; }

    /// <summary>The path's steps from the root.</summary>
    public IReadOnlyList<RecordPathSegment> Segments
    {
        get
        {
            var segments = new RecordPathSegment[Depth];
            for (var path = this; path._parent != null; path = path._parent)
                segments[path.Depth - 1] = path._segment;
            return segments;
        }
    }

    /// <summary>A field of the record here, by its JSON name or its code name (made camelCase, as the layouts make JSON names).</summary>
    public RecordPath Field(string name) => new(this, new(RecordPathKind.Field, JsonNamingPolicy.CamelCase.ConvertName(name), 0));

    /// <summary>An element of the list here.</summary>
    public RecordPath Index(int index) =>
        index >= 0 ? new(this, new(RecordPathKind.Index, null, index)) : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>A value of the map here, by its key's text (a string as it is, an integer in decimal, an enum member by its JSON name).</summary>
    public RecordPath Key(string key) => new(this, new(RecordPathKind.Key, key, 0));

    /// <summary>This path followed by <paramref name="relative"/>'s steps.</summary>
    public RecordPath Append(RecordPath relative)
    {
        var path = this;
        foreach (var segment in relative.Segments)
            path = new RecordPath(path, segment);
        return path;
    }

    public override string ToString()
    {
        if (_text != null)
            return _text;
        var text = new StringBuilder("$");
        foreach (var segment in Segments)
            text.Append(segment);
        return _text = text.ToString();
    }

    public bool Equals(RecordPath? other) => other is not null && ToString() == other.ToString();
    public override bool Equals(object? obj) => obj is RecordPath other && Equals(other);
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.Ordinal);
    public static bool operator ==(RecordPath? a, RecordPath? b) => a is null ? b is null : a.Equals(b);
    public static bool operator !=(RecordPath? a, RecordPath? b) => !(a == b);
}

/// <summary>
/// Something wrong with a record or a draft (P20): where (<see cref="RecordPath.Root"/> for the whole
/// entry) and what, in words.
/// </summary>
public sealed record RecordProblem(RecordPath Path, string Message)
{
    /// <summary>As <see cref="RecordFormatException.Message"/> writes it: the message, then the path unless it is the root.</summary>
    public override string ToString() => Path.IsRoot ? Message : $"{Message} (at {Path})";
}
