using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Entities;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Decides which brother each manifestation is coming for.
///
/// With one Warden this is a no-op. With two, naive "attack the nearest" produces
/// the two failures that make shared-screen co-op feel cheap: enemies flicking
/// between targets every frame when the brothers cross, and telegraphs that were
/// aimed at one player landing somewhere else because the target changed
/// mid-commitment.
///
/// The rules are therefore:
///   1. a committed enemy never re-targets — the attack you were shown is the
///      attack that resolves;
///   2. a target is held for at least <see cref="GameBalance.TargetCommitTime"/>;
///   3. after that, a switch needs a clear distance advantage, not a tie;
///   4. downed Wardens are never chosen, so nobody is finished off while helpless.
/// </summary>
public sealed class TargetDirector
{
    private readonly Dictionary<Enemy, Entry> _entries = [];
    private readonly List<Enemy> _stale = [];
    private WardenField _wardens = new();

    private sealed class Entry
    {
        public Player Target = null!;
        public float HoldRemaining;
    }

    public void Reset() => _entries.Clear();

    public void Update(float deltaTime, IReadOnlyList<Enemy> enemies, WardenField wardens)
    {
        _wardens = wardens;
        PruneFinished(enemies);

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            if (!_entries.TryGetValue(enemy, out Entry entry))
            {
                entry = new Entry();
                _entries[enemy] = entry;
            }

            entry.HoldRemaining = MathHelper.Max(0f, entry.HoldRemaining - deltaTime);

            bool committed = enemy.CommitmentRemaining >= 0f;
            bool targetUsable = entry.Target is not null && entry.Target.CanBeTargeted;

            if (committed && targetUsable)
            {
                continue;
            }

            if (targetUsable && entry.HoldRemaining > 0f)
            {
                continue;
            }

            Player chosen = Choose(enemy.Position, entry.Target, targetUsable);
            if (chosen is null)
            {
                continue;
            }

            if (!ReferenceEquals(chosen, entry.Target))
            {
                entry.Target = chosen;
                entry.HoldRemaining = GameBalance.TargetCommitTime;
            }
            else
            {
                entry.HoldRemaining = GameBalance.TargetCommitTime * 0.5f;
            }
        }
    }

    /// <summary>The Warden this enemy is committed to, falling back to the nearest.</summary>
    public Player TargetFor(Enemy enemy, WardenField wardens)
    {
        if (_entries.TryGetValue(enemy, out Entry entry) && entry.Target is not null && entry.Target.CanBeTargeted)
        {
            return entry.Target;
        }

        Player fallback = wardens.ClosestStanding(enemy.Position);
        return fallback ?? (wardens.All.Count > 0 ? wardens.All[0] : null!);
    }

    private Player Choose(Vector2 from, Player current, bool currentUsable)
    {
        Player best = null!;
        float bestScore = float.MaxValue;

        foreach (Player warden in _wardens.All)
        {
            if (!warden.CanBeTargeted)
            {
                continue;
            }

            float score = Vector2.Distance(from, warden.Position);
            if (currentUsable && ReferenceEquals(warden, current))
            {
                // Stickiness: the brother already being fought stays cheapest
                // until the other is meaningfully closer.
                score *= GameBalance.TargetSwitchAdvantage;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = warden;
            }
        }

        return best;
    }

    private void PruneFinished(IReadOnlyList<Enemy> enemies)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _stale.Clear();
        foreach (Enemy tracked in _entries.Keys)
        {
            bool present = false;
            for (int i = 0; i < enemies.Count && !present; i++)
            {
                present = ReferenceEquals(enemies[i], tracked);
            }
            if (!present)
            {
                _stale.Add(tracked);
            }
        }

        foreach (Enemy removed in _stale)
        {
            _entries.Remove(removed);
        }
    }
}
