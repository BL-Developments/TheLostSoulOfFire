# MonoGame Shaders Reference

## Table of Contents
1. [HLSL file structure](#hlsl-file-structure)
2. [Parameter types and SetValue](#parameter-types-and-setvalue)
3. [Sampler state in HLSL](#sampler-state-in-hlsl)
4. [SpriteBatch built-in parameters](#spritebatch-built-in-parameters)
5. [Common pixel shader patterns](#common-pixel-shader-patterns)
6. [Hot reload system](#hot-reload-system)
7. [Material class pattern](#material-class-pattern)
8. [Transition effect](#transition-effect)
9. [Color swap / LUT effect](#color-swap--lut-effect)
10. [Multi-pass post-processing](#multi-pass-post-processing)

---

## HLSL file structure

```hlsl
// ─── Parameters ───────────────────────────────────────────────
float4x4 MatrixTransform;   // SpriteBatch's built-in world-view-proj
float    Time;
float    Intensity;
float2   Resolution;
float4   Color;
texture2D InputTexture;
texture2D SecondaryTexture;

// ─── Samplers ─────────────────────────────────────────────────
sampler2D InputSampler = sampler_state
{
    Texture   = <InputTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

// ─── Vertex / pixel input structs ─────────────────────────────
struct VSInput
{
    float4 Position : POSITION;
    float4 Color    : COLOR0;
    float2 UV       : TEXCOORD0;
};

struct PSInput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float2 UV       : TEXCOORD0;
};

// ─── Vertex shader (pass-through — correct for 2D) ────────────
PSInput VS(VSInput input)
{
    PSInput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color    = input.Color;
    output.UV       = input.UV;
    return output;
}

// ─── Pixel shader ─────────────────────────────────────────────
float4 PS(PSInput input) : SV_Target
{
    float4 color = tex2D(InputSampler, input.UV) * input.Color;
    // modify color here...
    return color;
}

// ─── Technique ────────────────────────────────────────────────
#if OPENGL
    #define VS_PROFILE vs_3_0
    #define PS_PROFILE ps_3_0
#else
    #define VS_PROFILE vs_4_0_level_9_1
    #define PS_PROFILE ps_4_0_level_9_1
#endif

technique MyEffect
{
    pass Pass1
    {
        VertexShader = compile VS_PROFILE VS();
        PixelShader  = compile PS_PROFILE PS();
    }
}
```

---

## Parameter types and SetValue

```csharp
EffectParameter p = _effect.Parameters["Name"];

p.SetValue(1.5f);                      // float
p.SetValue(new Vector2(w, h));         // float2
p.SetValue(new Vector3(x, y, z));      // float3
p.SetValue(new Vector4(r, g, b, a));   // float4 / color
p.SetValue(Color.Red.ToVector4());     // float4 from Color
p.SetValue(myMatrix);                  // float4x4 Matrix
p.SetValue(myTexture);                 // Texture2D / texture2D
p.SetValue(new float[] { 1f, 2f });    // float array
p.SetValue(new Vector2[] { ... });     // float2 array

// Get (read back — rarely needed):
float v = p.GetValueSingle();
Vector2 v2 = p.GetValueVector2();
Matrix m = p.GetValueMatrix();
```

### Cache parameters to avoid per-frame string lookup

```csharp
// In LoadContent():
private EffectParameter _timeParam;
private EffectParameter _intensityParam;

_timeParam      = _effect.Parameters["Time"];
_intensityParam = _effect.Parameters["Intensity"];

// In Update():
_timeParam.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
_intensityParam.SetValue(_intensity);
```

### Safe set (won't crash if parameter was optimized away)

```csharp
static void SetParam(Effect effect, string name, float value)
{
    var p = effect.Parameters[name];
    p?.SetValue(value); // null-conditional — safe if optimized out
}
```

---

## Sampler state in HLSL

```hlsl
// Pixel art — nearest neighbor:
sampler2D PixelSampler = sampler_state
{
    Texture   = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

// Smooth — bilinear:
sampler2D SmoothSampler = sampler_state
{
    Texture   = <SpriteTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU  = Clamp;
    AddressV  = Clamp;
};

// Tiling / wrapping:
sampler2D TileSampler = sampler_state
{
    Texture   = <TileTexture>;
    AddressU  = Wrap;
    AddressV  = Wrap;
};
```

---

## SpriteBatch built-in parameters

When using a custom Effect with `SpriteBatch`, the sprite batch provides these automatically:

| HLSL name | C# type | Notes |
|-----------|---------|-------|
| `MatrixTransform` | `float4x4` | Combined world-view-projection matrix |
| `SpriteTexture` | `texture2D` | The texture passed to `Draw()` |

Your vertex shader **must** multiply position by `MatrixTransform` for sprites to appear correctly on screen.

---

## Common pixel shader patterns

### Grayscale (desaturate)

```hlsl
float4 PS(PSInput input) : SV_Target
{
    float4 color = tex2D(Sampler, input.UV) * input.Color;
    // Rec. 709 luminance weights:
    float grey = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    color.rgb  = lerp(color.rgb, float3(grey, grey, grey), Intensity);
    return color;
}
```

### Vignette (darken edges)

```hlsl
float4 PS(PSInput input) : SV_Target
{
    float4 color = tex2D(Sampler, input.UV);
    float2 center = input.UV - float2(0.5, 0.5);
    float dist    = length(center) / Radius;
    float vignette = 1.0 - smoothstep(0.0, 1.0, pow(dist, Intensity));
    color.rgb *= vignette;
    return color;
}
```

### Color tint / overlay

```hlsl
float4 PS(PSInput input) : SV_Target
{
    float4 color = tex2D(Sampler, input.UV) * input.Color;
    color.rgb = lerp(color.rgb, TintColor.rgb, TintStrength);
    return color;
}
```

### Screen wipe transition (horizontal)

```hlsl
float Progress; // 0 = start, 1 = fully wiped

float4 PS(PSInput input) : SV_Target
{
    float4 color = tex2D(Sampler, input.UV);
    // Soft edge using smoothstep:
    float edge  = smoothstep(Progress - 0.05, Progress + 0.05, input.UV.x);
    color.a    *= edge;
    return color;
}
```

### UV offset (scroll / parallax)

```hlsl
float2 UVOffset;  // set from C# each frame

float4 PS(PSInput input) : SV_Target
{
    float2 uv    = input.UV + UVOffset;
    float4 color = tex2D(WrapSampler, uv);  // use Wrap address mode
    return color;
}
```

---

## Hot reload system

The `WatchedAsset<T>` pattern recompiles and reloads a shader at runtime when the `.fx` file changes. Use only in `DEBUG` builds.

```csharp
#if DEBUG
private FileSystemWatcher _watcher;
private volatile bool     _pendingReload;

void WatchShader(string fxPath)
{
    _watcher = new FileSystemWatcher(
        Path.GetDirectoryName(fxPath),
        Path.GetFileName(fxPath))
    {
        NotifyFilter = NotifyFilters.LastWrite,
        EnableRaisingEvents = true
    };
    _watcher.Changed += (_, _) => _pendingReload = true;
}

// In Update():
if (_pendingReload)
{
    _pendingReload = false;
    RecompileAndReload(); // run MGFXC and Content.Load<Effect> again
}
#endif
```

---

## Material class pattern

```csharp
public class DesaturateMaterial
{
    private readonly Effect _effect;
    private readonly EffectParameter _intensityParam;

    public float Intensity { get; set; } = 1.0f;
    public bool  Enabled   { get; set; } = true;

    // Returns null when disabled — SpriteBatch falls back to default shader:
    public Effect Effect => Enabled ? _effect : null;

    public DesaturateMaterial(ContentManager content, string assetPath)
    {
        _effect         = content.Load<Effect>(assetPath);
        _intensityParam = _effect.Parameters["Intensity"];
    }

    public void Update()
    {
        _intensityParam?.SetValue(Intensity);
    }
}

// Usage:
_spriteBatch.Begin(effect: _desaturate.Effect);
```

---

## Transition effect

```csharp
public class SceneTransition
{
    private readonly Effect _effect;
    private readonly EffectParameter _progressParam;
    private readonly Texture2D _wipePattern; // drives the wipe shape

    public float Progress { get; private set; } = 0f; // 0→1

    public bool IsComplete => Progress >= 1f;

    public void Update(float delta, float speed = 1f)
    {
        Progress = MathHelper.Clamp(Progress + delta * speed, 0f, 1f);
        _progressParam.SetValue(Progress);
    }

    public void Draw(SpriteBatch sb, RenderTarget2D sceneTarget, Rectangle fullScreen)
    {
        _effect.Parameters["WipePattern"]?.SetValue(_wipePattern);
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, effect: _effect);
        sb.Draw(sceneTarget, fullScreen, Color.White);
        sb.End();
    }
}
```

---

## Color swap / LUT effect

```hlsl
// HLSL — maps original colors through a 256x1 lookup texture:
texture2D ColorMap;

sampler2D ColorMapSampler = sampler_state
{
    Texture   = <ColorMap>;
    MinFilter = Point;
    MagFilter = Point;
    AddressU  = Clamp;
};

float4 PS(PSInput input) : SV_Target
{
    float4 original = tex2D(Sampler, input.UV) * input.Color;
    // Use red channel as LUT index (normalized 0–1):
    float2 lutUV = float2(original.r, 0.5);
    float4 mapped = tex2D(ColorMapSampler, lutUV);
    return float4(mapped.rgb, original.a);
}
```

Generating the LUT texture in C#:

```csharp
Texture2D BuildLUT(GraphicsDevice gd, Func<byte, Color> mapping)
{
    var lut = new Texture2D(gd, 256, 1);
    var data = new Color[256];
    for (int i = 0; i < 256; i++)
        data[i] = mapping((byte)i);
    lut.SetData(data);
    return lut;
}

// Sepia-tone LUT:
Texture2D sepiaLUT = BuildLUT(GraphicsDevice, r =>
{
    float v = r / 255f;
    return new Color(
        (int)(v * 112 + 66),
        (int)(v * 75 + 43),
        (int)(v * 39 + 21)
    );
});
```

---

## Multi-pass post-processing

```csharp
// Two render targets for ping-pong:
RenderTarget2D _sceneTarget;   // scene renders here
RenderTarget2D _pingTarget;    // intermediate passes

// LoadContent():
_sceneTarget = new RenderTarget2D(GraphicsDevice, w, h);
_pingTarget  = new RenderTarget2D(GraphicsDevice, w, h);

// Draw() — pipeline:
// Pass 1: scene → _sceneTarget
GraphicsDevice.SetRenderTarget(_sceneTarget);
GraphicsDevice.Clear(Color.Black);
DrawScene();

// Pass 2: _sceneTarget → _pingTarget (blur)
GraphicsDevice.SetRenderTarget(_pingTarget);
_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, effect: _blurEffect);
_spriteBatch.Draw(_sceneTarget, fullScreen, Color.White);
_spriteBatch.End();

// Pass 3: _pingTarget → back buffer (vignette)
GraphicsDevice.SetRenderTarget(null);
_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, effect: _vignetteEffect);
_spriteBatch.Draw(_pingTarget, fullScreen, Color.White);
_spriteBatch.End();
```
