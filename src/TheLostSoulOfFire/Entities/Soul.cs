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
    public bool CanBeDevoured => State is SoulState.Exposed or SoulState.Releasing or SoulState.BeingDevoured;

    public Soul(Vector2 position)
    {
        _origin = position;
        Position = position;
        _stateTimer = GameBalance.SoulExposedDuration;
    }

    public void Update(float deltaTime, Player player, ParticleSystem particles)
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
                UpdateResidue(deltaTime, player, particles);
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
        Player player,
        bool soulSenseActive,
        bool useSpriteArt,
        Texture2D? softSpot = null)
    {
        if (State is SoulState.Released or SoulState.Consumed)
        {
            return;
        }

        if (State == SoulState.Residue)
        {
            if (softSpot is not null)
            {
                // The rendered game draws the released piece as a small soul (ArtAssets.DrawLostSoul).
                return;
            }
            Vector2 target = player.Position + player.FacingDirection * 2f;
            float arriving = useSpriteArt ? MathHelper.Clamp(1f - Vector2.Distance(Position, target) / 160f, 0f, 1f) : 0f;
            Vector2 drawn = Position - new Vector2(0f, FigureHeights.Core * arriving);
            WorldMarks.Beam(batch, pixel, drawn - new Vector2(9f, 0f), drawn + new Vector2(9f, 0f), 12f, GameBalance.DeathFlameBright * 0.8f);
            WorldMarks.Ring(batch, pixel, drawn, 6f, GameBalance.SoulWhite, true, 3f);
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 5f);
        float releaseProgress = State == SoulState.Releasing
            ? 1f - MathHelper.Clamp(_stateTimer / GameBalance.SoulReleaseDuration, 0f, 1f)
            : 0f;
        Color glow = Color.Lerp(GameBalance.DeathFlame, GameBalance.SoulWhite, releaseProgress);
        float emphasis = soulSenseActive ? 1.25f : 1f;

        if (!useSpriteArt)
        {
            batch.FillCircle(pixel, Position, (16f + pulse * 2f) * emphasis, GameBalance.DeepViolet * 0.54f);
            batch.FillCircle(pixel, Position, (10f + pulse) * emphasis, glow * 0.92f);
            batch.FillCircle(pixel, Position, 4f * emphasis, GameBalance.SoulWhite);
        }

        if (State == SoulState.BeingDevoured)
        {
            if (softSpot is not null)
            {
                // Pulled away, the soul flickers in its own light; no ring (the Devourer draws its light pouring in).
                float struggle = 0.5f + 0.5f * MathF.Sin(_visualTime * 17f);
                WorldMarks.Glow(batch, softSpot, Position, 22f + struggle * 6f, GameBalance.DeathFlameBright * (0.3f + struggle * 0.2f));
            }
            else
            {
                WorldMarks.Ring(batch, pixel, Position, 25f + pulse * 5f, GameBalance.DeathFlameBright * 0.85f, true, 4f);
            }
        }

        if (State == SoulState.Releasing)
        {
            Vector2 playerCore = player.Position + player.FacingDirection * 2f - (useSpriteArt ? new Vector2(0f, FigureHeights.Core) : Vector2.Zero);
            if (softSpot is not null)
            {
                // The soul lets go where it is (release flipbook, glow); what remains of it then
                // flies to the player as a small soul. No tether, no ring.
                return;
            }
            WorldMarks.Beam(batch, pixel, playerCore, Position, 18f, GameBalance.DeathFlame * (0.42f + releaseProgress * 0.3f));
            WorldMarks.Beam(batch, pixel, playerCore, Position, 7f, GameBalance.DeathFlameBright * (0.45f + releaseProgress * 0.4f));
            WorldMarks.Ring(batch, pixel, Position, 22f + releaseProgress * 18f, glow * (1f - releaseProgress) * 0.7f, false, 3f);
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

    private void UpdateResidue(float deltaTime, Player player, ParticleSystem particles)
    {
        _stateTimer = MathF.Max(0f, _stateTimer - deltaTime);
        Vector2 target = player.Position + player.FacingDirection * 2f;
        float follow = 1f - MathF.Exp(-deltaTime * 8.5f);
        Position = Vector2.Lerp(Position, target, follow);

        if (Vector2.DistanceSquared(Position, target) <= 13f * 13f || _stateTimer <= 0f)
        {
            Position = target;
            particles.EmitDeathFlame(target, 10, 0.82f);
            player.AddResonance(GameBalance.ResonancePerSoulRelease);
            State = SoulState.Released;
        }
    }
}
