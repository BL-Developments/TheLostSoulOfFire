using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

public sealed partial class GameWorld
{
    private readonly RunAbilities _abilities = new();
    private bool _abilitySelectionOpen;
    private int _abilitySelectionSlot;
    internal void ShowAutomatedAbility(RunAbility ability)
    {
        _player.Reset(ActiveCombatBounds.Center.ToVector2());
        _wallet.BeginRun(30);
        _enemies.Clear();
        _pendingSpawns.Clear();
        _enemies.Add(new TheLostSoulOfFire.Entities.Hollow(_player.Position + new Vector2(105, 0), 1));
        _enemies.Add(new TheLostSoulOfFire.Entities.Hollow(_player.Position + new Vector2(150, 35), 2));
        _abilities.Clear(_player);
        if (ability == RunAbility.SecondWind) _player.ApplyDamage(40, Vector2.Zero, _screenEffects, true);
        if (!_abilities.TryCast(ability, _player, _wallet, _player.Position + new Vector2(140, 0),
            ActiveCombatBounds, _enemies, _particles)) throw new InvalidOperationException("Automated cast failed: " + ability);
        if (ability == RunAbility.Revenge) _player.AbilityEffects.TryBlock();
        if (ability == RunAbility.Setup)
        {
            ApplyWeaponDamage(_enemies[0], new DamageInfo(1, Vector2.Zero, _enemies[0].Position));
        }
    }

    /// <summary>The sandbox has no intermission, so there the selection is open during combat as well.</summary>
    private bool CanChooseAbilities => !_pauseMenu.IsOpen && !_characterMenu.IsOpen && !_devMenu.IsOpen && !_player.IsDead && (_phase == GamePhase.Antechamber ||
        _phase == GamePhase.Arena && (_sandboxActive || _loopState is ArenaLoopState.Intermission or ArenaLoopState.Intro));

    private bool HandleAbilitySelection(InputState input)
    {
        if (!_abilitySelectionOpen && CanChooseAbilities && input.WasKeyPressed(Keys.C))
        {
            _abilitySelectionOpen = true;
            _audio.SetPaused(true);
            return true;
        }
        if (!_abilitySelectionOpen) return false;
        if (input.WasKeyPressed(Keys.C) || input.WasKeyPressed(Keys.Escape) || input.WasKeyPressed(Keys.Enter))
        {
            _abilitySelectionOpen = false;
            _audio.SetPaused(false);
            return true;
        }
        if (input.WasKeyPressed(Keys.Left) || input.WasKeyPressed(Keys.Right))
            _abilitySelectionSlot = 1 - _abilitySelectionSlot;
        Keys[] keys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6];
        for (int i = 0; i < keys.Length; i++)
            if (input.WasKeyPressed(keys[i])) _abilities.Equip(_abilitySelectionSlot, (RunAbility)i);
        return true;
    }

    private void UpdateAbilities(float dt, InputState input)
    {
        if (_phase != GamePhase.Arena) return;
        // Only arena combat owns ability simulation. Other phases cannot spend the wallet.
        if (_loopState != ArenaLoopState.Combat) { _abilities.Clear(_player); return; }
        _abilities.Update(dt, _player, ActiveCombatBounds, _enemies, _particles, ApplyEnemyDamage);
        if (!CombatActionsEnabled || _player.IsDead) return;
        // The sandbox has no Glut; casting there is free, cooldowns still apply.
        if (input.WasKeyPressed(Keys.Z)) _abilities.TryCast(_abilities.Slots[0], _player, _wallet,
            _lastMouseWorld, ActiveCombatBounds, _enemies, _particles, chargeCost: !_sandboxActive);
        if (input.WasKeyPressed(Keys.X)) _abilities.TryCast(_abilities.Slots[1], _player, _wallet,
            _lastMouseWorld, ActiveCombatBounds, _enemies, _particles, chargeCost: !_sandboxActive);
    }

    private void DrawAbilityWorld(SpriteBatch batch, Texture2D pixel)
    {
        if (_phase != GamePhase.Arena) return;
        foreach (AbilityProjectile projectile in _abilities.Projectiles)
        {
            batch.FillCircle(pixel, projectile.Position, 15, GameBalance.DeathFlame * 0.25f);
            batch.FillCircle(pixel, projectile.Position, 6, GameBalance.SoulWhite * 0.9f);
        }
        if (_abilities.VortexRemaining > 0)
        {
            batch.FillCircle(pixel, _abilities.VortexCenter, 155, GameBalance.DeepViolet * 0.13f);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * MathF.Tau / 12 + _presentationTime * 3;
                float radius = 30 + (i % 4) * 30;
                Vector2 point = _abilities.VortexCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                batch.FillCircle(pixel, point, 7, GameBalance.DeathFlameBright * 0.35f);
            }
        }
        foreach (var enemy in _enemies)
            if (enemy.IsAlive && enemy.AbilityMarkRemaining > 0)
                batch.FillCircle(pixel, enemy.Position - Vector2.UnitY * (enemy.Radius + 20), 6, GameBalance.GlutBright);
        if (_player.AbilityEffects.GuardRemaining > 0 || _player.AbilityEffects.RevengeRemaining > 0)
            batch.FillCircle(pixel, _player.Position, 32, GameBalance.DeathFlameBright * 0.16f);
    }

    private void DrawAbilityHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        if (_phase != GamePhase.Arena && _phase != GamePhase.Antechamber) return;
        if (_phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete) return;
        for (int slot = 0; slot < 2; slot++)
        {
            RunAbility ability = _abilities.Slots[slot];
            AbilityDefinition definition = RunAbilities.Definitions[(int)ability];
            float cooldown = _abilities.Cooldown(ability);
            bool affordable = _sandboxActive || _wallet.Run(Currency.Glut) >= definition.Cost;
            int x = 24 + slot * 300;
            Rectangle panel = new(x, viewport.Height - 105, 288, 40);
            batch.FillRectangle(pixel, panel, Color.Black * 0.8f);
            string cost = _sandboxActive ? "FREI" : $"{definition.Cost} GLUT";
            string label = $"{(slot == 0 ? "Z" : "X")}  {definition.Name}  {cost}";
            PixelText.Draw(batch, pixel, label, new Vector2(x + 10, panel.Y + 7), 1,
                affordable ? GameBalance.DeathFlameBright : Color.Gray);
            string state = cooldown > 0 ? $"BEREIT IN {cooldown:0.0}S" :
                !affordable ? "GLUT FEHLT" : "BEREIT";
            PixelText.Draw(batch, pixel, state, new Vector2(x + 10, panel.Y + 23), 1, GameBalance.SoulWhite * 0.7f);
        }
        if (CanChooseAbilities)
            PixelText.Draw(batch, pixel, "C  FAEHIGKEITEN WAEHLEN", new Vector2(650, viewport.Height - 85), 1, GameBalance.DeathFlameBright);
        if (_abilities.FeedbackRemaining > 0)
            PixelText.DrawCentered(batch, pixel, _abilities.Feedback, viewport.Width * 0.5f, 85, 1, GameBalance.GlutBright);
        if (_player.AbilityEffects.SetupRemaining > 0 || _player.AbilityEffects.RevengeRemaining > 0)
            PixelText.DrawCentered(batch, pixel, "NAECHSTER TREFFER VERSTAERKT", viewport.Width * 0.5f, 102, 1, GameBalance.DeathFlameBright);
        if (!_abilitySelectionOpen) return;
        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.88f);
        PixelText.DrawCentered(batch, pixel, "FAEHIGKEITEN", viewport.Width * 0.5f, 125, 3, GameBalance.DeathFlameBright);
        PixelText.DrawCentered(batch, pixel, $"SLOT {_abilitySelectionSlot + 1}  -  LINKS / RECHTS WECHSELN", viewport.Width * 0.5f, 170, 1, GameBalance.SoulWhite);
        for (int i = 0; i < 6; i++)
        {
            AbilityDefinition definition = RunAbilities.Definitions[i];
            int y = 210 + i * 62;
            bool selected = Array.IndexOf(_abilities.Slots, (RunAbility)i) >= 0;
            string cost = _sandboxActive ? "FREI" : $"{definition.Cost} GLUT";
            PixelText.Draw(batch, pixel, $"{i + 1}  {definition.Name}   {cost}{(selected ? "  AUSGERUESTET" : "")}",
                new Vector2(260, y), 2, selected ? GameBalance.GlutBright : GameBalance.DeathFlameBright);
            PixelText.Draw(batch, pixel, definition.Description, new Vector2(260, y + 26), 1, GameBalance.SoulWhite * 0.8f);
        }
        PixelText.DrawCentered(batch, pixel, "1-6 AUSWAEHLEN   ENTER / C SCHLIESSEN", viewport.Width * 0.5f, 625, 1, GameBalance.SoulWhite);
    }
}

