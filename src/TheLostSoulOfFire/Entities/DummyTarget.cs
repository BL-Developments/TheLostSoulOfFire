using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public sealed class DummyTarget
{
    private readonly Vector2 _spawnPosition;
    private Vector2 _knockbackVelocity;
    private float _hitFlashTimer;
    private float _respawnTimer;

    public Vector2 Position { get; private set; }
    public int Health { get; private set; } = GameBalance.DummyMaxHealth;
    public float Radius => 30f;
    public bool IsAlive => Health > 0;

    public DummyTarget(Vector2 position)
    {
        _spawnPosition = position;
        Position = position;
    }

    public void Reset()
    {
        Position = _spawnPosition;
        Health = GameBalance.DummyMaxHealth;
        _knockbackVelocity = Vector2.Zero;
        _hitFlashTimer = 0f;
        _respawnTimer = 0f;
    }

    public void Update(float deltaTime, Rectangle bounds)
    {
        _hitFlashTimer = MathF.Max(0f, _hitFlashTimer - deltaTime);

        if (!IsAlive)
        {
            _respawnTimer -= deltaTime;
            if (_respawnTimer <= 0f)
            {
                Reset();
            }
            return;
        }

        Position += _knockbackVelocity * deltaTime;
        _knockbackVelocity *= MathF.Pow(0.015f, deltaTime);
        Position = new Vector2(
            MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
            MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius));
    }

    public void ApplyStrike(ScytheStrike strike, Vector2 knockbackDirection)
    {
        if (!IsAlive)
        {
            return;
        }

        Health = Math.Max(0, Health - strike.Damage);
        _knockbackVelocity += knockbackDirection * strike.Knockback;
        _hitFlashTimer = strike.Step == 3 ? 0.15f : 0.09f;

        if (Health <= 0)
        {
            _respawnTimer = 1.1f;
            _knockbackVelocity *= 1.35f;
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, bool debugVisible)
    {
        if (!IsAlive)
        {
            DrawBroken(batch, pixel);
            return;
        }

        Color body = _hitFlashTimer > 0f ? GameBalance.SoulWhite : new Color(46, 42, 52);
        batch.FillCircle(pixel, Position + new Vector2(4f, 15f), 31f, new Color(5, 4, 9) * 0.6f);
        batch.DrawLine(pixel, Position + new Vector2(0f, -43f), Position + new Vector2(0f, 34f), new Color(70, 61, 69), 13f);
        batch.DrawLine(pixel, Position + new Vector2(-26f, -14f), Position + new Vector2(26f, -14f), body, 12f);
        batch.FillRectangle(pixel, new Rectangle((int)Position.X - 24, (int)Position.Y - 31, 48, 62), body);
        batch.DrawRectangle(pixel, new Rectangle((int)Position.X - 24, (int)Position.Y - 31, 48, 62), new Color(112, 98, 119), 4f);
        batch.FillCircle(pixel, Position, 12f, GameBalance.DeepViolet * 0.7f);
        batch.DrawCircle(pixel, Position, 16f, GameBalance.DeathFlame * 0.72f, 3f, 20);

        Rectangle healthBack = new((int)Position.X - 41, (int)Position.Y - 68, 82, 9);
        batch.FillRectangle(pixel, healthBack, new Color(7, 6, 12));
        batch.FillRectangle(pixel, new Rectangle(healthBack.X + 2, healthBack.Y + 2, (int)(78f * Health / GameBalance.DummyMaxHealth), 5), new Color(178, 165, 188));

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
        }
    }

    private void DrawBroken(SpriteBatch batch, Texture2D pixel)
    {
        Color fading = GameBalance.DeepViolet * MathHelper.Clamp(_respawnTimer, 0f, 1f);
        batch.DrawLine(pixel, Position + new Vector2(-30f, -5f), Position + new Vector2(-8f, 18f), new Color(72, 64, 77), 12f);
        batch.DrawLine(pixel, Position + new Vector2(9f, 14f), Position + new Vector2(31f, -9f), new Color(72, 64, 77), 12f);
        batch.DrawCircle(pixel, Position, 28f + (1.1f - _respawnTimer) * 22f, fading, 4f, 24);
    }
}
