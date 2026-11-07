using System.Numerics;
using Statventure.Editor.Form;
using Statventure.Records;

namespace Statventure.Editor.Import;

/// <summary>
/// One value that differs between an entry and the file imported again over it (P29): where it is, its name as the
/// form shows it (the fields' display names, list elements by index from 0, a map's values by key), the entry's
/// present value and the file's, the shape of the value, and the field it is in (for its colour mark and unit).
/// </summary>
public sealed record ImportDifference(RecordPath Path, string Name, DraftValue Present, DraftValue File, ValueShape Shape, FieldShape Field)
{
    /// <summary>Whether this is a whole value whose structure differs (a list's length, a map's keys, a union's subtype, a nullable set on one side only).</summary>
    public bool IsWhole => Present is not DraftLeaf || File is not DraftLeaf;
}

/// <summary>
/// Importing a file again over an entry of the same kind (M11-14, P29; the manager's decision of 2026-11-07 on how values
/// are matched): the bulk data comes from the file, and the single values that differ between the file and the entry's
/// present values are listed, each kept or taken. Values are matched by path along the kind's shape: bulk fields are the
/// file's and never listed; records are compared field by field; a list whose elements hold bulk data (a model's nodes
/// and animations) has the file's length and is compared element by element by index; any other list, and a map or
/// pairs, is compared element by element when the lengths (a map's keys, in order) are the same, else it is one
/// difference as a whole; a nullable set on one side only, a union of another subtype, or a value given on one side
/// only is one difference; but a whole value that holds bulk data is never listed (its structure is the file's);
/// a plain value (number, text, flag, member, vector, colour) is one difference when it isn't the same, bit for bit.
/// Known limit (recorded with the decision): a node inserted in the file shifts the names after it by index, so each
/// shifted name is listed as differing; "Take all" takes them.
/// </summary>
public sealed class ImportComparison
{
    static readonly Dictionary<RecordShape, bool> BulkHolders = [];
    static readonly object BulkLock = new();

    readonly List<ImportDifference> _differences = [];

    ImportComparison(RecordDraft present, RecordDraft file)
    {
        Present = present;
        File = file;
    }

    /// <summary>The entry's present draft (with its form edits).</summary>
    public RecordDraft Present { get; }

    /// <summary>The file's draft (its bulk data and its values).</summary>
    public RecordDraft File { get; }

    /// <summary>The values that differ, in the kind's field order (depth first).</summary>
    public IReadOnlyList<ImportDifference> Differences => _differences;

    /// <summary>Compares the entry's present draft with the file's (both of one kind).</summary>
    public static ImportComparison Of(RecordDraft present, RecordDraft file)
    {
        if (present.Shape != file.Shape)
            throw new ArgumentException($"a {file.Shape.Name} can't be imported again over a {present.Shape.Name}", nameof(file));
        var comparison = new ImportComparison(present, file);
        comparison.Fields(present, file, RecordPath.Root, "");
        return comparison;
    }

    /// <summary>
    /// The draft the import gives: the file's, with the entry's present value kept at every difference that
    /// <paramref name="take"/> doesn't take (none taken: every difference kept, P29's default).
    /// </summary>
    public RecordDraft Merge(Func<ImportDifference, bool>? take = null)
    {
        var merged = File;
        foreach (var difference in _differences)
            if (take?.Invoke(difference) != true)
                merged = merged.With(difference.Path, difference.Present);
        return merged;
    }

    void Fields(RecordDraft present, RecordDraft file, RecordPath path, string name)
    {
        foreach (var field in present.Shape.Fields)
            if (!field.IsBulk)
                Walk(present[field], file[field], field.Value, field, path.Field(field.JsonName), Join(name, field.DisplayName));
    }

    void Walk(DraftValue present, DraftValue file, ValueShape shape, FieldShape field, RecordPath path, string name)
    {
        if (ReferenceEquals(present, file))
            return;
        if (shape.Kind == ValueKind.Nullable)
        {
            if (Given(present) && Given(file))
                Walk(present, file, shape.Element!, field, path, name);
            else
                Whole(present, file, shape, field, path, name);
            return;
        }
        switch (present, file)
        {
            case (RecordDraft p, RecordDraft f) when p.Shape == f.Shape:
                Fields(p, f, path, name);
                return;
            case (DraftList p, DraftList f):
                if (HoldsBulk(shape.Element!) || p.Count == f.Count)
                {
                    for (var i = 0; i < Math.Min(p.Count, f.Count); i++)
                        Walk(p[i], f[i], shape.Element!, field, path.Index(i), Join(name, i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                else
                    Whole(present, file, shape, field, path, name);
                return;
            case (DraftMap p, DraftMap f):
                if (p.Count == f.Count && p.Entries.Zip(f.Entries).All(e => Same(e.First.Key, e.Second.Key)))
                {
                    for (var i = 0; i < f.Count; i++)
                        Walk(p[i].Value, f[i].Value, shape.Element!, field, KeyPath(path, shape, f[i].Key), Join(name, KeyName(shape, f[i].Key)));
                }
                else
                    Whole(present, file, shape, field, path, name);
                return;
            case (DraftLeaf p, DraftLeaf f):
                if (!Same(p.Value, f.Value))
                    _differences.Add(new(path, name, present, file, shape, field));
                return;
        }
        Whole(present, file, shape, field, path, name);
    }

    /// <summary>A value whose structure differs: one difference, unless it holds bulk data (then the file's, P29).</summary>
    void Whole(DraftValue present, DraftValue file, ValueShape shape, FieldShape field, RecordPath path, string name)
    {
        if (!HoldsBulk(shape))
            _differences.Add(new(path, name, present, file, shape, field));
    }

    static bool Given(DraftValue value) => value != DraftValue.Null && value != DraftValue.Missing;

    static RecordPath KeyPath(RecordPath path, ValueShape map, object key) => path.Key(ValueText.KeyText(map.Key!, key));

    static string KeyName(ValueShape map, object key) => ValueText.KeyText(map.Key!, key);

    static string Join(string name, string part) => name.Length == 0 ? part : name + " › " + part;

    /// <summary>Whether a value of <paramref name="shape"/> holds bulk data anywhere (a field marked bulk in a record it holds).</summary>
    public static bool HoldsBulk(ValueShape shape) => shape.Kind switch
    {
        ValueKind.Nullable or ValueKind.List or ValueKind.Map or ValueKind.Pairs => HoldsBulk(shape.Element!),
        ValueKind.Record or ValueKind.Union => HoldsBulk(shape.Record!),
        _ => false,
    };

    /// <summary>Whether a record of <paramref name="shape"/> holds bulk data anywhere.</summary>
    public static bool HoldsBulk(RecordShape shape)
    {
        lock (BulkLock)
        {
            if (BulkHolders.TryGetValue(shape, out var known))
                return known;
            // A record that holds itself: not bulk on the way round (its other fields decide).
            BulkHolders[shape] = false;
        }
        var holds = shape.IsUnion ? shape.Subtypes.Any(HoldsBulk) : shape.Fields.Any(f => f.IsBulk || HoldsBulk(f.Value));
        lock (BulkLock)
            BulkHolders[shape] = holds;
        return holds;
    }

    /// <summary>Whether two plain values are the same: numbers and vectors bit for bit, byte arrays byte for byte, the rest by equality.</summary>
    public static bool Same(object a, object b) => (a, b) switch
    {
        (float x, float y) => BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y),
        (double x, double y) => BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y),
        (Vector2 x, Vector2 y) => Bits(x.X, y.X) && Bits(x.Y, y.Y),
        (Vector3 x, Vector3 y) => Bits(x.X, y.X) && Bits(x.Y, y.Y) && Bits(x.Z, y.Z),
        (Vector4 x, Vector4 y) => Bits(x.X, y.X) && Bits(x.Y, y.Y) && Bits(x.Z, y.Z) && Bits(x.W, y.W),
        (Quaternion x, Quaternion y) => Bits(x.X, y.X) && Bits(x.Y, y.Y) && Bits(x.Z, y.Z) && Bits(x.W, y.W),
        (Matrix4x4 x, Matrix4x4 y) => Enumerable.Range(0, 16).All(i => Bits(x[i / 4, i % 4], y[i / 4, i % 4])),
        (byte[] x, byte[] y) => x.AsSpan().SequenceEqual(y),
        _ => Equals(a, b),
    };

    static bool Bits(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);
}
