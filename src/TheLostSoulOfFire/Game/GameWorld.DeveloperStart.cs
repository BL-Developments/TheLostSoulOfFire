using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Debugging;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Developer start (<c>--dev --start</c>): jumps straight into an area by calling the same
/// entry points the regular flow uses, so nothing after the jump behaves differently.
/// </summary>
public sealed partial class GameWorld
{
    public void ApplyDeveloperStart(DeveloperStartOptions options, Viewport viewport)
    {
        switch (options.Area)
        {
            case DeveloperStartArea.Title:
                return;

            case DeveloperStartArea.Prologue:
                BeginPrologue(viewport);
                return;

            case DeveloperStartArea.PrologueFindTrace:
                EnterPrologueStageDirectly(PrologueStage.FindTrace, viewport);
                return;

            case DeveloperStartArea.PrologueSearch:
                EnterPrologueStageDirectly(PrologueStage.SearchApproach, viewport);
                return;

            case DeveloperStartArea.PrologueDevourer:
                EnterPrologueStageDirectly(PrologueStage.DevourerPressure, viewport);
                return;

            case DeveloperStartArea.PrologueTransit:
                EnterPrologueStageDirectly(PrologueStage.Transit, viewport);
                return;

            case DeveloperStartArea.Hub:
                BeginAntechamber(viewport);
                return;

            case DeveloperStartArea.Arena:
                ClearRunState();
                _phase = GamePhase.Arena;
                BeginArenaIntro(viewport);
                // The intro always spawns the wave after the current one.
                _waveNumber = options.Wave - 1;
                return;
        }
    }

    private void EnterPrologueStageDirectly(PrologueStage stage, Viewport viewport)
    {
        BeginPrologue(viewport);
        DebugEnterPrologueStage(stage);
        _camera.Follow(_player.Position, PrologueDirector.WorldBounds, viewport, 1f);
    }
}
