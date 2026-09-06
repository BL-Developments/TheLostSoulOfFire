using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public enum BurningState
{
    Approach,
    Telegraph,
    Charge,
    Recovery,
    Dying,
    Detonating,
    Dead
}

public sealed class Burning : Enemy
{
    private readonly int _movementSeed;
    private float _stateTimer;
    private float _visualTime;
    private bool _chargeDamagePending;
    private bool _soulSpawnPending;
    private bool _detonationPending;
    private bool _detonationReleased;
    private bool _hasAggressionSlot;
    private Vector2 _facing = Vector2.UnitX;
    private Vector2 _chargeDirection = Vector2.UnitX;

    public BurningState State { get; private set; } = BurningState.Approach;
    public override string StateLabel => State.ToString().ToUpperInvariant();
    public bool IsCharging => State == BurningState.Charge;
    public bool IsAggressionCommitted => State is BurningState.Telegraph or BurningState.Charge;
    public Vector2 FacingDirection => _facing;
    public Vector2 ChargeDirection => _chargeDirection;
    public float TelegraphProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.BurningChargeTelegraph, 0f, 1f);
    public float ChargeProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.BurningChargeDuration, 0f, 1f);

    // The Burning's Anchor is the widest fracture in its shell.
    public override Vector2 AnchorPosition => Position + new Vector2(11f, -3f);
    public override float CommitmentThreatRange => Radius + 62f;
    public override float CommitmentRemaining => State switch
    {
        BurningState.Telegraph => _stateTimer,
        BurningState.Charge => 0f,
        _ => -1f
    };

    public Burning(Vector2 position, int movementSeed)
        : base(position, GameBalance.BurningMaxHealth, GameBalance.BurningRadius)
    {
        _movementSeed = movementSeed;
    }

    public override void Update(
        float deltaTime,
        WardenField wardens,
        IReadOnlyList<Soul> souls,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects)
    {
        UpdateCommon(deltaTime, movementBounds);
        _visualTime += deltaTime;

        if (State == BurningState.Dead)
        {
            return;
        }

        if (State is BurningState.Dying or BurningState.Detonating)
        {
            UpdateDeath(deltaTime, particles);
            return;
        }

        Vector2 toTarget = wardens.Target.Position - Position;
        float distance = toTarget.Length();
        if (distance > 0.001f && State != BurningState.Charge)
        {
            _facing = toTarget / distance;
        }

        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        switch (State)
        {
            case BurningState.Approach:
                UpdateApproach(deltaTime, distance, particles);
                break;

            case BurningState.Telegraph:
                _chargeDirection = distance > 0.001f ? toTarget / distance : _facing;
                if (_stateTimer <= 0f)
                {
                    State = BurningState.Charge;
                    _stateTimer = GameBalance.BurningChargeDuration;
                    _facing = _chargeDirection;
                    _chargeDamagePending = true;
                    particles.EmitBurst(Position, -_chargeDirection, 16, GameBalance.DeathFlameBright, 210f, 7f);
                    screenEffects.AddShake(0.1f, 4f);
                }
                break;

            case BurningState.Charge:
                Position += _chargeDirection * GameBalance.BurningChargeSpeed * deltaTime;
                particles.EmitDeathFlame(Position - _chargeDirection * 16f, 2, 0.72f);
                ResolveChargeHit(wardens, screenEffects);
                if (_stateTimer <= 0f)
                {
                    EnterRecovery();
                }
                break;

            case BurningState.Recovery:
                if (_stateTimer <= 0f)
                {
                    State = BurningState.Approach;
                }
                break;
        }
    }

    public void SetAggressionSlot(bool hasSlot)
    {
        _hasAggressionSlot = hasSlot || IsAggressionCommitted;
    }

    public override void ApplyDamage(DamageInfo damage)
    {
        base.ApplyDamage(damage);
        if (IsAlive && State == BurningState.Telegraph && damage.IsFullCannon)
        {
            State = BurningState.Recovery;
            _stateTimer = GameBalance.BurningRecoveryDuration;
        }
    }

    /// <summary>
    /// A Burning that is already committed is unstable. Severing its fracture
    /// during the charge collapses it into the same detonation the Cannon causes,
    /// so the reaction reinforces the Burning's existing identity instead of
    /// replacing it. Outside the charge it is only broken open and staggered.
    /// </summary>
    public override void ApplySeverance()
    {
        if (!IsAlive || State is BurningState.Dying or BurningState.Detonating or BurningState.Dead)
        {
            return;
        }

        if (State == BurningState.Charge)
        {
            Detonate();
            return;
        }

        _chargeDamagePending = false;
        State = BurningState.Recovery;
        _stateTimer = GameBalance.SeveranceBurningStagger;
    }

    public void Detonate()
    {
        if (!IsAlive || State != BurningState.Charge)
        {
            return;
        }

        Health = 0;
        State = BurningState.Detonating;
        _stateTimer = GameBalance.BurningDeathDuration;
        _detonationPending = false;
        _detonationReleased = false;
        _soulSpawnPending = false;
    }

    public bool TryConsumeDetonation(out Vector2 position)
    {
        if (!_detonationPending)
        {
            position = default;
            return false;
        }

        _detonationPending = false;
        position = Position;
        return true;
    }

    public override bool TryConsumeSoulSpawn(out Vector2 position)
    {
        if (!_soulSpawnPending)
        {
            position = default;
            return false;
        }

        _soulSpawnPending = false;
        position = Position;
        return true;
    }

    public Vector2[] GetFracturePositions() =>
    [
        Position + new Vector2(-10f, -20f),
        Position + new Vector2(11f, -3f),
        Position + new Vector2(-7f, 16f)
    ];

    public override void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        if (State == BurningState.Dead)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * (Health <= MaxHealth / 4 ? 13f : 8f));
        float telegraph = State == BurningState.Telegraph
            ? 1f - _stateTimer / GameBalance.BurningChargeTelegraph
            : 0f;
        Color body = HitFlashRemaining > 0f ? GameBalance.SoulWhite : new Color(29, 24, 31);
        Vector2 right = new(-_facing.Y, _facing.X);

        // Detonation is pure light; it is drawn in the additive combat pass.
        if (State == BurningState.Detonating)
        {
            return;
        }

        if (!useSpriteArt)
        {
            batch.FillCircle(pixel, Position + new Vector2(4f, 21f), 29f, new Color(3, 3, 6) * 0.62f);
            batch.DrawLine(pixel, Position + new Vector2(0f, -31f), Position + new Vector2(0f, 27f), body, 28f);
            batch.DrawLine(pixel, Position - right * 10f + new Vector2(0f, 6f), Position - right * 22f + _facing * 24f, body, 12f);
            batch.DrawLine(pixel, Position + right * 10f + new Vector2(0f, 6f), Position + right * 23f + _facing * 21f, body, 12f);
            batch.FillCircle(pixel, Position + new Vector2(0f, -37f), 12f, new Color(24, 20, 27));
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            if (State == BurningState.Charge)
            {
                batch.DrawCircle(pixel, Position, GameBalance.BurningDetonationRadius, new Color(255, 190, 70) * 0.45f, 2f, 32);
            }
        }
    }

    /// <summary>
    /// Light bleeding out of the shell's fractures, and the wake behind a commited
    /// charge. Both used to be stroked lines across the sprite.
    /// </summary>
    public void DrawCombatLight(SpriteBatch batch, Texture2D brush, bool soulSenseActive)
    {
        if (State == BurningState.Detonating)
        {
            DrawDetonationLight(batch, brush);
            return;
        }

        if (State is BurningState.Dead or BurningState.Dying)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * (Health <= MaxHealth / 4 ? 13f : 8f));
        float unstable = 0.16f + pulse * 0.16f;

        foreach (Vector2 fracture in GetFracturePositions())
        {
            SoftShapes.Blob(batch, brush, fracture, 15f + pulse * 4f, GameBalance.DeathFlame * unstable);
            if (soulSenseActive)
            {
                SoftShapes.Blob(batch, brush, fracture, 20f, GameBalance.DeepViolet * 0.42f);
                SoftShapes.Blob(batch, brush, fracture, 8f, GameBalance.SoulWhite * 0.5f);
            }
        }

        if (State != BurningState.Charge)
        {
            return;
        }

        SoftShapes.Streak(batch, brush, Position - _chargeDirection * 44f, _chargeDirection, 62f, 25f, GameBalance.DeepViolet * 0.4f);
        SoftShapes.Streak(batch, brush, Position - _chargeDirection * 30f, _chargeDirection, 42f, 10f, GameBalance.DeathFlameBright * 0.34f);
    }

    /// <summary>
    /// The shell pulls its Death Flame inward, holds, and lets go. Built entirely
    /// from feathered light so the detonation blooms instead of expanding a ring.
    /// </summary>
    private void DrawDetonationLight(SpriteBatch batch, Texture2D brush)
    {
        float releaseRemaining = GameBalance.BurningDeathDuration - CombatFeedbackTuning.BurningCompressionDuration;

        if (!_detonationReleased)
        {
            float compression = MathHelper.Clamp(
                (GameBalance.BurningDeathDuration - _stateTimer) / CombatFeedbackTuning.BurningCompressionDuration,
                0f,
                1f);
            float instability = 0.5f + 0.5f * MathF.Sin(_visualTime * 42f);
            float radius = MathHelper.Lerp(62f, 18f, compression);

            SoftShapes.Blob(batch, brush, Position, radius * 1.5f, GameBalance.DeepViolet * (0.16f + compression * 0.28f));
            SoftShapes.Ring(batch, brush, Position, radius + instability * 5f, 14f + compression * 10f,
                GameBalance.DeathFlameBright * (0.24f + compression * 0.24f), 20, _visualTime * 6f);
            SoftShapes.Blob(batch, brush, Position, 10f + compression * 12f, Color.White * (0.4f + compression * 0.4f));

            // The fractures are drawn shut as the flame is forced inward.
            foreach (Vector2 fracture in GetFracturePositions())
            {
                Vector2 point = Vector2.Lerp(fracture, Position, compression);
                SoftShapes.Blob(batch, brush, point, 11f + compression * 5f, GameBalance.DeathFlameBright * 0.4f);
            }
            return;
        }

        float progress = 1f - MathHelper.Clamp(_stateTimer / releaseRemaining, 0f, 1f);
        float fade = 1f - progress;
        SoftShapes.Blob(batch, brush, Position, 34f + progress * 128f, GameBalance.DeepViolet * (0.34f * fade));
        SoftShapes.Ring(batch, brush, Position, 40f + progress * 132f, 30f * fade,
            GameBalance.DeathFlameBright * (0.44f * fade), 30, _visualTime * 2f);
        SoftShapes.Blob(batch, brush, Position, 22f + progress * 40f, Color.White * (0.4f * fade * fade));
    }

    protected override void OnDeath()
    {
        State = BurningState.Dying;
        _stateTimer = GameBalance.BurningDeathDuration;
    }

    private void ResolveChargeHit(WardenField wardens, ScreenEffects screenEffects)
    {
        if (!_chargeDamagePending)
        {
            return;
        }

        // Body contact: whoever the charge actually runs into is hit, and the
        // charge is spent on the first Warden it reaches.
        if (!wardens.StrikeContact(
                Position,
                Radius,
                _chargeDirection,
                GameBalance.BurningChargeDamage,
                GameBalance.BurningChargeKnockback,
                screenEffects))
        {
            return;
        }

        _chargeDamagePending = false;
        EnterRecovery();
    }

    private void UpdateApproach(float deltaTime, float distance, ParticleSystem particles)
    {
        if (_hasAggressionSlot && distance <= GameBalance.BurningChargeStartRange)
        {
            State = BurningState.Telegraph;
            _stateTimer = GameBalance.BurningChargeTelegraph;
            _chargeDirection = _facing;
            particles.EmitDeathFlame(Position, 8, 0.9f);
            return;
        }

        if (_hasAggressionSlot || distance > GameBalance.BurningStalkOuterRange)
        {
            float approachSpeed = _hasAggressionSlot ? 1f : 0.68f;
            Position += _facing * GameBalance.BurningMoveSpeed * approachSpeed * deltaTime;
            return;
        }

        float strafeSign = _movementSeed % 2 == 0 ? 1f : -1f;
        Vector2 right = new(-_facing.Y, _facing.X);
        Vector2 stalkMovement = right * strafeSign * 0.58f;
        if (distance < GameBalance.BurningStalkInnerRange)
        {
            stalkMovement -= _facing * 0.7f;
        }
        else
        {
            stalkMovement += _facing * 0.18f;
        }

        Position += Vector2.Normalize(stalkMovement) * GameBalance.BurningMoveSpeed * 0.72f * deltaTime;
    }

    private void EnterRecovery()
    {
        State = BurningState.Recovery;
        _stateTimer = GameBalance.BurningRecoveryDuration;
        _chargeDamagePending = false;
    }

    private void UpdateDeath(float deltaTime, ParticleSystem particles)
    {
        float previous = _stateTimer;
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        float detonationReleaseRemaining = GameBalance.BurningDeathDuration - CombatFeedbackTuning.BurningCompressionDuration;
        if (State == BurningState.Detonating &&
            !_detonationReleased &&
            previous > detonationReleaseRemaining &&
            _stateTimer <= detonationReleaseRemaining)
        {
            _detonationReleased = true;
            _detonationPending = true;
            _soulSpawnPending = true;
        }

        if (State == BurningState.Dying && previous > 0.28f && _stateTimer <= 0.28f)
        {
            particles.EmitBurst(Position, -_facing, 22, new Color(45, 36, 46), 190f, 7f);
            particles.EmitDeathFlame(Position, 12, 1.05f);
            _soulSpawnPending = true;
        }

        if (_stateTimer <= 0f)
        {
            State = BurningState.Dead;
            IsFinished = true;
        }
    }
}
