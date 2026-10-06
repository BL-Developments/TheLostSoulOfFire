using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Rendering;

public static partial class ScytheBladePaths
{
    public readonly record struct Sample(float Progress, float Heading, float Distance, float Height);

    /// <summary>World units per metre in the renders (render_directions.py at 1.5 pixels per unit).</summary>
    public const float UnitsPerMetre = 66.7f;

    /// <summary>
    /// The core's place at <paramref name="progress"/> of swing <paramref name="step"/>, smoothly
    /// in between the recorded frames (Catmull-Rom through the samples).
    /// </summary>
    public static Sample At(int step, float progress)
    {
        ReadOnlySpan<Sample> samples = Of(step);
        float p = MathHelper.Clamp(progress, 0f, 1f);
        int index = 0;
        while (index < samples.Length - 2 && samples[index + 1].Progress < p)
        {
            index++;
        }

        Sample a = samples[Math.Max(index - 1, 0)];
        Sample b = samples[index];
        Sample c = samples[index + 1];
        Sample d = samples[Math.Min(index + 2, samples.Length - 1)];
        float t = MathHelper.Clamp((p - b.Progress) / MathF.Max(c.Progress - b.Progress, 0.0001f), 0f, 1f);
        return new Sample(
            p,
            MathHelper.CatmullRom(a.Heading, b.Heading, c.Heading, d.Heading, t),
            MathHelper.CatmullRom(a.Distance, b.Distance, c.Distance, d.Distance, t),
            MathHelper.CatmullRom(a.Height, b.Height, c.Height, d.Height, t));
    }

    /// <summary>
    /// Screen offset from the feet of a point <paramref name="levelDistance"/> world units away
    /// along <paramref name="angle"/> (screen radians) and <paramref name="height"/> metres up:
    /// the floor is seen at 35°, so level depth is squashed and height lifts the point.
    /// </summary>
    public static Vector2 Project(float angle, float levelDistance, float height) =>
        new(MathF.Cos(angle) * levelDistance,
            MathF.Sin(angle) * levelDistance * FigureHeights.LevelSquash - height * UnitsPerMetre * HeightPerLevel);

    /// <summary>A metre of height shows as cos 35° of a metre of level distance.</summary>
    private const float HeightPerLevel = 0.819f;
}
