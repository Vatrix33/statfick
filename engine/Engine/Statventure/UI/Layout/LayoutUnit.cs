namespace Statventure.UI.Layout;

/// <summary>
/// Blink lays out in LayoutUnits: fixed point with 1/64 px. Where it turns a float into one, the
/// value is truncated; doing the same keeps the engine's boxes on Chromium's grid, so pixel snapping
/// rounds the same way.
/// </summary>
public static class LayoutUnit
{
    public const float Epsilon = 1f / 64;

    /// <summary>Truncation toward zero to 1/64 px (LayoutUnit(float)).</summary>
    public static float Truncate(float value) => MathF.Truncate(value * 64) / 64;

    /// <summary>LayoutUnit::FromFloatRound.</summary>
    public static float Round(float value) => MathF.Round(value * 64, MidpointRounding.AwayFromZero) / 64;

    /// <summary>LayoutUnit::FromFloatCeil.</summary>
    public static float Ceil(float value) => MathF.Ceiling(value * 64) / 64;
}
