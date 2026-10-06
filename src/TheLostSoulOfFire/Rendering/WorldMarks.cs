using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Telegraphs and tethers in the world as soft light (tools/visuals/vfx_kit.py, "mark_*"): rings,
/// a swipe arc, a charge lane with chevrons, a beam. Each keeps exactly the geometry the old line
/// drawings had (radius, angle, length), so what a telegraph tells the player is unchanged; only
/// its look is. Without the textures every mark falls back to the flat shape.
/// </summary>
public static class WorldMarks
{
    private const float RingRadius = 116f;

    private static Texture2D? _ringThin;
    private static Texture2D? _ringBold;
    private static Texture2D? _arc;
    private static Texture2D? _lane;
    private static Texture2D? _beam;

    public static void Load(ContentManager content)
    {
        try
        {
            _ringThin = content.Load<Texture2D>("Textures/Effects/mark_ring_thin");
            _ringBold = content.Load<Texture2D>("Textures/Effects/mark_ring_bold");
            _arc = content.Load<Texture2D>("Textures/Effects/mark_arc");
            _lane = content.Load<Texture2D>("Textures/Effects/mark_lane");
            _beam = content.Load<Texture2D>("Textures/Effects/mark_beam");
        }
        catch (ContentLoadException)
        {
            _ringThin = null;
        }
    }

    private static bool Loaded => _ringThin is not null;

    /// <summary>A ring of light; <paramref name="bold"/> for the moment of impact.</summary>
    public static void Ring(SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, Color color, bool bold = false, float fallbackThickness = 4f)
    {
        if (!Loaded)
        {
            batch.DrawCircle(pixel, center, radius, color, fallbackThickness, 32);
            return;
        }

        Texture2D texture = bold ? _ringBold! : _ringThin!;
        float scale = radius / RingRadius;
        batch.Draw(texture, center, null, color, 0f, new Vector2(texture.Width, texture.Height) * 0.5f, scale, SpriteEffects.None, 0f);
    }

    /// <summary>An arc of <paramref name="span"/> radians centred on <paramref name="angle"/>.</summary>
    public static void Arc(SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, float angle, float span, Color color, float fallbackThickness = 4f)
    {
        if (!Loaded)
        {
            batch.DrawArc(pixel, center, radius, angle - span * 0.5f, span, color, fallbackThickness, 18);
            return;
        }

        // The texture spans 1.6 rad, as wide as every arc the game telegraphs (1.5–1.6 rad).
        float scale = radius / RingRadius;
        batch.Draw(_arc!, center, null, color, angle, new Vector2(_arc!.Width, _arc.Height) * 0.5f, scale, SpriteEffects.None, 0f);
    }

    /// <summary>A lane from <paramref name="from"/> along <paramref name="direction"/>, chevrons pointing ahead.</summary>
    public static void Lane(SpriteBatch batch, Texture2D pixel, Vector2 from, Vector2 direction, float length, float width, Color color)
    {
        if (!Loaded)
        {
            batch.DrawLine(pixel, from, from + direction * length, color, 4f);
            return;
        }

        float angle = MathF.Atan2(direction.Y, direction.X);
        Vector2 scale = new(length / _lane!.Width, width / _lane.Height);
        batch.Draw(_lane, from, null, color, angle, new Vector2(0f, _lane.Height * 0.5f), scale, SpriteEffects.None, 0f);
    }

    /// <summary>A beam of light between two points.</summary>
    public static void Beam(SpriteBatch batch, Texture2D pixel, Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length < 0.5f)
        {
            return;
        }

        if (!Loaded)
        {
            batch.DrawLine(pixel, from, to, color, MathF.Max(2f, width * 0.3f));
            return;
        }

        float angle = MathF.Atan2(delta.Y, delta.X);
        Vector2 scale = new(length / _beam!.Width, width / _beam.Height);
        batch.Draw(_beam, from, null, color, angle, new Vector2(0f, _beam.Height * 0.5f), scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Light drawn from <paramref name="from"/> into <paramref name="to"/> (a soul pulled away) in
    /// place of a beam: soft motes that bow out a little on the way, speed up and shrink as they
    /// arrive. No line. Colours are added onto an alpha-blended batch.
    /// </summary>
    public static void Stream(SpriteBatch batch, Texture2D softSpot, Vector2 from, Vector2 to, float time, float amount, Color color,
        float speed = 150f, float spacing = 14f, int seed = 0)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length < 4f || amount <= 0.001f)
        {
            return;
        }

        Vector2 along = delta / length;
        Vector2 side = new(-along.Y, along.X);
        Vector2 origin = new(softSpot.Width * 0.5f, softSpot.Height * 0.5f);
        int count = (int)(length / spacing) + 2;
        float travel = time * speed / length;
        for (int index = 0; index < count; index++)
        {
            float hash = MathF.Sin((index + seed * 17) * 12.9898f) * 43758.5453f;
            hash -= MathF.Floor(hash);
            float t = (index / (float)count + travel + hash * 0.04f) % 1f;
            // Slow where the motes leave, fast where they arrive.
            float reach = MathF.Pow(t, 1.6f);
            float bow = MathF.Sin(reach * MathF.PI) * (hash - 0.5f) * 18f;
            Vector2 at = from + along * (reach * length) + side * bow;
            float fade = MathHelper.Clamp(t / 0.15f, 0f, 1f) * MathHelper.Clamp((1f - t) / 0.12f, 0f, 1f);
            float size = MathHelper.Lerp(7f, 2.5f, reach) * (0.8f + hash * 0.4f);
            Color glow = color * (0.5f * fade * amount);
            glow.A = 0;
            batch.Draw(softSpot, at, null, glow, 0f, origin, size * 2.6f / softSpot.Width, SpriteEffects.None, 0f);
            Color core = GameBalance.SoulWhite * (0.3f * fade * amount);
            core.A = 0;
            batch.Draw(softSpot, at, null, core, 0f, origin, size * 0.9f / softSpot.Width, SpriteEffects.None, 0f);
        }
    }

    /// <summary>A soft glow of light at <paramref name="center"/> (added onto an alpha-blended batch).</summary>
    public static void Glow(SpriteBatch batch, Texture2D softSpot, Vector2 center, float radius, Color color)
    {
        color.A = 0;
        batch.Draw(softSpot, center, null, color, 0f, new Vector2(softSpot.Width, softSpot.Height) * 0.5f,
            radius * 2f / softSpot.Width, SpriteEffects.None, 0f);
    }
}
