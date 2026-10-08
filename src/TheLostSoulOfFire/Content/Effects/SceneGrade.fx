// Grades the finished scene through two colour lookup tables: the LUT of the current
// area and the Soul Sense LUT, blended by the existing Soul Sense strength.
// LUTs are 32×32×32 strips (1024×32): x = blue slice * 32 + red, y = green.
#include "Common.fxh"

Texture2D SpriteTexture;
sampler2D SceneSampler = sampler_state { Texture = <SpriteTexture>; };

Texture2D AreaLut;
sampler2D AreaLutSampler = sampler_state
{
    Texture = <AreaLut>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D SoulSenseLut;
sampler2D SoulSenseLutSampler = sampler_state
{
    Texture = <SoulSenseLut>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float SoulSense;

static const float LutSize = 32.0;

// Texture coordinates of the two blue slices around a colour, and the blend between them.
// (Samplers cannot be passed to functions under ShadowDusk/DXC, so callers sample.)
void LutCoordinates(float3 color, out float2 low, out float2 high, out float amount)
{
    float3 c = saturate(color);
    float blue = c.b * (LutSize - 1.0);
    float slice0 = floor(blue);
    float slice1 = min(slice0 + 1.0, LutSize - 1.0);
    float2 uv = float2((c.r * (LutSize - 1.0) + 0.5) / (LutSize * LutSize),
                       (c.g * (LutSize - 1.0) + 0.5) / LutSize);
    low = uv + float2(slice0 / LutSize, 0.0);
    high = uv + float2(slice1 / LutSize, 0.0);
    amount = blue - slice0;
}

float4 GradePS(SpriteVertexOutput input) : COLOR
{
    float4 scene = tex2D(SceneSampler, input.TextureCoordinates) * input.Color;
    float2 low;
    float2 high;
    float amount;
    LutCoordinates(scene.rgb, low, high, amount);
    float3 area = lerp(tex2D(AreaLutSampler, low).rgb, tex2D(AreaLutSampler, high).rgb, amount);
    float3 sense = lerp(tex2D(SoulSenseLutSampler, low).rgb, tex2D(SoulSenseLutSampler, high).rgb, amount);
    return float4(lerp(area, sense, saturate(SoulSense)), scene.a);
}

technique Grade
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL GradePS();
    }
};
