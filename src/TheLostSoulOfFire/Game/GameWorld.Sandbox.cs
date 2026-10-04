using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Sandbox (<c>--dev --start sandbox</c>, #104): the arena's look and combat without waves,
/// intro, chests, currencies or completion. Only the developer start enters it; the regular
/// flow never sets <see cref="_sandboxActive"/>. <c>F</c> opens the dev menu there.
/// </summary>
public sealed partial class GameWorld
{
    private readonly DevMenu _devMenu = new(SandboxDevMenuEntries.All);
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

    /// <summary>
    /// The dev menu freezes the sandbox like the pause menu. <c>F</c> or <c>Escape</c> close it
    /// without opening the pause menu; what an entry does lives in the handlers below.
    /// </summary>
    private void UpdateDevMenu(float deltaTime, InputState input)
    {
        _devMenu.Tick(deltaTime);
        if (input.WasKeyPressed(Keys.F) || input.WasKeyPressed(Keys.Escape))
        {
            _devMenu.Close();
            _audio.SetPaused(false);
            return;
        }

        if (input.WasKeyPressed(Keys.Up) || input.WasKeyPressed(Keys.W)) _devMenu.MoveSelection(-1);
        else if (input.WasKeyPressed(Keys.Down) || input.WasKeyPressed(Keys.S)) _devMenu.MoveSelection(1);

        bool largeStep = input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift);
        if (_devMenu.SelectedEntry is { Kind: DevMenuEntryKind.Value } valueEntry)
        {
            if (input.WasKeyPressed(Keys.Left) || input.WasKeyPressed(Keys.A)) AdjustDevEntry(valueEntry, -1, largeStep);
            else if (input.WasKeyPressed(Keys.Right) || input.WasKeyPressed(Keys.D)) AdjustDevEntry(valueEntry, 1, largeStep);
        }

        if (input.WasKeyPressed(Keys.Enter) && _devMenu.SelectedEntry is { Kind: DevMenuEntryKind.Action } actionEntry)
        {
            ActivateDevEntry(actionEntry);
        }

        IReadOnlyList<Rectangle> bounds = DevMenuRenderer.GetEntryBounds(_devMenu);
        for (int i = 0; i < bounds.Count; i++)
        {
            if (!bounds[i].Contains(input.MouseVirtualPosition))
            {
                continue;
            }

            if (input.MouseMoved || input.WasLeftMousePressed) _devMenu.Select(i);
            if (input.WasLeftMousePressed)
            {
                DevMenuEntry entry = _devMenu.Entries[i];
                if (entry.Kind == DevMenuEntryKind.Action) ActivateDevEntry(entry);
                else AdjustDevEntry(entry, input.MouseVirtualPosition.X < bounds[i].Center.X ? -1 : 1, largeStep);
            }
            break;
        }
    }

    private void AdjustDevEntry(DevMenuEntry entry, int direction, bool largeStep)
    {
    }

    private void ActivateDevEntry(DevMenuEntry entry)
    {
    }

    private string? DevEntryValue(DevMenuEntry entry) => null;

    private string? DevMenuFooter => null;

    private void DrawDevMenu(SpriteBatch batch, Texture2D pixel, Viewport viewport) =>
        DevMenuRenderer.Draw(batch, pixel, viewport, _devMenu, DevEntryValue, DevMenuFooter);

    private void DrawSandboxHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        PixelText.DrawCentered(batch, pixel, "SANDBOX · F DEV-MENÜ", viewport.Width * 0.5f, 28f, 2, GameBalance.DeathFlameBright * 0.7f);
    }
}
