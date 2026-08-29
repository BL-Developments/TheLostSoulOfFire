---
name: monogame-2d
description: MonoGame 2D rendering guide covering SpriteBatch patterns, sprite transforms (rotation, scaling, tiling, scrolling), render targets, 2D camera, blend/sampler states, resolution independence, and GPU performance rules. Use this skill whenever the user asks about drawing sprites, SpriteBatch, 2D rendering layers, 2D camera, post-processing, render targets, tiling textures, scrolling backgrounds, sprite rotation or scaling, resolution scaling, or any 2D visual rendering in MonoGame — even if they just say "how do I draw X", "my sprites are wrong", or "how do I tile/scroll a texture".
---

# MonoGame 2D Rendering Implementation Guide

This skill provides architecture rules and implementation patterns for MonoGame 2D graphics. Apply the rules below directly when writing rendering code. For detailed API signatures and code samples, read `references/2d.md`.

## SpriteBatch Fundamentals

`SpriteBatch` is the primary 2D rendering API. Every `Begin()`/`End()` pair flushes a draw batch to the GPU — minimize how many pairs you use per frame.

### Choosing a Sort Mode

| Mode | When to use |
|------|-------------|
| `Deferred` (default) | General sprite rendering; buffers all calls, sorts by texture at `End()` |
| `BackToFront` | Overlapping sprites with explicit depth; uses `layerDepth` parameter |
| `FrontToBack` | Same as above, reversed |
| `Texture` | Many sprites from multiple atlases; groups by texture to reduce state changes |
| `Immediate` | Per-sprite `Effect` parameter changes; sends each draw to GPU instantly — avoid unless needed |

### BlendState

Use the static presets — never recreate `BlendState` instances per frame:
- `BlendState.AlphaBlend` — standard transparency (default)
- `BlendState.Additive` — for particles, lights, and glow effects (colors stack and brighten)
- `BlendState.NonPremultiplied` — for textures loaded without premultiplied alpha
- `BlendState.Opaque` — for fully opaque geometry; disables blending

### SamplerState

- `SamplerState.PointClamp` — nearest-neighbor filtering; **required for pixel art** to prevent blurring
- `SamplerState.LinearClamp` — bilinear filtering for smooth high-res assets
- `SamplerState.LinearWrap` — bilinear filtering with texture repeat; **required for tiling**

## Sprite Layering

Use `layerDepth` (0.0f = front, 1.0f = back) with `SpriteSortMode.BackToFront` to control draw order without multiple `Begin`/`End` pairs. Define layers as named constants:

```csharp
private const float LayerBackground = 1.0f;
private const float LayerTerrain    = 0.8f;
private const float LayerEntities   = 0.5f;
private const float LayerEffects    = 0.3f;
private const float LayerUI         = 0.0f;
```

## Sprite Transforms

### Rotation

Set `origin` to the texture center so the sprite rotates around its own center, not its top-left corner:

```csharp
// In LoadContent — compute once:
_origin = new Vector2(_texture.Width / 2f, _texture.Height / 2f);

// In Draw:
_spriteBatch.Draw(_texture, _position, null, Color.White,
    _rotationAngle, _origin, 1f, SpriteEffects.None, 0f);
```

Angle is in radians, clockwise. Wrap with `rotationAngle %= MathHelper.TwoPi`.

### Scaling

Three methods — choose based on what you know at draw time:

```csharp
// Uniform float scale (1.0 = original size, 2.0 = double)
_spriteBatch.Draw(_texture, _position, null, Color.White,
    0f, Vector2.Zero, 2.0f, SpriteEffects.None, 0f);

// Non-uniform Vector2 scale (independent X/Y)
_spriteBatch.Draw(_texture, _position, null, Color.White,
    0f, Vector2.Zero, new Vector2(2f, 0.5f), SpriteEffects.None, 0f);

// Destination rectangle (stretches texture to fill exactly)
var destRect = new Rectangle(x, y, targetWidth, targetHeight);
_spriteBatch.Draw(_texture, destRect, Color.White);
```

### Group Rotation Around a Pivot

To rotate multiple sprites as a unit around a shared point:

```csharp
// In Update — rotate all positions around 'pivot':
static void RotateAroundPivot(Vector2 pivot, float radians, Vector2[] positions)
{
    Matrix rot = Matrix.CreateRotationZ(radians);
    for (int i = 0; i < positions.Length; i++)
        positions[i] = Vector2.Transform(positions[i] - pivot, rot) + pivot;
}
```

Each sprite is then drawn at its rotated position with the same `rotationAngle` passed to `Draw()`. Store un-rotated positions separately and clone/recalculate each frame from the original array.

## Tiling

Use `SamplerState.LinearWrap` and a destination rectangle larger than the texture. The GPU tiles automatically:

```csharp
// In LoadContent:
_tileRect = new Rectangle(0, 0,
    _tileTexture.Width * tilesX, _tileTexture.Height * tilesY);

// In Draw:
_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
    SamplerState.LinearWrap, null, null);
_spriteBatch.Draw(_tileTexture, Vector2.Zero, _tileRect, Color.White,
    0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
_spriteBatch.End();
```

For pixel-art tiles, swap `LinearWrap` for `PointWrap` to prevent bilinear blurring.

## Scrolling Background

Draw the texture twice to create a seamless vertical (or horizontal) scroll:

```csharp
// Fields:
private float _scrollOffset;

// In Update:
_scrollOffset += scrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
_scrollOffset %= _bgTexture.Height;   // wrap to texture height

// In Draw:
_spriteBatch.Begin();
// First copy — current position
_spriteBatch.Draw(_bgTexture,
    new Vector2(screenCenterX, _scrollOffset), null, Color.White,
    0f, new Vector2(_bgTexture.Width / 2f, 0), 1f, SpriteEffects.None, 0f);
// Second copy — fills the gap when first copy scrolls off screen
_spriteBatch.Draw(_bgTexture,
    new Vector2(screenCenterX, _scrollOffset - _bgTexture.Height), null, Color.White,
    0f, new Vector2(_bgTexture.Width / 2f, 0), 1f, SpriteEffects.None, 0f);
_spriteBatch.End();
```

Use a seamless/tileable texture for a continuous loop. For horizontal scroll, swap X and `Width`.

## RenderTarget2D

Render targets let you draw to an off-screen texture for post-processing, UI compositing, or multi-pass shader effects.

**Rules:**
- Create `RenderTarget2D` in `LoadContent()` — never in `Update()` or `Draw()`
- After drawing to a target, always restore with `GraphicsDevice.SetRenderTarget(null)` before presenting to screen
- Call `Dispose()` explicitly when unloading a scene — `RenderTarget2D` holds GPU memory
- When chaining passes (e.g., blur then color grade), use two targets and ping-pong between them

**Canonical frame pattern with render target:**
1. `SetRenderTarget(myTarget)` → clear → draw scene
2. `SetRenderTarget(null)` → draw `myTarget` through a post-process `Effect`

## Resolution Independence

Design at a fixed virtual resolution and scale to any screen size using a transform matrix — this keeps all game logic in virtual coordinates.

```csharp
// Compute once per frame (or when window resizes)
float scaleX = GraphicsDevice.Viewport.Width  / (float)VirtualWidth;
float scaleY = GraphicsDevice.Viewport.Height / (float)VirtualHeight;
Matrix scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);

// Use in every SpriteBatch.Begin call
_spriteBatch.Begin(transformMatrix: scaleMatrix);
```

Rebuild `scaleMatrix` whenever `GraphicsDeviceManager.ApplyChanges()` is called (window resize or resolution change). Never scatter screen-size conditionals across gameplay code — all positions and sizes stay in virtual space.

## Camera 2D

A 2D camera is a transform matrix built from position, zoom, and rotation. Compute it once per `Update()` frame and cache it as a field.

```csharp
// Build camera matrix (origin at screen center)
_cameraMatrix =
    Matrix.CreateTranslation(-_cameraPos.X, -_cameraPos.Y, 0f) *
    Matrix.CreateRotationZ(_cameraRotation) *
    Matrix.CreateScale(_cameraZoom, _cameraZoom, 1f) *
    Matrix.CreateTranslation(Viewport.Width / 2f, Viewport.Height / 2f, 0f);

// Apply
_spriteBatch.Begin(transformMatrix: _cameraMatrix);
```

To convert screen coordinates to world coordinates (e.g., mouse position):
```csharp
Vector2 worldPos = Vector2.Transform(screenPos, Matrix.Invert(_cameraMatrix));
```

Cache `Matrix.Invert` if called every frame — matrix inversion is not free.

## Performance Rules

These prevent GC stalls and frame-rate stuttering:

- **No allocations in `Update()` / `Draw()`** — never write `new Texture2D(...)`, `new RenderTarget2D(...)`, `new SpriteBatch(...)`, or `new Vector2(...)` inside the game loop. Allocate in `LoadContent()` or `Initialize()`.
- **Mutate struct fields instead of reassigning** — write `_pos.X = x; _pos.Y = y;` not `_pos = new Vector2(x, y)`.
- **Minimize Begin/End pairs** — each pair is a GPU flush. Group sprites that share the same `Effect`, `BlendState`, and `SamplerState` into one batch.
- **Use static Color presets** — `Color.White`, `Color.Black`, etc. When you need a custom tint, store it as a class field.

## Anti-Patterns to Avoid

- Never call `Begin()` without a matching `End()` in the same frame path.
- Never share a `SpriteBatch` across threads — it is not thread-safe.
- Never sample from a `RenderTarget2D` while it is also set as the active render target — GPU undefined behavior.
- Never use `SpriteSortMode.Immediate` with a shared `Effect` across sprites without resetting parameters between draws.

## Reference

For detailed method overloads, `Draw()` parameter descriptions, and worked examples, read `references/2d.md`.
