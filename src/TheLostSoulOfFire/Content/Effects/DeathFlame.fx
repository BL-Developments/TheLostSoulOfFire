// Death Flame colouring shared by every Death Flame effect: an intensity is mapped through
// the Death Flame ramp (dark violet → violet → near white). Two techniques:
//   Trail    — runtime ribbons (scythe arcs) with a scrolling flow texture;
//   Flipbook — greyscale flipbook frames drawn through SpriteBatch.
// The ramp texture is built from the colours in DeathFlameRamp.cs, so every source yields
// the same flame colour.
#include "Common.fxh"

float4x4 MatrixTransform;
float Time;
float FlowScale;
float FlowSpeed;

Texture2D SpriteTexture;
sampler2D SpriteSampler = sampler_state { Texture = <SpriteTexture>; };

Texture2D Ramp;
sampler2D RampSampler = sampler_state
{
    Texture = <Ramp>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D Flow;
sampler2D FlowSampler = sampler_state
{
    Texture = <Flow>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct TrailInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

SpriteVertexOutput TrailVS(TrailInput input)
{
    SpriteVertexOutput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

// x runs along the ribbon (0 at the tail, 1 at the blade), y across it (0..1).
float4 TrailPS(SpriteVertexOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float across = 1.0 - abs(uv.y * 2.0 - 1.0);
    float flow = tex2D(FlowSampler, float2(uv.x * FlowScale - Time * FlowSpeed, uv.y * 0.6)).r;
    float body = pow(saturate(across), 1.6) * (0.55 + 0.45 * flow);
    float intensity = saturate(body * (0.35 + 0.65 * uv.x) * input.Color.a * 1.25);
    float4 ramp = tex2D(RampSampler, float2(intensity, 0.5));
    return float4(ramp.rgb * ramp.a, ramp.a);
}

float4 FlipbookPS(SpriteVertexOutput input) : COLOR
{
    float4 frame = tex2D(SpriteSampler, input.TextureCoordinates);
    float intensity = saturate(dot(frame.rgb, float3(0.299, 0.587, 0.114)) / max(frame.a, 0.001));
    float4 ramp = tex2D(RampSampler, float2(intensity, 0.5));
    float alpha = ramp.a * frame.a * input.Color.a;
    return float4(ramp.rgb * alpha, alpha);
}

technique Trail
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL TrailVS();
        PixelShader = compile PS_SHADERMODEL TrailPS();
    }
};

technique Flipbook
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL FlipbookPS();
    }
};
