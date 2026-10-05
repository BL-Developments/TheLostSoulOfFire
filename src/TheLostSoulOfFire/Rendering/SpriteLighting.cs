using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>A light of the scene in world units, as drawn by the Soulfire lighting pass.</summary>
public readonly record struct SceneLight(Vector2 Position, float Radius, Color Color, float Intensity);

/// <summary>
/// Draws figures that have a normal map through <c>SpriteLit.fx</c>: a key light from the
/// upper left and up to eight of the scene's Soulfire lights. Figures without a normal map
/// never come here and look exactly as before.
/// </summary>
public sealed class SpriteLighting
{
    public const int MaxPointLights = 8;

    /// <summary>Toward the light: up and to the left on screen (y down), tilted toward the camera.</summary>
    public static readonly Vector3 KeyLightDirection = Vector3.Normalize(new Vector3(-0.55f, -0.55f, 0.63f));
    public const float KeyLightStrength = 0.55f;

    /// <summary>Ambient term chosen so a flat normal map leaves the painted colour unchanged.</summary>
    public static float Ambient => 1f - KeyLightStrength * KeyLightDirection.Z;

    public const float LightHeight = 46f;
    public const float LightRadiusScale = 1.8f;
    public const float PointLightStrength = 1.6f;
    public const float PointLightSpill = 0.35f;
    public const float MaxPointLight = 0.7f;

    private readonly Effect _effect;
    private readonly Vector4[] _positions = new Vector4[MaxPointLights];
    private readonly Vector4[] _colors = new Vector4[MaxPointLights];
    private readonly SceneLight[] _chosen = new SceneLight[MaxPointLights];
    private Matrix _transform;
    private IReadOnlyList<SceneLight> _lights = [];

    public SpriteLighting(Effect effect)
    {
        _effect = effect;
    }

    /// <summary>Called once per scene pass, after <c>batch.Begin</c> with <paramref name="transform"/>.</summary>
    public void BeginScene(Matrix transform, IReadOnlyList<SceneLight> lights)
    {
        _transform = transform;
        _lights = lights;
    }

    /// <summary>
    /// Draws one frame lit. The running deferred batch is flushed and restarted with the same
    /// settings the scene uses, so the draw order of everything else is unchanged.
    /// </summary>
    public void Draw(
        SpriteBatch batch,
        SpriteClip clip,
        Rectangle source,
        Vector2 position,
        float scale,
        Color color)
    {
        Vector2 size = new Vector2(source.Width, source.Height) * scale;
        Vector2 topLeft = position - clip.PixelOrigin * scale;
        int count = SelectLights(_lights, position, MathF.Min(size.X, size.Y) * 0.3f, _chosen);
        for (int index = 0; index < MaxPointLights; index++)
        {
            if (index < count)
            {
                SceneLight light = _chosen[index];
                _positions[index] = new Vector4(light.Position, LightHeight, light.Radius * LightRadiusScale);
                _colors[index] = new Vector4(light.Color.ToVector3() * light.Intensity, 0f);
            }
            else
            {
                _positions[index] = new Vector4(0f, 0f, LightHeight, 1f);
                _colors[index] = Vector4.Zero;
            }
        }

        Texture2D texture = clip.Texture;
        EffectParameterCollection parameters = _effect.Parameters;
        parameters["NormalMap"].SetValue(clip.NormalMap);
        parameters["SpriteWorldRect"].SetValue(new Vector4(topLeft, size.X, size.Y));
        parameters["FrameUvRect"].SetValue(new Vector4(
            (float)source.X / texture.Width,
            (float)source.Y / texture.Height,
            (float)source.Width / texture.Width,
            (float)source.Height / texture.Height));
        parameters["KeyLightDirection"].SetValue(KeyLightDirection);
        parameters["KeyLightStrength"].SetValue(KeyLightStrength);
        parameters["Ambient"].SetValue(Ambient);
        parameters["LightPositions"].SetValue(_positions);
        parameters["LightColors"].SetValue(_colors);
        parameters["PointLightStrength"].SetValue(PointLightStrength);
        parameters["PointLightSpill"].SetValue(PointLightSpill);
        parameters["MaxPointLight"].SetValue(MaxPointLight);

        batch.End();
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, _effect, _transform);
        batch.Draw(texture, position, source, color, 0f, clip.PixelOrigin, scale, SpriteEffects.None, 0f);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: _transform);
    }

    /// <summary>
    /// Picks the lights that reach <paramref name="center"/> most strongly, strongest first.
    /// Lights inside <paramref name="ignoreRadius"/> belong to the figure itself (its own core
    /// glow) and are skipped. Returns how many entries of <paramref name="chosen"/> are set.
    /// </summary>
    public static int SelectLights(IReadOnlyList<SceneLight> lights, Vector2 center, float ignoreRadius, Span<SceneLight> chosen)
    {
        Span<float> weights = stackalloc float[chosen.Length];
        int count = 0;
        foreach (SceneLight light in lights)
        {
            float distance = Vector2.Distance(light.Position, center);
            float reach = light.Radius * LightRadiusScale;
            if (distance < ignoreRadius || distance >= reach || light.Intensity <= 0f)
            {
                continue;
            }

            float falloff = 1f - distance / reach;
            float weight = light.Intensity * falloff * falloff * light.Color.ToVector3().Length();
            int slot = count < chosen.Length ? count++ : chosen.Length;
            if (slot == chosen.Length)
            {
                if (weight <= weights[count - 1])
                {
                    continue;
                }
                slot = count - 1;
            }

            // Insertion keeps the array ordered by weight, strongest first.
            while (slot > 0 && weights[slot - 1] < weight)
            {
                weights[slot] = weights[slot - 1];
                chosen[slot] = chosen[slot - 1];
                slot--;
            }
            weights[slot] = weight;
            chosen[slot] = light;
        }

        return count;
    }
}
