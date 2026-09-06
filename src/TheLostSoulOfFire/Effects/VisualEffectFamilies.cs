using System;
using System.Collections.Generic;

namespace TheLostSoulOfFire.Effects;

public enum VisualEffectPriority
{
    Decorative,
    Combat,
    Critical
}

public enum VisualEffectBlend
{
    Alpha,
    Additive
}

/// <summary>
/// The ownership and presentation contract for every existing sprite-VFX sheet.
/// Gameplay code chooses an event; this table chooses the cosmetic family details.
///
/// The glow is a single soft disc laid over the effect's position. It was sized
/// for the delivered sheets, which had no internal light of their own. The
/// authored sheets do, and at the old intensities the disc simply covered them:
/// a detonation with a shockwave, torn fragments and travelling embers arrived
/// on screen as a white ball. Every glow is now wider and much weaker, so it
/// lights the room around the effect instead of replacing it.
/// </summary>
public sealed record VisualEffectFamily(
    string Key,
    VisualEffectBlend Blend,
    VisualEffectPriority Priority,
    float GlowRadius,
    float GlowIntensity);

public static class VisualEffectFamilies
{
    private static readonly Dictionary<string, VisualEffectFamily> ByKey =
        new Dictionary<string, VisualEffectFamily>(StringComparer.Ordinal)
        {
            ["burning_detonation"] = new("burning_detonation", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 150.0f, 0.13f),
            ["cannon_charge_loop"] = new("cannon_charge_loop", VisualEffectBlend.Additive, VisualEffectPriority.Combat, 70.0f, 0.12f),
            ["cannon_muzzle_full"] = new("cannon_muzzle_full", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 120.0f, 0.15f),
            ["cannon_projectile_full"] = new("cannon_projectile_full", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 62.0f, 0.16f),
            ["core_hit"] = new("core_hit", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 88.0f, 0.14f),
            ["dash_ignition"] = new("dash_ignition", VisualEffectBlend.Additive, VisualEffectPriority.Combat, 58.0f, 0.08f),
            ["death_flame_loop"] = new("death_flame_loop", VisualEffectBlend.Alpha, VisualEffectPriority.Decorative, 46.0f, 0.07f),
            ["resonance_activate"] = new("resonance_activate", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 180.0f, 0.13f),
            ["scythe_cleave"] = new("scythe_cleave", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 96.0f, 0.12f),
            ["scythe_slash_01"] = new("scythe_slash_01", VisualEffectBlend.Alpha, VisualEffectPriority.Combat, 52.0f, 0.07f),
            ["scythe_slash_02"] = new("scythe_slash_02", VisualEffectBlend.Additive, VisualEffectPriority.Combat, 64.0f, 0.09f),
            ["soul_release"] = new("soul_release", VisualEffectBlend.Additive, VisualEffectPriority.Critical, 96.0f, 0.12f)
        };

    public static IReadOnlyCollection<VisualEffectFamily> All => ByKey.Values;

    public static VisualEffectFamily Get(string key) => ByKey.TryGetValue(key, out VisualEffectFamily family)
        ? family
        : throw new ArgumentOutOfRangeException(nameof(key), key, "No visual-effect family is registered for this sprite sheet.");
}
