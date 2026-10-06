using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Control hooks for <c>--slice-visual-test</c> (Debugging/SliceVisualTest). They place and
/// aim the player, spawn and defeat enemies and stage test figures; they never change rules.
/// </summary>
public sealed partial class GameWorld
{
    private Vector2? _automatedAim;
    private bool _automatedHideHud;
    private bool _automatedOverview;
    private int _automatedCoreHits;
    private readonly List<(Vector2 Position, string Clip)> _automatedLitFigures = [];
    private Vector2? _automatedLightSource;
    private Vector2? _automatedTrailCentre;
    private Vector2? _automatedRenderedFigures;
    private readonly object[] _renderedFigureOwners = Enumerable.Range(0, 8).Select(_ => new object()).ToArray();

    internal Vector2 PlayerPosition => _player.Position;
    internal Player AutomatedPlayer => _player;
    internal IReadOnlyList<Enemy> AutomatedEnemies => _enemies;
    internal IReadOnlyList<Soul> AutomatedSouls => _souls;
    internal int AutomatedCoreHits => _automatedCoreHits;
    internal Rectangle AutomatedCombatBounds => ActiveCombatBounds;

    /// <summary>Aims as if the mouse were 200 units from the player in <paramref name="direction"/>; null returns control to the mouse.</summary>
    internal void SetAutomatedAim(Vector2? direction) =>
        _automatedAim = direction is { } value && value.LengthSquared() > 0.0001f ? Vector2.Normalize(value) : null;

    internal void SetAutomatedHudHidden(bool hidden) => _automatedHideHud = hidden;

    internal AudioDirector AutomatedAudio => _audio;

    /// <summary>The drawn run phase of the player (0–1), or null when the figure is not running.</summary>
    internal float? AutomatedRunPhase => _art.CyclePhase(_player, VisualClips.Move);

    /// <summary>Zooms the camera out to the whole world for overview shots.</summary>
    internal void SetAutomatedOverview(bool overview) => _automatedOverview = overview;

    internal void PlaceAutomatedPlayer(Vector2 position) => _player.Reset(position);

    /// <summary>Jumps into a prologue stage through the same entry the regular flow uses.</summary>
    internal void EnterAutomatedPrologueStage(PrologueStage stage, Viewport viewport)
    {
        if (_phase != GamePhase.Prologue)
        {
            BeginPrologue(viewport);
        }
        if (stage == PrologueStage.Arrival)
        {
            ClearPrologueActivity();
            EnterThreshold();
        }
        else
        {
            DebugEnterPrologueStage(stage);
        }
        _camera.Follow(_player.Position, PrologueDirector.WorldBounds, viewport, 1f);
    }

    internal Hollow SpawnAutomatedHollow(Vector2 position)
    {
        Hollow hollow = new(position, _enemies.Count + 1);
        _enemies.Add(hollow);
        return hollow;
    }

    /// <summary>A full-cannon blow of one point on every living enemy of type <typeparamref name="T"/> (staggers them).</summary>
    internal void StaggerAutomatedEnemies<T>() where T : Enemy
    {
        foreach (T enemy in _enemies.OfType<T>().Where(enemy => enemy.IsAlive).ToArray())
        {
            ApplyEnemyDamage(enemy, new DamageInfo(1, _player.FacingDirection, enemy.Position, IsFullCannon: true));
        }
    }

    /// <summary>Defeats every living enemy that is not a <typeparamref name="T"/> (to show one enemy alone).</summary>
    internal void DefeatAutomatedEnemiesExcept<T>() where T : Enemy
    {
        foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive && enemy is not T).ToArray())
        {
            ApplyEnemyDamage(enemy, new DamageInfo(enemy.Health + enemy.MaxHealth, Vector2.Zero, enemy.Position));
        }
        _waveRun.DiscardRemaining();
        _pendingSpawns.Clear();
    }

    internal void DefeatAutomatedEnemies()
    {
        foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive).ToArray())
        {
            ApplyEnemyDamage(enemy, new DamageInfo(enemy.Health + enemy.MaxHealth, Vector2.Zero, enemy.Position));
        }
        _waveRun.DiscardRemaining();
        _pendingSpawns.Clear();
    }

    /// <summary>Stands the lighting test figures (flat and tilted normal map) next to a Death Flame light.</summary>
    internal void ShowAutomatedLightingTest(Vector2 lightPosition, Vector2 flatFigure, Vector2 tiltedFigure)
    {
        _automatedLightSource = lightPosition;
        _automatedLitFigures.Clear();
        _automatedLitFigures.Add((flatFigure, "flat"));
        _automatedLitFigures.Add((tiltedFigure, "tilted"));
    }

    /// <summary>Draws a Death Flame test ribbon arcing around <paramref name="centre"/>.</summary>
    internal void ShowAutomatedDeathFlameTrail(Vector2? centre) => _automatedTrailCentre = centre;

    /// <summary>Stands the Blender test figure in all eight directions in a row starting at <paramref name="start"/>.</summary>
    internal void ShowAutomatedRenderedFigures(Vector2? start) => _automatedRenderedFigures = start;

    internal void ClearAutomatedStaging()
    {
        _automatedRenderedFigures = null;
        _automatedLitFigures.Clear();
        _automatedLightSource = null;
        _automatedTrailCentre = null;
        _sceneProps.Clear();
    }

    private Vector2 AutomatedAimOr(Vector2 mouseWorld) =>
        _automatedAim is { } aim ? _player.Position + aim * 200f : mouseWorld;

    private void ApplyAutomatedOverview(Viewport viewport)
    {
        if (!_automatedOverview)
        {
            return;
        }

        Rectangle world = ActiveWorldBounds;
        _camera.Zoom = MathF.Min(viewport.Width / (float)world.Width, viewport.Height / (float)world.Height);
        _camera.Follow(world.Center.ToVector2(), world, viewport, 1f);
    }

    private void DrawAutomatedStaging(SpriteBatch batch)
    {
        if (_automatedLightSource is { } light)
        {
            _art.DrawLoopingEffect(batch, _automatedLitFigures, VisualIds.DeathFlameLoop, light, 0f, 0.56f, Color.White);
        }
        foreach ((Vector2 position, string clip) in _automatedLitFigures)
        {
            _art.DrawSprite(batch, VisualIds.TestLitFigure, position, 1f, Color.White, clip);
        }
        if (_automatedRenderedFigures is { } start)
        {
            for (int index = 0; index < 8; index++)
            {
                Vector2 facing = new(MathF.Cos(index * MathHelper.PiOver4), MathF.Sin(index * MathHelper.PiOver4));
                _art.DrawCharacter(batch, _renderedFigureOwners[index], VisualIds.TestBlenderFigure, VisualClips.Idle, facing, start + new Vector2(index * 92f, 0f), 1f, Color.White);
            }
        }
        if (_automatedTrailCentre is { } centre)
        {
            List<Vector2> arc = [];
            for (int index = 0; index <= 24; index++)
            {
                float angle = MathHelper.ToRadians(-150f + index * 120f / 24f);
                arc.Add(centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 110f);
            }
            _art.DrawDeathFlameTrail(batch, arc, 34f, 1f);
        }
    }

    private void DrawAutomatedLights(SoulfireRenderer renderer, SpriteBatch batch)
    {
        if (_automatedLightSource is { } light)
        {
            renderer.DrawGlow(batch, light, SoulfireRenderSettings.DeathFlameGlowRadius, GameBalance.DeathFlameBright, SoulfireRenderSettings.DeathFlameGlowIntensity);
        }
    }
}
