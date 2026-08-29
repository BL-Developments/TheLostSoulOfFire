using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

public enum ArenaLoopState
{
    Title,
    Intro,
    Combat,
    Transition,
    Complete
}

public sealed class GameWorld : IDisposable
{
    private readonly Arena _arena = new();
    private readonly AudioDirector _audio;
    private readonly Camera2D _camera;
    private readonly ScreenEffects _screenEffects = new();
    private readonly ParticleSystem _particles = new();
    private readonly HudRenderer _hud = new();
    private readonly ArtAssets _art;
    private readonly SpriteVfxSystem _spriteVfx;
    private readonly Player _player;
    private readonly List<Enemy> _enemies = [];
    private readonly List<Soul> _souls = [];
    private readonly List<CannonShot> _cannonShots = [];
    private Vector2 _lastMouseWorld;
    private bool _debugVisible;
    private bool _forceSoulSense;
    private int _waveNumber;
    private ArenaLoopState _loopState = ArenaLoopState.Title;
    private float _loopStateTimer = 1.25f;
    private float _burningHandoffTimer;
    private int _burningCommittedLastFrame;
    private float _endingTimer;
    private float _deathTimer;
    private float _presentationTime;
    private float _fpsTimer;
    private int _fpsFrames;
    private int _fps = 60;
    private bool _audioTestFatalDamageRequested;

    public string ScreenshotContext => GetScreenshotContext();
    public ArenaLoopState LoopState => _loopState;
    public int WaveNumber => _waveNumber;
    public bool PlayerDead => _player.IsDead;

    public string WindowTitle => _debugVisible
        ? $"The Lost Soul of Fire — DEBUG | Wave {_waveNumber}/4 {_loopState.ToString().ToUpperInvariant()} | HP {_player.Health} | RES {(_player.ResonanceActive ? $"ACTIVE {_player.ResonanceRemaining:0.0}s" : $"{_player.Resonance:0}/{GameBalance.ResonanceRequired:0}")} | Player {GetPlayerState()} | Enemies {_enemies.Count(enemy => enemy.IsAlive)} | Souls {_souls.Count}"
        : _player.IsDead
            ? "The Lost Soul of Fire — Flame extinguished | R retry | F9 screenshot"
            : _loopState == ArenaLoopState.Title
                ? "The Lost Soul of Fire — Press any key"
                : _loopState == ArenaLoopState.Complete
                    ? "The Lost Soul of Fire — Arena cleared | R restart | F9 screenshot"
                    : $"The Lost Soul of Fire — Wave {_waveNumber}/4 | WASD move | Mouse aim | Space dash | LMB Scythe | Q Soul Sense | RMB Cannon";

    public GameWorld(Viewport viewport, ArtAssets art, ContentManager content)
    {
        _art = art;
        _audio = new AudioDirector(content);
        _spriteVfx = new SpriteVfxSystem(art);
        _camera = new Camera2D(_arena.CombatBounds.Center.ToVector2());
        _player = new Player(_arena.CombatBounds.Center.ToVector2());
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Follow(_arena.CombatBounds.Center.ToVector2(), _arena.Bounds, viewport);
    }

    public void Update(GameTime gameTime, InputState input, Viewport viewport)
    {
        float deltaTime = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 20f);
        _presentationTime += deltaTime;
        _audio.Update(deltaTime);
        bool wasDashing = _player.IsDashing;
        bool wasResonanceActive = _player.ResonanceActive;
        bool wasResonanceReady = _player.IsResonanceReady;
        bool wasSoulSenseActive = _player.SoulSenseActive;
        bool wasCannonFull = _player.Cannon.IsFullCharge;
        SoulCannonState previousCannonState = _player.Cannon.State;
        int previousHealth = _player.Health;
        _art.Update(deltaTime);
        _spriteVfx.Update(deltaTime);
        UpdateFps(deltaTime);
        _screenEffects.Update(deltaTime);
        _arena.Update(deltaTime, _loopState == ArenaLoopState.Complete);

        if (_loopState == ArenaLoopState.Title)
        {
            if (input.AnyInputPressed)
            {
                _audio.Play(AudioCue.TitleConfirm, 0.58f);
                _loopState = ArenaLoopState.Intro;
                _loopStateTimer = 1.05f;
            }
            _particles.Update(deltaTime);
            return;
        }

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

        if (input.WasKeyPressed(Keys.F6))
        {
            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                ApplyEnemyDamage(enemy, new DamageInfo(enemy.Health + enemy.MaxHealth, Vector2.Zero, enemy.Position));
            }
        }

        if (input.WasKeyPressed(Keys.F7))
        {
            _forceSoulSense = !_forceSoulSense;
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

        if (_player.IsDead)
        {
            _deathTimer = MathF.Min(6f, _deathTimer + deltaTime);
            _particles.Update(deltaTime);
            return;
        }

        if (_loopState == ArenaLoopState.Complete)
        {
            UpdateArenaLoop(deltaTime);
            _particles.Update(deltaTime);
            float endingCameraSmoothing = 1f - MathF.Exp(-deltaTime * 4f);
            _camera.Follow(_player.Position, _arena.Bounds, viewport, endingCameraSmoothing);
            return;
        }

        _lastMouseWorld = _camera.ScreenToWorld(input.MousePosition, viewport);

        if (_screenEffects.IsHitStopped)
        {
            return;
        }

        _player.Update(deltaTime, input, _lastMouseWorld, _arena.CombatBounds, _particles, _screenEffects, _forceSoulSense);
        if (_audioTestFatalDamageRequested)
        {
            _audioTestFatalDamageRequested = false;
            _player.ApplyDamage(GameBalance.PlayerMaxHealth, Vector2.Zero, _screenEffects);
        }
        if (_player.Scythe.StartedThisFrame)
        {
            string effect = _player.Scythe.ActiveStep switch
            {
                2 => "scythe_slash_02",
                3 => "scythe_cleave",
                _ => "scythe_slash_01"
            };
            float scale = _player.Scythe.ActiveStep == 3 ? 0.86f : 0.68f;
            _spriteVfx.Spawn(
                effect,
                _player.Position + _player.FacingDirection * 22f,
                MathF.Atan2(_player.FacingDirection.Y, _player.FacingDirection.X),
                scale);
        }
        if (!wasDashing && _player.IsDashing)
        {
            _spriteVfx.Spawn(
                "dash_ignition",
                _player.Position - _player.DashDirection * 24f,
                MathF.Atan2(_player.DashDirection.Y, _player.DashDirection.X),
                0.72f);
        }
        if (!wasResonanceActive && _player.ResonanceActive)
        {
            _spriteVfx.Spawn("resonance_activate", _player.Position, 0f, 0.76f);
        }
        PlayPlayerActionAudio(wasDashing, wasResonanceActive, wasSoulSenseActive, wasCannonFull, previousCannonState);
        SpawnCannonShot();
        ResolveScytheStrike();
        UpdateCannonShots(deltaTime);
        UpdateBurningHandoff();
        ConfigureBurningAggression(deltaTime);
        foreach (Enemy enemy in _enemies)
        {
            HollowState? previousHollowState = enemy is Hollow hollowBefore ? hollowBefore.State : null;
            BurningState? previousBurningState = enemy is Burning burningBefore ? burningBefore.State : null;
            DevourerState? previousDevourerState = enemy is Devourer devourerBefore ? devourerBefore.State : null;
            enemy.Update(deltaTime, _player, _souls, _arena.CombatBounds, _particles, _screenEffects);
            if (enemy is Hollow hollowAfter && previousHollowState != HollowState.Swipe && hollowAfter.State == HollowState.Swipe)
            {
                _audio.Play(AudioCue.HollowSwipe, 0.48f);
            }
            if (enemy is Burning burningAfter && previousBurningState != BurningState.Charge && burningAfter.State == BurningState.Charge)
            {
                _audio.Play(AudioCue.BurningCharge, 0.72f);
            }
            if (enemy is Devourer devourerAfter)
            {
                if (previousDevourerState != DevourerState.Slam && devourerAfter.State == DevourerState.Slam)
                {
                    _audio.Play(AudioCue.DevourerSlam, 0.76f);
                }
                if (previousDevourerState != DevourerState.Devour && devourerAfter.State == DevourerState.Devour)
                {
                    _audio.Play(AudioCue.DevourerDevour, 0.6f);
                }
            }
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
                _spriteVfx.Spawn("soul_release", extractionPosition, 0f, 0.72f);
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
            SoulState previousSoulState = soul.State;
            soul.Update(deltaTime, _player, _particles);
            if (previousSoulState != SoulState.Releasing && soul.State == SoulState.Releasing)
            {
                _spriteVfx.Spawn("soul_release", soul.Position, 0f, 0.62f);
                _audio.Play(AudioCue.SoulRelease, 0.62f);
            }
        }

        _souls.RemoveAll(soul => soul.IsFinished);
        UpdateArenaLoop(deltaTime);
        if (previousHealth > _player.Health)
        {
            if (_player.IsDead)
            {
                _audio.SetSoulSense(false);
            }
            _audio.Play(_player.IsDead ? AudioCue.PlayerDeath : AudioCue.PlayerHit, _player.IsDead ? 0.78f : 0.6f);
        }
        if (!wasResonanceReady && _player.IsResonanceReady)
        {
            _audio.Play(AudioCue.ResonanceReady, 0.72f);
        }
        _particles.Update(deltaTime);

        float cameraSmoothing = 1f - MathF.Exp(-deltaTime * 9f);
        _camera.Follow(_player.Position, _arena.Bounds, viewport, cameraSmoothing);
    }

    public void Dispose()
    {
        _audio.Dispose();
        GC.SuppressFinalize(this);
    }

    internal void RequestAudioTestFatalDamage() => _audioTestFatalDamageRequested = true;

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, SoulfireRenderer renderer)
    {
        renderer.BeginScene(viewport);
        DrawScene(batch, pixel, viewport);
        renderer.PresentScene(batch, viewport);
        DrawSoulfireLighting(batch, renderer, viewport);
        renderer.DrawVignette(batch, viewport, _player.SoulSenseActive, _player.ResonanceActive);
        DrawScreenFeedback(batch, pixel, viewport);
        DrawHud(batch, pixel, viewport);
    }

    private void DrawScene(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            transformMatrix: _camera.GetTransform(viewport, _screenEffects.ShakeOffset));

        _art.DrawArena(batch);
        DrawArenaLoop(batch, pixel);
        if (_player.SoulSenseActive)
        {
            batch.FillRectangle(pixel, _arena.Bounds, new Color(4, 7, 11) * 0.48f);
            DrawSoulTraces(batch, pixel);
        }
        _player.DrawAfterimages(batch, pixel);
        foreach (Enemy enemy in _enemies)
        {
            _art.DrawEnemy(batch, enemy);
            enemy.Draw(batch, pixel, _debugVisible, _player.SoulSenseActive, true);
        }
        foreach (Soul soul in _souls)
        {
            _art.DrawLostSoul(batch, soul);
            soul.Draw(batch, pixel, _player, _player.SoulSenseActive, true);
        }
        foreach (CannonShot shot in _cannonShots)
        {
            shot.Draw(batch, pixel, true);
            _art.DrawCannonProjectile(batch, shot);
        }
        _particles.Draw(batch, pixel);
        batch.FillCircle(pixel, _player.Position + new Vector2(3f, 8f), 24f, new Color(3, 3, 7) * 0.55f);
        _art.DrawPlayer(batch, _player);
        _player.Draw(batch, pixel, _art, _debugVisible);
        if (_player.Cannon.State == SoulCannonState.Charging)
        {
            Vector2 muzzle = _player.Position + _player.FacingDirection * 74f;
            _art.DrawLoopingEffect(
                batch,
                _player.Cannon,
                "cannon_charge_loop",
                muzzle,
                0f,
                0.34f + _player.Cannon.ChargeProgress * 0.46f,
                Color.White);
        }
        if (_player.IsDead)
        {
            _art.DrawDeathFlame(batch, _player, _player.Position);
        }
        _spriteVfx.Draw(batch);

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
    }

    private void DrawSoulfireLighting(SpriteBatch batch, SoulfireRenderer renderer, Viewport viewport)
    {
        SoulfireLighting.Draw(
            batch,
            renderer,
            _camera.GetTransform(viewport, _screenEffects.ShakeOffset),
            _player,
            _enemies,
            _souls,
            _cannonShots,
            _particles,
            _arena.CombatBounds,
            _presentationTime,
            _loopState == ArenaLoopState.Complete,
            _endingTimer);
    }

    private void DrawScreenFeedback(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

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

        batch.End();
    }

    private void DrawHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_loopState == ArenaLoopState.Title)
        {
            DrawTitleHud(batch, pixel, viewport);
            batch.End();
            return;
        }
        if (_player.IsDead)
        {
            DrawDeathHud(batch, pixel, viewport);
            batch.End();
            return;
        }
        if (_loopState == ArenaLoopState.Complete)
        {
            DrawEndingHud(batch, pixel, viewport);
            batch.End();
            return;
        }

        _hud.Draw(batch, pixel, viewport, _player);

        if (_debugVisible)
        {
            DrawDebugOverlay(batch, pixel, viewport);
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
            ApplyEnemyDamage(enemy, new DamageInfo(
                damage,
                targetDirection * strike.Knockback,
                coreHit ? weakPoint : enemy.Position,
                coreHit));
            Color impactColor = coreHit || strike.Step == 3 ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
            int particleCount = coreHit ? 20 : strike.Step == 3 ? 24 : 10;
            _particles.EmitBurst(coreHit ? weakPoint : enemy.Position - targetDirection * enemy.Radius * 0.35f, targetDirection, particleCount, impactColor, strike.Step == 3 ? 260f : 150f, coreHit ? 8f : strike.Step == 3 ? 9f : 5f);
            if (coreHit)
            {
                _spriteVfx.Spawn("core_hit", weakPoint, 0f, 0.62f);
                _player.AddResonance(GameBalance.ResonancePerCoreHit);
                _screenEffects.Flash(0.075f, 0.2f);
                _audio.Play(AudioCue.CoreHit, 0.7f);
            }
            hitAnything = true;
        }

        if (!hitAnything)
        {
            return;
        }

        _screenEffects.BeginHitstop(strike.Hitstop);
        _audio.Play(AudioCue.ScytheHit, strike.Step == 3 ? 0.72f : 0.48f, strike.Step == 2 ? 0.08f : 0f);
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
        _audio.SetCalm(false);
        _audio.Play(AudioCue.WaveStart, 0.62f, MathF.Min(0.18f, waveNumber * 0.03f));
    }

    private void ResetEncounter()
    {
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _enemies.Clear();
        _souls.Clear();
        _cannonShots.Clear();
        _spriteVfx.Clear();
        _waveNumber = 0;
        _loopState = ArenaLoopState.Intro;
        _loopStateTimer = 1.05f;
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _endingTimer = 0f;
        _deathTimer = 0f;
        _forceSoulSense = false;
        _audioTestFatalDamageRequested = false;
        _audio.SetCalm(false);
        _audio.SetSoulSense(false);
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
                    _audio.Play(AudioCue.WaveClear, _waveNumber >= 4 ? 0.74f : 0.62f);
                    if (_waveNumber >= 4)
                    {
                        _loopState = ArenaLoopState.Complete;
                        _endingTimer = 0f;
                        _screenEffects.Flash(0.16f, 0.3f);
                        _audio.SetCalm(true);
                    }
                    else
                    {
                        _loopState = ArenaLoopState.Transition;
                        _loopStateTimer = 1.35f;
                        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 12, 0.8f);
                    }
                }
                break;

            case ArenaLoopState.Complete:
                float previousEndingTimer = _endingTimer;
                _endingTimer = MathF.Min(12f, _endingTimer + deltaTime);
                if (previousEndingTimer < 1.35f && _endingTimer >= 1.35f)
                {
                    _audio.Play(AudioCue.EndingReveal, 0.7f);
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
        else if (_loopState == ArenaLoopState.Complete)
        {
            DrawLifeFlame(batch, pixel);
        }
    }

    private void DrawLifeFlame(SpriteBatch batch, Texture2D pixel)
    {
        if (_endingTimer < 1.35f)
        {
            return;
        }

        float appear = MathHelper.Clamp((_endingTimer - 1.35f) / 1.2f, 0f, 1f);
        Vector2 origin = new(_arena.CombatBounds.Right - 246f, _arena.CombatBounds.Top + 154f);
        float breathe = 0.96f + 0.05f * MathF.Sin(_endingTimer * 2.2f);
        _art.DrawLifeFlame(batch, origin, appear, breathe);
    }

    private void DrawEndingHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float quiet = MathHelper.Clamp(_endingTimer / 1.4f, 0f, 1f);
        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (quiet * 0.38f));

        if (_endingTimer >= 2.7f)
        {
            float titleAlpha = MathHelper.Clamp((_endingTimer - 2.7f) / 0.9f, 0f, 1f);
            PixelText.DrawCentered(batch, pixel, "THE LOST SOUL OF FIRE", viewport.Width * 0.5f, viewport.Height * 0.39f, 5, GameBalance.SoulWhite * titleAlpha);
        }
        if (_endingTimer >= 3.7f)
        {
            float completeAlpha = MathHelper.Clamp((_endingTimer - 3.7f) / 0.8f, 0f, 1f);
            PixelText.DrawCentered(batch, pixel, "PROTOTYPE COMPLETE", viewport.Width * 0.5f, viewport.Height * 0.51f, 3, new Color(215, 205, 224) * completeAlpha);
        }
        if (_endingTimer >= 4.7f)
        {
            float promptPulse = 0.48f + 0.22f * MathF.Sin(_endingTimer * 2.4f);
            PixelText.DrawCentered(batch, pixel, "R TO RESTART", viewport.Width * 0.5f, viewport.Height * 0.62f, 2, GameBalance.DeathFlameBright * promptPulse);
        }
    }

    private static void DrawTitleHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.62f);
        PixelText.DrawCentered(batch, pixel, "THE LOST SOUL OF FIRE", viewport.Width * 0.5f, viewport.Height * 0.39f, 5, GameBalance.SoulWhite);
        PixelText.DrawCentered(batch, pixel, "PRESS ANY KEY", viewport.Width * 0.5f, viewport.Height * 0.56f, 2, GameBalance.DeathFlameBright * 0.78f);
    }

    private void DrawDeathHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float fade = MathHelper.Clamp(_deathTimer / 1.1f, 0.2f, 0.82f);
        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * fade);
        if (_deathTimer >= 0.35f)
        {
            float titleAlpha = MathHelper.Clamp((_deathTimer - 0.35f) / 0.55f, 0f, 1f);
            PixelText.DrawCentered(batch, pixel, "FLAME EXTINGUISHED", viewport.Width * 0.5f, viewport.Height * 0.44f, 4, GameBalance.DeathFlameBright * titleAlpha);
        }
        if (_deathTimer >= 1f)
        {
            PixelText.DrawCentered(batch, pixel, "R TO RETRY", viewport.Width * 0.5f, viewport.Height * 0.57f, 2, GameBalance.SoulWhite * 0.72f);
        }
    }

    private void DrawDebugOverlay(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        int x = viewport.Width - 324;
        int y = 24;
        batch.FillRectangle(pixel, new Rectangle(x - 14, y - 12, 308, 216), new Color(5, 5, 9) * 0.9f);
        batch.DrawRectangle(pixel, new Rectangle(x - 14, y - 12, 308, 216), new Color(80, 220, 210) * 0.72f, 2f);

        Color label = new(189, 231, 226);
        PixelText.Draw(batch, pixel, $"FPS: {_fps}", new Vector2(x, y), 2, label);
        PixelText.Draw(batch, pixel, $"HP: {_player.Health}/{GameBalance.PlayerMaxHealth}", new Vector2(x, y + 24), 2, label);
        string resonance = _player.ResonanceActive
            ? $"RESONANCE: {_player.ResonanceRemaining:0.0}"
            : $"RESONANCE: {_player.Resonance:0}/{GameBalance.ResonanceRequired:0}";
        PixelText.Draw(batch, pixel, resonance, new Vector2(x, y + 48), 2, label);
        PixelText.Draw(batch, pixel, $"WAVE: {_waveNumber}/4 {_loopState}", new Vector2(x, y + 72), 2, label);
        PixelText.Draw(batch, pixel, $"ENEMIES: {_enemies.Count(enemy => enemy.IsAlive)}", new Vector2(x, y + 96), 2, label);
        PixelText.Draw(batch, pixel, $"SOULS: {_souls.Count}", new Vector2(x, y + 120), 2, label);
        PixelText.Draw(batch, pixel, $"PLAYER: {GetPlayerState()}", new Vector2(x, y + 144), 2, label);
        PixelText.Draw(batch, pixel, $"SENSE FORCE: {(_forceSoulSense ? "ON" : "OFF")}", new Vector2(x, y + 168), 2, label);
    }

    private void UpdateFps(float deltaTime)
    {
        _fpsFrames++;
        _fpsTimer += deltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fps = (int)MathF.Round(_fpsFrames / _fpsTimer);
            _fpsFrames = 0;
            _fpsTimer = 0f;
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
        if (_loopState == ArenaLoopState.Title) return "phase15_title";
        if (_loopState == ArenaLoopState.Complete) return "phase15_prototype_complete";
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
        _spriteVfx.Spawn(
            "cannon_muzzle_full",
            origin,
            MathF.Atan2(request.Direction.Y, request.Direction.X),
            request.IsFullCharge ? 0.72f : 0.46f,
            request.IsFullCharge ? Color.White : new Color(205, 164, 242));
        _audio.Play(AudioCue.CannonFire, request.IsFullCharge ? 0.9f : 0.58f, request.IsFullCharge ? -0.08f : 0.08f);
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
                ApplyEnemyDamage(enemy, new DamageInfo(
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
                    _spriteVfx.Spawn("core_hit", weakPoint, 0f, shot.IsFullCharge ? 0.78f : 0.58f);
                    _player.AddResonance(GameBalance.ResonancePerCoreHit * (shot.IsFullCharge ? 2f : 1f));
                    _audio.Play(AudioCue.CoreHit, shot.IsFullCharge ? 0.86f : 0.66f);
                }
                else
                {
                    _audio.Play(AudioCue.CannonImpact, shot.IsFullCharge ? 0.72f : 0.48f);
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
        _spriteVfx.Spawn("burning_detonation", position, 0f, 0.88f);
        _particles.EmitBurst(position, Vector2.UnitX, 54, GameBalance.DeathFlameBright, 430f, 13f);
        _particles.EmitDeathFlame(position, 32, 1.65f);
        _screenEffects.BeginHitstop(0.1f);
        _screenEffects.BeginImpactFrame(0.065f);
        _screenEffects.AddShake(0.3f, 13f);
        _screenEffects.Flash(0.12f, 0.42f);
        _audio.Play(AudioCue.BurningDetonation, 0.9f);

        foreach (Enemy enemy in _enemies.Where(enemy => enemy != source && enemy.IsAlive))
        {
            Vector2 away = enemy.Position - position;
            float combinedRadius = GameBalance.BurningDetonationRadius + enemy.Radius;
            if (away.LengthSquared() > combinedRadius * combinedRadius)
            {
                continue;
            }

            Vector2 direction = away.LengthSquared() > 0.001f ? Vector2.Normalize(away) : Vector2.UnitX;
            ApplyEnemyDamage(enemy, new DamageInfo(
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

    private void PlayPlayerActionAudio(
        bool wasDashing,
        bool wasResonanceActive,
        bool wasSoulSenseActive,
        bool wasCannonFull,
        SoulCannonState previousCannonState)
    {
        if (_player.Scythe.StartedThisFrame)
        {
            AudioCue cue = _player.Scythe.ActiveStep switch
            {
                2 => AudioCue.ScytheSwing2,
                3 => AudioCue.SoulCleave,
                _ => AudioCue.ScytheSwing1
            };
            _audio.Play(cue, _player.Scythe.ActiveStep == 3 ? 0.78f : 0.5f);
        }

        if (!wasDashing && _player.IsDashing)
        {
            _audio.Play(AudioCue.Dash, 0.62f);
        }
        if (previousCannonState != SoulCannonState.Charging && _player.Cannon.State == SoulCannonState.Charging)
        {
            _audio.Play(AudioCue.CannonCharge, 0.42f);
        }
        if (!wasCannonFull && _player.Cannon.IsFullCharge)
        {
            _audio.Play(AudioCue.CannonFull, 0.72f);
        }
        if (!wasResonanceActive && _player.ResonanceActive)
        {
            _audio.Play(AudioCue.ResonanceActivate, 0.88f);
        }
        if (!wasSoulSenseActive && _player.SoulSenseActive && !_player.ResonanceActive)
        {
            _audio.Play(AudioCue.SoulSenseOn, 0.38f);
        }
        else if (wasSoulSenseActive && !_player.SoulSenseActive)
        {
            _audio.Play(AudioCue.SoulSenseOff, 0.3f);
        }

        if (wasSoulSenseActive != _player.SoulSenseActive)
        {
            _audio.SetSoulSense(_player.SoulSenseActive);
        }
    }

    private void ApplyEnemyDamage(Enemy enemy, DamageInfo damage)
    {
        bool wasAlive = enemy.IsAlive;
        enemy.ApplyDamage(damage);
        if (wasAlive && !enemy.IsAlive)
        {
            float volume = enemy is Devourer ? 0.72f : 0.52f;
            _audio.Play(AudioCue.EnemyDeath, volume);
        }
    }
}
