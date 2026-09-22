---
name: monogame-scenes
description: MonoGame scene and screen management implementation guide covering SceneManager patterns, scene lifecycle, ContentManager per-scene, transitions, and UI layering. Use this skill whenever the user asks about scenes, screens, screen management, game states, transitions between levels, pause menus, title screens, loading screens, or how to organize the top-level structure of a MonoGame game.
---

# MonoGame Scene Management Implementation Guide

This skill guides scene/screen architecture in MonoGame. For transition effect code, see `references/scenes.md`.

## Scene Base Class

Every scene derives from `Scene` (`Alca.MonoGame.Kernel.Scenes`). All lifecycle methods are `virtual`, none are abstract — override only what you need:

```csharp
public abstract class Scene : IDisposable
{
    protected ContentManager Content { get; }  // per-scene, auto-disposed
    public bool IsDisposed { get; }
    public virtual bool IsOverlay => false;    // true = drawn on top, base scene still draws

    public virtual void Initialize() { }       // calls PreInitialize → LoadContent → PostInitialize
    protected virtual void PreInitialize() { }
    public virtual void LoadContent() { }
    protected virtual void PostInitialize() { } // calls InitializeUI()
    protected virtual void InitializeUI() { }  // hook for UI setup after content is loaded
    public virtual void UnloadContent() { Content.Unload(); }
    public virtual void Update(GameTime gameTime) { }
    public virtual void Draw(GameTime gameTime) { }
    public void Dispose() { ... }              // calls UnloadContent + Content.Dispose
}
```

Each scene gets its own `ContentManager` automatically — no constructor parameters needed. Assets are released when the scene is disposed.

**Overlay scenes:** Override `IsOverlay => true` to let the scene below keep drawing (e.g. pause menu over gameplay). The base scene's `Draw` still runs; only the overlay scene's `Update` runs.

```csharp
public sealed class PauseScene : Scene
{
    public override bool IsOverlay => true;

    public override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("Fonts/UI");
    }

    protected override void InitializeUI()
    {
        // UI elements created here — content is already loaded
    }

    public override void Update(GameTime gameTime) { ... }
    public override void Draw(GameTime gameTime) { ... }
}
```

## SceneManager

Access via `Core.SceneManager`. The `SceneManager` sits inside the kernel and delegates `Update`/`Draw` to the active scene:

```csharp
// Full scene replacement with fade (0.3 s):
Core.SceneManager.RequestChange(new GameplayScene());

// Push overlay (no fade — base scene remains visible if IsOverlay == true):
Core.SceneManager.PushScene(new PauseScene());

// Remove top overlay:
Core.SceneManager.PopScene();
```

**`RequestChange` vs `PushScene`:**
- `RequestChange` — replaces the current scene with a fade-out/in transition; disposes the old scene and all stacked overlays.
- `PushScene` — adds an overlay on top of the current scene (max 4 overlays). No fade.

**DrawFadeOverlay:** The fade black overlay must be drawn manually after the scene draw pass:

```csharp
// In your Game.Draw override (if you don't subclass Core):
Core.SceneManager.Draw(gameTime);
Core.SceneManager.DrawFadeOverlay(Core.SpriteBatch, Core.GraphicsDevice, _fadePixel);
```

When using `Core` as base class, this is already wired in.

## Scene Transitions

For fade/slide transitions between scenes:

1. Render the outgoing scene to a `RenderTarget2D` on the last frame before switching.
2. Render the incoming scene normally.
3. Use an alpha accumulator to blend between the two targets over N frames.

```csharp
private float _transitionAlpha = 0f;
private const float TransitionSpeed = 2.0f; // seconds to complete

// In Update during transition:
_transitionAlpha += (float)gameTime.ElapsedGameTime.TotalSeconds * TransitionSpeed;
if (_transitionAlpha >= 1f)
    CompleteTransition(); // switch to new scene

// In Draw:
DrawCurrentScene();
// Overlay outgoing scene fading out:
_spriteBatch.Begin();
_spriteBatch.Draw(_outgoingTarget, Vector2.Zero,
    new Color(1f, 1f, 1f, 1f - _transitionAlpha));
_spriteBatch.End();
```

## UI Layering

Draw UI in a separate `SpriteBatch.Begin`/`End` pair at the end of `Draw()`, after all world geometry. UI is always in screen space (no camera transform):

```csharp
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);

    // World — with camera matrix
    _spriteBatch.Begin(SpriteSortMode.BackToFront, transformMatrix: _cameraMatrix);
    DrawWorld();
    _spriteBatch.End();

    // UI — no camera, screen space only
    _spriteBatch.Begin(SpriteSortMode.Deferred);
    DrawHUD();
    DrawDialogue();
    _spriteBatch.End();
}
```

Never mix world sprites and UI sprites in the same `Begin`/`End` pair — UI positions would be affected by the camera transform.

## Asset Isolation Between Scenes

- Never hold a reference to a texture or sound loaded by Scene A from Scene B.
- If two scenes share assets (e.g., a common font), load them with the root `Content` manager in `Game1.LoadContent()` and pass them as constructor arguments to scenes.
- Scene-specific assets (level tileset, boss music) go in the scene's own `ContentManager`.

## Rules

- Use `Core.SceneManager.RequestChange(scene)` for transitions; never switch mid-Update.
- Use `Core.SceneManager.PushScene(overlay)` / `PopScene()` for overlays (max 4 stacked).
- Each scene gets its own `ContentManager` automatically — no setup needed.
- Never store cross-scene asset references — each scene is self-contained.
- UI draws in a separate `SpriteBatch.Begin/End` with no camera transform.
- Override `IsOverlay => true` so the base scene keeps drawing behind the overlay.
- `DrawFadeOverlay()` must be called manually after `Draw()` when not using `Core`.

## Reference

For transition effect implementations (fade, slide, wipe) and loading screen patterns, see `references/scenes.md`.
