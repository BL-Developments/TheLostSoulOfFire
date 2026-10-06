using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// The Death Flame ramp ("Verlaufstabelle"): one colour per intensity from 0 to 1, shared by
/// every Death Flame effect. The style bible refers to these stops.
/// </summary>
public static class DeathFlameRamp
{
    public const int Width = 256;

    /// <summary>Intensity, colour and opacity, in ascending intensity.</summary>
    public static readonly (float At, Color Color, float Alpha)[] Stops =
    [
        (0f, GameBalance.DeepViolet, 0f),
        (0.3f, GameBalance.DeepViolet, 0.55f),
        (0.6f, GameBalance.DeathFlame, 0.9f),
        (0.85f, GameBalance.DeathFlameBright, 1f),
        (1f, GameBalance.SoulWhite, 1f)
    ];

    /// <summary>Straight (not premultiplied) colour and opacity at <paramref name="intensity"/>.</summary>
    public static (Color Color, float Alpha) Evaluate(float intensity)
    {
        float value = MathHelper.Clamp(intensity, 0f, 1f);
        for (int index = 1; index < Stops.Length; index++)
        {
            (float at, Color color, float alpha) = Stops[index];
            (float previousAt, Color previousColor, float previousAlpha) = Stops[index - 1];
            if (value <= at)
            {
                float amount = (value - previousAt) / MathF.Max(at - previousAt, 0.0001f);
                return (Color.Lerp(previousColor, color, amount), MathHelper.Lerp(previousAlpha, alpha, amount));
            }
        }

        return (Stops[^1].Color, Stops[^1].Alpha);
    }

    public static Color[] Pixels()
    {
        Color[] pixels = new Color[Width];
        for (int x = 0; x < Width; x++)
        {
            (Color color, float alpha) = Evaluate(x / (Width - 1f));
            pixels[x] = new Color(color.R, color.G, color.B, (byte)MathF.Round(alpha * 255f));
        }

        return pixels;
    }
}

/// <summary>
/// Draws Death Flame through <c>DeathFlame.fx</c>: ribbons along a path (scythe arcs, test
/// trails) and greyscale flipbooks, both coloured by <see cref="DeathFlameRamp"/>.
/// </summary>
public sealed class DeathFlameRenderer
{
    private const int FlowSize = 128;

    private readonly GraphicsDevice _device;
    private readonly Effect _effect;
    private readonly Texture2D _ramp;
    private readonly Texture2D _flow;
    private VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[64];

    public DeathFlameRenderer(GraphicsDevice device, Effect effect)
    {
        _device = device;
        _effect = effect;
        _ramp = new Texture2D(device, DeathFlameRamp.Width, 1);
        _ramp.SetData(DeathFlameRamp.Pixels());
        _flow = CreateFlowTexture(device);
    }

    /// <summary>
    /// Draws a ribbon of <paramref name="width"/> world units along <paramref name="path"/>
    /// (tail first). Ends and restarts the running deferred scene batch.
    /// </summary>
    public void DrawTrail(SpriteBatch batch, Matrix sceneTransform, IReadOnlyList<Vector2> path, float width, float opacity, float time)
    {
        if (path.Count < 2)
        {
            return;
        }

        int vertexCount = path.Count * 2;
        if (_vertices.Length < vertexCount)
        {
            _vertices = new VertexPositionColorTexture[vertexCount];
        }

        for (int index = 0; index < path.Count; index++)
        {
            Vector2 previous = path[Math.Max(index - 1, 0)];
            Vector2 next = path[Math.Min(index + 1, path.Count - 1)];
            Vector2 tangent = next - previous;
            tangent = tangent.LengthSquared() > 0.0001f ? Vector2.Normalize(tangent) : Vector2.UnitX;
            Vector2 normal = new(-tangent.Y, tangent.X);
            float along = index / (path.Count - 1f);
            float halfWidth = width * 0.5f * MathF.Sin(MathF.PI * MathHelper.Clamp(along * 0.92f + 0.08f, 0f, 1f));
            Color color = Color.White * opacity;
            _vertices[index * 2] = new VertexPositionColorTexture(new Vector3(path[index] + normal * halfWidth, 0f), color, new Vector2(along, 0f));
            _vertices[index * 2 + 1] = new VertexPositionColorTexture(new Vector3(path[index] - normal * halfWidth, 0f), color, new Vector2(along, 1f));
        }

        batch.End();
        Viewport viewport = _device.Viewport;
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, 0f, -1f);
        _effect.CurrentTechnique = _effect.Techniques["Trail"];
        _effect.Parameters["MatrixTransform"].SetValue(sceneTransform * projection);
        _effect.Parameters["Time"].SetValue(time);
        _effect.Parameters["FlowScale"].SetValue(2.2f);
        _effect.Parameters["FlowSpeed"].SetValue(1.6f);
        _effect.Parameters["Ramp"].SetValue(_ramp);
        _effect.Parameters["Flow"].SetValue(_flow);
        _device.BlendState = BlendState.AlphaBlend;
        _device.DepthStencilState = DepthStencilState.None;
        _device.RasterizerState = RasterizerState.CullNone;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, vertexCount - 2);
        }
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: sceneTransform);
    }

    /// <summary>
    /// A scythe slash along <paramref name="path"/> (tail first): thin and dim at the tail, widest
    /// just behind the blade, rounded at the head. <paramref name="heat"/> lifts the intensity, so a
    /// heavier stroke burns toward white. Ends and restarts the running deferred scene batch.
    /// </summary>
    /// <remarks>
    /// <paramref name="mask"/> (one value per path point, 0–1) hides parts of the slash, so a
    /// sweep can be drawn in two passes: the part behind a figure before it, the rest after it.
    /// </remarks>
    public void DrawSlash(SpriteBatch batch, Matrix sceneTransform, IReadOnlyList<Vector2> path, float headWidth, float opacity, float heat, float time,
        IReadOnlyList<float>? mask = null)
    {
        if (path.Count < 2)
        {
            return;
        }

        int vertexCount = path.Count * 2;
        if (_vertices.Length < vertexCount)
        {
            _vertices = new VertexPositionColorTexture[vertexCount];
        }

        for (int index = 0; index < path.Count; index++)
        {
            Vector2 previous = path[Math.Max(index - 1, 0)];
            Vector2 next = path[Math.Min(index + 1, path.Count - 1)];
            Vector2 tangent = next - previous;
            tangent = tangent.LengthSquared() > 0.0001f ? Vector2.Normalize(tangent) : Vector2.UnitX;
            Vector2 normal = new(-tangent.Y, tangent.X);
            float along = index / (path.Count - 1f);
            float body = MathF.Pow(along, 0.8f);
            float head = along > 0.86f ? MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow((along - 0.86f) / 0.14f, 2f))) : 1f;
            float halfWidth = headWidth * 0.5f * MathF.Max(0.06f, body * head);
            float intensity = MathHelper.Clamp(opacity * (0.25f + 0.75f * along) * heat, 0f, 1f);
            if (mask is not null)
            {
                intensity *= MathHelper.Clamp(mask[index], 0f, 1f);
            }
            Color color = new(1f, 1f, 1f, intensity);
            _vertices[index * 2] = new VertexPositionColorTexture(new Vector3(path[index] + normal * halfWidth, 0f), color, new Vector2(along, 0f));
            _vertices[index * 2 + 1] = new VertexPositionColorTexture(new Vector3(path[index] - normal * halfWidth, 0f), color, new Vector2(along, 1f));
        }

        DrawStrip(batch, sceneTransform, vertexCount, time, 3.2f, 2.4f);
    }

    private void DrawStrip(SpriteBatch batch, Matrix sceneTransform, int vertexCount, float time, float flowScale, float flowSpeed)
    {
        batch.End();
        Viewport viewport = _device.Viewport;
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, 0f, -1f);
        _effect.CurrentTechnique = _effect.Techniques["Trail"];
        _effect.Parameters["MatrixTransform"].SetValue(sceneTransform * projection);
        _effect.Parameters["Time"].SetValue(time);
        _effect.Parameters["FlowScale"].SetValue(flowScale);
        _effect.Parameters["FlowSpeed"].SetValue(flowSpeed);
        _effect.Parameters["Ramp"].SetValue(_ramp);
        _effect.Parameters["Flow"].SetValue(_flow);
        _device.BlendState = BlendState.AlphaBlend;
        _device.DepthStencilState = DepthStencilState.None;
        _device.RasterizerState = RasterizerState.CullNone;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, vertexCount - 2);
        }
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: sceneTransform);
    }

    /// <summary>Draws a greyscale flipbook frame coloured by the ramp. Ends and restarts the scene batch.</summary>
    public void DrawFlipbook(SpriteBatch batch, Matrix sceneTransform, SpriteClip clip, float elapsed, Vector2 position, float rotation, float scale, float opacity)
    {
        _effect.CurrentTechnique = _effect.Techniques["Flipbook"];
        _effect.Parameters["Ramp"].SetValue(_ramp);
        batch.End();
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, _effect, sceneTransform);
        batch.Draw(clip.Texture, position, clip.GetSourceRectangle(elapsed), Color.White * opacity, rotation, clip.PixelOrigin, scale, SpriteEffects.None, 0f);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: sceneTransform);
    }

    /// <summary>Streaky, tileable flow noise: stretched along x so it reads as moving flame.</summary>
    private static Texture2D CreateFlowTexture(GraphicsDevice device)
    {
        Random random = new(4242);
        const int cellsX = 4;
        const int cellsY = 16;
        float[,] grid = new float[cellsX, cellsY];
        for (int y = 0; y < cellsY; y++)
        {
            for (int x = 0; x < cellsX; x++)
            {
                grid[x, y] = (float)random.NextDouble();
            }
        }

        Color[] data = new Color[FlowSize * FlowSize];
        for (int y = 0; y < FlowSize; y++)
        {
            for (int x = 0; x < FlowSize; x++)
            {
                float gx = (float)x / FlowSize * cellsX;
                float gy = (float)y / FlowSize * cellsY;
                int x0 = (int)gx, y0 = (int)gy;
                float tx = MathHelper.SmoothStep(0f, 1f, gx - x0);
                float ty = MathHelper.SmoothStep(0f, 1f, gy - y0);
                float top = MathHelper.Lerp(grid[x0 % cellsX, y0 % cellsY], grid[(x0 + 1) % cellsX, y0 % cellsY], tx);
                float bottom = MathHelper.Lerp(grid[x0 % cellsX, (y0 + 1) % cellsY], grid[(x0 + 1) % cellsX, (y0 + 1) % cellsY], tx);
                byte value = (byte)MathF.Round(MathHelper.Lerp(top, bottom, ty) * 255f);
                data[y * FlowSize + x] = new Color(value, value, value, (byte)255);
            }
        }

        Texture2D texture = new(device, FlowSize, FlowSize);
        texture.SetData(data);
        return texture;
    }
}
