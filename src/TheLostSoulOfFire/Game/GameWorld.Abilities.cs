using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

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
        bool rendered = _art.HasClip(VisualIds.Player, VisualClips.Aim);
        foreach (AbilityProjectile projectile in _abilities.Projectiles)
        {
            // The piercing shot flies at body height like the cannon's shots: a soul-fire bolt.
            Vector2 travel = projectile.Position - projectile.PreviousPosition;
            float angle = travel.LengthSquared() > 0.01f ? MathF.Atan2(travel.Y, travel.X) : 0f;
            Vector2 drawn = rendered ? projectile.Position - new Vector2(0f, FigureHeights.Air) : projectile.Position;
            _art.DrawSoftSpot(batch, drawn, new Vector2(30f), GameBalance.DeathFlame * 0.35f);
            _art.DrawLoopingEffect(batch, projectile, VisualIds.CannonProjectileFull, drawn, angle, 0.62f, Color.White);
        }
        if (_abilities.VortexRemaining > 0)
        {
            // The vortex: a slowly turning well of Death Flame drawing motes inward.
            Vector2 center = _abilities.VortexCenter;
            float strength = MathHelper.Clamp(_abilities.VortexRemaining / 0.3f, 0f, 1f);
            _art.DrawSoftSpot(batch, center, new Vector2(155f, 155f), GameBalance.DeepViolet * (0.22f * strength));
            WorldMarks.Ring(batch, pixel, center, 150f, GameBalance.DeathFlame * (0.35f * strength));
            WorldMarks.Ring(batch, pixel, center, 70f + MathF.Sin(_presentationTime * 5f) * 6f, GameBalance.DeathFlameBright * (0.3f * strength));
            for (int i = 0; i < 18; i++)
            {
                float local = (_presentationTime * 0.8f + i / 18f) % 1f;
                float radius = MathHelper.Lerp(150f, 18f, local * local);
                float angle = i * 2.4f + _presentationTime * 3f + local * 4f;
                Vector2 point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                float glow = MathF.Sin(local * MathF.PI) * strength;
                _art.DrawSoftSpot(batch, point, new Vector2(9f), GameBalance.DeathFlame * (0.6f * glow));
                _art.DrawSoftSpot(batch, point, new Vector2(3.5f), GameBalance.SoulWhite * (0.8f * glow));
            }
            _art.DrawSoftSpot(batch, center, new Vector2(22f), GameBalance.SoulWhite * (0.35f * strength));
        }
        foreach (var enemy in _enemies)
            if (enemy.IsAlive && enemy.AbilityMarkRemaining > 0)
            {
                // A Glut mark burning above the head of a marked enemy.
                Vector2 mark = enemy.DrawnAsFigure ? enemy.Position - new Vector2(0f, 122f) : enemy.Position - Vector2.UnitY * (enemy.Radius + 20);
                float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 6f);
                _art.DrawSoftSpot(batch, mark, new Vector2(16f + pulse * 3f), GameBalance.Glut * 0.45f);
                UiKit.FillDiamond(batch, pixel, mark, 6, GameBalance.Glut);
                UiKit.FillDiamond(batch, pixel, mark, 3, GameBalance.GlutBright);
            }
        if (_player.AbilityEffects.GuardRemaining > 0 || _player.AbilityEffects.RevengeRemaining > 0)
        {
            // Guard and the stored counter: a ward of Death Flame around the body.
            Vector2 body = rendered ? _player.Position - new Vector2(0f, FigureHeights.Core) : _player.Position;
            float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 4f);
            bool guard = _player.AbilityEffects.GuardRemaining > 0;
            _art.DrawSoftSpot(batch, body, new Vector2(42f), GameBalance.DeathFlameBright * (guard ? 0.18f : 0.1f));
            WorldMarks.Ring(batch, pixel, body, 40f + pulse * 3f, GameBalance.DeathFlameBright * (guard ? 0.7f : 0.4f), guard);
        }
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
