using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The input fixtures played into the engine see what Chromium 152 saw when the same input was
/// played into it as real input (Reference/input-results.json, from
/// Tools/Scripts/make-input-references.sh): the same events in the same order, and the same
/// values, checkedness, open details, focus and matching pseudo-classes afterwards.
/// </summary>
public class InputReferenceTests
{
    const float StepSeconds = 0.04f;

    static readonly string[] LoggedTypes =
    [
        "pointerdown", "pointerup", "mousedown", "mouseup", "click", "dblclick", "auxclick", "contextmenu",
        "keydown", "keypress", "keyup", "beforeinput", "input", "change", "submit", "reset", "invalid", "toggle", "search",
        "focus", "blur", "focusin", "focusout", "select", "selectionchange", "copy", "cut", "paste", "scroll", "scrollend", "wheel",
        "gotpointercapture", "lostpointercapture",
    ];

    /// <summary>Logged only when a fixture lists them.</summary>
    static readonly string[] OptInTypes =
    [
        "animationstart", "animationiteration", "animationend", "animationcancel",
        "transitionrun", "transitionstart", "transitionend", "transitioncancel",
    ];

    static readonly string[] Pseudo =
    [
        ":checked", ":indeterminate", ":hover", ":active", ":focus", ":focus-visible", ":valid", ":invalid", ":user-valid",
        ":user-invalid", ":placeholder-shown", ":open",
    ];

    /// <summary>What the engine doesn't model yet (compared once it does).</summary>
    static readonly HashSet<string> NotYetCompared = [];
    static readonly HashSet<string> NotYetComparedEvents = [];

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    public static TheoryData<string> Fixtures => new(InputFixtures.All.Select(f => f.Name));

    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
    };

    [Fact]
    public void TheFixtureFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "input-fixtures.json");
        var fixtures = InputFixtures.All.Select(f => new { f.Name, f.Width, f.Height, Css = InputFixtures.BaseCss + f.Css, f.Html, f.Steps });
        var text = JsonSerializer.Serialize(fixtures, Json) + "\n";
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.NotEmpty(InputFixtures.All);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheEngineSeesWhatChromiumSaw(string name)
    {
        var fixture = InputFixtures.All.Single(f => f.Name == name);
        var resultsPath = Path.Combine(ReferenceDirectory, "input-results.json");
        Assert.True(File.Exists(resultsPath), "no Chromium results: run Tools/Scripts/make-input-references.sh");
        using var results = JsonDocument.Parse(File.ReadAllText(resultsPath));
        Assert.True(results.RootElement.TryGetProperty(name, out var expected), $"no Chromium result for '{name}': run Tools/Scripts/make-input-references.sh");
        Check(fixture, expected, 1);
    }

    /// <summary>Plays the fixture at a device scale (in the viewport Chromium had, when its result says) and compares what the engine saw with Chromium's result.</summary>
    internal static void Check(InputFixture fixture, JsonElement expected, float deviceScale)
    {
        Vector2? viewport = expected.TryGetProperty("viewport", out var size) ? new Vector2(size[0].GetSingle(), size[1].GetSingle()) : null;
        var (log, states, active, hasFocus, snapshots, pageSelection) = Play(fixture, deviceScale: deviceScale, viewport: viewport);

        var expectedLog = expected.GetProperty("log").EnumerateArray().Select(e => e.GetString()!).Where(e => !NotYetComparedEvents.Contains(e.Split(' ')[0])).ToList();
        log.RemoveAll(e => NotYetComparedEvents.Contains(e.Split(' ')[0]));
        if (!expectedLog.SequenceEqual(log))
            Assert.Fail("event log differs:\n" + Diff(expectedLog, log));
        var problems = new List<string>();
        CompareStates(expected, states, active, "", problems);
        ComparePageSelection(expected, pageSelection, "", problems);
        var expectedSnapshots = expected.GetProperty("snapshots").EnumerateArray().ToList();
        if (expectedSnapshots.Count != snapshots.Count)
            problems.Add($"snapshots: Chromium {expectedSnapshots.Count}, engine {snapshots.Count}");
        for (var i = 0; i < Math.Min(expectedSnapshots.Count, snapshots.Count); i++)
        {
            CompareStates(expectedSnapshots[i], snapshots[i].States, snapshots[i].Active, $"snapshot {i + 1}: ", problems);
            ComparePageSelection(expectedSnapshots[i], snapshots[i].PageSelection, $"snapshot {i + 1}: ", problems);
        }
        if (expected.GetProperty("hasFocus").GetBoolean() != hasFocus)
            problems.Add($"hasFocus: Chromium {expected.GetProperty("hasFocus").GetBoolean()}, engine {hasFocus}");
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    /// <summary>The page selection as text: "null", "field", or anchor | focus | its text.</summary>
    static string PageSelectionOf(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => "null",
        JsonValueKind.String => element.GetString()!,
        _ => $"{element.GetProperty("anchor")} | {element.GetProperty("focus")} | {Quote(element.GetProperty("text").GetString()!)}",
    };

    static void ComparePageSelection(JsonElement expected, string actual, string prefix, List<string> problems)
    {
        if (!expected.TryGetProperty("pageSelection", out var selection))
            return;
        var want = PageSelectionOf(selection);
        if (want != actual)
            problems.Add($"{prefix}page selection: Chromium {want}, engine {actual}");
    }

    static string PageSelectionOf(UiDocument document)
    {
        if (document.FocusedElement is { IsTextField: true })
            return "field";
        // A field that isn't focused but holds the selection shows as a caret where it stands.
        if (document.SelectionHolder is { Parent: { } holderParent } holder)
        {
            var at = $"{Id(holderParent)}:{holderParent.Children.ToList().IndexOf(holder)}";
            return $"{at} | {at} | {Quote("")}";
        }
        if (document.PageSelection is not { } selection)
            return "null";
        static string Describe(DomPosition position)
        {
            if (position.Node is UiText text && text.Parent is { } parent)
            {
                var index = 0;
                for (; index < parent.Children.Count && parent.Children[index] != text; index++)
                {
                }
                return $"{Id(parent)}/{index}:{position.Offset}";
            }
            return $"{Id(position.Node as UiElement)}:{position.Offset}";
        }
        return $"{Describe(selection.Anchor)} | {Describe(selection.Focus)} | {Quote(document.PageSelectionText())}";
    }

    static void CompareStates(JsonElement expected, Dictionary<string, Dictionary<string, string>> states, string? active, string prefix, List<string> problems)
    {
        foreach (var element in expected.GetProperty("states").EnumerateObject())
        {
            if (!states.TryGetValue(element.Name, out var actual))
            {
                problems.Add($"{prefix}{element.Name}: missing");
                continue;
            }
            foreach (var property in element.Value.EnumerateObject())
            {
                if (NotYetCompared.Contains(property.Name))
                    continue;
                var want = property.Value.ValueKind == JsonValueKind.Array ? JsonSerializer.Serialize(property.Value) : property.Value.ToString();
                var got = actual.TryGetValue(property.Name, out var value) ? value : "(none)";
                if (want != got)
                    problems.Add($"{prefix}{element.Name}.{property.Name}: Chromium {want}, engine {got}");
            }
        }
        var expectedActive = expected.GetProperty("activeElement").ValueKind == JsonValueKind.Null ? null : expected.GetProperty("activeElement").GetString();
        if (expectedActive != active)
            problems.Add($"{prefix}activeElement: Chromium {expectedActive ?? "null"}, engine {active ?? "null"}");
    }

    /// <summary>
    /// Plays the fixture's steps into a document on the steps' clock, logging what the page sees (and
    /// showing it at its screenshot steps). The steps' positions are window units; the viewport is the
    /// fixture's size unless given.
    /// </summary>
    public static (List<string> Log, Dictionary<string, Dictionary<string, string>> States, string? Active, bool HasFocus, List<(Dictionary<string, Dictionary<string, string>> States, string? Active, string PageSelection)> Snapshots, string PageSelection) Play(InputFixture fixture, Action<string, UiDocument>? screenshot = null, float deviceScale = 1, Vector2? viewport = null)
    {
        var document = new UiDocument(TestFonts.Library);
        if (deviceScale != 1)
            document.SetZoom(deviceScale, 1);
        document.SetViewport(viewport?.X ?? fixture.Width, viewport?.Y ?? fixture.Height);
        document.AddStylesheet(InputFixtures.BaseCss + fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        double time = 0;
        document.Update(time);
        var log = new List<string>();
        var logged = document.QuerySelector("[data-log]")?.GetAttribute("data-log") is { } only ? only.Split(' ').ToHashSet() : LoggedTypes.ToHashSet();
        foreach (var type in LoggedTypes.Concat(OptInTypes))
            document.AddEventListener(type, e =>
            {
                // The window's focus and blur (the engine's document stands for the window) aren't the document's in Chromium.
                if (logged.Contains(type) && !(e.Target is null && type is "focus" or "blur"))
                    log.Add(Describe(e));
            }, capture: true);
        foreach (var element in document.AllElements().ToList())
            if (element.GetAttribute("data-prevent") is { } prevent)
                foreach (var type in prevent.Split(' '))
                    element.AddEventListener(type, e => e.PreventDefault());
        time += 0.1;
        document.Update(time);
        var snapshots = new List<(Dictionary<string, Dictionary<string, string>>, string?, string)>();
        foreach (var step in fixture.Steps)
        {
            if (step.Kind == "snapshot")
                snapshots.Add((Collect(document), ActiveOf(document), PageSelectionOf(document)));
            else if (step.Kind == "screenshot")
                screenshot?.Invoke(step.Name!, document);
            else
                PlayStep(document, step);
            // The game's loop runs frames at 60 Hz: time passes frame by frame.
            var end = time + (step.Kind == "wait" ? step.Ms / 1000.0 : StepSeconds);
            while (time < end - 1e-9)
            {
                time = Math.Min(end, time + 1 / 60.0);
                document.Update(time);
            }
        }
        time += 0.1;
        document.Update(time);
        return (log, Collect(document), ActiveOf(document), document.WindowFocused, snapshots, PageSelectionOf(document));
    }

    static string? ActiveOf(UiDocument document) => Id(document.FocusedElement is { } f && f != document.Body ? f : null);

    static void PlayStep(UiDocument document, InputStep step)
    {
        var input = document.Input;
        var modifiers = KeyModifiers.None;
        foreach (var m in step.Modifiers ?? [])
            modifiers |= m switch
            {
                "Shift" => KeyModifiers.Shift,
                "Control" => KeyModifiers.Control,
                "Alt" => KeyModifiers.Alt,
                _ => KeyModifiers.Super,
            };
        var button = step.Button switch
        {
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            _ => MouseButton.Left,
        };
        UiElement Target() => document.QuerySelector(step.Target!) ?? throw new InvalidOperationException("no element " + step.Target);
        switch (step.Kind)
        {
            case "move":
                input.PointerMove(new Vector2(step.X, step.Y), modifiers);
                break;
            case "down":
                input.PointerDown(new Vector2(step.X, step.Y), button, modifiers);
                break;
            case "up":
                input.PointerUp(new Vector2(step.X, step.Y), button, modifiers);
                break;
            case "wheel":
                input.Wheel(new Vector2(step.X, step.Y), new Vector2(step.Dx, step.Dy), modifiers, precise: false);
                break;
            case "keydown":
                input.KeyDown(KeyValue(step.Key!, modifiers), CodeOf(step.Key!), modifiers);
                break;
            case "keyup":
                input.KeyUp(KeyValue(step.Key!, modifiers), CodeOf(step.Key!), modifiers);
                break;
            case "wait":
                break;
            case "windowBlur":
                input.WindowBlur();
                break;
            case "windowFocus":
                input.WindowFocus();
                break;
            case "focus":
                Target().Focus();
                break;
            case "blur":
                Target().Blur();
                break;
            case "click":
                Target().Click();
                break;
            case "reset":
                Target().Reset();
                break;
            case "requestSubmit":
                Target().RequestSubmit();
                break;
            case "stepUp":
                Target().StepUp();
                break;
            case "stepDown":
                Target().StepDown();
                break;
            case "set":
            {
                var element = Target();
                switch (step.Property)
                {
                    case "value":
                        element.Value = step.Value!;
                        break;
                    case "checked":
                        element.Checked = step.Value == "true";
                        break;
                    case "indeterminate":
                        element.Indeterminate = step.Value == "true";
                        break;
                    case "open":
                        element.Open = step.Value == "true";
                        break;
                    case "disabled" or "required" or "inert":
                        element.ToggleAttribute(step.Property, step.Value == "true");
                        break;
                    case "readOnly":
                        element.ReadOnly = step.Value == "true";
                        break;
                    case "innerHTML":
                        Markup.SetInnerHtml(element, step.Value!);
                        break;
                    default:
                        throw new InvalidOperationException("can't set " + step.Property);
                }
                break;
            }
            default:
                throw new InvalidOperationException("unknown step " + step.Kind);
        }
    }

    /// <summary>The key value a keyboard gives (a letter with Shift is a capital, as the window host maps it).</summary>
    static string KeyValue(string key, KeyModifiers modifiers) =>
        key.Length == 1 && char.IsAsciiLetterLower(key[0]) && modifiers.HasFlag(KeyModifiers.Shift) ? key.ToUpperInvariant() : key;

    /// <summary>The physical key for a DOM key value (as a US keyboard).</summary>
    static string CodeOf(string key) => key switch
    {
        " " => "Space",
        "Shift" => "ShiftLeft",
        "Control" => "ControlLeft",
        "Alt" => "AltLeft",
        "Meta" => "MetaLeft",
        _ when key.Length == 1 && char.IsAsciiLetter(key[0]) => "Key" + char.ToUpperInvariant(key[0]),
        _ when key.Length == 1 && char.IsAsciiDigit(key[0]) => "Digit" + key,
        _ => key,
    };

    static string? Id(UiElement? element) => element is null ? null : element.Id is { Length: > 0 } id ? "#" + id : element.Tag;

    /// <summary>A logged event, exactly as the browser side writes it.</summary>
    static string Describe(UiEvent e)
    {
        var line = new StringBuilder(e.Type).Append(' ').Append(e.Target is { } target ? Id(target) : "document");
        if (!e.IsTrusted)
            line.Append(" untrusted");
        if (e is UiKeyboardEvent key)
            line.Append(" key=").Append(Quote(key.Key));
        if (e is UiMouseEvent mouse && e.Type is "click" or "mousedown" or "mouseup" or "dblclick")
            line.Append(" detail=").Append(mouse.Detail);
        if (e is UiInputEvent input)
        {
            line.Append(" inputType=").Append(input.InputType);
            if (input.Data is { } data)
                line.Append(" data=").Append(Quote(data));
        }
        if (e is UiToggleEvent toggle)
            line.Append(' ').Append(toggle.OldState).Append("->").Append(toggle.NewState);
        // Elapsed times to a tenth (cancel times depend on the frames).
        static string Tenths(double seconds) => (Math.Round(seconds * 10, MidpointRounding.AwayFromZero) / 10).ToString(CultureInfo.InvariantCulture);
        if (e is UiAnimationEvent animation)
            line.Append(" name=").Append(animation.AnimationName).Append(" elapsed=").Append(Tenths(animation.ElapsedTime))
                .Append(animation.PseudoElement.Length > 0 ? " pseudo=" + animation.PseudoElement : "");
        if (e is UiTransitionEvent transition)
            line.Append(" property=").Append(transition.PropertyName).Append(" elapsed=").Append(Tenths(transition.ElapsedTime))
                .Append(transition.PseudoElement.Length > 0 ? " pseudo=" + transition.PseudoElement : "");
        if (e is UiSubmitEvent submit)
            line.Append(" submitter=").Append(Id(submit.Submitter) ?? "null");
        if (e.Type is "input" or "change" && e.Target is { Tag: "input" or "textarea" or "select" or "button" } control)
            line.Append(" value=").Append(Quote(control.Value));
        if (e.Type is "input" or "change" or "click" && e.Target is { IsCheckable: true } checkable)
            line.Append(" checked=").Append(checkable.Checked ? "true" : "false");
        if (e.DefaultPrevented)
            line.Append(" prevented");
        return line.ToString();
    }

    /// <summary>JSON.stringify of a string.</summary>
    static string Quote(string text)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in text)
            quoted.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                '\b' => "\\b",
                '\f' => "\\f",
                < ' ' => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture),
                _ => c.ToString(),
            });
        return quoted.Append('"').ToString();
    }

    static Dictionary<string, Dictionary<string, string>> Collect(UiDocument document)
    {
        var states = new Dictionary<string, Dictionary<string, string>>();
        foreach (var element in document.AllElements())
        {
            if (element.Id is not { Length: > 0 } id)
                continue;
            var s = new Dictionary<string, string>();
            if (element.Tag is "input" or "textarea" or "select")
                s["value"] = element.Value;
            if (element.IsCheckable)
                s["checked"] = element.Checked ? "True" : "False";
            if (element.Tag == "details")
                s["open"] = element.Open ? "True" : "False";
            if (element.Tag == "select")
                s["selectedIndex"] = element.SelectedIndex.ToString(CultureInfo.InvariantCulture);
            if (element.SupportsSelectionApi)
                s["selection"] = $"[{element.SelectionStart},{element.SelectionEnd},\"{element.SelectionDirection}\"]";
            if (element.HasAttribute("data-size"))
                s["size"] = $"[\"{element.GetStyle("width") ?? ""}\",\"{element.GetStyle("height") ?? ""}\"]";
            // A text field's scroll is its inner editor's (a textarea's its own).
            if (element.Tag == "textarea")
                s["scroll"] = $"[{Number(element.ScrollLeft)},{Number(element.ScrollTop)}]";
            else if (element.Tag == "input" && element.InputType is "text" or "search" or "url" or "tel" or "password" or "email" or "number")
                s["scroll"] = $"[{Number(element.EditorScrollLeft / document.Zoom)},0]";
            if (element.Tag is not ("html" or "body"))
                s["states"] = string.Join(' ', Pseudo.Where(element.Matches));
            states["#" + id] = s;
        }
        return states;
    }

    /// <summary>A number as JavaScript prints it.</summary>
    static string Number(float value) => ((double)value).ToString(CultureInfo.InvariantCulture);

    static string Diff(List<string> expected, List<string> actual)
    {
        var text = new StringBuilder();
        var count = Math.Max(expected.Count, actual.Count);
        for (var i = 0; i < count; i++)
        {
            var want = i < expected.Count ? expected[i] : "";
            var got = i < actual.Count ? actual[i] : "";
            text.Append(want == got ? "   " : " ! ").Append(want.PadRight(48)).Append(" | ").Append(got).Append('\n');
        }
        return text.ToString();
    }
}
