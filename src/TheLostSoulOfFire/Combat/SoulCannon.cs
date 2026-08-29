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
        _shotPending = false;
        _fullCueCreated = false;
        _aimDirection = Vector2.UnitX;
        _resonanceActive = false;
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 playerPosition,
        Vector2 facingDirection,
        bool canStart,
        bool soulSenseActive,
        ParticleSystem particles,
        bool resonanceActive)
    {
        _resonanceActive = resonanceActive;
        _aimDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;

        switch (State)
        {
            case SoulCannonState.Stored:
                if (canStart && input.WasRightMousePressed)
                {
                    State = SoulCannonState.Drawing;
                    _stateTimer = GameBalance.CannonDrawDuration;
                    _chargeTime = 0f;
                    _fullCueCreated = false;
                }
                break;

            case SoulCannonState.Drawing:
                _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
                if (input.WasRightMouseReleased)
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
                    particles.EmitBurst(muzzle, -_aimDirection, 22, GameBalance.SoulWhite, 150f, 7f);
                    particles.EmitDeathFlame(muzzle, 15, 1.3f);
                }

                if (input.WasRightMouseReleased || !input.IsRightMouseDown)
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

    public void DrawBack(SpriteBatch batch, Texture2D pixel, Vector2 playerPosition, Vector2 facingDirection)
    {
        if (State != SoulCannonState.Stored)
        {
            return;
        }

        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        Vector2 stock = playerPosition - facingDirection * 24f - right * 22f;
        Vector2 barrel = playerPosition + facingDirection * 34f + right * 20f;
        DrawWeapon(batch, pixel, stock, barrel, 0f, false);
    }

    public void DrawActive(SpriteBatch batch, Texture2D pixel, Vector2 playerPosition, Vector2 facingDirection)
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
        DrawWeapon(batch, pixel, stock, barrel, ChargeProgress, IsFullCharge);

        if (State == SoulCannonState.Charging)
        {
            Vector2 core = playerPosition + facingDirection * 2f;
            batch.DrawLine(pixel, core, playerPosition + facingDirection * 22f + right * 6f, GameBalance.DeathFlame * (0.35f + ChargeProgress * 0.45f), 4f + ChargeProgress * 3f);
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

        _chargeParticleTimer = MathHelper.Lerp(0.12f, 0.025f, ChargeProgress);
        Vector2 muzzle = playerPosition + _aimDirection * 68f;
        particles.EmitDeathFlame(muzzle, ChargeStage, 0.55f + ChargeProgress * 0.75f);
    }

    private float GetFullChargeTime() => _resonanceActive
        ? GameBalance.CannonFullChargeTime / GameBalance.ResonanceCannonChargeSpeedMultiplier
        : GameBalance.CannonFullChargeTime;

    private static void DrawWeapon(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 stock,
        Vector2 barrel,
        float charge,
        bool full)
    {
        Vector2 direction = Vector2.Normalize(barrel - stock);
        Vector2 right = new(-direction.Y, direction.X);
        batch.DrawLine(pixel, stock, barrel, new Color(12, 12, 17), 25f);
        batch.DrawLine(pixel, stock + right * 9f, barrel + right * 9f, new Color(85, 82, 92), 4f);
        batch.DrawLine(pixel, stock - right * 8f, barrel - right * 8f, new Color(46, 44, 54), 6f);
        batch.FillCircle(pixel, stock + direction * 26f, 13f, new Color(28, 26, 34));
        batch.DrawCircle(pixel, stock + direction * 26f, 13f, new Color(126, 120, 132), 3f, 18);
        batch.FillRectangle(pixel, new Rectangle((int)(barrel.X - 8f), (int)(barrel.Y - 8f), 16, 16), new Color(19, 18, 24));

        if (charge <= 0f)
        {
            return;
        }

        Color energy = full ? GameBalance.SoulWhite : Color.Lerp(GameBalance.DeepViolet, GameBalance.DeathFlameBright, charge);
        batch.FillCircle(pixel, stock + direction * 28f, 4f + charge * 10f, energy * (0.55f + charge * 0.4f));
        batch.DrawCircle(pixel, barrel, 10f + charge * 17f, GameBalance.DeathFlame * (0.4f + charge * 0.5f), 3f + charge * 3f, 24);
        if (full)
        {
            batch.FillCircle(pixel, barrel, 10f, GameBalance.SoulWhite);
            batch.DrawCircle(pixel, barrel, 34f, GameBalance.SoulWhite * 0.8f, 3f, 28);
        }
    }
}
