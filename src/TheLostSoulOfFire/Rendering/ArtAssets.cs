using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Rendering;

public sealed record SpriteClip(
    Texture2D Texture,
    int FrameWidth,
    int FrameHeight,
    int FrameCount,
    float FramesPerSecond,
    bool Loop)
{
    /// <summary>Normal map in the same sheet layout, if the registry names one.</summary>
    public Texture2D? NormalMap { get; init; }

    /// <summary>Pivot as a fraction of the frame; (0.5, 0.5) is the centre.</summary>
    public Vector2 Origin { get; init; } = new(0.5f);

    public float Duration => FrameCount / FramesPerSecond;

    public int GetFrameIndex(float elapsed)
    {
        int frame = (int)(MathF.Max(0f, elapsed) * FramesPerSecond);
        return Loop ? frame % FrameCount : Math.Min(frame, FrameCount - 1);
    }

    public Rectangle GetSourceRectangle(float elapsed)
    {
        int index = GetFrameIndex(elapsed);
        int columns = Texture.Width / FrameWidth;
        return new Rectangle(
            index % columns * FrameWidth,
            index / columns * FrameHeight,
            FrameWidth,
            FrameHeight);
    }

    public Vector2 PixelOrigin => new(FrameWidth * Origin.X, FrameHeight * Origin.Y);
}

public sealed class SpritePlayback
{
    private string _clipKey = string.Empty;
    private float _startedAt;

    public float Elapsed(string clipKey, float globalTime)
    {
        if (!string.Equals(_clipKey, clipKey, StringComparison.Ordinal))
        {
            _clipKey = clipKey;
            _startedAt = globalTime;
        }

        return MathF.Max(0f, globalTime - _startedAt);
    }

    public bool IsComplete(SpriteClip clip, string clipKey, float globalTime) =>
        !clip.Loop && Elapsed(clipKey, globalTime) >= clip.Duration;
}

/// <summary>
/// Loads and draws graphics by Visual-ID. Which textures, frames, sizes and normal maps
/// belong to an ID comes only from <see cref="VisualRegistry"/>; anything missing is drawn
/// as a dummy and listed once in <see cref="MissingVisuals"/>.
/// </summary>
public sealed class ArtAssets
{
    private const float FallbackCharacterSize = 100f;
    private const float EffectDummyDuration = 0.45f;

    private readonly Dictionary<string, SpriteClip?> _clips = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<object, SpritePlayback> _playbacks = new();
    private readonly List<string> _missing = [];
    private readonly HashSet<string> _missingSet = new(StringComparer.Ordinal);
    private readonly Texture2D _pixel;
    private float _time;

    public ArtAssets(ContentManager content)
        : this(content, LoadRegistry(out string? error))
    {
        RegistryError = error;
        if (error is not null)
        {
            Console.Error.WriteLine(error);
        }
    }

    public ArtAssets(ContentManager content, VisualRegistry registry)
    {
        Registry = registry;
        GraphicsDevice device = ((IGraphicsDeviceService)content.ServiceProvider.GetService(typeof(IGraphicsDeviceService))!).GraphicsDevice;
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData([Color.White]);

        foreach (VisualEntry entry in registry.Entries)
        {
            foreach (VisualClipDefinition clip in entry.Clips.Values)
            {
                if (clip.IsDirectional)
                {
                    foreach (string direction in VisualDirections.All)
                    {
                        _clips[ClipKey(entry.Id, clip.Name, direction)] = LoadClip(content, entry, clip, direction);
                    }
                }
                else
                {
                    _clips[ClipKey(entry.Id, clip.Name, null)] = LoadClip(content, entry, clip, null);
                }
            }
        }
    }

    public VisualRegistry Registry { get; }

    /// <summary>Set when the registry file could not be read; the game then draws dummies.</summary>
    public string? RegistryError { get; }

    /// <summary>Missing Visual-IDs (<c>id</c>), clips (<c>id/clip</c>) and textures (<c>id/clip/dir</c>), each once, in order of discovery.</summary>
    public IReadOnlyList<string> MissingVisuals => _missing;

    public void Update(float deltaTime) => _time += MathF.Max(0f, deltaTime);

    /// <summary>The clip for an effect or sprite, or <c>null</c> (reported as missing) when there is none.</summary>
    public SpriteClip? GetEffect(string id) => Resolve(id, VisualClips.Default, null, out _);

    public Texture2D? GetSpriteTexture(string id) => GetEffect(id)?.Texture;

    public Vector2 WorldSizeOf(string id, Vector2 fallback) =>
        Registry.TryGet(id, out VisualEntry entry) ? entry.WorldSize : fallback;

    public void DrawEnvironment(SpriteBatch batch, string id, Vector2 position)
    {
        SpriteClip? clip = GetEffect(id);
        Vector2 size = WorldSizeOf(id, new Vector2(1800f, 1000f));
        Vector2 origin = Registry.TryGet(id, out VisualEntry entry) ? entry.Origin : Vector2.Zero;
        Vector2 topLeft = position - origin * size;
        Rectangle bounds = new((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y);
        if (clip is null)
        {
            batch.DrawPropDummy(_pixel, bounds, position + (new Vector2(0.5f, 1f) - origin) * size);
            return;
        }

        batch.Draw(clip.Texture, bounds, clip.GetSourceRectangle(0f), Color.White);
    }

    public void DrawPlayer(SpriteBatch batch, Player player)
    {
        if (player.IsDead)
        {
            return;
        }

        string clip = player.Velocity.LengthSquared() > 120f ? VisualClips.Move : VisualClips.Idle;
        DrawCharacter(batch, player, VisualIds.Player, clip, player.FacingDirection, player.Position, 1f, Color.White);
    }

    public void DrawEnemy(SpriteBatch batch, Enemy enemy)
    {
        if (enemy.VisualId is not { } id || enemy.VisualClip is not { } clip)
        {
            return;
        }

        Color tint = enemy.HitFlashRemaining > 0f ? new Color(255, 235, 255) : Color.White;
        DrawCharacter(batch, enemy, id, clip, enemy.VisualFacing, enemy.Position, enemy.VisualScale, tint, enemy.Radius * 2.6f);
    }

    public void DrawCharacter(
        SpriteBatch batch,
        object owner,
        string id,
        string clipName,
        Vector2 facing,
        Vector2 position,
        float sizeScale,
        Color tint,
        float fallbackSize = FallbackCharacterSize)
    {
        string direction = VisualDirections.FromVector(facing);
        Vector2 worldSize = WorldSizeOf(id, new Vector2(fallbackSize)) * sizeScale;
        SpriteClip? clip = Resolve(id, clipName, direction, out string resolvedName);
        if (clip is null)
        {
            batch.DrawCharacterDummy(_pixel, position, worldSize, facing, tint);
            return;
        }

        SpritePlayback playback = _playbacks.GetValue(owner, _ => new SpritePlayback());
        float elapsed = playback.Elapsed(ClipKey(id, resolvedName, direction), _time);
        DrawClip(batch, clip, elapsed, position, 0f, worldSize.X / clip.FrameWidth, tint);
    }

    public void DrawSprite(SpriteBatch batch, string id, Vector2 position, float scale, Color color)
    {
        SpriteClip? clip = GetEffect(id);
        Vector2 worldSize = WorldSizeOf(id, new Vector2(64f)) * scale;
        if (clip is null)
        {
            Vector2 topLeft = position - worldSize * 0.5f;
            batch.DrawPropDummy(_pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)worldSize.X, (int)worldSize.Y), position + new Vector2(0f, worldSize.Y * 0.5f));
            return;
        }

        DrawClip(batch, clip, _time, position, 0f, worldSize.X / clip.FrameWidth, color);
    }

    public void DrawLostSoul(SpriteBatch batch, Soul soul)
    {
        if (soul.State is SoulState.Released or SoulState.Consumed or SoulState.Residue)
        {
            return;
        }

        float pulse = 0.94f + MathF.Sin(_time * 5f) * 0.07f;
        DrawSprite(batch, VisualIds.LostSoul, soul.Position, pulse, Color.White);
    }

    public void DrawLifeFlame(SpriteBatch batch, Vector2 position, float alpha, float scale) =>
        DrawSprite(batch, VisualIds.LifeFlame, position, scale, Color.White * alpha);

    public void DrawLoopingEffect(
        SpriteBatch batch,
        object owner,
        string id,
        Vector2 position,
        float rotation,
        float scale,
        Color color)
    {
        SpritePlayback playback = _playbacks.GetValue(owner, _ => new SpritePlayback());
        float elapsed = playback.Elapsed($"effect/{id}", _time);
        SpriteClip? clip = GetEffect(id);
        if (clip is null)
        {
            DrawEffectDummy(batch, id, position, rotation, scale, elapsed % EffectDummyDuration / EffectDummyDuration);
            return;
        }

        DrawClip(batch, clip, elapsed, position, rotation, scale * WorldSizeOf(id, new Vector2(clip.FrameWidth)).X / clip.FrameWidth, color);
    }

    /// <summary>Stand-in for an effect without graphics, sized from the registry or a default.</summary>
    public void DrawEffectDummy(SpriteBatch batch, string id, Vector2 position, float rotation, float scale, float progress)
    {
        float radius = WorldSizeOf(id, new Vector2(128f)).X * 0.5f * scale;
        batch.DrawEffectDummy(_pixel, position, radius, rotation, progress);
    }

    public static float EffectDummyLifetime => EffectDummyDuration;

    public void DrawCannonProjectile(SpriteBatch batch, CannonShot shot)
    {
        float rotation = MathF.Atan2(shot.Direction.Y, shot.Direction.X);
        float scale = MathHelper.Lerp(0.34f, 0.72f, shot.Charge);
        Color color = shot.IsFullCharge ? Color.White : new Color(220, 190, 255);
        DrawLoopingEffect(batch, shot, VisualIds.CannonProjectileFull, shot.Position, rotation, scale, color);
    }

    public void DrawDeathFlame(SpriteBatch batch, object owner, Vector2 position) =>
        DrawLoopingEffect(batch, owner, VisualIds.DeathFlameLoop, position, 0f, 0.56f, Color.White);

    public static void DrawClip(
        SpriteBatch batch,
        SpriteClip clip,
        float elapsed,
        Vector2 position,
        float rotation,
        float scale,
        Color color)
    {
        batch.Draw(
            clip.Texture,
            position,
            clip.GetSourceRectangle(elapsed),
            color,
            rotation,
            clip.PixelOrigin,
            scale,
            SpriteEffects.None,
            0f);
    }

    private SpriteClip? Resolve(string id, string clipName, string? direction, out string resolvedName)
    {
        ClipResolution resolution = VisualResolver.Resolve(Registry, id, clipName);
        if (resolution.Missing is not null)
        {
            ReportMissing(resolution.Missing);
        }

        resolvedName = resolution.Clip?.Name ?? clipName;
        if (resolution.Clip is null)
        {
            return null;
        }

        string? clipDirection = resolution.Clip.IsDirectional ? direction ?? "s" : null;
        string key = ClipKey(id, resolution.Clip.Name, clipDirection);
        if (_clips.TryGetValue(key, out SpriteClip? clip) && clip is not null)
        {
            return clip;
        }

        ReportMissing(key);
        return null;
    }

    private void ReportMissing(string key)
    {
        if (_missingSet.Add(key))
        {
            _missing.Add(key);
        }
    }

    private SpriteClip? LoadClip(ContentManager content, VisualEntry entry, VisualClipDefinition clip, string? direction)
    {
        string path = direction is null ? clip.Path : clip.PathFor(direction);
        Texture2D? texture = TryLoad(content, path);
        if (texture is null)
        {
            ReportMissing(ClipKey(entry.Id, clip.Name, direction));
            return null;
        }

        string? normalPath = direction is null ? clip.NormalMap : clip.NormalMapFor(direction);
        return new SpriteClip(texture, clip.FrameWidth, clip.FrameHeight, clip.Frames, clip.FramesPerSecond, clip.Loop)
        {
            NormalMap = normalPath is null ? null : TryLoad(content, normalPath),
            Origin = entry.Origin
        };
    }

    private static Texture2D? TryLoad(ContentManager content, string path)
    {
        try
        {
            return content.Load<Texture2D>(path);
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }

    private static string ClipKey(string id, string clip, string? direction) =>
        direction is null ? $"{id}/{clip}" : $"{id}/{clip}/{direction}";

    private static VisualRegistry LoadRegistry(out string? error)
    {
        error = null;
        try
        {
            using System.IO.Stream stream = TitleContainer.OpenStream(VisualRegistry.ContentPath);
            return VisualRegistry.Load(stream);
        }
        catch (Exception exception) when (exception is VisualRegistryException or System.IO.IOException)
        {
            error = exception.Message;
            return VisualRegistry.Empty;
        }
    }
}
