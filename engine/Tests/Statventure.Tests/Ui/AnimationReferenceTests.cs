using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Statventure.Core;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// Animations and transitions must produce the values the game's browser computes: each fixture is
/// sampled at <see cref="AnimationFixtures.Times"/> by the engine and compared with the browser's
/// getComputedStyle values at the same times (Golden/animation-reference.json, made by
/// Tools/Scripts/make-animation-references.sh).
/// </summary>
public class AnimationReferenceTests
{
    public static TheoryData<string> Fixtures => new(AnimationFixtures.All.Select(f => f.Name));

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheBrowserScriptIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "animation-fixtures.js");
        var script = ExportScript();
        if (!File.Exists(path) || File.ReadAllText(path) != script)
            File.WriteAllText(path, script);
        Assert.Contains("statventureAnimations", script);
    }

    static readonly Lazy<JsonElement> Reference = new(() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "animation-reference.json"))).RootElement);

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void AnimationsMatchTheBrowser(string name)
    {
        var fixture = AnimationFixtures.All.Single(f => f.Name == name);
        var reference = Reference.Value.GetProperty("fixtures").GetProperty(name);
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet(LayoutFixtures.BaseCss + fixture.Css, name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update(0);
        if (fixture.Trigger is { } trigger)
        {
            document.Body.AddClass(trigger);
            document.Update(0);
        }
        var problems = new List<string>();
        for (var i = 0; i < AnimationFixtures.Times.Length; i++)
        {
            var time = AnimationFixtures.Times[i];
            document.Update(time);
            var sample = reference[i];
            foreach (var element in sample.EnumerateObject())
            {
                var el = document.GetElementById(element.Name) ?? throw new InvalidOperationException("no element " + element.Name);
                foreach (var property in element.Value.EnumerateObject())
                {
                    var expected = property.Value.GetString()!;
                    var actual = Format(el, property.Name);
                    if (!Same(expected, actual))
                        problems.Add($"t={time} #{element.Name} {property.Name}: {actual}, browser {expected}");
                }
            }
        }
        Assert.True(problems.Count == 0, $"{name}: {problems.Count} differences\n" + string.Join("\n", problems.Take(30)));
    }

    // ---------- Comparing ----------

    static readonly Regex Token = new(@"-?\d*\.?\d+(?:e-?\d+)?|[a-z-]+|[(),%]", RegexOptions.Compiled);

    static bool Same(string expected, string actual)
    {
        // Equal values written differently: normal letter-spacing is 0, identity individual transforms
        // are none, and a zero percentage is zero.
        static string Normalize(string s) => s switch
        {
            "normal" or "0px" or "0px 0px" or "0deg" or "1" or "1 1" => "none",
            _ => Regex.Replace(s, @"(?<![\d.])0%", "0px"),
        };
        var a = Token.Matches(Normalize(expected)).Select(m => m.Value).ToList();
        var b = Token.Matches(Normalize(actual)).Select(m => m.Value).ToList();
        if (a.Count != b.Count)
            return false;
        // Colour channels (0-255) may differ by one level of rounding.
        var channel = -1;
        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] is "rgb" or "rgba")
                channel = 0;
            else if (a[i] == ")")
                channel = -1;
            var na = double.TryParse(a[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var x);
            var nb = double.TryParse(b[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var y);
            if (na && nb)
            {
                var tolerance = channel is >= 0 and < 3 ? 1.01 : 0.015 + 0.004 * Math.Max(Math.Abs(x), Math.Abs(y));
                if (channel >= 0)
                    channel++;
                if (Math.Abs(x - y) > tolerance)
                    return false;
            }
            else if (a[i] != b[i])
                return false;
        }
        return true;
    }

    // ---------- The engine's values as getComputedStyle prints them ----------

    static string N(float v) => Math.Round(v, 4).ToString(CultureInfo.InvariantCulture);

    static string Px(float v) => N(v) + "px";

    static string ColorText(Color c)
    {
        int B(float v) => (int)MathF.Round(Math.Clamp(v, 0, 1) * 255);
        return c.A >= 1 ? $"rgb({B(c.R)}, {B(c.G)}, {B(c.B)})" : $"rgba({B(c.R)}, {B(c.G)}, {B(c.B)}, {N(MathF.Round(c.A * 255) / 255)})";
    }

    static string LengthText(Length l, float basis) => l.IsAuto ? "auto" : Px(l.ResolveOr(basis, 0));

    public static string Format(UiElement element, string property)
    {
        var s = element.Style!;
        var box = element.Box;
        var size = box is null ? Vector2.Zero : new Vector2(box.Width, box.Height);
        switch (property)
        {
            case "opacity":
                return N(s.Opacity);
            case "left":
                return box is not null && s.Position != PositionKind.Static ? Px(s.Left.ResolveOr(box.Container?.Width ?? 0, 0)) : LengthText(s.Left, 0);
            case "width":
                return box is not null ? Px(box.Width - (s.BoxSizing == BoxSizing.ContentBox ? box.Border.Horizontal + box.Padding.Horizontal : 0)) : LengthText(s.Width, 0);
            case "letter-spacing":
                return s.LetterSpacing == 0 ? "normal" : Px(s.LetterSpacing);
            case "color":
                return ColorText(s.Color);
            case "background-color":
                return ColorText(s.BackgroundColor);
            case "visibility":
                return s.Visibility.ToString().ToLowerInvariant();
            case "transform":
            {
                if (s.Transform.Length == 0)
                    return "none";
                var m = Matrix3x2.Identity;
                foreach (var f in s.Transform.Reverse())
                    m *= f.ToMatrix(size);
                return $"matrix({N(m.M11)}, {N(m.M12)}, {N(m.M21)}, {N(m.M22)}, {N(m.M31)}, {N(m.M32)})";
            }
            case "translate":
                return s.TranslateX.IsZero && s.TranslateY.IsZero ? "none" : s.TranslateY.IsZero ? LengthText(s.TranslateX, size.X) : $"{LengthText(s.TranslateX, size.X)} {LengthText(s.TranslateY, size.Y)}";
            case "rotate":
                return s.Rotate == 0 ? "none" : N(s.Rotate) + "deg";
            case "scale":
                return s.ScaleX == 1 && s.ScaleY == 1 ? "none" : s.ScaleX == s.ScaleY ? N(s.ScaleX) : $"{N(s.ScaleX)} {N(s.ScaleY)}";
            case "filter":
            {
                if (s.Filters.Length == 0)
                    return "none";
                return string.Join(" ", s.Filters.Select(f => f.Kind switch
                {
                    FilterFunctionKind.Blur => $"blur({Px(f.Amount)})",
                    FilterFunctionKind.DropShadow => $"drop-shadow({ColorText(f.Color)} {Px(f.OffsetX)} {Px(f.OffsetY)} {Px(f.Amount)})",
                    FilterFunctionKind.HueRotate => $"hue-rotate({N(f.Amount)}deg)",
                    _ => $"{Kebab(f.Kind.ToString())}({N(f.Amount)})",
                }));
            }
            case "box-shadow":
                return s.BoxShadows.Length == 0 ? "none" : string.Join(", ", s.BoxShadows.Select(sh => $"{ColorText(sh.Color)} {Px(sh.OffsetX)} {Px(sh.OffsetY)} {Px(sh.Blur)} {Px(sh.Spread)}{(sh.Inset ? " inset" : "")}"));
            case "clip-path":
            {
                if (s.ClipPath is not { } c)
                    return "none";
                string E(Length l) => l.HasPercent && l.Pixels == 0 ? N(l.Percent) + "%" : Px(l.Pixels);
                return $"inset({E(c.Top)} {E(c.Right)} {E(c.Bottom)} {E(c.Left)})";
            }
        }
        throw new ArgumentException("no formatter for " + property);
    }

    static string Kebab(string name)
    {
        var text = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && text.Length > 0)
                text.Append('-');
            text.Append(char.ToLowerInvariant(c));
        }
        return text.ToString();
    }

    // ---------- The browser script ----------

    public static string ExportScript()
    {
        var fixtures = AnimationFixtures.All.Select(f => new { name = f.Name, css = LayoutFixtures.BaseCss + f.Css, html = f.Html, properties = f.Properties, trigger = f.Trigger });
        return "// Generated by Statventure.Tests (AnimationReferenceTests.ExportScript): samples the animation fixtures.\n"
            + "const STATVENTURE_ANIMATION_FIXTURES = " + JsonSerializer.Serialize(fixtures) + ";\n"
            + "const STATVENTURE_ANIMATION_TIMES = " + JsonSerializer.Serialize(AnimationFixtures.Times) + ";\n" + """
            async function statventureAnimations() {
              const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
              const base = new URL(".", location.href).href;
              const fonts = `@font-face { font-family: Lato; src: url("${base}objects/fonts/Lato-Regular.ttf"); font-weight: 400; }`;
              const out = { userAgent: navigator.userAgent, fixtures: {} };
              for (const fixture of STATVENTURE_ANIMATION_FIXTURES) {
                const frame = document.createElement("iframe");
                frame.style.cssText = "position:fixed;left:0;top:0;width:400px;height:300px;border:0;z-index:2147483647;background:#fff";
                const loaded = new Promise(resolve => frame.onload = resolve);
                frame.srcdoc = `<!DOCTYPE html><html><head><style>${fonts}\n${fixture.css}</style></head><body>${fixture.html}</body></html>`;
                document.body.append(frame);
                await loaded;
                const doc = frame.contentDocument;
                const win = frame.contentWindow;
                const ids = [...doc.querySelectorAll("[id]")];
                ids.forEach(e => win.getComputedStyle(e).opacity);
                if (fixture.trigger) {
                  doc.body.classList.add(fixture.trigger);
                  ids.forEach(e => win.getComputedStyle(e).opacity);
                }
                const animations = doc.getAnimations();
                const samples = [];
                for (const time of STATVENTURE_ANIMATION_TIMES) {
                  for (const a of animations) { a.pause(); a.currentTime = time * 1000; }
                  const sample = {};
                  for (const e of ids) {
                    const cs = win.getComputedStyle(e);
                    const values = {};
                    for (const p of fixture.properties) values[p] = cs.getPropertyValue(p);
                    sample[e.id] = values;
                  }
                  samples.push(sample);
                }
                out.fixtures[fixture.name] = samples;
                frame.remove();
              }
              throw new Error("OUT" + JSON.stringify(out));
            }
            """;
    }
}
