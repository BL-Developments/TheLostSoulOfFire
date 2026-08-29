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
    WaveStart,
    HollowSwipe,
    DevourerSlam,
    DevourerDevour,
    EnemyDeath,
    CannonImpact,
    TitleConfirm,
    WaveClear,
    EndingReveal
}

/// <summary>
/// Central authored sound bank and mix policy. Real content-pipeline assets are
/// preferred; generated tones remain a non-fatal fallback for missing content.
/// </summary>
public sealed class AudioDirector : IDisposable
{
    private const int FallbackSampleRate = 22050;
    private const int EnemyVoiceLimit = 4;

    private enum CueGroup
    {
        General,
        Enemy
    }

    private readonly record struct CuePolicy(
        float Cooldown,
        int Polyphony,
        float PitchVariation = 0f,
        CueGroup Group = CueGroup.General);

    private static readonly Dictionary<AudioCue, CuePolicy> Policies = new()
    {
        [AudioCue.ScytheSwing1] = new(0.04f, 2, 0.018f),
        [AudioCue.ScytheSwing2] = new(0.05f, 2, 0.018f),
        [AudioCue.SoulCleave] = new(0.12f, 1, 0.01f),
        [AudioCue.ScytheHit] = new(0.035f, 3, 0.025f),
        [AudioCue.Dash] = new(0.12f, 1, 0.015f),
        [AudioCue.CannonCharge] = new(0.16f, 1),
        [AudioCue.CannonFull] = new(0.2f, 1),
        [AudioCue.CannonFire] = new(0.1f, 2, 0.012f),
        [AudioCue.BurningCharge] = new(0.12f, 2, 0.02f, CueGroup.Enemy),
        [AudioCue.BurningDetonation] = new(0.12f, 2, 0.015f, CueGroup.Enemy),
        [AudioCue.CoreHit] = new(0.035f, 3, 0.02f),
        [AudioCue.SoulRelease] = new(0.08f, 2, 0.012f),
        [AudioCue.ResonanceReady] = new(0.35f, 1),
        [AudioCue.ResonanceActivate] = new(0.5f, 1),
        [AudioCue.PlayerHit] = new(0.08f, 2, 0.02f),
        [AudioCue.PlayerDeath] = new(0.5f, 1),
        [AudioCue.SoulSenseOn] = new(0.2f, 1),
        [AudioCue.SoulSenseOff] = new(0.2f, 1),
        [AudioCue.WaveStart] = new(0.45f, 1),
        [AudioCue.HollowSwipe] = new(0.075f, 3, 0.028f, CueGroup.Enemy),
        [AudioCue.DevourerSlam] = new(0.18f, 1, 0.012f, CueGroup.Enemy),
        [AudioCue.DevourerDevour] = new(0.35f, 1, 0.01f, CueGroup.Enemy),
        [AudioCue.EnemyDeath] = new(0.045f, 3, 0.03f, CueGroup.Enemy),
        [AudioCue.CannonImpact] = new(0.035f, 3, 0.025f),
        [AudioCue.TitleConfirm] = new(0.5f, 1),
        [AudioCue.WaveClear] = new(0.45f, 1),
        [AudioCue.EndingReveal] = new(1f, 1)
    };

    private readonly Dictionary<AudioCue, SoundEffect> _sounds = [];
    private readonly Dictionary<AudioCue, List<SoundEffectInstance>> _activeInstances = [];
    private readonly Dictionary<AudioCue, float> _cooldowns = [];
    private readonly HashSet<SoundEffect> _ownedFallbackSounds = [];
    private SoundEffect _ambienceSound;
    private SoundEffectInstance _ambience;
    private Song _music;
    private bool _musicPlaying;
    private bool _calm;
    private bool _soulSense;
    private float _duckTimer;
    private float _duckAmount;
    private uint _random = 0xA17D3C5Bu;
    private bool _available = true;

    public int FallbackSoundCount => _ownedFallbackSounds.Count;

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
            Add(content, AudioCue.HollowSwipe, "Audio/Sfx/hollow_swipe", 190f, 0.24f, 0.42f, 0.52f);
            Add(content, AudioCue.DevourerSlam, "Audio/Sfx/devourer_slam", 42f, 0.48f, 0.76f, 0.62f);
            Add(content, AudioCue.DevourerDevour, "Audio/Sfx/devourer_devour", 74f, 0.58f, 0.54f, 0.36f);
            Add(content, AudioCue.EnemyDeath, "Audio/Sfx/enemy_death", 68f, 0.38f, 0.52f, 0.48f);
            Add(content, AudioCue.CannonImpact, "Audio/Sfx/cannon_impact", 72f, 0.24f, 0.6f, 0.52f);
            Add(content, AudioCue.TitleConfirm, "Audio/Sfx/title_confirm", 440f, 0.26f, 0.3f, 0.015f);
            Add(content, AudioCue.WaveClear, "Audio/Sfx/wave_clear", 294f, 0.52f, 0.32f, 0.01f, rising: true);
            Add(content, AudioCue.EndingReveal, "Audio/Sfx/ending_reveal", 147f, 0.9f, 0.3f, 0.015f, rising: true);

            _ambienceSound = LoadOrCreateFallback(content, "Audio/Ambience/arena_ambience", 43f, 2.4f, 0.2f, 0.16f, false);
            _ambience = _ambienceSound.CreateInstance();
            _ambience.IsLooped = true;
            _ambience.Play();

            TryStartMusic(content);
            ApplyMix();
        }
        catch
        {
            // Audio hardware is optional for CI/headless verification; never crash gameplay.
            _available = false;
            DisposeSounds();
        }
    }

    public void Update(float deltaTime)
    {
        if (!_available)
        {
            return;
        }

        foreach (AudioCue cue in Enum.GetValues<AudioCue>())
        {
            if (_cooldowns.TryGetValue(cue, out float cooldown))
            {
                _cooldowns[cue] = MathF.Max(0f, cooldown - deltaTime);
            }
            CleanupInstances(cue);
        }

        _duckTimer = MathF.Max(0f, _duckTimer - deltaTime);
        if (_duckTimer <= 0f)
        {
            _duckAmount = MathF.Max(0f, _duckAmount - deltaTime * 2.4f);
        }
        ApplyMix();
    }

    public void Play(AudioCue cue, float volume = 1f, float pitch = 0f)
    {
        if (!_available || !_sounds.TryGetValue(cue, out SoundEffect sound))
        {
            return;
        }

        CuePolicy policy = Policies[cue];
        if (_cooldowns.TryGetValue(cue, out float cooldown) && cooldown > 0f)
        {
            return;
        }

        CleanupInstances(cue);
        List<SoundEffectInstance> instances = GetInstances(cue);
        if (instances.Count >= policy.Polyphony ||
            policy.Group == CueGroup.Enemy && CountActiveEnemyVoices() >= EnemyVoiceLimit)
        {
            return;
        }

        SoundEffectInstance instance = null;
        try
        {
            instance = sound.CreateInstance();
            instance.Volume = Math.Clamp(volume, 0f, 1f);
            instance.Pitch = Math.Clamp(pitch + NextSignedFloat() * policy.PitchVariation, -1f, 1f);
            instance.Pan = 0f;
            instance.Play();
            instances.Add(instance);
            _cooldowns[cue] = policy.Cooldown;
            ApplyCueDuck(cue);
        }
        catch
        {
            instance?.Dispose();
            _available = false;
        }
    }

    public void SetCalm(bool calm)
    {
        _calm = calm;
        ApplyMix();
    }

    public void SetSoulSense(bool active)
    {
        _soulSense = active;
        ApplyMix();
    }

    public void Dispose()
    {
        DisposeSounds();
        GC.SuppressFinalize(this);
    }

    private void Add(ContentManager content, AudioCue cue, string assetName, float frequency, float duration, float volume, float noise, bool rising = false)
    {
        _sounds[cue] = LoadOrCreateFallback(content, assetName, frequency, duration, volume, noise, rising);
        _activeInstances[cue] = [];
    }

    private SoundEffect LoadOrCreateFallback(ContentManager content, string assetName, float frequency, float duration, float volume, float noise, bool rising)
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
            MediaPlayer.Play(_music);
            _musicPlaying = true;
        }
        catch (ContentLoadException)
        {
            // Music remains optional so a missing file never stops gameplay.
        }
    }

    private void ApplyCueDuck(AudioCue cue)
    {
        switch (cue)
        {
            case AudioCue.ResonanceActivate:
                BeginDuck(1.05f, 0.68f);
                break;
            case AudioCue.SoulRelease:
                BeginDuck(0.72f, 0.48f);
                break;
            case AudioCue.WaveClear:
                BeginDuck(0.62f, 0.34f);
                break;
            case AudioCue.EndingReveal:
                BeginDuck(1.3f, 0.52f);
                break;
        }
    }

    private void BeginDuck(float duration, float amount)
    {
        _duckTimer = MathF.Max(_duckTimer, duration);
        _duckAmount = MathF.Max(_duckAmount, amount);
        ApplyMix();
    }

    private void ApplyMix()
    {
        float ambienceBase = _calm ? 0.035f : 0.12f;
        float musicBase = _calm ? 0.12f : 0.28f;
        if (_soulSense)
        {
            ambienceBase *= 0.52f;
            musicBase *= 0.68f;
        }

        if (_ambience is not null)
        {
            _ambience.Volume = Math.Clamp(ambienceBase * (1f - _duckAmount * 0.72f), 0f, 1f);
        }
        if (_musicPlaying)
        {
            MediaPlayer.Volume = Math.Clamp(musicBase * (1f - _duckAmount * 0.62f), 0f, 1f);
        }
    }

    private int CountActiveEnemyVoices()
    {
        int count = 0;
        foreach ((AudioCue cue, List<SoundEffectInstance> instances) in _activeInstances)
        {
            if (Policies[cue].Group == CueGroup.Enemy)
            {
                count += instances.Count;
            }
        }
        return count;
    }

    private List<SoundEffectInstance> GetInstances(AudioCue cue)
    {
        if (!_activeInstances.TryGetValue(cue, out List<SoundEffectInstance> instances))
        {
            instances = [];
            _activeInstances[cue] = instances;
        }
        return instances;
    }

    private void CleanupInstances(AudioCue cue)
    {
        List<SoundEffectInstance> instances = GetInstances(cue);
        for (int i = instances.Count - 1; i >= 0; i--)
        {
            if (instances[i].State != SoundState.Stopped)
            {
                continue;
            }
            instances[i].Dispose();
            instances.RemoveAt(i);
        }
    }

    private float NextSignedFloat()
    {
        _random = _random * 1664525u + 1013904223u;
        return ((_random >> 8) / 8388607.5f) - 1f;
    }

    private static SoundEffect CreateTone(float frequency, float duration, float volume, float noise, bool rising)
    {
        int sampleCount = Math.Max(1, (int)(FallbackSampleRate * duration));
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
            phase += Math.Tau * currentFrequency / FallbackSampleRate;
            random = random * 1664525u + 1013904223u;
            float noiseSample = ((random >> 8) / 8388607.5f - 1f) * noise;
            float harmonic = MathF.Sin((float)phase) * 0.72f + MathF.Sin((float)phase * 2.01f) * 0.2f;
            short sample = (short)(Math.Clamp((harmonic + noiseSample) * envelope * volume, -1f, 1f) * short.MaxValue);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, FallbackSampleRate, AudioChannels.Mono);
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

        foreach (List<SoundEffectInstance> instances in _activeInstances.Values)
        {
            foreach (SoundEffectInstance instance in instances)
            {
                instance.Stop();
                instance.Dispose();
            }
            instances.Clear();
        }
        foreach (SoundEffect sound in _ownedFallbackSounds)
        {
            sound.Dispose();
        }
        _ownedFallbackSounds.Clear();
        _activeInstances.Clear();
        _cooldowns.Clear();
        _sounds.Clear();
    }
}
