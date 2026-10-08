// Dissolves a defeated figure's last pose from its edges inward into violet Death Flame
// fragments. Progress runs 0 → 1; a noise mask breaks the edge into fragments and a thin
// band at the dissolving edge glows in Death Flame colour.
#include "Common.fxh"

Texture2D SpriteTexture;
sampler2D ColorSampler = sampler_state { Texture = <SpriteTexture>; };

Texture2D NoiseTexture;
sampler2D NoiseSampler = sampler_state
{
    Texture = <NoiseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

float4 FrameUvRect;   // u0, v0, du, dv of the frame inside the sheet
float Progress;       // 0 = whole figure, 1 = gone
float EdgeWidth;
float3 EdgeColor;     // Death Flame bright
float3 EdgeCore;      // near white

float4 DissolvePS(SpriteVertexOutput input) : COLOR
{
    float4 color = tex2D(ColorSampler, input.TextureCoordinates) * input.Color;
    float2 local = (input.TextureCoordinates - FrameUvRect.xy) / FrameUvRect.zw;

    // Outer pixels carry low values and go first; noise breaks the front into fragments.
    float centre = 1.0 - saturate(length(local - 0.5) * 2.0);
    float noise = tex2D(NoiseSampler, local * 2.3).r;
    float value = noise * 0.55 + centre * 0.45;
    float threshold = Progress * 1.08 - 0.04;

    float keep = step(threshold, value);
    float edge = 1.0 - saturate((value - threshold) / EdgeWidth);
    float3 glow = lerp(EdgeColor, EdgeCore, edge * edge);
    float3 rgb = lerp(color.rgb, glow * color.a, edge * step(0.001, Progress));
    return float4(rgb, color.a) * keep;
}

technique Dissolve
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL DissolvePS();
    }
};
