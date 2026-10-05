// Lights a figure that has a normal map: a key light from the upper left plus up to eight
// Soulfire point lights taken from the scene's glow lights. A flat normal (0, 0, 1) leaves
// the colour exactly as painted (Ambient + KeyLightStrength * KeyLightDirection.z = 1).
// Normal maps are tangent space with +X right, +Y up (Blender/OpenGL convention), +Z toward
// the camera; screen space has +Y down, so Y is flipped here.
#include "Common.fxh"

Texture2D SpriteTexture;
sampler2D ColorSampler = sampler_state { Texture = <SpriteTexture>; };

Texture2D NormalMap;
sampler2D NormalSampler = sampler_state
{
    Texture = <NormalMap>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 SpriteWorldRect;   // left, top, width, height of the drawn frame in world units
float4 FrameUvRect;       // u0, v0, du, dv of the frame inside the sheet

float3 KeyLightDirection; // normalised, pointing toward the light
float KeyLightStrength;
float Ambient;

float4 LightPositions[8]; // xy world position, z height above the floor, w radius
float4 LightColors[8];    // rgb colour times intensity; zero for unused slots
float PointLightStrength;
float PointLightSpill;    // share of the light added on top, so dark cloth still catches colour
float MaxPointLight;      // cap that keeps faces, hands and weapons from blowing out

float4 LitPS(SpriteVertexOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float4 albedo = tex2D(ColorSampler, uv) * input.Color;

    float3 n = tex2D(NormalSampler, uv).xyz * 2.0 - 1.0;
    n.y = -n.y;
    n = normalize(n);

    float2 local = (uv - FrameUvRect.xy) / FrameUvRect.zw;
    float2 world = SpriteWorldRect.xy + local * SpriteWorldRect.zw;

    float key = Ambient + KeyLightStrength * saturate(dot(n, KeyLightDirection));

    float3 pointLight = float3(0.0, 0.0, 0.0);
    for (int i = 0; i < 8; i++)
    {
        float3 toLight = float3(LightPositions[i].xy - world, LightPositions[i].z);
        float distance = length(toLight.xy);
        float attenuation = saturate(1.0 - distance / max(LightPositions[i].w, 1.0));
        attenuation *= attenuation;
        pointLight += LightColors[i].rgb * attenuation * saturate(dot(n, normalize(toLight)));
    }
    pointLight = min(pointLight * PointLightStrength, float3(MaxPointLight, MaxPointLight, MaxPointLight));

    float3 lit = albedo.rgb * (key + pointLight) + pointLight * PointLightSpill * albedo.a;
    return float4(lit, albedo.a);
}

technique Lit
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL LitPS();
    }
};
