using Statventure.Packs;
using Statventure.Text;
using Statventure.UI.Dom;

namespace Statventure.UI;

/// <summary>
/// The BRUTAL logo splash (the BRUTAL License v1.0, 2.2; Q11, Q29; Docs/Packaging.md, "The BRUTAL logo and the
/// credits"): BRUTAL's white logo (<c>LOGO_BRUTAL_WHT.png</c>, 4096 × 896, from ahwoo.com/brutal-branding, embedded
/// unaltered) centred on the dark ground of the editor's look (Q23, P4's <c>--paper</c>), drawn at the size the decision
/// was made with, <see cref="LogoWidth"/> CSS px wide (the earlier 712 × 168 file's artwork; the user, 2026-11-08), and
/// scaled down further only to fit the window; nothing else on it. It lasts <see cref="Length"/> seconds at every start;
/// a key or a mouse button pressed after its first <see cref="SkippableAfter"/> second ends it. A document of its own
/// with its own data (the logo at <see cref="LogoPath"/>), so a <see cref="UiHost"/> shows it with no wiring; the
/// application (M12 step 5) shows it once the window and device are made, before <c>Load</c>, and moves it on with
/// <see cref="Advance"/> each frame, on the document's clock: the frames' times, so tests run it without waiting.
/// </summary>
public sealed class Splash : IDisposable
{
    /// <summary>How long the splash shows (seconds).</summary>
    public const double Length = 2;

    /// <summary>From when a key or a mouse button skips it (seconds after it started).</summary>
    public const double SkippableAfter = 1;

    /// <summary>The logo's width on the splash (CSS px): the approved size, 696 × 152.25 at the logo's ratio.</summary>
    public const int LogoWidth = 696;

    /// <summary>Where the splash's data has the logo (its <c>img</c>'s source).</summary>
    public const string LogoPath = "engine/brutal-logo.png";

    /// <summary>The ground: the editor's dark look's <c>--paper</c> (P4).</summary>
    public const string Ground = "#1a1c1a";

    /// <summary>The splash's stylesheet: the logo centred, its width <see cref="LogoWidth"/>, never wider or taller than the window.</summary>
    public static readonly string Css = $$"""
        html, body { height: 100%; margin: 0; }
        html { background: {{Ground}}; }
        body { display: flex; align-items: center; justify-content: center; overflow: hidden; }
        .logo { display: block; width: {{LogoWidth}}px; max-width: 100%; max-height: 100%; object-fit: contain; }
        """;

    /// <summary>The splash's markup, the logo's source <paramref name="logo"/> (its data's path by default).</summary>
    public static string Markup(string logo = LogoPath) => $"""<img class="logo" src="{logo}" alt="BRUTAL">""";

    static readonly Lazy<byte[]> LogoBytes = new(() => EngineFonts.Resource("LOGO_BRUTAL_WHT.png"));

    /// <summary>The logo's PNG file as embedded (BRUTAL's, byte for byte).</summary>
    public static ReadOnlySpan<byte> Logo => LogoBytes.Value;

    readonly FontLibrary _fonts = new();
    readonly VirtualFileSystem _files;
    double? _start;
    double _now;
    bool _skipped;

    public Splash()
    {
        // No text shows; the engine's Lato is there so nothing falls back to a system font.
        EngineFonts.AddTo(_fonts);
        _files = new VirtualFileSystem([Mount.Custom("the BRUTAL splash", new Entries())]);
        Document = new UiDocument(_fonts) { Files = _files };
        Document.AddStylesheet(Css, "Statventure.UI.Splash");
        Dom.Markup.SetInnerHtml(Document.Body, Markup());
        Document.AddEventListener("keydown", _ => Skip());
        Document.AddEventListener("mousedown", _ => Skip());
    }

    /// <summary>The splash's document.</summary>
    public UiDocument Document { get; }

    /// <summary>Its time so far (seconds since the first <see cref="Advance"/>; 0 before).</summary>
    public double Elapsed => _start is { } start ? _now - start : 0;

    /// <summary>Whether a key or a click ended it early.</summary>
    public bool Skipped => _skipped;

    /// <summary>Whether it is over: its <see cref="Length"/> has passed, or it was skipped.</summary>
    public bool Finished => _skipped || (_start is not null && Elapsed >= Length);

    /// <summary>
    /// Moves the splash on to <paramref name="time"/> (seconds, the frame's time on the document's clock, after the host
    /// updated the document); the first call starts it. Keys and clicks the document gets count at the time of the
    /// last call.
    /// </summary>
    public void Advance(double time)
    {
        _start ??= time;
        _now = time;
    }

    void Skip()
    {
        if (_start is not null && Elapsed >= SkippableAfter)
            _skipped = true;
    }

    sealed class Entries : IMountEntries
    {
        public IEnumerable<string> Paths => [LogoPath];

        public bool IsRecord(string path) => false;

        public byte[] Read(string path) => path == LogoPath ? (byte[])LogoBytes.Value.Clone() : throw new FileNotFoundException(path);
    }

    public void Dispose()
    {
        _files.Dispose();
        _fonts.Dispose();
    }
}
