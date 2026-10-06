using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// A few motes of ash or mist floating between the camera and the floor (presentation only).
/// They sit closer to the camera than the room, so they slide past faster than the floor when
/// the view moves (parallax) and are out of focus: large, soft and faint. Sparse and slow, they
/// give the air in front of the scene a depth of its own without covering the fight.
/// </summary>
public static class ForegroundMotes
{
    public readonly record struct Look(Color Tint, float Opacity, Vector2 Drift, int Count);

    /// <summary>
    /// Draws the motes in world space inside the running scene batch, placed so that on screen
    /// they move with <c>1 + depth</c> times the camera's motion.
    /// </summary>
    public static void Draw(SpriteBatch batch, ArtAssets art, Camera2D camera, Viewport viewport, float time, Look look, int seed)
    {
        if (look.Count <= 0 || look.Opacity <= 0f)
        {
            return;
        }

        float zoom = MathF.Max(0.1f, camera.Zoom);
        Vector2 view = new(viewport.Width, viewport.Height);
        Vector2 margin = new(80f);
        Vector2 span = view + margin * 2f;
        uint state = (uint)seed * 2654435761u + 7u;
        for (int index = 0; index < look.Count; index++)
        {
            Vector2 home = new(Next(ref state) * span.X, Next(ref state) * span.Y);
            float depth = 0.35f + Next(ref state) * 0.5f;
            float size = 3.5f + Next(ref state) * 6f;
            float phase = Next(ref state) * MathF.Tau;
            float pace = 0.6f + Next(ref state) * 0.8f;
            // Screen position: drifts slowly and slides against the camera by its depth.
            Vector2 drift = look.Drift * pace * time + new Vector2(MathF.Sin(time * 0.31f + phase), MathF.Cos(time * 0.23f + phase)) * 14f;
            Vector2 screen = home + drift - camera.Position * zoom * (1f + depth);
            screen = new Vector2(Wrap(screen.X, span.X), Wrap(screen.Y, span.Y)) - margin;
            Vector2 world = camera.Position + (screen - view * 0.5f) / zoom;
            float twinkle = 0.65f + 0.35f * MathF.Sin(time * 0.5f + phase);
            float alpha = look.Opacity * twinkle * (0.6f + depth * 0.5f);
            art.DrawSoftSpot(batch, world, new Vector2(size * (1f + depth)), look.Tint * alpha);
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
