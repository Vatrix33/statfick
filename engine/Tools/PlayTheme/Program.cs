// Plays the game's music through the default output device, to listen to it (Docs/Audio.md):
//   play-theme.sh                     lists the themes
//   play-theme.sh alder-vale          plays one, looping, until Enter
//   play-theme.sh alder-vale --volume 30
// While it plays, type another theme's id to change to it (with the game's fades) or a number from 0 to
// 100 to set the volume. Enter alone quits, fading out. The music is the repo's audio.pak (engine/Assets/Packs/).
// With --effect it plays sound effects instead (play-sounds.sh; Docs/Audio.md, "The game's sound effects"):
//   PlayTheme --effect [name ...] [--volume 0-100] [--candidates <folder>]
using System.Globalization;
using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;

if (args.Contains("--effect"))
    return PlayEffects([.. args.Where(a => a != "--effect")]);

var arguments = args.ToList();
var volume = 100.0;
var volumeAt = arguments.IndexOf("--volume");
if (volumeAt >= 0)
{
    if (volumeAt + 1 >= arguments.Count || !TryVolume(arguments[volumeAt + 1], out volume))
    {
        Console.Error.WriteLine("--volume takes a number from 0 to 100");
        return 1;
    }
    arguments.RemoveRange(volumeAt, 2);
}
var music = RepoMusic();
if (arguments.Count == 0)
{
    Console.WriteLine("Themes (play one with: play-theme.sh <theme> [--volume 0-100]):");
    foreach (var id in music.Themes)
        Console.WriteLine("  " + id);
    return 0;
}
if (!music.Themes.Contains(arguments[0]))
{
    Console.Error.WriteLine($"No theme '{arguments[0]}' (play-theme.sh without arguments lists them)");
    return 1;
}

using var audio = new AudioSystem(music);
if (!audio.HasDevice)
    Console.WriteLine("No output device: nothing will be heard until one appears.");
audio.Music.Volume = volume / 100;
audio.Music.Play(arguments[0]);
Console.WriteLine($"Playing {arguments[0]} at {volume.ToString(CultureInfo.InvariantCulture)} %. Type a theme or a volume (0-100); Enter alone quits.");
while (Console.ReadLine() is { Length: > 0 } line)
{
    line = line.Trim();
    if (TryVolume(line, out var newVolume))
    {
        audio.Music.Volume = newVolume / 100;
        Console.WriteLine($"Volume {newVolume.ToString(CultureInfo.InvariantCulture)} %");
    }
    else if (music.Themes.Contains(line))
    {
        audio.Music.Play(line);
        Console.WriteLine("Playing " + line);
    }
    else
        Console.WriteLine($"No theme '{line}'");
}
// The game's fade out, not a click.
audio.Music.Stop();
Thread.Sleep(650);
return 0;

static bool TryVolume(string text, out double value) =>
    double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value is >= 0 and <= 100;

/// <summary>The repo's committed audio.pak (engine/Assets/Packs/), found above the tool's folder, and the music in it.</summary>
static MusicLibrary RepoMusic()
{
    for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
    {
        var pack = Path.Combine(folder.FullName, "Assets", "Packs", "audio.pak");
        if (File.Exists(pack))
        {
            using var data = new VirtualFileSystem([Mount.Pack(pack)]);
            return MusicLibrary.Load(data);
        }
    }
    throw new FileNotFoundException("No Assets/Packs/audio.pak above " + AppContext.BaseDirectory);
}

/// <summary>
/// The --effect mode (Tools/Scripts/play-sounds.sh): sound effects played one after another through the default device
/// as the game plays them (the audio system's effects, on the effects bus at the effects volume, the master at 100 %).
/// The patches are the repo's audio.pak's, or with --candidates every <c>*.json</c> in a folder, each read as an
/// <c>audio.effect</c> record's JSON form and registered under its file's name. A name plays the patch of that name,
/// or when there is none every patch whose name is it and "-" and more (<c>ui-press</c>: <c>ui-press-a</c>,
/// <c>ui-press-b</c> …), in name order; no names play every patch in name order. --volume is the effects volume, 60 by
/// default (the game's first start, Docs/Game/Foundation.md, Q22). Each sound's name is printed, it plays, and the next
/// follows a pause (1.2 s) after its end.
/// </summary>
static int PlayEffects(List<string> arguments)
{
    const int PauseMilliseconds = 1200;
    var volume = 60.0;
    var volumeAt = arguments.IndexOf("--volume");
    if (volumeAt >= 0)
    {
        if (volumeAt + 1 >= arguments.Count || !TryVolume(arguments[volumeAt + 1], out volume))
        {
            Console.Error.WriteLine("--volume takes a number from 0 to 100");
            return 1;
        }
        arguments.RemoveRange(volumeAt, 2);
    }
    string? candidates = null;
    var candidatesAt = arguments.IndexOf("--candidates");
    if (candidatesAt >= 0)
    {
        if (candidatesAt + 1 >= arguments.Count || !Directory.Exists(arguments[candidatesAt + 1]))
        {
            Console.Error.WriteLine("--candidates takes a folder of patches (<name>.json)");
            return 1;
        }
        candidates = arguments[candidatesAt + 1];
        arguments.RemoveRange(candidatesAt, 2);
    }
    if (arguments.FirstOrDefault(a => a.StartsWith('-')) is { } unknown)
    {
        Console.Error.WriteLine($"Unknown option {unknown}");
        return 1;
    }

    var music = RepoMusic();
    var patches = new SortedDictionary<string, EffectPatch>(StringComparer.Ordinal);
    if (candidates is null)
        foreach (var (name, patch) in music.Effects)
            patches[name] = patch;
    else
        foreach (var file in Directory.GetFiles(candidates, "*.json"))
        {
            try
            {
                patches[Path.GetFileNameWithoutExtension(file)] = RecordSerializer.FromJson<EffectPatch>(File.ReadAllText(file));
            }
            catch (Exception problem) when (problem is IOException or RecordFormatException or ArgumentException)
            {
                Console.Error.WriteLine($"{file}: {problem.Message}");
                return 1;
            }
        }
    var order = new List<string>();
    foreach (var name in arguments)
    {
        var chosen = patches.ContainsKey(name) ? [name] : patches.Keys.Where(k => k.StartsWith(name + "-", StringComparison.Ordinal)).ToList();
        if (chosen.Count == 0)
        {
            Console.Error.WriteLine($"No sound '{name}' ({(candidates is null ? "the effects in audio.pak" : "the patches in " + candidates)}: {string.Join(", ", patches.Keys)})");
            return 1;
        }
        order.AddRange(chosen);
    }
    if (arguments.Count == 0)
        order.AddRange(patches.Keys);
    if (order.Count == 0)
    {
        Console.Error.WriteLine(candidates is null ? "audio.pak holds no sound effects" : $"No patches (*.json) in {candidates}");
        return 1;
    }

    using var audio = new AudioSystem(music);
    if (!audio.HasDevice)
        Console.WriteLine("No output device: nothing will be heard until one appears.");
    foreach (var name in order.Distinct())
    {
        try
        {
            audio.Effects.Register(name, patches[name]);
        }
        catch (ArgumentException problem)
        {
            Console.Error.WriteLine(problem.Message);
            return 1;
        }
    }
    // The master stays at the audio system's 1, as the game's first start has it.
    audio.EffectsVolume = volume / 100;
    // Each patch once, silently, so the first sound heard isn't the one the code is compiled on.
    foreach (var name in order.Distinct())
        audio.Effects.Play(name, volume: 0);
    Thread.Sleep(600);
    Console.WriteLine($"Effects volume {volume.ToString(CultureInfo.InvariantCulture)} %, master 100 %.");
    foreach (var name in order)
    {
        Console.WriteLine(name);
        audio.Effects.Play(name);
        Thread.Sleep((int)Math.Ceiling(EffectLength(patches[name]) * 1000) + PauseMilliseconds);
    }
    return 0;
}

/// <summary>Seconds from a play's start to its last layer's stop (SoundEffects stops a layer 50 ms after its length).</summary>
static double EffectLength(EffectPatch patch) => patch.Layers.Max(l => l.Delay + l.Attack + l.Length + 0.05);

