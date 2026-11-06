using System.Globalization;
using Statventure.Packs;

namespace Statventure.Records;

/// <summary>
/// The entries references are checked against (M11-11): the record kind of the entry at a path. The
/// editor gives one over the game's data and the open pack; any other caller its own.
/// </summary>
public interface IReferenceLookup
{
    /// <summary>The record name (<c>audio.effect</c>) of the record entry at <paramref name="path"/>, or null when no record entry is there.</summary>
    string? KindAt(string path);
}

/// <summary>
/// What a record type's own check (<see cref="CheckAttribute"/>) is given besides the value: the data,
/// for checks across entries, and where to report. Paths are relative to the value checked (its
/// <see cref="RecordPath.Root"/> is the value itself, wherever it is held).
/// </summary>
public sealed class CheckContext
{
    readonly List<RecordProblem> _problems;

    internal CheckContext(VirtualFileSystem? data, IReferenceLookup? references, List<RecordProblem> problems)
    {
        Data = data;
        References = references;
        _problems = problems;
    }

    /// <summary>The game's data as the value is checked against it (in the editor, with the open pack's entries as edited), or null when it is checked alone.</summary>
    public VirtualFileSystem? Data { get; }

    /// <summary>The entries references are checked against, or null.</summary>
    public IReferenceLookup? References { get; }

    /// <summary>Where the value checked is, in the record being checked.</summary>
    internal RecordPath At { get; set; } = RecordPath.Root;

    /// <summary>Reports a problem with the whole value.</summary>
    public void Report(string message) => Report(RecordPath.Root, message);

    /// <summary>Reports a problem at <paramref name="path"/> within the value.</summary>
    public void Report(RecordPath path, string message) => _problems.Add(new(At.Append(path), message));

    /// <summary>Reports a problem at <paramref name="path"/> in the record being checked.</summary>
    internal void ReportAt(RecordPath path, string message) => _problems.Add(new(path, message));
}

/// <summary>
/// The checks of a record (P20; Docs/Editor.md, "Records: shapes, drafts and marks"): every record in it,
/// at its path, gets its type's own checks (the unions it is a subtype of first) and its fields' marks'
/// checks: numbers within their <see cref="LimitsAttribute"/>, strings among their
/// <see cref="ChoicesAttribute"/>, and, when a lookup is given, names that point at an entry of their
/// <see cref="ReferencesAttribute"/>'s kind. A draft is checked once it builds (<see cref="RecordDraft.Build"/>
/// reports what keeps it from building).
/// </summary>
public static class RecordChecks
{
    /// <summary>Every problem of <paramref name="record"/>, in the order met: each record's own checks, then its fields' in number order.</summary>
    public static IReadOnlyList<RecordProblem> Check(object record, VirtualFileSystem? data = null, IReferenceLookup? references = null)
    {
        var problems = new List<RecordProblem>();
        Record(record, RecordPath.Root, new CheckContext(data, references, problems));
        return problems;
    }

    static void Record(object record, RecordPath path, CheckContext context)
    {
        var shape = RecordShape.Of(record.GetType());
        foreach (var owner in shape.CheckedBy)
        {
            foreach (var check in owner.Checks)
            {
                context.At = path;
                check(record, context);
            }
        }
        foreach (var field in shape.Fields)
        {
            var value = field.Get(record);
            var at = path.Field(field.JsonName);
            if (field.Limits is { } limits)
                foreach (var (number, where) in Values(field.Value, value, at))
                {
                    var n = Convert.ToDouble(number, CultureInfo.InvariantCulture);
                    if (!limits.Contains(n))
                        context.ReportAt(where, $"must be {limits.Describe()} (it is {Number(number)})");
                }
            if (field.Choices is { } choices)
                foreach (var (text, where) in Values(field.Value, value, at))
                    if (!choices.Contains((string)text, StringComparer.Ordinal))
                        context.ReportAt(where, $"\"{text}\" isn't one of the choices: {string.Join(", ", choices)}");
            if (context.References is { } lookup && field.ReferenceIn(record) is { } reference)
                foreach (var (name, where) in reference.Keys ? Keys(field.Value, value, at) : Values(field.Value, value, at))
                    Reference(context, lookup, reference, (string)name, where);
            if (field.Value.HoldsRecords)
                foreach (var (child, where) in Values(field.Value, value, at))
                    Record(child, where, context);
        }
    }

    static void Reference(CheckContext context, IReferenceLookup lookup, ReferenceShape reference, string name, RecordPath where)
    {
        var kind = Words(reference.Kind.DisplayName);
        if (reference.PathOf(name) is not { } path)
            context.ReportAt(where, $"\"{name}\" can't name {Article(kind)} {kind} in {reference.Folder}");
        else if (lookup.KindAt(path) is not { } found)
            context.ReportAt(where, $"no {kind} \"{name}\" ({path})");
        else if (found != reference.Kind.Name)
            context.ReportAt(where, $"{path} isn't {Article(kind)} {kind}: it holds a {found} record");
    }

    /// <summary>
    /// The values a field holds at the end of its shape, with their paths: itself, through a nullable, each
    /// of a list's elements and each of a map's values (records too: the walk stops at them).
    /// </summary>
    static IEnumerable<(object Value, RecordPath Path)> Values(ValueShape shape, object? value, RecordPath path)
    {
        if (value is null)
            yield break;
        switch (shape.Kind)
        {
            case ValueKind.Nullable:
                foreach (var found in Values(shape.Element!, value, path))
                    yield return found;
                break;
            case ValueKind.List:
                var items = shape.ItemsOf(value);
                for (var i = 0; i < items.Count; i++)
                    foreach (var found in Values(shape.Element!, items[i], path.Index(i)))
                        yield return found;
                break;
            case ValueKind.Map or ValueKind.Pairs:
                foreach (var (key, item) in shape.EntriesOf(value))
                    foreach (var found in Values(shape.Element!, item, path.Key(shape.Key!.TextOf(key))))
                        yield return found;
                break;
            default:
                yield return (value, path);
                break;
        }
    }

    /// <summary>A map's keys (the field itself or through a nullable), each at its entry's path.</summary>
    static IEnumerable<(object Value, RecordPath Path)> Keys(ValueShape shape, object? value, RecordPath path)
    {
        if (shape.Kind == ValueKind.Nullable)
            shape = shape.Element!;
        return value is null ? [] : shape.EntriesOf(value).Select(e => (e.Key, path.Key(shape.Key!.TextOf(e.Key))));
    }

    /// <summary>A number in its shortest round-trip form.</summary>
    static string Number(object number) => Convert.ToString(number, CultureInfo.InvariantCulture)!;

    /// <summary>A display name inside a sentence: its first word in small letters unless it is an acronym ("Sound effect" → "sound effect").</summary>
    internal static string Words(string displayName) =>
        displayName.Length > 1 && !char.IsUpper(displayName[1])
            ? char.ToLowerInvariant(displayName[0]) + displayName[1..]
            : displayName;

    internal static string Article(string words) => "aeiou".Contains(char.ToLowerInvariant(words[0])) ? "an" : "a";
}
