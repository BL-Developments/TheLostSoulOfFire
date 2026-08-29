# MonoGame Effects Reference

Cross-platform MGFX effect patterns extracted from official documentation.

## Table of Contents
1. [Cross-platform .fx template](#cross-platform-fx-template)
2. [Stock effect usage](#stock-effect-usage)
3. [Effect parameter API](#effect-parameter-api)
4. [Technique and pass iteration](#technique-and-pass-iteration)
5. [BasicEffect setup](#basiceffect-setup)
6. [SkinnedEffect setup](#skinnedeffect-setup)
7. [Preprocessor symbol reference](#preprocessor-symbol-reference)

---

## Cross-platform .fx template

Minimal portable effect that compiles on both DirectX and OpenGL:

```hlsl
#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// --- Parameters ---
float4x4 WorldViewProjection;
texture2D SpriteTexture;

sampler2D SpriteSampler = sampler_state
{
    Texture   = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
};

// --- Structs ---
struct VSInput
{
    float4 Position : POSITION;
    float2 UV       : TEXCOORD0;
    float4 Color    : COLOR0;
};

struct PSInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
    float4 Color    : COLOR0;
};

// --- Vertex shader ---
PSInput MainVS(VSInput input)
{
    PSInput output;
    output.Position = mul(input.Position, WorldViewProjection);
    output.UV       = input.UV;
    output.Color    = input.Color;
    return output;
}

// --- Pixel shader ---
float4 MainPS(PSInput input) : SV_Target
{
    return tex2D(SpriteSampler, input.UV) * input.Color;
}

// --- Technique ---
technique MyEffect
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();
    }
}
```

> For 2D SpriteBatch shaders, `WorldViewProjection` is set automatically — you only need to add custom parameters.

---

## Stock effect usage

### SpriteEffect (implicit — used by SpriteBatch)
```csharp
// SpriteBatch uses SpriteEffect internally. No setup needed.
_spriteBatch.Begin();
_spriteBatch.Draw(texture, position, Color.White);
_spriteBatch.End();
```

### AlphaTestEffect
```csharp
var alphaTest = new AlphaTestEffect(GraphicsDevice)
{
    Projection = Matrix.CreateOrthographicOffCenter(
        0, GraphicsDevice.Viewport.Width,
        GraphicsDevice.Viewport.Height, 0,
        0, 1),
    AlphaFunction = CompareFunction.Greater,
    ReferenceAlpha = 128  // pixels with alpha <= 128 are discarded
};

_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, effect: alphaTest);
```

### DualTextureEffect
```csharp
var dualTex = new DualTextureEffect(GraphicsDevice);
dualTex.Texture  = _baseTexture;
dualTex.Texture2 = _detailTexture;
dualTex.World      = worldMatrix;
dualTex.View       = viewMatrix;
dualTex.Projection = projMatrix;
```

---

## Effect parameter API

### Load and access
```csharp
// Load (in LoadContent):
Effect _fx = Content.Load<Effect>("Shaders/MyEffect");

// Access by name:
EffectParameter _timeParam = _fx.Parameters["Time"];
```

### Set values
```csharp
_fx.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
_fx.Parameters["Resolution"].SetValue(new Vector2(1920, 1080));
_fx.Parameters["Intensity"].SetValue(0.8f);
_fx.Parameters["World"].SetValue(worldMatrix);          // Matrix
_fx.Parameters["MaskTex"].SetValue(_maskTexture);       // Texture2D
_fx.Parameters["Colors"].SetValue(colorArray);          // float[] / Vector4[]
```

### Cache hot parameters (called 60×/sec)
```csharp
// In LoadContent:
_timeParam = _fx.Parameters["Time"];

// In Update:
_timeParam.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
```

### Check parameter existence (defensive, for optional features)
```csharp
var p = _fx.Parameters["OptionalFeature"];
if (p != null) p.SetValue(true);
```

---

## Technique and pass iteration

### Apply a single pass (most common)
```csharp
_fx.CurrentTechnique.Passes[0].Apply();
// Then issue draw calls
```

### Multi-pass loop
```csharp
foreach (EffectPass pass in _fx.CurrentTechnique.Passes)
{
    pass.Apply();
    // issue draws for this pass
}
```

### Switch technique
```csharp
_fx.CurrentTechnique = _fx.Techniques["HighQuality"];
```

---

## BasicEffect setup

Minimal 3D setup with per-vertex lighting:

```csharp
// In LoadContent:
_basicEffect = new BasicEffect(GraphicsDevice)
{
    TextureEnabled    = true,
    Texture           = _meshTexture,
    LightingEnabled   = true,
    AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f)
};
_basicEffect.EnableDefaultLighting(); // adds 3 directional lights

// In Draw:
_basicEffect.World      = worldMatrix;
_basicEffect.View       = _camera.View;
_basicEffect.Projection = _camera.Projection;

foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
{
    pass.Apply();
    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, indexCount / 3);
}
```

---

## SkinnedEffect setup

GPU skinning — supports up to 72 bones:

```csharp
// In LoadContent:
_skinnedEffect = new SkinnedEffect(GraphicsDevice);
_skinnedEffect.Texture = _characterTexture;
_skinnedEffect.EnableDefaultLighting();

// In Draw (after updating animation):
Matrix[] boneTransforms = new Matrix[_model.Bones.Count]; // allocate in LoadContent
_model.CopyAbsoluteBoneTransformsTo(boneTransforms);
_skinnedEffect.SetBoneTransforms(boneTransforms);

_skinnedEffect.World      = worldMatrix;
_skinnedEffect.View       = viewMatrix;
_skinnedEffect.Projection = projMatrix;

foreach (EffectPass pass in _skinnedEffect.CurrentTechnique.Passes)
{
    pass.Apply();
    // draw mesh parts
}
```

---

## Preprocessor symbol reference

| Symbol | Platform | Use |
|--------|----------|-----|
| `2MGFX` | Always | Guard code that must not run under FXC |
| `HLSL` | DirectX | Enable DX-specific syntax |
| `SM4` | DirectX | Enable SM4+ features |
| `OpenGL` | OpenGL | Enable GLSL-compatible code paths |
| `GLSL` | OpenGL | Same as `OpenGL` — use either |

### Conditional feature example
```hlsl
#if SM4
    // SM4 allows structured buffers and compute-like tricks
    float4 result = tex2Dlod(Sampler, float4(uv, 0, 0));
#else
    float4 result = tex2D(Sampler, uv);
#endif
```

### Define custom symbols from MGCB Editor
1. Select the `.fx` file in MGCB Editor.
2. Set the **Defines** property: `MY_SYMBOL;ANOTHER_SYMBOL`.
3. Rebuild content.

From MGFXC command line:
```
mgfxc MyEffect.fx MyEffect.mgfxo /defines:MY_SYMBOL,ANOTHER_SYMBOL
```
