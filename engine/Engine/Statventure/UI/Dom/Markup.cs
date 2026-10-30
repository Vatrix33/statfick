using System.Globalization;
using System.Text;

namespace Statventure.UI.Dom;

/// <summary>
/// Parses HTML fragments into UI nodes (what innerHTML does for the markup the game writes):
/// elements with quoted, unquoted and boolean attributes, void elements, text with character
/// references, comments, and raw text in script/style. Unclosed elements close at their parent's
/// end tag; p and li close when a sibling of the same kind starts, as the HTML parser does.
/// </summary>
public static class Markup
{
    static readonly HashSet<string> VoidElements = ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"];
    static readonly HashSet<string> RawText = ["script", "style", "textarea", "title"];
    static readonly HashSet<string> ClosesParagraph =
    [
        "address", "article", "aside", "blockquote", "details", "dialog", "div", "dl", "fieldset", "figcaption", "figure", "footer", "form",
        "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr", "main", "menu", "nav", "ol", "p", "pre", "section", "table", "ul",
    ];

    static readonly Dictionary<string, string> Entities = new(StringComparer.Ordinal)
    {
        ["amp"] = "&", ["lt"] = "<", ["gt"] = ">", ["quot"] = "\"", ["apos"] = "'", ["nbsp"] = " ", ["times"] = "×", ["divide"] = "÷",
        ["middot"] = "·", ["hellip"] = "…", ["mdash"] = "—", ["ndash"] = "–", ["copy"] = "©", ["reg"] = "®", ["deg"] = "°", ["plusmn"] = "±",
        ["larr"] = "←", ["rarr"] = "→", ["uarr"] = "↑", ["darr"] = "↓", ["harr"] = "↔", ["bull"] = "•", ["lsquo"] = "‘", ["rsquo"] = "’",
        ["ldquo"] = "“", ["rdquo"] = "”", ["laquo"] = "«", ["raquo"] = "»", ["minus"] = "−", ["frac12"] = "½", ["frac14"] = "¼", ["frac34"] = "¾",
        ["sup2"] = "²", ["sup3"] = "³", ["trade"] = "™", ["sect"] = "§", ["para"] = "¶", ["thinsp"] = " ", ["ensp"] = " ", ["emsp"] = " ",
        ["zwj"] = "‍", ["zwnj"] = "‌", ["shy"] = "­", ["star"] = "☆", ["hearts"] = "♥", ["infin"] = "∞", ["prime"] = "′", ["Prime"] = "″",
    };

    /// <summary>Parses markup into nodes (not yet attached anywhere).</summary>
    public static List<UiNode> Parse(string html)
    {
        var root = new UiElement("#fragment");
        var stack = new List<UiElement> { root };
        var text = new StringBuilder();
        var i = 0;
        void FlushText()
        {
            if (text.Length == 0)
                return;
            stack[^1].AppendText(text.ToString());
            text.Clear();
        }
        while (i < html.Length)
        {
            var c = html[i];
            if (c == '<')
            {
                if (string.CompareOrdinal(html, i, "<!--", 0, 4) == 0)
                {
                    var end = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    i = end < 0 ? html.Length : end + 3;
                    continue;
                }
                if (i + 1 < html.Length && html[i + 1] == '!')
                {
                    var end = html.IndexOf('>', i);
                    i = end < 0 ? html.Length : end + 1;
                    continue;
                }
                if (i + 1 < html.Length && html[i + 1] == '/')
                {
                    var end = html.IndexOf('>', i);
                    if (end < 0)
                        break;
                    var name = html[(i + 2)..end].Trim().ToLowerInvariant();
                    FlushText();
                    for (var s = stack.Count - 1; s > 0; s--)
                        if (stack[s].Tag == name)
                        {
                            stack.RemoveRange(s, stack.Count - s);
                            break;
                        }
                    i = end + 1;
                    continue;
                }
                if (i + 1 < html.Length && char.IsLetter(html[i + 1]))
                {
                    FlushText();
                    i = ParseTag(html, i + 1, stack);
                    continue;
                }
            }
            if (c == '&')
            {
                i = Reference(html, i, text);
                continue;
            }
            text.Append(c);
            i++;
        }
        FlushText();
        var nodes = root.Children.ToList();
        foreach (var node in nodes)
            root.RemoveChild(node);
        return nodes;
    }

    /// <summary>Replaces an element's children with parsed markup (innerHTML).</summary>
    public static void SetInnerHtml(UiElement element, string html)
    {
        element.ClearChildren();
        foreach (var node in Parse(html))
            element.Append(node);
    }

    /// <summary>Parses markup and returns its first element.</summary>
    public static UiElement Element(string html) => Parse(html).OfType<UiElement>().First();

    static int ParseTag(string html, int i, List<UiElement> stack)
    {
        var start = i;
        while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] is not ('>' or '/'))
            i++;
        var tag = html[start..i].ToLowerInvariant();
        var element = new UiElement(tag);
        var selfClosing = false;
        while (i < html.Length)
        {
            while (i < html.Length && char.IsWhiteSpace(html[i]))
                i++;
            if (i >= html.Length)
                break;
            if (html[i] == '>')
            {
                i++;
                break;
            }
            if (html[i] == '/')
            {
                selfClosing = true;
                i++;
                continue;
            }
            var nameStart = i;
            while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] is not ('=' or '>' or '/'))
                i++;
            var name = html[nameStart..i].ToLowerInvariant();
            while (i < html.Length && char.IsWhiteSpace(html[i]))
                i++;
            var value = "";
            if (i < html.Length && html[i] == '=')
            {
                i++;
                while (i < html.Length && char.IsWhiteSpace(html[i]))
                    i++;
                var raw = new StringBuilder();
                if (i < html.Length && html[i] is '"' or '\'')
                {
                    var quote = html[i++];
                    while (i < html.Length && html[i] != quote)
                    {
                        if (html[i] == '&')
                            i = Reference(html, i, raw);
                        else
                            raw.Append(html[i++]);
                    }
                    i++;
                }
                else
                {
                    while (i < html.Length && !char.IsWhiteSpace(html[i]) && html[i] != '>')
                    {
                        if (html[i] == '&')
                            i = Reference(html, i, raw);
                        else
                            raw.Append(html[i++]);
                    }
                }
                value = raw.ToString();
            }
            if (name.Length > 0 && !element.HasAttribute(name))
                element.SetAttribute(name, value);
        }

        // Implied end tags: a new p closes an open p; li, dt/dd and option close their open siblings.
        var parent = stack[^1];
        if (ClosesParagraph.Contains(tag))
            CloseIfOpen(stack, "p", stopAt: ["div", "section", "article", "aside", "li", "td", "button", "form", "header", "footer", "main", "nav"]);
        if (tag == "li")
            CloseIfOpen(stack, "li", stopAt: ["ul", "ol", "menu"]);
        if (tag is "dt" or "dd")
        {
            CloseIfOpen(stack, "dt", stopAt: ["dl"]);
            CloseIfOpen(stack, "dd", stopAt: ["dl"]);
        }
        if (tag == "option")
            CloseIfOpen(stack, "option", stopAt: ["select", "datalist", "optgroup"]);
        stack[^1].Append(element);

        if (VoidElements.Contains(tag) || selfClosing)
            return i;
        if (RawText.Contains(tag))
        {
            var close = html.IndexOf("</" + tag, i, StringComparison.OrdinalIgnoreCase);
            var end = close < 0 ? html.Length : close;
            var content = html[i..end];
            if (tag is "textarea" or "title")
            {
                var decoded = new StringBuilder();
                for (var j = 0; j < content.Length;)
                {
                    if (content[j] == '&')
                        j = Reference(content, j, decoded);
                    else
                        decoded.Append(content[j++]);
                }
                content = decoded.ToString();
                if (tag == "textarea" && content.StartsWith('\n'))
                    content = content[1..];
            }
            if (content.Length > 0)
                element.AppendText(content);
            var closeEnd = close < 0 ? html.Length : html.IndexOf('>', close);
            return closeEnd < 0 ? html.Length : closeEnd + 1;
        }
        stack.Add(element);
        return i;
    }

    static void CloseIfOpen(List<UiElement> stack, string tag, string[] stopAt)
    {
        for (var s = stack.Count - 1; s > 0; s--)
        {
            if (stack[s].Tag == tag)
            {
                stack.RemoveRange(s, stack.Count - s);
                return;
            }
            if (stopAt.Contains(stack[s].Tag))
                return;
        }
    }

    /// <summary>A character reference at <paramref name="i"/> ('&amp;...;'), appended decoded; returns the position after it.</summary>
    static int Reference(string html, int i, StringBuilder output)
    {
        var semicolon = html.IndexOf(';', i + 1);
        if (semicolon > i + 1 && semicolon - i <= 12)
        {
            var name = html[(i + 1)..semicolon];
            if (name.StartsWith('#'))
            {
                var hex = name.Length > 1 && name[1] is 'x' or 'X';
                var digits = hex ? name[2..] : name[1..];
                if (int.TryParse(digits, hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) && code is > 0 and <= 0x10FFFF)
                {
                    output.Append(char.ConvertFromUtf32(code));
                    return semicolon + 1;
                }
            }
            else if (Entities.TryGetValue(name, out var value))
            {
                output.Append(value);
                return semicolon + 1;
            }
        }
        output.Append('&');
        return i + 1;
    }

    /// <summary>Serializes nodes back to markup (outerHTML of each).</summary>
    public static string Serialize(UiNode node)
    {
        var builder = new StringBuilder();
        Write(node, builder);
        return builder.ToString();
    }

    static void Write(UiNode node, StringBuilder output)
    {
        switch (node)
        {
            case UiText text:
                foreach (var c in text.Text)
                    output.Append(c switch { '&' => "&amp;", '<' => "&lt;", '>' => "&gt;", ' ' => "&nbsp;", _ => c.ToString() });
                break;
            case UiElement element:
                output.Append('<').Append(element.Tag);
                foreach (var (name, value) in element.Attributes)
                {
                    output.Append(' ').Append(name);
                    if (value.Length > 0)
                        output.Append("=\"").Append(value.Replace("&", "&amp;").Replace("\"", "&quot;")).Append('"');
                }
                output.Append('>');
                if (VoidElements.Contains(element.Tag))
                    break;
                foreach (var child in element.Children)
                    Write(child, output);
                output.Append("</").Append(element.Tag).Append('>');
                break;
        }
    }
}
