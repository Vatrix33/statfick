// Plays the game's music through the default output device, to listen to it (Docs/Audio.md):
//   play-theme.sh                     lists the themes
//   play-theme.sh alder-vale          plays one, looping, until Enter
//   play-theme.sh alder-vale --volume 30
// While it plays, type another theme's id to change to it (with the game's fades) or a number from 0 to
// 100 to set the volume. Enter alone quits, fading out.
using System.Globalization;
using Statventure.Audio;

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
if (arguments.Count == 0)
{
    Console.WriteLine("Themes (play one with: play-theme.sh <theme> [--volume 0-100]):");
    foreach (var id in Soundtrack.Themes)
        Console.WriteLine("  " + id);
    return 0;
}
if (!Soundtrack.Themes.Contains(arguments[0]))
{
    Console.Error.WriteLine($"No theme '{arguments[0]}' (play-theme.sh without arguments lists them)");
    return 1;
}

using var audio = new AudioSystem();
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
    else if (Soundtrack.Themes.Contains(line))
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
