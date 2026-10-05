using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TheLostSoulOfFire.Rendering.Visuals;

/// <summary>
/// Every Visual-ID the game code draws. Code names an ID; which textures, frames,
/// sizes and normal maps belong to it lives only in <c>Content/Visuals/registry.json</c>.
/// Each ID needs a Visual-Spec under <c>art/specs/&lt;id&gt;.md</c>; the tests read this list.
/// </summary>
public static class VisualIds
{
    public const string Player = "player";
    public const string Hollow = "enemy.hollow";
    public const string Burning = "enemy.burning";
    public const string Devourer = "enemy.devourer";

    public const string ArenaFloor = "environment.arena";

    public const string Scythe = "weapon.scythe";
    public const string SoulCannon = "weapon.soul-cannon";
    public const string LostSoul = "pickup.lost-soul";
    public const string LifeFlame = "ending.life-flame";

    public const string ScytheSlash1 = "fx.scythe-slash-01";
    public const string ScytheSlash2 = "fx.scythe-slash-02";
    public const string ScytheCleave = "fx.scythe-cleave";
    public const string CoreHit = "fx.core-hit";
    public const string DashIgnition = "fx.dash-ignition";
    public const string CannonChargeLoop = "fx.cannon-charge-loop";
    public const string CannonMuzzleFull = "fx.cannon-muzzle-full";
    public const string CannonProjectileFull = "fx.cannon-projectile-full";
    public const string BurningDetonation = "fx.burning-detonation";
    public const string SoulRelease = "fx.soul-release";
    public const string ResonanceActivate = "fx.resonance-activate";
    public const string DeathFlameLoop = "fx.death-flame-loop";

    /// <summary>All IDs declared above, for tests and tooling.</summary>
    public static IReadOnlyList<string> All { get; } = typeof(VisualIds)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();
}

/// <summary>Clip names the game code asks for. A registry entry may lack any of them; see <see cref="VisualResolver"/>.</summary>
public static class VisualClips
{
    public const string Default = "default";
    public const string Idle = "idle";
    public const string Move = "move";
    public const string Swipe = "swipe";
    public const string Charge = "charge";
    public const string Slam = "slam";
    public const string Devour = "devour";
}
