using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Why the Soul Cannon works (07_SOUL_CANNON): it cannot make the Flame of Death, the player feeds
/// his own into it. While it charges, motes of the core's flame leave the chest, run up to the
/// shoulder and down the arm into the reliquary chamber at the right hip (CORE → SHOULDER → ARM →
/// CANNON), faster and brighter as the charge grows; the chamber fills with light. Seen from
/// behind, the body hides the core, so the stream starts at the shoulder. Presentation only.
/// </summary>
public static class CannonFeed
{
    private const int Motes = 12;

    /// <summary>
    /// The reliquary chamber of the braced cannon, where its grilles are: by the right hip, just
    /// ahead of the grip (measured on the aim clip, CANNON_POINT: 14 units ahead, 13.5 to the right,
    /// 58 up uncharged and 56.5 at full charge, as the body sinks into the brace).
    /// </summary>
    public static Vector2 ChamberOf(Vector2 foot, Vector2 facing, float charge)
    {
        Vector2 right = new(-facing.Y, facing.X);
        Vector2 level = facing * 14f + right * 13.5f;
        float up = MathHelper.Lerp(58f, 56.5f, MathHelper.Clamp(charge, 0f, 1f));
        return foot + new Vector2(level.X, level.Y * FigureHeights.LevelSquash - up);
    }

    public static void Draw(SpriteBatch batch, Texture2D softSpot, Vector2 foot, Vector2 core, Vector2 facing, float charge, bool full, float time)
    {
        Vector2 right = new(-facing.Y, facing.X);
        Vector2 shoulder = core + new Vector2(right.X * 10f, right.Y * 10f * FigureHeights.LevelSquash - 13f);
        Vector2 chamber = ChamberOf(foot, facing, charge);
        // The core is hidden behind the body when the figure faces away; arm and chamber too, once
        // it turns its back to the camera, so then nothing is drawn over the back.
        float front = MathHelper.Clamp(0.55f + facing.Y, 0f, 1f);
        float start = front > 0.2f ? 0f : 0.45f;
        float seen = MathHelper.Clamp((facing.Y + 0.6f) / 0.5f, 0f, 1f);
        if (seen <= 0f)
        {
            return;
        }
        float speed = MathHelper.Lerp(0.9f, 2.4f, charge);
        Color flame = Color.Lerp(GameBalance.DeathFlame, GameBalance.DeathFlameBright, charge);
        Vector2 origin = new(softSpot.Width * 0.5f, softSpot.Height * 0.5f);

        for (int index = 0; index < Motes; index++)
        {
            float t = (index / (float)Motes + time * speed) % 1f;
            if (t < start)
            {
                continue;
            }
            // Quadratic curve core → shoulder → chamber, the shoulder as the control point.
            float u = 1f - t;
            Vector2 at = core * (u * u) + shoulder * (2f * u * t) + chamber * (t * t);
            float sway = MathF.Sin(time * 9f + index * 2.1f) * 1.5f;
            at += new Vector2(sway, 0f);
            float fade = MathHelper.Clamp((t - start) / 0.12f, 0f, 1f) * MathHelper.Clamp((1f - t) / 0.1f, 0f, 1f);
            float size = MathHelper.Lerp(3.2f, 2.2f, t) * (0.8f + 0.4f * charge);
            Color glow = flame * (0.55f * fade * (0.5f + 0.5f * charge) * seen);
            glow.A = 0;
            batch.Draw(softSpot, at, null, glow, 0f, origin, size * 3f / softSpot.Width, SpriteEffects.None, 0f);
            Color hot = GameBalance.SoulWhite * (0.45f * fade * charge * seen);
            hot.A = 0;
            batch.Draw(softSpot, at, null, hot, 0f, origin, size * 0.9f / softSpot.Width, SpriteEffects.None, 0f);
        }

        // The chamber fills: its windows glow brighter with every mote, blazing when full.
        float beat = full ? 0.85f + 0.15f * MathF.Sin(time * 22f) : 1f;
        Color fill = flame * ((0.25f + 0.55f * charge) * beat * seen);
        fill.A = 0;
        batch.Draw(softSpot, chamber, null, fill, 0f, origin, (10f + 8f * charge) * 2f / softSpot.Width, SpriteEffects.None, 0f);
        if (charge > 0.5f)
        {
            Color white = GameBalance.SoulWhite * ((charge - 0.5f) * (full ? 1.2f : 0.8f) * beat * seen);
            white.A = 0;
            batch.Draw(softSpot, chamber, null, white, 0f, origin, (4f + 3f * charge) * 2f / softSpot.Width, SpriteEffects.None, 0f);
        }
    }
}
