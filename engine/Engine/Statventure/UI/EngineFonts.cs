using Statventure.Packs;
using Statventure.Text;

namespace Statventure.UI;

/// <summary>
/// The engine's own Lato (Docs/Packaging.md, "The engine's embedded Lato"; Q17, Q23): the regular and bold faces the
/// editor's look uses (P4), with their OFL text, embedded in the engine's assembly, so every single-file program
/// carries them and can draw text before it has any data: the crash window, the credits, the editor's first screen.
/// The bytes are ui.pak's (<c>ui/fonts/lato-regular.ttf</c>, <c>lato-bold.ttf</c>, <c>lato-ofl.txt</c>; all three from
/// the old game's <c>objects/fonts/</c>, as <c>Tools/Scripts/make-ui-pack.sh</c> takes them). A document gets them one
/// of two ways: <see cref="AddTo"/> registers them in its font library as the family <see cref="Family"/>, weights 400
/// and 700 (its stylesheets then need no @font-face: the engine's own documents); <see cref="Mount"/> gives them at
/// the paths ui.pak has them, so a stylesheet whose @font-face rules name those paths (the editor's <c>Fonts.css</c>)
/// loads them from the document's <c>Files</c> unchanged.
/// </summary>
public static class EngineFonts
{
    /// <summary>The family both faces have (the name the editor's stylesheet uses).</summary>
    public const string Family = "Lato";

    /// <summary>Where <see cref="Mount"/> gives the regular face, the bold face and the licence: ui.pak's paths.</summary>
    public const string RegularPath = "ui/fonts/lato-regular.ttf", BoldPath = "ui/fonts/lato-bold.ttf", LicencePath = "ui/fonts/lato-ofl.txt";

    static readonly Lazy<byte[]> RegularBytes = new(() => Resource("lato-regular.ttf"));
    static readonly Lazy<byte[]> BoldBytes = new(() => Resource("lato-bold.ttf"));
    static readonly Lazy<byte[]> LicenceBytes = new(() => Resource("lato-ofl.txt"));

    /// <summary>Lato Regular (weight 400).</summary>
    public static ReadOnlySpan<byte> Regular => RegularBytes.Value;

    /// <summary>Lato Bold (weight 700).</summary>
    public static ReadOnlySpan<byte> Bold => BoldBytes.Value;

    /// <summary>The fonts' licence, the SIL Open Font License 1.1 with Lato's copyright (UTF-8).</summary>
    public static ReadOnlySpan<byte> Licence => LicenceBytes.Value;

    /// <summary>Adds both faces to <paramref name="library"/> as the family <see cref="Family"/>, weights 400 and 700.</summary>
    public static void AddTo(FontLibrary library)
    {
        library.Add(Regular, Family, 400);
        library.Add(Bold, Family, 700);
    }

    /// <summary>
    /// A mount giving both faces and their licence at ui.pak's paths (<see cref="RegularPath"/>, <see cref="BoldPath"/>,
    /// <see cref="LicencePath"/>), for a document's <c>Files</c>.
    /// </summary>
    public static Mount Mount() => Packs.Mount.Custom("the engine's fonts", new Entries());

    sealed class Entries : IMountEntries
    {
        public IEnumerable<string> Paths => [RegularPath, BoldPath, LicencePath];

        public bool IsRecord(string path) => false;

        public byte[] Read(string path) => path switch
        {
            RegularPath => (byte[])RegularBytes.Value.Clone(),
            BoldPath => (byte[])BoldBytes.Value.Clone(),
            LicencePath => (byte[])LicenceBytes.Value.Clone(),
            _ => throw new FileNotFoundException($"the engine's fonts have no {path}"),
        };
    }

    /// <summary>One of the engine's UI resources (UI/Resources, embedded as Statventure.UI.&lt;name&gt;).</summary>
    internal static byte[] Resource(string name)
    {
        using var stream = typeof(EngineFonts).Assembly.GetManifestResourceStream("Statventure.UI." + name)
            ?? throw new InvalidOperationException("the engine has no resource " + name);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
