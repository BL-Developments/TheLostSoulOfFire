using System;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public sealed class ScreenEffects
{
    private readonly Random _random = new(947);
    private readonly PresentationSettings _settings;
    private float _shakeTimer;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private Vector2 _cameraKick;
    private float _hitstopTimer;
    private float _flashTimer;
    private float _flashDuration;
    private float _flashStrength;
    private Color _flashColor = GameBalance.DeathFlameBright;
    private float _impactFrameTimer;
    private float _impactFrameDuration;
    private float _zoomPunch;
    private float _zoomPunchVelocity;

    /// <summary>
    /// A short inward push of the frame on impact, released as a spring.
    /// Shake says "something happened"; a punch says "that landed". Bounded and
    /// scaled by the same accessibility setting as every other camera motion.
    /// </summary>
    public float ZoomPunch => _zoomPunch;

    public Vector2 ShakeOffset { get; private set; }
    public Vector2 CameraOffset => ShakeOffset + _cameraKick;
    public bool IsHitStopped => _hitstopTimer > 0f;
    public float FlashAlpha => _flashDuration <= 0f
        ? 0f
        : _flashStrength * MathHelper.Clamp(_flashTimer / _flashDuration, 0f, 1f);
    public float ImpactFrameAlpha => _impactFrameDuration <= 0f
        ? 0f
        : MathHelper.Clamp(_impactFrameTimer / _impactFrameDuration, 0f, 1f);
    public Color FlashColor => _flashColor;

    public ScreenEffects(PresentationSettings settings)
    {
        _settings = settings;
    }

    public void AddShake(float duration, float magnitude)
    {
        magnitude *= _settings.CameraMotionScale;
        if (magnitude <= 0f)
        {
            return;
        }

        _shakeTimer = MathF.Max(_shakeTimer, duration);
        _shakeDuration = MathF.Max(_shakeDuration, duration);
        _shakeMagnitude = MathF.Max(_shakeMagnitude, magnitude);
    }

    /// <summary>Weight behind a hit, 0..1. Only the strongest punch in flight survives.</summary>
    public void AddZoomPunch(float strength)
    {
        strength *= _settings.CameraMotionScale;
        if (strength <= 0f)
        {
            return;
        }

        _zoomPunch = MathF.Min(0.075f, MathF.Max(_zoomPunch, strength * 0.075f));
    }

    public void AddCameraKick(Vector2 direction, float magnitude)
    {
        magnitude *= _settings.CameraMotionScale;
        if (direction.LengthSquared() <= 0.001f || magnitude <= 0f)
        {
            return;
        }

        _cameraKick += Vector2.Normalize(direction) * magnitude;
        if (_cameraKick.LengthSquared() > 18f * 18f)
        {
            _cameraKick = Vector2.Normalize(_cameraKick) * 18f;
        }
    }

    public void BeginHitstop(float duration)
    {
        _hitstopTimer = MathF.Max(_hitstopTimer, duration);
    }

    public void Flash(float duration, float strength, Color? color = null)
    {
        strength *= _settings.FlashScale;
        if (strength <= 0f)
        {
            return;
        }

        _flashTimer = MathF.Max(_flashTimer, duration);
        _flashDuration = MathF.Max(_flashDuration, duration);
        _flashStrength = MathF.Max(_flashStrength, strength);
        if (color.HasValue)
        {
            _flashColor = color.Value;
        }
    }

    public void BeginImpactFrame(float duration)
    {
        if (_settings.FlashScale <= 0f)
        {
            return;
        }

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
            _flashColor = GameBalance.DeathFlameBright;
        }

        // Spring the punch back out. Slightly under-damped, so the frame
        // breathes out rather than sliding back.
        if (_zoomPunch > 0.0001f || MathF.Abs(_zoomPunchVelocity) > 0.0001f)
        {
            _zoomPunchVelocity += -_zoomPunch * 320f * deltaTime;
            _zoomPunchVelocity *= MathF.Pow(0.0009f, deltaTime);
            _zoomPunch = MathF.Max(0f, _zoomPunch + _zoomPunchVelocity * deltaTime);
        }
        else
        {
            _zoomPunch = 0f;
            _zoomPunchVelocity = 0f;
        }

        _shakeTimer = MathF.Max(0f, _shakeTimer - deltaTime);
        if (_shakeTimer <= 0f)
        {
            ShakeOffset = Vector2.Zero;
            _shakeDuration = 0f;
            _shakeMagnitude = 0f;
        }
        else
        {
            float decay = _shakeDuration <= 0f
                ? 0f
                : MathF.Pow(MathHelper.Clamp(_shakeTimer / _shakeDuration, 0f, 1f), 1.6f);
            ShakeOffset = new Vector2(
                (float)(_random.NextDouble() * 2d - 1d),
                (float)(_random.NextDouble() * 2d - 1d)) * (_shakeMagnitude * decay);
        }

        _cameraKick *= MathF.Exp(-deltaTime * 18f);
        if (_cameraKick.LengthSquared() < 0.01f)
        {
            _cameraKick = Vector2.Zero;
        }
    }

    public void Clear()
    {
        _shakeTimer = 0f;
        _shakeDuration = 0f;
        _shakeMagnitude = 0f;
        _cameraKick = Vector2.Zero;
        _hitstopTimer = 0f;
        _flashTimer = 0f;
        _flashDuration = 0f;
        _flashStrength = 0f;
        _flashColor = GameBalance.DeathFlameBright;
        _impactFrameTimer = 0f;
        _impactFrameDuration = 0f;
        ShakeOffset = Vector2.Zero;
    }
}
