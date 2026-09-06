using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// One pool of Resonance held by the Wardens together.
///
/// Chosen over per-Warden pools because a shared pool removes the two co-op
/// failures that would otherwise appear immediately: brothers racing each other
/// for released residue, and one player sitting on a full bar while the other has
/// none. It is also the coherent reading of the fiction — Resonance is agreement
/// with the Death Flame, and the brothers are already tethered by it.
///
/// Ownership is therefore explicit and simple:
///   * anything that earns Resonance credits the pool, whoever earned it;
///   * either brother may spend it, and spending it lights both;
///   * stabilising a downed brother spends from the same pool.
///
/// In solo this is arithmetically identical to the Session 1 behaviour.
/// </summary>
public sealed class TeamResonance
{
    public float Charge { get; private set; }

    /// <summary>How many times the team has been relit this encounter.</summary>
    public int StabilizeCount { get; private set; }

    public bool IsReady => Charge >= GameBalance.ResonanceRequired;

    /// <summary>Each relight is slower than the last, so it never becomes routine.</summary>
    public float StabilizeSeconds =>
        GameBalance.StabilizeDuration * (1f + StabilizeCount * GameBalance.StabilizeEscalation);

    public void Reset()
    {
        Charge = 0f;
        StabilizeCount = 0;
    }

    public void Add(float amount) =>
        Charge = MathHelper.Clamp(Charge + amount, 0f, GameBalance.ResonanceRequired);

    public void Fill() => Charge = GameBalance.ResonanceRequired;

    /// <summary>Spends the whole pool to light both Wardens.</summary>
    public bool TrySpendForResonance()
    {
        if (!IsReady)
        {
            return false;
        }

        Charge = 0f;
        return true;
    }

    /// <summary>
    /// Relighting a brother draws on the same gathered flame. It is never blocked
    /// outright — a team with nothing banked can still save each other — but it
    /// costs whatever is there and makes the next Resonance further away.
    /// </summary>
    public void ChargeStabilization()
    {
        Charge = MathF.Max(0f, Charge - GameBalance.StabilizeResonanceCost);
        StabilizeCount++;
    }
}
