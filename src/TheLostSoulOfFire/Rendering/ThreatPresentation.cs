using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Draws critical enemy warnings above the scene and Soul Sense pass, so lighting
/// and reduced effects can never hide an incoming attack.
///
/// Every cue is painted with the feathered brush. The previous version outlined
/// wedges, rails, chevrons and rings with 2–3px vector strokes, which read as an
/// interface layer sitting on the pixel art rather than as Death Flame gathering
/// in the room. Readability now comes from intensity, growth and motion instead
/// of from a shiny edge.
/// </summary>
public static class ThreatPresentation
{
    public static void Draw(
        SpriteBatch batch,
        Texture2D brush,
        IReadOnlyList<Enemy> enemies,
        float time)
    {
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            switch (enemy)
            {
                case Hollow hollow:
                    DrawHollow(batch, brush, hollow, time);
                    break;
                case Burning burning:
                    DrawBurning(batch, brush, burning, time);
                    break;
                case Devourer devourer:
                    DrawDevourer(batch, brush, devourer, time);
                    break;
            }

            DrawSeveranceMark(batch, brush, enemy, time);
        }
    }

    private static void DrawHollow(SpriteBatch batch, Texture2D brush, Hollow hollow, float time)
    {
        if (hollow.State is not (HollowState.Telegraph or HollowState.Swipe))
        {
            return;
        }

        float aim = MathF.Atan2(hollow.FacingDirection.Y, hollow.FacingDirection.X);
        const float HalfSweep = 0.78f;

        if (hollow.State == HollowState.Telegraph)
        {
            // Death Flame gathers along the reach of the coming swipe. It grows
            // inward-out and brightens; it never draws a boundary.
            float progress = hollow.TelegraphProgress;
            float radius = MathHelper.Lerp(GameBalance.HollowSwipeRange * 0.72f, GameBalance.HollowSwipeRange * 1.02f, progress);
            float breathe = 0.88f + 0.12f * MathF.Sin(time * 17f);
            Color gather = GameBalance.DeathFlame * (0.12f + progress * 0.4f);
            Color inner = GameBalance.DeathFlameBright * (0.06f + progress * 0.26f);

            SoftShapes.ArcBand(batch, brush, hollow.Position, radius, aim - HalfSweep, HalfSweep * 2f,
                (10f + progress * 8f) * breathe, gather);
            SoftShapes.ArcBand(batch, brush, hollow.Position, radius, aim - HalfSweep * 0.7f, HalfSweep * 1.4f,
                (4f + progress * 4f) * breathe, inner);
            return;
        }

        // The strike itself: one bright, short-lived sweep of light.
        float strike = 1f - hollow.StrikeProgress;
        SoftShapes.ArcBand(batch, brush, hollow.Position, GameBalance.HollowSwipeRange * 1.02f,
            aim - HalfSweep * 1.08f, HalfSweep * 2.16f, 15f * strike, GameBalance.SoulWhite * (0.46f * strike));
        SoftShapes.ArcBand(batch, brush, hollow.Position, GameBalance.HollowSwipeRange * 1.02f,
            aim - HalfSweep * 1.08f, HalfSweep * 2.16f, 6f * strike, Color.White * (0.34f * strike));
    }

    private static void DrawBurning(SpriteBatch batch, Texture2D brush, Burning burning, float time)
    {
        Vector2 direction = SafeDirection(burning.ChargeDirection);

        if (burning.State == BurningState.Telegraph)
        {
            // The lane it intends to run is lit from within, brightest where the
            // charge is about to arrive.
            float progress = burning.TelegraphProgress;
            float laneLength = GameBalance.BurningChargeSpeed * GameBalance.BurningChargeDuration;
            float flicker = 0.84f + 0.16f * MathF.Sin(time * 23f);
            Color lane = GameBalance.DeathFlame * ((0.07f + progress * 0.2f) * flicker);
            Color core = GameBalance.DeathFlameBright * ((0.05f + progress * 0.16f) * flicker);

            SoftShapes.Lane(batch, brush, burning.Position, direction, laneLength, 34f, lane, progress * 0.85f, 18);
            SoftShapes.Lane(batch, brush, burning.Position, direction, laneLength, 14f, core, progress * 0.85f, 18);
            SoftShapes.Blob(batch, brush, burning.Position, 30f + progress * 26f,
                GameBalance.DeathFlameBright * (0.12f + progress * 0.3f));
            return;
        }

        if (burning.State == BurningState.Charge)
        {
            SoftShapes.Streak(batch, brush, burning.Position - direction * 46f, direction, 74f, 26f,
                GameBalance.DeathFlame * 0.34f);
            SoftShapes.Streak(batch, brush, burning.Position - direction * 30f, direction, 48f, 11f,
                GameBalance.SoulWhite * 0.3f);
        }
    }

    private static void DrawDevourer(SpriteBatch batch, Texture2D brush, Devourer devourer, float time)
    {
        if (devourer.State == DevourerState.SlamTelegraph)
        {
            // Pressure gathers under it and the reach closes inward. The ring is
            // a band of light with wobble, not a drawn circle.
            float progress = devourer.TelegraphProgress;
            float radius = MathHelper.Lerp(GameBalance.DevourerSlamRange * 1.16f, GameBalance.DevourerSlamRange * 0.94f, progress);
            float pulse = 0.86f + 0.14f * MathF.Sin(time * 13f);

            SoftShapes.Pool(batch, brush, devourer.Position, GameBalance.DevourerSlamRange * 0.95f,
                GameBalance.DevourerSlamRange * 0.44f, GameBalance.DeepViolet * (0.1f + progress * 0.2f));
            SoftShapes.Ring(batch, brush, devourer.Position, radius, (13f + progress * 9f) * pulse,
                GameBalance.DeathFlame * (0.07f + progress * 0.2f), 0, time * 0.6f);
            SoftShapes.Ring(batch, brush, devourer.Position, radius * 0.97f, (5f + progress * 4f) * pulse,
                GameBalance.DeathFlameBright * (0.05f + progress * 0.15f), 0, -time * 0.4f);
            return;
        }

        if (devourer.State == DevourerState.Slam)
        {
            float strike = 1f - devourer.StrikeProgress;
            SoftShapes.Ring(batch, brush, devourer.Position, GameBalance.DevourerSlamRange, 22f * strike,
                GameBalance.SoulWhite * (0.3f * strike), 0, time);
            SoftShapes.Pool(batch, brush, devourer.Position, GameBalance.DevourerSlamRange * 1.05f,
                GameBalance.DevourerSlamRange * 0.5f, Color.White * (0.16f * strike));
        }
    }

    /// <summary>
    /// The Anchor mark. It swells and then draws tight as the enemy's commitment
    /// closes, flaring white during the Severance read window. Taught by watching
    /// the enemy: no ring, no ticks, no prompt.
    /// </summary>
    private static void DrawSeveranceMark(SpriteBatch batch, Texture2D brush, Enemy enemy, float time)
    {
        float remaining = enemy.CommitmentRemaining;
        if (remaining < 0f || remaining > GameBalance.SeveranceMarkLead)
        {
            return;
        }

        Vector2 anchor = enemy.AnchorPosition;
        bool inWindow = remaining <= GameBalance.SeveranceReadTime;
        float closing = 1f - MathHelper.Clamp(remaining / GameBalance.SeveranceMarkLead, 0f, 1f);
        float radius = MathHelper.Lerp(40f, 15f, closing);
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 21f);

        if (inWindow)
        {
            // The Anchor is taut: a tight, hot point the Player can cut. Kept
            // deliberately small — it has to be found, not stared at, and it
            // must never blow out the enemy it belongs to.
            float heat = 0.62f + pulse * 0.38f;
            SoftShapes.Blob(batch, brush, anchor, radius * 1.5f, GameBalance.DeathFlameBright * (0.13f * heat));
            SoftShapes.Blob(batch, brush, anchor, radius * 0.78f, GameBalance.SoulWhite * (0.26f * heat));
            SoftShapes.Blob(batch, brush, anchor, radius * 0.34f, Color.White * (0.34f * heat));
            return;
        }

        SoftShapes.Blob(batch, brush, anchor, radius * 1.5f, GameBalance.DeathFlame * (0.08f + closing * 0.14f));
        SoftShapes.Blob(batch, brush, anchor, radius * 0.6f, GameBalance.DeathFlameBright * (0.06f + closing * 0.16f));
    }

    private static Vector2 SafeDirection(Vector2 direction) =>
        direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : Vector2.UnitX;
}
