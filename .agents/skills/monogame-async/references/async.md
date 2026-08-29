# MonoGame Async Programming Reference

## Table of Contents
1. [Thread safety quick-reference](#thread-safety-quick-reference)
2. [Task-based background loading (recommended)](#task-based-background-loading-recommended)
3. [Loading screen scaffold](#loading-screen-scaffold)
4. [Progress reporting with IProgress<T>](#progress-reporting-with-iprogresst)
5. [Legacy APM Begin/End pattern](#legacy-apm-beginend-pattern)
6. [Wrapping APM in Task (TAP adapter)](#wrapping-apm-in-task-tap-adapter)
7. [Save/load async patterns](#saveload-async-patterns)
8. [Background world generation](#background-world-generation)
9. [Thread-safe result queue](#thread-safe-result-queue)
10. [Exception handling for background tasks](#exception-handling-for-background-tasks)

---

## Thread safety quick-reference

| Operation | Safe on background thread? |
|-----------|---------------------------|
| `File.ReadAllText` / `File.WriteAllText` | Yes |
| `JsonSerializer.Deserialize` | Yes |
| `HttpClient.GetAsync` | Yes |
| Heavy computation (pathfinding, gen) | Yes |
| `Content.Load<T>()` | **No — main thread only** |
| `GraphicsDevice` any method | **No — main thread only** |
| `Texture2D` / `RenderTarget2D` creation | **No — main thread only** |
| `SpriteBatch.Begin/Draw/End` | **No — main thread only** |
| `SoundEffect.Play()` | Generally yes (XAudio2), but test per platform |

---

## Task-based background loading (recommended)

```csharp
public class MyGame : Game
{
    private Task<RawLevelData> _loadTask;
    private RawLevelData       _rawData;
    private bool               _contentReady;

    protected override void LoadContent()
    {
        // Start background I/O immediately:
        _loadTask = Task.Run(() => RawLevelData.LoadFromDisk("Content/Levels/level1.json"));
    }

    protected override void Update(GameTime gameTime)
    {
        if (_loadTask != null && _loadTask.IsCompleted && !_contentReady)
        {
            // Check for exceptions before reading Result:
            if (_loadTask.IsFaulted)
                throw _loadTask.Exception!.GetBaseException();

            _rawData = _loadTask.Result;  // safe — already completed
            _loadTask = null;

            // GPU upload happens here, on the main thread:
            _tileset = Content.Load<Texture2D>(_rawData.TilesetPath);
            _contentReady = true;
        }

        base.Update(gameTime);
    }
}
```

---

## Loading screen scaffold

```csharp
public class LoadingScreen
{
    private Task         _backgroundTask;
    private float        _progress;        // 0.0 → 1.0
    private bool         _isDone;
    private Action       _onComplete;      // called on main thread when ready

    // Call this to kick off loading:
    public void Start(Func<IProgress<float>, Task> workFactory, Action onComplete)
    {
        _onComplete = onComplete;
        var progress = new Progress<float>(p => _progress = p);
        _backgroundTask = Task.Run(() => workFactory(progress));
    }

    public void Update(GameTime gameTime)
    {
        if (_isDone || _backgroundTask == null) return;

        if (_backgroundTask.IsCompleted)
        {
            if (_backgroundTask.IsFaulted)
                throw _backgroundTask.Exception!.GetBaseException();

            _isDone = true;
            _onComplete?.Invoke();
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D barTexture, GraphicsDevice gd)
    {
        int barWidth  = gd.Viewport.Width / 2;
        int barHeight = 20;
        int x = (gd.Viewport.Width - barWidth) / 2;
        int y = gd.Viewport.Height / 2;

        // Background track:
        sb.Draw(barTexture, new Rectangle(x, y, barWidth, barHeight), Color.DarkGray);
        // Filled portion:
        sb.Draw(barTexture, new Rectangle(x, y, (int)(barWidth * _progress), barHeight), Color.LimeGreen);

        sb.DrawString(font, $"Loading... {_progress:P0}", new Vector2(x, y - 24), Color.White);
    }
}
```

Usage in the game:

```csharp
_loadingScreen = new LoadingScreen();
_loadingScreen.Start(
    async progress =>
    {
        // All background-safe work goes here:
        _rawTiles  = await File.ReadAllBytesAsync("Content/Tilesets/dungeon.png");
        progress.Report(0.4f);
        _levelJson = await File.ReadAllTextAsync("Content/Levels/level1.json");
        progress.Report(0.8f);
        _levelData = JsonSerializer.Deserialize<LevelData>(_levelJson);
        progress.Report(1.0f);
    },
    onComplete: FinishLoadingOnMainThread   // called in Update() on main thread
);

private void FinishLoadingOnMainThread()
{
    // ContentManager calls are safe here — we're in Update():
    _tileset = Content.Load<Texture2D>("Tilesets/dungeon");
    _state   = GameState.Playing;
}
```

---

## Progress reporting with IProgress\<T\>

`Progress<T>` captures the `SynchronizationContext` at construction time. In a MonoGame app there is no automatic synchronization context, so the callback fires on a thread-pool thread. Keep the callback lightweight (just set a float field) to avoid races.

```csharp
// Constructed on the main thread:
var progress = new Progress<float>(value => _loadPercent = value);

// Used inside Task.Run — safe:
await Task.Run(async () =>
{
    for (int i = 0; i < steps; i++)
    {
        await ProcessStep(i);
        ((IProgress<float>)progress).Report((i + 1f) / steps);
    }
});
```

For a more explicit guarantee, write to a `volatile float` or use `Interlocked.Exchange`:

```csharp
private volatile float _loadPercent;

// In background task:
_loadPercent = (step + 1f) / totalSteps;
```

---

## Legacy APM Begin/End pattern

Some MonoGame / platform APIs (e.g., `StorageDevice`, `Guide`) use the old APM model.

### Polling approach

```csharp
// Field:
private IAsyncResult _asyncResult;

// Kick off (in Update or on button press):
_asyncResult = StorageDevice.BeginShowSelector(null, null);

// Poll in Update():
if (_asyncResult != null && _asyncResult.IsCompleted)
{
    StorageDevice device = StorageDevice.EndShowSelector(_asyncResult);
    _asyncResult = null;
    // Use device...
}
```

### Callback approach

```csharp
StorageDevice.BeginShowSelector(
    ar =>
    {
        // Called on a thread-pool thread — do NOT call Content or GraphicsDevice here
        StorageDevice device = StorageDevice.EndShowSelector(ar);
        _pendingDevice = device;  // volatile field or ConcurrentQueue
    },
    state: null
);

// Consume _pendingDevice in the next Update() on the main thread.
```

**Always call `End*` for every `Begin*` call** — even if you don't need the return value. Skipping it leaks handles.

---

## Wrapping APM in Task (TAP adapter)

Convert any Begin/End pair into an awaitable Task using `TaskFactory.FromAsync`:

```csharp
// Before — APM:
IAsyncResult ar = stream.BeginRead(buffer, 0, buffer.Length, null, null);
int bytesRead   = stream.EndRead(ar);

// After — TAP wrapper:
int bytesRead = await Task.Factory.FromAsync(
    stream.BeginRead(buffer, 0, buffer.Length, null, null),
    stream.EndRead);
```

Or with the overload that takes both delegates:

```csharp
int bytesRead = await Task<int>.Factory.FromAsync(
    (cb, state) => stream.BeginRead(buffer, 0, buffer.Length, cb, state),
    stream.EndRead,
    state: null);
```

---

## Save/load async patterns

```csharp
using System.IO;
using System.Text.Json;

public class SaveSystem
{
    private readonly string _savePath;

    public SaveSystem(string savePath) => _savePath = savePath;

    // Call with: await _save.SaveAsync(_data);
    public async Task SaveAsync<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_savePath, json);
    }

    // Returns null if file missing; throws on corrupt JSON.
    public async Task<T?> LoadAsync<T>()
    {
        if (!File.Exists(_savePath)) return default;
        var json = await File.ReadAllTextAsync(_savePath);
        return JsonSerializer.Deserialize<T>(json);
    }
}
```

Trigger from Update on a button press:

```csharp
if (input.SavePressed && _saveTask == null)
    _saveTask = _save.SaveAsync(_currentSave);

// Clear when done (check IsFaulted for error handling):
if (_saveTask?.IsCompleted == true)
    _saveTask = null;
```

---

## Background world generation

```csharp
public class WorldGenerator
{
    private Task<World> _genTask;
    public  World?      Result { get; private set; }
    public  bool        IsReady => _genTask?.IsCompleted == true;

    public void StartGeneration(int seed)
        => _genTask = Task.Run(() => GenerateWorld(seed));

    // Call once IsReady == true, on main thread:
    public void Finalize(ContentManager content)
    {
        if (_genTask!.IsFaulted) throw _genTask.Exception!.GetBaseException();
        Result = _genTask.Result;
        // Load any GPU resources needed for the world:
        Result.TileTexture = content.Load<Texture2D>(Result.TilesetPath);
    }

    private static World GenerateWorld(int seed)
    {
        // CPU-only: noise, BSP, cellular automata, etc.
        // No GraphicsDevice or ContentManager calls here.
        var world = new World(seed);
        world.Generate();
        return world;
    }
}
```

---

## Thread-safe result queue

When a background thread produces multiple results (e.g., a streaming downloader), use `ConcurrentQueue<T>` to hand them back to the main thread:

```csharp
using System.Collections.Concurrent;

private ConcurrentQueue<string> _downloadedChunks = new();

// Background thread pushes:
Task.Run(async () =>
{
    await foreach (var chunk in DownloadChunksAsync(url))
        _downloadedChunks.Enqueue(chunk);
});

// Main thread (Update) drains:
while (_downloadedChunks.TryDequeue(out var chunk))
    ProcessChunk(chunk);  // safe: main thread
```

---

## Exception handling for background tasks

Never swallow background task exceptions silently — they surface as `AggregateException` wrapped in `task.Exception`:

```csharp
// In Update():
if (_task != null && _task.IsCompleted)
{
    if (_task.IsFaulted)
    {
        var inner = _task.Exception!.GetBaseException();
        _errorMessage = $"Load failed: {inner.Message}";
        _state = GameState.Error;
    }
    else
    {
        // success path
    }
    _task = null;
}
```

For `async void` (unavoidable in some event patterns), add a try/catch inside:

```csharp
// Only use async void when forced by an event signature:
private async void OnButtonClick(object sender, EventArgs e)
{
    try   { await DoWorkAsync(); }
    catch (Exception ex) { _errorMessage = ex.Message; }
}
```
