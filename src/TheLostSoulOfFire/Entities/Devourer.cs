using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public enum DevourerState
{
    ApproachPlayer,
    ApproachSoul,
    SlamTelegraph,
    Slam,
    Devour,
    Recovery,
    Staggered,
    Dying,
    Dead
}

public sealed class Devourer : Enemy
{
    private readonly List<Soul> _consumedSouls = [];
    private float _stateTimer;
    private float _visualTime;
    private bool _slamDamagePending;
    private bool _soulSpawnPending;
    private bool _extractionEffectPending;
    private Soul _targetSoul;
    private Vector2 _facing = -Vector2.UnitY;

    public DevourerState State { get; private set; } = DevourerState.ApproachPlayer;
    public override string StateLabel => State.ToString().ToUpperInvariant();
    public int ConsumedSoulCount => _consumedSouls.Count;
    public Vector2 FacingDirection => _facing;
    public Vector2 TorsoPosition => Position + new Vector2(0f, -8f);
    public float TelegraphProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.DevourerSlamTelegraph, 0f, 1f);
    public float StrikeProgress => MathHelper.Clamp(1f - _stateTimer / GameBalance.DevourerSlamDuration, 0f, 1f);

    // The Devourer's Anchor is the torso cavity where it holds what it has taken.
    public override Vector2 AnchorPosition => TorsoPosition;
    public override float CommitmentThreatRange => GameBalance.DevourerSlamRange;
    public override float CommitmentRemaining => State switch
    {
        DevourerState.SlamTelegraph => _stateTimer,
        DevourerState.Slam => 0f,
        _ => -1f
    };

    public Devourer(Vector2 position)
        : base(position, GameBalance.DevourerMaxHealth, GameBalance.DevourerRadius)
    {
    }

    /// <summary>
    /// Places a Soul the Devourer swallowed before the Player arrived. It is held,
    /// not destroyed: Soul Sense shows it, a full Cannon shot or a Severance cut
    /// frees it, and killing the manifestation releases whatever is left. The
    /// caller keeps ownership of the Soul so the world still updates and draws it.
    /// </summary>
    public void SeedHeldSoul(Soul soul)
    {
        soul.BeginDevour();
        soul.Consume();
        _consumedSouls.Add(soul);
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

        if (State == DevourerState.Dead)
        {
            return;
        }

        if (State == DevourerState.Dying)
        {
            UpdateDying(deltaTime, particles);
            return;
        }

        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        if (State is not (DevourerState.SlamTelegraph or DevourerState.Slam or DevourerState.Devour or DevourerState.Recovery or DevourerState.Staggered))
        {
            Soul availableSoul = FindClosestSoul(souls);
            if (availableSoul is not null)
            {
                if (_targetSoul != availableSoul)
                {
                    particles.EmitBurst(TorsoPosition, Vector2.UnitY, 10, GameBalance.DeathFlame, 90f, 5f);
                }

                _targetSoul = availableSoul;
                State = DevourerState.ApproachSoul;
            }
            else
            {
                _targetSoul = null;
                State = DevourerState.ApproachPlayer;
            }
        }

        switch (State)
        {
            case DevourerState.ApproachPlayer:
                UpdateApproachPlayer(deltaTime, wardens.Target);
                break;

            case DevourerState.ApproachSoul:
                UpdateApproachSoul(deltaTime);
                break;

            case DevourerState.SlamTelegraph:
                // Committed: keep facing the brother the slam was shown to.
                Face(wardens.Target.Position);
                if (_stateTimer <= 0f)
                {
                    State = DevourerState.Slam;
                    _stateTimer = GameBalance.DevourerSlamDuration;
                    _slamDamagePending = true;
                    screenEffects.AddShake(0.15f, 7f);
                }
                break;

            case DevourerState.Slam:
                ResolveSlam(wardens, screenEffects);
                if (_stateTimer <= 0f)
                {
                    State = DevourerState.Recovery;
                    _stateTimer = GameBalance.DevourerRecoveryDuration;
                }
                break;

            case DevourerState.Devour:
                UpdateDevour(deltaTime, particles);
                break;

            case DevourerState.Recovery:
            case DevourerState.Staggered:
                if (_stateTimer <= 0f)
                {
                    State = DevourerState.ApproachPlayer;
                }
                break;
        }
    }

    public override void ApplyDamage(DamageInfo damage)
    {
        DevourerState previousState = State;
        Soul interruptedSoul = _targetSoul;
        base.ApplyDamage(damage);
        if (!IsAlive)
        {
            return;
        }

        if (previousState == DevourerState.Devour && interruptedSoul is not null)
        {
            interruptedSoul.CancelDevour();
            _targetSoul = null;
            State = DevourerState.Staggered;
            _stateTimer = 0.38f;
        }

        if (damage.IsFullCannon)
        {
            if (_targetSoul?.State == SoulState.BeingDevoured)
            {
                _targetSoul.CancelDevour();
                _targetSoul = null;
            }

            State = DevourerState.Staggered;
            _stateTimer = GameBalance.DevourerFullCannonStagger;
            ExpelOneSoul();
        }
    }

    /// <summary>
    /// Severing the Devourer's Anchor tears the cavity open: the slam is cancelled
    /// and one held Soul is released. This is the moment the mechanic exists for —
    /// a correct read rescues a Soul that damage alone could not reach.
    /// </summary>
    public override void ApplySeverance()
    {
        if (!IsAlive || State is DevourerState.Dying or DevourerState.Dead)
        {
            return;
        }

        _slamDamagePending = false;
        if (_targetSoul?.State == SoulState.BeingDevoured)
        {
            _targetSoul.CancelDevour();
            _targetSoul = null;
        }

        State = DevourerState.Staggered;
        _stateTimer = GameBalance.SeveranceDevourerStagger;
        ExpelOneSoul();
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

    public bool TryConsumeExtractionEffect(out Vector2 position)
    {
        if (!_extractionEffectPending)
        {
            position = default;
            return false;
        }

        _extractionEffectPending = false;
        position = TorsoPosition;
        return true;
    }

    public override void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        if (State == DevourerState.Dead)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 4.2f);
        float stackScale = 1f + ConsumedSoulCount * 0.035f;
        Color body = HitFlashRemaining > 0f ? GameBalance.SoulWhite : new Color(27, 25, 33);
        Vector2 right = new(-_facing.Y, _facing.X);

        // The collapse is light, drawn in the additive combat pass.
        if (State == DevourerState.Dying)
        {
            return;
        }

        if (!useSpriteArt)
        {
            batch.FillCircle(pixel, Position + new Vector2(7f, 30f), 54f * stackScale, new Color(3, 3, 7) * 0.66f);
            batch.FillCircle(pixel, Position, 48f * stackScale, body);
            batch.DrawLine(pixel, Position - right * 32f, Position - right * 56f + _facing * 25f, body, 25f);
            batch.DrawLine(pixel, Position + right * 32f, Position + right * 57f + _facing * 22f, body, 25f);
            batch.DrawLine(pixel, Position - right * 21f + new Vector2(0f, 30f), Position - right * 26f + new Vector2(0f, 59f), new Color(20, 19, 25), 25f);
            batch.DrawLine(pixel, Position + right * 21f + new Vector2(0f, 30f), Position + right * 27f + new Vector2(0f, 57f), new Color(20, 19, 25), 25f);
            batch.FillCircle(pixel, Position + new Vector2(0f, -52f), 18f, new Color(20, 19, 26));
        }

        if (!useSpriteArt)
            batch.FillCircle(pixel, TorsoPosition, 28f, new Color(7, 5, 10));

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawCircle(pixel, TorsoPosition, GameBalance.DevourerTorsoRadius, new Color(255, 210, 80), 2f);
        }
    }

    /// <summary>
    /// The cavity light and the pull it exerts on a Soul. Everything that used to
    /// be a drawn tether line is now a stream of light between the Soul and the
    /// torso, so the theft reads as force rather than as a connector.
    /// </summary>
    public void DrawCombatLight(SpriteBatch batch, Texture2D brush, bool soulSenseActive)
    {
        if (State == DevourerState.Dead)
        {
            return;
        }

        if (State == DevourerState.Dying)
        {
            // The cavity fails and everything it was holding comes apart. Soft
            // light spilling out of the seams, not drawn cracks.
            float collapse = 1f - _stateTimer / GameBalance.DevourerDeathDuration;
            float fade = 1f - collapse;
            SoftShapes.Blob(batch, brush, TorsoPosition, (58f + collapse * 46f), GameBalance.DeepViolet * (0.4f * fade));
            for (int i = 0; i < 7; i++)
            {
                float angle = i * MathHelper.TwoPi / 7f + _visualTime * 0.4f;
                Vector2 seam = new(MathF.Cos(angle), MathF.Sin(angle) * 0.72f);
                Vector2 point = TorsoPosition + seam * (24f + collapse * 52f);
                SoftShapes.Blob(batch, brush, point, 15f + collapse * 8f, GameBalance.DeathFlameBright * (0.34f * fade));
            }
            SoftShapes.Blob(batch, brush, TorsoPosition, 20f + collapse * 14f, Color.White * (0.3f * fade));
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 4.2f);
        float held = 0.24f + ConsumedSoulCount * 0.09f;
        SoftShapes.Blob(batch, brush, TorsoPosition, 30f + pulse * 5f, GameBalance.DeepViolet * held);

        if (_targetSoul is not null && State is DevourerState.ApproachSoul or DevourerState.Devour)
        {
            bool feeding = State == DevourerState.Devour;
            Vector2 delta = TorsoPosition - _targetSoul.Position;
            float distance = delta.Length();
            if (distance > 1f)
            {
                Vector2 direction = delta / distance;
                int steps = Math.Max(3, (int)(distance / 22f));
                for (int i = 0; i < steps; i++)
                {
                    // Motes stream toward the cavity, faster and denser while feeding.
                    float amount = (i + 0.5f) / steps;
                    float flow = (amount + _visualTime * (feeding ? 1.5f : 0.55f)) % 1f;
                    Vector2 point = _targetSoul.Position + direction * (distance * flow);
                    float taper = MathF.Sin(flow * MathHelper.Pi);
                    SoftShapes.Blob(batch, brush, point, (feeding ? 13f : 7f) * taper,
                        (feeding ? GameBalance.DeathFlameBright : GameBalance.DeathFlame) * ((feeding ? 0.3f : 0.16f) * taper));
                }
            }

            SoftShapes.Blob(batch, brush, _targetSoul.Position, 34f + pulse * 8f,
                GameBalance.DeathFlameBright * (feeding ? 0.26f : 0.16f));
        }

        if (!soulSenseActive)
        {
            return;
        }

        SoftShapes.Blob(batch, brush, TorsoPosition, GameBalance.DevourerTorsoRadius * 1.6f, GameBalance.DeepViolet * 0.34f);
        int visibleSouls = Math.Max(1, ConsumedSoulCount);
        for (int i = 0; i < visibleSouls; i++)
        {
            float angle = _visualTime * (1.2f + i * 0.16f) + i * MathHelper.TwoPi / visibleSouls;
            Vector2 trapped = TorsoPosition + new Vector2(MathF.Cos(angle) * 13f, MathF.Sin(angle) * 10f);
            SoftShapes.Blob(batch, brush, trapped, ConsumedSoulCount > 0 ? 12f : 7f,
                (ConsumedSoulCount > 0 ? GameBalance.SoulWhite : GameBalance.DeathFlame) * 0.44f);
        }
    }

    protected override void OnDeath()
    {
        _targetSoul?.CancelDevour();
        _targetSoul = null;
        State = DevourerState.Dying;
        _stateTimer = GameBalance.DevourerDeathDuration;

        for (int i = _consumedSouls.Count - 1; i >= 0; i--)
        {
            float angle = i * MathHelper.TwoPi / Math.Max(1, _consumedSouls.Count);
            _consumedSouls[i].Expel(Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 58f);
        }

        _consumedSouls.Clear();
    }

    private Soul FindClosestSoul(IReadOnlyList<Soul> souls)
    {
        float maxDistanceSquared = GameBalance.DevourerSoulTargetRange * GameBalance.DevourerSoulTargetRange;
        Soul closest = null;
        foreach (Soul soul in souls.Where(soul => soul.CanBeDevoured))
        {
            float distanceSquared = Vector2.DistanceSquared(Position, soul.Position);
            if (distanceSquared < maxDistanceSquared)
            {
                maxDistanceSquared = distanceSquared;
                closest = soul;
            }
        }

        return closest;
    }

    private void UpdateApproachPlayer(float deltaTime, Player player)
    {
        Face(player.Position);
        float distance = Vector2.Distance(Position, player.Position);
        if (distance <= GameBalance.DevourerSlamStartRange)
        {
            State = DevourerState.SlamTelegraph;
            _stateTimer = GameBalance.DevourerSlamTelegraph;
            return;
        }

        Position += _facing * GameBalance.DevourerMoveSpeed * deltaTime;
    }

    private void UpdateApproachSoul(float deltaTime)
    {
        if (_targetSoul is null || !_targetSoul.CanBeDevoured)
        {
            _targetSoul = null;
            State = DevourerState.ApproachPlayer;
            return;
        }

        Face(_targetSoul.Position);
        if (Vector2.DistanceSquared(Position, _targetSoul.Position) <= GameBalance.DevourerDevourStartRange * GameBalance.DevourerDevourStartRange)
        {
            State = DevourerState.Devour;
            _stateTimer = GameBalance.DevourerDevourDuration;
            _targetSoul.BeginDevour();
            return;
        }

        Position += _facing * GameBalance.DevourerMoveSpeed * deltaTime;
    }

    private void UpdateDevour(float deltaTime, ParticleSystem particles)
    {
        if (_targetSoul is null || _targetSoul.State != SoulState.BeingDevoured)
        {
            _targetSoul = null;
            State = DevourerState.ApproachPlayer;
            return;
        }

        _targetSoul.PullToward(TorsoPosition, deltaTime);
        if (_stateTimer > 0f)
        {
            return;
        }

        _targetSoul.Consume();
        _consumedSouls.Add(_targetSoul);
        _targetSoul = null;
        Health = Math.Min(MaxHealth, Health + GameBalance.DevourerHealPerSoul);
        particles.EmitDeathFlame(TorsoPosition, 18, 1.25f);
        State = DevourerState.Recovery;
        _stateTimer = GameBalance.DevourerRecoveryDuration;
    }

    /// <summary>
    /// The slam is a ring of pressure, so it crushes every Warden standing in the
    /// ring the Players were shown — not only the one it was aimed at.
    /// </summary>
    private void ResolveSlam(WardenField wardens, ScreenEffects screenEffects)
    {
        if (!_slamDamagePending)
        {
            return;
        }

        _slamDamagePending = false;
        int damage = GameBalance.DevourerSlamDamage + Math.Min(ConsumedSoulCount, GameBalance.DevourerMaxSoulStacks) * GameBalance.DevourerDamagePerSoul;
        wardens.StrikeCircle(
            Position,
            GameBalance.DevourerSlamRange,
            damage,
            GameBalance.DevourerSlamKnockback,
            screenEffects,
            _facing);
    }

    private void ExpelOneSoul()
    {
        if (_consumedSouls.Count == 0)
        {
            return;
        }

        Soul soul = _consumedSouls[^1];
        _consumedSouls.RemoveAt(_consumedSouls.Count - 1);
        soul.Expel(TorsoPosition + _facing * 74f);
        _extractionEffectPending = true;
    }

    private void UpdateDying(float deltaTime, ParticleSystem particles)
    {
        float previous = _stateTimer;
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        if (previous > 0.38f && _stateTimer <= 0.38f)
        {
            particles.EmitBurst(Position, -_facing, 44, new Color(44, 38, 50), 250f, 12f);
            particles.EmitDeathFlame(Position, 24, 1.5f);
            _soulSpawnPending = true;
        }

        if (_stateTimer <= 0f)
        {
            State = DevourerState.Dead;
            IsFinished = true;
        }
    }

    private void Face(Vector2 target)
    {
        Vector2 direction = target - Position;
        if (direction.LengthSquared() > 0.001f)
        {
            _facing = Vector2.Normalize(direction);
        }
    }
}
