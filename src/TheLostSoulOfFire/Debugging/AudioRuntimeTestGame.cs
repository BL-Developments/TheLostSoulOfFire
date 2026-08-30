using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Audio;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Non-interactive authored-audio runtime check. This mode loads the real
/// content pipeline, exercises every cue and mix transition, stresses voice
/// limiting, and exits automatically without changing normal game behavior.
/// </summary>
public sealed class AudioRuntimeTestGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly bool _expectFallback;
    private readonly bool _verifyLongLoops;
    private readonly float _exitTime;
    private readonly AudioCue[] _cues = Enum.GetValues<AudioCue>();
    private AudioDirector _audio;
    private int _cueIndex;
    private float _cueTimer = 0.1f;
    private float _elapsed;
    private float _stressTimer;

    public AudioRuntimeTestGame(bool expectFallback, bool verifyLongLoops = false)
    {
        _expectFallback = expectFallback;
        _verifyLongLoops = verifyLongLoops;
        _exitTime = verifyLongLoops ? 102f : 8.2f;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 480,
            PreferredBackBufferHeight = 270,
            SynchronizeWithVerticalRetrace = false
        };
        Content.RootDirectory = "Content";
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
        Window.Title = "The Lost Soul of Fire — Audio runtime test";
    }

    protected override void LoadContent()
    {
        _audio = new AudioDirector(Content);
        Console.WriteLine($"AUDIO_RUNTIME_LOAD fallbacks={_audio.FallbackSoundCount} music={_audio.MusicPlaying} cues={_cues.Length}");
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed += deltaTime;
        _cueTimer -= deltaTime;
        _audio.Update(deltaTime);

        if (_cueIndex < _cues.Length && _cueTimer <= 0f)
        {
            AudioCue cue = _cues[_cueIndex++];
            _audio.Play(cue, 0.48f);
            Console.WriteLine($"AUDIO_RUNTIME_CUE {cue}");
            _cueTimer = 0.22f;
        }

        if (_cueIndex >= _cues.Length)
        {
            _stressTimer -= deltaTime;
            if (_stressTimer <= 0f && _elapsed < 7.4f)
            {
                _stressTimer = 0.035f;
                _audio.Play(AudioCue.HollowSwipe, 0.3f);
                _audio.Play(AudioCue.EnemyDeath, 0.3f);
                _audio.Play(AudioCue.CannonImpact, 0.3f);
                _audio.Play(AudioCue.ScytheHit, 0.3f);
            }
        }

        if (_elapsed >= 6.3f && _elapsed - deltaTime < 6.3f)
        {
            _audio.SetSoulSense(true);
        }
        if (_elapsed >= 6.8f && _elapsed - deltaTime < 6.8f)
        {
            _audio.SetSoulSense(false);
            _audio.SetCalm(true);
        }
        if (_elapsed >= _exitTime)
        {
            bool fallbackStateValid = _expectFallback
                ? _audio.FallbackSoundCount > 0
                : _audio.FallbackSoundCount == 0;
            bool passed = fallbackStateValid && _audio.MusicPlaying;
            Console.WriteLine($"AUDIO_RUNTIME_TEST_{(passed ? "PASS" : "FAIL")} fallbacks={_audio.FallbackSoundCount} music={_audio.MusicPlaying} cues={_cues.Length} longLoop={_verifyLongLoops}");
            Environment.ExitCode = passed ? 0 : 1;
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(5, 4, 8));
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _audio?.Dispose();
        base.UnloadContent();
    }
}
