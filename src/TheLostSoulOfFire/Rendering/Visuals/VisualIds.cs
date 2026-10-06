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
    public const string ArenaWall = "environment.arena-wall";
    public const string ArenaPillar = "prop.arena-pillar";
    public const string ArenaLockers = "prop.arena-lockers";
    public const string ArenaWorkbench = "prop.arena-workbench";
    public const string ArenaToolRack = "prop.arena-toolrack";
    public const string ArenaSlag = "prop.arena-slag";
    public const string ArenaChains = "prop.arena-chains";
    public const string ArenaGate = "prop.arena-gate";
    public const string ArenaChest = "prop.arena-chest";
    public const string TrainingDummy = "prop.training-dummy";
    public const string ShoreFloor = "environment.shore";
    public const string ShoreBench = "prop.shore-bench";
    public const string ShoreSuitcase = "prop.shore-suitcase";
    public const string ShoreBoard = "prop.shore-board";
    public const string ShoreCanopyPost = "prop.shore-canopy-post";
    public const string ShoreLamp = "prop.shore-lamp";
    public const string ShoreBollard = "prop.shore-bollard";
    public const string SearchFloor = "environment.search";
    public const string SearchLuggage = "prop.search-luggage";
    public const string SearchMast = "prop.search-mast";
    public const string WardenMarker = "prop.warden-marker";
    public const string CausewayFloor = "environment.causeway";
    public const string DeckFloor = "environment.deck";
    public const string DeckSea = "environment.sea";
    public const string PassingFar = "environment.passing-far";
    public const string PassingNear = "environment.passing-near";
    public const string DeckRail = "prop.deck-rail";
    public const string ThresholdFloor = "environment.threshold";
    public const string ThresholdGate = "prop.threshold-gate";
    public const string TitleBackdrop = "environment.title";
    public const string HubFloor = "environment.hub";
    public const string HubBrazier = "prop.hub-brazier";
    public const string HubLeaves = "environment.hub-leaves";
    public const string HubLeavesFinal = "environment.hub-leaves-final";
    public const string HubSeal = "environment.hub-seal";
    public const string HubSealFinal = "environment.hub-seal-final";

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

    /// <summary>Scene grading: one LUT per area plus Soul Sense; areas without a LUT use the neutral one.</summary>
    public const string GradeNeutral = "grade.neutral";
    public const string GradeSoulSense = "grade.soul-sense";
    public const string GradeShore = "grade.shore";
    public const string GradeArena = "grade.arena";

    /// <summary>Test figure for the lighting pass: clips "flat" and "tilted" differ only in their normal map.</summary>
    public const string TestLitFigure = "test.lit-figure";

    /// <summary>Procedural Blender figure that proves the render pipeline (tools/visuals/blender).</summary>
    public const string TestBlenderFigure = "test.blender-figure";

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

    /// <summary>Player: the three steps of the scythe combo and the raised Soul Cannon.</summary>
    public const string Swing1 = "swing1";
    public const string Swing2 = "swing2";
    public const string Swing3 = "swing3";
    public const string Aim = "aim";

    /// <summary>Player: walking while the Soul Cannon charges (distance clip, played backward when backing away).</summary>
    public const string AimMove = "aim_move";
    public const string CannonDraw = "cannon_draw";
    public const string CannonFire = "cannon_fire";
    public const string Dash = "dash";
    public const string Hit = "hit";
    public const string Death = "death";

    /// <summary>Player: the backward leap of the Rückstoßsprung ability.</summary>
    public const string Retreat = "retreat";

    /// <summary>Enemies: the rest after an attack and the long stagger after a full cannon.</summary>
    public const string Recover = "recover";
    public const string Telegraph = "telegraph";
    public const string Stagger = "stagger";

    public static string Swing(int step) => step switch
    {
        1 => Swing1,
        2 => Swing2,
        _ => Swing3
    };

    /// <summary>Player: gathering back into the guard after a swing, while standing still.</summary>
    public const string Swing1Return = "swing1_return";
    public const string Swing2Return = "swing2_return";
    public const string Swing3Return = "swing3_return";

    public static string SwingReturn(int step) => step switch
    {
        1 => Swing1Return,
        2 => Swing2Return,
        _ => Swing3Return
    };

    /// <summary>How long the return after each swing plays (the Soul Cleave settles longest).</summary>
    public static float SwingReturnDuration(int step) => step == 3 ? 0.36f : 0.26f;
}
