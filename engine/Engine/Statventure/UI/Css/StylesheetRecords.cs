using Statventure.Records;

namespace Statventure.UI.Css;

// A compiled stylesheet (Docs/Assets.md, "UI"; Q23, S7-3): the rule structure as records, everything
// inside it (selectors, media queries, declaration values, descriptors) as CSS tokens. Made by
// Stylesheet.Compile (the UI importer), read back by Stylesheet.FromRecord / Stylesheet.Load, which parse
// the selectors and media queries from their tokens with the engine's own parser and check the rest.

/// <summary>
/// A token's kind in a compiled stylesheet: <see cref="CssTokenType"/> with the token's type flag folded
/// in (CSS Syntax §4: a number's "integer" flag, a hash's "id" flag), so every kind says which fields it has.
/// </summary>
public enum CssTokenKind
{
    Ident = 0,
    Function = 1,
    AtKeyword = 2,
    /// <summary>A hash whose name doesn't start as an identifier (<c>#3a0</c>).</summary>
    Hash = 3,
    /// <summary>A hash whose name starts as an identifier (<c>#menu</c>, CSS Syntax's "id" flag).</summary>
    IdHash = 4,
    String = 5,
    Url = 6,
    /// <summary>A number written with a fraction or an exponent.</summary>
    Number = 7,
    /// <summary>A number written as an integer.</summary>
    Integer = 8,
    Percentage = 9,
    Dimension = 10,
    /// <summary>A dimension whose number is written as an integer.</summary>
    IntegerDimension = 11,
    Whitespace = 12,
    Delim = 13,
    Colon = 14,
    Semicolon = 15,
    Comma = 16,
    LeftBracket = 17,
    RightBracket = 18,
    LeftParen = 19,
    RightParen = 20,
    LeftBrace = 21,
    RightBrace = 22,
}

/// <summary>
/// One CSS token (<c>ui.token</c>): its kind, its text (an identifier's or function's name, a string's
/// value, a delim's character, a whitespace's " "; null when the token has none), its number (numeric
/// kinds only) and its unit (dimensions only).
/// </summary>
[Record("ui.token", 1)]
public sealed record CssTokenRecord(
    [property: Field(1)] CssTokenKind Kind,
    [property: Field(2)] string? Text = null,
    [property: Field(3)] double? Number = null,
    [property: Field(4)] string? Unit = null)
{
    static bool IsNumeric(CssTokenKind kind) => kind is CssTokenKind.Number or CssTokenKind.Integer or CssTokenKind.Percentage or CssTokenKind.Dimension or CssTokenKind.IntegerDimension;

    static bool IsDimension(CssTokenKind kind) => kind is CssTokenKind.Dimension or CssTokenKind.IntegerDimension;

    /// <summary>The record of a token. Every token the tokenizer and the shorthands make has one; anything else is an <see cref="InvalidOperationException"/>.</summary>
    public static CssTokenRecord From(CssToken token)
    {
        var kind = token.Type switch
        {
            CssTokenType.Ident => CssTokenKind.Ident,
            CssTokenType.Function => CssTokenKind.Function,
            CssTokenType.AtKeyword => CssTokenKind.AtKeyword,
            CssTokenType.Hash => token.HashIsId ? CssTokenKind.IdHash : CssTokenKind.Hash,
            CssTokenType.String => CssTokenKind.String,
            CssTokenType.Url => CssTokenKind.Url,
            CssTokenType.Number => token.IsInteger ? CssTokenKind.Integer : CssTokenKind.Number,
            CssTokenType.Percentage => CssTokenKind.Percentage,
            CssTokenType.Dimension => token.IsInteger ? CssTokenKind.IntegerDimension : CssTokenKind.Dimension,
            CssTokenType.Whitespace => CssTokenKind.Whitespace,
            CssTokenType.Delim => CssTokenKind.Delim,
            CssTokenType.Colon => CssTokenKind.Colon,
            CssTokenType.Semicolon => CssTokenKind.Semicolon,
            CssTokenType.Comma => CssTokenKind.Comma,
            CssTokenType.LeftBracket => CssTokenKind.LeftBracket,
            CssTokenType.RightBracket => CssTokenKind.RightBracket,
            CssTokenType.LeftParen => CssTokenKind.LeftParen,
            CssTokenType.RightParen => CssTokenKind.RightParen,
            CssTokenType.LeftBrace => CssTokenKind.LeftBrace,
            CssTokenType.RightBrace => CssTokenKind.RightBrace,
            _ => throw new InvalidOperationException($"a {token.Type} token has no compiled form"),
        };
        var record = new CssTokenRecord(kind, token.Value.Length == 0 ? null : token.Value,
            IsNumeric(kind) ? token.Number : null, token.Unit.Length == 0 ? null : token.Unit);
        CssToken back;
        try
        {
            back = record.ToToken();
        }
        catch (InvalidDataException)
        {
            back = default;
        }
        if (!back.Equals(token))
            throw new InvalidOperationException($"the token {token} ({token.Type}) has no exact compiled form");
        return record;
    }

    /// <summary>The token; an <see cref="InvalidDataException"/> when the fields don't fit the kind.</summary>
    public CssToken ToToken()
    {
        if (!Enum.IsDefined(Kind))
            throw new InvalidDataException($"a token of kind {(int)Kind}");
        if (IsNumeric(Kind) != Number.HasValue)
            throw new InvalidDataException(Number.HasValue ? $"a {Kind} token with a number" : $"a {Kind} token without a number");
        if (Number is { } n && double.IsNaN(n))
            throw new InvalidDataException($"a {Kind} token whose number is NaN");
        if (IsDimension(Kind) != (Unit is { Length: > 0 }))
            throw new InvalidDataException(IsDimension(Kind) ? $"a {Kind} token without a unit" : $"a {Kind} token with a unit");
        var type = Kind switch
        {
            CssTokenKind.Ident => CssTokenType.Ident,
            CssTokenKind.Function => CssTokenType.Function,
            CssTokenKind.AtKeyword => CssTokenType.AtKeyword,
            CssTokenKind.Hash or CssTokenKind.IdHash => CssTokenType.Hash,
            CssTokenKind.String => CssTokenType.String,
            CssTokenKind.Url => CssTokenType.Url,
            CssTokenKind.Number or CssTokenKind.Integer => CssTokenType.Number,
            CssTokenKind.Percentage => CssTokenType.Percentage,
            CssTokenKind.Dimension or CssTokenKind.IntegerDimension => CssTokenType.Dimension,
            CssTokenKind.Whitespace => CssTokenType.Whitespace,
            CssTokenKind.Delim => CssTokenType.Delim,
            CssTokenKind.Colon => CssTokenType.Colon,
            CssTokenKind.Semicolon => CssTokenType.Semicolon,
            CssTokenKind.Comma => CssTokenType.Comma,
            CssTokenKind.LeftBracket => CssTokenType.LeftBracket,
            CssTokenKind.RightBracket => CssTokenType.RightBracket,
            CssTokenKind.LeftParen => CssTokenType.LeftParen,
            CssTokenKind.RightParen => CssTokenType.RightParen,
            CssTokenKind.LeftBrace => CssTokenType.LeftBrace,
            _ => CssTokenType.RightBrace,
        };
        return new CssToken(type, Text ?? "", Number ?? 0, Unit ?? "",
            IsInteger: Kind is CssTokenKind.Integer or CssTokenKind.IntegerDimension, HashIsId: Kind == CssTokenKind.IdHash);
    }

    /// <summary>The records of tokens.</summary>
    public static CssTokenRecord[] From(IEnumerable<CssToken> tokens) => [.. tokens.Select(From)];

    /// <summary>The tokens of records (see <see cref="ToToken"/>).</summary>
    public static List<CssToken> ToTokens(IReadOnlyList<CssTokenRecord> records)
    {
        var tokens = new List<CssToken>(records.Count);
        foreach (var record in records)
            tokens.Add(record.ToToken());
        return tokens;
    }
}

/// <summary>
/// A declaration (<c>ui.declaration</c>). In a style rule it is prepared for the cascade: a longhand
/// (by its canonical name) or a custom property, shorthands expanded, values the engine can't parse
/// dropped; a longhand of a shorthand whose value holds var() has the shorthand's whole value and names
/// the shorthand (expanded once the variables are substituted). In keyframes and font faces it is as
/// written (the style engine prepares keyframes when it uses them).
/// </summary>
[Record("ui.declaration", 1)]
public sealed record DeclarationRecord(
    [property: Field(1)] string Property,
    [property: Field(2)] IReadOnlyList<CssTokenRecord> Value,
    [property: Field(3)] bool Important = false,
    [property: Field(4)] string? Shorthand = null);

/// <summary>A style rule (<c>ui.style-rule</c>): its selector list's tokens, its prepared declarations, and the media query it sits under (none: null).</summary>
[Record("ui.style-rule", 1)]
public sealed record StyleRuleRecord(
    [property: Field(1)] IReadOnlyList<CssTokenRecord> Selectors,
    [property: Field(2)] IReadOnlyList<DeclarationRecord> Declarations,
    [property: Field(3)] IReadOnlyList<CssTokenRecord>? Media = null);

/// <summary>One keyframe (<c>ui.keyframe</c>): its offsets (0 to 1) and its declarations as written.</summary>
[Record("ui.keyframe", 1)]
public sealed record KeyframeRecord(
    [property: Field(1)] IReadOnlyList<float> Offsets,
    [property: Field(2)] IReadOnlyList<DeclarationRecord> Declarations);

/// <summary>A @keyframes rule (<c>ui.keyframes</c>): its name, its keyframes, and the media query it sits under (none: null).</summary>
[Record("ui.keyframes", 1)]
public sealed record KeyframesRecord(
    [property: Field(1)] string Name,
    [property: Field(2)] IReadOnlyList<KeyframeRecord> Frames,
    [property: Field(3)] IReadOnlyList<CssTokenRecord>? Media = null);

/// <summary>A @font-face rule (<c>ui.font-face</c>): its descriptors as written.</summary>
[Record("ui.font-face", 1)]
public sealed record FontFaceRecord(
    [property: Field(1)] IReadOnlyList<DeclarationRecord> Descriptors);

/// <summary>
/// A compiled stylesheet (<c>ui.stylesheet</c>): its style rules in cascade order, its @keyframes and its
/// @font-face rules, each in the order the sheet has them.
/// </summary>
[Record("ui.stylesheet", 1)]
public sealed record StylesheetRecord(
    [property: Field(1)] IReadOnlyList<StyleRuleRecord> Rules,
    [property: Field(2)] IReadOnlyList<KeyframesRecord> Keyframes,
    [property: Field(3)] IReadOnlyList<FontFaceRecord> FontFaces);
