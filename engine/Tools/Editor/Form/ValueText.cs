using System.Globalization;
using System.Numerics;
using Statventure.Records;

namespace Statventure.Editor.Form;

/// <summary>
/// Values as the form's fields show and read them (P12): numbers in the invariant form (a point, the shortest text
/// that reads back to the same value: <c>0.1</c> for a float 0.1), integers whole and within their kind, colours as
/// <c>#rrggbb</c> (and <c>aa</c> for a Vector4), the read-only values as text; and the value a new element, a set
/// nullable or a chosen subtype starts with.
/// </summary>
static class ValueText
{
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>A number as its field shows it.</summary>
    public static string Number(object value) => value switch
    {
        float f => f.ToString("R", Invariant),
        double d => d.ToString("R", Invariant),
        _ => Convert.ToString(value, Invariant)!,
    };

    /// <summary>
    /// Reads a number field's text as a value of <paramref name="kind"/> (the CLR type <paramref name="type"/>), or
    /// gives why it can't: not a number, not whole, out of the kind's range, too large for a float.
    /// </summary>
    public static bool TryNumber(string text, ValueKind kind, Type type, out object? value, out string? problem)
    {
        value = null;
        text = text.Trim();
        if (kind is ValueKind.Float32 or ValueKind.Float64)
        {
            if (!double.TryParse(text, NumberStyles.Float, Invariant, out var number) || double.IsNaN(number))
            {
                problem = "isn't a number";
                return false;
            }
            if (kind == ValueKind.Float32)
            {
                var single = (float)number;
                if (float.IsInfinity(single) && !double.IsInfinity(number))
                {
                    problem = "is too large for this field";
                    return false;
                }
                value = single;
            }
            else
                value = number;
            if (double.IsInfinity(number))
            {
                problem = "is too large for this field";
                value = null;
                return false;
            }
            problem = null;
            return true;
        }
        if (!decimal.TryParse(text, NumberStyles.Float, Invariant, out var exact))
        {
            problem = "isn't a number";
            return false;
        }
        if (exact != decimal.Truncate(exact))
        {
            problem = "isn't a whole number";
            return false;
        }
        var (min, max) = IntegerRange(kind);
        if (exact < min || exact > max)
        {
            problem = $"isn't a whole number from {min.ToString(Invariant)} to {max.ToString(Invariant)}";
            return false;
        }
        value = Convert.ChangeType(exact, type, Invariant);
        problem = null;
        return true;
    }

    /// <summary>The range of an integer kind.</summary>
    public static (decimal Min, decimal Max) IntegerRange(ValueKind kind) => kind switch
    {
        ValueKind.Int8 => (sbyte.MinValue, sbyte.MaxValue),
        ValueKind.UInt8 => (byte.MinValue, byte.MaxValue),
        ValueKind.Int16 => (short.MinValue, short.MaxValue),
        ValueKind.UInt16 => (ushort.MinValue, ushort.MaxValue),
        ValueKind.Int32 => (int.MinValue, int.MaxValue),
        ValueKind.UInt32 => (uint.MinValue, uint.MaxValue),
        ValueKind.Int64 => (long.MinValue, long.MaxValue),
        ValueKind.UInt64 => (ulong.MinValue, ulong.MaxValue),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "not an integer kind"),
    };

    /// <summary>A vector's components as floats.</summary>
    public static float[] Components(object vector) => vector switch
    {
        Vector2 v => [v.X, v.Y],
        Vector3 v => [v.X, v.Y, v.Z],
        Vector4 v => [v.X, v.Y, v.Z, v.W],
        _ => throw new ArgumentException($"not a vector: {vector.GetType().Name}", nameof(vector)),
    };

    /// <summary>A vector of <paramref name="kind"/> from its components.</summary>
    public static object Vector(ValueKind kind, float[] c) => kind switch
    {
        ValueKind.Vector2 => new Vector2(c[0], c[1]),
        ValueKind.Vector3 => new Vector3(c[0], c[1], c[2]),
        ValueKind.Vector4 => new Vector4(c[0], c[1], c[2], c[3]),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a vector kind"),
    };

    /// <summary>The number of components of a vector kind.</summary>
    public static int Size(ValueKind kind) => kind switch
    {
        ValueKind.Vector2 => 2,
        ValueKind.Vector3 => 3,
        ValueKind.Vector4 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a vector kind"),
    };

    /// <summary>A colour vector as <c>#rrggbb</c> (<c>#rrggbbaa</c> for four components): each component from 0 to 1 in 255 steps, clamped.</summary>
    public static string Hex(float[] components)
    {
        var text = "#";
        foreach (var c in components)
            text += ((int)MathF.Round(Math.Clamp(float.IsNaN(c) ? 0 : c, 0, 1) * 255, MidpointRounding.AwayFromZero)).ToString("x2", Invariant);
        return text;
    }

    /// <summary>Reads <c>#rrggbb</c> (or <c>#rrggbbaa</c> for <paramref name="size"/> 4, where <c>#rrggbb</c> keeps an alpha of 1) into components.</summary>
    public static bool TryHex(string text, int size, out float[] components)
    {
        components = [];
        text = text.Trim();
        if (text.Length == 0 || text[0] != '#')
            return false;
        var digits = text[1..];
        if (!(digits.Length == 6 || (size == 4 && digits.Length == 8)) || !digits.All(Uri.IsHexDigit))
            return false;
        components = new float[size];
        for (var i = 0; i < size; i++)
            components[i] = i * 2 < digits.Length ? int.Parse(digits.AsSpan(i * 2, 2), NumberStyles.HexNumber, Invariant) / 255f : 1;
        return true;
    }

    /// <summary>A read-only value as text: a quaternion's or matrix's numbers, a byte array's size.</summary>
    public static string ReadOnly(object value) => value switch
    {
        Quaternion q => $"({Number(q.X)}, {Number(q.Y)}, {Number(q.Z)}, {Number(q.W)})",
        Matrix4x4 m => $"({Number(m.M11)}, {Number(m.M12)}, {Number(m.M13)}, {Number(m.M14)}; {Number(m.M21)}, {Number(m.M22)}, {Number(m.M23)}, {Number(m.M24)}; "
            + $"{Number(m.M31)}, {Number(m.M32)}, {Number(m.M33)}, {Number(m.M34)}; {Number(m.M41)}, {Number(m.M42)}, {Number(m.M43)}, {Number(m.M44)})",
        byte[] bytes => bytes.Length == 1 ? "1 byte" : $"{bytes.Length.ToString("N0", Invariant)} bytes",
        _ => Convert.ToString(value, Invariant) ?? "",
    };

    /// <summary>
    /// What a value of <paramref name="shape"/> starts as when the form makes one (a list's new element, a nullable
    /// set, a map's new value): zero, false, an empty string, the first member, an empty list or map, a record's blank
    /// draft, a union's first subtype's blank; a nullable starts as its value's start, not null.
    /// </summary>
    public static DraftValue Start(ValueShape shape) => shape.Kind switch
    {
        ValueKind.Bool => DraftValue.Of(false),
        ValueKind.String => DraftValue.Of(""),
        ValueKind.Enum => DraftValue.Of(shape.Members[0].Value),
        ValueKind.Bytes => DraftValue.Of(Array.Empty<byte>()),
        ValueKind.Quaternion => DraftValue.Of(Quaternion.Identity),
        ValueKind.Matrix4x4 => DraftValue.Of(Matrix4x4.Identity),
        ValueKind.Nullable => Start(shape.Element!),
        ValueKind.List => DraftList.Empty,
        ValueKind.Map or ValueKind.Pairs => DraftMap.Empty,
        ValueKind.Record => RecordDraft.Blank(shape.Record!),
        ValueKind.Union => RecordDraft.Blank(shape.Record!.Subtypes[0]),
        _ => DraftValue.Of(Activator.CreateInstance(shape.Type)!),
    };

    /// <summary>A map key's text as paths and the JSON form write it: a string as it is, an integer in decimal, an enum member by its JSON name.</summary>
    public static string KeyText(ValueShape key, object value)
    {
        if (key.Kind == ValueKind.String)
            return (string)value;
        if (key.Kind == ValueKind.Enum)
            return key.Members.FirstOrDefault(m => m.Value.Equals(value))?.JsonName
                ?? Convert.ToString(Convert.ChangeType(value, Enum.GetUnderlyingType(key.Type), Invariant), Invariant)!;
        return Convert.ToString(value, Invariant)!;
    }

    /// <summary>A key for a map's new entry that none of <paramref name="taken"/> is: "" (then "new", "new 2" …) for strings, the next integer, the first member not taken.</summary>
    public static object NewKey(ValueShape key, IReadOnlyList<object> taken)
    {
        switch (key.Kind)
        {
            case ValueKind.String:
                if (!taken.Contains(""))
                    return "";
                for (var n = 1; ; n++)
                {
                    var name = n == 1 ? "new" : $"new {n}";
                    if (!taken.Contains(name))
                        return name;
                }
            case ValueKind.Enum:
                return key.Members.FirstOrDefault(m => !taken.Contains(m.Value))?.Value ?? key.Members[0].Value;
            default:
            {
                var next = taken.Count == 0 ? 0m : taken.Max(k => Convert.ToDecimal(k, Invariant)) + 1;
                var (_, max) = IntegerRange(key.Kind);
                return Convert.ChangeType(next > max ? 0 : next, key.Type, Invariant);
            }
        }
    }
}
