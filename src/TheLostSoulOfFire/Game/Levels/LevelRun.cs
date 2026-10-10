namespace TheLostSoulOfFire.Game.Levels;

/// <summary>
/// The state of one level run: which room the player stands in and whether its exits are open.
/// Pure state, so room changes can be tested without MonoGame.
/// </summary>
public sealed class LevelRun
{
    private readonly bool _guardianAtEnd;

    /// <param name="guardianAtEnd">The level end is the guardian room of a biome (change <c>add-biome-run-flow</c>), not a travel point.</param>
    /// <param name="combatRoomsEntered">Progress carried over from earlier levels of the same run.</param>
    public LevelRun(LevelLayout layout, bool guardianAtEnd = false, int combatRoomsEntered = 0)
    {
        Layout = layout;
        Current = layout.Start;
        _guardianAtEnd = guardianAtEnd;
        CombatRoomsEntered = combatRoomsEntered;
        IsCleared = !HasEncounter;
    }

    public LevelLayout Layout { get; }

    public LevelRoom Current { get; private set; }

    /// <summary>True once the room's encounter is over, or at once for rooms without one.</summary>
    public bool IsCleared { get; private set; }

    public int Seed => Layout.Seed;

    /// <summary>Combat rooms entered in this run, including the current one: the progress that scales their encounters.</summary>
    public int CombatRoomsEntered { get; private set; }

    /// <summary>Seed of the current room's encounter; the same room in the same level always gets the same seed.</summary>
    public int RoomSeed => unchecked(Layout.Seed * 31 + Current.Id);

    /// <summary>The guardian room ends the biome: its encounter has to be cleared before the run completes.</summary>
    public bool IsGuardianRoom => Current.Kind == LevelRoomKind.LevelEnd && _guardianAtEnd;

    public bool HasEncounter => Current.Kind == LevelRoomKind.Combat || IsGuardianRoom;

    public void MarkCleared() => IsCleared = true;

    /// <summary>Takes the exit at <paramref name="exitIndex"/> of the current room once it is open.</summary>
    public bool TryTakeExit(int exitIndex, out LevelRoom next)
    {
        next = Current;
        if (!IsCleared || exitIndex < 0 || exitIndex >= Current.Exits.Count)
        {
            return false;
        }

        next = Layout.Room(Current.Exits[exitIndex]);
        Current = next;
        IsCleared = !HasEncounter;
        if (HasEncounter)
        {
            CombatRoomsEntered++;
        }

        return true;
    }
}
