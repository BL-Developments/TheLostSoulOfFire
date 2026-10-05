using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Central tuning surface for the restrained scene grade and supernatural light layer.
/// Art is painted and drawn with linear filtering at the output resolution (see RenderResolution).
/// </summary>
public static class SoulfireRenderSettings
{
    public static readonly Color SceneGrade = new(234, 228, 244);
    public static readonly Color GradeShadow = new(6, 5, 12);

    public const float GradeShadowOpacity = 0.08f;
    public const float VignetteOpacity = 0.28f;
    public const float SoulSenseVignetteBoost = 0.1f;
    public const float SoulSenseWorldVeilOpacity = 0.25f;
    public const float ResonanceVignetteReduction = 0.05f;

    public const int GlowTextureSize = 128;
    public const int VignetteTextureWidth = 256;
    public const int VignetteTextureHeight = 144;
    public const float GlowFalloff = 2.35f;

    public const float PlayerCoreGlowRadius = 46f;
    public const float PlayerCoreGlowIntensity = 0.25f;
    public const float ReadyCoreGlowRadius = 78f;
    public const float ReadyCoreGlowIntensity = 0.42f;
    public const float ResonanceGlowRadius = 128f;
    public const float ResonanceGlowIntensity = 0.34f;
    public const float SoulGlowRadius = 66f;
    public const float SoulGlowIntensity = 0.34f;
    public const float CannonGlowRadius = 54f;
    public const float CannonGlowIntensity = 0.38f;
    public const float DeathFlameGlowRadius = 92f;
    public const float DeathFlameGlowIntensity = 0.48f;
    public const float ParticleGlowRadiusMultiplier = 3.4f;
    public const float ParticleGlowIntensity = 0.17f;
}

/// <summary>
/// A deliberately small render foundation: one output-sized scene target, one procedural glow
/// texture, and one procedural vignette. UI is drawn after this renderer has finished.
/// </summary>
public sealed class SoulfireRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BlendState _lightBlend;
    private readonly Effect? _sceneGrade;
    private List<SceneLight> _recordedLights = [];
    private List<SceneLight> _lastFrameLights = [];
    private RenderTarget2D _sceneTarget;
    private Texture2D _solidTexture;
    private Texture2D _glowTexture;
    private Texture2D _vignetteTexture;
    private int _targetWidth;
    private int _targetHeight;

    public SoulfireRenderer(GraphicsDevice graphicsDevice, ContentManager? content = null)
    {
        _graphicsDevice = graphicsDevice;
        _sceneGrade = TryLoadEffect(content, "Effects/SceneGrade");
        _lightBlend = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };
        _solidTexture = new Texture2D(graphicsDevice, 1, 1);
        _solidTexture.SetData([Color.White]);
        _glowTexture = CreateGlowTexture(graphicsDevice);
        _vignetteTexture = CreateVignetteTexture(graphicsDevice);
    }

    /// <summary>
    /// The scene target is always output-sized; callers keep passing the logical viewport and
    /// draw through <see cref="RenderResolution.ScaleMatrix"/>.
    /// </summary>
    public void BeginScene(Viewport viewport)
    {
        // Glows recorded by the previous frame's lighting pass light this frame's figures.
        (_lastFrameLights, _recordedLights) = (_recordedLights, _lastFrameLights);
        _recordedLights.Clear();
        EnsureSceneTarget(RenderResolution.OutputWidth, RenderResolution.OutputHeight);
        _graphicsDevice.SetRenderTarget(_sceneTarget);
        _graphicsDevice.Clear(GameBalance.VoidColor);
    }

    /// <summary>
    /// Composites the scene target with the area grade (<paramref name="areaLut"/>) blended toward
    /// the Soul Sense grade by <paramref name="soulSenseWorldSuppression"/>. HUD and menus are drawn
    /// afterwards and stay ungraded. Without LUTs or shader the scene is drawn as before.
    /// </summary>
    public void PresentScene(
        SpriteBatch batch,
        RenderTarget2D? rootTarget,
        Viewport viewport,
        float soulSenseWorldSuppression = 0f,
        Texture2D? areaLut = null,
        Texture2D? soulSenseLut = null)
    {
        _graphicsDevice.SetRenderTarget(rootTarget);
        _graphicsDevice.Clear(GameBalance.VoidColor);

        Rectangle destination = RenderResolution.OutputBounds;
        float suppression = MathHelper.Clamp(soulSenseWorldSuppression, 0f, 1f);
        Color sceneGrade = Color.Lerp(
            SoulfireRenderSettings.SceneGrade,
            GameBalance.SoulSenseWorldGrade,
            suppression);
        Effect? grade = _sceneGrade is not null && areaLut is not null ? _sceneGrade : null;
        if (grade is not null)
        {
            grade.Parameters["AreaLut"].SetValue(areaLut);
            grade.Parameters["SoulSenseLut"].SetValue(soulSenseLut ?? areaLut);
            grade.Parameters["SoulSense"].SetValue(soulSenseLut is null ? 0f : suppression);
        }
        batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, effect: grade);
        batch.Draw(_sceneTarget, destination, sceneGrade);
        batch.End();

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        batch.Draw(
            _solidTexture,
            destination,
            SoulfireRenderSettings.GradeShadow * SoulfireRenderSettings.GradeShadowOpacity);
        if (suppression > 0f)
        {
            batch.Draw(
                _solidTexture,
                destination,
                GameBalance.SoulSenseWorldVeil * (SoulfireRenderSettings.SoulSenseWorldVeilOpacity * suppression));
        }
        batch.End();
    }

    public void BeginLighting(SpriteBatch batch, Matrix worldTransform) =>
        batch.Begin(
            SpriteSortMode.Deferred,
            _lightBlend,
            SamplerState.LinearClamp,
            transformMatrix: worldTransform);

    /// <summary>
    /// Every glow of the previous frame's lighting pass, in world units. Figures with normal maps
    /// use these as their Soulfire point lights (see <see cref="SpriteLighting"/>).
    /// </summary>
    public IReadOnlyList<SceneLight> SceneLights => _lastFrameLights;

    public void DrawGlow(SpriteBatch batch, Vector2 position, float radius, Color color, float intensity)
    {
        _recordedLights.Add(new SceneLight(position, radius, color, MathHelper.Clamp(intensity, 0f, 1f)));
        float diameter = MathF.Max(1f, radius * 2f);
        batch.Draw(
            _glowTexture,
            position,
            null,
            color * MathHelper.Clamp(intensity, 0f, 1f),
            0f,
            new Vector2(_glowTexture.Width, _glowTexture.Height) * 0.5f,
            diameter / _glowTexture.Width,
            SpriteEffects.None,
            0f);
    }

    public void DrawVignette(SpriteBatch batch, Viewport viewport, float soulSenseAmount, bool resonanceActive)
    {
        float opacity = SoulfireRenderSettings.VignetteOpacity +
            SoulfireRenderSettings.SoulSenseVignetteBoost * MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        if (resonanceActive)
        {
            opacity -= SoulfireRenderSettings.ResonanceVignetteReduction;
        }

        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        batch.Draw(
            _vignetteTexture,
            RenderResolution.OutputBounds,
            Color.White * MathHelper.Clamp(opacity, 0f, 1f));
        batch.End();
    }

    private static Effect? TryLoadEffect(ContentManager? content, string path)
    {
        if (content is null)
        {
            return null;
        }

        try
        {
            return content.Load<Effect>(path);
        }
        catch (Exception exception) when (exception is ContentLoadException or NoSuitableGraphicsDeviceException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Shader {path} nicht geladen: {exception.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        _sceneTarget?.Dispose();
        _solidTexture.Dispose();
        _glowTexture.Dispose();
        _vignetteTexture.Dispose();
        _lightBlend.Dispose();
        GC.SuppressFinalize(this);
    }

    private void EnsureSceneTarget(int width, int height)
    {
        if (_sceneTarget is not null && _targetWidth == width && _targetHeight == height)
        {
            return;
        }

        _sceneTarget?.Dispose();
        _targetWidth = width;
        _targetHeight = height;
        _sceneTarget = new RenderTarget2D(
            _graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
    }

    private static Texture2D CreateGlowTexture(GraphicsDevice graphicsDevice)
    {
        int size = SoulfireRenderSettings.GlowTextureSize;
        Texture2D texture = new(graphicsDevice, size, size, false, SurfaceFormat.Color);
        Color[] data = new Color[size * size];
        Vector2 center = new((size - 1) * 0.5f);
        float inverseRadius = 1f / (size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) * inverseRadius;
                float glow = MathF.Pow(1f - MathHelper.Clamp(distance, 0f, 1f), SoulfireRenderSettings.GlowFalloff);
                byte value = (byte)MathF.Round(glow * 255f);
                // Premultiplied white lets the custom One/One blend add light without dark fringes.
                data[y * size + x] = new Color(value, value, value, value);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateVignetteTexture(GraphicsDevice graphicsDevice)
    {
        int width = SoulfireRenderSettings.VignetteTextureWidth;
        int height = SoulfireRenderSettings.VignetteTextureHeight;
        Texture2D texture = new(graphicsDevice, width, height, false, SurfaceFormat.Color);
        Color[] data = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float normalizedY = (y + 0.5f) / height * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float normalizedX = (x + 0.5f) / width * 2f - 1f;
                float distance = MathF.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY * 0.82f);
                float edge = SmoothStep(0.48f, 1.24f, distance);
                byte alpha = (byte)MathF.Round(edge * 255f);
                data[y * width + x] = new Color(0, 0, 0, (int)alpha);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private static float SmoothStep(float minimum, float maximum, float value)
    {
        float amount = MathHelper.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
        return amount * amount * (3f - 2f * amount);
    }
}
