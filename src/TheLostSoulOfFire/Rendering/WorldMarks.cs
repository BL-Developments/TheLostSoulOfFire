using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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
}
