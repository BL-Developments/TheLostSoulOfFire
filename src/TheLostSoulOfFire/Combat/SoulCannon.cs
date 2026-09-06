using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Combat;

public enum SoulCannonState
{
    Stored,
    Drawing,
    Charging,
    Returning
}

public readonly record struct CannonShotRequest(
    Vector2 Direction,
    float Charge,
    bool IsFullCharge,
    bool SoulSenseAtFire,
    int Damage,
    float Radius);

public sealed class SoulCannon
{
    private float _stateTimer;
    private float _chargeTime;
    private float _chargeParticleTimer;
    private float _visualTime;
    private bool _shotPending;
    private bool _fullCueCreated;
    private CannonShotRequest _pendingShot;
    private Vector2 _aimDirection = Vector2.UnitX;
    private bool _resonanceActive;

    public SoulCannonState State { get; private set; } = SoulCannonState.Stored;
    public float ChargeProgress => MathHelper.Clamp(_chargeTime / GetFullChargeTime(), 0f, 1f);
    public bool IsFullCharge => ChargeProgress >= 1f;
    public bool IsHandling => State != SoulCannonState.Stored;
    public bool CanUseScythe => State == SoulCannonState.Stored;
    public int ChargeStage => State is SoulCannonState.Stored or SoulCannonState.Returning
        ? 0
        : ChargeProgress < 0.25f
            ? 1
            : ChargeProgress < 0.67f
                ? 2
                : 3;
    public string StateLabel => State == SoulCannonState.Charging
        ? $"CHARGE {ChargeStage} {(int)(ChargeProgress * 100f)}%"
        : State.ToString().ToUpperInvariant();

    public void Reset()
    {
        State = SoulCannonState.Stored;
        _stateTimer = 0f;
        _chargeTime = 0f;
        _chargeParticleTimer = 0f;
        _visualTime = 0f;
        _shotPending = false;
        _fullCueCreated = false;
        _aimDirection = Vector2.UnitX;
        _resonanceActive = false;
    }

    public void Update(
        float deltaTime,
        PlayerCommand command,
        Vector2 playerPosition,
        Vector2 facingDirection,
        bool canStart,
        bool soulSenseActive,
        ParticleSystem particles,
        bool resonanceActive)
    {
        _visualTime += deltaTime;
        _resonanceActive = resonanceActive;
        _aimDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;

        switch (State)
        {
            case SoulCannonState.Stored:
                if (canStart && command.CannonPressed)
                {
                    State = SoulCannonState.Drawing;
                    _stateTimer = GameBalance.CannonDrawDuration;
                    _chargeTime = 0f;
                    _fullCueCreated = false;
                }
                break;

            case SoulCannonState.Drawing:
                _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
                if (!command.CannonHeld)
                {
                    Fire(soulSenseActive);
                }
                else if (_stateTimer <= 0f)
                {
                    State = SoulCannonState.Charging;
                    _chargeParticleTimer = 0f;
                }
                break;

            case SoulCannonState.Charging:
                _chargeTime = MathF.Min(GetFullChargeTime(), _chargeTime + deltaTime);
                EmitChargeParticles(deltaTime, playerPosition, particles);
                if (IsFullCharge && !_fullCueCreated)
                {
                    _fullCueCreated = true;
                    Vector2 muzzle = playerPosition + _aimDirection * 68f;
                    particles.EmitConvergence(muzzle, 18, 82f, GameBalance.SoulWhite, 0.2f, 5.5f);
                    particles.EmitBurst(muzzle, -_aimDirection, 7, GameBalance.SoulWhite, 105f, 5f);
                }

                if (!command.CannonHeld)
                {
                    Fire(soulSenseActive);
                }
                break;

            case SoulCannonState.Returning:
                _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
                if (_stateTimer <= 0f)
                {
                    State = SoulCannonState.Stored;
                    _chargeTime = 0f;
                }
                break;
        }
    }

    public bool TryConsumeShot(out CannonShotRequest shot)
    {
        if (!_shotPending)
        {
            shot = default;
            return false;
        }

        _shotPending = false;
        shot = _pendingShot;
        return true;
    }

    public float GetMovementMultiplier() => State switch
    {
        SoulCannonState.Charging => GameBalance.CannonChargeMovementMultiplier,
        SoulCannonState.Drawing or SoulCannonState.Returning => GameBalance.CannonHandlingMovementMultiplier,
        _ => 1f
    };

    // The stowed cannon used to be drawn here and is now deliberately absent.
    // The Soul Cannon is Death Flame given a shape: it is manifested when the
    // Warden draws it and let go when he stops, so there is nothing on his back
    // to draw. That also keeps the carried silhouette to one readable object —
    // the scythe — instead of two competing ones.

    public void DrawActive(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D weaponTexture,
        Vector2 playerPosition,
        Vector2 facingDirection)
    {
        if (State == SoulCannonState.Stored)
        {
            return;
        }

        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        float transition = State switch
        {
            SoulCannonState.Drawing => 1f - _stateTimer / GameBalance.CannonDrawDuration,
            SoulCannonState.Returning => _stateTimer / GameBalance.CannonReturnDuration,
            _ => 1f
        };
        Vector2 storedStock = playerPosition - facingDirection * 24f - right * 22f;
        Vector2 storedBarrel = playerPosition + facingDirection * 34f + right * 20f;
        Vector2 activeStock = playerPosition - facingDirection * 19f + right * 6f;
        Vector2 activeBarrel = playerPosition + facingDirection * 72f + right * 6f;
        Vector2 stock = Vector2.Lerp(storedStock, activeStock, transition);
        Vector2 barrel = Vector2.Lerp(storedBarrel, activeBarrel, transition);
        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * (IsFullCharge ? 28f : 17f));
        float vibrationStrength = State == SoulCannonState.Charging && ChargeStage >= 3
            ? MathHelper.Lerp(0.65f, 1.8f, MathHelper.Clamp((ChargeProgress - 0.67f) / 0.33f, 0f, 1f))
            : 0f;
        Vector2 vibration = right * MathF.Sin(_visualTime * 53f) * vibrationStrength;
        stock += vibration * 0.35f;
        barrel += vibration;
        DrawWeapon(batch, weaponTexture, stock, barrel);
    }

    /// <summary>
    /// The charge, painted with the feathered brush in the additive combat-light
    /// pass.
    ///
    /// This used to be drawn as filled and stroked circles in the sprite pass: a
    /// hard bright ring at the muzzle and a hard line into the chest. That is
    /// exactly the "shiny vector lines in the fighting plane" the owner rejected
    /// on 2026-09-06, and it survived that pass because it lived in the cannon
    /// rather than in the shared telegraph code.
    /// </summary>
    public void DrawChargeLight(SpriteBatch batch, Texture2D brush, Vector2 playerPosition, Vector2 facingDirection)
    {
        if (State == SoulCannonState.Stored || ChargeProgress <= 0f)
        {
            return;
        }

        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        float charge = ChargeProgress;
        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * (IsFullCharge ? 28f : 17f));
        Color energy = IsFullCharge
            ? GameBalance.SoulWhite
            : Color.Lerp(GameBalance.DeepViolet, GameBalance.DeathFlameBright, charge);

        Vector2 core = playerPosition + facingDirection * 2f;
        Vector2 chamber = playerPosition + facingDirection * 24f + right * 6f;
        Vector2 muzzle = playerPosition + facingDirection * 72f + right * 6f;

        // The Soul being drawn out of the Warden and into the brace.
        SoftShapes.Streak(batch, brush, Vector2.Lerp(core, chamber, 0.5f), facingDirection,
            13f, 4.5f + charge * 2.5f, GameBalance.DeathFlame * (0.16f + charge * 0.2f));
        SoftShapes.Blob(batch, brush, chamber, 7f + charge * 5f + pulse * 1.5f, energy * (0.2f + charge * 0.2f));
        SoftShapes.Blob(batch, brush, muzzle, 12f + charge * 16f + pulse * 2f, energy * (0.13f + charge * 0.17f));
        SoftShapes.Blob(batch, brush, muzzle, 4f + charge * 6f + pulse, energy * (0.18f + charge * 0.22f));
        if (IsFullCharge)
        {
            SoftShapes.Blob(batch, brush, muzzle, 26f + pulse * 6f, GameBalance.SoulWhite * 0.12f);
        }
    }

    private void Fire(bool soulSenseActive)
    {
        float charge = ChargeProgress;
        bool full = IsFullCharge;
        int damage = (int)MathF.Round(MathHelper.Lerp(GameBalance.CannonWeakDamage, GameBalance.CannonFullDamage, charge));
        float radius = MathHelper.Lerp(11f, 25f, charge);
        if (_resonanceActive)
        {
            damage = (int)MathF.Round(damage * GameBalance.ResonanceCannonDamageMultiplier);
            radius *= GameBalance.ResonanceCannonSizeMultiplier;
        }
        _pendingShot = new CannonShotRequest(
            _aimDirection,
            charge,
            full,
            soulSenseActive,
            damage,
            radius);
        _shotPending = true;
        State = SoulCannonState.Returning;
        _stateTimer = GameBalance.CannonReturnDuration;
    }

    private void EmitChargeParticles(float deltaTime, Vector2 playerPosition, ParticleSystem particles)
    {
        _chargeParticleTimer -= deltaTime;
        if (_chargeParticleTimer > 0f)
        {
            return;
        }

        _chargeParticleTimer = ChargeStage switch
        {
            1 => 0.12f,
            2 => 0.075f,
            _ => IsFullCharge ? 0.045f : 0.055f
        };
        Vector2 muzzle = playerPosition + _aimDirection * 68f;
        int particleCount = ChargeStage switch { 1 => 1, 2 => 2, _ => 3 };
        float convergenceRadius = ChargeStage switch { 1 => 38f, 2 => 56f, _ => 72f };
        Color color = IsFullCharge
            ? GameBalance.SoulWhite
            : ChargeStage >= 3
                ? GameBalance.DeathFlameBright
                : GameBalance.DeathFlame;
        particles.EmitConvergence(
            muzzle,
            particleCount,
            convergenceRadius,
            color,
            MathHelper.Lerp(0.16f, 0.11f, ChargeProgress),
            2.8f + ChargeProgress * 2.1f);
    }

    private float GetFullChargeTime() => _resonanceActive
        ? GameBalance.CannonFullChargeTime / GameBalance.ResonanceCannonChargeSpeedMultiplier
        : GameBalance.CannonFullChargeTime;

    private static void DrawWeapon(
        SpriteBatch batch,
        Texture2D weaponTexture,
        Vector2 stock,
        Vector2 barrel)
    {
        Vector2 direction = Vector2.Normalize(barrel - stock);
        float rotation = MathF.Atan2(direction.Y, direction.X);
        float displayLength = Vector2.Distance(stock, barrel) + 36f;
        batch.Draw(
            weaponTexture,
            Vector2.Lerp(stock, barrel, 0.52f),
            null,
            Color.White,
            rotation,
            new Vector2(weaponTexture.Width, weaponTexture.Height) * 0.5f,
            displayLength / weaponTexture.Width,
            SpriteEffects.None,
            0f);
    }
}
