using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Drifting mist and haze over the ground (presentation only): broad, faint banks of soft light
/// that move slowly and wrap around, so rooms read with air and depth. Each bank is a soft spot;
/// placement and drift come from a fixed seed, so a room always breathes the same way.
/// </summary>
public static class Atmosphere
{
    /// <summary>A band of mist: banks between <paramref name="top"/> and <paramref name="bottom"/> drifting along x.</summary>
    public readonly record struct Band(float Top, float Bottom, int Banks, float Speed, Color Tint, float Opacity, float Width = 340f, float Height = 46f);

    public static void Draw(SpriteBatch batch, ArtAssets art, float left, float right, float time, int seed, params Band[] bands)
    {
        float span = right - left;
        if (span <= 0f)
        {
            return;
        }

        uint state = (uint)seed * 2654435761u + 1u;
        foreach (Band band in bands)
        {
            for (int index = 0; index < band.Banks; index++)
            {
                float offset = Next(ref state) * (span + band.Width * 2f);
                float y = band.Top + Next(ref state) * (band.Bottom - band.Top);
                float scale = 0.7f + Next(ref state) * 0.6f;
                float pace = band.Speed * (0.6f + Next(ref state) * 0.8f);
                float phase = Next(ref state) * MathF.Tau;
                float x = left - band.Width + Wrap(offset + time * pace, span + band.Width * 2f);
                float breathe = 0.75f + 0.25f * MathF.Sin(time * 0.35f + phase);
                Vector2 radii = new(band.Width * scale, band.Height * scale);
                art.DrawSoftSpot(batch, new Vector2(x, y + MathF.Sin(time * 0.2f + phase) * 6f), radii, band.Tint * (band.Opacity * breathe));
            }
        }
    }

    private static float Wrap(float value, float length) => ((value % length) + length) % length;

    private static float Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xFFFFFF) / (float)0x1000000;
    }
}
