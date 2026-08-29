using System;
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
    private readonly DummyTarget _dummy;
    private Vector2 _lastMouseWorld;
    private bool _debugVisible;

    public string ScreenshotContext => !_dummy.IsAlive
        ? "phase04_dummy_down"
        : _player.Scythe.ActiveStep > 0
            ? $"phase04_scythe_hit_{_player.Scythe.ActiveStep}"
            : _debugVisible
                ? "phase04_debug_gameplay"
                : "phase04_arena_gameplay";

    public string WindowTitle => _debugVisible
        ? $"The Lost Soul of Fire — DEBUG | HP {_player.Health} | Dash {(_player.DashCooldownRemaining <= 0f ? "READY" : _player.DashCooldownRemaining.ToString("0.00"))} | Scythe {_player.Scythe.StateLabel} | Dummy {_dummy.Health}"
        : "The Lost Soul of Fire — WASD move | Mouse aim | Space dash | LMB Scythe | F1 debug | F9 screenshot";

    public GameWorld(Viewport viewport)
    {
        _camera = new Camera2D(_arena.CombatBounds.Center.ToVector2());
        _player = new Player(_arena.CombatBounds.Center.ToVector2());
        _dummy = new DummyTarget(_arena.CombatBounds.Center.ToVector2() + new Vector2(280f, 0f));
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
            _player.Reset(_arena.CombatBounds.Center.ToVector2());
            _dummy.Reset();
        }

        _lastMouseWorld = _camera.ScreenToWorld(input.MousePosition, viewport);

        if (_screenEffects.IsHitStopped)
        {
            return;
        }

        _player.Update(deltaTime, input, _lastMouseWorld, _arena.CombatBounds, _particles, _screenEffects);
        ResolveScytheStrike();
        _dummy.Update(deltaTime, _arena.CombatBounds);
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
        _player.DrawAfterimages(batch, pixel);
        _dummy.Draw(batch, pixel, _debugVisible);
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

        batch.End();
    }

    private void ResolveScytheStrike()
    {
        if (!_player.Scythe.TryConsumeStrike(out ScytheStrike strike) || !_dummy.IsAlive)
        {
            return;
        }

        Vector2 toTarget = _dummy.Position - _player.Position;
        float combinedRange = strike.Range + _dummy.Radius;
        if (toTarget.LengthSquared() > combinedRange * combinedRange)
        {
            return;
        }

        Vector2 targetDirection = toTarget.LengthSquared() > 0.001f ? Vector2.Normalize(toTarget) : strike.Direction;
        float facingDot = Vector2.Dot(strike.Direction, targetDirection);
        if (facingDot < MathF.Cos(strike.ArcRadians * 0.5f))
        {
            return;
        }

        _dummy.ApplyStrike(strike, targetDirection);
        Color impactColor = strike.Step == 3 ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        _particles.EmitBurst(_dummy.Position - targetDirection * _dummy.Radius * 0.35f, targetDirection, strike.Step == 3 ? 24 : 10, impactColor, strike.Step == 3 ? 260f : 150f, strike.Step == 3 ? 9f : 5f);
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
}
