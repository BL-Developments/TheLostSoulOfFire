using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Combat;

public readonly record struct ScytheStrike(
    int Step,
    int Damage,
    float Range,
    float ArcRadians,
    float Knockback,
    Vector2 Direction,
    bool IsSeverance = false);

public sealed class ScytheCombat
{
    private float _attackElapsed;
    private float _attackDuration;
    private float _strikeTime;
    private float _comboTimer;
    private bool _strikeCreated;
    private bool _strikePending;
    private bool _queuedAttack;
    private int _nextStep = 1;
    private Vector2 _attackDirection = Vector2.UnitX;
    private bool _resonanceActive;
    private bool _severanceArmed;
    private bool _severanceRequested;

    public int ActiveStep { get; private set; }
    public bool StartedThisFrame { get; private set; }
    public Vector2 AttackDirection => _attackDirection;

    /// <summary>
    /// True while the swing that is currently in flight was started inside a
    /// Severance Window. The window is claimed at the start of the swing, so a
    /// late window expiry cannot silently downgrade a committed cut.
    /// </summary>
    public bool SeveranceArmed => _severanceArmed;

    /// <summary>
    /// Set for the single frame on which an armed swing began, so the caller can
    /// close the Player's window exactly once.
    /// </summary>
    public bool ConsumedSeveranceThisFrame { get; private set; }
    public float NormalizedProgress => ActiveStep == 0 ? 0f : MathHelper.Clamp(_attackElapsed / _attackDuration, 0f, 1f);
    public string StateLabel => ActiveStep == 0
        ? _comboTimer > 0f ? $"CHAIN {_nextStep}" : "READY"
        : _severanceArmed ? $"SEVER {ActiveStep}" : $"HIT {ActiveStep}";

    public void Reset()
    {
        _attackElapsed = 0f;
        _attackDuration = 0f;
        _comboTimer = 0f;
        _strikeCreated = false;
        _strikePending = false;
        _queuedAttack = false;
        _nextStep = 1;
        _severanceArmed = false;
        _severanceRequested = false;
        ConsumedSeveranceThisFrame = false;
        ActiveStep = 0;
    }

    public void Update(
        float deltaTime,
        PlayerCommand command,
        Vector2 facingDirection,
        Vector2 playerPosition,
        ParticleSystem particles,
        bool canStartAttack,
        bool resonanceActive,
        bool severanceReady = false)
    {
        StartedThisFrame = false;
        ConsumedSeveranceThisFrame = false;
        _resonanceActive = resonanceActive;
        _severanceRequested = severanceReady;

        if (ActiveStep == 0)
        {
            _comboTimer = MathF.Max(0f, _comboTimer - deltaTime);
            if (_comboTimer <= 0f)
            {
                _nextStep = 1;
            }

            if (canStartAttack && (command.ScythePressed || _queuedAttack))
            {
                _queuedAttack = false;
                StartAttack(facingDirection, playerPosition, particles);
            }

            return;
        }

        if (command.ScythePressed && _attackElapsed > 0.055f)
        {
            _queuedAttack = true;
        }

        _attackElapsed += deltaTime;
        if (!_strikeCreated && _attackElapsed >= _strikeTime)
        {
            _strikeCreated = true;
            _strikePending = true;
        }

        if (_attackElapsed < _attackDuration)
        {
            return;
        }

        ActiveStep = 0;
        _comboTimer = GameBalance.ComboResetTime;
        if (canStartAttack && _queuedAttack)
        {
            _queuedAttack = false;
            StartAttack(facingDirection, playerPosition, particles);
        }
    }

    public bool TryConsumeStrike(out ScytheStrike strike)
    {
        if (!_strikePending)
        {
            strike = default;
            return false;
        }

        _strikePending = false;
        strike = BuildStrike(ActiveStep, _attackDirection, _resonanceActive, _severanceArmed);
        return true;
    }

    public float GetForwardImpulse()
    {
        float impulse = ActiveStep switch
        {
            1 => 105f,
            2 => 132f,
            3 => 225f,
            _ => 0f
        };
        return impulse * (_resonanceActive ? 1.18f : 1f);
    }

    /// <summary>
    /// Draws the physical weapon only. The swing's light lives in the additive
    /// combat-light pass so it can be feathered instead of stroked.
    /// </summary>
    public void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D physicalScythe,
        Vector2 playerPosition,
        Vector2 facingDirection,
        bool debugVisible)
    {
        if (ActiveStep == 0)
        {
            DrawRestingScythe(batch, physicalScythe, playerPosition, facingDirection);
            return;
        }

        DrawAttackingScythe(batch, pixel, physicalScythe, playerPosition, debugVisible);
    }

    private void StartAttack(Vector2 facingDirection, Vector2 playerPosition, ParticleSystem particles)
    {
        ActiveStep = _nextStep;
        _severanceArmed = _severanceRequested;
        ConsumedSeveranceThisFrame = _severanceArmed;
        // A Severance cut is its own beat. It never advances the ordinary chain,
        // so the combo stays a rhythm the Player owns rather than a lottery.
        _nextStep = _severanceArmed ? 1 : ActiveStep == 3 ? 1 : ActiveStep + 1;
        _attackDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;
        _attackElapsed = 0f;
        _strikeCreated = false;
        StartedThisFrame = true;

        (_attackDuration, _strikeTime) = _severanceArmed
            ? (0.38f, 0.088f)
            : ActiveStep switch
            {
                1 => (0.205f, 0.062f),
                2 => (0.255f, 0.085f),
                _ => (0.42f, 0.155f)
            };

        Color flame = _severanceArmed || ActiveStep == 3 ? GameBalance.DeathFlameBright : GameBalance.DeathFlame;
        int ignitionParticles = _severanceArmed ? 12 : ActiveStep switch { 1 => 2, 2 => 4, _ => 8 };
        particles.EmitBurst(
            playerPosition + _attackDirection * 42f,
            _attackDirection,
            ignitionParticles,
            flame,
            _severanceArmed ? 150f : ActiveStep == 3 ? 115f : 60f,
            _severanceArmed ? 7f : ActiveStep == 3 ? 6f : 3f);
    }

    private static ScytheStrike BuildStrike(int step, Vector2 direction, bool resonanceActive, bool severanceArmed)
    {
        ScytheStrike strike = step switch
        {
            1 => new ScytheStrike(1, GameBalance.ScytheDamage1, GameBalance.ScytheRange1, MathHelper.ToRadians(120f), 170f, direction),
            2 => new ScytheStrike(2, GameBalance.ScytheDamage2, GameBalance.ScytheRange2, MathHelper.ToRadians(140f), 220f, direction),
            _ => new ScytheStrike(3, GameBalance.ScytheDamage3, GameBalance.ScytheRange3, MathHelper.ToRadians(198f), 410f, direction)
        };

        if (resonanceActive)
        {
            strike = strike with
            {
                Damage = (int)MathF.Round(strike.Damage * GameBalance.ResonanceScytheDamageMultiplier),
                Range = strike.Range * GameBalance.ResonanceScytheRangeMultiplier,
                Knockback = strike.Knockback * GameBalance.ResonanceScytheKnockbackMultiplier
            };
        }

        if (!severanceArmed)
        {
            return strike;
        }

        // The cut reaches around the Anchor rather than in front of the Player.
        return strike with
        {
            Damage = (int)MathF.Round(strike.Damage * GameBalance.SeveranceDamageMultiplier),
            Range = strike.Range * GameBalance.SeveranceRangeMultiplier,
            ArcRadians = MathF.Max(strike.ArcRadians, GameBalance.SeveranceArcRadians),
            Knockback = strike.Knockback * GameBalance.SeveranceKnockbackMultiplier,
            IsSeverance = true
        };
    }

    private static void DrawRestingScythe(
        SpriteBatch batch,
        Texture2D physicalScythe,
        Vector2 playerPosition,
        Vector2 facingDirection)
    {
        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        float rotation = MathF.Atan2(facingDirection.Y, facingDirection.X) + 0.35f;
        batch.Draw(
            physicalScythe,
            playerPosition + facingDirection * 12f + right * 8f,
            null,
            Color.White,
            rotation,
            new Vector2(148f, 158f),
            0.46f,
            SpriteEffects.None,
            0f);
    }

    /// <summary>
    /// Swing geometry shared by the weapon sprite and the light trail, so both
    /// always describe the same arc.
    /// </summary>
    private readonly record struct SwingArc(
        float Start,
        float Sweep,
        float Current,
        float Radius,
        float Thickness,
        float TrailAlpha,
        Color Trail);

    private SwingArc BuildSwingArc()
    {
        float aim = MathF.Atan2(_attackDirection.Y, _attackDirection.X);
        float attackProgress = NormalizedProgress;
        float swingProgress = ActiveStep == 3
            ? MathHelper.Clamp((attackProgress - 0.2f) / 0.58f, 0f, 1f)
            : attackProgress;
        float eased = 1f - MathF.Pow(1f - swingProgress, ActiveStep == 3 ? 2.35f : 3f);
        float totalArc = _severanceArmed
            ? MathHelper.ToRadians(268f)
            : ActiveStep switch
            {
                1 => MathHelper.ToRadians(120f),
                2 => -MathHelper.ToRadians(140f),
                _ => MathHelper.ToRadians(198f)
            };

        float start = aim - totalArc * 0.5f;
        float radius = ActiveStep switch { 1 => 88f, 2 => 99f, _ => 119f };
        float thickness = ActiveStep switch { 1 => 6f, 2 => 9f, _ => 15f };
        if (_resonanceActive)
        {
            radius *= GameBalance.ResonanceScytheRangeMultiplier;
            thickness *= 1.22f;
        }
        if (_severanceArmed)
        {
            radius *= GameBalance.SeveranceRangeMultiplier;
            thickness = MathF.Max(thickness, 14f) * 1.3f;
        }

        Color trail = _severanceArmed
            ? GameBalance.SoulWhite
            : ActiveStep switch
            {
                1 => GameBalance.DeathFlame,
                2 => GameBalance.DeathFlameBright,
                _ => GameBalance.DeathFlameBright
            };

        float fadeStart = ActiveStep == 3 ? 0.7f : 0.62f;
        float trailAlpha = 1f - MathHelper.Clamp((attackProgress - fadeStart) / (1f - fadeStart), 0f, 1f);
        float visibleSweep = totalArc * MathHelper.Clamp(eased, 0.08f, 1f);

        return new SwingArc(start, visibleSweep, start + totalArc * eased, radius, thickness, trailAlpha, trail);
    }

    /// <summary>
    /// The swing's light. Painted with the feathered brush in the additive combat
    /// pass: a sweep of Death Flame with falloff, never a stroked arc. The sprite
    /// VFX sheets spawned by CombatPresentation still carry the slash shape.
    /// </summary>
    public void DrawTrail(SpriteBatch batch, Texture2D brush, Vector2 playerPosition)
    {
        if (ActiveStep == 0)
        {
            return;
        }

        SwingArc arc = BuildSwingArc();
        if (arc.TrailAlpha <= 0.001f)
        {
            return;
        }

        int samples = _severanceArmed ? 26 : ActiveStep == 3 ? 22 : 16;
        SoftShapes.ArcBand(batch, brush, playerPosition, arc.Radius, arc.Start, arc.Sweep,
            arc.Thickness * 2.1f, GameBalance.DeepViolet * (0.34f * arc.TrailAlpha), samples);
        SoftShapes.ArcBand(batch, brush, playerPosition, arc.Radius, arc.Start, arc.Sweep,
            arc.Thickness, arc.Trail * (0.3f * arc.TrailAlpha), samples);

        if (ActiveStep != 3 && !_severanceArmed)
        {
            return;
        }

        SoftShapes.ArcBand(batch, brush, playerPosition, arc.Radius, arc.Start, arc.Sweep,
            arc.Thickness * 0.42f, GameBalance.SoulWhite * (0.3f * arc.TrailAlpha), samples);

        // The leading edge of a heavy or severing cut carries a brighter head.
        Vector2 head = playerPosition + new Vector2(MathF.Cos(arc.Current), MathF.Sin(arc.Current)) * arc.Radius;
        SoftShapes.Blob(batch, brush, head, arc.Thickness * 1.4f, GameBalance.SoulWhite * (0.34f * arc.TrailAlpha));
    }

    private void DrawAttackingScythe(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D physicalScythe,
        Vector2 playerPosition,
        bool debugVisible)
    {
        SwingArc arc = BuildSwingArc();
        Vector2 bladeDirection = new(MathF.Cos(arc.Current), MathF.Sin(arc.Current));
        batch.Draw(
            physicalScythe,
            playerPosition + bladeDirection * 29f,
            null,
            Color.White,
            arc.Current + MathHelper.PiOver2,
            new Vector2(148f, 158f),
            _severanceArmed ? 0.74f : ActiveStep switch { 1 => 0.55f, 2 => 0.6f, _ => 0.7f },
            SpriteEffects.None,
            0f);

        if (debugVisible)
        {
            float aim = MathF.Atan2(_attackDirection.Y, _attackDirection.X);
            ScytheStrike strike = BuildStrike(ActiveStep, _attackDirection, _resonanceActive, _severanceArmed);
            batch.DrawArc(pixel, playerPosition, strike.Range, aim - strike.ArcRadians * 0.5f, strike.ArcRadians, new Color(80, 220, 210) * 0.65f, 2f, 28);
        }
    }
}
