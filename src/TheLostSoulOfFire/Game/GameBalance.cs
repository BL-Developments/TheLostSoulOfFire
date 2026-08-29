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

    public const float SoulExposedDuration = 0.5f;
    public const float SoulReleaseDuration = 1.25f;
    public const float SoulResidueTravelTime = 0.85f;
    public const float ResonanceRequired = 100f;
    public const float ResonancePerSoulRelease = 18f;
    public const float ResonancePerCoreHit = 2f;

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
}
