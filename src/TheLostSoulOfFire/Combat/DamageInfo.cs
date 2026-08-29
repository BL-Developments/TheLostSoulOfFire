using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Combat;

public readonly record struct DamageInfo(
    int Damage,
    Vector2 Knockback,
    Vector2 HitPosition,
    bool IsSoulCoreHit = false,
    bool IsFullCannon = false);
