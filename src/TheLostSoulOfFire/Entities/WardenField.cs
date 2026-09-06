using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Effects;

namespace TheLostSoulOfFire.Entities;

/// <summary>
/// The Wardens an enemy is fighting, as seen from the enemy's side.
///
/// This is the whole multiplayer seam on the combat side. Enemies used to be
/// handed one <see cref="Player"/> and did two different things with it: they
/// aimed at it, and they damaged it. Those are separate questions once there are
/// two brothers in the room — a Devourer commits to one target, but its slam
/// still crushes whoever is standing in the ring.
///
/// <see cref="Target"/> answers "who am I coming for" and is assigned per enemy by
/// <see cref="Game.TargetDirector"/>. The Strike helpers answer "who did that
/// actually hit" and always consider every Warden. Nothing here knows about input,
/// slots or networking.
/// </summary>
public sealed class WardenField
{
    private readonly List<Player> _wardens = [];

    /// <summary>Every Warden in the encounter, downed or not.</summary>
    public IReadOnlyList<Player> All => _wardens;

    /// <summary>
    /// The Warden the enemy currently being updated is committed to. Never null
    /// while an encounter is running; a downed Warden is replaced by a standing
    /// one so enemies do not stand over a body.
    /// </summary>
    public Player Target { get; private set; } = null!;

    public void Configure(IEnumerable<Player> wardens)
    {
        _wardens.Clear();
        _wardens.AddRange(wardens);
        Target = _wardens.Count > 0 ? _wardens[0] : null!;
    }

    internal void SetTarget(Player target) => Target = target;

    /// <summary>True while at least one Warden can still act.</summary>
    public bool AnyStanding
    {
        get
        {
            foreach (Player warden in _wardens)
            {
                if (warden.CanBeTargeted)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Closest Warden that can be fought, measured from a point. Used for aiming
    /// and approach decisions.
    /// </summary>
    public Player ClosestStanding(Vector2 from)
    {
        Player closest = null!;
        float best = float.MaxValue;
        foreach (Player warden in _wardens)
        {
            if (!warden.CanBeTargeted)
            {
                continue;
            }

            float distance = Vector2.DistanceSquared(from, warden.Position);
            if (distance < best)
            {
                best = distance;
                closest = warden;
            }
        }

        return closest;
    }

    /// <summary>
    /// A radial strike. Every Warden inside the radius takes it, which keeps a
    /// Devourer slam honest: the ring the Player was shown is the ring that hurts,
    /// no matter which brother it was aimed at.
    /// </summary>
    public bool StrikeCircle(
        Vector2 center,
        float radius,
        int damage,
        float knockback,
        ScreenEffects screenEffects,
        Vector2 fallbackDirection)
    {
        bool hitAnything = false;
        foreach (Player warden in _wardens)
        {
            if (!warden.CanBeDamaged)
            {
                continue;
            }

            Vector2 toWarden = warden.Position - center;
            float reach = radius + warden.Radius;
            if (toWarden.LengthSquared() > reach * reach)
            {
                continue;
            }

            Vector2 direction = toWarden.LengthSquared() > 0.001f
                ? Vector2.Normalize(toWarden)
                : fallbackDirection;
            warden.ApplyDamage(damage, direction * knockback, screenEffects);
            hitAnything = true;
        }

        return hitAnything;
    }

    /// <summary>
    /// A body-contact strike along a charge. Knockback follows the charge
    /// direction rather than the offset, so a Burning always throws Wardens the
    /// way it was running.
    /// </summary>
    public bool StrikeContact(
        Vector2 center,
        float radius,
        Vector2 direction,
        int damage,
        float knockback,
        ScreenEffects screenEffects)
    {
        bool hitAnything = false;
        foreach (Player warden in _wardens)
        {
            if (!warden.CanBeDamaged)
            {
                continue;
            }

            float reach = radius + warden.Radius;
            if (Vector2.DistanceSquared(warden.Position, center) > reach * reach)
            {
                continue;
            }

            warden.ApplyDamage(damage, direction * knockback, screenEffects);
            hitAnything = true;
        }

        return hitAnything;
    }
}
