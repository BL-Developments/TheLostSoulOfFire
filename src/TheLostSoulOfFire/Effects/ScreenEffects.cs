using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Effects;

public sealed class ScreenEffects
{
    private readonly Random _random = new(947);
    private float _shakeTimer;
    private float _shakeMagnitude;
    private float _hitstopTimer;
    private float _flashTimer;
    private float _flashDuration;
    private float _flashStrength;
    private float _impactFrameTimer;
    private float _impactFrameDuration;

    public Vector2 ShakeOffset { get; private set; }
    public bool IsHitStopped => _hitstopTimer > 0f;
    public float FlashAlpha => _flashDuration <= 0f
        ? 0f
        : _flashStrength * MathHelper.Clamp(_flashTimer / _flashDuration, 0f, 1f);
    public float ImpactFrameAlpha => _impactFrameDuration <= 0f
        ? 0f
        : MathHelper.Clamp(_impactFrameTimer / _impactFrameDuration, 0f, 1f);

    public void AddShake(float duration, float magnitude)
    {
        _shakeTimer = MathF.Max(_shakeTimer, duration);
        _shakeMagnitude = MathF.Max(_shakeMagnitude, magnitude);
    }

    public void BeginHitstop(float duration)
    {
        _hitstopTimer = MathF.Max(_hitstopTimer, duration);
    }

    public void Flash(float duration, float strength)
    {
        _flashTimer = MathF.Max(_flashTimer, duration);
        _flashDuration = MathF.Max(_flashDuration, duration);
        _flashStrength = MathF.Max(_flashStrength, strength);
    }

    public void BeginImpactFrame(float duration)
    {
        _impactFrameTimer = MathF.Max(_impactFrameTimer, duration);
        _impactFrameDuration = MathF.Max(_impactFrameDuration, duration);
    }

    public void Update(float deltaTime)
    {
        _hitstopTimer = MathF.Max(0f, _hitstopTimer - deltaTime);
        _flashTimer = MathF.Max(0f, _flashTimer - deltaTime);
        _impactFrameTimer = MathF.Max(0f, _impactFrameTimer - deltaTime);
        if (_impactFrameTimer <= 0f)
        {
            _impactFrameDuration = 0f;
        }
        if (_flashTimer <= 0f)
        {
            _flashDuration = 0f;
            _flashStrength = 0f;
        }

        _shakeTimer = MathF.Max(0f, _shakeTimer - deltaTime);
        if (_shakeTimer <= 0f)
        {
            ShakeOffset = Vector2.Zero;
            _shakeMagnitude = 0f;
        }
        else
        {
            ShakeOffset = new Vector2(
                (float)(_random.NextDouble() * 2d - 1d),
                (float)(_random.NextDouble() * 2d - 1d)) * _shakeMagnitude;
        }
    }
}
