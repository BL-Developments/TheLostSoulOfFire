using System;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// When the bound soul runs low it throbs: a double beat (as in the <c>soul_throb</c> takes) that
/// quickens as health falls, heard from within and seen as the edges of the world closing in and
/// the health bar beating along. Presentation only; nothing here changes play.
/// </summary>
public sealed class LowHealthPresentation
{
    /// <summary>Health share at or below which the soul throbs; the HUD's low state uses the same.</summary>
    public const float Threshold = 0.3f;

    /// <summary>Seconds between the two beats of a throb, as recorded in the takes.</summary>
    public const float SecondBeat = 0.24f;

    /// <summary>The first throb waits a moment, so it never lands on the hurt sound that caused it.</summary>
    private const float FirstBeatDelay = 0.45f;

    private float _untilBeat = -1f;
    private float _sinceBeat = 10f;

    /// <summary>How strongly the low state shows (0 off, about 0.55 at the threshold, 1 near death), eased.</summary>
    public float Amount { get; private set; }

    /// <summary>The throb's envelope: 1 on a beat, falling off, a smaller rise on the second beat.</summary>
    public float Pulse { get; private set; }

    /// <summary>True on the frame a throb starts (the cue is played then).</summary>
    public bool BeatStarted { get; private set; }

    public void Update(float deltaTime, float healthFraction, bool active)
    {
        BeatStarted = false;
        bool low = active && healthFraction > 0f && healthFraction <= Threshold;
        float depth = low ? 1f - healthFraction / Threshold : 0f;
        float target = low ? MathHelper.Lerp(0.55f, 1f, depth) : 0f;
        float rate = target > Amount ? 2.5f : 1.4f;
        Amount = target > Amount ? MathF.Min(target, Amount + rate * deltaTime) : MathF.Max(target, Amount - rate * deltaTime);

        _sinceBeat += deltaTime;
        if (!low)
        {
            _untilBeat = -1f;
        }
        else
        {
            if (_untilBeat < 0f)
            {
                _untilBeat = FirstBeatDelay;
            }

            _untilBeat -= deltaTime;
            if (_untilBeat <= 0f)
            {
                BeatStarted = true;
                _sinceBeat = 0f;
                // About 58 beats a minute at the threshold, 78 near death.
                _untilBeat += MathHelper.Lerp(1.04f, 0.77f, depth);
            }
        }

        Pulse = Envelope(_sinceBeat) * MathHelper.Clamp(Amount / 0.55f, 0f, 1f);
    }

    private static float Envelope(float since)
    {
        float first = MathF.Exp(-since * 9f);
        float second = since >= SecondBeat ? 0.7f * MathF.Exp(-(since - SecondBeat) * 10f) : 0f;
        return MathHelper.Clamp(first + second, 0f, 1f);
    }
}
