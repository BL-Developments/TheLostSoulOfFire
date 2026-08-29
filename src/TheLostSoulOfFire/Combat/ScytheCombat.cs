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
    float Hitstop,
    Vector2 Direction);

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

    public int ActiveStep { get; private set; }
    public bool StartedThisFrame { get; private set; }
    public Vector2 AttackDirection => _attackDirection;
    public float NormalizedProgress => ActiveStep == 0 ? 0f : MathHelper.Clamp(_attackElapsed / _attackDuration, 0f, 1f);
    public string StateLabel => ActiveStep == 0 ? (_comboTimer > 0f ? $"CHAIN {_nextStep}" : "READY") : $"HIT {ActiveStep}";

    public void Reset()
    {
        _attackElapsed = 0f;
        _attackDuration = 0f;
        _comboTimer = 0f;
        _strikeCreated = false;
        _strikePending = false;
        _queuedAttack = false;
        _nextStep = 1;
        ActiveStep = 0;
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 facingDirection,
        Vector2 playerPosition,
        ParticleSystem particles,
        bool canStartAttack)
    {
        StartedThisFrame = false;

        if (ActiveStep == 0)
        {
            _comboTimer = MathF.Max(0f, _comboTimer - deltaTime);
            if (_comboTimer <= 0f)
            {
                _nextStep = 1;
            }

            if (canStartAttack && (input.WasLeftMousePressed || _queuedAttack))
            {
                _queuedAttack = false;
                StartAttack(facingDirection, playerPosition, particles);
            }

            return;
        }

        if (input.WasLeftMousePressed && _attackElapsed > 0.055f)
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
        strike = BuildStrike(ActiveStep, _attackDirection);
        return true;
    }

    public float GetForwardImpulse() => ActiveStep switch
    {
        1 => 105f,
        2 => 132f,
        3 => 225f,
        _ => 0f
    };

    public void Draw(SpriteBatch batch, Texture2D pixel, Vector2 playerPosition, Vector2 facingDirection, bool debugVisible)
    {
        if (ActiveStep == 0)
        {
            DrawRestingScythe(batch, pixel, playerPosition, facingDirection);
            return;
        }

        DrawAttackingScythe(batch, pixel, playerPosition, debugVisible);
    }

    private void StartAttack(Vector2 facingDirection, Vector2 playerPosition, ParticleSystem particles)
    {
        ActiveStep = _nextStep;
        _nextStep = ActiveStep == 3 ? 1 : ActiveStep + 1;
        _attackDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;
        _attackElapsed = 0f;
        _strikeCreated = false;
        StartedThisFrame = true;

        (_attackDuration, _strikeTime) = ActiveStep switch
        {
            1 => (0.225f, 0.075f),
            2 => (0.255f, 0.085f),
            _ => (0.405f, 0.145f)
        };

        Color flame = ActiveStep == 3 ? GameBalance.DeathFlameBright : GameBalance.DeathFlame;
        particles.EmitBurst(playerPosition + _attackDirection * 42f, _attackDirection, ActiveStep == 3 ? 10 : 4, flame, ActiveStep == 3 ? 105f : 60f, ActiveStep == 3 ? 6f : 3f);
    }

    private static ScytheStrike BuildStrike(int step, Vector2 direction) => step switch
    {
        1 => new ScytheStrike(1, GameBalance.ScytheDamage1, GameBalance.ScytheRange1, MathHelper.ToRadians(120f), 170f, 0.038f, direction),
        2 => new ScytheStrike(2, GameBalance.ScytheDamage2, GameBalance.ScytheRange2, MathHelper.ToRadians(140f), 220f, 0.048f, direction),
        _ => new ScytheStrike(3, GameBalance.ScytheDamage3, GameBalance.ScytheRange3, MathHelper.ToRadians(198f), 410f, 0.09f, direction)
    };

    private static void DrawRestingScythe(SpriteBatch batch, Texture2D pixel, Vector2 playerPosition, Vector2 facingDirection)
    {
        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        Vector2 handleStart = playerPosition - facingDirection * 31f - right * 15f;
        Vector2 handleEnd = playerPosition + facingDirection * 52f + right * 19f;
        Vector2 bladeTip = handleEnd + facingDirection * 18f + right * 26f;

        batch.DrawLine(pixel, handleStart, handleEnd, new Color(20, 18, 24), 7f);
        batch.DrawLine(pixel, handleStart, handleEnd, new Color(102, 98, 112), 2f);
        batch.DrawLine(pixel, handleEnd, bladeTip, new Color(151, 151, 161), 8f);
        batch.DrawLine(pixel, handleEnd + right * 8f, bladeTip + right * 8f, GameBalance.DeathFlame * 0.62f, 5f);
    }

    private void DrawAttackingScythe(SpriteBatch batch, Texture2D pixel, Vector2 playerPosition, bool debugVisible)
    {
        float aim = MathF.Atan2(_attackDirection.Y, _attackDirection.X);
        float eased = 1f - MathF.Pow(1f - NormalizedProgress, ActiveStep == 3 ? 2.2f : 3f);
        float totalArc = ActiveStep switch
        {
            1 => MathHelper.ToRadians(120f),
            2 => -MathHelper.ToRadians(140f),
            _ => MathHelper.ToRadians(198f)
        };
        float start = aim - totalArc * 0.5f;
        float current = start + totalArc * eased;
        float radius = ActiveStep switch { 1 => 90f, 2 => 98f, _ => 116f };
        float thickness = ActiveStep switch { 1 => 7f, 2 => 11f, _ => 21f };
        Color trail = ActiveStep switch
        {
            1 => GameBalance.DeathFlame * 0.62f,
            2 => GameBalance.DeathFlame * 0.82f,
            _ => GameBalance.DeathFlameBright * 0.92f
        };

        float visibleSweep = totalArc * MathHelper.Clamp(eased, 0.08f, 1f);
        batch.DrawArc(pixel, playerPosition, radius, start, visibleSweep, GameBalance.DeepViolet * 0.7f, thickness + 8f, ActiveStep == 3 ? 34 : 24);
        batch.DrawArc(pixel, playerPosition, radius, start, visibleSweep, trail, thickness, ActiveStep == 3 ? 34 : 24);
        if (ActiveStep == 3)
        {
            batch.DrawArc(pixel, playerPosition, radius + 3f, start, visibleSweep, GameBalance.SoulWhite * 0.72f, 5f, 34);
        }

        Vector2 bladeDirection = new(MathF.Cos(current), MathF.Sin(current));
        Vector2 tangent = new Vector2(-bladeDirection.Y, bladeDirection.X) * MathF.Sign(totalArc);
        Vector2 handleStart = playerPosition - bladeDirection * 28f;
        Vector2 handleEnd = playerPosition + bladeDirection * (radius - 13f);
        batch.DrawLine(pixel, handleStart, handleEnd, new Color(20, 18, 24), 8f);
        batch.DrawLine(pixel, handleStart, handleEnd, new Color(119, 112, 129), 2f);
        batch.DrawLine(pixel, handleEnd, handleEnd + tangent * (ActiveStep == 3 ? 48f : 36f), new Color(167, 165, 176), ActiveStep == 3 ? 10f : 8f);
        batch.DrawLine(pixel, handleEnd + tangent * 15f, handleEnd + tangent * (ActiveStep == 3 ? 58f : 43f), trail, ActiveStep == 3 ? 10f : 6f);

        if (debugVisible)
        {
            ScytheStrike strike = BuildStrike(ActiveStep, _attackDirection);
            batch.DrawArc(pixel, playerPosition, strike.Range, aim - strike.ArcRadians * 0.5f, strike.ArcRadians, new Color(80, 220, 210) * 0.65f, 2f, 28);
        }
    }
}
