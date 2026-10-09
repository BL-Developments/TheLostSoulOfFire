namespace TheLostSoulOfFire.Game.Levels;

/// <summary>
/// The state of one level run: which room the player stands in and whether its exits are open.
/// Pure state, so room changes can be tested without MonoGame.
/// </summary>
public sealed class LevelRun
{
    public LevelRun(LevelLayout layout)
    {
        Layout = layout;
        Current = layout.Start;
        IsCleared = !HasEncounter;
    }

    public LevelLayout Layout { get; }

    public LevelRoom Current { get; private set; }

    /// <summary>True once the room's encounter is over, or at once for rooms without one.</summary>
    public bool IsCleared { get; private set; }

    public int Seed => Layout.Seed;

    public bool HasEncounter => Current.Kind == LevelRoomKind.Combat;

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
        return true;
    }
}
