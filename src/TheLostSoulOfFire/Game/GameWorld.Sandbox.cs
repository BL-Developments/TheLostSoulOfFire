using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
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
    /// <summary>Attributes at sandbox start (default or the <c>--strength</c>/<c>--armor</c> flags); ZURÜCKSETZEN returns to them.</summary>
    private PlayerAttributes _sandboxStartAttributes = PlayerAttributes.Default;
    private int _sandboxSpawnCount;

    private void BeginSandbox(Viewport viewport)
    {
        ClearRunState();
        _sandboxActive = true;
        _sandboxStartAttributes = _player.Attributes;
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
        ClearRunState(stayInSandbox: true);
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

    /// <summary>Leaving the sandbox: the regular flow starts with the regular health and the start attributes.</summary>
    private void RestoreSandboxStartValues()
    {
        _player.Attributes = _sandboxStartAttributes;
        _player.SetMaxHealth(GameBalance.PlayerMaxHealth);
    }

    private void AdjustDevEntry(DevMenuEntry entry, int direction, bool largeStep)
    {
        PlayerAttributes attributes = _player.Attributes;
        DevValueRange attribute = DevValueRange.Attribute;
        switch (entry.Id)
        {
            case SandboxDevMenuEntries.Health:
                _player.SetMaxHealth(DevValueRange.Health.Adjust(_player.MaxHealth, direction, largeStep));
                break;
            case SandboxDevMenuEntries.Strength:
                _player.Attributes = attributes with { Strength = attribute.Adjust(attributes.Strength, direction, largeStep) };
                break;
            case SandboxDevMenuEntries.AbilityPower:
                _player.Attributes = attributes with { AbilityPower = attribute.Adjust(attributes.AbilityPower, direction, largeStep) };
                break;
            case SandboxDevMenuEntries.Armor:
                _player.Attributes = attributes with { Armor = attribute.Adjust(attributes.Armor, direction, largeStep) };
                break;
        }
    }

    private void ActivateDevEntry(DevMenuEntry entry)
    {
        if (SandboxDevMenuEntries.SpawnKind(entry) is { } kind)
        {
            SpawnSandboxEnemy(kind);
            return;
        }

        switch (entry.Id)
        {
            case SandboxDevMenuEntries.ResetCharacter:
                RestoreSandboxStartValues();
                break;
            case SandboxDevMenuEntries.RemoveEnemies:
                RemoveSandboxEnemies();
                break;
        }
    }

    private void SpawnSandboxEnemy(SandboxEnemyKind kind)
    {
        Vector2 position = SandboxSpawner.ChoosePosition(_arena.CombatBounds, _player.Position, _sandboxSpawnCount, SandboxSpawner.Radius(kind));
        _sandboxSpawnCount++;
        _enemies.Add(SandboxSpawner.Create(kind, position, 1000 + _sandboxSpawnCount));
        _particles.EmitDeathFlame(position, 10, 0.7f);
    }

    /// <summary>Takes every enemy and lost soul off the field without defeat effects.</summary>
    private void RemoveSandboxEnemies()
    {
        foreach (Enemy enemy in _enemies)
        {
            _particles.EmitDeathFlame(enemy.Position, 6, 0.5f);
        }
        _enemies.Clear();
        _souls.Clear();
    }

    private string? DevEntryValue(DevMenuEntry entry) => entry.Id switch
    {
        SandboxDevMenuEntries.Health => _player.MaxHealth.ToString(),
        SandboxDevMenuEntries.Strength => _player.Attributes.Strength.ToString(),
        SandboxDevMenuEntries.AbilityPower => _player.Attributes.AbilityPower.ToString(),
        SandboxDevMenuEntries.Armor => _player.Attributes.Armor.ToString(),
        SandboxDevMenuEntries.RemoveEnemies => _enemies.Count(enemy => enemy.IsAlive).ToString(),
        _ when SandboxDevMenuEntries.SpawnKind(entry) is { } kind => _enemies.Count(enemy => enemy.IsAlive && SandboxSpawner.IsKind(enemy, kind)).ToString(),
        _ => null
    };

    private void DrawDevMenu(SpriteBatch batch, Texture2D pixel, Viewport viewport) =>
        DevMenuRenderer.Draw(batch, pixel, viewport, _devMenu, DevEntryValue);

    private void DrawSandboxHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        PixelText.DrawCentered(batch, pixel, "SANDBOX · F DEV-MENÜ", viewport.Width * 0.5f, 28f, 2, GameBalance.DeathFlameBright * 0.7f);
    }
}
