namespace TheLostSoulOfFire.Game;

/// <summary>
/// Pure transition policy for the top-level run flow. Keeping these decisions
/// independent of MonoGame services makes restart semantics directly testable.
/// </summary>
public static class GameFlowRules
{
    public static GamePhase ConfirmTitle(GamePhase phase, bool skipPrologue = false) =>
        phase == GamePhase.Title
            ? skipPrologue ? GamePhase.Antechamber : GamePhase.Prologue
            : phase;

    public static GamePhase FinishPrologue(GamePhase phase) =>
        phase == GamePhase.Prologue ? GamePhase.Antechamber : phase;

    public static GamePhase EnterDoor(GamePhase phase) =>
        phase == GamePhase.Antechamber ? GamePhase.EnteringArena : phase;

    public static GamePhase FinishDoorTransition(GamePhase phase) =>
        phase == GamePhase.EnteringArena ? GamePhase.Arena : phase;

    public static GamePhase RetryAfterDeath() => GamePhase.Arena;

    public static GamePhase RestartAfterCompletion() => GamePhase.Title;

    /// <summary>Extraction at a travel point ends the run and returns to the hub.</summary>
    public static GamePhase ExtractToHub(GamePhase phase) =>
        phase == GamePhase.Arena ? GamePhase.Antechamber : phase;

    /// <summary>A defeat in a level returns to the hub; the arena-only encounter retry does not apply.</summary>
    public static GamePhase ReturnToHubAfterDefeat(GamePhase phase) =>
        phase == GamePhase.Arena ? GamePhase.Antechamber : phase;

    public static bool AllowsCombat(GamePhase phase) => phase is GamePhase.Prologue or GamePhase.Arena;
}
