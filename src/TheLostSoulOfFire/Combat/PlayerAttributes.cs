using System;

namespace TheLostSoulOfFire.Combat;

/// <summary>
/// The player's character attributes. Strength scales the scythe, ability power scales the
/// Soul Cannon and armor reduces incoming damage. The later attributes (change
/// <c>add-more-player-attributes</c>) tune attack speed, core hits, ability cooldowns,
/// Resonance gain, dodging, knockback and rewards. At the baseline every attribute leaves the
/// tuned values untouched, except armor, which starts above zero.
/// </summary>
public readonly record struct PlayerAttributes(int Strength, int AbilityPower, int Armor)
{
    public const int Baseline = 10;
    public const int MinValue = 0;
    public const int MaxValue = 99;

    /// <summary>Change per attribute point above or below <see cref="Baseline"/>; also the step of every other multiplier.</summary>
    public const float DamagePerPoint = 0.05f;

    /// <summary>Movement speed change per agility point; smaller than the dodge step so speed stays controllable.</summary>
    public const float MoveSpeedPerPoint = 0.01f;

    /// <summary>Armor that halves incoming damage; reduction is armor / (armor + this).</summary>
    public const float ArmorHalvingValue = 50f;

    /// <summary>Tempo: speeds up scythe swings and the Soul Cannon charge.</summary>
    public int AttackSpeed { get; init; } = Baseline;

    /// <summary>Glück: raises the Glut from defeated enemies and the Geld from chests.</summary>
    public int Luck { get; init; } = Baseline;

    /// <summary>Kernschärfe: raises the damage of core hits.</summary>
    public int CoreSharpness { get; init; } = Baseline;

    /// <summary>Einklang: raises every Resonance gain.</summary>
    public int Attunement { get; init; } = Baseline;

    /// <summary>Gewandtheit: shortens the dodge cooldown and raises movement speed.</summary>
    public int Agility { get; init; } = Baseline;

    /// <summary>Fokus: run ability cooldowns run down faster.</summary>
    public int Focus { get; init; } = Baseline;

    /// <summary>Standfestigkeit: reduces the knockback of hits the player takes.</summary>
    public int Steadiness { get; init; } = Baseline;

    public static PlayerAttributes Default { get; } = new(Baseline, Baseline, Baseline);

    public float StrengthMultiplier => PointMultiplier(Strength);

    public float AbilityPowerMultiplier => PointMultiplier(AbilityPower);

    public float AttackSpeedMultiplier => PointMultiplier(AttackSpeed);

    public float LuckMultiplier => PointMultiplier(Luck);

    public float CoreSharpnessMultiplier => PointMultiplier(CoreSharpness);

    public float AttunementMultiplier => PointMultiplier(Attunement);

    /// <summary>How much faster the dodge cooldown runs out.</summary>
    public float AgilityMultiplier => PointMultiplier(Agility);

    public float MoveSpeedMultiplier => 1f + (Agility - Baseline) * MoveSpeedPerPoint;

    public float FocusMultiplier => PointMultiplier(Focus);

    /// <summary>Share of knockback that still reaches the player; 1 at the baseline, never zero.</summary>
    public float KnockbackTaken => 1f / PointMultiplier(Steadiness);

    /// <summary>Share of incoming damage armor absorbs, from 0 to just under 1.</summary>
    public float ArmorReduction => Math.Max(0, Armor) / (Math.Max(0, Armor) + ArmorHalvingValue);

    public int ScaleWeaponDamage(float baseDamage) => ScaleDamage(baseDamage, StrengthMultiplier);

    public int ScaleAbilityDamage(float baseDamage) => ScaleDamage(baseDamage, AbilityPowerMultiplier);

    /// <summary>Damage left after armor; a hit that deals damage always deals at least 1.</summary>
    public int MitigateIncomingDamage(int damage)
    {
        if (damage <= 0)
        {
            return 0;
        }

        return Math.Max(1, (int)MathF.Round(damage * (1f - ArmorReduction)));
    }

    /// <summary>
    /// Core hit damage from the damage already scaled by strength or ability power and the
    /// core bonus of the weapon, then scaled by core sharpness.
    /// </summary>
    public int ScaleCoreDamage(int damage, float coreMultiplier) =>
        ScaleDamage(damage * coreMultiplier, CoreSharpnessMultiplier);

    /// <summary>
    /// Reward after luck. The fraction becomes one more unit with its own probability, so
    /// every luck point counts even on small rewards. <paramref name="roll"/> is uniform in [0, 1).
    /// </summary>
    public int ScaleReward(int baseAmount, double roll)
    {
        if (baseAmount <= 0)
        {
            return 0;
        }

        // Rounded so float noise (1.2f is 1.2000000476…) never turns an exact amount into a rare extra unit.
        double scaled = Math.Round(baseAmount * (double)LuckMultiplier, 4);
        int whole = (int)Math.Floor(scaled);
        return whole + (roll < scaled - whole ? 1 : 0);
    }

    private static float PointMultiplier(int value) => 1f + (value - Baseline) * DamagePerPoint;

    private static int ScaleDamage(float baseDamage, float multiplier) =>
        Math.Max(1, (int)MathF.Round(baseDamage * multiplier));
}
