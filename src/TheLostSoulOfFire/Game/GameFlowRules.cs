namespace TheLostSoulOfFire.Game;

/// <summary>
/// Pure transition policy for the top-level run flow. Keeping these decisions
/// independent of MonoGame services makes restart semantics directly testable.
/// </summary>
public static class GameFlowRules
{
    public static GamePhase ConfirmTitle(GamePhase phase) =>
        phase == GamePhase.Title ? GamePhase.Antechamber : phase;

    public static GamePhase EnterGate(GamePhase phase) =>
        phase == GamePhase.Antechamber ? GamePhase.EnteringArena : phase;

    public static GamePhase FinishGateTransition(GamePhase phase) =>
        phase == GamePhase.EnteringArena ? GamePhase.Arena : phase;

    public static GamePhase RetryAfterDeath() => GamePhase.Arena;

    public static GamePhase RestartAfterCompletion() => GamePhase.Title;

    public static bool AllowsCombat(GamePhase phase) => phase == GamePhase.Arena;
}
