using System;

namespace TheLostSoulOfFire.Combat;

/// <summary>
/// The player's character attributes. Strength scales the scythe, ability power scales the
/// Soul Cannon and armor reduces incoming damage. At the baseline every attribute leaves the
/// tuned damage values untouched, except armor, which starts above zero.
/// </summary>
public readonly record struct PlayerAttributes(int Strength, int AbilityPower, int Armor)
{
    public const int Baseline = 10;
    public const int MinValue = 0;
    public const int MaxValue = 99;

    /// <summary>Damage change per attribute point above or below <see cref="Baseline"/>.</summary>
    public const float DamagePerPoint = 0.05f;

    /// <summary>Armor that halves incoming damage; reduction is armor / (armor + this).</summary>
    public const float ArmorHalvingValue = 50f;

    public static PlayerAttributes Default { get; } = new(Baseline, Baseline, Baseline);

    public float StrengthMultiplier => DamageMultiplier(Strength);

    public float AbilityPowerMultiplier => DamageMultiplier(AbilityPower);

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

    private static float DamageMultiplier(int value) => 1f + (value - Baseline) * DamagePerPoint;

    private static int ScaleDamage(float baseDamage, float multiplier) =>
        Math.Max(1, (int)MathF.Round(baseDamage * multiplier));
}
