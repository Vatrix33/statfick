using System.Globalization;
using System.Text;

namespace Statventure.UI.Css;

public enum CssTokenType
{
    Ident,
    Function,
    AtKeyword,
    Hash,
    String,
    Url,
    Number,
    Percentage,
    Dimension,
    Whitespace,
    Delim,
    Colon,
    Semicolon,
    Comma,
    LeftBracket,
    RightBracket,
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    EndOfFile,
}

/// <summary>A CSS token (CSS Syntax Level 3). Value holds the name/text; Number the numeric value; Unit a dimension's unit.</summary>
public readonly record struct CssToken(CssTokenType Type, string Value = "", double Number = 0, string Unit = "", bool IsInteger = false, bool HashIsId = false)
{
    public static readonly CssToken Eof = new(CssTokenType.EndOfFile);
    public static readonly CssToken Space = new(CssTokenType.Whitespace, " ");

    public bool IsDelim(char c) => Type == CssTokenType.Delim && Value.Length == 1 && Value[0] == c;
    public bool IsIdent(string name) => Type == CssTokenType.Ident && string.Equals(Value, name, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Type switch
    {
        CssTokenType.Ident => Value,
        CssTokenType.Function => Value + "(",
        CssTokenType.AtKeyword => "@" + Value,
        CssTokenType.Hash => "#" + Value,
        CssTokenType.String => "\"" + Value.Replace("\"", "\\\"") + "\"",
        CssTokenType.Url => "url(" + Value + ")",
        CssTokenType.Number => Number.ToString(CultureInfo.InvariantCulture),
        CssTokenType.Percentage => Number.ToString(CultureInfo.InvariantCulture) + "%",
        CssTokenType.Dimension => Number.ToString(CultureInfo.InvariantCulture) + Unit,
        CssTokenType.Whitespace => " ",
        CssTokenType.Colon => ":",
        CssTokenType.Semicolon => ";",
        CssTokenType.Comma => ",",
        CssTokenType.LeftBracket => "[",
        CssTokenType.RightBracket => "]",
        CssTokenType.LeftParen => "(",
        CssTokenType.RightParen => ")",
        CssTokenType.LeftBrace => "{",
        CssTokenType.RightBrace => "}",
        CssTokenType.EndOfFile => "",
        _ => Value,
    };
}

/// <summary>Turns CSS text into tokens (CSS Syntax Level 3 §4), comments dropped.</summary>
public sealed class CssTokenizer
{
    readonly string _text;
    int _at;

    public CssTokenizer(string text)
    {
        _text = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\f', '\n');
    }

    public static List<CssToken> Tokenize(string text)
    {
        var tokenizer = new CssTokenizer(text);
        var tokens = new List<CssToken>();
        while (true)
        {
            var token = tokenizer.Next();
            if (token.Type == CssTokenType.EndOfFile)
                break;
            tokens.Add(token);
        }
        return tokens;
    }

    char Peek(int offset = 0) => _at + offset < _text.Length ? _text[_at + offset] : '\0';

    static bool IsNameStart(char c) => char.IsLetter(c) || c == '_' || c >= 0x80;
    static bool IsName(char c) => IsNameStart(c) || char.IsDigit(c) || c == '-';
    static bool IsHex(char c) => char.IsAsciiHexDigit(c);

    bool ValidEscape(int offset = 0) => Peek(offset) == '\\' && Peek(offset + 1) != '\n' && Peek(offset + 1) != '\0';

    bool StartsIdentifier(int offset = 0)
    {
        var c = Peek(offset);
        if (c == '-')
            return IsNameStart(Peek(offset + 1)) || Peek(offset + 1) == '-' || ValidEscape(offset + 1);
        return IsNameStart(c) || ValidEscape(offset);
    }

    bool StartsNumber(int offset = 0)
    {
        var c = Peek(offset);
        if (c is '+' or '-')
            return char.IsDigit(Peek(offset + 1)) || (Peek(offset + 1) == '.' && char.IsDigit(Peek(offset + 2)));
        if (c == '.')
            return char.IsDigit(Peek(offset + 1));
        return char.IsDigit(c);
    }

    public CssToken Next()
    {
        // Comments.
        while (Peek() == '/' && Peek(1) == '*')
        {
            var end = _text.IndexOf("*/", _at + 2, StringComparison.Ordinal);
            _at = end < 0 ? _text.Length : end + 2;
        }
        if (_at >= _text.Length)
            return CssToken.Eof;
        var c = _text[_at];
        if (char.IsWhiteSpace(c))
        {
            while (_at < _text.Length && char.IsWhiteSpace(_text[_at]))
                _at++;
            return CssToken.Space;
        }
        if (c is '"' or '\'')
        {
            _at++;
            return new CssToken(CssTokenType.String, ConsumeString(c));
        }
        if (c == '#')
        {
            if (IsName(Peek(1)) || ValidEscape(1))
            {
                _at++;
                var isId = StartsIdentifier();
                return new CssToken(CssTokenType.Hash, ConsumeName(), HashIsId: isId);
            }
            _at++;
            return new CssToken(CssTokenType.Delim, "#");
        }
        if (StartsNumber())
            return ConsumeNumeric();
        if (c == '-' && Peek(1) == '-' && Peek(2) == '>')
        {
            _at += 3;
            return Next();
        }
        if (StartsIdentifier())
            return ConsumeIdentLike();
        if (c == '<' && Peek(1) == '!' && Peek(2) == '-' && Peek(3) == '-')
        {
            _at += 4;
            return Next();
        }
        if (c == '@' && StartsIdentifier(1))
        {
            _at++;
            return new CssToken(CssTokenType.AtKeyword, ConsumeName());
        }
        _at++;
        return c switch
        {
            '(' => new CssToken(CssTokenType.LeftParen),
            ')' => new CssToken(CssTokenType.RightParen),
            '[' => new CssToken(CssTokenType.LeftBracket),
            ']' => new CssToken(CssTokenType.RightBracket),
            '{' => new CssToken(CssTokenType.LeftBrace),
            '}' => new CssToken(CssTokenType.RightBrace),
            ':' => new CssToken(CssTokenType.Colon),
            ';' => new CssToken(CssTokenType.Semicolon),
            ',' => new CssToken(CssTokenType.Comma),
            _ => new CssToken(CssTokenType.Delim, c.ToString()),
        };
    }

    string ConsumeString(char quote)
    {
        var value = new StringBuilder();
        while (_at < _text.Length)
        {
            var c = _text[_at++];
            if (c == quote)
                break;
            if (c == '\n')
                break;
            if (c == '\\')
            {
                if (_at >= _text.Length)
                    break;
                if (_text[_at] == '\n')
                {
                    _at++;
                    continue;
                }
                value.Append(ConsumeEscape());
                continue;
            }
            value.Append(c);
        }
        return value.ToString();
    }

    string ConsumeEscape()
    {
        var c = _text[_at++];
        if (!IsHex(c))
            return c.ToString();
        var hex = new StringBuilder().Append(c);
        while (hex.Length < 6 && IsHex(Peek()))
            hex.Append(_text[_at++]);
        if (char.IsWhiteSpace(Peek()))
            _at++;
        var code = int.Parse(hex.ToString(), NumberStyles.HexNumber);
        return code == 0 || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF) ? "�" : char.ConvertFromUtf32(code);
    }

    string ConsumeName()
    {
        var name = new StringBuilder();
        while (_at < _text.Length)
        {
            var c = _text[_at];
            if (IsName(c))
            {
                name.Append(c);
                _at++;
            }
            else if (ValidEscape())
            {
                _at++;
                name.Append(ConsumeEscape());
            }
            else
                break;
        }
        return name.ToString();
    }

    CssToken ConsumeNumeric()
    {
        var start = _at;
        var integer = true;
        if (Peek() is '+' or '-')
            _at++;
        while (char.IsDigit(Peek()))
            _at++;
        if (Peek() == '.' && char.IsDigit(Peek(1)))
        {
            integer = false;
            _at++;
            while (char.IsDigit(Peek()))
                _at++;
        }
        if ((Peek() is 'e' or 'E') && (char.IsDigit(Peek(1)) || ((Peek(1) is '+' or '-') && char.IsDigit(Peek(2)))))
        {
            integer = false;
            _at += 2;
            while (char.IsDigit(Peek()))
                _at++;
        }
        var number = double.Parse(_text.AsSpan(start, _at - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        if (StartsIdentifier())
            return new CssToken(CssTokenType.Dimension, Number: number, Unit: ConsumeName().ToLowerInvariant(), IsInteger: integer);
        if (Peek() == '%')
        {
            _at++;
            return new CssToken(CssTokenType.Percentage, Number: number);
        }
        return new CssToken(CssTokenType.Number, Number: number, IsInteger: integer);
    }

    CssToken ConsumeIdentLike()
    {
        var name = ConsumeName();
        if (Peek() == '(')
        {
            _at++;
            if (name.Equals("url", StringComparison.OrdinalIgnoreCase))
            {
                var save = _at;
                while (char.IsWhiteSpace(Peek()))
                    _at++;
                if (Peek() is '"' or '\'')
                {
                    _at = save;
                    return new CssToken(CssTokenType.Function, "url");
                }
                var url = new StringBuilder();
                while (_at < _text.Length && Peek() != ')')
                {
                    if (ValidEscape())
                    {
                        _at++;
                        url.Append(ConsumeEscape());
                    }
                    else
                        url.Append(_text[_at++]);
                }
                if (Peek() == ')')
                    _at++;
                return new CssToken(CssTokenType.Url, url.ToString().Trim());
            }
            return new CssToken(CssTokenType.Function, name.ToLowerInvariant());
        }
        return new CssToken(CssTokenType.Ident, name);
    }
}
