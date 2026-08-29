using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game;

public static class GameBalance
{
    public const int BackBufferWidth = 1280;
    public const int BackBufferHeight = 720;

    public const float PlayerMoveSpeed = 310f;
    public const float PlayerRadius = 22f;
    public const int PlayerMaxHealth = 100;
    public const float SoulSenseMovementMultiplier = 0.85f;
    public const float SoulSenseCoreDamageMultiplier = 1.45f;

    public const float DashDistance = 170f;
    public const float DashDuration = 0.14f;
    public const float DashCooldown = 0.62f;
    public const float DashInvulnerability = 0.18f;

    public const float ComboResetTime = 0.6f;
    public const int ScytheDamage1 = 20;
    public const int ScytheDamage2 = 25;
    public const int ScytheDamage3 = 40;
    public const float ScytheRange1 = 112f;
    public const float ScytheRange2 = 120f;
    public const float ScytheRange3 = 138f;
    public const int DummyMaxHealth = 180;

    public const int HollowMaxHealth = 100;
    public const float HollowRadius = 27f;
    public const float HollowMoveSpeed = 108f;
    public const float HollowAttackStartRange = 82f;
    public const float HollowSwipeRange = 92f;
    public const int HollowSwipeDamage = 16;
    public const float HollowSwipeKnockback = 260f;
    public const float HollowSwipeTelegraph = 0.42f;
    public const float HollowSwipeDuration = 0.13f;
    public const float HollowRecoveryDuration = 0.48f;
    public const float HollowDeathDuration = 0.62f;
    public const float HollowCoreRadius = 12f;
    public const float HollowFullCannonStagger = 1.15f;

    public const int BurningMaxHealth = 70;
    public const float BurningRadius = 25f;
    public const float BurningMoveSpeed = 168f;
    public const float BurningChargeStartRange = 330f;
    public const float BurningChargeTelegraph = 0.62f;
    public const float BurningChargeSpeed = 690f;
    public const float BurningChargeDuration = 0.58f;
    public const float BurningRecoveryDuration = 1f;
    public const float BurningAggressionHandoffDelay = 0.28f;
    public const float BurningStalkInnerRange = 190f;
    public const float BurningStalkOuterRange = 285f;
    public const int BurningChargeDamage = 20;
    public const float BurningChargeKnockback = 390f;
    public const float BurningDeathDuration = 0.58f;
    public const float BurningFractureRadius = 9f;
    public const float BurningDetonationRadius = 185f;
    public const int BurningDetonationDamage = 64;
    public const float BurningDetonationKnockback = 520f;

    public const int DevourerMaxHealth = 200;
    public const float DevourerRadius = 43f;
    public const float DevourerMoveSpeed = 72f;
    public const float DevourerSlamStartRange = 132f;
    public const float DevourerSlamRange = 145f;
    public const float DevourerSlamTelegraph = 0.88f;
    public const float DevourerSlamDuration = 0.18f;
    public const float DevourerRecoveryDuration = 0.78f;
    public const int DevourerSlamDamage = 24;
    public const float DevourerSlamKnockback = 460f;
    public const float DevourerSoulTargetRange = 1400f;
    public const float DevourerDevourStartRange = 78f;
    public const float DevourerDevourDuration = 1.1f;
    public const int DevourerHealPerSoul = 42;
    public const int DevourerDamagePerSoul = 3;
    public const int DevourerMaxSoulStacks = 3;
    public const float DevourerFullCannonStagger = 1.4f;
    public const float DevourerDeathDuration = 0.82f;
    public const float DevourerTorsoRadius = 26f;

    public const float SoulExposedDuration = 0.5f;
    public const float SoulReleaseDuration = 1.25f;
    public const float SoulResidueTravelTime = 0.85f;
    public const float ResonanceRequired = 100f;
    public const float ResonancePerSoulRelease = 18f;
    public const float ResonancePerCoreHit = 2f;
    public const float ResonanceDuration = 10f;
    public const float ResonanceMovementMultiplier = 1.12f;
    public const float ResonanceScytheDamageMultiplier = 1.3f;
    public const float ResonanceScytheRangeMultiplier = 1.18f;
    public const float ResonanceScytheKnockbackMultiplier = 1.25f;
    public const float ResonanceDashDistanceMultiplier = 1.22f;
    public const float ResonanceDashCooldownMultiplier = 0.72f;
    public const float ResonanceCannonChargeSpeedMultiplier = 1.35f;
    public const float ResonanceCannonDamageMultiplier = 1.28f;
    public const float ResonanceCannonSizeMultiplier = 1.25f;

    public const float CannonDrawDuration = 0.16f;
    public const float CannonFullChargeTime = 1.2f;
    public const float CannonReturnDuration = 0.28f;
    public const float CannonChargeMovementMultiplier = 0.58f;
    public const float CannonHandlingMovementMultiplier = 0.78f;
    public const int CannonWeakDamage = 24;
    public const int CannonFullDamage = 68;
    public const float CannonProjectileSpeed = 1180f;
    public const float CannonProjectileLifetime = 1.25f;
    public const float CannonCoreDamageMultiplier = 1.35f;

    public static readonly Rectangle ArenaBounds = new(0, 0, 1800, 1000);
    public static readonly Rectangle CombatBounds = new(105, 95, 1590, 810);

    public static readonly Color VoidColor = new(7, 6, 12);
    public static readonly Color FloorColor = new(19, 18, 27);
    public static readonly Color FloorDetailColor = new(31, 28, 42);
    public static readonly Color StoneColor = new(39, 38, 48);
    public static readonly Color MetalColor = new(61, 60, 72);
    public static readonly Color DeepViolet = new(63, 24, 112);
    public static readonly Color DeathFlame = new(145, 71, 255);
    public static readonly Color DeathFlameBright = new(221, 190, 255);
    public static readonly Color SoulWhite = new(246, 239, 255);
    public static readonly Color SoulSenseWorldGrade = new(199, 204, 208);
    public static readonly Color SoulSenseWorldVeil = new(8, 11, 15);
    public static readonly Color SoulSenseTrace = new(170, 112, 232);
}
