---
name: monogame-shaders
description: MonoGame shader implementation guide covering HLSL .fx files, Effect class, Material pattern, SpriteBatch integration, render target post-processing, and hot reload. Use this skill whenever the user asks about shaders, HLSL, pixel effects, post-processing, visual effects, vignette, blur, color grading, distortion, the Effect class, or any shader-related implementation in MonoGame — even if they just say "I want a visual effect" without saying "shader".
---

# MonoGame Shader Implementation Guide

This skill guides shader development in MonoGame. For worked HLSL examples, see `references/shaders.md`.

## File Pipeline

```
MyEffect.fx  →  MGFXC compiler  →  MyEffect.mgfxo  →  Content.Load<Effect>()
```

- Write shaders in **HLSL** in `.fx` files.
- Add them to the MGCB with processor **Effect - MonoGame** → compiled to `.mgfxo`.
- Load at runtime: `_effect = Content.Load<Effect>("Shaders/MyEffect");`

## Minimal HLSL Shader Structure

```hlsl
// Parameters declared at top
float4x4 World;
float    Time;
texture2D Texture;

sampler2D Sampler = sampler_state
{
    Texture   = <Texture>;
    MinFilter = Point;
    MagFilter = Point;
};

// Input/output structs
struct VSInput  { float4 Position : POSITION; float2 UV : TEXCOORD0; float4 Color : COLOR0; };
struct PSInput  { float4 Position : SV_Position; float2 UV : TEXCOORD0; float4 Color : COLOR0; };

// Vertex shader (pass-through for 2D)
PSInput VS(VSInput input)
{
    PSInput output;
    output.Position = mul(input.Position, World);
    output.UV       = input.UV;
    output.Color    = input.Color;
    return output;
}

// Pixel shader
float4 PS(PSInput input) : SV_Target
{
    float4 color = tex2D(Sampler, input.UV) * input.Color;
    // ... modify color here ...
    return color;
}

// Technique
technique MyTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VS();
        PixelShader  = compile ps_3_0 PS();
    }
}
```

For pure 2D sprite shaders, a pass-through vertex shader is always correct — only the pixel shader needs customization.

## Setting Shader Parameters

Access parameters by name. Set them in `LoadContent()` for constants, in `Update()` for values that change each frame:

```csharp
// Single values:
_effect.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
_effect.Parameters["Resolution"].SetValue(new Vector2(1920, 1080));
_effect.Parameters["Intensity"].SetValue(0.8f);

// Textures:
_effect.Parameters["MaskTexture"].SetValue(_maskTexture);

// Matrices:
_effect.Parameters["World"].SetValue(worldMatrix);
```

Use string indexing (`Parameters["Name"]`) rather than numeric indexing — it's clear and safe. For hot-path updates (called 60× per second), cache the `EffectParameter` as a field:

```csharp
// In LoadContent():
_timeParam = _effect.Parameters["Time"];

// In Update():
_timeParam.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
```

## Material Pattern

Wrap each `Effect` in a **Material** class that owns its parameters and exposes a typed API. This prevents scattered parameter strings and makes the effect reusable:

```csharp
public class VignetteMaterial
{
    private readonly Effect _effect;
    private readonly EffectParameter _intensityParam;
    private readonly EffectParameter _radiusParam;

    public float Intensity { get; set; } = 0.8f;
    public float Radius    { get; set; } = 0.6f;

    public VignetteMaterial(Effect effect)
    {
        _effect         = effect;
        _intensityParam = effect.Parameters["Intensity"];
        _radiusParam    = effect.Parameters["Radius"];
    }

    public void Apply()
    {
        _intensityParam.SetValue(Intensity);
        _radiusParam.SetValue(Radius);
        _effect.CurrentTechnique.Passes[0].Apply();
    }

    public Effect Effect => _effect;
}
```

## Applying to SpriteBatch

### Whole-batch effect (all sprites in batch use the shader)
```csharp
_spriteBatch.Begin(
    SpriteSortMode.Deferred,
    BlendState.AlphaBlend,
    effect: _grayscaleEffect
);
```

### Per-sprite effect changes (requires Immediate mode)
```csharp
_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
_colorSwapEffect.Parameters["SwapColor"].SetValue(Color.Red.ToVector4());
_colorSwapEffect.CurrentTechnique.Passes[0].Apply();
_spriteBatch.Draw(sprite, pos, Color.White);
_spriteBatch.End();
```

Use `SpriteSortMode.Immediate` only when you need different parameters per sprite. Each parameter change inside an `Immediate` batch sends a draw call to the GPU.

## Post-Processing Pipeline

Chain effects using two `RenderTarget2D` instances (ping-pong):

```csharp
// Pass 1: scene → sceneTarget
GraphicsDevice.SetRenderTarget(_sceneTarget);
DrawScene();

// Pass 2: sceneTarget → blurTarget through blur shader
GraphicsDevice.SetRenderTarget(_blurTarget);
_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, effect: _blurEffect);
_spriteBatch.Draw(_sceneTarget, fullScreen, Color.White);
_spriteBatch.End();

// Final pass: blurTarget → back buffer through vignette shader
GraphicsDevice.SetRenderTarget(null);
_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, effect: _vignetteEffect);
_spriteBatch.Draw(_blurTarget, fullScreen, Color.White);
_spriteBatch.End();
```

Always `SetRenderTarget(null)` before the final pass to the screen.

## UV Coordinate System

In HLSL for MonoGame (DirectX):
- UV (0, 0) = top-left of the texture
- UV (1, 1) = bottom-right

Screen center: `float2(0.5, 0.5)`. Distance from center: `length(uv - float2(0.5, 0.5))`.

## Rules

- Compile `.fx` files through MGCB — never load raw HLSL at runtime on release builds.
- Cache `EffectParameter` references as fields when setting values every frame.
- Never read back GPU data (no `GetData()` calls) inside the shader pass — write to a `RenderTarget2D` and read it on the next frame if needed.
- `SpriteSortMode.Immediate` flushes each draw call individually — minimize its use.
- All `RenderTarget2D` instances used for post-processing must be created in `LoadContent()`.

## Reference

For HLSL function reference, transition effect patterns, color manipulation, and light/shadow shaders, see `references/shaders.md`.
