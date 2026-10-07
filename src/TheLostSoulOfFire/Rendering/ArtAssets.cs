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

    /// <summary>
    /// The rendered training post, standing on its iron foot 36 units below the dummy's position.
    /// A hit rocks it on its foot for a moment (presentation only; the dummy never moves).
    /// </summary>
    private void DrawTrainingDummy(SpriteBatch batch, TrainingDummy dummy)
    {
        SpriteClip? clip = GetEffect(VisualIds.TrainingDummy);
        dummy.DrawnAsFigure = clip is not null;
        if (clip is null || !Registry.TryGet(VisualIds.TrainingDummy, out VisualEntry entry))
        {
            return;
        }

        Vector2 foot = dummy.Position + new Vector2(0f, 36f);
        float hit = MathHelper.Clamp(dummy.HitFlashRemaining / 0.14f, 0f, 1f);
        float rock = MathF.Sin((1f - hit) * MathF.PI * 3f) * hit * 0.07f;
        DrawSoftSpot(batch, foot + new Vector2(6f, 0f), new Vector2(34f, 11f), new Color(3, 3, 7) * 0.65f);
        Vector2 scale = entry.WorldSize / new Vector2(clip.FrameWidth, clip.FrameHeight);
        Vector2 origin = entry.Origin * new Vector2(clip.FrameWidth, clip.FrameHeight);
        Color tint = hit > 0f ? Color.Lerp(Color.White, new Color(255, 235, 255), hit) : Color.White;
        batch.Draw(clip.Texture, foot, clip.GetSourceRectangle(0f), tint, rock, origin, scale, SpriteEffects.None, 0f);
    }

    /// <summary>A Death Flame slash along <paramref name="path"/> (tail first); false without the shader.</summary>
    /// <summary>The scythe's sweep as a soft surface between blade root and tip (DeathFlameRenderer.DrawSmear).</summary>
    public bool DrawDeathFlameSmear(SpriteBatch batch, IReadOnlyList<Vector2> inner, IReadOnlyList<Vector2> outer, float opacity,
        IReadOnlyList<float>? mask = null)
    {
        if (_deathFlame is null || !_litSceneActive)
        {
            return false;
        }

        _deathFlame.DrawSmear(batch, _sceneTransform, inner, outer, opacity, _time, mask);
        return true;
    }

    public bool DrawDeathFlameSlash(SpriteBatch batch, IReadOnlyList<Vector2> path, float headWidth, float opacity, float heat,
        IReadOnlyList<float>? mask = null)
    {
        if (_deathFlame is null || !_litSceneActive)
        {
            return false;
        }

        _deathFlame.DrawSlash(batch, _sceneTransform, path, headWidth, opacity, heat, _time, mask);
        return true;
    }

    /// <summary>True when Death Flame ribbons can be drawn (the shader loaded).</summary>
    public bool CanDrawDeathFlame => _deathFlame is not null;

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

    public void DrawEnvironment(SpriteBatch batch, string id, Vector2 position) => DrawEnvironment(batch, id, position, Color.White);

    /// <summary>Draws an environment plate tinted, e.g. distant strips sunk into the night.</summary>
    public void DrawEnvironment(SpriteBatch batch, string id, Vector2 position, Color tint)
    {
        if (Registry.TryGet(id, out VisualEntry tiledEntry) && tiledEntry.TryGetClip(VisualClips.Default, out VisualClipDefinition tiled) && tiled.IsTiled)
        {
            DrawTiled(batch, tiledEntry, tiled, position, tint);
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

        batch.Draw(clip.Texture, bounds, clip.GetSourceRectangle(0f), tint);
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
    public void DrawEnvironmentScrolled(SpriteBatch batch, string id, float top, float scroll, float coverWidth) =>
        DrawEnvironmentScrolled(batch, id, top, scroll, coverWidth, Color.White);

    public void DrawEnvironmentScrolled(SpriteBatch batch, string id, float top, float scroll, float coverWidth, Color tint)
    {
        float width = WorldSizeOf(id, new Vector2(1800f, 1000f)).X;
        if (width <= 1f)
        {
            return;
        }
        float start = -(scroll % width);
        for (float x = start; x < coverWidth; x += width)
        {
            DrawEnvironment(batch, id, new Vector2(MathF.Round(x), top), tint);
        }
    }

    private void DrawTiled(SpriteBatch batch, VisualEntry entry, VisualClipDefinition clip, Vector2 position, Color tint)
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
                    batch.Draw(tile.Texture, bounds, tint);
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
        if (player.WakeProgress is { } wake && HasClip(VisualIds.Player, VisualClips.Wake))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Wake, player.FacingDirection, player.Position, 1f, Color.White,
                progress: wake, snapFacing: true);
            return;
        }

        if (player.IsDashing && HasClip(VisualIds.Player, VisualClips.Dash))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Dash, player.DashDirection, player.Position, 1f, Color.White,
                progress: player.DashProgress, snapFacing: true);
            return;
        }

        if (player.LeapProgress is { } leap && HasClip(VisualIds.Player, VisualClips.Retreat))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Retreat, player.FacingDirection, player.Position, 1f, Color.White,
                progress: leap, snapFacing: true);
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
        if (cannonClip == VisualClips.Aim && moving && HasClip(VisualIds.Player, VisualClips.AimMove))
        {
            // Charging on the move: the cannon stays on target while the legs walk under it, at
            // the size of the charge stage it has reached.
            string walk = VisualClips.AimMoveStage(player.Cannon.ChargeStage);
            DrawCharacter(batch, player, VisualIds.Player, HasClip(VisualIds.Player, walk) ? walk : VisualClips.AimMove,
                player.FacingDirection, player.Position, 1f, Color.White, walkAxis: player.FacingDirection);
            return;
        }

        if (cannonClip is not null && HasClip(VisualIds.Player, cannonClip))
        {
            // Standing, the braced cannon is drawn at its charge: it grows as the Death Flame fills it.
            float? progress = cannonClip == VisualClips.Aim ? player.Cannon.ChargeProgress : player.Cannon.StateProgress;
            DrawCharacter(batch, player, VisualIds.Player, cannonClip, player.FacingDirection, player.Position, 1f, Color.White, progress: progress);
            return;
        }

        if (player.HitFlashRemaining > 0f && HasClip(VisualIds.Player, VisualClips.Hit))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.Hit, player.FacingDirection, player.Position, 1f, Color.White,
                progress: 1f - player.HitFlashRemaining / PlayerHitFlash);
            return;
        }

        // Standing still after a swing, the figure gathers itself back into the guard instead of
        // snapping there; moving cuts the return short and runs.
        int lastStep = player.Scythe.LastStep;
        if (!moving && lastStep > 0 && player.Scythe.SinceSwingEnd < VisualClips.SwingReturnDuration(lastStep) &&
            HasClip(VisualIds.Player, VisualClips.SwingReturn(lastStep)))
        {
            DrawCharacter(batch, player, VisualIds.Player, VisualClips.SwingReturn(lastStep), player.Scythe.AttackDirection, player.Position, 1f, Color.White,
                progress: player.Scythe.SinceSwingEnd / VisualClips.SwingReturnDuration(lastStep), snapFacing: true);
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
        if (enemy is TrainingDummy dummy)
        {
            DrawTrainingDummy(batch, dummy);
            return;
        }

        if (enemy.VisualId is not { } id)
        {
            return;
        }

        enemy.DrawnAsFigure = IsRendered(id);
        enemy.LightSpot = SoftSpot;
        if (enemy.VisualClip is not { } clip)
        {
            StartDissolve(enemy, id);
            return;
        }

        Color tint = enemy.HitFlashRemaining > 0f && !_litSceneActive ? new Color(255, 235, 255) : Color.White;
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
        bool snapFacing = false,
        Vector2? walkAxis = null)
    {
        FigureState figure = _figures.GetValue(owner, created => new FigureState(created is Enemy));
        float deltaTime = figure.Advance(_time, position, out float distance, out Vector2 moved);
        if (walkAxis is { } axis && axis.LengthSquared() > 0.0001f)
        {
            // Walking backward plays the cycle backward, so the feet keep their grip.
            distance = Vector2.Dot(moved, Vector2.Normalize(axis));
        }
        switch (owner)
        {
            case Enemy enemy:
                figure.TrackImpact(_time, enemy.HitFlashRemaining, enemy.LastHitDirection);
                break;
            case Player hurt:
                figure.TrackImpact(_time, hurt.HitFlashRemaining, hurt.LastHitDirection);
                break;
        }
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
        (Vector2 impactScale, float lean, float flash) = figure.ImpactPose(_time);
        if (owner is Player)
        {
            // The player's own figure flashes less, so it never vanishes into white in a crowd.
            flash *= 0.7f;
        }
        DrawFrame(batch, clip, elapsed, position, scale * impactScale, lean, tint, new Vector4(HitFlashColor, flash));
        if (figure.SettleRemaining > 0f && figure.Settling is { } previous)
        {
            // The pose being left lies over the new one and fades out, moving with the figure.
            float fade = figure.SettleRemaining / FigureState.SettleDuration;
            DrawSource(batch, previous.Clip, previous.Source, position, previous.Scale, previous.Tint * (fade * fade * 0.9f));
        }
        figure.RememberPose(clip, clip.GetSourceRectangle(elapsed), position, scale, tint);
    }

    /// <summary>A figure that casts a shadow: who it is (its last drawn pose is used), where its feet are, how dark.</summary>
    public readonly record struct ShadowCaster(object Owner, Vector2 Foot, float Strength);

    /// <summary>
    /// Shadows of rendered figures on the floor, drawn before any figure: the figure's own pose
    /// laid down along the room's key light (<paramref name="keyShadow"/>: where a point one unit
    /// up lands, in world units on the floor), plus a second, fainter one thrown away from the
    /// strongest nearby light (braziers, furnaces, a burning foe). Several layers of growing
    /// length make it dark at the feet and soft toward the tip. Presentation only.
    /// </summary>
    public void DrawCastShadows(SpriteBatch batch, IReadOnlyList<ShadowCaster> casters, IReadOnlyList<SceneLight> lights, Vector2 keyShadow, float keyStrength)
    {
        if (casters.Count == 0)
        {
            return;
        }

        batch.End();
        foreach (ShadowCaster caster in casters)
        {
            if (!_figures.TryGetValue(caster.Owner, out FigureState? figure) || figure.LastClip is not { } clip)
            {
                continue;
            }

            DrawShadow(batch, clip, figure.LastSource, figure.LastScale, caster.Foot, keyShadow, keyStrength * caster.Strength);
            if (StrongestLight(lights, caster.Foot) is { } light)
            {
                Vector2 away = caster.Foot - light.Position;
                float distance = away.Length();
                float reach = light.Radius * SpriteLighting.LightRadiusScale;
                float falloff = 1f - distance / reach;
                // Close to a light the shadow is long and dark; far from it, short and faint.
                Vector2 direction = away / distance * MathHelper.Clamp(70f / distance, 0.35f, 1.1f);
                direction.Y *= FigureHeights.LevelSquash;
                DrawShadow(batch, clip, figure.LastSource, figure.LastScale, caster.Foot, direction,
                    0.32f * caster.Strength * MathHelper.Clamp(light.Intensity * 1.6f, 0f, 1f) * falloff * falloff);
            }
        }
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: _sceneTransform);
    }

    private static SceneLight? StrongestLight(IReadOnlyList<SceneLight> lights, Vector2 foot)
    {
        SceneLight? best = null;
        float bestWeight = 0.05f;
        foreach (SceneLight light in lights)
        {
            // Small glows (sparks, cores, souls) light a figure but do not throw a shadow worth drawing.
            if (light.Radius < 70f || light.Intensity < 0.2f)
            {
                continue;
            }
            float distance = Vector2.Distance(light.Position, foot);
            float reach = light.Radius * SpriteLighting.LightRadiusScale;
            if (distance < 36f || distance >= reach)
            {
                continue;
            }
            float falloff = 1f - distance / reach;
            float weight = light.Intensity * falloff * falloff;
            if (weight > bestWeight)
            {
                bestWeight = weight;
                best = light;
            }
        }
        return best;
    }

    private void DrawShadow(SpriteBatch batch, SpriteClip clip, Rectangle source, float scale, Vector2 foot, Vector2 direction, float alpha)
    {
        if (alpha <= 0.01f || direction.LengthSquared() < 0.0001f)
        {
            return;
        }

        // The figure's width lies across the shadow, its height along it.
        Vector2 across = Vector2.Normalize(new Vector2(-direction.Y, direction.X));
        if (across.X < 0f)
        {
            across = -across;
        }
        across = Vector2.Lerp(Vector2.UnitX, across, 0.6f);
        const int layers = 4;
        for (int layer = 0; layer < layers; layer++)
        {
            float length = 0.55f + 0.15f * layer;
            Vector2 along = direction * length;
            Matrix shear = new(
                across.X, across.Y, 0f, 0f,
                -along.X, -along.Y, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f);
            Matrix transform = Matrix.CreateTranslation(-foot.X, -foot.Y, 0f) * shear * Matrix.CreateTranslation(foot.X, foot.Y, 0f) * _sceneTransform;
            // A shadow thrown toward the camera mirrors the sprite; without culling it still draws.
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, RasterizerState.CullNone, null, transform);
            batch.Draw(clip.Texture, foot, source, ShadowColor * (alpha / layers), 0f, clip.PixelOrigin, scale, SpriteEffects.None, 0f);
            batch.End();
        }
    }

    private static readonly Color ShadowColor = new(4, 3, 9);

    /// <summary>
    /// Where a figure's clip <paramref name="clipName"/> stands in its cycle (0–1) as last drawn,
    /// or null when the figure shows another clip: footsteps land on the drawn footfalls.
    /// </summary>
    public float? CyclePhase(object owner, string clipName) =>
        _figures.TryGetValue(owner, out FigureState? figure) ? figure.CyclePhase(clipName) : null;

    /// <summary>
    /// An afterimage of a rendered figure: the pose it showed <paramref name="age"/> seconds ago,
    /// drawn at <paramref name="position"/> in <paramref name="color"/>. False when no pose is known.
    /// </summary>
    public bool DrawGhost(SpriteBatch batch, object owner, Vector2 position, float age, Color color)
    {
        if (!_figures.TryGetValue(owner, out FigureState? figure) || figure.PoseAt(_time - age) is not { } pose)
        {
            return false;
        }

        batch.Draw(pose.Clip.Texture, position, pose.Source, color, 0f, pose.Clip.PixelOrigin, pose.Scale, SpriteEffects.None, 0f);
        return true;
    }

    private void DrawSource(SpriteBatch batch, SpriteClip clip, Rectangle source, Vector2 position, float scale, Color color)
    {
        if (_litSceneActive && clip.NormalMap is not null)
        {
            _lighting!.Draw(batch, clip, source, position, scale, color);
            return;
        }

        batch.Draw(clip.Texture, position, source, color, 0f, clip.PixelOrigin, scale, SpriteEffects.None, 0f);
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

        /// <summary>
        /// Enemies of a wave appear together and would breathe and sway in lockstep, like clones:
        /// each takes its own place in its first rest cycle and its own idle tempo (within 8 %).
        /// After an action the rest starts on its first frame as before (actions end on it).
        /// </summary>
        private readonly float _phase;
        private readonly float _tempo = 1f;
        private bool _phased;

        /// <summary>Seeded, so a tour run shows the same figures the same way each time.</summary>
        private static readonly Random Individuality = new(7121);

        public FigureState(bool individual = false)
        {
            if (!individual)
            {
                _phased = true;
                return;
            }
            _phase = (float)Individuality.NextDouble();
            _tempo = 0.92f + 0.16f * (float)Individuality.NextDouble();
        }

        public FacingTracker Facing { get; } = new();

        private float _lastHitRemaining;
        private float _impactAt = float.NegativeInfinity;
        private Vector2 _impactDirection;
        private float _impactStrength;

        /// <summary>A hit flash that starts (or restarts) marks a new blow; remember when and from where.</summary>
        public void TrackImpact(float time, float hitRemaining, Vector2 direction)
        {
            if (hitRemaining > _lastHitRemaining + 0.0001f)
            {
                _impactAt = time;
                _impactDirection = direction;
                // Longer flashes belong to heavier blows (core hits, the fatal one).
                _impactStrength = hitRemaining >= 0.155f ? 1.35f : 1f;
            }
            _lastHitRemaining = hitRemaining;
        }

        /// <summary>
        /// How the figure reacts to the last blow, as a pose on top of its clip: it is squashed
        /// for a moment, leans with the blow and rocks back once, and is washed light for the
        /// first frames. Never moves the figure's feet off its position.
        /// </summary>
        public (Vector2 Scale, float Lean, float Flash) ImpactPose(float time)
        {
            float t = time - _impactAt;
            if (t < 0f || t > 0.45f)
            {
                return (Vector2.One, 0f, 0f);
            }

            float strength = _impactStrength;
            float attack = MathHelper.Clamp(t / 0.03f, 0f, 1f);
            float after = MathF.Max(0f, t - 0.03f);
            float lean = 0.075f * strength * _impactDirection.X * attack * MathF.Exp(-after / 0.09f) * MathF.Cos(after * 15f);
            float squash = strength * MathF.Exp(-t / 0.07f);
            Vector2 scale = new(1f + 0.035f * squash, 1f - 0.055f * squash);
            float flash = t < 0.11f ? 0.82f * MathF.Pow(1f - t / 0.11f, 2f) * MathF.Min(1f, strength) : 0f;
            return (scale, lean, flash);
        }
        public SpriteClip? LastClip { get; private set; }
        public Rectangle LastSource { get; private set; }
        public Vector2 LastPosition { get; private set; }
        public float LastScale { get; private set; }
        public Color LastTint { get; private set; }
        public bool DissolveStarted { get; set; }

        /// <summary>Time and distance since this figure was last drawn.</summary>
        public float Advance(float time, Vector2 position, out float distance, out Vector2 moved)
        {
            float deltaTime = _hasDrawn ? MathHelper.Clamp(time - _lastTime, 0f, 0.25f) : 0f;
            distance = _hasDrawn ? MathF.Min(Vector2.Distance(position, _lastDrawPosition), MaxDistancePerDraw) : 0f;
            moved = _hasDrawn && distance > 0f ? Vector2.Normalize(position - _lastDrawPosition) * distance : Vector2.Zero;
            _hasDrawn = true;
            _lastTime = time;
            _lastDrawPosition = position;
            return deltaTime;
        }

        /// <summary>How long a figure settles from an action back into standing or running.</summary>
        public const float SettleDuration = 0.1f;

        /// <summary>The pose the figure left when it switched into a rest clip, fading out over it.</summary>
        public (SpriteClip Clip, Rectangle Source, float Scale, Color Tint)? Settling { get; private set; }
        public float SettleRemaining { get; private set; }

        /// <summary>Elapsed playback of <paramref name="name"/>; switching clips restarts, turning does not.</summary>
        public float PlayClip(string name, VisualClipDefinition clip, float deltaTime, float distance)
        {
            SettleRemaining = MathF.Max(0f, SettleRemaining - deltaTime);
            if (!string.Equals(_clipName, name, StringComparison.Ordinal) && !SameCycle(_clipName, name))
            {
                // Actions start at once (their first frame is the feedback); only the way back
                // into standing or running blends, so a figure settles instead of snapping.
                bool intoRest = name is VisualClips.Idle or VisualClips.Move;
                if (intoRest && LastClip is not null && _clipName.Length > 0)
                {
                    Settling = (LastClip, LastSource, LastScale, LastTint);
                    SettleRemaining = SettleDuration;
                }
                else
                {
                    SettleRemaining = 0f;
                }
                _clipName = name;
                _elapsed = 0f;
                if (!_phased && intoRest && clip.Loop)
                {
                    _elapsed = _phase * clip.Duration;
                }
                _phased = true;
                _cycleLength = clip.Duration;
                return _elapsed;
            }

            _clipName = name;
            _elapsed = ClipClock.Advance(_elapsed, clip, name == VisualClips.Idle ? deltaTime * _tempo : deltaTime, distance);
            _cycleLength = clip.Duration;
            return _elapsed;
        }

        private float _cycleLength;

        /// <summary>Clips that are one cycle drawn in variants (the charged walk): switching keeps the phase.</summary>
        private static bool SameCycle(string a, string b) =>
            a.StartsWith(VisualClips.AimMove, StringComparison.Ordinal) && b.StartsWith(VisualClips.AimMove, StringComparison.Ordinal);

        /// <summary>Where in its cycle the clip <paramref name="name"/> is (0–1), or null if another clip plays.</summary>
        public float? CyclePhase(string name) =>
            string.Equals(_clipName, name, StringComparison.Ordinal) && _cycleLength > 0f ? _elapsed / _cycleLength % 1f : null;

        public void RememberPose(SpriteClip clip, Rectangle source, Vector2 position, float scale, Color tint)
        {
            LastClip = clip;
            LastSource = source;
            LastPosition = position;
            LastScale = scale;
            LastTint = tint;
            _history[_historyNext] = (_lastTime, clip, source, scale);
            _historyNext = (_historyNext + 1) % _history.Length;
        }

        private readonly (float Time, SpriteClip? Clip, Rectangle Source, float Scale)[] _history = new (float, SpriteClip?, Rectangle, float)[32];
        private int _historyNext;

        /// <summary>The pose drawn closest to <paramref name="time"/> within the last half second.</summary>
        public (SpriteClip Clip, Rectangle Source, float Scale)? PoseAt(float time)
        {
            (float Time, SpriteClip? Clip, Rectangle Source, float Scale) best = default;
            float bestGap = float.MaxValue;
            foreach (var pose in _history)
            {
                if (pose.Clip is null)
                {
                    continue;
                }
                float gap = MathF.Abs(pose.Time - time);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = pose;
                }
            }
            return best.Clip is null || bestGap > 0.5f ? null : (best.Clip, best.Source, best.Scale);
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

    /// <summary>The vertical shade (opaque at the top, clear at the bottom), for flipped uses.</summary>
    public Texture2D ShadeTexture => _shade;

    /// <summary>The transform of the running lit scene pass, for callers that restart the batch.</summary>
    public Matrix SceneTransform => _sceneTransform;

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

    /// <summary>The soft round light used for spots and particles (premultiplied white).</summary>
    public Texture2D SoftSpot => _softSpot;

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

    /// <summary>The colour a figure is washed into for an instant when a blow lands.</summary>
    private static readonly Vector3 HitFlashColor = new(0.96f, 0.92f, 1f);

    /// <summary>
    /// The room's back light on lit figures: colour times strength (zero for none) and the
    /// screen direction it comes from. Set per area before the scene is drawn.
    /// </summary>
    public void SetBackLight(Vector3 color, Vector2 from)
    {
        if (_lighting is not null)
        {
            _lighting.RimColor = color;
            _lighting.RimDirection = from;
        }
    }

    /// <summary>A frame turned about its feet and scaled unevenly (a figure reacting to a blow), lit when possible.</summary>
    private void DrawFrame(SpriteBatch batch, SpriteClip clip, float elapsed, Vector2 position, Vector2 scale, float rotation, Color color, Vector4 flash)
    {
        if (_litSceneActive && clip.NormalMap is not null)
        {
            _lighting!.Draw(batch, clip, clip.GetSourceRectangle(elapsed), position, scale, rotation, color, flash);
            return;
        }

        batch.Draw(clip.Texture, position, clip.GetSourceRectangle(elapsed), color, rotation, clip.PixelOrigin, scale, SpriteEffects.None, 0f);
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

    /// <summary>
    /// Draws one frame of a prop's clip chosen by <paramref name="progress"/> (0 = first, 1 = last
    /// frame), on its foot point: an animation driven by a gameplay timer, such as a chest's lid.
    /// Returns false without the clip.
    /// </summary>
    public bool DrawPropFrame(SpriteBatch batch, string id, string clipName, Vector2 foot, float progress, Color tint)
    {
        SpriteClip? clip = Resolve(id, clipName, null, out _);
        if (clip is null)
        {
            return false;
        }

        RectangleF bounds = PropBounds(id, foot, Vector2.One);
        int index = (int)MathF.Round(MathHelper.Clamp(progress, 0f, 1f) * (clip.FrameCount - 1));
        float elapsed = (index + 0.5f) / clip.FramesPerSecond;
        Rectangle destination = new((int)MathF.Round(bounds.X), (int)MathF.Round(bounds.Y), (int)MathF.Round(bounds.Width), (int)MathF.Round(bounds.Height));
        batch.Draw(clip.Texture, destination, clip.GetSourceRectangle(elapsed), tint);
        return true;
    }

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

    public void DrawLostSoul(SpriteBatch batch, Soul soul, Player? player = null)
    {
        if (soul.State is SoulState.Released or SoulState.Consumed)
        {
            return;
        }

        float pulse = 0.94f + MathF.Sin(_time * 5f) * 0.07f;
        if (soul.State == SoulState.Residue)
        {
            if (player is null)
            {
                return;
            }
            // What remains after the release flies to the player as a small soul and rises to the
            // core as it arrives; a short tail of fading copies shows where it came from.
            Vector2 target = player.Position + player.FacingDirection * 2f;
            Vector2 toward = target - soul.Position;
            float distance = toward.Length();
            float arriving = MathHelper.Clamp(1f - distance / 160f, 0f, 1f);
            Vector2 lift = new(0f, -FigureHeights.Core * arriving - 18f * (1f - arriving));
            Vector2 back = distance > 0.5f ? -toward / distance : Vector2.Zero;
            for (int index = 3; index >= 1; index--)
            {
                DrawSprite(batch, VisualIds.LostSoul, soul.Position + lift + back * (index * 9f), 0.36f - index * 0.05f, Color.White * (0.4f - index * 0.1f));
            }
            DrawSprite(batch, VisualIds.LostSoul, soul.Position + lift, 0.38f * pulse, Color.White);
            return;
        }

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
