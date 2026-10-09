namespace TheLostSoulOfFire.Game.Levels;

public enum LevelRoomKind
{
    Start,
    Combat,
    LevelEnd,
}

/// <summary>
/// One room of a level. <paramref name="Progress"/> is the stage index (start = 0, first combat
/// stage = 1); <paramref name="Exits"/> are the ids of the next stage, left before right.
/// </summary>
public sealed record LevelRoom(int Id, LevelRoomKind Kind, int Progress, IReadOnlyList<int> Exits);

/// <summary>Limits for <see cref="LevelLayoutGenerator"/>: number of combat stages and the chance of a fork.</summary>
public sealed record LevelLayoutSettings(int CombatStagesMin, int CombatStagesMax, float ForkChance)
{
    public static LevelLayoutSettings Default { get; } = new(
        GameBalance.LevelCombatStagesMin,
        GameBalance.LevelCombatStagesMax,
        GameBalance.LevelForkChance);
}

/// <summary>The room sequence of one level: a start room, one or two combat rooms per stage, and the level end.</summary>
public sealed class LevelLayout
{
    private readonly List<LevelRoom> _roomsById;

    public LevelLayout(int seed, IReadOnlyList<IReadOnlyList<LevelRoom>> stages)
    {
        Seed = seed;
        Stages = stages;
        _roomsById = [];
        foreach (IReadOnlyList<LevelRoom> stage in stages)
        {
            _roomsById.AddRange(stage);
        }
    }

    public IReadOnlyList<IReadOnlyList<LevelRoom>> Stages { get; }

    public int Seed { get; }

    public LevelRoom Start => Stages[0][0];

    /// <summary>Stages between the start room and the level end.</summary>
    public int CombatStageCount => Stages.Count - 2;

    /// <summary>Ids are assigned in stage order from 0, so an id is also the room's index.</summary>
    public LevelRoom Room(int id) => _roomsById[id];
}
