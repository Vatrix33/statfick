using System.Globalization;
using Statventure.Editor.Session;
using Statventure.Records;

namespace Statventure.Editor.References;

/// <summary>
/// The entries references are checked against (M11-11; Docs/Editor.md, "How it works", step 1: the file system can't
/// tell a record entry's kind without loading it, so the lookup is the session's own classification): the kind of the
/// record entry the session shows at a path, the open pack's over the game's; null for a file, an unreadable entry, or
/// nothing there.
/// </summary>
public sealed class SessionReferences(EditorSession session) : IReferenceLookup
{
    public string? KindAt(string path) => session.Entry(path)?.Kind?.Name;
}

/// <summary>A name in an entry's draft that points at another entry (M11-11): where it is, which mark holds for it and the path it names.</summary>
/// <param name="Path">Where the name is in the draft: a string's place, or for a map's key (<see cref="ReferenceShape.Keys"/>) its entry's.</param>
/// <param name="Name">The name as written.</param>
/// <param name="Target">The entry path it names by the mark's rule, or null when the rule can't store it.</param>
/// <param name="Map">For a map's key, the map's path; null for a string value.</param>
public sealed record ReferenceAt(RecordPath Path, ReferenceShape Reference, string Name, string? Target, RecordPath? Map = null);

/// <summary>
/// References in drafts (M11-11, P19): the names a draft holds that its marks say point at entries (each mark's
/// condition read from the draft, so a draft that doesn't build is read too), the entries that hold a name pointing
/// at a path, the names a mark offers (the reference field's list), and a draft with names pointed at another entry.
/// </summary>
public static class EntryReferences
{
    /// <summary>Every reference in <paramref name="draft"/>, in field order (a map's key before its value).</summary>
    public static IReadOnlyList<ReferenceAt> In(RecordDraft draft)
    {
        var found = new List<ReferenceAt>();
        Record(draft, RecordPath.Root, found, empty: false);
        return found;
    }

    /// <summary>
    /// The reference that holds for the text field at <paramref name="path"/> (as a form row's <c>data-path</c> writes
    /// it) in <paramref name="draft"/>: a string value, or with <paramref name="key"/> a map's key; a required string
    /// not given yet too (its name empty). Null when no mark holds there.
    /// </summary>
    public static ReferenceAt? At(RecordDraft draft, string path, bool key)
    {
        var found = new List<ReferenceAt>();
        Record(draft, RecordPath.Root, found, empty: true);
        return found.FirstOrDefault(r => r.Path.ToString() == path && (r.Map is not null) == key);
    }

    /// <summary>
    /// The entries whose drafts name <paramref name="target"/> (P19: the open pack's own and new entries and the mod's copies
    /// of game entries; the game's own entries aren't, a mod can't change them), each with its references to it, by path.
    /// </summary>
    public static IReadOnlyList<(EditorEntry Entry, IReadOnlyList<ReferenceAt> References)> To(EditorSession session, EditorEntry target)
    {
        if (target.Kind is not { } kind)
            return [];
        var referrers = new List<(EditorEntry, IReadOnlyList<ReferenceAt>)>();
        foreach (var entry in session.Entries)
        {
            if (entry.Draft is not { } draft || !IsThePacks(session, entry))
                continue;
            var references = In(draft).Where(r => r.Target == target.Path && r.Reference.Kind.Name == kind.Name).ToList();
            if (references.Count > 0)
                referrers.Add((entry, references));
        }
        return referrers;
    }

    /// <summary>Whether <paramref name="entry"/> is the open pack's (its own, new, or the mod's copy of a game entry): what a referrer update may change.</summary>
    public static bool IsThePacks(EditorSession session, EditorEntry entry) =>
        entry.Source != EntrySource.Game || session.IsModCopy(entry);

    /// <summary>
    /// <paramref name="draft"/> with each of <paramref name="references"/> pointed at <paramref name="path"/> (a string
    /// written again, a map's key replaced in place); those whose mark can't name that path are left. Gives the draft
    /// and how many were left.
    /// </summary>
    public static (RecordDraft Draft, int Left) PointAt(RecordDraft draft, IEnumerable<ReferenceAt> references, string path)
    {
        var left = 0;
        foreach (var reference in references)
        {
            if (reference.Reference.NameOf(path) is not { } name)
            {
                left++;
                continue;
            }
            if (reference.Reference.Keys)
            {
                var map = reference.Map!;
                var node = (DraftMap)draft.At(map);
                var index = node.Entries.ToList().FindIndex(e => e.Key is string key && key == reference.Name);
                draft = draft.With(map, node.With(index, node[index] with { Key = name }));
            }
            else
                draft = draft.With(reference.Path, DraftValue.Of(name));
        }
        return (draft, left);
    }

    /// <summary>
    /// The entries a mark can name (the reference field's list, M11-11): the session's record entries of its kind (the
    /// game's and the pack's) whose path its rule names, each with that name, by name (ordinal).
    /// </summary>
    public static IReadOnlyList<(string Name, EditorEntry Entry)> Candidates(EditorSession session, ReferenceShape reference)
    {
        var found = new List<(string, EditorEntry)>();
        foreach (var entry in session.Entries)
            if (entry.Kind is { } kind && kind.Name == reference.Kind.Name && reference.NameOf(entry.Path) is { } name)
                found.Add((name, entry));
        found.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return found;
    }

    static void Record(RecordDraft draft, RecordPath at, List<ReferenceAt> found, bool empty)
    {
        foreach (var field in draft.Shape.Fields)
        {
            var node = draft.Fields[field.Index];
            var path = at.Field(field.JsonName);
            if (field.References.Count > 0)
            {
                var condition = field.ReferenceCondition is { } when && draft.Fields[when.Index] is DraftLeaf { Value: var value } ? value : null;
                if (field.ReferenceFor(condition) is { } reference)
                    foreach (var (name, where) in reference.Keys ? Keys(node, path) : Strings(field.Value, node, path, empty))
                        found.Add(new(where, reference, name ?? "", name is null ? null : reference.PathOf(name), reference.Keys ? path : null));
            }
            if (field.Value.HoldsRecords)
                foreach (var (child, where) in Records(field.Value, node, path))
                    Record(child, where, found, empty);
        }
    }

    /// <summary>
    /// The strings a value of <paramref name="shape"/> holds (through a nullable, a list's elements, a map's values), at
    /// their paths; with <paramref name="empty"/> also a string not given yet (null).
    /// </summary>
    static IEnumerable<(string?, RecordPath)> Strings(ValueShape shape, DraftValue node, RecordPath path, bool empty)
    {
        switch (shape.Kind)
        {
            case ValueKind.String when node is DraftLeaf { Value: string text }:
                yield return (text, path);
                break;
            case ValueKind.String when empty && node == DraftValue.Missing:
                yield return (null, path);
                break;
            case ValueKind.Nullable:
                foreach (var found in Strings(shape.Element!, node, path, empty))
                    yield return found;
                break;
            case ValueKind.List when node is DraftList list:
                for (var i = 0; i < list.Count; i++)
                    foreach (var found in Strings(shape.Element!, list[i], path.Index(i), empty))
                        yield return found;
                break;
            case ValueKind.Map or ValueKind.Pairs when node is DraftMap map:
                foreach (var entry in map.Entries)
                    foreach (var found in Strings(shape.Element!, entry.Value, path.Key(KeyText(shape.Key!, entry.Key)), empty))
                        yield return found;
                break;
        }
    }

    /// <summary>A map's string keys (the field itself or through a nullable), each at its entry's path.</summary>
    static IEnumerable<(string?, RecordPath)> Keys(DraftValue node, RecordPath path)
    {
        if (node is DraftMap map)
            foreach (var entry in map.Entries)
                yield return ((string)entry.Key, path.Key((string)entry.Key));
    }

    /// <summary>The records a value holds (itself, a union's subtype, through a nullable, a list's elements, a map's values), at their paths.</summary>
    static IEnumerable<(RecordDraft, RecordPath)> Records(ValueShape shape, DraftValue node, RecordPath path)
    {
        switch (shape.Kind)
        {
            case ValueKind.Record or ValueKind.Union when node is RecordDraft record:
                yield return (record, path);
                break;
            case ValueKind.Nullable:
                foreach (var found in Records(shape.Element!, node, path))
                    yield return found;
                break;
            case ValueKind.List when node is DraftList list:
                for (var i = 0; i < list.Count; i++)
                    foreach (var found in Records(shape.Element!, list[i], path.Index(i)))
                        yield return found;
                break;
            case ValueKind.Map or ValueKind.Pairs when node is DraftMap map:
                foreach (var entry in map.Entries)
                    foreach (var found in Records(shape.Element!, entry.Value, path.Key(KeyText(shape.Key!, entry.Key))))
                        yield return found;
                break;
        }
    }

    /// <summary>A map key's text as paths write it (a string as it is, an enum member by its JSON name, an integer in decimal).</summary>
    static string KeyText(ValueShape key, object value) => value switch
    {
        string text => text,
        Enum => key.Members.FirstOrDefault(m => Equals(m.Value, value))?.JsonName ?? Convert.ToString(value, CultureInfo.InvariantCulture)!,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!,
    };
}
