namespace Statventure.Records;

/// <summary>
/// Data that doesn't fit a record's layout: damaged or truncated input, an unknown key, type or tag, a
/// version newer than the engine reads, or (when writing) a value its declaration doesn't allow, such
/// as a null in a non-nullable field. <see cref="Path"/> says where (<c>$.layers[2].filter.q</c>).
/// </summary>
public sealed class RecordFormatException : Exception
{
    readonly string _reason;
    readonly List<string> _segments = [];

    public RecordFormatException(string reason) : base(reason) => _reason = reason;

    public RecordFormatException(string reason, Exception inner) : base(reason, inner) => _reason = reason;

    /// <summary>What is wrong, without the place.</summary>
    public string Reason => _reason;

    /// <summary>Where in the record: <c>$</c> is the root, then fields, keys and indices.</summary>
    public string Path => "$" + string.Concat(Enumerable.Reverse(_segments));

    public override string Message => _segments.Count == 0 ? _reason : $"{_reason} (at {Path})";

    /// <summary>Adds the enclosing field, key or index while the error travels up.</summary>
    internal RecordFormatException Within(string segment)
    {
        _segments.Add(segment);
        return this;
    }
}
