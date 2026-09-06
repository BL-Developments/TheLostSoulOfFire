using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Combat feedback is painted with a feathered brush instead of drawn with vector
/// strokes.
///
/// Hard 2px arcs, rings and lines read as interface overlaid on the world: they
/// have no falloff, no thickness variation and a constant, non-diegetic shine.
/// Death Flame is light and dissolution, so every telegraph, trail and mark is
/// built here from overlapping soft blobs that inherit the same falloff as the
/// lighting pass. Nothing in this file produces a crisp edge.
/// </summary>
public static class SoftShapes
{
    private const int BrushSize = 96;

    // A gentle falloff matters more than it looks. A peaked brush (>2.0) keeps a
    // bright centre, so overlapping dabs sum into visible beads under additive
    // blending and a painted band reads as a dotted line. This profile is nearly
    // flat across the inner third and falls off smoothly to zero.
    private const float BrushFalloff = 1.35f;
    private const float BrushPlateau = 0.26f;

    /// <summary>
    /// One/One blend for the premultiplied brush. Combat cues add light to the
    /// scene the way the emission pass does, so they never produce a dark fringe.
    /// </summary>
    public static readonly BlendState AdditiveLight = new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };

    public static Texture2D CreateBrush(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, BrushSize, BrushSize, false, SurfaceFormat.Color);
        Color[] data = new Color[BrushSize * BrushSize];
        Vector2 center = new((BrushSize - 1) * 0.5f);
        float inverseRadius = 1f / (BrushSize * 0.5f);

        for (int y = 0; y < BrushSize; y++)
        {
            for (int x = 0; x < BrushSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) * inverseRadius;
                // Flat plateau, then a smooth shoulder to zero.
                float shoulder = MathHelper.Clamp(
                    (distance - BrushPlateau) / (1f - BrushPlateau),
                    0f,
                    1f);
                float value = MathF.Pow(1f - shoulder, BrushFalloff);
                value *= value * (3f - 2f * value);
                byte alpha = (byte)MathF.Round(value * 255f);
                // Premultiplied so it composites correctly under AlphaBlend and
                // adds cleanly under the additive light blend.
                data[y * BrushSize + x] = new Color(alpha, alpha, alpha, alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }

    /// <summary>A single feathered dab. Every other helper is built from these.</summary>
    public static void Blob(SpriteBatch batch, Texture2D brush, Vector2 position, float radius, Color color)
    {
        if (radius <= 0.5f || color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0)
        {
            return;
        }

        batch.Draw(
            brush,
            position,
            null,
            color,
            0f,
            new Vector2(brush.Width, brush.Height) * 0.5f,
            radius * 2f / brush.Width,
            SpriteEffects.None,
            0f);
    }

    /// <summary>An elongated dab, for trails and lanes that need direction.</summary>
    public static void Streak(
        SpriteBatch batch,
        Texture2D brush,
        Vector2 position,
        Vector2 direction,
        float length,
        float width,
        Color color)
    {
        if (length <= 0.5f || width <= 0.5f)
        {
            return;
        }

        float rotation = MathF.Atan2(direction.Y, direction.X);
        batch.Draw(
            brush,
            position,
            null,
            color,
            rotation,
            new Vector2(brush.Width, brush.Height) * 0.5f,
            new Vector2(length * 2f / brush.Width, width * 2f / brush.Height),
            SpriteEffects.None,
            0f);
    }

    /// <summary>
    /// A soft crescent of light. Used for melee arcs and sweeps: the intensity
    /// falls off toward both ends so it never terminates in a visible edge.
    /// </summary>
    public static void ArcBand(
        SpriteBatch batch,
        Texture2D brush,
        Vector2 center,
        float radius,
        float startAngle,
        float sweep,
        float thickness,
        Color color,
        int samples = 0)
    {
        // Sample density follows arc length: too few dabs bead apart into dots,
        // too many at a short arc collapse into an undirected blob.
        int count = samples > 0
            ? samples
            : DabCount(MathF.Abs(sweep) * radius, thickness);

        for (int i = 0; i < count; i++)
        {
            float amount = (i + 0.5f) / count;
            float angle = startAngle + sweep * amount;
            // Fade both ends so the band dissolves rather than stopping.
            float taper = MathF.Sin(amount * MathHelper.Pi);
            Vector2 point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Vector2 tangent = new(-MathF.Sin(angle), MathF.Cos(angle));
            Streak(batch, brush, point, tangent, thickness * 1.2f, thickness * (0.35f + taper * 0.65f), color * taper);
        }
    }

    /// <summary>
    /// Dabs needed to keep a painted band continuous without over-stacking. Kept
    /// in one place so every soft shape has the same visual density.
    /// </summary>
    private static int DabCount(float length, float thickness) =>
        (int)MathHelper.Clamp(MathF.Ceiling(length / MathF.Max(1.5f, thickness * 0.34f)), 6f, 170f);

    /// <summary>
    /// A soft annulus. Replaces the hard slam ring: the light sits on the radius
    /// and bleeds inward and outward.
    /// </summary>
    public static void Ring(
        SpriteBatch batch,
        Texture2D brush,
        Vector2 center,
        float radius,
        float thickness,
        Color color,
        int samples = 0,
        float phase = 0f)
    {
        int count = samples > 0
            ? samples
            : DabCount(MathHelper.TwoPi * radius, thickness);

        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.TwoPi * i / count + phase;
            // Gentle per-sample variation keeps the ring from reading as geometry.
            float wobble = 0.84f + 0.16f * MathF.Sin(angle * 3f + phase * 2.4f);
            Vector2 point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Blob(batch, brush, point, thickness * wobble, color);
        }
    }

    /// <summary>
    /// Pressure gathering on the floor at a given reach.
    ///
    /// <see cref="Ring"/> stacked so many even dabs that it summed to a saturated,
    /// geometrically perfect neon donut — the single worst "interface over pixel
    /// art" offender left after Session 1. This paints the same reach as an
    /// uneven band of light: the radius breathes per angle, the density varies,
    /// and a few soft gaps stop the eye closing it into a drawn circle. The reach
    /// itself is still honest, because the slam hitbox is a circle in the same
    /// plane.
    /// </summary>
    public static void PressureBand(
        SpriteBatch batch,
        Texture2D brush,
        Vector2 center,
        float radius,
        float thickness,
        Color color,
        float phase,
        float irregularity = 0.11f)
    {
        int count = DabCount(MathHelper.TwoPi * radius, thickness * 1.55f);
        for (int i = 0; i < count; i++)
        {
            float angle = MathHelper.TwoPi * i / count;
            // Two incommensurate lobes: never repeats into a visible pattern.
            float wobble = MathF.Sin(angle * 3f + phase) * 0.62f + MathF.Sin(angle * 5f - phase * 1.7f) * 0.38f;
            float localRadius = radius * (1f + wobble * irregularity);
            // Soft gaps where the pressure has not gathered yet.
            float density = MathHelper.Clamp(0.42f + 0.58f * (0.5f + 0.5f * MathF.Sin(angle * 2f + phase * 0.8f)), 0f, 1f);
            Vector2 point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * localRadius;
            Blob(batch, brush, point, thickness * (0.7f + density * 0.5f), color * density);
        }
    }

    /// <summary>
    /// A soft directional lane. Replaces the Burning's rails and chevrons: the
    /// charge path is lit rather than fenced.
    /// </summary>
    public static void Lane(
        SpriteBatch batch,
        Texture2D brush,
        Vector2 origin,
        Vector2 direction,
        float length,
        float width,
        Color color,
        float headAmount,
        int samples = 16)
    {
        for (int i = 0; i < samples; i++)
        {
            float amount = (i + 0.5f) / samples;
            Vector2 point = origin + direction * (length * amount);
            // Brightest just ahead of the charge, fading out down the lane.
            float nearHead = 1f - MathHelper.Clamp(MathF.Abs(amount - headAmount) / 0.42f, 0f, 1f);
            float falloff = (1f - amount * 0.55f) * (0.32f + nearHead * 0.68f);
            Blob(batch, brush, point, width * (0.6f + nearHead * 0.55f), color * falloff);
        }
    }

    /// <summary>
    /// A soft pool on the ground. Used for pressure marks under an impending slam.
    /// </summary>
    public static void Pool(
        SpriteBatch batch,
        Texture2D brush,
        Vector2 center,
        float radiusX,
        float radiusY,
        Color color)
    {
        batch.Draw(
            brush,
            center,
            null,
            color,
            0f,
            new Vector2(brush.Width, brush.Height) * 0.5f,
            new Vector2(radiusX * 2f / brush.Width, radiusY * 2f / brush.Height),
            SpriteEffects.None,
            0f);
    }
}
