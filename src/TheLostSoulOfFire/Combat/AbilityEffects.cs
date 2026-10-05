using System;
namespace TheLostSoulOfFire.Combat;

/// <summary>Transient defensive/offensive state; cleared on death and run transitions.</summary>
public sealed class AbilityEffects
{
    public float GuardRemaining { get; private set; }
    public float RevengeRemaining { get; private set; }
    public float SetupRemaining { get; private set; }
    public void Update(float dt)
    {
        GuardRemaining = MathF.Max(0, GuardRemaining - dt);
        RevengeRemaining = MathF.Max(0, RevengeRemaining - dt);
        SetupRemaining = MathF.Max(0, SetupRemaining - dt);
    }
    public void Guard() { GuardRemaining = 2f; RevengeRemaining = 0; }
    public void PrepareSetup() => SetupRemaining = 5f;
    public bool TryBlock()
    {
        if (GuardRemaining <= 0) return false;
        GuardRemaining = 0;
        RevengeRemaining = 5f;
        return true;
    }
    public int ConsumeRevenge()
    {
        if (RevengeRemaining <= 0) return 0;
        RevengeRemaining = 0;
        return 24;
    }
    public bool ConsumeSetup()
    {
        if (SetupRemaining <= 0) return false;
        SetupRemaining = 0;
        return true;
    }
    public void Clear() => GuardRemaining = RevengeRemaining = SetupRemaining = 0;
}

