using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game;

public enum ArenaEnemyKind
{
    Hollow,
    Burning,
    Devourer
}

/// <summary>One push of a wave: how many enemies of each type appear together.</summary>
public readonly record struct ArenaPush(int Hollow, int Burning, int Devourer)
{
    public int Total => Hollow + Burning + Devourer;

    /// <summary>The enemies of this push in a fixed order (Hollow, then Burning, then Devourer).</summary>
    public IEnumerable<ArenaEnemyKind> Kinds() =>
        Enumerable.Repeat(ArenaEnemyKind.Hollow, Hollow)
            .Concat(Enumerable.Repeat(ArenaEnemyKind.Burning, Burning))
            .Concat(Enumerable.Repeat(ArenaEnemyKind.Devourer, Devourer));
}

/// <summary>
/// Layouts and spawn points of the arena waves (change <c>extend-arena-waves</c>). The
/// composition lives in <see cref="GameBalance.ArenaWaves"/>; waves 1 to 4 keep their
/// hand-placed layouts, later waves enter from fixed points along the arena edge.
/// </summary>
public static class ArenaWaves
{
    private const float SpawnPointInset = 80f;
    private const float SpawnSpread = 44f;

    public static IReadOnlyList<ArenaPush> Pushes(int waveNumber) => GameBalance.ArenaWaves[waveNumber - 1];

    public static bool HasAuthoredLayout(int waveNumber) => waveNumber is >= 1 and <= 4;

    /// <summary>Hand-placed first push of waves 1 to 4 as offsets from the arena centre.</summary>
    public static IReadOnlyList<(ArenaEnemyKind Kind, Vector2 Offset)> AuthoredLayout(int waveNumber) => waveNumber switch
    {
        1 =>
        [
            (ArenaEnemyKind.Hollow, new Vector2(360f, -195f)),
            (ArenaEnemyKind.Hollow, new Vector2(-390f, -125f)),
            (ArenaEnemyKind.Hollow, new Vector2(235f, 265f))
        ],
        2 =>
        [
            (ArenaEnemyKind.Hollow, new Vector2(-470f, -210f)),
            (ArenaEnemyKind.Hollow, new Vector2(430f, 235f)),
            (ArenaEnemyKind.Burning, new Vector2(445f, -170f)),
            (ArenaEnemyKind.Burning, new Vector2(-420f, 225f))
        ],
        3 =>
        [
            (ArenaEnemyKind.Hollow, new Vector2(-500f, -230f)),
            (ArenaEnemyKind.Hollow, new Vector2(480f, 235f)),
            (ArenaEnemyKind.Burning, new Vector2(420f, -250f)),
            (ArenaEnemyKind.Burning, new Vector2(-420f, 260f)),
            (ArenaEnemyKind.Devourer, new Vector2(560f, 10f))
        ],
        4 =>
        [
            (ArenaEnemyKind.Devourer, new Vector2(575f, -35f)),
            (ArenaEnemyKind.Burning, new Vector2(-500f, -265f)),
            (ArenaEnemyKind.Burning, new Vector2(-525f, 40f)),
            (ArenaEnemyKind.Burning, new Vector2(390f, 275f)),
            (ArenaEnemyKind.Hollow, new Vector2(455f, -245f)),
            (ArenaEnemyKind.Hollow, new Vector2(-320f, 285f))
        ],
        _ => []
    };

    /// <summary>Corners and side midpoints of the combat area, slightly inset.</summary>
    public static Vector2[] SpawnPoints(Rectangle combatBounds)
    {
        float left = combatBounds.Left + SpawnPointInset;
        float right = combatBounds.Right - SpawnPointInset;
        float top = combatBounds.Top + SpawnPointInset;
        float bottom = combatBounds.Bottom - SpawnPointInset;
        float midX = combatBounds.Center.X;
        float midY = combatBounds.Center.Y;
        return
        [
            new(left, top), new(midX, top), new(right, top),
            new(right, midY), new(right, bottom), new(midX, bottom),
            new(left, bottom), new(left, midY)
        ];
    }

    /// <summary>
    /// Positions for <paramref name="count"/> enemies: only points at least
    /// <see cref="GameBalance.ArenaSpawnMinPlayerDistance"/> from the player, farthest first,
    /// filled round-robin with a small fixed spread so enemies never stack.
    /// </summary>
    public static List<Vector2> ChooseSpawnPositions(Rectangle combatBounds, Vector2 playerPosition, int count)
    {
        float minimumSquared = GameBalance.ArenaSpawnMinPlayerDistance * GameBalance.ArenaSpawnMinPlayerDistance;
        Vector2[] ordered = SpawnPoints(combatBounds)
            .OrderByDescending(point => Vector2.DistanceSquared(point, playerPosition))
            .ToArray();
        Vector2[] usable = ordered.Where(point => Vector2.DistanceSquared(point, playerPosition) >= minimumSquared).ToArray();
        if (usable.Length == 0)
        {
            usable = [ordered[0]];
        }

        List<Vector2> positions = new(count);
        for (int i = 0; i < count; i++)
        {
            Vector2 point = usable[i % usable.Length];
            int ring = i / usable.Length;
            if (ring > 0)
            {
                // Pull later enemies on the same point towards the arena centre so they stay inside.
                point += Vector2.Normalize(combatBounds.Center.ToVector2() - point) * SpawnSpread * ring;
            }

            positions.Add(point);
        }

        return positions;
    }
}

/// <summary>
/// Tracks the pushes of one running wave. Pure logic so the timing rules are testable:
/// a push is due after <see cref="GameBalance.ArenaPushInterval"/> or as soon as at most
/// <see cref="GameBalance.ArenaPushEarlyAlive"/> enemies are left, and waits while it would
/// lift the field above <see cref="GameBalance.ArenaMaxAliveEnemies"/>.
/// </summary>
public sealed class ArenaWaveRun
{
    private readonly IReadOnlyList<ArenaPush> _pushes;
    private int _nextPush;
    private float _sinceLastPush;

    public ArenaWaveRun(IReadOnlyList<ArenaPush> pushes)
    {
        _pushes = pushes;
    }

    public static ArenaWaveRun Empty { get; } = new(Array.Empty<ArenaPush>());

    public bool AllPushesReleased => _nextPush >= _pushes.Count;

    public int PushesReleased => _nextPush;

    /// <summary>Releases the first push at wave start.</summary>
    public ArenaPush TakeFirst()
    {
        _nextPush = 1;
        _sinceLastPush = 0f;
        return _pushes[0];
    }

    /// <param name="aliveEnemies">Living enemies plus announced ones that have not appeared yet.</param>
    public bool TryTakeNext(float deltaTime, int aliveEnemies, out ArenaPush push)
    {
        push = default;
        if (AllPushesReleased)
        {
            return false;
        }

        _sinceLastPush += deltaTime;
        ArenaPush next = _pushes[_nextPush];
        bool due = _sinceLastPush >= GameBalance.ArenaPushInterval || aliveEnemies <= GameBalance.ArenaPushEarlyAlive;
        if (!due || aliveEnemies + next.Total > GameBalance.ArenaMaxAliveEnemies)
        {
            return false;
        }

        push = next;
        _nextPush++;
        _sinceLastPush = 0f;
        return true;
    }

    /// <summary>Drops the pushes that have not been released yet (debug <c>F6</c>).</summary>
    public void DiscardRemaining() => _nextPush = _pushes.Count;
}
