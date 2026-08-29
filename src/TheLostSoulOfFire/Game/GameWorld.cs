using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

public enum ArenaLoopState
{
    Intro,
    Combat,
    Transition,
    Complete
}

public sealed class GameWorld
{
    private readonly Arena _arena = new();
    private readonly Camera2D _camera;
    private readonly ScreenEffects _screenEffects = new();
    private readonly ParticleSystem _particles = new();
    private readonly Player _player;
    private readonly List<Enemy> _enemies = [];
    private readonly List<Soul> _souls = [];
    private readonly List<CannonShot> _cannonShots = [];
    private Vector2 _lastMouseWorld;
    private bool _debugVisible;
    private int _waveNumber;
    private ArenaLoopState _loopState = ArenaLoopState.Intro;
    private float _loopStateTimer = 1.25f;
    private float _burningHandoffTimer;
    private int _burningCommittedLastFrame;

    public string ScreenshotContext => GetScreenshotContext();

    public string WindowTitle => _debugVisible
        ? $"The Lost Soul of Fire — DEBUG | Wave {_waveNumber}/4 {_loopState.ToString().ToUpperInvariant()} | HP {_player.Health} | RES {(_player.ResonanceActive ? $"ACTIVE {_player.ResonanceRemaining:0.0}s" : $"{_player.Resonance:0}/{GameBalance.ResonanceRequired:0}")} | Player {GetPlayerState()} | Enemies {_enemies.Count(enemy => enemy.IsAlive)} | Souls {_souls.Count}"
        : _player.IsDead
            ? "The Lost Soul of Fire — Flame extinguished | R retry | F9 screenshot"
            : _loopState == ArenaLoopState.Complete
                ? "The Lost Soul of Fire — Arena cleared | R restart | F9 screenshot"
                : $"The Lost Soul of Fire — Wave {_waveNumber}/4 | WASD move | Mouse aim | Space dash | LMB Scythe | Q Soul Sense | RMB Cannon";

    public GameWorld(Viewport viewport)
    {
        _camera = new Camera2D(_arena.CombatBounds.Center.ToVector2());
        _player = new Player(_arena.CombatBounds.Center.ToVector2());
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Follow(_arena.CombatBounds.Center.ToVector2(), _arena.Bounds, viewport);
    }

    public void Update(GameTime gameTime, InputState input, Viewport viewport)
    {
        float deltaTime = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 20f);
        _screenEffects.Update(deltaTime);

        if (input.WasKeyPressed(Keys.F1))
        {
            _debugVisible = !_debugVisible;
        }

        if (input.WasKeyPressed(Keys.F2))
        {
            _enemies.Add(new Hollow(_player.Position + new Vector2(290f, 0f), _enemies.Count + 1));
        }

        if (input.WasKeyPressed(Keys.F3))
        {
            _enemies.Add(new Burning(_player.Position + new Vector2(310f, 0f), _enemies.Count + 1));
        }

        if (input.WasKeyPressed(Keys.F4))
        {
            _enemies.Add(new Devourer(_player.Position + new Vector2(390f, 0f)));
        }

        if (input.WasKeyPressed(Keys.F5))
        {
            _player.FillResonance();
        }

        if (input.WasKeyPressed(Keys.F8))
        {
            ResetEncounter();
        }

        if (_player.IsDead && input.WasKeyPressed(Keys.R))
        {
            ResetEncounter();
        }

        if (_loopState == ArenaLoopState.Complete && input.WasKeyPressed(Keys.R))
        {
            ResetEncounter();
        }

        _lastMouseWorld = _camera.ScreenToWorld(input.MousePosition, viewport);

        if (_screenEffects.IsHitStopped)
        {
            return;
        }

        _player.Update(deltaTime, input, _lastMouseWorld, _arena.CombatBounds, _particles, _screenEffects);
        SpawnCannonShot();
        ResolveScytheStrike();
        UpdateCannonShots(deltaTime);
        UpdateBurningHandoff();
        ConfigureBurningAggression(deltaTime);
        foreach (Enemy enemy in _enemies)
        {
            enemy.Update(deltaTime, _player, _souls, _arena.CombatBounds, _particles, _screenEffects);
            if (enemy.TryConsumeSoulSpawn(out Vector2 soulPosition))
            {
                _souls.Add(new Soul(soulPosition));
            }

            if (enemy is Burning burning && burning.TryConsumeDetonation(out Vector2 detonationPosition))
            {
                ResolveBurningDetonation(burning, detonationPosition);
            }

            if (enemy is Devourer devourer && devourer.TryConsumeExtractionEffect(out Vector2 extractionPosition))
            {
                _particles.EmitBurst(extractionPosition, Vector2.UnitY, 28, GameBalance.SoulWhite, 260f, 9f);
                _particles.EmitDeathFlame(extractionPosition, 18, 1.25f);
                _screenEffects.AddShake(0.18f, 8f);
                _screenEffects.Flash(0.08f, 0.26f);
            }
        }

        UpdateBurningHandoff();

        _enemies.RemoveAll(enemy => enemy.IsFinished);
        foreach (Soul soul in _souls)
        {
            soul.Update(deltaTime, _player, _particles);
        }

        _souls.RemoveAll(soul => soul.IsFinished);
        UpdateArenaLoop(deltaTime);
        _particles.Update(deltaTime);

        float cameraSmoothing = 1f - MathF.Exp(-deltaTime * 9f);
        _camera.Follow(_player.Position, _arena.Bounds, viewport, cameraSmoothing);
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            transformMatrix: _camera.GetTransform(viewport, _screenEffects.ShakeOffset));

        _arena.Draw(batch, pixel);
        DrawArenaLoop(batch, pixel);
        if (_player.SoulSenseActive)
        {
            batch.FillRectangle(pixel, _arena.Bounds, new Color(4, 7, 11) * 0.48f);
            DrawSoulTraces(batch, pixel);
        }
        _player.DrawAfterimages(batch, pixel);
        foreach (Enemy enemy in _enemies)
        {
            enemy.Draw(batch, pixel, _debugVisible, _player.SoulSenseActive);
        }
        foreach (Soul soul in _souls)
        {
            soul.Draw(batch, pixel, _player, _player.SoulSenseActive);
        }
        foreach (CannonShot shot in _cannonShots)
        {
            shot.Draw(batch, pixel);
        }
        _particles.Draw(batch, pixel);
        _player.Draw(batch, pixel, _debugVisible);

        batch.DrawCircle(pixel, _lastMouseWorld, 9f, GameBalance.DeathFlameBright * 0.75f, 2f, 16);
        batch.DrawLine(pixel, _lastMouseWorld - Vector2.UnitX * 13f, _lastMouseWorld + Vector2.UnitX * 13f, GameBalance.DeathFlame * 0.6f, 1f);
        batch.DrawLine(pixel, _lastMouseWorld - Vector2.UnitY * 13f, _lastMouseWorld + Vector2.UnitY * 13f, GameBalance.DeathFlame * 0.6f, 1f);

        if (_debugVisible)
        {
            batch.DrawRectangle(pixel, _arena.CombatBounds, new Color(80, 220, 210) * 0.8f, 3f);
            Vector2 center = _arena.CombatBounds.Center.ToVector2();
            batch.DrawLine(pixel, center - Vector2.UnitX * 28f, center + Vector2.UnitX * 28f, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, center - Vector2.UnitY * 28f, center + Vector2.UnitY * 28f, new Color(80, 220, 210), 2f);
        }

        batch.End();

        DrawHud(batch, pixel, viewport);
    }

    private void DrawHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        Rectangle healthBack = new(28, 28, 264, 20);
        batch.FillRectangle(pixel, healthBack, new Color(7, 6, 12) * 0.9f);
        batch.FillRectangle(pixel, new Rectangle(32, 32, (int)(256f * _player.Health / GameBalance.PlayerMaxHealth), 12), new Color(194, 203, 216));
        batch.DrawRectangle(pixel, healthBack, new Color(87, 74, 106), 2f);

        float dashReady = 1f - MathHelper.Clamp(_player.DashCooldownRemaining / GameBalance.DashCooldown, 0f, 1f);
        batch.FillRectangle(pixel, new Rectangle(32, 53, (int)(96f * dashReady), 4), GameBalance.DeathFlame * 0.9f);
        batch.DrawRectangle(pixel, new Rectangle(30, 51, 100, 8), new Color(87, 74, 106), 1f);

        Rectangle resonanceBack = new(28, 82, 184, 12);
        batch.FillRectangle(pixel, resonanceBack, new Color(7, 6, 12) * 0.9f);
        float resonanceFill = _player.ResonanceActive
            ? _player.ResonanceRemaining / GameBalance.ResonanceDuration
            : _player.Resonance / GameBalance.ResonanceRequired;
        Color resonanceColor = _player.ResonanceActive || _player.IsResonanceReady
            ? GameBalance.SoulWhite
            : GameBalance.DeathFlame;
        batch.FillRectangle(pixel, new Rectangle(31, 85, (int)(178f * resonanceFill), 6), resonanceColor);
        batch.DrawRectangle(pixel, resonanceBack, _player.IsResonanceReady ? GameBalance.SoulWhite : new Color(87, 74, 106), _player.IsResonanceReady ? 3f : 1f);
        if (_player.IsResonanceReady)
        {
            batch.FillCircle(pixel, new Vector2(222f, 88f), 5f, GameBalance.SoulWhite);
            batch.DrawCircle(pixel, new Vector2(222f, 88f), 10f, GameBalance.DeathFlameBright * 0.8f, 2f, 16);
        }

        for (int i = 0; i < 4; i++)
        {
            Color waveColor = i < _waveNumber
                ? i == _waveNumber - 1 && _loopState == ArenaLoopState.Combat ? GameBalance.SoulWhite : GameBalance.DeathFlame
                : new Color(45, 40, 53);
            batch.FillRectangle(pixel, new Rectangle(viewport.Width / 2 - 38 + i * 22, 28, 14, 6), waveColor);
        }

        if (_loopState is ArenaLoopState.Intro or ArenaLoopState.Transition)
        {
            batch.DrawRectangle(pixel, new Rectangle(viewport.Width / 2 - 58, 48, 116, 8), GameBalance.DeathFlameBright * 0.72f, 2f);
        }
        else if (_loopState == ArenaLoopState.Complete)
        {
            batch.DrawLine(pixel, new Vector2(viewport.Width / 2 - 58f, 51f), new Vector2(viewport.Width / 2 + 58f, 51f), GameBalance.SoulWhite, 5f);
        }

        // Minimal controller legend without requiring a font asset.
        batch.FillRectangle(pixel, new Rectangle(28, viewport.Height - 26, 186, 4), GameBalance.DeepViolet * 0.65f);

        for (int i = 0; i < 3; i++)
        {
            Color comboColor = _player.Scythe.ActiveStep == i + 1
                ? (i == 2 ? GameBalance.SoulWhite : GameBalance.DeathFlameBright)
                : new Color(55, 48, 66);
            int size = i == 2 ? 14 : 10;
            batch.FillRectangle(pixel, new Rectangle(32 + i * 20, 68 - (size - 10) / 2, size, size), comboColor);
        }

        if (_screenEffects.FlashAlpha > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), GameBalance.DeathFlameBright * _screenEffects.FlashAlpha);
        }

        if (_screenEffects.ImpactFrameAlpha > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (_screenEffects.ImpactFrameAlpha * 0.82f));
        }

        if (_player.ResonanceActivationRemaining > 0f)
        {
            float activationFade = MathHelper.Clamp(_player.ResonanceActivationRemaining / 0.5f, 0f, 1f);
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (activationFade * 0.38f));
        }

        if (_player.SoulSenseActive)
        {
            Color vignette = new Color(2, 3, 7) * 0.72f;
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, 54), vignette);
            batch.FillRectangle(pixel, new Rectangle(0, viewport.Height - 54, viewport.Width, 54), vignette);
            batch.FillRectangle(pixel, new Rectangle(0, 54, 54, viewport.Height - 108), vignette);
            batch.FillRectangle(pixel, new Rectangle(viewport.Width - 54, 54, 54, viewport.Height - 108), vignette);
        }

        for (int i = 0; i < 3; i++)
        {
            Color stageColor = _player.Cannon.ChargeStage > i
                ? i == 2 && _player.Cannon.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright
                : new Color(45, 40, 53);
            int width = i == 2 ? 20 : 14;
            batch.FillRectangle(pixel, new Rectangle(viewport.Width - 86 + i * 22, 32, width, 7), stageColor);
        }

        batch.End();
    }

    private void ResolveScytheStrike()
    {
        if (!_player.Scythe.TryConsumeStrike(out ScytheStrike strike))
        {
            return;
        }

        bool hitAnything = false;
        foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
        {
            Vector2 toTarget = enemy.Position - _player.Position;
            float combinedRange = strike.Range + enemy.Radius;
            if (toTarget.LengthSquared() > combinedRange * combinedRange)
            {
                continue;
            }

            Vector2 targetDirection = toTarget.LengthSquared() > 0.001f ? Vector2.Normalize(toTarget) : strike.Direction;
            if (Vector2.Dot(strike.Direction, targetDirection) < MathF.Cos(strike.ArcRadians * 0.5f))
            {
                continue;
            }

            Vector2 weakPoint = FindStrikeWeakPoint(enemy, strike);
            bool coreHit = _player.SoulSenseActive && weakPoint != Vector2.Zero;
            int damage = coreHit
                ? (int)MathF.Round(strike.Damage * GameBalance.SoulSenseCoreDamageMultiplier)
                : strike.Damage;
            enemy.ApplyDamage(new DamageInfo(
                damage,
                targetDirection * strike.Knockback,
                coreHit ? weakPoint : enemy.Position,
                coreHit));
            Color impactColor = coreHit || strike.Step == 3 ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
            int particleCount = coreHit ? 20 : strike.Step == 3 ? 24 : 10;
            _particles.EmitBurst(coreHit ? weakPoint : enemy.Position - targetDirection * enemy.Radius * 0.35f, targetDirection, particleCount, impactColor, strike.Step == 3 ? 260f : 150f, coreHit ? 8f : strike.Step == 3 ? 9f : 5f);
            if (coreHit)
            {
                _player.AddResonance(GameBalance.ResonancePerCoreHit);
                _screenEffects.Flash(0.075f, 0.2f);
            }
            hitAnything = true;
        }

        if (!hitAnything)
        {
            return;
        }

        _screenEffects.BeginHitstop(strike.Hitstop);
        if (strike.Step == 3)
        {
            _screenEffects.AddShake(0.18f, 8f);
            _screenEffects.Flash(0.09f, 0.24f);
        }
        else
        {
            _screenEffects.AddShake(0.07f, strike.Step == 2 ? 2.5f : 1.6f);
            _screenEffects.Flash(0.045f, 0.08f);
        }
    }

    private void SpawnWave(int waveNumber)
    {
        Vector2 center = _arena.CombatBounds.Center.ToVector2();
        int seed = waveNumber * 10;
        switch (waveNumber)
        {
            case 1:
                _enemies.Add(new Hollow(center + new Vector2(360f, -195f), seed + 1));
                _enemies.Add(new Hollow(center + new Vector2(-390f, -125f), seed + 2));
                _enemies.Add(new Hollow(center + new Vector2(235f, 265f), seed + 3));
                break;

            case 2:
                _enemies.Add(new Hollow(center + new Vector2(-470f, -210f), seed + 1));
                _enemies.Add(new Hollow(center + new Vector2(430f, 235f), seed + 2));
                _enemies.Add(new Burning(center + new Vector2(445f, -170f), seed + 3));
                _enemies.Add(new Burning(center + new Vector2(-420f, 225f), seed + 4));
                break;

            case 3:
                _enemies.Add(new Hollow(center + new Vector2(-500f, -230f), seed + 1));
                _enemies.Add(new Hollow(center + new Vector2(480f, 235f), seed + 2));
                _enemies.Add(new Burning(center + new Vector2(420f, -250f), seed + 3));
                _enemies.Add(new Burning(center + new Vector2(-420f, 260f), seed + 4));
                _enemies.Add(new Devourer(center + new Vector2(560f, 10f)));
                break;

            case 4:
                _enemies.Add(new Devourer(center + new Vector2(575f, -35f)));
                _enemies.Add(new Burning(center + new Vector2(-500f, -265f), seed + 1));
                _enemies.Add(new Burning(center + new Vector2(-525f, 40f), seed + 2));
                _enemies.Add(new Burning(center + new Vector2(390f, 275f), seed + 3));
                _enemies.Add(new Hollow(center + new Vector2(455f, -245f), seed + 4));
                _enemies.Add(new Hollow(center + new Vector2(-320f, 285f), seed + 5));
                break;
        }

        _waveNumber = waveNumber;
        _loopState = ArenaLoopState.Combat;
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _particles.EmitDeathFlame(center, 18 + waveNumber * 5, 1f + waveNumber * 0.12f);
        _screenEffects.AddShake(0.16f, 4f + waveNumber);
        _screenEffects.Flash(0.08f, 0.12f + waveNumber * 0.035f);
    }

    private void ResetEncounter()
    {
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _enemies.Clear();
        _souls.Clear();
        _cannonShots.Clear();
        _waveNumber = 0;
        _loopState = ArenaLoopState.Intro;
        _loopStateTimer = 1.05f;
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
    }

    private void ConfigureBurningAggression(float deltaTime)
    {
        _burningHandoffTimer = MathF.Max(0f, _burningHandoffTimer - deltaTime);
        List<Burning> burnings = _enemies
            .OfType<Burning>()
            .Where(burning => burning.IsAlive)
            .ToList();

        foreach (Burning burning in burnings)
        {
            burning.SetAggressionSlot(false);
        }

        int maximumCommitments = _waveNumber >= 4 ? 2 : 1;
        int committed = burnings.Count(burning => burning.IsAggressionCommitted);
        if (_burningHandoffTimer > 0f || committed >= maximumCommitments)
        {
            return;
        }

        foreach (Burning burning in burnings
            .Where(burning => burning.State == BurningState.Approach)
            .OrderBy(burning => Vector2.DistanceSquared(burning.Position, _player.Position))
            .Take(maximumCommitments - committed))
        {
            burning.SetAggressionSlot(true);
        }
    }

    private void UpdateBurningHandoff()
    {
        int committed = _enemies
            .OfType<Burning>()
            .Count(burning => burning.IsAlive && burning.IsAggressionCommitted);
        if (committed < _burningCommittedLastFrame)
        {
            _burningHandoffTimer = GameBalance.BurningAggressionHandoffDelay;
        }

        _burningCommittedLastFrame = committed;
    }

    private void UpdateArenaLoop(float deltaTime)
    {
        switch (_loopState)
        {
            case ArenaLoopState.Intro:
            case ArenaLoopState.Transition:
                _loopStateTimer = MathF.Max(0f, _loopStateTimer - deltaTime);
                if (_loopStateTimer <= 0f)
                {
                    SpawnWave(_waveNumber + 1);
                }
                break;

            case ArenaLoopState.Combat:
                if (_enemies.Count == 0 && _souls.Count == 0)
                {
                    if (_waveNumber >= 4)
                    {
                        _loopState = ArenaLoopState.Complete;
                        _screenEffects.Flash(0.16f, 0.3f);
                    }
                    else
                    {
                        _loopState = ArenaLoopState.Transition;
                        _loopStateTimer = 1.35f;
                        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 12, 0.8f);
                    }
                }
                break;
        }
    }

    private void DrawArenaLoop(SpriteBatch batch, Texture2D pixel)
    {
        if (_loopState != ArenaLoopState.Complete)
        {
            Rectangle gate = new(_arena.CombatBounds.Center.X - 92, _arena.CombatBounds.Bottom - 14, 184, 20);
            batch.FillRectangle(pixel, gate, new Color(24, 22, 30));
            batch.DrawRectangle(pixel, gate, GameBalance.MetalColor, 5f);
            for (int x = gate.Left + 18; x < gate.Right; x += 24)
            {
                batch.DrawLine(pixel, new Vector2(x, gate.Top - 17), new Vector2(x, gate.Bottom + 17), GameBalance.StoneColor, 7f);
            }
        }

        if (_loopState is ArenaLoopState.Intro or ArenaLoopState.Transition)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_loopStateTimer * 12f);
            batch.DrawCircle(pixel, _arena.CombatBounds.Center.ToVector2(), 118f + pulse * 14f, GameBalance.DeathFlame * (0.18f + pulse * 0.18f), 5f, 40);
        }
    }

    private string GetPlayerState()
    {
        if (_player.IsDead) return "DEAD";
        if (_player.ResonanceActive) return "RESONANCE";
        if (_player.IsDashing) return "DASH";
        if (_player.Cannon.IsHandling) return "CANNON";
        if (_player.Scythe.ActiveStep > 0) return $"SCYTHE {_player.Scythe.ActiveStep}";
        if (_player.SoulSenseActive) return "SOUL SENSE";
        return "NORMAL";
    }

    private bool IsPointInsideStrike(Vector2 point, ScytheStrike strike)
    {
        Vector2 toPoint = point - _player.Position;
        if (toPoint.LengthSquared() > MathF.Pow(strike.Range + GameBalance.HollowCoreRadius, 2f))
        {
            return false;
        }

        Vector2 direction = toPoint.LengthSquared() > 0.001f ? Vector2.Normalize(toPoint) : strike.Direction;
        return Vector2.Dot(strike.Direction, direction) >= MathF.Cos(strike.ArcRadians * 0.5f);
    }

    private Vector2 FindStrikeWeakPoint(Enemy enemy, ScytheStrike strike)
    {
        if (!_player.SoulSenseActive)
        {
            return Vector2.Zero;
        }

        if (enemy is Hollow hollow && IsPointInsideStrike(hollow.CorePosition, strike))
        {
            return hollow.CorePosition;
        }

        if (enemy is Burning burning)
        {
            foreach (Vector2 fracture in burning.GetFracturePositions())
            {
                if (IsPointInsideStrike(fracture, strike))
                {
                    return fracture;
                }
            }
        }


        if (enemy is Devourer devourer && IsPointInsideStrike(devourer.TorsoPosition, strike))
        {
            return devourer.TorsoPosition;
        }

        return Vector2.Zero;
    }

    private static void DrawSoulTraces(SpriteBatch batch, Texture2D pixel)
    {
        Color trace = GameBalance.DeathFlame * 0.24f;
        batch.DrawLine(pixel, new Vector2(235f, 742f), new Vector2(490f, 665f), trace, 2f);
        batch.DrawLine(pixel, new Vector2(490f, 665f), new Vector2(805f, 706f), trace, 2f);
        batch.DrawLine(pixel, new Vector2(1090f, 260f), new Vector2(1345f, 355f), trace, 2f);
        batch.DrawLine(pixel, new Vector2(1345f, 355f), new Vector2(1510f, 320f), trace, 2f);
    }

    private string GetScreenshotContext()
    {
        if (_player.IsDead) return "phase05_player_down";
        if (_loopState == ArenaLoopState.Complete) return "phase12_arena_complete";
        if (_loopState == ArenaLoopState.Transition) return $"phase12_wave_{_waveNumber}_clear";
        if (_loopState == ArenaLoopState.Intro) return "phase12_arena_intro";
        if (_player.ResonanceActive) return "phase11_resonance_active";
        if (_player.IsResonanceReady) return "phase11_resonance_ready";
        if (_cannonShots.Any(shot => shot.IsFullCharge && !shot.IsFinished)) return "phase08_full_cannon_shot";
        if (_player.Cannon.IsFullCharge) return "phase08_cannon_full_charge";
        if (_player.Cannon.ChargeStage == 3) return "phase08_cannon_charge_stage_3";
        if (_player.Cannon.ChargeStage == 2) return "phase08_cannon_charge_stage_2";
        if (_player.Cannon.ChargeStage == 1) return "phase08_cannon_charge_stage_1";
        if (_enemies.OfType<Burning>().Any(burning => burning.State == BurningState.Detonating)) return "phase09_burning_detonation";
        if (_enemies.OfType<Burning>().Any(burning => burning.State == BurningState.Charge)) return "phase09_burning_charge";
        if (_player.SoulSenseActive && _enemies.OfType<Burning>().Any(burning => burning.IsAlive)) return "phase09_burning_fractures";
        if (_player.SoulSenseActive && _enemies.OfType<Devourer>().Any(devourer => devourer.ConsumedSoulCount > 0)) return "phase10_devourer_trapped_souls";
        if (_enemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.Devour)) return "phase10_devourer_devouring";
        if (_enemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.ApproachSoul)) return "phase10_devourer_soul_target";
        if (_player.SoulSenseActive && _enemies.Any(enemy => enemy.IsAlive)) return "phase07_soul_sense_hollow_cores";
        if (_player.SoulSenseActive) return "phase07_soul_sense_arena";
        if (_souls.Any(soul => soul.State == SoulState.Releasing)) return "phase06_soul_release";
        if (_souls.Any(soul => soul.State == SoulState.Residue)) return "phase06_residue_to_player";
        if (_souls.Any(soul => soul.State == SoulState.Exposed)) return "phase06_exposed_soul";
        if (_enemies.OfType<Hollow>().Any(hollow => hollow.State == HollowState.Telegraph)) return "phase05_hollow_swipe_telegraph";
        if (_enemies.OfType<Hollow>().Any(hollow => hollow.State == HollowState.Dying)) return "phase05_hollow_death";
        if (_player.Scythe.ActiveStep > 0) return $"phase05_scythe_hit_{_player.Scythe.ActiveStep}";
        return _debugVisible ? $"phase12_wave_{_waveNumber}_debug" : $"phase12_wave_{_waveNumber}_combat";
    }

    private void SpawnCannonShot()
    {
        if (!_player.Cannon.TryConsumeShot(out CannonShotRequest request))
        {
            return;
        }

        Vector2 origin = _player.Position + request.Direction * 74f;
        _cannonShots.Add(new CannonShot(origin, request));
        _player.ApplyCannonRecoil(request.Direction, request.Charge);
        _particles.EmitBurst(origin, request.Direction, request.IsFullCharge ? 30 : 14, request.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright, request.IsFullCharge ? 360f : 190f, request.IsFullCharge ? 11f : 6f);
        _particles.EmitDeathFlame(origin, request.IsFullCharge ? 18 : 8, request.IsFullCharge ? 1.55f : 0.9f);
        _screenEffects.AddShake(request.IsFullCharge ? 0.32f : 0.14f, request.IsFullCharge ? 12f : 5f);
        _screenEffects.Flash(request.IsFullCharge ? 0.1f : 0.055f, request.IsFullCharge ? 0.34f : 0.13f);
        if (request.IsFullCharge)
        {
            _screenEffects.BeginHitstop(0.105f);
            _screenEffects.BeginImpactFrame(0.05f);
        }
    }

    private void UpdateCannonShots(float deltaTime)
    {
        foreach (CannonShot shot in _cannonShots)
        {
            shot.Update(deltaTime, _arena.Bounds);
            if (shot.IsFinished)
            {
                continue;
            }

            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                float bodyRadius = enemy.Radius + shot.Radius;
                if (DistanceSquaredToSegment(enemy.Position, shot.PreviousPosition, shot.Position) > bodyRadius * bodyRadius)
                {
                    continue;
                }

                if (enemy is Burning chargingBurning && chargingBurning.IsCharging)
                {
                    chargingBurning.Detonate();
                    shot.MarkHit();
                    break;
                }

                Vector2 weakPoint = FindCannonWeakPoint(enemy, shot);
                bool coreHit = weakPoint != Vector2.Zero;
                int damage = coreHit
                    ? (int)MathF.Round(shot.Damage * GameBalance.CannonCoreDamageMultiplier)
                    : shot.Damage;
                float knockback = MathHelper.Lerp(330f, 760f, shot.Charge);
                enemy.ApplyDamage(new DamageInfo(
                    damage,
                    shot.Direction * knockback,
                    coreHit ? weakPoint : enemy.Position,
                    coreHit,
                    shot.IsFullCharge));

                Color impact = coreHit || shot.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
                _particles.EmitBurst(coreHit ? weakPoint : enemy.Position, shot.Direction, shot.IsFullCharge ? 34 : 18, impact, shot.IsFullCharge ? 390f : 220f, shot.IsFullCharge ? 12f : 7f);
                _particles.EmitDeathFlame(enemy.Position, shot.IsFullCharge ? 15 : 7, shot.IsFullCharge ? 1.4f : 0.85f);
                _screenEffects.BeginHitstop(shot.IsFullCharge ? 0.12f : 0.065f);
                _screenEffects.AddShake(shot.IsFullCharge ? 0.24f : 0.12f, shot.IsFullCharge ? 11f : 5f);
                _screenEffects.Flash(shot.IsFullCharge ? 0.1f : 0.06f, shot.IsFullCharge ? 0.34f : 0.16f);
                if (shot.IsFullCharge)
                {
                    _screenEffects.BeginImpactFrame(0.055f);
                }
                if (coreHit)
                {
                    _player.AddResonance(GameBalance.ResonancePerCoreHit * (shot.IsFullCharge ? 2f : 1f));
                }

                shot.MarkHit();
                break;
            }
        }

        _cannonShots.RemoveAll(shot => shot.IsFinished);
    }

    private Vector2 FindCannonWeakPoint(Enemy enemy, CannonShot shot)
    {
        if (!shot.SoulSenseAtFire)
        {
            return Vector2.Zero;
        }

        if (enemy is Hollow hollow)
        {
            float coreRadius = GameBalance.HollowCoreRadius + shot.Radius;
            if (DistanceSquaredToSegment(hollow.CorePosition, shot.PreviousPosition, shot.Position) <= coreRadius * coreRadius)
            {
                return hollow.CorePosition;
            }
        }

        if (enemy is Burning burning)
        {
            foreach (Vector2 fracture in burning.GetFracturePositions())
            {
                float fractureRadius = GameBalance.BurningFractureRadius + shot.Radius;
                if (DistanceSquaredToSegment(fracture, shot.PreviousPosition, shot.Position) <= fractureRadius * fractureRadius)
                {
                    return fracture;
                }
            }
        }


        if (enemy is Devourer devourer)
        {
            float torsoRadius = GameBalance.DevourerTorsoRadius + shot.Radius;
            if (DistanceSquaredToSegment(devourer.TorsoPosition, shot.PreviousPosition, shot.Position) <= torsoRadius * torsoRadius)
            {
                return devourer.TorsoPosition;
            }
        }

        return Vector2.Zero;
    }

    private void ResolveBurningDetonation(Burning source, Vector2 position)
    {
        _particles.EmitBurst(position, Vector2.UnitX, 54, GameBalance.DeathFlameBright, 430f, 13f);
        _particles.EmitDeathFlame(position, 32, 1.65f);
        _screenEffects.BeginHitstop(0.1f);
        _screenEffects.BeginImpactFrame(0.065f);
        _screenEffects.AddShake(0.3f, 13f);
        _screenEffects.Flash(0.12f, 0.42f);

        foreach (Enemy enemy in _enemies.Where(enemy => enemy != source && enemy.IsAlive))
        {
            Vector2 away = enemy.Position - position;
            float combinedRadius = GameBalance.BurningDetonationRadius + enemy.Radius;
            if (away.LengthSquared() > combinedRadius * combinedRadius)
            {
                continue;
            }

            Vector2 direction = away.LengthSquared() > 0.001f ? Vector2.Normalize(away) : Vector2.UnitX;
            enemy.ApplyDamage(new DamageInfo(
                GameBalance.BurningDetonationDamage,
                direction * GameBalance.BurningDetonationKnockback,
                enemy.Position));
            _particles.EmitBurst(enemy.Position, direction, 18, GameBalance.DeathFlame, 260f, 8f);
        }
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared <= 0.001f)
        {
            return Vector2.DistanceSquared(point, start);
        }

        float amount = MathHelper.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.DistanceSquared(point, start + segment * amount);
    }
}
