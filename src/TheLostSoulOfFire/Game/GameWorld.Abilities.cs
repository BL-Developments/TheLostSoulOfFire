using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

public sealed partial class GameWorld
{
    private readonly RunAbilities _abilities = new();
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

    private bool AbilityChoicePhase => !_player.IsDead && (_phase == GamePhase.Antechamber ||
        _phase == GamePhase.Arena && (_sandboxActive || _loopState is ArenaLoopState.Intermission or ArenaLoopState.Intro));
    private bool CanChooseAbilities => !_pauseMenu.IsOpen && !_characterMenu.IsOpen && !_devMenu.IsOpen && AbilityChoicePhase;

    private AbilityCard[] CurrentAbilityCards()
    {
        var cards = new AbilityCard[RunAbilities.Definitions.Length];
        for (int i = 0; i < cards.Length; i++)
            cards[i] = AbilityCard.Create((RunAbility)i, _abilities, _player, _wallet.Run(Currency.Glut),
                _phase == GamePhase.Arena && _loopState == ArenaLoopState.Combat, freeCast: _sandboxActive);
        return cards;
    }

    private void OpenSkillsMenu()
    {
        _characterMenu.Open();
        _characterMenu.Select(TheLostSoulOfFire.Menu.CharacterMenuTab.Abilities);
        _audio.SetPaused(true);
    }

    internal void ShowAutomatedSkillsMenu(bool open)
    {
        if (open) OpenSkillsMenu();
        else { _characterMenu.Close(); _audio.SetPaused(false); }
    }

    internal void VerifyAutomatedSkillLoadout()
    {
        if (_abilities.Slots[0] != RunAbility.Revenge || _abilities.Slots[1] != RunAbility.Vortex)
            throw new InvalidOperationException("Skill menu did not equip the expected slots.");
    }

    private bool HandleAbilitySelection(InputState input)
    {
        if (!CanChooseAbilities || _devMenu.IsOpen || !input.WasKeyPressed(Keys.C)) return false;
        OpenSkillsMenu();
        return true;
    }

    private void UpdateSkillSelection(InputState input, Viewport viewport)
    {
        if (input.WasKeyPressed(Keys.Z)) _characterMenu.SelectSkillSlot(0);
        if (input.WasKeyPressed(Keys.X)) _characterMenu.SelectSkillSlot(1);
        Keys[] keys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6];
        for (int i = 0; i < keys.Length; i++)
            if (input.WasKeyPressed(keys[i])) _characterMenu.EquipSkill(_abilities, (RunAbility)i, AbilityChoicePhase);
        if (!input.WasLeftMousePressed) return;
        for (int slot = 0; slot < 2; slot++)
            if (AbilityPresentation.SlotBounds(viewport, slot).Contains(input.MouseVirtualPosition))
            {
                _characterMenu.SelectSkillSlot(slot);
                return;
            }
        for (int i = 0; i < RunAbilities.Definitions.Length; i++)
            if (AbilityPresentation.CatalogueBounds(viewport, i).Contains(input.MouseVirtualPosition))
            {
                _characterMenu.EquipSkill(_abilities, (RunAbility)i, AbilityChoicePhase);
                return;
            }
    }

    private void UpdateAbilities(float dt, InputState input)
    {
        if (_phase != GamePhase.Arena) return;
        // Only arena combat owns ability simulation. Other phases cannot spend the wallet.
        if (_loopState != ArenaLoopState.Combat) { _abilities.Clear(_player); return; }
        _abilities.Update(dt, _player, ActiveCombatBounds, _enemies, _particles, ApplyEnemyDamage);
        if (!CombatActionsEnabled || _player.IsDead) return;
        // The sandbox has no Glut; casting there is free, cooldowns still apply.
        if (input.WasKeyPressed(Keys.Z) && _abilities.TryCast(_abilities.Slots[0], _player, _wallet,
            _lastMouseWorld, ActiveCombatBounds, _enemies, _particles, chargeCost: !_sandboxActive))
            PlayAbilityCue(_abilities.Slots[0]);
        if (input.WasKeyPressed(Keys.X) && _abilities.TryCast(_abilities.Slots[1], _player, _wallet,
            _lastMouseWorld, ActiveCombatBounds, _enemies, _particles, chargeCost: !_sandboxActive))
            PlayAbilityCue(_abilities.Slots[1]);
    }

    /// <summary>Each ability has its own sound when it is actually cast (presentation only).</summary>
    private void PlayAbilityCue(RunAbility ability)
    {
        AudioCue cue = ability switch
        {
            RunAbility.SecondWind => AudioCue.AbilityHeal,
            RunAbility.PiercingShot => AudioCue.AbilityPierce,
            RunAbility.Retreat => AudioCue.AbilityLeap,
            RunAbility.Vortex => AudioCue.AbilityVortex,
            RunAbility.Revenge => AudioCue.AbilityGuard,
            _ => AudioCue.AbilityMark
        };
        _audio.Play(cue, 0.72f);
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
        if (_characterMenu.IsOpen || _pauseMenu.IsOpen || _devMenu.IsOpen || _player.IsDead) return;
        if (_phase != GamePhase.Arena && _phase != GamePhase.Antechamber) return;
        if (_phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete) return;
        var cards = CurrentAbilityCards();
        for (int slot = 0; slot < 2; slot++)
            AbilityPresentation.DrawHud(batch, pixel, viewport, cards[(int)_abilities.Slots[slot]], _presentationTime);
        float hintX = DrawKeyHint(batch, pixel, new Vector2(24, viewport.Height - 62), "TAB", "FÄHIGKEITEN UND CHARAKTER", GameBalance.SoulWhite * 0.72f);
        if (CanChooseAbilities)
            DrawKeyHint(batch, pixel, new Vector2(hintX + 28, viewport.Height - 62), "C", "FÄHIGKEITEN WÄHLEN", GameBalance.DeathFlameBright);
        if (_abilities.FeedbackRemaining > 0)
            PixelText.DrawCentered(batch, pixel, _abilities.Feedback, viewport.Width * 0.5f, 85, 1, GameBalance.GlutBright);
        if (_player.AbilityEffects.SetupRemaining > 0 || _player.AbilityEffects.RevengeRemaining > 0)
            PixelText.DrawCentered(batch, pixel, "NÄCHSTER TREFFER VERSTÄRKT", viewport.Width * 0.5f, 102, 1, GameBalance.DeathFlameBright);
    }

    /// <summary>A keycap followed by its label; returns the right edge.</summary>
    private static float DrawKeyHint(SpriteBatch batch, Texture2D pixel, Vector2 position, string key, string label, Color color)
    {
        UiKit.Key(batch, pixel, position, key, color, 1f, 1, 17);
        float labelX = position.X + UiKit.KeyWidth(key, 1) + 8;
        PixelText.DrawFace(batch, pixel, label, new Vector2(labelX, position.Y + 4f), TextFace.Body, 9.5f, color, 0.5f);
        return labelX + PixelText.MeasureFace(label, TextFace.Body, 9.5f, 0.5f);
    }
}
