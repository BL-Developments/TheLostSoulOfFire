using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public sealed class Player
{
    private sealed class Afterimage
    {
        public Vector2 Position;
        public Vector2 Facing;
        public float Remaining;
        public float Lifetime;
    }

    private readonly List<Afterimage> _afterimages = [];
    private float _idleParticleTimer;
    private float _visualTime;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashTrailTimer;
    private float _afterimageTimer;
    private Vector2 _dashDirection = Vector2.UnitX;
    private Vector2 _attackImpulse;
    private Vector2 _damageKnockback;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.UnitX;
    public int Health { get; private set; } = GameBalance.PlayerMaxHealth;
    public float Radius => GameBalance.PlayerRadius;
    public float InvulnerabilityRemaining { get; private set; }
    public float DashCooldownRemaining => _dashCooldownTimer;
    public bool IsDashing => _dashTimer > 0f;
    public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
    public bool IsDead => Health <= 0;
    public float Resonance { get; private set; }
    public bool SoulSenseActive { get; private set; }
    public ScytheCombat Scythe { get; } = new();
    public SoulCannon Cannon { get; } = new();

    public Player(Vector2 position)
    {
        Position = position;
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        FacingDirection = Vector2.UnitX;
        Health = GameBalance.PlayerMaxHealth;
        _idleParticleTimer = 0f;
        _dashTimer = 0f;
        _dashCooldownTimer = 0f;
        InvulnerabilityRemaining = 0f;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        Resonance = 0f;
        SoulSenseActive = false;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 mouseWorld,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects)
    {
        _visualTime += deltaTime;
        SoulSenseActive = !IsDead && input.IsKeyDown(Keys.Q);
        _dashCooldownTimer = MathF.Max(0f, _dashCooldownTimer - deltaTime);
        InvulnerabilityRemaining = MathF.Max(0f, InvulnerabilityRemaining - deltaTime);
        UpdateAfterimages(deltaTime);

        if (IsDead)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 toMouse = mouseWorld - Position;
        if (toMouse.LengthSquared() > 4f)
        {
            FacingDirection = Vector2.Normalize(toMouse);
        }

        Vector2 movement = ReadMovement(input);

        Cannon.Update(
            deltaTime,
            input,
            Position,
            FacingDirection,
            !IsDashing && Scythe.ActiveStep == 0,
            SoulSenseActive,
            particles);

        Scythe.Update(deltaTime, input, FacingDirection, Position, particles, !IsDashing && Cannon.CanUseScythe);
        if (Scythe.StartedThisFrame)
        {
            _attackImpulse = Scythe.AttackDirection * Scythe.GetForwardImpulse();
        }

        if (input.WasKeyPressed(Keys.Space) && _dashCooldownTimer <= 0f && Scythe.ActiveStep == 0)
        {
            StartDash(movement, particles, screenEffects);
        }

        if (_dashTimer > 0f)
        {
            UpdateDash(deltaTime, particles);
        }
        else
        {
            float movementMultiplier = SoulSenseActive ? GameBalance.SoulSenseMovementMultiplier : 1f;
            movementMultiplier *= Cannon.GetMovementMultiplier();
            Velocity = movement * GameBalance.PlayerMoveSpeed * movementMultiplier + _attackImpulse + _damageKnockback;
            _attackImpulse *= MathF.Pow(0.002f, deltaTime);
            _damageKnockback *= MathF.Pow(0.012f, deltaTime);
        }

        Position += Velocity * deltaTime;
        ClampTo(movementBounds);

        _idleParticleTimer -= deltaTime;
        if (_idleParticleTimer <= 0f && !IsDashing)
        {
            _idleParticleTimer = 0.16f;
            particles.EmitDeathFlame(Position - FacingDirection * 2f, 1, 0.55f);
        }
    }

    public void DrawAfterimages(SpriteBatch batch, Texture2D pixel)
    {
        foreach (Afterimage afterimage in _afterimages)
        {
            float alpha = afterimage.Remaining / afterimage.Lifetime;
            Vector2 right = new(-afterimage.Facing.Y, afterimage.Facing.X);
            Color silhouette = new Color(69, 28, 112) * (alpha * 0.48f);
            batch.DrawLine(pixel, afterimage.Position - afterimage.Facing * 15f, afterimage.Position + afterimage.Facing * 14f, silhouette, 28f);
            batch.DrawLine(pixel, afterimage.Position - afterimage.Facing * 13f, afterimage.Position - afterimage.Facing * 35f + right * 9f, silhouette, 11f);
            batch.FillCircle(pixel, afterimage.Position + afterimage.Facing * 18f, 10f, silhouette);
            batch.FillCircle(pixel, afterimage.Position, 4f, GameBalance.DeathFlameBright * (alpha * 0.35f));
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, bool debugVisible)
    {
        if (IsDead)
        {
            float deathPulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 5f);
            batch.FillCircle(pixel, Position, 13f + deathPulse * 3f, GameBalance.DeepViolet * 0.8f);
            batch.FillCircle(pixel, Position, 6f + deathPulse, GameBalance.SoulWhite * 0.8f);
            return;
        }

        Vector2 right = new(-FacingDirection.Y, FacingDirection.X);
        Vector2 rear = Position - FacingDirection * 16f;
        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 4f);

        batch.FillCircle(pixel, Position + new Vector2(3f, 8f), 24f, new Color(3, 3, 7) * 0.55f);

        Cannon.DrawBack(batch, pixel, Position, FacingDirection);
        Scythe.Draw(batch, pixel, Position, FacingDirection, debugVisible);

        // Long asymmetrical coat and narrow silhouette.
        batch.DrawLine(pixel, rear - right * 7f, Position - FacingDirection * 34f - right * 15f, new Color(20, 18, 28), 12f);
        batch.DrawLine(pixel, rear + right * 7f, Position - FacingDirection * 39f + right * 7f, new Color(28, 24, 38), 10f);
        batch.DrawLine(pixel, Position - FacingDirection * 10f, Position + FacingDirection * 11f, new Color(30, 28, 39), 31f);
        batch.DrawLine(pixel, Position - right * 10f, Position + right * 10f, new Color(41, 37, 52), 7f);

        Vector2 head = Position + FacingDirection * 18f;
        batch.FillCircle(pixel, head, 11f, new Color(22, 21, 29));
        batch.DrawLine(pixel, head + right * 7f - FacingDirection * 6f, head - right * 8f - FacingDirection * 3f, new Color(15, 14, 20), 5f);

        Vector2 eye = head + FacingDirection * 8f;
        Color eyeColor = SoulSenseActive ? GameBalance.SoulWhite : new Color(174, 166, 183);
        if (SoulSenseActive)
        {
            batch.FillCircle(pixel, eye, 8f, GameBalance.DeepViolet * 0.68f);
            batch.DrawLine(pixel, Position + FacingDirection * 4f, head, GameBalance.DeathFlame * 0.5f, 4f);
        }
        batch.DrawLine(pixel, eye - right * 4f, eye + right * 4f, eyeColor, SoulSenseActive ? 3f : 2f);

        batch.FillCircle(pixel, Position + FacingDirection * 2f, 7f + pulse * 1.3f, GameBalance.DeepViolet * 0.75f);
        batch.FillCircle(pixel, Position + FacingDirection * 2f, 3.2f + pulse * 0.6f, GameBalance.DeathFlameBright * 0.78f);

        Cannon.DrawActive(batch, pixel, Position, FacingDirection);

        if (IsDashing)
        {
            Vector2 ignitionOrigin = Position - _dashDirection * 15f;
            batch.DrawLine(pixel, ignitionOrigin - right * 8f, ignitionOrigin - _dashDirection * 23f - right * 11f, GameBalance.DeathFlame, 7f);
            batch.DrawLine(pixel, ignitionOrigin + right * 8f, ignitionOrigin - _dashDirection * 27f + right * 12f, GameBalance.DeathFlameBright, 5f);
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, Position, Position + FacingDirection * 70f, new Color(80, 220, 210) * 0.8f, 2f);
        }
    }

    private void StartDash(Vector2 movement, ParticleSystem particles, ScreenEffects screenEffects)
    {
        _dashDirection = movement.LengthSquared() > 0.001f ? Vector2.Normalize(movement) : FacingDirection;
        _dashTimer = GameBalance.DashDuration;
        _dashCooldownTimer = GameBalance.DashCooldown;
        InvulnerabilityRemaining = GameBalance.DashInvulnerability;
        _dashTrailTimer = 0f;
        _afterimageTimer = 0f;
        Velocity = _dashDirection * (GameBalance.DashDistance / GameBalance.DashDuration);

        AddAfterimage();
        particles.EmitBurst(Position - _dashDirection * 12f, -_dashDirection, 12, GameBalance.DeathFlameBright, 145f, 6f);
        particles.EmitDeathFlame(Position, 8, 1.35f);
        screenEffects.AddShake(0.1f, 3f);
    }

    public void ApplyDamage(int damage, Vector2 knockback, ScreenEffects screenEffects)
    {
        if (IsDead || IsInvulnerable)
        {
            return;
        }

        Health = Math.Max(0, Health - damage);
        _damageKnockback += knockback;
        InvulnerabilityRemaining = 0.5f;
        screenEffects.BeginHitstop(Health == 0 ? 0.12f : 0.045f);
        screenEffects.AddShake(Health == 0 ? 0.28f : 0.12f, Health == 0 ? 9f : 5f);
        screenEffects.Flash(0.09f, Health == 0 ? 0.34f : 0.2f);
    }

    public void AddResonance(float amount)
    {
        Resonance = MathHelper.Clamp(Resonance + amount, 0f, GameBalance.ResonanceRequired);
    }

    public void ApplyCannonRecoil(Vector2 shotDirection, float charge)
    {
        _damageKnockback -= shotDirection * MathHelper.Lerp(180f, 520f, charge);
    }

    private void UpdateDash(float deltaTime, ParticleSystem particles)
    {
        _dashTimer = MathF.Max(0f, _dashTimer - deltaTime);
        Velocity = _dashDirection * (GameBalance.DashDistance / GameBalance.DashDuration);

        _dashTrailTimer -= deltaTime;
        if (_dashTrailTimer <= 0f)
        {
            _dashTrailTimer = 0.02f;
            particles.EmitDeathFlame(Position - _dashDirection * 10f, 2, 1.05f);
        }

        _afterimageTimer -= deltaTime;
        if (_afterimageTimer <= 0f && _afterimages.Count < 3)
        {
            _afterimageTimer = 0.045f;
            AddAfterimage();
        }

        if (_dashTimer <= 0f)
        {
            Velocity *= 0.18f;
        }
    }

    private void AddAfterimage()
    {
        _afterimages.Add(new Afterimage
        {
            Position = Position,
            Facing = FacingDirection,
            Remaining = 0.19f,
            Lifetime = 0.19f
        });
    }

    private void UpdateAfterimages(float deltaTime)
    {
        for (int i = _afterimages.Count - 1; i >= 0; i--)
        {
            _afterimages[i].Remaining -= deltaTime;
            if (_afterimages[i].Remaining <= 0f)
            {
                _afterimages.RemoveAt(i);
            }
        }
    }

    public static Vector2 ReadMovement(InputState input)
    {
        Vector2 movement = Vector2.Zero;
        if (input.IsKeyDown(Keys.W)) movement.Y -= 1f;
        if (input.IsKeyDown(Keys.S)) movement.Y += 1f;
        if (input.IsKeyDown(Keys.A)) movement.X -= 1f;
        if (input.IsKeyDown(Keys.D)) movement.X += 1f;

        return movement.LengthSquared() > 1f ? Vector2.Normalize(movement) : movement;
    }

    private void ClampTo(Rectangle bounds)
    {
        Position = new Vector2(
            MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
            MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius));
    }
}
