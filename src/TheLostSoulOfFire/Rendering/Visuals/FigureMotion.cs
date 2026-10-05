using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Rendering.Visuals;

/// <summary>
/// The direction a figure is <em>drawn</em> facing. It follows the gameplay facing with a
/// limited turn rate and only switches between the eight sheet directions with hysteresis,
/// so the shoulders never jump to the mouse. Aiming, attacks and hits keep using the
/// immediate gameplay facing; this class never feeds back into them.
/// </summary>
public sealed class FacingTracker
{
    /// <summary>Radians per second; one full turn takes about half a second.</summary>
    public static readonly float TurnRate = MathHelper.TwoPi * 2f;

    /// <summary>How far past a sector border the body must turn before the drawn direction changes.</summary>
    public static readonly float Hysteresis = MathHelper.ToRadians(9f);

    /// <summary>Longest step taken at once, so a frame hitch cannot skip a direction.</summary>
    public const float MaxStep = 1f / 30f;

    private static readonly float HalfSector = MathHelper.PiOver4 * 0.5f;
    private bool _initialised;

    public float Angle { get; private set; }
    public int Sector { get; private set; } = 2;
    public string Direction => VisualDirections.FromSector(Sector);

    public string Update(Vector2 facing, float deltaTime)
    {
        if (facing.LengthSquared() < 0.0001f)
        {
            return Direction;
        }

        float target = MathF.Atan2(facing.Y, facing.X);
        if (!_initialised)
        {
            _initialised = true;
            Angle = target;
            Sector = VisualDirections.SectorOf(target);
            return Direction;
        }

        float remaining = MathF.Max(0f, deltaTime);
        do
        {
            float step = MathF.Min(remaining, MaxStep);
            remaining -= step;
            float delta = MathHelper.WrapAngle(target - Angle);
            float turn = TurnRate * step;
            Angle = MathHelper.WrapAngle(Angle + MathHelper.Clamp(delta, -turn, turn));

            float fromCentre = MathHelper.WrapAngle(Angle - VisualDirections.AngleOfSector(Sector));
            if (MathF.Abs(fromCentre) > HalfSector + Hysteresis)
            {
                Sector = VisualDirections.SectorOf(Angle);
            }
        }
        while (remaining > 0f);

        return Direction;
    }
}

/// <summary>
/// Advances a clip's playback: time clips with the clock, distance clips by the distance the
/// figure travelled, one full cycle per <see cref="VisualClipDefinition.CycleDistance"/>. Half
/// the speed gives half the animation rate, so feet do not slide.
/// </summary>
public static class ClipClock
{
    public static float Advance(float elapsed, VisualClipDefinition clip, float deltaTime, float distance) =>
        clip.Progress == ClipProgress.Distance
            ? elapsed + MathF.Max(0f, distance) / clip.CycleDistance * clip.Duration
            : elapsed + MathF.Max(0f, deltaTime);
}
