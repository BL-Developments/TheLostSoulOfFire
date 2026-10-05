using System;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Entities;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Every enemy type the sandbox dev menu can spawn. The menu builds one entry per value, so a
/// new type only needs a value here and a case in <see cref="SandboxSpawner.Create"/>.
/// </summary>
public enum SandboxEnemyKind
{
    Hollow,
    Burning,
    Devourer,
    TrainingDummy
}

/// <summary>Creates sandbox enemies and picks where they appear; free of game state so it is testable.</summary>
public static class SandboxSpawner
{
    /// <summary>Distance from the player at which a spawned enemy appears.</summary>
    public const float SpawnDistance = 260f;

    /// <summary>A spawn never lands closer to the player than this, even near a wall.</summary>
    public const float MinPlayerDistance = 160f;

    /// <summary>Gap kept between a spawned enemy and the arena edge.</summary>
    public const float EdgeMargin = 16f;

    // Golden angle: consecutive spawns spread around the player instead of stacking.
    private const float AngleStep = 2.3999631f;

    // Turns in eighths of a circle, nearest first, tried when a wall pushes the spawn too close.
    private static readonly float[] FallbackTurns = [0f, 1f, -1f, 2f, -2f, 3f, -3f, 4f];

    public static string Label(SandboxEnemyKind kind) => kind switch
    {
        SandboxEnemyKind.Hollow => "HOLLOW",
        SandboxEnemyKind.Burning => "BURNING",
        SandboxEnemyKind.Devourer => "DEVOURER",
        _ => "TRAININGSPUPPE"
    };

    public static float Radius(SandboxEnemyKind kind) => kind switch
    {
        SandboxEnemyKind.Hollow => GameBalance.HollowRadius,
        SandboxEnemyKind.Burning => GameBalance.BurningRadius,
        SandboxEnemyKind.Devourer => GameBalance.DevourerRadius,
        _ => GameBalance.TrainingDummyRadius
    };

    public static bool IsKind(Enemy enemy, SandboxEnemyKind kind) => kind switch
    {
        SandboxEnemyKind.Hollow => enemy is Hollow,
        SandboxEnemyKind.Burning => enemy is Burning,
        SandboxEnemyKind.Devourer => enemy is Devourer,
        _ => enemy is TrainingDummy
    };

    public static Enemy Create(SandboxEnemyKind kind, Vector2 position, int seed) => kind switch
    {
        SandboxEnemyKind.Hollow => new Hollow(position, seed),
        SandboxEnemyKind.Burning => new Burning(position, seed),
        SandboxEnemyKind.Devourer => new Devourer(position),
        _ => new TrainingDummy(position)
    };

    /// <summary>
    /// A point <see cref="SpawnDistance"/> from the player, rotated per spawn, inside the combat
    /// bounds. Near a wall other directions are tried so the enemy keeps its distance.
    /// </summary>
    public static Vector2 ChoosePosition(Rectangle combatBounds, Vector2 playerPosition, int spawnIndex, float radius)
    {
        float angle = spawnIndex * AngleStep;
        Vector2 best = playerPosition;
        float bestDistance = -1f;
        foreach (float eighths in FallbackTurns)
        {
            float tryAngle = angle + eighths * MathF.PI / 4f;
            Vector2 candidate = Clamp(playerPosition + new Vector2(MathF.Cos(tryAngle), MathF.Sin(tryAngle)) * SpawnDistance, combatBounds, radius);
            float distance = Vector2.Distance(candidate, playerPosition);
            if (distance >= MinPlayerDistance)
            {
                return candidate;
            }
            if (distance > bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Vector2 Clamp(Vector2 point, Rectangle bounds, float radius)
    {
        float margin = radius + EdgeMargin;
        return new Vector2(
            MathHelper.Clamp(point.X, bounds.Left + margin, bounds.Right - margin),
            MathHelper.Clamp(point.Y, bounds.Top + margin, bounds.Bottom - margin));
    }
}
