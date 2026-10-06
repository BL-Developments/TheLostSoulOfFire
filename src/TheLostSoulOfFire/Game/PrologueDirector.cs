using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

public enum PrologueSector
{
    Emergence,
    Search,
    Escape,
    Threshold
}

public enum PrologueStage
{
    Dormant,
    Waking,
    FindTrace,
    TraceWitnessed,
    EmergenceThreat,
    LeaveEmergence,
    SearchApproach,
    HollowLesson,
    BurningLesson,
    LeaveSearch,
    DevourerPressure,
    ReleaseWitness,
    BoardVehicle,
    Transit,
    Arrival,
    Complete
}

/// <summary>
/// The authored spine of the first playable. It owns only story/sector state and
/// the few coordinates shared by gameplay and presentation; enemies, Souls,
/// combat and local players remain in <see cref="GameWorld"/>.
/// </summary>
public sealed class PrologueDirector
{
    public static readonly Rectangle WorldBounds = new(0, 0, 1800, 1000);
    public static readonly Rectangle ExplorationBounds = new(110, 125, 1580, 750);
    public static readonly Rectangle VehicleDeckBounds = new(475, 330, 850, 390);
    public static readonly Rectangle ThresholdApproachBounds = new(110, 545, 1580, 330);

    public static readonly Vector2 EmergenceSpawn = new(292f, 600f);
    public static readonly Vector2 SearchSpawn = new(225f, 610f);
    public static readonly Vector2 EscapeSpawn = new(238f, 600f);
    public static readonly Vector2 VehicleSpawn = new(900f, 545f);
    public static readonly Vector2 ThresholdSpawn = new(900f, 770f);
    public static readonly Vector2 SoulTrace = new(805f, 520f);
    public static readonly Vector2 SearchExitBeacon = new(1370f, 545f);
    public static readonly Vector2 VehicleDock = new(1510f, 600f);

    /// <summary>
    /// Props of the shore (docs/current/regions/prologue.md): the bench of the waiting and the
    /// suitcase at the trace, canopy posts along the north kerb, a cold lamp at the exit,
    /// bollards on the south kerb, the tilted departure board rising from the water in front
    /// of the platform and the Warden mark at the exit; all rendered with the platform
    /// (tools/visuals/blender/build_prologue.py, sector shore). Visual only; none of them collide.
    /// </summary>
    public static IReadOnlyList<PropPlacement> ShoreProps { get; } =
    [
        new(VisualIds.ShoreBench, new Vector2(790f, 478f), new Vector2(170f, 90f), SceneLayer.HighProp),
        new(VisualIds.ShoreBench, new Vector2(480f, 212f), new Vector2(170f, 90f), SceneLayer.HighProp),
        new(VisualIds.ShoreSuitcase, new Vector2(872f, 596f), new Vector2(50f, 45f), SceneLayer.HighProp),
        new(VisualIds.ShoreCanopyPost, new Vector2(330f, 262f), new Vector2(40f, 240f), SceneLayer.HighProp),
        new(VisualIds.ShoreCanopyPost, new Vector2(720f, 262f), new Vector2(40f, 240f), SceneLayer.HighProp),
        new(VisualIds.ShoreCanopyPost, new Vector2(1110f, 262f), new Vector2(40f, 240f), SceneLayer.HighProp),
        new(VisualIds.ShoreLamp, new Vector2(1600f, 455f), new Vector2(50f, 220f), SceneLayer.HighProp),
        new(VisualIds.ShoreBollard, new Vector2(260f, 878f), new Vector2(45f, 45f), SceneLayer.HighProp),
        new(VisualIds.ShoreBollard, new Vector2(1500f, 878f), new Vector2(45f, 45f), SceneLayer.HighProp),
        new(VisualIds.ShoreBoard, new Vector2(1180f, 992f), new Vector2(180f, 200f), SceneLayer.Occluder),
        new(VisualIds.WardenMarker, new Vector2(1535f, 515f), new Vector2(30f, 90f), SceneLayer.HighProp)
    ];

    /// <summary>
    /// Props of the searchway, rendered with its plate (tools/visuals/blender/build_prologue.py):
    /// the luggage the passengers left, the three Warden way-marks and the harbour signal mast in
    /// the south-west, whose search fire lights the quay. Visual only; none of them collide.
    /// </summary>
    public static IReadOnlyList<PropPlacement> SearchProps { get; } =
    [
        new(VisualIds.SearchLuggage, new Vector2(835f, 735f), new Vector2(110f, 70f), SceneLayer.HighProp),
        new(VisualIds.WardenMarker, new Vector2(548f, 432f), new Vector2(30f, 90f), SceneLayer.HighProp),
        new(VisualIds.WardenMarker, new Vector2(1038f, 652f), new Vector2(30f, 90f), SceneLayer.HighProp),
        new(VisualIds.WardenMarker, new Vector2(1416f, 533f), new Vector2(30f, 90f), SceneLayer.HighProp),
        new(VisualIds.SearchMast, new Vector2(330f, 968f), new Vector2(110f, 520f), SceneLayer.Occluder)
    ];

    /// <summary>The Warden mark on the landing of the causeway.</summary>
    public static IReadOnlyList<PropPlacement> CausewayProps { get; } =
    [
        new(VisualIds.WardenMarker, new Vector2(1395f, 505f), new Vector2(30f, 90f), SceneLayer.HighProp)
    ];

    /// <summary>The skiff's near railing, drawn in front of everyone on deck.</summary>
    public static IReadOnlyList<PropPlacement> DeckProps { get; } =
    [
        new(VisualIds.DeckRail, new Vector2(922f, 757f), new Vector2(990f, 60f), SceneLayer.Foreground)
    ];

    /// <summary>
    /// The gatehouse of the threshold (whoever walks through its door disappears into it) and the
    /// two Warden marks flanking the steps.
    /// </summary>
    public static IReadOnlyList<PropPlacement> ThresholdProps { get; } =
    [
        new(VisualIds.ThresholdGate, new Vector2(900f, 690f), new Vector2(290f, 700f), SceneLayer.HighProp),
        new(VisualIds.WardenMarker, new Vector2(754f, 660f), new Vector2(30f, 90f), SceneLayer.HighProp),
        new(VisualIds.WardenMarker, new Vector2(1046f, 660f), new Vector2(30f, 90f), SceneLayer.HighProp)
    ];

    public static IReadOnlyList<PropPlacement> SectorProps(PrologueSector sector, bool ride) => sector switch
    {
        PrologueSector.Search => SearchProps,
        PrologueSector.Escape => ride ? DeckProps : CausewayProps,
        PrologueSector.Threshold => ThresholdProps,
        _ => ShoreProps
    };

    /// <summary>Warden flames standing in the fittings of a sector's props: base point and height.</summary>
    public static IReadOnlyList<(Vector2 Base, float Height)> WardenFlames(PrologueSector sector, bool ride) => sector switch
    {
        PrologueSector.Emergence => [(new Vector2(1535f, 436f), 24f)],
        PrologueSector.Search =>
        [
            (new Vector2(548f, 353f), 24f), (new Vector2(1038f, 573f), 24f), (new Vector2(1416f, 454f), 24f),
            (new Vector2(330f, 500f), 46f)
        ],
        PrologueSector.Escape when ride =>
        [
            (new Vector2(695f, 261f), 22f), (new Vector2(1208f, 261f), 22f), (new Vector2(487f, 492f), 52f)
        ],
        PrologueSector.Escape =>
        [
            (new Vector2(1395f, 426f), 24f), (new Vector2(1362f, 522f), 20f), (new Vector2(1620f, 522f), 20f), (new Vector2(1323f, 623f), 40f)
        ],
        PrologueSector.Threshold =>
        [
            (new Vector2(900f, 432f), 70f), (new Vector2(754f, 581f), 24f), (new Vector2(1046f, 581f), 24f)
        ],
        _ => []
    };

    /// <summary>How long the player lies and rises at the start, before the first objective.</summary>
    public const float WakingDuration = 3.6f;

    public PrologueStage Stage { get; private set; } = PrologueStage.Dormant;
    public PrologueSector Sector => Stage switch
    {
        <= PrologueStage.LeaveEmergence => PrologueSector.Emergence,
        <= PrologueStage.LeaveSearch => PrologueSector.Search,
        <= PrologueStage.Transit => PrologueSector.Escape,
        _ => PrologueSector.Threshold
    };

    public float StateTime { get; private set; }
    public float SectorTime { get; private set; }
    public float RunTime { get; private set; }
    public bool Started => Stage != PrologueStage.Dormant;
    public bool IsComplete => Stage == PrologueStage.Complete;
    public bool IsVehicleRide => Stage == PrologueStage.Transit;
    public Rectangle MovementBounds => IsVehicleRide
        ? VehicleDeckBounds
        : Sector == PrologueSector.Threshold
            ? ThresholdApproachBounds
            : ExplorationBounds;

    public string SectorTitle => Sector switch
    {
        PrologueSector.Emergence => "I  THE UNFINISHED SHORE",
        PrologueSector.Search => "II  THE SEARCHWAY",
        PrologueSector.Escape => "III  THE LAST CROSSING",
        _ => "THE WARDEN THRESHOLD"
    };

    public string Objective => Stage switch
    {
        PrologueStage.Waking => "STAND",
        PrologueStage.FindTrace => "FOLLOW THE HUMAN ECHO  HOLD Q FOR SOUL SENSE",
        PrologueStage.TraceWitnessed => "THE PLACE REMEMBERS",
        PrologueStage.EmergenceThreat => "CUT THE MANIFESTATION  RELEASE THE SOUL",
        PrologueStage.LeaveEmergence => "FOLLOW THE WHITE WARDEN MARKS EAST",
        PrologueStage.SearchApproach => "FOLLOW THE SEARCH FIRES",
        PrologueStage.HollowLesson => "READ THE SWIPE  DASH LATE  STRIKE THE ANCHOR",
        PrologueStage.BurningLesson => "BREAK THE CHARGE WITH A FULL SOUL CANNON",
        PrologueStage.LeaveSearch => "REACH THE EXTRACTION CAUSEWAY",
        PrologueStage.DevourerPressure => "THE DEVOURER HOLDS A SOUL  SEVER OR BREAK ITS CAVITY",
        PrologueStage.ReleaseWitness => "LET THE SOUL LEAVE",
        PrologueStage.BoardVehicle => "BOARD THE DEATH FLAME SKIFF",
        PrologueStage.Transit => "HOLD THE DECK  USE THE SAME SOUL CANNON",
        PrologueStage.Arrival => "CROSS THE THRESHOLD",
        PrologueStage.Complete => "PRESS ANY INPUT TO ENTER THE SOUL FURNACE",
        _ => string.Empty
    };

    public string StoryLine
    {
        get
        {
            if (Stage == PrologueStage.Waking)
                return StateTime < 2.3f ? "YOU REMEMBER THE IMPACT" : "THEN COLD  THEN FLAME";
            if (Stage == PrologueStage.TraceWitnessed)
                return StateTime < 2.4f ? "A BENCH  A DEPARTURE BOARD  NOBODY CAME BACK" : "THE DEAD DID NOT LEAVE THE WAITING BEHIND";
            if (Stage == PrologueStage.ReleaseWitness)
                return "THE PERSON LEAVES  ONLY THE ECHO RETURNS";
            if (Stage == PrologueStage.Transit && StateTime < 5f)
                return "THE THRESHOLD IS SIXTY SECONDS EAST";
            if (Stage == PrologueStage.Arrival)
                return StateTime < 3.6f ? "THIS IS WHERE WARDENS HOLD" : "NOT FOREVER  ONLY UNTIL THE WORK IS DONE";
            if (Stage == PrologueStage.Complete)
                return "YOU WOKE ALONE  THE FURNACE STILL BURNS";
            return string.Empty;
        }
    }

    public void Start()
    {
        RunTime = 0f;
        SectorTime = 0f;
        Enter(PrologueStage.Waking);
    }

    public void Enter(PrologueStage stage)
    {
        PrologueSector previousSector = Sector;
        Stage = stage;
        StateTime = 0f;
        if (Sector != previousSector)
        {
            SectorTime = 0f;
        }
    }

    public void Update(float deltaTime)
    {
        StateTime += deltaTime;
        SectorTime += deltaTime;
        if (Started && !IsComplete)
        {
            RunTime += deltaTime;
        }
    }
}
