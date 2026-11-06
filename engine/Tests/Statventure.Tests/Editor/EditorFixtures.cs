using System.Globalization;
using System.Text;
using System.Text.Json;
using Statventure.Editor.Shell;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor;

/// <summary>
/// An editor screen compared with Chromium 152 (M11-20, P36): the live editor at <see cref="Width"/> by
/// <see cref="Height"/> CSS px, put into its state by <see cref="Setup"/>. The test run writes it out as a paint
/// fixture (the editor's stylesheets without the fonts sheet, and the document's markup with its state written
/// as attributes, <see cref="EditorFixtures.Html"/>) into <c>Reference/editor-fixtures.json</c>;
/// <c>Tools/Scripts/make-editor-references.sh</c> screenshots it in Chromium at 1 and 1.65 into
/// <c>Golden/editor-&lt;name&gt;@1.png</c> and <c>@1.65.png</c>, which the engine's paint of the live editor must
/// match at the paint fixtures' limits. <see cref="WindowFocused"/> false draws it with the window unfocused.
/// </summary>
public sealed record EditorFixture(string Name, int Width, int Height, Action<EditorHarness> Setup, bool WindowFocused = true);

/// <summary>
/// A part's editor fixtures, in a file of its own beside its tests (Docs/Editor.md, "How it works", step 4): every
/// class of the test assembly implementing this is found, so no list is shared.
/// </summary>
public interface IEditorFixtures
{
    IEnumerable<EditorFixture> Fixtures { get; }
}

public static class EditorFixtures
{
    /// <summary>The device scales every fixture is compared at.</summary>
    public static readonly float[] Scales = [1, 1.65f];

    /// <summary>Every fixture of the test assembly, by name.</summary>
    public static IReadOnlyList<EditorFixture> All { get; } = Find();

    static List<EditorFixture> Find() =>
        [.. typeof(EditorFixtures).Assembly.GetTypes()
            .Where(t => typeof(IEditorFixtures).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => ((IEditorFixtures)Activator.CreateInstance(t)!).Fixtures)
            .OrderBy(f => f.Name, StringComparer.Ordinal)];

    /// <summary>A scale as the golden's name has it ("1", "1.65").</summary>
    public static string ScaleName(float scale) => scale.ToString(CultureInfo.InvariantCulture);

    /// <summary>The live editor in the fixture's state, at <paramref name="scale"/>.</summary>
    public static EditorHarness Build(EditorFixture fixture, float scale)
    {
        var harness = new EditorHarness(fixture.Width, fixture.Height, scale);
        fixture.Setup(harness);
        if (!fixture.WindowFocused)
            harness.Input.WindowBlur();
        harness.Document.Update(0);
        return harness;
    }

    /// <summary>The fixture's stylesheet: the editor's sheets in the document's order, without the fonts sheet (the harness gives Lato).</summary>
    public static string Css => string.Join("\n", EditorView.Stylesheets.Where(s => s.Name != EditorView.FontsSheet).Select(s => s.Css));

    /// <summary>The fixture list as PaintReference reads it (of <paramref name="fixtures"/>, by default all).</summary>
    public static string ListJson(IEnumerable<EditorFixture>? fixtures = null) => JsonSerializer.Serialize((fixtures ?? All).Select(f =>
    {
        using var harness = Build(f, 1);
        return new { name = f.Name, width = f.Width, height = f.Height, css = Css, html = Html(harness.Document), scrollX = 0, scrollY = 0, windowFocused = f.WindowFocused };
    }).ToList());

    static readonly HashSet<string> VoidTags = ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"];

    /// <summary>
    /// The body's content as markup Chromium shows the same way (P36): the document's state that isn't in its
    /// attributes written as attributes (a field's value as <c>value</c>, a textarea's as its text, checked boxes
    /// and selected options, <c>data-indeterminate</c>, <c>data-focus</c> on the focused element, scroll offsets
    /// as <c>data-scroll-top</c> and <c>data-scroll-left</c>), and the preview's <c>surface</c> as an empty box of its size.
    /// </summary>
    public static string Html(UiDocument document)
    {
        var html = new StringBuilder();
        foreach (var child in document.Body.Children)
            Write(html, child, document);
        return html.ToString();
    }

    static void Write(StringBuilder html, UiNode node, UiDocument document)
    {
        if (node is UiText text)
        {
            html.Append(Escape(text.Text, attribute: false));
            return;
        }
        var element = (UiElement)node;
        var attributes = new List<(string Name, string Value)>(element.Attributes.Select(a => (a.Key, a.Value)));
        void Set(string name, string? value)
        {
            attributes.RemoveAll(a => a.Name == name);
            if (value is not null)
                attributes.Add((name, value));
        }
        var tag = element.Tag;
        if (tag == "surface")
        {
            // The preview's 3D is left out of the picture (P36): an empty box of the surface's size in its place.
            var rect = element.GetBoundingClientRect();
            var display = element.Style?.Display.ToString().ToLowerInvariant() == "block" ? "block" : "inline-block";
            tag = "div";
            Set("style", $"{element.InlineStyle?.TrimEnd(';', ' ')}{(element.InlineStyle is { Length: > 0 } ? "; " : "")}display: {display}; box-sizing: border-box; width: {Number(rect.Width)}px; height: {Number(rect.Height)}px");
        }
        if (element.Tag == "input" && !element.IsCheckable)
            Set("value", element.Value);
        if (element.IsCheckable)
            Set("checked", element.Checked ? "" : null);
        if (element.Tag == "option")
            Set("selected", element.Selected ? "" : null);
        if (element.Indeterminate)
            Set("data-indeterminate", "");
        if (document.FocusedElement == element)
            Set("data-focus", "");
        if (element.ScrollTop != 0)
            Set("data-scroll-top", Number(element.ScrollTop));
        if (element.ScrollLeft != 0)
            Set("data-scroll-left", Number(element.ScrollLeft));
        html.Append('<').Append(tag);
        foreach (var (name, value) in attributes)
        {
            html.Append(' ').Append(name);
            if (value.Length > 0)
                html.Append("=\"").Append(Escape(value, attribute: true)).Append('"');
        }
        html.Append('>');
        if (VoidTags.Contains(tag))
            return;
        if (element.Tag == "textarea")
            html.Append(Escape(element.Value, attribute: false));
        else if (element.Tag != "surface")
            foreach (var child in element.Children)
                Write(html, child, document);
        html.Append("</").Append(tag).Append('>');
    }

    static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    static string Escape(string text, bool attribute)
    {
        var escaped = text.Replace("&", "&amp;").Replace("\u00A0", "&nbsp;");
        return attribute ? escaped.Replace("\"", "&quot;") : escaped.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
