using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
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
    private readonly SpriteLighting? _lighting;
    private readonly Effect? _dissolve;
    private readonly DeathFlameRenderer? _deathFlame;
    private readonly Texture2D _dissolveNoise;
    private readonly Texture2D _softSpot;
    private readonly Texture2D _shade;
    private readonly ConditionalWeakTable<object, FigureState> _figures = new();
    private readonly List<DissolveInstance> _dissolves = [];
    private Matrix _sceneTransform = Matrix.Identity;
    private bool _litSceneActive;
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
        Effect? spriteLit = TryLoadEffect(content, "Effects/SpriteLit");
        _lighting = spriteLit is null ? null : new SpriteLighting(spriteLit);
        _dissolve = TryLoadEffect(content, "Effects/Dissolve");
        Effect? deathFlame = TryLoadEffect(content, "Effects/DeathFlame");
        _deathFlame = deathFlame is null ? null : new DeathFlameRenderer(device, deathFlame);
        _dissolveNoise = CreateNoiseTexture(device, 64, seed: 1709);
        _softSpot = CreateSoftSpotTexture(device, 64);
        _shade = CreateShadeTexture(device, 256);

        foreach (VisualEntry entry in registry.Entries)
        {
            foreach (VisualClipDefinition clip in entry.Clips.Values)
            {
                if (clip.IsTiled)
                {
                    for (int row = 0; row < clip.TileRows; row++)
                    {
                        for (int column = 0; column < clip.TileColumns; column++)
                        {
                            Texture2D? tile = TryLoad(content, clip.PathForTile(column, row));
                            _clips[TileKey(entry.Id, clip.Name, column, row)] = tile is null
                                ? null
                                : new SpriteClip(tile, clip.FrameWidth, clip.FrameHeight, 1, 1f, true);
                            if (tile is null)
                            {
                                ReportMissing(TileKey(entry.Id, clip.Name, column, row));
                            }
                        }
                    }
                }
                else if (clip.IsDirectional)
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

    /// <summary>
    /// Enables lighting for clips with normal maps until <see cref="EndLitScene"/>. Call right
    /// after <c>batch.Begin(Deferred, AlphaBlend, LinearClamp, transformMatrix: transform)</c>.
    /// </summary>
    public void BeginLitScene(Matrix transform, IReadOnlyList<SceneLight> lights)
    {
        _sceneTransform = transform;
        _lighting?.BeginScene(transform, lights);
        _litSceneActive = _lighting is not null;
    }

    public void EndLitScene() => _litSceneActive = false;

    /// <summary>
    /// Draws a Death Flame ribbon along <paramref name="path"/> (tail first) inside the lit scene.
    /// Without the shader nothing is drawn; callers keep their own fallback.
    /// </summary>
    public bool DrawDeathFlameTrail(SpriteBatch batch, IReadOnlyList<Vector2> path, float width, float opacity)
    {
        if (_deathFlame is null || !_litSceneActive)
        {
            return false;
        }

        _deathFlame.DrawTrail(batch, _sceneTransform, path, width, opacity, _time);
        return true;
    }

    /// <summary>Drops dissolves and other presentation-only leftovers, for example when a run restarts.</summary>
    public void ClearTransient() => _dissolves.Clear();

    /// <summary>
    /// Draws the dissolving last poses of defeated figures. Purely visual: the enemy may already
    /// be gone from the world; collision, waves and Souls never see these.
    /// </summary>
    public void DrawDissolves(SpriteBatch batch)
    {
        _dissolves.RemoveAll(dissolve => _time - dissolve.StartedAt >= dissolve.Duration);
        foreach (DissolveInstance dissolve in _dissolves)
        {
            float progress = MathHelper.Clamp((_time - dissolve.StartedAt) / dissolve.Duration, 0f, 1f);
            if (_dissolve is null || !_litSceneActive)
            {
                batch.Draw(dissolve.Clip.Texture, dissolve.Position, dissolve.Source, dissolve.Tint * (1f - progress), 0f, dissolve.Clip.PixelOrigin, dissolve.Scale, SpriteEffects.None, 0f);
                continue;
            }

            Texture2D texture = dissolve.Clip.Texture;
            EffectParameterCollection parameters = _dissolve.Parameters;
            parameters["NoiseTexture"].SetValue(_dissolveNoise);
            parameters["FrameUvRect"].SetValue(new Vector4(
                (float)dissolve.Source.X / texture.Width,
                (float)dissolve.Source.Y / texture.Height,
                (float)dissolve.Source.Width / texture.Width,
                (float)dissolve.Source.Height / texture.Height));
            parameters["Progress"].SetValue(progress);
            parameters["EdgeWidth"].SetValue(0.08f);
            parameters["EdgeColor"].SetValue(Game.GameBalance.DeathFlame.ToVector3());
            parameters["EdgeCore"].SetValue(Game.GameBalance.DeathFlameBright.ToVector3());

            batch.End();
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, _dissolve, _sceneTransform);
            batch.Draw(texture, dissolve.Position, dissolve.Source, dissolve.Tint, 0f, dissolve.Clip.PixelOrigin, dissolve.Scale, SpriteEffects.None, 0f);
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: _sceneTransform);
        }
    }

    /// <summary>The clip for an effect or sprite, or <c>null</c> (reported as missing) when there is none.</summary>
    public SpriteClip? GetEffect(string id) => Resolve(id, VisualClips.Default, null, out _);

    public Texture2D? GetSpriteTexture(string id) => GetEffect(id)?.Texture;

    public Vector2 WorldSizeOf(string id, Vector2 fallback) =>
        Registry.TryGet(id, out VisualEntry entry) ? entry.WorldSize : fallback;

    public void DrawEnvironment(SpriteBatch batch, string id, Vector2 position)
    {
        if (Registry.TryGet(id, out VisualEntry tiledEntry) && tiledEntry.TryGetClip(VisualClips.Default, out VisualClipDefinition tiled) && tiled.IsTiled)
        {
            DrawTiled(batch, tiledEntry, tiled, position);
            return;
        }

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

    /// <summary>Draws a single-image environment stretched over a screen rectangle (title key art).</summary>
    public void DrawEnvironmentStretched(SpriteBatch batch, string id, Rectangle destination)
    {
        if (GetEffect(id) is { } clip)
        {
            batch.Draw(clip.Texture, destination, clip.GetSourceRectangle(0f), Color.White);
        }
    }

    /// <summary>
    /// Draws an environment piece repeated sideways from x = 0 to <paramref name="coverWidth"/>,
    /// shifted left by <paramref name="scroll"/> world units (wrapping), its top at <paramref name="top"/>.
    /// </summary>
    public void DrawEnvironmentScrolled(SpriteBatch batch, string id, float top, float scroll, float coverWidth)
    {
        float width = WorldSizeOf(id, new Vector2(1800f, 1000f)).X;
        if (width <= 1f)
        {
            return;
        }
        float start = -(scroll % width);
        for (float x = start; x < coverWidth; x += width)
        {
            DrawEnvironment(batch, id, new Vector2(MathF.Round(x), top));
        }
    }

    private void DrawTiled(SpriteBatch batch, VisualEntry entry, VisualClipDefinition clip, Vector2 position)
    {
        Vector2 topLeft = position - entry.Origin * entry.WorldSize;
        Vector2 tileSize = entry.WorldSize / new Vector2(clip.TileColumns, clip.TileRows);
        for (int row = 0; row < clip.TileRows; row++)
        {
            for (int column = 0; column < clip.TileColumns; column++)
            {
                // Edges are rounded from the tile grid so neighbouring tiles meet without gaps.
                int left = (int)MathF.Round(topLeft.X + column * tileSize.X);
                int top = (int)MathF.Round(topLeft.Y + row * tileSize.Y);
                int right = (int)MathF.Round(topLeft.X + (column + 1) * tileSize.X);
                int bottom = (int)MathF.Round(topLeft.Y + (row + 1) * tileSize.Y);
                Rectangle bounds = new(left, top, right - left, bottom - top);
                if (_clips.TryGetValue(TileKey(entry.Id, clip.Name, column, row), out SpriteClip? tile) && tile is not null)
                {
                    batch.Draw(tile.Texture, bounds, Color.White);
                }
                else
                {
                    batch.DrawPropDummy(_pixel, bounds, new Vector2(bounds.Center.X, bounds.Bottom));
                }
            }
        }
    }

    private static string TileKey(string id, string clip, int column, int row) => $"{id}/{clip}/c{column}r{row}";

    /// <summary>Normal hit flash of the player (Player.ApplyDamage); the fatal one is longer.</summary>
    private const float PlayerHitFlash = 0.14f;

    public void DrawPlayer(SpriteBatch batch, Player player)
    {
        if (player.IsDead)
        {
            // The fall plays once from the fatal hit and holds its last frame.
            if (HasClip(VisualIds.Player, VisualClips.Death))
            {
                DrawCharacter(batch, player, VisualIds.Player, VisualClips.Death, player.FacingDirection, player.Position, 1f, Color.White, snapFacing: true);
            }
            return;
        }

        // Every action shows its own clip, sampled by the gameplay timer that already defines it
        // (dash, swing, cannon states, hit flash); otherwise the figure runs where it moves and
        // stands facing the aim.
        if (player.IsDashing && HasClip(VisualIds.Player, VisualClips.Dash))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Dash, player.DashDirection, player.Position, 1f, Color.White,
                progress: player.DashProgress, snapFacing: true);
            return;
        }

        if (player.Scythe.ActiveStep > 0 && HasClip(VisualIds.Player, VisualClips.Swing(player.Scythe.ActiveStep)))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Swing(player.Scythe.ActiveStep), player.Scythe.AttackDirection,
                player.Position, 1f, Color.White, progress: player.Scythe.NormalizedProgress, snapFacing: true);
            return;
        }

        bool moving = player.Velocity.LengthSquared() > 120f;
        string? cannonClip = player.Cannon.State switch
        {
            SoulCannonState.Drawing => VisualClips.CannonDraw,
            SoulCannonState.Charging => VisualClips.Aim,
            SoulCannonState.Returning => VisualClips.CannonFire,
            _ => null
        };
        if (cannonClip is not null && HasClip(VisualIds.Player, cannonClip))
        {
            float? progress = cannonClip == VisualClips.Aim ? null : player.Cannon.StateProgress;
            DrawCharacter(batch, player, VisualIds.Player, cannonClip, player.FacingDirection, player.Position, 1f, Color.White, progress: progress);
            return;
        }

        if (player.HitFlashRemaining > 0f && HasClip(VisualIds.Player, VisualClips.Hit))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Hit, player.FacingDirection, player.Position, 1f, Color.White,
                progress: 1f - player.HitFlashRemaining / PlayerHitFlash);
            return;
        }

        Vector2 facing = moving && HasClip(VisualIds.Player, VisualClips.Swing1) ? player.Velocity : player.FacingDirection;
        DrawCharacter(batch, player, VisualIds.Player, moving ? VisualClips.Move : VisualClips.Idle, facing, player.Position, 1f, Color.White);
    }

    /// <summary>
    /// Whether a Visual-ID is a rendered figure (it has a fixed pixel scale): it stands with its
    /// feet on its position, so marks on its body sit <see cref="FigureHeights"/> above it.
    /// </summary>
    public bool IsRendered(string? id) =>
        id is not null && Registry.TryGet(id, out VisualEntry entry) && entry.PixelsPerUnit is not null;

    /// <summary>Whether a Visual-ID has its own clip of that name (no fallback).</summary>
    public bool HasClip(string id, string clipName) =>
        Registry.TryGet(id, out VisualEntry entry) && entry.TryGetClip(clipName, out _);

    public void DrawEnemy(SpriteBatch batch, Enemy enemy)
    {
        if (enemy.VisualId is not { } id)
        {
            return;
        }

        if (enemy.VisualClip is not { } clip)
        {
            StartDissolve(enemy, id);
            return;
        }

        Color tint = enemy.HitFlashRemaining > 0f ? new Color(255, 235, 255) : Color.White;
        if (IsRendered(id))
        {
            DrawSoftSpot(batch, enemy.Position + new Vector2(8f, 2f), new Vector2(enemy.Radius * 1.5f, enemy.Radius * 0.5f) * enemy.VisualScale, new Color(3, 3, 7) * 0.7f);
        }
        DrawCharacter(batch, enemy, id, clip, enemy.VisualFacing, enemy.Position, enemy.VisualScale, tint, enemy.Radius * 2.6f,
            progress: HasClip(id, clip) ? enemy.VisualProgress : null);
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
        float fallbackSize = FallbackCharacterSize,
        float? progress = null,
        bool snapFacing = false)
    {
        FigureState figure = _figures.GetValue(owner, _ => new FigureState());
        float deltaTime = figure.Advance(_time, position, out float distance);
        string direction = snapFacing ? figure.Facing.Snap(facing) : figure.Facing.Update(facing, deltaTime);
        Vector2 worldSize = WorldSizeOf(id, new Vector2(fallbackSize)) * sizeScale;
        SpriteClip? clip = Resolve(id, clipName, direction, out string resolvedName, out VisualClipDefinition? definition);
        if (clip is null || definition is null)
        {
            Vector2 drawnFacing = new(MathF.Cos(figure.Facing.Angle), MathF.Sin(figure.Facing.Angle));
            batch.DrawCharacterDummy(_pixel, position, worldSize, drawnFacing, tint);
            return;
        }

        float elapsed = figure.PlayClip(resolvedName, definition, deltaTime, distance);
        if (progress is { } share)
        {
            // Frame i was rendered at progress i / (frames - 1); sample the middle of that frame.
            int frame = (int)MathF.Round(MathHelper.Clamp(share, 0f, 1f) * (clip.FrameCount - 1));
            elapsed = (frame + 0.5f) / clip.FramesPerSecond;
        }

        float scale = Registry.TryGet(id, out VisualEntry entry) && entry.PixelsPerUnit is { } pixelsPerUnit
            ? sizeScale / pixelsPerUnit
            : worldSize.X / clip.FrameWidth;
        figure.RememberPose(clip, clip.GetSourceRectangle(elapsed), position, scale, tint);
        DrawFrame(batch, clip, elapsed, position, scale, tint);
    }

    private void StartDissolve(object owner, string id)
    {
        if (!_figures.TryGetValue(owner, out FigureState? figure) || figure.DissolveStarted || figure.LastClip is null)
        {
            return;
        }

        figure.DissolveStarted = true;
        if (Registry.TryGet(id, out VisualEntry entry) && entry.Dissolve is { } dissolve)
        {
            _dissolves.Add(new DissolveInstance(figure.LastClip, figure.LastSource, figure.LastPosition, figure.LastScale, figure.LastTint, dissolve.Duration, _time));
        }
    }

    /// <summary>Per-figure presentation state; never read by gameplay.</summary>
    private sealed class FigureState
    {
        /// <summary>A teleport or respawn must not fast-forward the run cycle.</summary>
        private const float MaxDistancePerDraw = 120f;

        private bool _hasDrawn;
        private float _lastTime;
        private Vector2 _lastDrawPosition;
        private string _clipName = string.Empty;
        private float _elapsed;

        public FacingTracker Facing { get; } = new();
        public SpriteClip? LastClip { get; private set; }
        public Rectangle LastSource { get; private set; }
        public Vector2 LastPosition { get; private set; }
        public float LastScale { get; private set; }
        public Color LastTint { get; private set; }
        public bool DissolveStarted { get; set; }

        /// <summary>Time and distance since this figure was last drawn.</summary>
        public float Advance(float time, Vector2 position, out float distance)
        {
            float deltaTime = _hasDrawn ? MathHelper.Clamp(time - _lastTime, 0f, 0.25f) : 0f;
            distance = _hasDrawn ? MathF.Min(Vector2.Distance(position, _lastDrawPosition), MaxDistancePerDraw) : 0f;
            _hasDrawn = true;
            _lastTime = time;
            _lastDrawPosition = position;
            return deltaTime;
        }

        /// <summary>Elapsed playback of <paramref name="name"/>; switching clips restarts, turning does not.</summary>
        public float PlayClip(string name, VisualClipDefinition clip, float deltaTime, float distance)
        {
            if (!string.Equals(_clipName, name, StringComparison.Ordinal))
            {
                _clipName = name;
                _elapsed = 0f;
                return _elapsed;
            }

            _elapsed = ClipClock.Advance(_elapsed, clip, deltaTime, distance);
            return _elapsed;
        }

        public void RememberPose(SpriteClip clip, Rectangle source, Vector2 position, float scale, Color tint)
        {
            LastClip = clip;
            LastSource = source;
            LastPosition = position;
            LastScale = scale;
            LastTint = tint;
        }
    }

    private sealed record DissolveInstance(SpriteClip Clip, Rectangle Source, Vector2 Position, float Scale, Color Tint, float Duration, float StartedAt);

    /// <summary>
    /// A soft round spot stretched to <paramref name="radii"/>: contact shadows under feet and small
    /// glows on bodies, without the stepped edges of shapes built from lines.
    /// </summary>
    public void DrawSoftSpot(SpriteBatch batch, Vector2 center, Vector2 radii, Color color) =>
        batch.Draw(_softSpot, center, null, color, 0f, new Vector2(_softSpot.Width, _softSpot.Height) * 0.5f,
            radii * 2f / _softSpot.Width, SpriteEffects.None, 0f);

    /// <summary>
    /// A smooth vertical shade over a rectangle: <paramref name="top"/> at its top edge fading to
    /// nothing at its bottom (premultiplied), without the banding of stacked rectangles.
    /// </summary>
    public void DrawShade(SpriteBatch batch, Rectangle area, Color top) =>
        batch.Draw(_shade, area, top);

    private static Texture2D CreateShadeTexture(GraphicsDevice device, int height)
    {
        Color[] data = new Color[height];
        for (int y = 0; y < height; y++)
        {
            float share = 1f - y / (height - 1f);
            float alpha = share * share * (3f - 2f * share);
            data[y] = Color.White * alpha;
        }
        Texture2D texture = new(device, 1, height);
        texture.SetData(data);
        return texture;
    }

    /// <summary>Premultiplied white with a smooth falloff to the edge.</summary>
    private static Texture2D CreateSoftSpotTexture(GraphicsDevice device, int size)
    {
        Color[] data = new Color[size * size];
        Vector2 center = new((size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = MathHelper.SmoothStep(1f, 0f, MathHelper.Clamp((distance - 0.35f) / 0.65f, 0f, 1f));
                byte value = (byte)MathF.Round(alpha * 255f);
                data[y * size + x] = new Color(value, value, value, value);
            }
        }

        Texture2D texture = new(device, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>Smooth value noise for the dissolve mask; generated once, no asset needed.</summary>
    private static Texture2D CreateNoiseTexture(GraphicsDevice device, int size, int seed)
    {
        Random random = new(seed);
        const int cells = 8;
        float[,] grid = new float[cells + 1, cells + 1];
        for (int y = 0; y <= cells; y++)
        {
            for (int x = 0; x <= cells; x++)
            {
                grid[x % cells, y % cells] = (float)random.NextDouble();
            }
        }

        Color[] data = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float gx = (float)x / size * cells;
                float gy = (float)y / size * cells;
                int x0 = (int)gx, y0 = (int)gy;
                float tx = MathHelper.SmoothStep(0f, 1f, gx - x0);
                float ty = MathHelper.SmoothStep(0f, 1f, gy - y0);
                float top = MathHelper.Lerp(grid[x0 % cells, y0 % cells], grid[(x0 + 1) % cells, y0 % cells], tx);
                float bottom = MathHelper.Lerp(grid[x0 % cells, (y0 + 1) % cells], grid[(x0 + 1) % cells, (y0 + 1) % cells], tx);
                float fine = (float)random.NextDouble() * 0.18f;
                byte value = (byte)MathF.Round(MathHelper.Clamp(MathHelper.Lerp(top, bottom, ty) * 0.82f + fine, 0f, 1f) * 255f);
                data[y * size + x] = new Color(value, value, value, (byte)255);
            }
        }

        Texture2D texture = new(device, size, size);
        texture.SetData(data);
        return texture;
    }

    /// <summary>Draws an unrotated frame, lit when the clip has a normal map and a lit scene is running.</summary>
    private void DrawFrame(SpriteBatch batch, SpriteClip clip, float elapsed, Vector2 position, float scale, Color color)
    {
        if (_litSceneActive && clip.NormalMap is not null)
        {
            _lighting!.Draw(batch, clip, clip.GetSourceRectangle(elapsed), position, scale, color);
            return;
        }

        DrawClip(batch, clip, elapsed, position, 0f, scale, color);
    }

    /// <summary>The drawing band of an environment layer or prop; <paramref name="fallback"/> without a registry entry.</summary>
    public SceneLayer LayerOf(string id, SceneLayer fallback) =>
        Registry.TryGet(id, out VisualEntry entry) ? entry.Layer : fallback;

    /// <summary>World bounds of a prop standing on <paramref name="foot"/>.</summary>
    public RectangleF PropBounds(string id, Vector2 foot, Vector2 fallbackSize) =>
        Registry.TryGet(id, out VisualEntry entry)
            ? RectangleF.FromFoot(foot, entry.WorldSize, entry.Origin)
            : RectangleF.FromFoot(foot, fallbackSize, new Vector2(0.5f, 1f));

    /// <summary>Draws a prop on its foot point; occluders pass a reduced <paramref name="alpha"/> while they hide something.</summary>
    /// <summary>Whether a Visual-ID has a loaded texture (no dummy), e.g. a rendered room.</summary>
    public bool HasArt(string id) =>
        Registry.TryGet(id, out VisualEntry entry) && entry.TryGetClip(VisualClips.Default, out VisualClipDefinition clip) &&
        (clip.IsTiled || GetEffect(id) is not null);

    /// <summary>
    /// Draws the part of a sprite (placed with its origin on <paramref name="anchor"/>) that falls
    /// in <paramref name="window"/>, after shifting the sprite by <paramref name="shift"/>: door
    /// leaves that slide into the wall, cut at the opening. Returns false without a texture.
    /// </summary>
    public bool DrawSpriteWindow(SpriteBatch batch, string id, Vector2 anchor, RectangleF window, Vector2 shift, Color tint)
    {
        SpriteClip? clip = GetEffect(id);
        if (clip is null || !Registry.TryGet(id, out VisualEntry entry))
        {
            return false;
        }

        RectangleF sprite = RectangleF.FromFoot(anchor + shift, entry.WorldSize, entry.Origin);
        float left = MathF.Max(window.X, sprite.X);
        float top = MathF.Max(window.Y, sprite.Y);
        float right = MathF.Min(window.Right, sprite.Right);
        float bottom = MathF.Min(window.Bottom, sprite.Bottom);
        if (right <= left || bottom <= top)
        {
            return true;
        }

        Rectangle frame = clip.GetSourceRectangle(0f);
        float scaleX = frame.Width / sprite.Width;
        float scaleY = frame.Height / sprite.Height;
        Rectangle source = new(
            frame.X + (int)MathF.Round((left - sprite.X) * scaleX),
            frame.Y + (int)MathF.Round((top - sprite.Y) * scaleY),
            Math.Max(1, (int)MathF.Round((right - left) * scaleX)),
            Math.Max(1, (int)MathF.Round((bottom - top) * scaleY)));
        Rectangle destination = new((int)MathF.Round(left), (int)MathF.Round(top), (int)MathF.Round(right - left), (int)MathF.Round(bottom - top));
        batch.Draw(clip.Texture, destination, source, tint);
        return true;
    }

    /// <summary>
    /// A Warden flame (VISUAL-ART-DIRECTION S8–S11): a vertical, calm Death Flame standing on
    /// <paramref name="base"/> in its fitting, breathing a little, never still and never wild.
    /// </summary>
    public void DrawWardenFlame(SpriteBatch batch, Vector2 @base, float height, float time, float alpha = 1f)
    {
        float breathe = 1f + 0.06f * MathF.Sin(time * 2.3f) + 0.03f * MathF.Sin(time * 5.7f + 1.3f);
        float sway = 0.8f * MathF.Sin(time * 1.7f + 0.4f);
        float h = height * breathe;
        Vector2 Up(float share) => @base + new Vector2(sway * share, -h * share);
        DrawSoftSpot(batch, Up(0.42f), new Vector2(height * 0.34f, h * 0.58f), GameBalance.DeepViolet * (0.55f * alpha));
        DrawSoftSpot(batch, Up(0.40f), new Vector2(height * 0.22f, h * 0.44f), GameBalance.DeathFlame * (0.85f * alpha));
        DrawSoftSpot(batch, Up(0.70f), new Vector2(height * 0.07f, h * 0.26f), GameBalance.DeathFlame * (0.7f * alpha));
        DrawSoftSpot(batch, Up(0.33f), new Vector2(height * 0.13f, h * 0.29f), GameBalance.DeathFlameBright * alpha);
        DrawSoftSpot(batch, Up(0.22f), new Vector2(height * 0.06f, h * 0.13f), GameBalance.SoulWhite * (0.9f * alpha));
    }

    public void DrawProp(SpriteBatch batch, string id, Vector2 foot, Vector2 fallbackSize, float alpha)
    {
        RectangleF bounds = PropBounds(id, foot, fallbackSize);
        SpriteClip? clip = GetEffect(id);
        if (clip is null)
        {
            batch.DrawPropDummy(_pixel, new Rectangle((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height), foot, alpha);
            return;
        }

        Rectangle destination = new((int)MathF.Round(bounds.X), (int)MathF.Round(bounds.Y), (int)MathF.Round(bounds.Width), (int)MathF.Round(bounds.Height));
        batch.Draw(clip.Texture, destination, clip.GetSourceRectangle(_time), Color.White * alpha);
    }

    public void DrawSprite(SpriteBatch batch, string id, Vector2 position, float scale, Color color, string clipName = VisualClips.Default)
    {
        SpriteClip? clip = Resolve(id, clipName, null, out _);
        Vector2 worldSize = WorldSizeOf(id, new Vector2(64f)) * scale;
        if (clip is null)
        {
            Vector2 topLeft = position - worldSize * 0.5f;
            batch.DrawPropDummy(_pixel, new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)worldSize.X, (int)worldSize.Y), position + new Vector2(0f, worldSize.Y * 0.5f));
            return;
        }

        DrawFrame(batch, clip, _time, position, worldSize.X / clip.FrameWidth, color);
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

    private SpriteClip? Resolve(string id, string clipName, string? direction, out string resolvedName) =>
        Resolve(id, clipName, direction, out resolvedName, out _);

    private SpriteClip? Resolve(string id, string clipName, string? direction, out string resolvedName, out VisualClipDefinition? definition)
    {
        ClipResolution resolution = VisualResolver.Resolve(Registry, id, clipName);
        definition = resolution.Clip;
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
            Origin = clip.Origin ?? entry.Origin
        };
    }

    private static Effect? TryLoadEffect(ContentManager content, string path)
    {
        try
        {
            return content.Load<Effect>(path);
        }
        catch (Exception exception) when (exception is ContentLoadException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Shader {path} nicht geladen: {exception.Message}");
            return null;
        }
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
