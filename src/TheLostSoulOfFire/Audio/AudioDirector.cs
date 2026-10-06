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
    EndingReveal,
    Footstep,
    FootstepWood,
    HollowStep,
    BurningStep,
    DevourerStep,
    EnemyEmerge,
    UiMove,
    UiBack,
    UiOpen,
    UiClose,
    ChestOpen,
    CurrencyGain,
    AbilityHeal,
    AbilityPierce,
    AbilityLeap,
    AbilityVortex,
    AbilityGuard,
    AbilityMark,
    DoorAwaken,
    /// <summary>Contact layers under a landed blow: the target's material (and its weight).</summary>
    HitHollow,
    HitBurning,
    HitDevourer,
    HitDummy,
    HitHeavy,
    /// <summary>The blow landing on the player's body, at once, under the Ludo hurt sound.</summary>
    BodyHit,
    /// <summary>Wind-ups the enemies had no sound for, and the Burning's run.</summary>
    HollowWindup,
    DevourerWindup,
    BurningRush,
    /// <summary>How each enemy dies, layered with the shared death sound.</summary>
    DeathHollow,
    DeathBurning,
    DeathDevourer
}

/// <summary>Where the player is, for the ambience bed and the music (presentation only).</summary>
public enum AudioZone
{
    Title,
    Shore,
    Harbour,
    Causeway,
    Crossing,
    Threshold,
    Hub,
    Arena
}

/// <summary>
/// Central authored sound bank and mix policy. Real content-pipeline assets are
/// preferred; generated tones remain a non-fatal fallback for missing content.
/// </summary>
public sealed class AudioDirector : IDisposable
{
    private const int FallbackSampleRate = 22050;
    private const int EnemyVoiceLimit = 4;
    private const float MusicGameplayVolume = 0.5f;
    private const float MusicCalmVolume = 0.26f;

    private enum CueGroup
    {
        General,
        Enemy
    }

    private readonly record struct CuePolicy(
        float Cooldown,
        int Polyphony,
        float PitchVariation = 0f,
        CueGroup Group = CueGroup.General,
        bool Danger = false);

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
        [AudioCue.BurningCharge] = new(0.12f, 2, 0.02f, CueGroup.Enemy, Danger: true),
        [AudioCue.BurningDetonation] = new(0.12f, 2, 0.015f, CueGroup.Enemy),
        [AudioCue.CoreHit] = new(0.035f, 3, 0.02f),
        [AudioCue.SoulRelease] = new(0.08f, 2, 0.012f),
        [AudioCue.ResonanceReady] = new(0.35f, 1),
        [AudioCue.ResonanceActivate] = new(0.5f, 1),
        [AudioCue.PlayerHit] = new(0.08f, 2, 0.02f, Danger: true),
        [AudioCue.PlayerDeath] = new(0.5f, 1),
        [AudioCue.SoulSenseOn] = new(0.2f, 1),
        [AudioCue.SoulSenseOff] = new(0.2f, 1),
        [AudioCue.WaveStart] = new(0.45f, 1),
        [AudioCue.HollowSwipe] = new(0.075f, 3, 0.028f, CueGroup.Enemy, Danger: true),
        [AudioCue.DevourerSlam] = new(0.18f, 1, 0.012f, CueGroup.Enemy, Danger: true),
        [AudioCue.DevourerDevour] = new(0.35f, 1, 0.01f, CueGroup.Enemy),
        [AudioCue.EnemyDeath] = new(0.045f, 3, 0.03f, CueGroup.Enemy),
        [AudioCue.CannonImpact] = new(0.035f, 3, 0.025f),
        [AudioCue.TitleConfirm] = new(0.5f, 1),
        [AudioCue.WaveClear] = new(0.45f, 1),
        [AudioCue.EndingReveal] = new(1f, 1),
        [AudioCue.Footstep] = new(0.12f, 2, 0.05f),
        [AudioCue.FootstepWood] = new(0.12f, 2, 0.05f),
        [AudioCue.HollowStep] = new(0.06f, 3, 0.06f),
        [AudioCue.BurningStep] = new(0.05f, 3, 0.07f),
        [AudioCue.DevourerStep] = new(0.12f, 2, 0.04f),
        [AudioCue.EnemyEmerge] = new(0.08f, 3, 0.05f, CueGroup.Enemy),
        [AudioCue.UiMove] = new(0.03f, 2, 0.03f),
        [AudioCue.UiBack] = new(0.05f, 1, 0.02f),
        [AudioCue.UiOpen] = new(0.15f, 1),
        [AudioCue.UiClose] = new(0.15f, 1),
        [AudioCue.ChestOpen] = new(0.3f, 1),
        [AudioCue.CurrencyGain] = new(0.08f, 2, 0.04f),
        [AudioCue.AbilityHeal] = new(0.2f, 1),
        [AudioCue.AbilityPierce] = new(0.1f, 2, 0.02f),
        [AudioCue.AbilityLeap] = new(0.15f, 1, 0.02f),
        [AudioCue.AbilityVortex] = new(0.2f, 1),
        [AudioCue.AbilityGuard] = new(0.2f, 1, 0.02f),
        [AudioCue.AbilityMark] = new(0.15f, 1, 0.03f),
        [AudioCue.DoorAwaken] = new(1f, 1),
        [AudioCue.HitHollow] = new(0.03f, 3, 0.04f),
        [AudioCue.HitBurning] = new(0.03f, 3, 0.04f),
        [AudioCue.HitDevourer] = new(0.04f, 2, 0.03f),
        [AudioCue.HitDummy] = new(0.03f, 2, 0.05f),
        [AudioCue.HitHeavy] = new(0.08f, 1, 0.02f),
        [AudioCue.BodyHit] = new(0.08f, 2, 0.03f),
        [AudioCue.HollowWindup] = new(0.1f, 3, 0.04f, CueGroup.Enemy, Danger: true),
        [AudioCue.DevourerWindup] = new(0.3f, 1, 0.02f, CueGroup.Enemy, Danger: true),
        [AudioCue.BurningRush] = new(0.12f, 2, 0.03f, CueGroup.Enemy, Danger: true),
        [AudioCue.DeathHollow] = new(0.05f, 3, 0.03f, CueGroup.Enemy),
        [AudioCue.DeathBurning] = new(0.05f, 3, 0.03f, CueGroup.Enemy),
        [AudioCue.DeathDevourer] = new(0.2f, 1, 0.02f, CueGroup.Enemy)
    };

    /// <summary>
    /// Sounds that make way for a danger signal: started while one is fresh, they come in
    /// quieter (swings, steps, the cannon's charge), so the warning cuts through a busy fight.
    /// </summary>
    private static readonly HashSet<AudioCue> Yielding =
    [
        AudioCue.ScytheSwing1, AudioCue.ScytheSwing2, AudioCue.SoulCleave, AudioCue.Dash, AudioCue.CannonCharge,
        AudioCue.Footstep, AudioCue.FootstepWood, AudioCue.HollowStep, AudioCue.BurningStep, AudioCue.DevourerStep,
        AudioCue.EnemyEmerge, AudioCue.SoulRelease
    ];

    /// <summary>
    /// Hall tails (tools/audio/hall_tails.py): the reverberation of the hall a cue sounds in,
    /// rendered offline, played with the cue only in that hall at the given send. The foundry of
    /// the arena gives its blows and deaths their space; in the stone antechamber the player's
    /// steps carry. Everywhere else (the open-air prologue) the cues stay dry.
    /// </summary>
    private static readonly (AudioCue Cue, string Asset, AudioZone Hall, float Send)[] HallSends =
    [
        (AudioCue.ScytheHit, "Audio/Sfx/scythe_hit_hall", AudioZone.Arena, 0.34f),
        (AudioCue.CoreHit, "Audio/Sfx/core_hit_hall", AudioZone.Arena, 0.3f),
        (AudioCue.CannonFire, "Audio/Sfx/cannon_fire_hall", AudioZone.Arena, 0.36f),
        (AudioCue.CannonImpact, "Audio/Sfx/cannon_impact_hall", AudioZone.Arena, 0.34f),
        (AudioCue.BurningDetonation, "Audio/Sfx/burning_detonation_hall", AudioZone.Arena, 0.4f),
        (AudioCue.DevourerSlam, "Audio/Sfx/devourer_slam_hall", AudioZone.Arena, 0.4f),
        (AudioCue.EnemyDeath, "Audio/Sfx/enemy_death_hall", AudioZone.Arena, 0.34f),
        (AudioCue.SoulCleave, "Audio/Sfx/soul_cleave_hall", AudioZone.Arena, 0.36f),
        (AudioCue.PlayerHit, "Audio/Sfx/player_hit_hall", AudioZone.Arena, 0.3f),
        (AudioCue.WaveStart, "Audio/Sfx/wave_start_hall", AudioZone.Arena, 0.4f),
        (AudioCue.Footstep, "Audio/Sfx/footstep_stone_1_hall", AudioZone.Hub, 0.3f)
    ];

    private const int MaximumHallTails = 6;
    private readonly Dictionary<AudioCue, (SoundEffect Tail, AudioZone Hall, float Send)> _hallTails = [];
    private readonly List<SoundEffectInstance> _hallInstances = [];

    /// <summary>Extra takes of a cue (e.g. footsteps); Play picks one at random so repeats never match exactly.</summary>
    private readonly Dictionary<AudioCue, List<SoundEffect>> _variants = [];

    private readonly Dictionary<AudioCue, SoundEffect> _sounds = [];
    private readonly Dictionary<AudioCue, List<SoundEffectInstance>> _activeInstances = [];
    private readonly Dictionary<AudioCue, float> _cooldowns = [];
    private readonly HashSet<SoundEffect> _ownedFallbackSounds = [];
    private SoundEffect _ambienceSound;
    private SoundEffectInstance _ambience;
    private Song _music;
    private bool _musicPlaying;

    /// <summary>Ambience bed and music per zone; a zone without its own asset keeps silence or the arena bed.</summary>
    private static readonly Dictionary<AudioZone, string> AmbienceAssets = new()
    {
        [AudioZone.Title] = "Audio/Ambience/shore_ambience",
        [AudioZone.Shore] = "Audio/Ambience/shore_ambience",
        [AudioZone.Harbour] = "Audio/Ambience/harbour_ambience",
        [AudioZone.Causeway] = "Audio/Ambience/causeway_ambience",
        [AudioZone.Crossing] = "Audio/Ambience/crossing_ambience",
        [AudioZone.Threshold] = "Audio/Ambience/threshold_ambience",
        [AudioZone.Hub] = "Audio/Ambience/hub_ambience",
        [AudioZone.Arena] = "Audio/Ambience/arena_ambience"
    };

    private static readonly Dictionary<AudioZone, string> MusicAssets = new()
    {
        [AudioZone.Title] = "Audio/Music/title_theme",
        [AudioZone.Shore] = "Audio/Music/shore_theme",
        [AudioZone.Harbour] = "Audio/Music/shore_theme",
        [AudioZone.Causeway] = "Audio/Music/causeway_theme",
        [AudioZone.Crossing] = "Audio/Music/crossing_theme",
        [AudioZone.Threshold] = "Audio/Music/threshold_theme",
        [AudioZone.Hub] = "Audio/Music/hub_theme",
        [AudioZone.Arena] = "Audio/Music/arena_loop"
    };

    private const float ZoneCrossfadeSeconds = 1.6f;
    private const float MusicFadeOutSeconds = 1.4f;
    private const float MusicFadeInSeconds = 2.4f;

    private readonly ContentManager _content;
    private readonly Dictionary<string, SoundEffect?> _beds = [];
    private readonly Dictionary<string, Song?> _songs = [];
    private AudioZone _zone = AudioZone.Arena;
    private AudioZone _fadingZone = AudioZone.Arena;
    private SoundEffectInstance? _fadingAmbience;
    private SoundEffect? _lifeFlameSound;
    private SoundEffectInstance? _lifeFlame;
    private const float LifeFlameVolume = 0.44f;
    private float _zoneBlend = 1f;
    private Song? _wantedSong;
    private float _musicGain = 1f;

    public AudioZone Zone => _zone;
    private bool _calm;
    private bool _soulSense;
    private float _arenaMix = 1f;
    private float _targetArenaMix = 1f;
    private float _duckTimer;
    private float _duckAmount;

    /// <summary>Freshness of the last danger signal (1 at its start, fading over about 0.4 s).</summary>
    private float _focus;
    private uint _random = 0xA17D3C5Bu;
    private bool _available = true;
    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _effectsVolume = 1f;
    private bool _paused;

    /// <summary>Music and ambience level while the pause menu is open.</summary>
    private const float PausedBedVolume = 0.4f;

    public int FallbackSoundCount => _ownedFallbackSounds.Count;
    public bool MusicPlaying => _musicPlaying && MediaPlayer.State == MediaState.Playing;

    /// <summary>Diagnostics for the tour: what plays now (song asset, Life Flame loop level).</summary>
    public string DescribeEnding() => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"song={_music?.Name ?? "none"} gain={_musicGain:0.00} life_flame={(_lifeFlame?.State == SoundState.Playing ? _lifeFlame.Volume : 0f):0.00}");

    public AudioDirector(ContentManager content)
    {
        _content = content;
        try
        {
            Add(content, AudioCue.ScytheSwing1, "Audio/Sfx/scythe_swing_1_lead", 250f, 0.09f, 0.32f, 0.22f);
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
            foreach ((AudioCue cue, string asset) in new[]
            {
                (AudioCue.ScytheSwing1, "Audio/Sfx/scythe_swing_1_lead"), (AudioCue.ScytheSwing2, "Audio/Sfx/scythe_swing_2"),
                (AudioCue.ScytheHit, "Audio/Sfx/scythe_hit"), (AudioCue.CoreHit, "Audio/Sfx/core_hit"),
                (AudioCue.CannonImpact, "Audio/Sfx/cannon_impact"), (AudioCue.EnemyDeath, "Audio/Sfx/enemy_death"),
                (AudioCue.Dash, "Audio/Sfx/dash"), (AudioCue.HollowSwipe, "Audio/Sfx/hollow_swipe"),
                (AudioCue.SoulCleave, "Audio/Sfx/soul_cleave"), (AudioCue.SoulRelease, "Audio/Sfx/soul_release"),
                (AudioCue.CannonFire, "Audio/Sfx/cannon_fire"), (AudioCue.BurningDetonation, "Audio/Sfx/burning_detonation"),
                (AudioCue.BurningCharge, "Audio/Sfx/burning_charge"), (AudioCue.PlayerHit, "Audio/Sfx/player_hit")
            })
            {
                AddDerived(content, cue, asset, 2);
            }
            AddVariants(content, AudioCue.Footstep, "Audio/Sfx/footstep_stone", 4, 90f, 0.08f, 0.2f, 0.6f);
            AddVariants(content, AudioCue.FootstepWood, "Audio/Sfx/footstep_wood", 4, 120f, 0.09f, 0.2f, 0.5f);
            AddVariants(content, AudioCue.HollowStep, "Audio/Sfx/step_hollow", 4, 70f, 0.12f, 0.15f, 0.7f);
            AddVariants(content, AudioCue.BurningStep, "Audio/Sfx/step_burning", 4, 180f, 0.06f, 0.15f, 0.8f);
            AddVariants(content, AudioCue.DevourerStep, "Audio/Sfx/step_devourer", 4, 45f, 0.2f, 0.25f, 0.5f);
            Add(content, AudioCue.EnemyEmerge, "Audio/Sfx/enemy_emerge", 60f, 0.5f, 0.4f, 0.4f, rising: true);
            // Enemies often emerge together; identical takes started at once would phase.
            AddDerived(content, AudioCue.EnemyEmerge, "Audio/Sfx/enemy_emerge", 2);
            Add(content, AudioCue.UiMove, "Audio/Sfx/ui_move", 1568f, 0.05f, 0.15f, 0.02f);
            Add(content, AudioCue.UiBack, "Audio/Sfx/ui_back", 1046f, 0.06f, 0.15f, 0.02f);
            Add(content, AudioCue.UiOpen, "Audio/Sfx/ui_open", 600f, 0.2f, 0.15f, 0.2f, rising: true);
            Add(content, AudioCue.UiClose, "Audio/Sfx/ui_close", 600f, 0.15f, 0.15f, 0.2f);
            Add(content, AudioCue.ChestOpen, "Audio/Sfx/chest_open", 300f, 0.5f, 0.4f, 0.3f);
            Add(content, AudioCue.CurrencyGain, "Audio/Sfx/currency_gain", 3200f, 0.2f, 0.2f, 0.1f);
            AddDerived(content, AudioCue.CurrencyGain, "Audio/Sfx/currency_gain", 2);
            Add(content, AudioCue.AbilityHeal, "Audio/Sfx/ability_heal", 392f, 0.8f, 0.3f, 0.1f, rising: true);
            Add(content, AudioCue.AbilityPierce, "Audio/Sfx/ability_pierce", 880f, 0.3f, 0.4f, 0.4f);
            Add(content, AudioCue.AbilityLeap, "Audio/Sfx/ability_leap", 80f, 0.3f, 0.5f, 0.5f);
            Add(content, AudioCue.AbilityVortex, "Audio/Sfx/ability_vortex", 200f, 0.8f, 0.4f, 0.4f, rising: true);
            Add(content, AudioCue.AbilityGuard, "Audio/Sfx/ability_guard", 330f, 0.5f, 0.4f, 0.1f);
            Add(content, AudioCue.AbilityMark, "Audio/Sfx/ability_mark", 1661f, 0.3f, 0.3f, 0.3f);
            Add(content, AudioCue.DoorAwaken, "Audio/Sfx/door_awaken", 104f, 1.2f, 0.5f, 0.3f);
            AddVariants(content, AudioCue.HitHollow, "Audio/Sfx/hit_hollow", 3, 180f, 0.1f, 0.3f, 0.7f);
            AddVariants(content, AudioCue.HitBurning, "Audio/Sfx/hit_burning", 3, 240f, 0.12f, 0.3f, 0.8f);
            AddVariants(content, AudioCue.HitDevourer, "Audio/Sfx/hit_devourer", 3, 70f, 0.2f, 0.35f, 0.5f);
            AddVariants(content, AudioCue.HitDummy, "Audio/Sfx/hit_dummy", 3, 600f, 0.08f, 0.25f, 0.4f);
            AddVariants(content, AudioCue.HitHeavy, "Audio/Sfx/hit_heavy", 3, 50f, 0.2f, 0.35f, 0.4f);
            AddVariants(content, AudioCue.BodyHit, "Audio/Sfx/body_hit", 3, 110f, 0.1f, 0.3f, 0.6f);
            AddVariants(content, AudioCue.HollowWindup, "Audio/Sfx/hollow_windup", 2, 500f, 0.4f, 0.2f, 0.6f);
            AddVariants(content, AudioCue.DevourerWindup, "Audio/Sfx/devourer_windup", 2, 55f, 0.8f, 0.3f, 0.3f);
            AddVariants(content, AudioCue.BurningRush, "Audio/Sfx/burning_rush", 2, 140f, 0.6f, 0.3f, 0.8f);
            AddVariants(content, AudioCue.DeathHollow, "Audio/Sfx/death_hollow", 2, 2200f, 0.6f, 0.2f, 0.4f);
            AddVariants(content, AudioCue.DeathBurning, "Audio/Sfx/death_burning", 2, 120f, 0.7f, 0.2f, 0.7f);
            AddVariants(content, AudioCue.DeathDevourer, "Audio/Sfx/death_devourer", 2, 60f, 1.2f, 0.3f, 0.4f);
            foreach ((AudioCue cue, string asset, AudioZone hall, float send) in HallSends)
            {
                try
                {
                    _hallTails[cue] = (content.Load<SoundEffect>(asset), hall, send);
                }
                catch (ContentLoadException)
                {
                }
            }

            _ambienceSound = LoadOrCreateFallback(content, "Audio/Ambience/arena_ambience", 43f, 2.4f, 0.2f, 0.16f, false);
            _beds[AmbienceAssets[AudioZone.Arena]] = _ambienceSound;
            _ambience = _ambienceSound.CreateInstance();
            _ambience.IsLooped = true;
            _ambience.Play();

            TryStartMusic(content);
            // Beds and songs of every zone load now, not at the moment a zone is entered, so a
            // zone change never stalls a frame while a long ambience is read from disk.
            foreach (AudioZone zone in AmbienceAssets.Keys)
            {
                Bed(zone);
                SongFor(zone);
            }
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

        _focus = MathF.Max(0f, _focus - deltaTime / 0.4f);
        _duckTimer = MathF.Max(0f, _duckTimer - deltaTime);
        if (_duckTimer <= 0f)
        {
            _duckAmount = MathF.Max(0f, _duckAmount - deltaTime * 2.4f);
        }
        float mixStep = deltaTime / 0.9f;
        _arenaMix = _arenaMix < _targetArenaMix
            ? MathF.Min(_targetArenaMix, _arenaMix + mixStep)
            : MathF.Max(_targetArenaMix, _arenaMix - mixStep);
        UpdateZoneFades(deltaTime);
        EnsureMusicPlaying();
        ApplyMix();
    }

    /// <summary>
    /// Moves the ambience bed and the music to the zone the player is in: the beds crossfade,
    /// the music fades out, switches and fades in. Calling it every frame with the same zone is free.
    /// </summary>
    public void SetZone(AudioZone zone)
    {
        if (!_available || zone == _zone)
        {
            return;
        }

        try
        {
            SoundEffect? bed = Bed(zone) ?? Bed(AudioZone.Arena);
            if (bed is not null && bed != _ambienceSound)
            {
                _fadingAmbience?.Stop();
                _fadingAmbience?.Dispose();
                _fadingAmbience = _ambience;
                _fadingZone = _zone;
                _ambienceSound = bed;
                _ambience = bed.CreateInstance();
                _ambience.IsLooped = true;
                _ambience.Volume = 0f;
                _ambience.Play();
                if (_paused) _ambience.Pause();
                _zoneBlend = 0f;
            }
            _zone = zone;
            _wantedSong = SongFor(zone);
        }
        catch
        {
            _available = false;
        }
        ApplyMix();
    }

    private SoundEffect? Bed(AudioZone zone)
    {
        string asset = AmbienceAssets[zone];
        if (!_beds.TryGetValue(asset, out SoundEffect? bed))
        {
            try { bed = _content.Load<SoundEffect>(asset); }
            catch (ContentLoadException) { bed = null; }
            _beds[asset] = bed;
        }
        return bed;
    }

    private Song? SongFor(AudioZone zone)
    {
        string asset = MusicAssets[zone];
        if (!_songs.TryGetValue(asset, out Song? song))
        {
            try { song = asset == "Audio/Music/arena_loop" && _music is not null ? _music : _content.Load<Song>(asset); }
            catch (ContentLoadException) { song = null; }
            _songs[asset] = song;
        }
        return song;
    }

    private void UpdateZoneFades(float deltaTime)
    {
        if (_zoneBlend < 1f)
        {
            _zoneBlend = MathF.Min(1f, _zoneBlend + deltaTime / ZoneCrossfadeSeconds);
            if (_zoneBlend >= 1f && _fadingAmbience is not null)
            {
                try { _fadingAmbience.Stop(); _fadingAmbience.Dispose(); } catch { }
                _fadingAmbience = null;
            }
        }

        if (_wantedSong != _music)
        {
            _musicGain = MathF.Max(0f, _musicGain - deltaTime / MusicFadeOutSeconds);
            if (_musicGain <= 0f)
            {
                try
                {
                    MediaPlayer.Stop();
                    _music = _wantedSong;
                    _musicPlaying = _music is not null;
                    if (_music is not null)
                    {
                        MediaPlayer.IsRepeating = true;
                        MediaPlayer.Play(_music);
                    }
                }
                catch (Exception exception)
                {
                    _musicPlaying = false;
                    Console.Error.WriteLine($"AUDIO_MUSIC_SWITCH_FAILED message={exception.Message}");
                }
            }
        }
        else if (_musicGain < 1f)
        {
            _musicGain = MathF.Min(1f, _musicGain + deltaTime / MusicFadeInSeconds);
        }
    }

    public void SetVolumes(float master, float music, float effects)
    {
        _masterVolume = Math.Clamp(master, 0f, 1f);
        _musicVolume = Math.Clamp(music, 0f, 1f);
        _effectsVolume = Math.Clamp(effects, 0f, 1f);
        try { SoundEffect.MasterVolume = _masterVolume * _effectsVolume; } catch { }
        ApplyMix();
    }

    /// <summary>Raised for every cue that actually starts (diagnostics: the tour logs footsteps).</summary>
    public event Action<AudioCue>? CuePlayed;

    public void Play(AudioCue cue, float volume = 1f, float pitch = 0f) => Play(cue, volume, pitch, 0f);

    /// <summary>Plays a cue placed left or right of the listener (<paramref name="pan"/> −1 … 1).</summary>
    public void Play(AudioCue cue, float volume, float pitch, float pan)
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

        if (_variants.TryGetValue(cue, out List<SoundEffect>? takes) && takes.Count > 1)
        {
            sound = takes[(int)((NextSignedFloat() * 0.5f + 0.5f) * takes.Count) % takes.Count];
        }

        if (Yielding.Contains(cue))
        {
            volume *= 1f - 0.4f * _focus;
        }
        if (!policy.Danger)
        {
            // Many sounds at once: each new one comes in a little quieter, so a crowded fight
            // stays full without stacking into one loud, tiring wall.
            int busy = CountActiveVoices();
            if (busy > 6)
            {
                volume *= 1f / MathF.Sqrt(1f + (busy - 6) * 0.12f);
            }
        }

        SoundEffectInstance instance = null;
        try
        {
            instance = sound.CreateInstance();
            instance.Volume = Math.Clamp(volume, 0f, 1f);
            instance.Pitch = Math.Clamp(pitch + NextSignedFloat() * policy.PitchVariation, -1f, 1f);
            instance.Pan = Math.Clamp(pan, -1f, 1f);
            instance.Play();
            CuePlayed?.Invoke(cue);
            instances.Add(instance);
            PlayHallTail(cue, instance.Volume, instance.Pitch, pan);
            _cooldowns[cue] = policy.Cooldown;
            if (policy.Danger)
            {
                _focus = 1f;
            }
            ApplyCueDuck(cue);
        }
        catch
        {
            instance?.Dispose();
            _available = false;
        }
    }

    /// <summary>
    /// The hall answers a cue: its tail, at the cue's level times the send, at the same pitch and
    /// less to one side (a hall's reverberation comes from all around). In a crowded moment the
    /// tails already sounding carry the room, and a new one is left out.
    /// </summary>
    private void PlayHallTail(AudioCue cue, float volume, float pitch, float pan)
    {
        if (!_hallTails.TryGetValue(cue, out (SoundEffect Tail, AudioZone Hall, float Send) hall) || hall.Hall != _zone)
        {
            return;
        }

        for (int index = _hallInstances.Count - 1; index >= 0; index--)
        {
            if (_hallInstances[index].State == SoundState.Stopped)
            {
                _hallInstances[index].Dispose();
                _hallInstances.RemoveAt(index);
            }
        }
        if (_hallInstances.Count >= MaximumHallTails)
        {
            return;
        }

        SoundEffectInstance tail = hall.Tail.CreateInstance();
        tail.Volume = Math.Clamp(volume * hall.Send, 0f, 1f);
        tail.Pitch = pitch;
        tail.Pan = Math.Clamp(pan * 0.4f, -1f, 1f);
        tail.Play();
        _hallInstances.Add(tail);
        HallTailsPlayed[_zone] = HallTailsPlayed.GetValueOrDefault(_zone) + 1;
    }

    /// <summary>Diagnostics for the tour: how many hall tails each zone has played.</summary>
    public Dictionary<AudioZone, int> HallTailsPlayed { get; } = [];

    /// <summary>
    /// Pause menu mix: running effects are paused (and resumed later), music and
    /// ambience keep playing at a lower level.
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (_paused == paused)
        {
            return;
        }

        _paused = paused;
        try
        {
            foreach (List<SoundEffectInstance> instances in _activeInstances.Values)
            {
                foreach (SoundEffectInstance instance in instances)
                {
                    if (paused && instance.State == SoundState.Playing) instance.Pause();
                    else if (!paused && instance.State == SoundState.Paused) instance.Resume();
                }
            }
            foreach (SoundEffectInstance tail in _hallInstances)
            {
                if (paused && tail.State == SoundState.Playing) tail.Pause();
                else if (!paused && tail.State == SoundState.Paused) tail.Resume();
            }
            if (paused && _lifeFlame?.State == SoundState.Playing) _lifeFlame.Pause();
            else if (!paused && _lifeFlame?.State == SoundState.Paused) _lifeFlame.Resume();
        }
        catch
        {
            _available = false;
        }
        ApplyMix();
    }

    /// <summary>
    /// The Life Flame's fire after the last wave: a quiet crackling loop from the furnace.
    /// <paramref name="level"/> (0..1) follows the flame as it kindles; 0 stops it.
    /// </summary>
    public void SetLifeFlame(float level, float pan)
    {
        if (!_available || (level <= 0.001f && _lifeFlame is null))
        {
            return;
        }

        try
        {
            if (level <= 0.001f)
            {
                _lifeFlame!.Stop();
                _lifeFlame.Dispose();
                _lifeFlame = null;
                return;
            }
            if (_lifeFlame is null)
            {
                _lifeFlameSound ??= _content.Load<SoundEffect>("Audio/Sfx/life_flame_loop");
                _lifeFlame = _lifeFlameSound.CreateInstance();
                _lifeFlame.IsLooped = true;
                _lifeFlame.Volume = 0f;
                _lifeFlame.Play();
                if (_paused) _lifeFlame.Pause();
            }
            _lifeFlame.Volume = Math.Clamp(LifeFlameVolume * level, 0f, 1f);
            _lifeFlame.Pan = Math.Clamp(pan, -1f, 1f);
        }
        catch (ContentLoadException)
        {
            _lifeFlame = null;
        }
    }

    /// <summary>Discards every running or paused effect, e.g. when a run is abandoned.</summary>
    public void StopEffects()
    {
        SetLifeFlame(0f, 0f);
        foreach (SoundEffectInstance tail in _hallInstances)
        {
            try { tail.Stop(); tail.Dispose(); } catch { }
        }
        _hallInstances.Clear();
        foreach (List<SoundEffectInstance> instances in _activeInstances.Values)
        {
            foreach (SoundEffectInstance instance in instances)
            {
                try { instance.Stop(); instance.Dispose(); } catch { }
            }
            instances.Clear();
        }
    }

    private bool _ending;

    /// <summary>
    /// After the last wave the music turns to the threshold's theme, where the lost soul's motif
    /// finally resolves; the arena's bed stays. Calling it every frame with the same value is free.
    /// </summary>
    public void SetEnding(bool ending)
    {
        if (!_available || ending == _ending)
        {
            return;
        }

        _ending = ending;
        _wantedSong = SongFor(ending ? AudioZone.Threshold : _zone);
        ApplyMix();
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

    public void SetArenaActive(bool active, bool immediate = false)
    {
        _targetArenaMix = active ? 1f : 0f;
        if (immediate)
        {
            _arenaMix = _targetArenaMix;
        }
        ApplyMix();
    }

    public void Dispose()
    {
        DisposeSounds();
        try { SoundEffect.MasterVolume = 1f; } catch { }
        GC.SuppressFinalize(this);
    }

    private void Add(ContentManager content, AudioCue cue, string assetName, float frequency, float duration, float volume, float noise, bool rising = false)
    {
        _sounds[cue] = LoadOrCreateFallback(content, assetName, frequency, duration, volume, noise, rising);
        _activeInstances[cue] = [];
    }

    /// <summary>
    /// Adds the derived takes <paramref name="assetName"/>_v2, _v3 … (tools/audio/derive_variants.py)
    /// beside the cue's main take, so frequent hits and swings never repeat exactly.
    /// </summary>
    private void AddDerived(ContentManager content, AudioCue cue, string assetName, int count)
    {
        List<SoundEffect> takes = [_sounds[cue]];
        for (int index = 2; index <= count + 1; index++)
        {
            try
            {
                takes.Add(content.Load<SoundEffect>($"{assetName}_v{index}"));
            }
            catch (ContentLoadException)
            {
            }
        }
        _variants[cue] = takes;
    }

    /// <summary>Loads <paramref name="assetBase"/>_1 … _<paramref name="count"/>; the first is the cue's main take.</summary>
    private void AddVariants(ContentManager content, AudioCue cue, string assetBase, int count, float frequency, float duration, float volume, float noise)
    {
        List<SoundEffect> takes = [];
        for (int index = 1; index <= count; index++)
        {
            try
            {
                takes.Add(content.Load<SoundEffect>($"{assetBase}_{index}"));
            }
            catch (ContentLoadException)
            {
            }
        }
        if (takes.Count == 0)
        {
            SoundEffect fallback = CreateTone(frequency, duration, volume, noise, false);
            _ownedFallbackSounds.Add(fallback);
            takes.Add(fallback);
        }
        _sounds[cue] = takes[0];
        _variants[cue] = takes;
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
        const string assetName = "Audio/Music/arena_loop";
        try
        {
            _music = content.Load<Song>(assetName);
            _wantedSong = _music;
            _songs[assetName] = _music;
            MediaPlayer.IsMuted = false;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(_music);
            _musicPlaying = true;
        }
        catch (ContentLoadException exception)
        {
            Console.Error.WriteLine($"AUDIO_MUSIC_LOAD_FAILED asset={assetName} message={exception.Message}");
        }
    }

    private void EnsureMusicPlaying()
    {
        if (!_musicPlaying || _music is null || MediaPlayer.State != MediaState.Stopped)
        {
            return;
        }

        try
        {
            MediaPlayer.Play(_music);
        }
        catch (Exception exception)
        {
            _musicPlaying = false;
            Console.Error.WriteLine($"AUDIO_MUSIC_PLAYBACK_FAILED message={exception.Message}");
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
            case AudioCue.DevourerWindup:
                BeginDuck(0.8f, 0.22f);
                break;
        }
    }

    private void BeginDuck(float duration, float amount)
    {
        _duckTimer = MathF.Max(_duckTimer, duration);
        _duckAmount = MathF.Max(_duckAmount, amount);
        ApplyMix();
    }

    /// <summary>Bed level of a zone before Soul Sense, pause and event ducking.</summary>
    private float AmbienceLevel(AudioZone zone) => zone switch
    {
        AudioZone.Arena => Lerp(0.048f, _calm ? 0.035f : 0.12f, _arenaMix),
        AudioZone.Title => 0.07f,
        AudioZone.Hub => 0.13f,
        AudioZone.Crossing => 0.1f,
        _ => _calm ? 0.15f : 0.11f
    };

    /// <summary>Music level of a zone: quiet under exploration, fuller in fights; the arena keeps its mix.</summary>
    private float MusicLevel(AudioZone zone) => zone switch
    {
        AudioZone.Arena => (_calm ? MusicCalmVolume : MusicGameplayVolume) * _arenaMix,
        AudioZone.Title => 0.5f,
        AudioZone.Hub => 0.3f,
        AudioZone.Crossing => 0.32f,
        AudioZone.Threshold => 0.34f,
        _ => _calm ? 0.26f : 0.5f
    };

    private void ApplyMix()
    {
        float ambienceBase = AmbienceLevel(_zone);
        float fadingBase = AmbienceLevel(_fadingZone);
        float musicBase = MusicLevel(_ending ? AudioZone.Threshold : _zone) * _musicGain;
        if (_soulSense)
        {
            ambienceBase *= 0.52f;
            musicBase *= 0.68f;
        }

        if (_paused)
        {
            ambienceBase *= PausedBedVolume;
            musicBase *= PausedBedVolume;
        }

        float bedScale = _masterVolume * _effectsVolume * (1f - _duckAmount * 0.72f) * (_soulSense ? 0.52f : 1f) * (_paused ? PausedBedVolume : 1f);
        if (_ambience is not null)
        {
            _ambience.Volume = Math.Clamp(ambienceBase * _zoneBlend * _masterVolume * _effectsVolume * (1f - _duckAmount * 0.72f), 0f, 1f);
        }
        if (_fadingAmbience is not null)
        {
            _fadingAmbience.Volume = Math.Clamp(fadingBase * (1f - _zoneBlend) * bedScale, 0f, 1f);
        }
        if (_musicPlaying)
        {
            MediaPlayer.Volume = Math.Clamp(musicBase * _masterVolume * _musicVolume * (1f - _duckAmount * 0.62f), 0f, 1f);
        }
    }

    private static float Lerp(float from, float to, float amount) =>
        from + (to - from) * Math.Clamp(amount, 0f, 1f);

    private int CountActiveVoices()
    {
        int count = 0;
        foreach (List<SoundEffectInstance> instances in _activeInstances.Values)
        {
            count += instances.Count;
        }
        return count;
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
        _fadingAmbience?.Stop();
        _fadingAmbience?.Dispose();
        _fadingAmbience = null;
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
