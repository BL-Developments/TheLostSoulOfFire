using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public static class SoulfireLighting
{
    public static void DrawAntechamber(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        Matrix worldTransform,
        Player player,
        ParticleSystem particles,
        SoulFurnaceAntechamber antechamber,
        float presentationTime,
        float soulSenseAmount,
        float gateProgress,
        bool renderedPlayer = false)
    {
        renderer.BeginLighting(batch, worldTransform);
        float breathe = 0.88f + MathF.Sin(presentationTime * 4.6f) * 0.12f;
        antechamber.DrawLighting(batch, renderer, presentationTime, soulSenseAmount, gateProgress);
        renderer.GlowOffset = new Vector2(0f, -FigureHeights.Air);
        particles.DrawLighting(batch, renderer);
        renderer.GlowOffset = Vector2.Zero;
        DrawPlayerEnergy(batch, renderer, player, presentationTime, soulSenseAmount, breathe, renderedPlayer);
        batch.End();
    }

    public static void Draw(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        Matrix worldTransform,
        Player player,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<Soul> souls,
        IReadOnlyList<CannonShot> cannonShots,
        ParticleSystem particles,
        ArenaAtmosphere arenaAtmosphere,
        float presentationTime,
        float soulSenseAmount,
        bool endingComplete,
        Vector2 lifeFlamePosition,
        float lifeFlameAlpha,
        bool drawArenaFurnaces = true,
        bool renderedPlayer = false,
        Func<Enemy, bool>? renderedEnemy = null)
    {
        renderer.BeginLighting(batch, worldTransform);
        float breathe = 0.88f + MathF.Sin(presentationTime * 4.6f) * 0.12f;

        if (drawArenaFurnaces)
        {
            arenaAtmosphere.DrawLighting(batch, renderer, soulSenseAmount);
        }
        // Sparks and shots are drawn at body height; so is their light.
        renderer.GlowOffset = new Vector2(0f, -FigureHeights.Air);
        particles.DrawLighting(batch, renderer);
        DrawShotEnergy(batch, renderer, cannonShots);
        renderer.GlowOffset = Vector2.Zero;
        DrawSouls(batch, renderer, souls, soulSenseAmount, breathe);
        DrawEnemyEnergy(batch, renderer, enemies, soulSenseAmount, breathe, renderedEnemy);
        DrawChargeEnergy(batch, renderer, player, renderedPlayer);
        DrawPlayerEnergy(batch, renderer, player, presentationTime, soulSenseAmount, breathe, renderedPlayer);
        DrawEndingLight(batch, renderer, endingComplete, lifeFlamePosition, lifeFlameAlpha);

        batch.End();
    }

    private static void DrawSouls(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        IReadOnlyList<Soul> souls,
        float soulSenseAmount,
        float breathe)
    {
        foreach (Soul soul in souls)
        {
            if (soul.State is SoulState.Released or SoulState.Consumed)
            {
                continue;
            }

            float radius = soul.State == SoulState.Residue
                ? SoulfireRenderSettings.SoulGlowRadius * 0.48f
                : SoulfireRenderSettings.SoulGlowRadius * breathe;
            float intensity = SoulfireRenderSettings.SoulGlowIntensity * MathHelper.Lerp(1f, 1.42f, soulSenseAmount);
            renderer.DrawGlow(batch, soul.Position, radius, GameBalance.SoulWhite, intensity);
            renderer.DrawGlow(batch, soul.Position, radius * 0.48f, GameBalance.DeathFlameBright, intensity * 0.72f);
        }
    }

    private static void DrawEnemyEnergy(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        IReadOnlyList<Enemy> enemies,
        float soulSenseAmount,
        float breathe,
        Func<Enemy, bool>? renderedEnemy)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy is Burning burning && burning.State != BurningState.Dead)
            {
                float fractureIntensity = MathHelper.Lerp(0.11f, 0.27f, soulSenseAmount);
                foreach (Vector2 fracture in burning.GetFracturePositions())
                {
                    renderer.DrawGlow(batch, burning.DrawnFracture(fracture), 30f * breathe, GameBalance.DeathFlame, fractureIntensity);
                }

                if (burning.State is BurningState.Charge or BurningState.Detonating or BurningState.Telegraph)
                {
                    renderer.DrawGlow(batch, burning.DrawnCore, 78f, GameBalance.DeathFlameBright, burning.State == BurningState.Telegraph ? 0.16f : 0.28f);
                }
            }

            if (soulSenseAmount <= 0.001f)
            {
                continue;
            }

            switch (enemy)
            {
                case Hollow hollow when hollow.State is not (HollowState.Dying or HollowState.Dead):
                    Vector2 core = renderedEnemy?.Invoke(hollow) == true
                        ? hollow.CorePosition - new Vector2(0f, FigureHeights.Air)
                        : hollow.CorePosition;
                    renderer.DrawGlow(batch, core, 50f * breathe, GameBalance.SoulWhite, 0.38f * soulSenseAmount);
                    break;
                case Devourer devourer when devourer.State != DevourerState.Dead:
                    float torsoIntensity = 0.22f + devourer.ConsumedSoulCount * 0.07f;
                    renderer.DrawGlow(batch, devourer.DrawnTorso, 66f, GameBalance.DeathFlameBright, torsoIntensity * soulSenseAmount);
                    break;
            }
        }
    }

    private static void DrawShotEnergy(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        IReadOnlyList<CannonShot> cannonShots)
    {
        foreach (CannonShot shot in cannonShots)
        {
            if (shot.IsFinished)
            {
                continue;
            }

            float radius = MathHelper.Lerp(38f, 72f, shot.Charge);
            Color color = shot.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
            renderer.DrawGlow(batch, shot.Position, radius, color, 0.25f + shot.Charge * 0.24f);
        }
    }

    private static void DrawChargeEnergy(SpriteBatch batch, SoulfireRenderer renderer, Player player, bool renderedPlayer)
    {
        if (player.Cannon.State != SoulCannonState.Charging)
        {
            return;
        }

        float charge = player.Cannon.ChargeProgress;
        Vector2 muzzle = renderedPlayer
            ? FigureHeights.MuzzleOf(player.Position, player.FacingDirection, charge)
            : player.Position + player.FacingDirection * 74f;
        float chargeRadius = SoulfireRenderSettings.CannonGlowRadius * MathHelper.Lerp(0.68f, 1.55f, charge);
        Color chargeColor = player.Cannon.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        float intensity = SoulfireRenderSettings.CannonGlowIntensity * MathHelper.Lerp(0.55f, 1.35f, charge);
        renderer.DrawGlow(batch, muzzle, chargeRadius, chargeColor, intensity);
        if (player.Cannon.IsFullCharge)
        {
            renderer.DrawGlow(batch, muzzle, chargeRadius * 0.48f, GameBalance.SoulWhite, 0.58f);
        }
    }

    private static void DrawPlayerEnergy(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        Player player,
        float presentationTime,
        float soulSenseAmount,
        float breathe,
        bool renderedPlayer)
    {
        Vector2 playerCore = renderedPlayer
            ? player.Position - new Vector2(0f, player.DrawnCoreHeight)
            : player.Position + player.FacingDirection * 2f;
        if (player.IsDead)
        {
            // The core's light sinks with the falling body (death clip, 1.3 s) and flares when the
            // Death Flame takes it (0.85 s after the fall begins, CinematicPresentation).
            Vector2 fallen = renderedPlayer ? FigureHeights.FallenChestOf(player.Position, player.FacingDirection) : player.Position;
            float falling = renderedPlayer ? MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(player.SinceDeath / 1.1f, 0f, 1f)) : 1f;
            Vector2 standing = player.Position - new Vector2(0f, FigureHeights.Core);
            Vector2 light = Vector2.Lerp(standing, fallen, falling);
            float taken = renderedPlayer ? MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((player.SinceDeath - 0.85f) / 0.3f, 0f, 1f)) : 1f;
            renderer.DrawGlow(batch, light, SoulfireRenderSettings.PlayerCoreGlowRadius * breathe, GameBalance.DeathFlameBright,
                SoulfireRenderSettings.PlayerCoreGlowIntensity * (1f - taken));
            renderer.DrawGlow(
                batch,
                light,
                SoulfireRenderSettings.DeathFlameGlowRadius * breathe,
                GameBalance.DeathFlame,
                SoulfireRenderSettings.DeathFlameGlowIntensity * taken);
            renderer.DrawGlow(batch, light, 38f, GameBalance.SoulWhite, 0.26f * taken);
            return;
        }

        renderer.DrawGlow(
            batch,
            playerCore,
            SoulfireRenderSettings.PlayerCoreGlowRadius * breathe,
            GameBalance.DeathFlameBright,
            SoulfireRenderSettings.PlayerCoreGlowIntensity);

        // The swing's Death Flame lights what it passes: the floor and anyone close to the blade.
        if (renderedPlayer && player.Scythe.TryGetFlameLight(player.Position, out Vector2 blade, out float flame))
        {
            renderer.DrawGlow(batch, blade, 96f, GameBalance.DeathFlame, flame);
            renderer.DrawGlow(batch, blade, 40f, GameBalance.DeathFlameBright, flame * 0.6f);
        }

        if (soulSenseAmount > 0.001f)
        {
            Vector2 eye = renderedPlayer
                ? player.Position - new Vector2(0f, FigureHeights.Eyes)
                : player.Position + player.FacingDirection * 26f;
            renderer.DrawGlow(batch, eye, 34f, GameBalance.SoulWhite, 0.25f * soulSenseAmount);
        }

        if (player.IsResonanceReady)
        {
            renderer.DrawGlow(
                batch,
                playerCore,
                SoulfireRenderSettings.ReadyCoreGlowRadius * breathe,
                GameBalance.SoulWhite,
                SoulfireRenderSettings.ReadyCoreGlowIntensity);
        }

        if (player.ResonanceActive)
        {
            float resonancePulse = 0.88f + MathF.Sin(presentationTime * 7.2f) * 0.12f;
            renderer.DrawGlow(
                batch,
                playerCore,
                SoulfireRenderSettings.ResonanceGlowRadius * resonancePulse,
                GameBalance.DeathFlame,
                SoulfireRenderSettings.ResonanceGlowIntensity);
            renderer.DrawGlow(batch, playerCore, 58f * resonancePulse, GameBalance.SoulWhite, 0.46f);
        }
    }

    private static void DrawEndingLight(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        bool endingComplete,
        Vector2 lifeFlamePosition,
        float lifeFlameAlpha)
    {
        if (!endingComplete || lifeFlameAlpha <= 0f)
        {
            return;
        }

        // The only warm light of the game: it fills the furnace mouth, warms the bricks around it
        // and spills across the floor toward the figure (whose lit sprite picks it up as well).
        renderer.DrawGlow(batch, lifeFlamePosition, 64f, new Color(255, 214, 150), 0.5f * lifeFlameAlpha);
        renderer.DrawGlow(batch, lifeFlamePosition + new Vector2(0f, 26f), 200f, new Color(255, 146, 58), 0.42f * lifeFlameAlpha);
        renderer.DrawGlow(batch, lifeFlamePosition + new Vector2(0f, 110f), 460f, new Color(255, 112, 36), 0.24f * lifeFlameAlpha);
    }
}
