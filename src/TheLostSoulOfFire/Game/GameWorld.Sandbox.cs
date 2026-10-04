using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Sandbox (<c>--dev --start sandbox</c>, #104): the arena's look and combat without waves,
/// intro, chests, currencies or completion. Only the developer start enters it; the regular
/// flow never sets <see cref="_sandboxActive"/>.
/// </summary>
public sealed partial class GameWorld
{
    private bool _sandboxActive;

    public bool IsSandbox => _sandboxActive;

    private void BeginSandbox(Viewport viewport)
    {
        ClearRunState();
        _sandboxActive = true;
        _phase = GamePhase.Arena;
        _phaseTime = 0f;
        _loopState = ArenaLoopState.Combat;
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Zoom = 1f;
        _camera.Follow(_player.Position, _arena.Bounds, viewport, 1f);
        _audio.SetCalm(true);
        _audio.SetSoulSense(false);
        _audio.SetArenaActive(true);
    }

    /// <summary>Death or <c>F8</c> in the sandbox: the player starts again in the middle, the field is cleared.</summary>
    private void ResetSandbox()
    {
        ClearRunState();
        _sandboxActive = true;
        _phaseTime = 0f;
        _loopState = ArenaLoopState.Combat;
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _audio.SetCalm(true);
        _audio.SetSoulSense(false);
    }

    private void DrawSandboxHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        PixelText.DrawCentered(batch, pixel, "SANDBOX", viewport.Width * 0.5f, 28f, 2, GameBalance.DeathFlameBright * 0.7f);
    }
}
