using Brutal.GlfwApi;
using Statventure.Audio;

namespace Statventure.Game;

/// <summary>
/// What the application class does with the window (Docs/Game/Foundation.md, "The window"; Q19, GM1-P14): its size and
/// state read to keep them in the settings, maximized and full screen put in place, brought forward for a later start.
/// The program's is the engine's window (<see cref="EngineGameWindow"/>); the tests give one of their own.
/// </summary>
internal interface IGameWindow
{
    /// <summary>The window's size in logical units (the content area).</summary>
    int Width { get; }
    int Height { get; }
    bool Maximized { get; }
    bool Minimized { get; }
    bool Fullscreen { get; set; }
    void Maximize();
    /// <summary>Back from maximized.</summary>
    void Restore();
    /// <summary>Restored if minimized, the focus asked for; gives what was done, for the log.</summary>
    string BringForward();
}

/// <summary>
/// The engine's window as the game uses it. The engine's <see cref="Platform.WindowOptions"/> have no maximized or full
/// screen state, so the application class puts them in place at <c>Load</c>, after the splash (GLFW's own window: its
/// <c>Maximize</c>, <c>Restore</c>, <c>Focus</c> and attention request, which the engine's <see cref="Platform.Window"/>
/// gives as <see cref="Platform.Window.Native"/>).
/// </summary>
sealed class EngineGameWindow(Platform.Window window) : IGameWindow
{
    public int Width => window.Width;
    public int Height => window.Height;
    public bool Maximized => window.Native.GetAttribute(GlfwWindowAttribute.Maximized) != 0;
    public bool Minimized => window.IsMinimized;

    public bool Fullscreen
    {
        get => window.Fullscreen;
        set => window.Fullscreen = value;
    }

    public void Maximize() => window.Native.Maximize();

    public void Restore() => window.Native.Restore();

    public string BringForward()
    {
        var done = new List<string>();
        // Restore only when minimized: GLFW's restore also ends a maximized state.
        if (window.Native.GetAttribute(GlfwWindowAttribute.Iconified) != 0)
        {
            window.Native.Restore();
            done.Add("restored");
        }
        window.Native.Focus();
        done.Add("focused");
        // Wayland lets the compositor refuse the focus: GLFW's attention request then (GM1-P14).
        if (Glfw.GetPlatform() == GlfwPlatform.Wayland)
        {
            window.Native.RequestAttention();
            done.Add("attention requested (Wayland)");
        }
        return string.Join(", ", done);
    }
}

/// <summary>Where the three volumes go (GM1-4, Q22): the sound's master, music and effects (0–1 each); the tests give their own.</summary>
internal interface IGameVolumes
{
    void Apply(double master, double music, double effects);
}

/// <summary>The engine's sound: <see cref="AudioSystem.MasterVolume"/>, the soundtrack's volume (applied though no theme plays in GM1, Q15) and <see cref="AudioSystem.EffectsVolume"/>.</summary>
sealed class AudioVolumes(AudioSystem audio) : IGameVolumes
{
    public void Apply(double master, double music, double effects)
    {
        audio.MasterVolume = master;
        audio.Music.Volume = music;
        audio.EffectsVolume = effects;
    }
}
