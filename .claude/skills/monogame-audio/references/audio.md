# MonoGame Audio Reference

## Table of Contents
1. [SoundEffect — fire and forget](#soundeffect--fire-and-forget)
2. [SoundEffectInstance — controlled playback](#soundeffectinstance--controlled-playback)
3. [Song + MediaPlayer — background music](#song--mediaplayer--background-music)
4. [DynamicSoundEffectInstance — streaming / procedural](#dynamicsoundeffectinstance--streaming--procedural)
5. [WAV streaming via TitleContainer](#wav-streaming-via-titlecontainer)
6. [3D Audio — AudioListener + AudioEmitter](#3d-audio--audiolistener--audioemitter)
7. [Sound pooling pattern](#sound-pooling-pattern)
8. [Microphone recording](#microphone-recording)
9. [Platform limits and constraints](#platform-limits-and-constraints)

---

## SoundEffect — fire and forget

```csharp
// Load in LoadContent():
SoundEffect _sfx = Content.Load<SoundEffect>("Audio/Explosion");

// Play with defaults (volume 1, pitch 0, pan 0):
_sfx.Play();

// Play with parameters:
_sfx.Play(
    volume: 0.8f,   // 0.0 (silent) – 1.0 (full)
    pitch:  0.1f,   // -1.0 – 1.0 (semitone × 12 up/down)
    pan:   -0.3f    // -1.0 (left) – 1.0 (right)
);

// Duration of the clip:
TimeSpan duration = _sfx.Duration;
```

`SoundEffect.Play()` returns false if the voice limit is reached (no exception).

---

## SoundEffectInstance — controlled playback

```csharp
// Create once in LoadContent():
SoundEffectInstance _instance = _sfx.CreateInstance();
_instance.IsLooped = false;
_instance.Volume   = 0.75f;
_instance.Pitch    = 0f;
_instance.Pan      = 0f;

// Control:
_instance.Play();    // Start / resume
_instance.Pause();   // Pause (preserves position)
_instance.Resume();  // Resume from pause
_instance.Stop();    // Stop (resets position)
_instance.Stop(immediate: false); // Fade-out stop (platform dependent)

// State check:
SoundState state = _instance.State;
// SoundState.Playing / Paused / Stopped

// Safe dispose — ALWAYS Stop() first:
_instance.Stop();
_instance.Dispose();
```

### Looping

```csharp
_footstepInstance = _footstep.CreateInstance();
_footstepInstance.IsLooped = true;

// Toggle based on movement:
if (isWalking && _footstepInstance.State != SoundState.Playing)
    _footstepInstance.Play();
else if (!isWalking && _footstepInstance.State == SoundState.Playing)
    _footstepInstance.Pause(); // Pause, not Stop — preserves loop position
```

### Volume and pitch at runtime

```csharp
// Adjust in Update():
_instance.Volume = MathHelper.Clamp(_instance.Volume + delta, 0f, 1f);
_instance.Pitch  = MathHelper.Clamp(_instance.Pitch + 0.1f, -1f, 1f);
```

---

## Song + MediaPlayer — background music

```csharp
// Load:
Song _theme = Content.Load<Song>("Audio/Theme");

// Play:
MediaPlayer.Volume      = 0.6f;
MediaPlayer.IsRepeating = true;
MediaPlayer.Play(_theme);

// Control:
MediaPlayer.Pause();
MediaPlayer.Resume();
MediaPlayer.Stop();

// State check:
if (MediaPlayer.State == MediaState.Playing)  { ... }
if (MediaPlayer.State == MediaState.Paused)   { ... }
if (MediaPlayer.State == MediaState.Stopped)  { ... }

// Volume fade in Update():
MediaPlayer.Volume = MathHelper.Clamp(MediaPlayer.Volume - 0.01f, 0f, 1f);
```

Only one `Song` can play at a time. Calling `Play(newSong)` while another song plays stops it first. Always `Stop()` before unloading a `Song` — MonoGame can hold an internal reference otherwise.

---

## DynamicSoundEffectInstance — streaming / procedural

```csharp
// Create (not loaded from Content — constructed directly):
var dynamic = new DynamicSoundEffectInstance(
    sampleRate:   44100,
    channels:     AudioChannels.Stereo
);

// Submit PCM buffers:
dynamic.BufferNeeded += OnBufferNeeded;
dynamic.Play();

private void OnBufferNeeded(object sender, EventArgs e)
{
    byte[] buffer = GetNextAudioChunk(); // your streaming logic
    dynamic.SubmitBuffer(buffer);
}

// Pending buffer count:
int pending = dynamic.PendingBufferCount;

// Cleanup:
dynamic.Stop();
dynamic.Dispose();
```

Buffer format: PCM wave, 16-bit samples, 8–48 kHz, mono or stereo.

---

## WAV streaming via TitleContainer

Use this pattern when you need raw PCM data for `DynamicSoundEffectInstance` or procedural audio. In MGCB, set the WAV file's **Build Action to `Copy`** (not Compress).

```csharp
// In LoadContent():
DynamicSoundEffectInstance _dynamicSound;
byte[] _wavData;
int    _wavPosition;
int    _chunkSize;

using System.IO.Stream waveStream = TitleContainer.OpenStream(@"Content\rock_loop_mono.wav");
using var reader = new BinaryReader(waveStream);

// Parse WAV RIFF header:
reader.ReadInt32();  // chunkID   "RIFF"
reader.ReadInt32();  // fileSize
reader.ReadInt32();  // riffType  "WAVE"
reader.ReadInt32();  // fmtID     "fmt "
int fmtSize     = reader.ReadInt32();
reader.ReadInt16();  // fmtCode   PCM = 1
int channels    = reader.ReadInt16();
int sampleRate  = reader.ReadInt32();
reader.ReadInt32();  // fmtAvgBPS
reader.ReadInt16();  // fmtBlockAlign
reader.ReadInt16();  // bitDepth

if (fmtSize == 18)
{
    int extraSize = reader.ReadInt16();
    reader.ReadBytes(extraSize);
}

reader.ReadInt32();  // dataID  "data"
int dataSize = reader.ReadInt32();
_wavData = reader.ReadBytes(dataSize);

_dynamicSound = new DynamicSoundEffectInstance(sampleRate, (AudioChannels)channels);
_chunkSize    = _dynamicSound.GetSampleSizeInBytes(TimeSpan.FromMilliseconds(100));
_dynamicSound.BufferNeeded += OnBufferNeeded;
_dynamicSound.Play();

// BufferNeeded handler (submit 2 × half-chunks per call for smooth streaming):
private void OnBufferNeeded(object sender, EventArgs e)
{
    _dynamicSound.SubmitBuffer(_wavData, _wavPosition,             _chunkSize / 2);
    _dynamicSound.SubmitBuffer(_wavData, _wavPosition + _chunkSize / 2, _chunkSize / 2);
    _wavPosition += _chunkSize;
    if (_wavPosition + _chunkSize > _wavData.Length)
        _wavPosition = 0; // loop
}
```

PCM format constraints: 16-bit samples, 8–48 kHz, mono or stereo, little-endian interleaved.

---

## 3D Audio — AudioListener + AudioEmitter

```csharp
// Declare as fields (NOT per-frame allocations):
private AudioListener _listener = new AudioListener();
private AudioEmitter  _emitter  = new AudioEmitter();

// Update positions in Update():
_listener.Position = new Vector3(playerPos.X, playerPos.Y, 0f);
_listener.Up       = Vector3.Up;
_listener.Forward  = Vector3.Forward;

_emitter.Position = new Vector3(enemyPos.X, enemyPos.Y, 0f);

// Apply before (or after) Play():
_sfxInstance.Apply3D(_listener, _emitter);
_sfxInstance.Play();
```

`Apply3D` automatically sets `Volume` and `Pan` — do not set them manually after calling `Apply3D`. Update the listener/emitter positions every frame and call `Apply3D` again to update the spatialization.

---

## Sound pooling pattern

```csharp
// In LoadContent():
private const int PoolSize = 8;
private SoundEffectInstance[] _pool;
private int _poolIndex = 0;

_pool = new SoundEffectInstance[PoolSize];
for (int i = 0; i < PoolSize; i++)
    _pool[i] = _gunshot.CreateInstance();

// In gameplay code:
private void PlayGunshot(float pitch = 0f)
{
    var slot = _pool[_poolIndex];
    _poolIndex = (_poolIndex + 1) % PoolSize;

    slot.Stop();               // reclaim if still playing
    slot.Pitch = pitch;
    slot.Play();
}
```

---

## Microphone recording

Only available on **OpenAL platforms**: DesktopGL, iOS, Android. Not available on DirectX/Windows DirectX backend.

```csharp
// Fields:
private Microphone                _mic;
private byte[]                    _micBuffer;
private DynamicSoundEffectInstance _playback;

// Initialization (once, guard for null):
_mic = Microphone.Default;
if (_mic != null)
{
    _mic.BufferDuration = TimeSpan.FromMilliseconds(100);
    _micBuffer          = new byte[_mic.GetSampleSizeInBytes(_mic.BufferDuration)];
    _mic.BufferReady   += OnMicBufferReady;

    // For echo/playback — same sample rate as mic:
    _playback = new DynamicSoundEffectInstance(_mic.SampleRate, AudioChannels.Mono);
}

// Start / Stop recording:
private void StartRecording()
{
    if (_mic == null) return;
    try { _mic.Start(); }
    catch (NoMicrophoneConnectedException) { }
}

private void StopRecording()
{
    if (_mic == null) return;
    try { _mic.Stop(); }
    catch (NoMicrophoneConnectedException) { }
}

// BufferReady handler — echoes mic audio to speakers:
private void OnMicBufferReady(object sender, EventArgs e)
{
    try
    {
        int size = _mic.GetData(_micBuffer);
        _playback.SubmitBuffer(_micBuffer, 0, size);
        _playback.Play();
    }
    catch (NoMicrophoneConnectedException) { }
}
```

Key API:

| Member | Description |
|--------|-------------|
| `Microphone.Default` | Returns the default mic, or `null` if none connected |
| `Microphone.All` | `ReadOnlyCollection<Microphone>` of all connected mics |
| `mic.SampleRate` | Sample rate of captured audio (read-only) |
| `mic.BufferDuration` | Size of capture buffer (TimeSpan, min 100 ms) |
| `mic.GetSampleSizeInBytes(duration)` | Bytes needed for a given capture duration |
| `mic.GetData(byte[])` | Copies captured PCM into buffer, returns bytes written |
| `mic.Start()` / `mic.Stop()` | Begin / end capture |
| `mic.BufferReady` | Event raised when capture buffer is full |
| `mic.State` | `MicrophoneState.Started` / `Stopped` |

---

## Platform limits and constraints

| Platform | Max simultaneous voices |
|----------|------------------------|
| Desktop (Windows/Linux/macOS) | ~256 |
| Mobile (iOS/Android) | ~32 |

- `SoundEffect.Play()` (fire-and-forget) returns `false` silently when the limit is reached — no exception.
- `SoundEffectInstance.Play()` **throws `InstancePlayLimitException`** when the limit is exceeded — pool instances to avoid this.
- `DynamicSoundEffectInstance` counts against the voice limit.
- `Song` / `MediaPlayer` is independent of the voice count.
- `Apply3D` requires mono source audio for correct spatialization on all platforms.
