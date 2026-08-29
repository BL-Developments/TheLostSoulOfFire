using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace TheLostSoulOfFire.Audio;

public enum AudioCue
{
    ScytheSwing1,
    ScytheSwing2,
    SoulCleave,
    ScytheHit,
    Dash,
    CannonCharge,
    CannonFull,
    CannonFire,
    BurningCharge,
    BurningDetonation,
    CoreHit,
    SoulRelease,
    ResonanceReady,
    ResonanceActivate,
    PlayerHit,
    PlayerDeath,
    SoulSenseOn,
    SoulSenseOff,
    WaveStart
}

/// <summary>
/// Central placeholder sound bank. Replace CreateTone calls with loaded SoundEffects
/// when final assets arrive; gameplay code only knows named AudioCue values.
/// </summary>
public sealed class AudioDirector : IDisposable
{
    private const int SampleRate = 22050;
    private readonly Dictionary<AudioCue, SoundEffect> _sounds = [];
    private SoundEffect _ambienceSound;
    private SoundEffectInstance _ambience;
    private bool _available = true;

    public AudioDirector()
    {
        try
        {
            Add(AudioCue.ScytheSwing1, 250f, 0.09f, 0.32f, 0.22f);
            Add(AudioCue.ScytheSwing2, 205f, 0.12f, 0.4f, 0.3f);
            Add(AudioCue.SoulCleave, 82f, 0.22f, 0.7f, 0.48f);
            Add(AudioCue.ScytheHit, 118f, 0.08f, 0.48f, 0.72f);
            Add(AudioCue.Dash, 72f, 0.16f, 0.48f, 0.3f);
            Add(AudioCue.CannonCharge, 105f, 0.34f, 0.32f, 0.08f, rising: true);
            Add(AudioCue.CannonFull, 740f, 0.18f, 0.45f, 0.05f);
            Add(AudioCue.CannonFire, 58f, 0.3f, 0.8f, 0.62f);
            Add(AudioCue.BurningCharge, 145f, 0.23f, 0.5f, 0.24f, rising: true);
            Add(AudioCue.BurningDetonation, 48f, 0.34f, 0.82f, 0.8f);
            Add(AudioCue.CoreHit, 910f, 0.13f, 0.42f, 0.08f);
            Add(AudioCue.SoulRelease, 560f, 0.45f, 0.28f, 0.02f, rising: true);
            Add(AudioCue.ResonanceReady, 360f, 0.3f, 0.42f, 0.08f);
            Add(AudioCue.ResonanceActivate, 55f, 0.5f, 0.88f, 0.5f, rising: true);
            Add(AudioCue.PlayerHit, 96f, 0.12f, 0.62f, 0.56f);
            Add(AudioCue.PlayerDeath, 52f, 0.65f, 0.7f, 0.32f);
            Add(AudioCue.SoulSenseOn, 440f, 0.18f, 0.24f, 0.04f, rising: true);
            Add(AudioCue.SoulSenseOff, 320f, 0.13f, 0.18f, 0.03f);
            Add(AudioCue.WaveStart, 64f, 0.42f, 0.5f, 0.24f);

            _ambienceSound = CreateTone(43f, 2.4f, 0.2f, 0.16f, false);
            _ambience = _ambienceSound.CreateInstance();
            _ambience.IsLooped = true;
            _ambience.Volume = 0.12f;
            _ambience.Play();
        }
        catch
        {
            // Audio hardware is optional for CI/headless verification; never crash gameplay.
            _available = false;
            DisposeSounds();
        }
    }

    public void Play(AudioCue cue, float volume = 1f, float pitch = 0f)
    {
        if (!_available || !_sounds.TryGetValue(cue, out SoundEffect sound))
        {
            return;
        }

        try
        {
            sound.Play(Math.Clamp(volume, 0f, 1f), Math.Clamp(pitch, -1f, 1f), 0f);
        }
        catch
        {
            _available = false;
        }
    }

    public void SetCalm(bool calm)
    {
        if (_ambience is not null)
        {
            _ambience.Volume = calm ? 0.035f : 0.12f;
        }
    }

    public void Dispose()
    {
        DisposeSounds();
        GC.SuppressFinalize(this);
    }

    private void Add(AudioCue cue, float frequency, float duration, float volume, float noise, bool rising = false) =>
        _sounds[cue] = CreateTone(frequency, duration, volume, noise, rising);

    private static SoundEffect CreateTone(float frequency, float duration, float volume, float noise, bool rising)
    {
        int sampleCount = Math.Max(1, (int)(SampleRate * duration));
        byte[] buffer = new byte[sampleCount * 2];
        uint random = 0x91E10DA5u;
        double phase = 0d;

        for (int i = 0; i < sampleCount; i++)
        {
            float progress = i / (float)sampleCount;
            float attack = Math.Clamp(progress / 0.035f, 0f, 1f);
            float release = Math.Clamp((1f - progress) / 0.22f, 0f, 1f);
            float envelope = attack * release;
            float currentFrequency = frequency * (rising ? 0.72f + progress * 0.78f : 1f - progress * 0.12f);
            phase += Math.Tau * currentFrequency / SampleRate;
            random = random * 1664525u + 1013904223u;
            float noiseSample = ((random >> 8) / 8388607.5f - 1f) * noise;
            float harmonic = MathF.Sin((float)phase) * 0.72f + MathF.Sin((float)phase * 2.01f) * 0.2f;
            short sample = (short)(Math.Clamp((harmonic + noiseSample) * envelope * volume, -1f, 1f) * short.MaxValue);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, SampleRate, AudioChannels.Mono);
    }

    private void DisposeSounds()
    {
        _ambience?.Stop();
        _ambience?.Dispose();
        _ambience = null;
        _ambienceSound?.Dispose();
        _ambienceSound = null;
        foreach (SoundEffect sound in _sounds.Values)
        {
            sound.Dispose();
        }
        _sounds.Clear();
    }
}
