using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Entities;

namespace TheLostSoulOfFire.Audio;

/// <summary>
/// When each enemy speaks between its attacks (presentation only): a Hollow calls after someone
/// who leaves while it shuffles closer, a Burning cackles while it stalks, a Devourer growls on
/// its way to the player. Each enemy keeps its own timer; a fresh one often calls soon after it
/// appears. Calls stop while it attacks, staggers or dies, the whole room shares one pace (more
/// enemies call less often each, at least <see cref="MinimumGap"/> apart), and the farther an
/// enemy stands, the quieter it is.
/// </summary>
public sealed class EnemyVoices
{
    private const float MinimumGap = 0.55f;
    private const float AfterAttack = 0.8f;
    private const float Hearing = 1300f;

    private readonly Dictionary<Enemy, float> _next = new(ReferenceEqualityComparer.Instance);
    private readonly List<Enemy> _gone = [];
    private readonly Random _random = new(613);
    private float _sinceLast = 10f;

    public void Update(float deltaTime, IReadOnlyList<Enemy> enemies, Vector2 listener, bool active, Action<AudioCue, float, Enemy> speak)
    {
        _sinceLast += deltaTime;
        if (!active)
        {
            _next.Clear();
            return;
        }

        int alive = 0;
        foreach (Enemy enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                alive++;
            }
        }
        float crowd = MathF.Max(1f, alive / 3f);

        _gone.Clear();
        foreach (Enemy enemy in _next.Keys)
        {
            if (!enemy.IsAlive || !Contains(enemies, enemy))
            {
                _gone.Add(enemy);
            }
        }
        foreach (Enemy enemy in _gone)
        {
            _next.Remove(enemy);
        }

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            if (!_next.TryGetValue(enemy, out float left))
            {
                // Newly in the fight: most call out soon after they appear.
                _next[enemy] = _random.NextDouble() < 0.7 ? Range(0.4f, 1.4f) : Interval(enemy) * crowd;
                continue;
            }

            if (CallOf(enemy) is not { } call)
            {
                // Busy attacking, staggered or feeding: it calls again only a while after.
                _next[enemy] = MathF.Max(left, AfterAttack);
                continue;
            }

            left -= deltaTime;
            if (left > 0f)
            {
                _next[enemy] = left;
                continue;
            }
            if (_sinceLast < MinimumGap)
            {
                _next[enemy] = Range(0.2f, 0.5f);
                continue;
            }

            float nearness = MathHelper.Clamp(1f - Vector2.Distance(enemy.Position, listener) / Hearing, 0.2f, 1f);
            speak(call, LoudnessOf(enemy) * (0.45f + 0.55f * nearness), enemy);
            _sinceLast = 0f;
            _next[enemy] = Interval(enemy) * crowd;
        }
    }

    public void Clear() => _next.Clear();

    private static AudioCue? CallOf(Enemy enemy) => enemy switch
    {
        Hollow { State: HollowState.Approach or HollowState.Pause } => AudioCue.HollowCall,
        Burning { State: BurningState.Approach } => AudioCue.BurningCackle,
        Devourer { State: DevourerState.ApproachPlayer } => AudioCue.DevourerGrowl,
        _ => null
    };

    private float Interval(Enemy enemy) => enemy switch
    {
        Burning => Range(2.5f, 5f),
        Devourer => Range(4f, 7.5f),
        _ => Range(3.5f, 7f)
    };

    private static float LoudnessOf(Enemy enemy) => enemy switch
    {
        Burning => 0.6f,
        Devourer => 0.72f,
        _ => 0.55f
    };

    private static bool Contains(IReadOnlyList<Enemy> enemies, Enemy enemy)
    {
        foreach (Enemy candidate in enemies)
        {
            if (ReferenceEquals(candidate, enemy))
            {
                return true;
            }
        }
        return false;
    }

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);
}
