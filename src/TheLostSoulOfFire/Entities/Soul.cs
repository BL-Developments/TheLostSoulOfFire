using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

public enum SoulState
{
    Exposed,
    BeingDevoured,
    Releasing,
    Residue,
    Released,
    Consumed
}

public sealed class Soul
{
    private Vector2 _origin;
    private float _stateTimer;
    private float _visualTime;
    private bool _releaseBurstCreated;

    public SoulState State { get; private set; } = SoulState.Exposed;
    public Vector2 Position { get; private set; }
    public bool IsFinished => State == SoulState.Released;
    public float ReleaseProgress => State == SoulState.Releasing
        ? 1f - MathHelper.Clamp(_stateTimer / GameBalance.SoulReleaseDuration, 0f, 1f) : 0f;
    public bool CanBeDevoured => State is SoulState.Exposed or SoulState.Releasing or SoulState.BeingDevoured;

    /// <summary>
    /// The Warden the residue reached, set once on the frame it lands. The world
    /// reads it to credit the shared Resonance pool and then clears it.
    /// </summary>
    public Player ResidueReceiver { get; private set; }

    public bool TryConsumeResidueReceiver(out Player receiver)
    {
        receiver = ResidueReceiver;
        if (receiver is null)
        {
            return false;
        }

        ResidueReceiver = null;
        return true;
    }

    public Soul(Vector2 position)
    {
        _origin = position;
        Position = position;
        _stateTimer = GameBalance.SoulExposedDuration;
    }

    public void Update(float deltaTime, WardenField wardens, ParticleSystem particles)
    {
        _visualTime += deltaTime;

        switch (State)
        {
            case SoulState.Exposed:
                Position = _origin + new Vector2(0f, -24f + MathF.Sin(_visualTime * 3f) * 5f);
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f)
                {
                    State = SoulState.Releasing;
                    _stateTimer = GameBalance.SoulReleaseDuration;
                }
                break;

            case SoulState.Releasing:
                UpdateRelease(deltaTime, particles);
                break;

            case SoulState.Residue:
                UpdateResidue(deltaTime, wardens, particles);
                break;
        }
    }

    public void BeginDevour()
    {
        if (State is SoulState.Exposed or SoulState.Releasing)
        {
            State = SoulState.BeingDevoured;
        }
    }

    public void PullToward(Vector2 target, float deltaTime)
    {
        if (State != SoulState.BeingDevoured)
        {
            return;
        }

        float pull = 1f - MathF.Exp(-deltaTime * 4.8f);
        Position = Vector2.Lerp(Position, target, pull);
    }

    public void CancelDevour()
    {
        if (State != SoulState.BeingDevoured)
        {
            return;
        }

        _origin = Position + new Vector2(0f, 24f);
        State = SoulState.Exposed;
        _stateTimer = GameBalance.SoulExposedDuration;
    }

    public void Consume()
    {
        if (State == SoulState.BeingDevoured)
        {
            State = SoulState.Consumed;
        }
    }

    public void Expel(Vector2 position)
    {
        Position = position;
        _origin = position;
        State = SoulState.Exposed;
        _stateTimer = GameBalance.SoulExposedDuration;
        _releaseBurstCreated = false;
    }

    public void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        if (State is SoulState.Released or SoulState.Consumed)
        {
            return;
        }

        if (State == SoulState.Residue || !useSpriteArt)
        {
            // Residue and the primitive fallback are light only; see DrawCombatLight.
            return;
        }
    }

    /// <summary>
    /// The Soul's own light. Painted softly so a Soul always reads as something
    /// alive and fragile rather than as a marked object.
    /// </summary>
    public void DrawCombatLight(SpriteBatch batch, Texture2D brush, bool soulSenseActive)
    {
        if (State is SoulState.Released or SoulState.Consumed)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 5f);
        float emphasis = soulSenseActive ? 1.25f : 1f;

        if (State == SoulState.Residue)
        {
            SoftShapes.Blob(batch, brush, Position, 18f, GameBalance.DeathFlameBright * 0.26f);
            SoftShapes.Blob(batch, brush, Position, 6f, GameBalance.SoulWhite * 0.5f);
            return;
        }

        float releaseProgress = ReleaseProgress;
        Color glow = Color.Lerp(GameBalance.DeathFlame, GameBalance.SoulWhite, releaseProgress);

        SoftShapes.Blob(batch, brush, Position, (30f + pulse * 5f) * emphasis, GameBalance.DeepViolet * 0.24f);
        SoftShapes.Blob(batch, brush, Position, (15f + pulse * 2f) * emphasis, glow * 0.3f);
        SoftShapes.Blob(batch, brush, Position, 6f * emphasis, GameBalance.SoulWhite * 0.42f);

        if (State == SoulState.BeingDevoured)
        {
            // Being pulled apart: the light strains outward and flickers.
            float strain = 0.5f + 0.5f * MathF.Sin(_visualTime * 19f);
            SoftShapes.Ring(batch, brush, Position, 26f + pulse * 5f, 10f,
                GameBalance.DeathFlameBright * (0.2f + strain * 0.2f), 14, _visualTime * 4f);
        }
        else if (State == SoulState.Releasing)
        {
            // The intact Soul departs freely. Only the later residue returns; a
            // tether to the Player would falsely imply Soul consumption.
            float departure = 1f - releaseProgress;
            SoftShapes.Blob(batch, brush, Position, 34f + releaseProgress * 44f, glow * (0.16f * departure));
        }
    }

    private void UpdateRelease(float deltaTime, ParticleSystem particles)
    {
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        float progress = 1f - _stateTimer / GameBalance.SoulReleaseDuration;
        Position = _origin + new Vector2(
            MathF.Sin(_visualTime * 2.4f) * 4f,
            -24f - progress * 42f);

        if (!_releaseBurstCreated && progress >= 0.68f)
        {
            _releaseBurstCreated = true;
            particles.EmitDeathFlame(Position, 14, 0.72f);
        }

        if (_stateTimer <= 0f)
        {
            particles.EmitBurst(Position, -Vector2.UnitY, 14, GameBalance.SoulWhite, 92f, 5f);
            particles.EmitSoulRelease(Position);
            State = SoulState.Residue;
            _stateTimer = GameBalance.SoulResidueTravelTime;
        }
    }

    /// <summary>
    /// What the Soul leaves behind returns to the nearest Warden who can still
    /// carry it. Ownership is deliberately not contested: the residue feeds the
    /// brothers' shared Resonance, so there is no pickup to race for.
    /// </summary>
    private void UpdateResidue(float deltaTime, WardenField wardens, ParticleSystem particles)
    {
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        Player receiver = wardens.ClosestStanding(Position);
        if (receiver is null)
        {
            // Nobody left standing to receive it. The residue simply disperses.
            if (_stateTimer <= 0f)
            {
                State = SoulState.Released;
            }
            return;
        }

        Vector2 target = receiver.Position + receiver.FacingDirection * 2f;
        float follow = 1f - MathF.Exp(-deltaTime * 8.5f);
        Position = Vector2.Lerp(Position, target, follow);

        if (Vector2.DistanceSquared(Position, target) <= 13f * 13f || _stateTimer <= 0f)
        {
            Position = target;
            particles.EmitDeathFlame(target, 10, 0.82f);
            ResidueReceiver = receiver;
            State = SoulState.Released;
        }
    }
}
