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

public sealed class GameWorld
{
    private readonly Arena _arena = new();
    private readonly Camera2D _camera;
    private readonly ScreenEffects _screenEffects = new();
    private readonly ParticleSystem _particles = new();
    private readonly Player _player;
    private readonly List<Hollow> _hollows = [];
    private readonly List<Soul> _souls = [];
    private readonly List<CannonShot> _cannonShots = [];
    private Vector2 _lastMouseWorld;
    private bool _debugVisible;

    public string ScreenshotContext => GetScreenshotContext();

    public string WindowTitle => _debugVisible
        ? $"The Lost Soul of Fire — DEBUG | HP {_player.Health} | RES {_player.Resonance:0}/{GameBalance.ResonanceRequired:0} | Scythe {_player.Scythe.StateLabel} | Cannon {_player.Cannon.StateLabel} | Hollows {_hollows.Count(hollow => hollow.IsAlive)} | Souls {_souls.Count}"
        : _player.IsDead
            ? "The Lost Soul of Fire — Flame extinguished | R retry | F9 screenshot"
            : "The Lost Soul of Fire — WASD move | Mouse aim | Space dash | LMB Scythe | Q Soul Sense | RMB Cannon | F9 screenshot";

    public GameWorld(Viewport viewport)
    {
        _camera = new Camera2D(_arena.CombatBounds.Center.ToVector2());
        _player = new Player(_arena.CombatBounds.Center.ToVector2());
        SpawnHollows();
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

        if (input.WasKeyPressed(Keys.F8))
        {
            ResetEncounter();
        }

        if (_player.IsDead && input.WasKeyPressed(Keys.R))
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
        foreach (Hollow hollow in _hollows)
        {
            hollow.Update(deltaTime, _player, _arena.CombatBounds, _particles, _screenEffects);
            if (hollow.TryConsumeSoulSpawn(out Vector2 soulPosition))
            {
                _souls.Add(new Soul(soulPosition));
            }
        }

        _hollows.RemoveAll(hollow => hollow.IsFinished);
        foreach (Soul soul in _souls)
        {
            soul.Update(deltaTime, _player, _particles);
        }

        _souls.RemoveAll(soul => soul.IsFinished);
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
        if (_player.SoulSenseActive)
        {
            batch.FillRectangle(pixel, _arena.Bounds, new Color(4, 7, 11) * 0.48f);
            DrawSoulTraces(batch, pixel);
        }
        _player.DrawAfterimages(batch, pixel);
        foreach (Hollow hollow in _hollows)
        {
            hollow.Draw(batch, pixel, _debugVisible, _player.SoulSenseActive);
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
        batch.FillRectangle(pixel, new Rectangle(31, 85, (int)(178f * _player.Resonance / GameBalance.ResonanceRequired), 6), GameBalance.DeathFlame);
        batch.DrawRectangle(pixel, resonanceBack, new Color(87, 74, 106), 1f);

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
        foreach (Hollow hollow in _hollows.Where(hollow => hollow.IsAlive))
        {
            Vector2 toTarget = hollow.Position - _player.Position;
            float combinedRange = strike.Range + hollow.Radius;
            if (toTarget.LengthSquared() > combinedRange * combinedRange)
            {
                continue;
            }

            Vector2 targetDirection = toTarget.LengthSquared() > 0.001f ? Vector2.Normalize(toTarget) : strike.Direction;
            if (Vector2.Dot(strike.Direction, targetDirection) < MathF.Cos(strike.ArcRadians * 0.5f))
            {
                continue;
            }

            bool coreHit = _player.SoulSenseActive && IsPointInsideStrike(hollow.CorePosition, strike);
            int damage = coreHit
                ? (int)MathF.Round(strike.Damage * GameBalance.SoulSenseCoreDamageMultiplier)
                : strike.Damage;
            hollow.ApplyDamage(new DamageInfo(
                damage,
                targetDirection * strike.Knockback,
                coreHit ? hollow.CorePosition : hollow.Position,
                coreHit));
            Color impactColor = coreHit || strike.Step == 3 ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
            int particleCount = coreHit ? 20 : strike.Step == 3 ? 24 : 10;
            _particles.EmitBurst(coreHit ? hollow.CorePosition : hollow.Position - targetDirection * hollow.Radius * 0.35f, targetDirection, particleCount, impactColor, strike.Step == 3 ? 260f : 150f, coreHit ? 8f : strike.Step == 3 ? 9f : 5f);
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

    private void SpawnHollows()
    {
        Vector2 center = _arena.CombatBounds.Center.ToVector2();
        _hollows.Add(new Hollow(center + new Vector2(330f, -180f), 1));
        _hollows.Add(new Hollow(center + new Vector2(-360f, -110f), 2));
        _hollows.Add(new Hollow(center + new Vector2(210f, 245f), 3));
    }

    private void ResetEncounter()
    {
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _hollows.Clear();
        _souls.Clear();
        _cannonShots.Clear();
        SpawnHollows();
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
        if (_cannonShots.Any(shot => shot.IsFullCharge && !shot.IsFinished)) return "phase08_full_cannon_shot";
        if (_player.Cannon.IsFullCharge) return "phase08_cannon_full_charge";
        if (_player.Cannon.ChargeStage == 3) return "phase08_cannon_charge_stage_3";
        if (_player.Cannon.ChargeStage == 2) return "phase08_cannon_charge_stage_2";
        if (_player.Cannon.ChargeStage == 1) return "phase08_cannon_charge_stage_1";
        if (_player.SoulSenseActive && _hollows.Any(hollow => hollow.IsAlive)) return "phase07_soul_sense_hollow_cores";
        if (_player.SoulSenseActive) return "phase07_soul_sense_arena";
        if (_souls.Any(soul => soul.State == SoulState.Releasing)) return "phase06_soul_release";
        if (_souls.Any(soul => soul.State == SoulState.Residue)) return "phase06_residue_to_player";
        if (_souls.Any(soul => soul.State == SoulState.Exposed)) return "phase06_exposed_soul";
        if (_hollows.Any(hollow => hollow.State == HollowState.Telegraph)) return "phase05_hollow_swipe_telegraph";
        if (_hollows.Any(hollow => hollow.State == HollowState.Dying)) return "phase05_hollow_death";
        if (_player.Scythe.ActiveStep > 0) return $"phase05_scythe_hit_{_player.Scythe.ActiveStep}";
        return _debugVisible ? "phase05_debug_gameplay" : "phase05_hollow_combat";
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

            foreach (Hollow hollow in _hollows.Where(hollow => hollow.IsAlive))
            {
                float bodyRadius = hollow.Radius + shot.Radius;
                if (DistanceSquaredToSegment(hollow.Position, shot.PreviousPosition, shot.Position) > bodyRadius * bodyRadius)
                {
                    continue;
                }

                float coreRadius = GameBalance.HollowCoreRadius + shot.Radius;
                bool coreHit = shot.SoulSenseAtFire &&
                    DistanceSquaredToSegment(hollow.CorePosition, shot.PreviousPosition, shot.Position) <= coreRadius * coreRadius;
                int damage = coreHit
                    ? (int)MathF.Round(shot.Damage * GameBalance.CannonCoreDamageMultiplier)
                    : shot.Damage;
                float knockback = MathHelper.Lerp(330f, 760f, shot.Charge);
                hollow.ApplyDamage(new DamageInfo(
                    damage,
                    shot.Direction * knockback,
                    coreHit ? hollow.CorePosition : hollow.Position,
                    coreHit,
                    shot.IsFullCharge));

                Color impact = coreHit || shot.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
                _particles.EmitBurst(coreHit ? hollow.CorePosition : hollow.Position, shot.Direction, shot.IsFullCharge ? 34 : 18, impact, shot.IsFullCharge ? 390f : 220f, shot.IsFullCharge ? 12f : 7f);
                _particles.EmitDeathFlame(hollow.Position, shot.IsFullCharge ? 15 : 7, shot.IsFullCharge ? 1.4f : 0.85f);
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
