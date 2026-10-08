using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Effects;

/// <summary>
/// Camera reactions and screen feedback (presentation only, apart from the hitstop the game
/// already used). Shake is smooth noise, not a fresh random offset each frame, so a strong hit
/// rocks the picture instead of making it buzz; a kick is a damped spring that pushes the view
/// and lets it swing back; a zoom punch leans in for a moment on the heaviest blows. Flashes
/// can be placed in the world, so light bursts from where something happened instead of
/// washing the whole screen.
/// </summary>
public sealed class ScreenEffects
{
    /// <summary>How fast the shake noise moves (cycles per second): a rock, not a buzz.</summary>
    private const float ShakeFrequency = 17f;

    /// <summary>Kick spring: angular frequency (rad/s) and damping ratio (slightly underdamped).</summary>
    private const float KickOmega = 21f;
    private const float KickDamping = 0.72f;
    private const float MaxKick = 18f;

    private const float ZoomOmega = 16f;
    private const float ZoomDamping = 0.8f;
    private const float MaxZoomPunch = 0.035f;

    private float _shakeTimer;
    private float _shakeDuration;
    private float _shakeMagnitude;
    private float _shakeTime;
    private Vector2 _shakeOffset;
    private Vector2 _kick;
    private Vector2 _kickVelocity;
    private float _zoom;
    private float _zoomVelocity;
    private float _hitstopTimer;
    private float _flashTimer;
    private float _flashDuration;
    private float _flashStrength;
    private Vector2? _flashCenter;
    private float _impactFrameTimer;
    private float _impactFrameDuration;

    public float MotionScale { get; set; } = 1f;

    public Vector2 ShakeOffset => _shakeOffset * MotionScale;
    public Vector2 CameraOffset => ShakeOffset + _kick * MotionScale;

    /// <summary>Extra zoom on top of the camera's own (0 = none, 0.02 = two percent closer).</summary>
    public float ZoomPunch => _zoom * MotionScale;
    public bool IsHitStopped => _hitstopTimer > 0f;
    public float FlashAlpha => _flashDuration <= 0f
        ? 0f
        : _flashStrength * MathHelper.Clamp(_flashTimer / _flashDuration, 0f, 1f);

    /// <summary>Where the current flash bursts from in the world, or null for an even wash.</summary>
    public Vector2? FlashCenter => FlashAlpha > 0f ? _flashCenter : null;

    public float ImpactFrameAlpha => _impactFrameDuration <= 0f
        ? 0f
        : MathHelper.Clamp(_impactFrameTimer / _impactFrameDuration, 0f, 1f);

    public void AddShake(float duration, float magnitude)
    {
        _shakeTimer = MathF.Max(_shakeTimer, duration);
        _shakeDuration = MathF.Max(_shakeDuration, duration);
        _shakeMagnitude = MathF.Max(_shakeMagnitude, magnitude);
    }

    /// <summary>Pushes the view along <paramref name="direction"/>; it peaks near <paramref name="magnitude"/> and swings back.</summary>
    public void AddCameraKick(Vector2 direction, float magnitude)
    {
        if (direction.LengthSquared() <= 0.001f || magnitude <= 0f)
        {
            return;
        }

        // An underdamped spring started with this velocity peaks at about magnitude.
        _kickVelocity += Vector2.Normalize(direction) * magnitude * KickOmega * 1.75f;
    }

    /// <summary>Leans the view in by <paramref name="amount"/> (a share of the zoom) for a moment.</summary>
    public void AddZoomPunch(float amount)
    {
        if (amount > 0f)
        {
            _zoomVelocity += amount * ZoomOmega * 1.9f;
        }
    }

    public void BeginHitstop(float duration)
    {
        _hitstopTimer = MathF.Max(_hitstopTimer, duration);
    }

    public void Flash(float duration, float strength)
    {
        if (strength >= _flashStrength || FlashAlpha <= 0f)
        {
            _flashCenter = null;
        }
        _flashTimer = MathF.Max(_flashTimer, duration);
        _flashDuration = MathF.Max(_flashDuration, duration);
        _flashStrength = MathF.Max(_flashStrength, strength);
    }

    /// <summary>A flash that bursts from <paramref name="worldPosition"/> and fades toward the edges.</summary>
    public void FlashAt(Vector2 worldPosition, float duration, float strength)
    {
        bool takesOver = strength >= _flashStrength || FlashAlpha <= 0f;
        Flash(duration, strength);
        if (takesOver)
        {
            _flashCenter = worldPosition;
        }
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
            _flashCenter = null;
        }

        _shakeTime += deltaTime;
        _shakeTimer = MathF.Max(0f, _shakeTimer - deltaTime);
        if (_shakeTimer <= 0f)
        {
            _shakeOffset = Vector2.Zero;
            _shakeDuration = 0f;
            _shakeMagnitude = 0f;
        }
        else
        {
            float decay = _shakeDuration <= 0f
                ? 0f
                : MathF.Pow(MathHelper.Clamp(_shakeTimer / _shakeDuration, 0f, 1f), 1.6f);
            float phase = _shakeTime * ShakeFrequency;
            _shakeOffset = new Vector2(SmoothNoise(phase + 11.3f), SmoothNoise(phase * 0.93f + 57.1f)) * (_shakeMagnitude * decay);
        }

        // Springs are stepped in small pieces so a long frame cannot make them explode.
        float remaining = MathF.Max(0f, deltaTime);
        while (remaining > 0f)
        {
            float step = MathF.Min(remaining, 1f / 240f);
            remaining -= step;
            _kickVelocity += (-KickOmega * KickOmega * _kick - 2f * KickDamping * KickOmega * _kickVelocity) * step;
            _kick += _kickVelocity * step;
            _zoomVelocity += (-ZoomOmega * ZoomOmega * _zoom - 2f * ZoomDamping * ZoomOmega * _zoomVelocity) * step;
            _zoom += _zoomVelocity * step;
        }

        if (_kick.LengthSquared() > MaxKick * MaxKick)
        {
            _kick = Vector2.Normalize(_kick) * MaxKick;
            _kickVelocity *= 0.5f;
        }
        if (_kick.LengthSquared() < 0.0004f && _kickVelocity.LengthSquared() < 0.01f)
        {
            _kick = Vector2.Zero;
            _kickVelocity = Vector2.Zero;
        }
        _zoom = MathHelper.Clamp(_zoom, -MaxZoomPunch, MaxZoomPunch);
        if (MathF.Abs(_zoom) < 0.00005f && MathF.Abs(_zoomVelocity) < 0.0005f)
        {
            _zoom = 0f;
            _zoomVelocity = 0f;
        }
    }

    public void Clear()
    {
        _shakeTimer = 0f;
        _shakeDuration = 0f;
        _shakeMagnitude = 0f;
        _kick = Vector2.Zero;
        _kickVelocity = Vector2.Zero;
        _zoom = 0f;
        _zoomVelocity = 0f;
        _hitstopTimer = 0f;
        _flashTimer = 0f;
        _flashDuration = 0f;
        _flashStrength = 0f;
        _flashCenter = null;
        _impactFrameTimer = 0f;
        _impactFrameDuration = 0f;
        _shakeOffset = Vector2.Zero;
    }

    /// <summary>Smooth value noise in −1…1: random heights on whole numbers, eased in between.</summary>
    private static float SmoothNoise(float x)
    {
        float floor = MathF.Floor(x);
        float t = x - floor;
        t = t * t * (3f - 2f * t);
        return MathHelper.Lerp(Hash(floor), Hash(floor + 1f), t);
    }

    private static float Hash(float n)
    {
        uint value = (uint)(int)n * 747796405u + 2891336453u;
        value = ((value >> (int)((value >> 28) + 4u)) ^ value) * 277803737u;
        value = (value >> 22) ^ value;
        return value / (float)uint.MaxValue * 2f - 1f;
    }
}
