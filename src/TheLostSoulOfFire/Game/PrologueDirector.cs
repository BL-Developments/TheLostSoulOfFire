using Microsoft.Xna.Framework;

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
    FindBrother,
    BrotherMeeting,
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
    public static readonly Vector2 BrotherMeetingPoint = new(1370f, 545f);
    public static readonly Vector2 VehicleDock = new(1510f, 600f);

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
        PrologueStage.FindBrother => "FOLLOW THE LAST SEARCH FIRE",
        PrologueStage.BrotherMeeting => "YOUR BROTHER FOUND YOU",
        PrologueStage.LeaveSearch => "REACH THE EXTRACTION CAUSEWAY",
        PrologueStage.DevourerPressure => "THE DEVOURER HOLDS A SOUL  SEVER OR BREAK ITS CAVITY",
        PrologueStage.ReleaseWitness => "LET THE SOUL LEAVE",
        PrologueStage.BoardVehicle => "BOARD THE DEATH FLAME SKIFF",
        PrologueStage.Transit => "HOLD THE DECK  USE THE SAME SOUL CANNON",
        PrologueStage.Arrival => "CROSS THE THRESHOLD",
        PrologueStage.Complete => "THE FIRST PLAYABLE IS COMPLETE  R TO REPLAY",
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
            if (Stage == PrologueStage.BrotherMeeting)
            {
                if (StateTime < 2.6f) return "BROTHER  I FELT YOUR FLAME THREE RIDGES BACK";
                if (StateTime < 5.2f) return "YOU  I THOUGHT YOU WERE GONE";
                return "BROTHER  I AM  SO ARE YOU  MOVE";
            }
            if (Stage == PrologueStage.ReleaseWitness)
                return "THE PERSON LEAVES  ONLY THE ECHO RETURNS";
            if (Stage == PrologueStage.Transit && StateTime < 5f)
                return "BROTHER  THE THRESHOLD IS SIXTY SECONDS EAST";
            if (Stage == PrologueStage.Arrival)
                return StateTime < 3.6f ? "BROTHER  THIS IS WHERE WARDENS HOLD" : "NOT FOREVER  ONLY UNTIL THE WORK IS DONE";
            if (Stage == PrologueStage.Complete)
                return "YOU WOKE ALONE  YOU ARRIVE WITH SOMEONE WHO REMEMBERS YOU";
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
