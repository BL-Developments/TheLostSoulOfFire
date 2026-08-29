using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

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
/// Central sound bank. Real content-pipeline assets are preferred when present;
/// generated tones remain as a non-fatal fallback for incomplete builds and CI.
/// </summary>
public sealed class AudioDirector : IDisposable
{
    private const int SampleRate = 22050;
    private readonly Dictionary<AudioCue, SoundEffect> _sounds = [];
    private readonly HashSet<SoundEffect> _ownedFallbackSounds = [];
    private SoundEffect _ambienceSound;
    private SoundEffectInstance _ambience;
    private Song _music;
    private bool _musicPlaying;
    private bool _available = true;

    public AudioDirector(ContentManager content)
    {
        try
        {
            Add(content, AudioCue.ScytheSwing1, "Audio/Sfx/scythe_swing_1", 250f, 0.09f, 0.32f, 0.22f);
            Add(content, AudioCue.ScytheSwing2, "Audio/Sfx/scythe_swing_2", 205f, 0.12f, 0.4f, 0.3f);
            Add(content, AudioCue.SoulCleave, "Audio/Sfx/soul_cleave", 82f, 0.22f, 0.7f, 0.48f);
            Add(content, AudioCue.ScytheHit, "Audio/Sfx/scythe_hit", 118f, 0.08f, 0.48f, 0.72f);
            Add(content, AudioCue.Dash, "Audio/Sfx/dash", 72f, 0.16f, 0.48f, 0.3f);
            Add(content, AudioCue.CannonCharge, "Audio/Sfx/cannon_charge", 105f, 0.34f, 0.32f, 0.08f, rising: true);
            Add(content, AudioCue.CannonFull, "Audio/Sfx/cannon_full", 740f, 0.18f, 0.45f, 0.05f);
            Add(content, AudioCue.CannonFire, "Audio/Sfx/cannon_fire", 58f, 0.3f, 0.8f, 0.62f);
            Add(content, AudioCue.BurningCharge, "Audio/Sfx/burning_charge", 145f, 0.23f, 0.5f, 0.24f, rising: true);
            Add(content, AudioCue.BurningDetonation, "Audio/Sfx/burning_detonation", 48f, 0.34f, 0.82f, 0.8f);
            Add(content, AudioCue.CoreHit, "Audio/Sfx/core_hit", 910f, 0.13f, 0.42f, 0.08f);
            Add(content, AudioCue.SoulRelease, "Audio/Sfx/soul_release", 560f, 0.45f, 0.28f, 0.02f, rising: true);
            Add(content, AudioCue.ResonanceReady, "Audio/Sfx/resonance_ready", 360f, 0.3f, 0.42f, 0.08f);
            Add(content, AudioCue.ResonanceActivate, "Audio/Sfx/resonance_activate", 55f, 0.5f, 0.88f, 0.5f, rising: true);
            Add(content, AudioCue.PlayerHit, "Audio/Sfx/player_hit", 96f, 0.12f, 0.62f, 0.56f);
            Add(content, AudioCue.PlayerDeath, "Audio/Sfx/player_death", 52f, 0.65f, 0.7f, 0.32f);
            Add(content, AudioCue.SoulSenseOn, "Audio/Sfx/soul_sense_on", 440f, 0.18f, 0.24f, 0.04f, rising: true);
            Add(content, AudioCue.SoulSenseOff, "Audio/Sfx/soul_sense_off", 320f, 0.13f, 0.18f, 0.03f);
            Add(content, AudioCue.WaveStart, "Audio/Sfx/wave_start", 64f, 0.42f, 0.5f, 0.24f);

            _ambienceSound = LoadOrCreateFallback(content, "Audio/Ambience/arena_ambience", 43f, 2.4f, 0.2f, 0.16f, false);
            _ambience = _ambienceSound.CreateInstance();
            _ambience.IsLooped = true;
            _ambience.Volume = 0.12f;
            _ambience.Play();

            TryStartMusic(content);
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

        if (_musicPlaying)
        {
            MediaPlayer.Volume = calm ? 0.12f : 0.28f;
        }
    }

    public void Dispose()
    {
        DisposeSounds();
        GC.SuppressFinalize(this);
    }

    private void Add(
        ContentManager content,
        AudioCue cue,
        string assetName,
        float frequency,
        float duration,
        float volume,
        float noise,
        bool rising = false) =>
        _sounds[cue] = LoadOrCreateFallback(content, assetName, frequency, duration, volume, noise, rising);

    private SoundEffect LoadOrCreateFallback(
        ContentManager content,
        string assetName,
        float frequency,
        float duration,
        float volume,
        float noise,
        bool rising)
    {
        try
        {
            return content.Load<SoundEffect>(assetName);
        }
        catch (ContentLoadException)
        {
            SoundEffect fallback = CreateTone(frequency, duration, volume, noise, rising);
            _ownedFallbackSounds.Add(fallback);
            return fallback;
        }
    }

    private void TryStartMusic(ContentManager content)
    {
        try
        {
            _music = content.Load<Song>("Audio/Music/arena_loop");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.28f;
            MediaPlayer.Play(_music);
            _musicPlaying = true;
        }
        catch (ContentLoadException)
        {
            // Music is optional until the authored loop is added to Content.mgcb.
        }
    }

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
        if (_musicPlaying)
        {
            MediaPlayer.Stop();
            _musicPlaying = false;
        }
        _ambience?.Stop();
        _ambience?.Dispose();
        _ambience = null;
        _ambienceSound = null;
        foreach (SoundEffect sound in _ownedFallbackSounds)
        {
            sound.Dispose();
        }
        _ownedFallbackSounds.Clear();
        _sounds.Clear();
    }
}
