// The figure mode's keys and its playing clock (GM2-P14: "keys to step and turn"; GM2 step 3a), apart from the window so
// the tests press them: Space plays and pauses; Right and Left step a frame (1/60 s, on whole frames) and pause; Down
// and Up go to the next and the previous clip's start (in the same round of the cycle); A and D turn the figure 15°
// (the camera stays the view's); 1–5 show the views front, three-quarter, side, back and close-up; Escape quits.
using Statventure.Platform;

namespace Statventure.Sandbox.Figure;

public sealed class FigureControls(FigureView figure)
{
    public const double Frame = 1.0 / 60;
    public const float TurnStep = 15;
    static readonly Key[] ViewKeys = [Key.D1, Key.D2, Key.D3, Key.D4, Key.D5];

    /// <summary>The playing time (seconds on the clips' cycle, played round).</summary>
    public double Time { get; private set; }
    public bool Playing { get; private set; } = true;
    /// <summary>Escape was pressed.</summary>
    public bool Quit { get; private set; }

    /// <summary>A key pressed.</summary>
    public void Press(Key key)
    {
        switch (key)
        {
            case Key.Escape:
                Quit = true;
                break;
            case Key.Space:
                Playing = !Playing;
                break;
            case Key.Right or Key.Left:
                Playing = false;
                Time = Math.Round(Time / Frame) * Frame + (key == Key.Right ? Frame : -Frame);
                break;
            case Key.Down or Key.Up when figure.Clips.Clips.Count > 0:
                var clips = figure.Clips;
                // The clip begun last (at a clip's start that clip, though the stack shows the one before at its end there).
                var clip = clips.Starts.Count(s => s <= clips.Wrap(Time) + 1e-9) - 1;
                var next = (clip + (key == Key.Down ? 1 : clips.Clips.Count - 1)) % clips.Clips.Count;
                Time = clips.Starts[next] + Math.Floor(Time / clips.Cycle) * clips.Cycle;
                break;
            case Key.A:
                figure.Turn -= TurnStep;
                break;
            case Key.D:
                figure.Turn += TurnStep;
                break;
            default:
                var view = Array.IndexOf(ViewKeys, key);
                if (view >= 0)
                    figure.ShowView(FigureFraming.Views[view]);
                break;
        }
    }

    /// <summary>A frame: the clock moved on while playing, the figure posed at its time.</summary>
    public void Advance(double seconds)
    {
        if (Playing)
            Time += seconds;
        figure.Seek(Time);
    }
}
