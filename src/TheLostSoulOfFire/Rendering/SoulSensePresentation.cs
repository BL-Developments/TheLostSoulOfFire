using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Owns the visual transition into the hidden soul layer. Gameplay continues to use
/// Player.SoulSenseActive; these curves exist only to stage world recession and soul emergence.
/// </summary>
public sealed class SoulSensePresentation
{
    private const float ActivationDuration = 0.25f;
    private const float DeactivationDuration = 0.15f;

    private static readonly Vector2[][] TracePaths =
    [
        [
            new(178f, 758f), new(276f, 732f), new(382f, 686f), new(492f, 664f),
            new(612f, 690f), new(735f, 704f), new(842f, 683f), new(955f, 648f)
        ],
        [
            new(948f, 232f), new(1068f, 248f), new(1175f, 296f), new(1278f, 346f),
            new(1390f, 358f), new(1508f, 321f), new(1627f, 334f)
        ],
        [
            new(356f, 268f), new(454f, 292f), new(548f, 342f), new(650f, 375f),
            new(760f, 361f), new(858f, 390f)
        ]
    ];

    private float _transition;

    /// <summary>Early curve: the physical scene becomes quieter in roughly the first 100 ms.</summary>
    public float WorldSuppression => SmoothStep(0f, 0.42f, _transition);

    /// <summary>Delayed curve: supernatural information resolves after the world starts receding.</summary>
    public float SoulEmergence => SmoothStep(0.36f, 1f, _transition);

    public void Update(float deltaTime, bool active)
    {
        float duration = active ? ActivationDuration : DeactivationDuration;
        float target = active ? 1f : 0f;
        _transition = MoveTowards(_transition, target, MathF.Max(0f, deltaTime) / duration);
    }

    public void Reset() => _transition = 0f;

    public void DrawSoulLayer(
        SpriteBatch batch,
        Texture2D pixel,
        Matrix worldTransform,
        Player player,
        IReadOnlyList<Enemy> enemies,
        IReadOnlyList<Soul> souls,
        float presentationTime,
        ArtAssets? art = null)
    {
        float amount = SoulEmergence;
        if (amount <= 0.001f)
        {
            return;
        }

        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            transformMatrix: worldTransform);

        DrawTraces(batch, pixel, presentationTime, amount, art);
        DrawSouls(batch, pixel, souls, presentationTime, amount, art);
        DrawEnemySouls(batch, pixel, enemies, presentationTime, amount, art);
        DrawPlayerResponse(batch, pixel, player, presentationTime, amount, art?.IsRendered(VisualIds.Player) == true);

        batch.End();
    }

    /// <summary>
    /// A trail of soul residue along <paramref name="path"/> as soft light, not a line: small motes
    /// every few units, each flickering on its own and drifting slowly along the way the person
    /// went, so the echo reads as something left behind. Shared by every Soul Sense trace.
    /// </summary>
    /// <remarks>
    /// <paramref name="additiveBatch"/>: true when the batch already blends additively by source
    /// alpha (BlendState.Additive); otherwise light is added through premultiplied colours with
    /// alpha 0 in an alpha-blended batch.
    /// </remarks>
    public static void DrawResidueTrail(SpriteBatch batch, Texture2D softSpot, IReadOnlyList<Vector2> path, float time, float amount,
        Color color, int seed = 0, float spacing = 18f, bool additiveBatch = false)
    {
        if (path.Count < 2 || amount <= 0.001f)
        {
            return;
        }

        Vector2 origin = new(softSpot.Width * 0.5f, softSpot.Height * 0.5f);
        float drift = time * 9f;
        int index = 0;
        for (int segment = 0; segment < path.Count - 1; segment++)
        {
            Vector2 start = path[segment];
            Vector2 delta = path[segment + 1] - start;
            float length = delta.Length();
            if (length < 1f)
            {
                continue;
            }
            Vector2 along = delta / length;
            Vector2 side = new(-along.Y, along.X);
            for (float d = (drift + segment * 7f) % spacing; d < length; d += spacing, index++)
            {
                float hash = MathF.Sin((index + seed * 31) * 12.9898f) * 43758.5453f;
                hash -= MathF.Floor(hash);
                float flicker = 0.55f + 0.45f * MathF.Sin(time * (2.4f + hash * 2f) + hash * 6.3f);
                // Fade in where a mote enters a segment and out where it leaves it, so the drift
                // never pops.
                float edge = MathHelper.Clamp(MathF.Min(d, length - d) / 10f, 0f, 1f);
                Vector2 at = start + along * d + side * ((hash - 0.5f) * 7f);
                float size = 3.5f + hash * 2.5f;
                Color glow = color * (0.28f * flicker * edge * amount);
                if (!additiveBatch) glow.A = 0;
                batch.Draw(softSpot, at, null, glow, 0f, origin, size * 2.6f / softSpot.Width, SpriteEffects.None, 0f);
                Color core = GameBalance.SoulWhite * (0.12f * flicker * edge * amount);
                if (!additiveBatch) core.A = 0;
                batch.Draw(softSpot, at, null, core, 0f, origin, size * 0.9f / softSpot.Width, SpriteEffects.None, 0f);
            }
        }
    }

    private static void DrawTraces(SpriteBatch batch, Texture2D pixel, float time, float amount, ArtAssets? art)
    {
        float breathe = 0.82f + MathF.Sin(time * 2.1f) * 0.18f;
        for (int pathIndex = 0; pathIndex < TracePaths.Length; pathIndex++)
        {
            Vector2[] path = TracePaths[pathIndex];
            if (art is not null)
            {
                DrawResidueTrail(batch, art.SoftSpot, path, time, amount * breathe, GameBalance.SoulSenseTrace, pathIndex);
            }
            else
            {
                for (int segment = 0; segment < path.Length - 1; segment++)
                {
                    WorldMarks.Beam(batch, pixel, path[segment], path[segment + 1], 12f, GameBalance.SoulSenseTrace * (0.42f * breathe * amount));
                }
            }

            for (int node = 1; node < path.Length - 1; node += 2)
            {
                float nodePulse = 0.7f + 0.3f * MathF.Sin(time * 2.7f + pathIndex * 1.9f + node);
                art?.DrawSoftSpot(batch, path[node], new Vector2(8f + nodePulse * 2f), Light(GameBalance.SoulSenseTrace, 0.2f * amount));
            }

            float travel = (time * 0.055f + pathIndex * 0.31f) % 1f;
            Vector2 mote = PointAlongPath(path, travel);
            art?.DrawSoftSpot(batch, mote, new Vector2(12f), Light(GameBalance.DeathFlame, 0.25f * amount));
            art?.DrawSoftSpot(batch, mote, new Vector2(3.5f), Light(GameBalance.SoulWhite, 0.4f * amount));
        }
    }

    private static void DrawSouls(
        SpriteBatch batch,
        Texture2D pixel,
        IReadOnlyList<Soul> souls,
        float time,
        float amount,
        ArtAssets? art)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 4.4f);
        foreach (Soul soul in souls)
        {
            if (soul.State is SoulState.Released or SoulState.Consumed)
            {
                continue;
            }

            float scale = soul.State == SoulState.Residue ? 0.55f : 1f;
            float urgency = soul.State == SoulState.BeingDevoured ? 1.2f : 1f;
            if (art is null)
            {
                WorldMarks.Ring(batch, pixel, soul.Position, (20f + pulse * 4f) * scale * urgency, GameBalance.DeathFlameBright * (0.6f * amount), urgency > 1f, 2f);
            }
            art?.DrawSoftSpot(batch, soul.Position, new Vector2((20f + pulse * 4f) * scale * urgency), Light(GameBalance.DeathFlameBright, 0.18f * amount));
            art?.DrawSoftSpot(batch, soul.Position, new Vector2(4f * scale), Light(GameBalance.SoulWhite, 0.4f * amount));
        }
    }

    private static void DrawEnemySouls(
        SpriteBatch batch,
        Texture2D pixel,
        IReadOnlyList<Enemy> enemies,
        float time,
        float amount,
        ArtAssets? art)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 5.2f);
        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            switch (enemy)
            {
                case Hollow hollow:
                    // A rendered Hollow carries its core in the chest, where shots fly (FigureHeights.Air).
                    Vector2 core = art?.IsRendered(hollow.VisualId) == true
                        ? hollow.CorePosition - new Vector2(0f, FigureHeights.Air)
                        : hollow.CorePosition;
                    DrawCriticalCore(batch, pixel, core, 13f, pulse, amount, art);
                    break;

                case Burning burning:
                    foreach (Vector2 gameplayFracture in burning.GetFracturePositions())
                    {
                        Vector2 fracture = burning.DrawnFracture(gameplayFracture);
                        if (art is not null)
                        {
                            // A crack that burns from within: a hot point in a pulsing violet glow.
                            art.DrawSoftSpot(batch, fracture, new Vector2(11f + pulse * 3f), Light(GameBalance.DeathFlameBright, 0.3f * amount));
                            art.DrawSoftSpot(batch, fracture, new Vector2(3f), Light(GameBalance.SoulWhite, 0.5f * amount));
                            continue;
                        }
                        batch.FillCircle(pixel, fracture, 9f, GameBalance.DeepViolet * (0.6f * amount));
                        WorldMarks.Ring(batch, pixel, fracture, 8f + pulse * 2f, GameBalance.DeathFlameBright * (0.7f * amount), true, 2f);
                        batch.FillCircle(pixel, fracture, 4f, GameBalance.SoulWhite * (0.98f * amount));
                    }
                    break;

                case Devourer devourer:
                    DrawDevourerSoul(batch, pixel, devourer, time, pulse, amount, art);
                    break;
            }
        }
    }

    private static void DrawCriticalCore(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 position,
        float radius,
        float pulse,
        float amount,
        ArtAssets? art = null)
    {
        if (art is not null)
        {
            // The weak point as light: a pulsing violet glow around a white-hot centre.
            art.DrawSoftSpot(batch, position, new Vector2(radius * 1.3f + pulse * 3f), Light(GameBalance.DeathFlameBright, 0.28f * amount));
            art.DrawSoftSpot(batch, position, new Vector2(3.5f), Light(GameBalance.SoulWhite, 0.5f * amount));
            return;
        }
        batch.FillCircle(pixel, position, radius, GameBalance.DeepViolet * (0.72f * amount));
        WorldMarks.Ring(batch, pixel, position, radius + 3f + pulse * 2f, GameBalance.DeathFlameBright * (0.7f * amount), true, 2f);
        batch.FillCircle(pixel, position, 8f, GameBalance.DeathFlameBright * (0.9f * amount));
        batch.FillCircle(pixel, position, 5f, GameBalance.SoulWhite * amount);
    }

    private static void DrawDevourerSoul(
        SpriteBatch batch,
        Texture2D pixel,
        Devourer devourer,
        float time,
        float pulse,
        float amount,
        ArtAssets? art = null)
    {
        Vector2 torso = devourer.DrawnTorso;
        if (art is not null)
        {
            // The prison: a dark violet glow in the torso with the trapped souls circling in it.
            art.DrawSoftSpot(batch, torso, new Vector2(24f + pulse * 3f), Light(GameBalance.DeathFlame, 0.25f * amount));
            art.DrawSoftSpot(batch, torso, new Vector2(3.5f), Light(GameBalance.SoulWhite, 0.45f * amount));
        }
        else
        {
            batch.FillCircle(pixel, torso, 22f + pulse * 2f, GameBalance.DeepViolet * (0.48f * amount));
            batch.FillCircle(pixel, torso, 5f, GameBalance.SoulWhite * (0.94f * amount));
        }

        for (int index = 0; index < devourer.ConsumedSoulCount; index++)
        {
            float angle = time * (1.05f + index * 0.12f) + index * MathHelper.TwoPi / devourer.ConsumedSoulCount;
            Vector2 trapped = torso + new Vector2(MathF.Cos(angle) * 16f, MathF.Sin(angle) * 11f);
            if (art is not null)
            {
                art.DrawSoftSpot(batch, trapped, new Vector2(6f), Light(GameBalance.DeathFlameBright, 0.3f * amount));
                art.DrawSoftSpot(batch, trapped, new Vector2(2f), Light(GameBalance.SoulWhite, 0.45f * amount));
                continue;
            }
            batch.FillCircle(pixel, trapped, 5f, GameBalance.DeathFlameBright * (0.78f * amount));
            batch.FillCircle(pixel, trapped, 2f, GameBalance.SoulWhite * amount);
        }
    }

    /// <summary>Light added over the scene: a premultiplied colour with no coverage.</summary>
    private static Color Light(Color color, float amount)
    {
        Color light = color * MathHelper.Clamp(amount, 0f, 1f);
        light.A = 0;
        return light;
    }

    private static void DrawPlayerResponse(
        SpriteBatch batch,
        Texture2D pixel,
        Player player,
        float time,
        float amount,
        bool rendered)
    {
        if (player.IsDead)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(time * 4.8f);
        Vector2 core = rendered ? player.Position - new Vector2(0f, player.DrawnCoreHeight) : player.Position + player.FacingDirection * 2f;
        if (!rendered)
        {
            WorldMarks.Ring(batch, pixel, core, 11f + pulse * 2f, GameBalance.DeathFlameBright * (0.45f * amount), false, 1.5f);
        }
        if (!rendered)
        {
            // The rendered figure's eyes burn in Player.Draw.
            Vector2 eye = player.Position + player.FacingDirection * 26f;
            batch.FillCircle(pixel, eye, 2f, GameBalance.SoulWhite * (0.9f * amount));
        }
    }

    private static Vector2 PointAlongPath(Vector2[] path, float amount)
    {
        float totalLength = 0f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            totalLength += Vector2.Distance(path[i], path[i + 1]);
        }

        float target = totalLength * MathHelper.Clamp(amount, 0f, 1f);
        for (int i = 0; i < path.Length - 1; i++)
        {
            float length = Vector2.Distance(path[i], path[i + 1]);
            if (target <= length)
            {
                return Vector2.Lerp(path[i], path[i + 1], length <= 0f ? 0f : target / length);
            }
            target -= length;
        }

        return path[^1];
    }

    private static float SmoothStep(float minimum, float maximum, float value)
    {
        float amount = MathHelper.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
        return amount * amount * (3f - 2f * amount);
    }

    private static float MoveTowards(float current, float target, float maximumDelta)
    {
        if (MathF.Abs(target - current) <= maximumDelta)
        {
            return target;
        }

        return current + MathF.Sign(target - current) * maximumDelta;
    }
}
