using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// <c>--slice-visual-test</c>: plays the shore of the prologue and wave 1 of the arena without
/// input and records a fixed, named series of captures (spec "visual-quality-checks"). Each step
/// waits for its moment; if the moment does not come in time, the run fails with the step's name.
/// Setting the environment variable <c>SLICE_TEST_WITHOUT_HOLLOWS=1</c> removes every Hollow as
/// soon as it appears, to check that a missing moment fails the run with the capture's name.
/// </summary>
internal sealed class SliceVisualTest
{
    private sealed record Step(string Name, Action? Enter, Func<bool> Ready, bool Capture, float Timeout, float MinWait = 0f, Action? EveryFrame = null);

    private static readonly Vector2 ArenaCentre = new(900f, 560f);

    /// <summary>A clear patch of floor away from the wave trigger ring, for the shader test shots.</summary>
    private static readonly Vector2 TestBench = new(480f, 360f);

    private readonly GameWorld _world;
    private readonly InputState _input;
    private readonly Viewport _viewport;
    private readonly List<Step> _steps = [];
    private readonly List<string> _captured = [];
    private int _index = -1;
    private float _stepTime;
    private int _coreHitsBefore;
    private float _hollowSeenFor;
    private readonly bool _withoutHollows = Environment.GetEnvironmentVariable("SLICE_TEST_WITHOUT_HOLLOWS") == "1";

    public SliceVisualTest(GameWorld world, InputState input, Viewport viewport)
    {
        _world = world;
        _input = input;
        _viewport = viewport;
        BuildShore();
        BuildArena();
    }

    /// <summary>Name of the capture the next draw must save, if any.</summary>
    public string? PendingCapture { get; private set; }

    public bool Finished { get; private set; }
    public int ExitCode { get; private set; }

    /// <summary>All capture names, in order; the spec's required series is a subset.</summary>
    public IReadOnlyList<string> CaptureNames => _steps.Where(step => step.Capture).Select(step => step.Name).ToArray();

    public void Update(float deltaTime)
    {
        if (Finished || PendingCapture is not null)
        {
            return;
        }

        if (_index < 0)
        {
            Advance();
        }

        Step step = _steps[_index];
        _stepTime += deltaTime;
        _hollowSeenFor = NearestHollow() is null ? 0f : _hollowSeenFor + deltaTime;
        if (_withoutHollows && _world.AutomatedEnemies.OfType<Hollow>().Any(hollow => hollow.IsAlive))
        {
            _world.DefeatAutomatedEnemies();
        }

        if (_world.PlayerDead)
        {
            Fail(step.Name, "player died");
            return;
        }

        // The moment is checked before any input of this frame, so the capture shows the state
        // that was reached, not the start of the next action.
        if (_stepTime >= step.MinWait && step.Ready())
        {
            if (step.Capture)
            {
                PendingCapture = step.Name;
                return;
            }
            Advance();
            return;
        }

        if (_stepTime >= step.Timeout)
        {
            Fail(step.Name, "timeout");
            return;
        }

        step.EveryFrame?.Invoke();
    }

    /// <summary>Called by the game after it tried to save <see cref="PendingCapture"/>.</summary>
    public void CaptureCompleted(bool saved, string result)
    {
        string name = PendingCapture!;
        PendingCapture = null;
        if (!saved)
        {
            Fail(name, "capture not saved: " + result);
            return;
        }

        _captured.Add(name);
        Advance();
    }

    private void Advance()
    {
        _index++;
        _stepTime = 0f;
        if (_index >= _steps.Count)
        {
            Console.WriteLine($"SLICE_VISUAL_TEST_PASS captures={_captured.Count} names={string.Join(",", _captured)}");
            Finished = true;
            ExitCode = 0;
            return;
        }

        _steps[_index].Enter?.Invoke();
    }

    private void Fail(string name, string reason)
    {
        Console.WriteLine($"SLICE_VISUAL_TEST_FAIL capture={name} reason={reason}");
        Finished = true;
        ExitCode = 1;
    }

    private void Do(string name, Action enter, float wait = 0.05f) =>
        _steps.Add(new Step(name, enter, () => true, false, 1f, wait));

    private void Shot(string name, Action? enter, Func<bool> ready, float timeout, float minWait = 0f, Action? everyFrame = null) =>
        _steps.Add(new Step(name, enter, ready, true, timeout, minWait, everyFrame));

    private void Wait(string name, Func<bool> ready, float timeout, Action? enter = null, Action? everyFrame = null) =>
        _steps.Add(new Step(name, enter, ready, false, timeout, 0f, everyFrame));

    private Hollow? NearestHollow() =>
        _world.AutomatedEnemies.OfType<Hollow>().Where(hollow => hollow.IsAlive)
            .OrderBy(hollow => Vector2.DistanceSquared(hollow.Position, _world.PlayerPosition)).FirstOrDefault();

    private void AimAt(Vector2 target) => _world.SetAutomatedAim(target - _world.PlayerPosition);

    // ---- Prologue: the shore ----

    private void BuildShore()
    {
        Do("shore_enter", () =>
        {
            _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.PrologueFindTrace, 1), _viewport);
            _world.SetAutomatedAim(Vector2.UnitX);
        });
        Wait("shore_ready", () => _world.Phase == GamePhase.Prologue && _world.PrologueStage == PrologueStage.FindTrace, 5f);

        Shot("shore_overview", () =>
        {
            _world.SetAutomatedHudHidden(true);
            _world.SetAutomatedOverview(true);
        }, () => true, 3f, minWait: 1.2f);
        Do("shore_overview_done", () =>
        {
            _world.SetAutomatedHudHidden(false);
            _world.SetAutomatedOverview(false);
        });

        Shot("shore_soul_sense_trace", () =>
        {
            _world.PlaceAutomatedPlayer(PrologueDirector.SoulTrace + new Vector2(-150f, 60f));
            _world.SetAutomatedSoulSense(true);
            AimAt(PrologueDirector.SoulTrace);
        }, () => _world.PrologueStage >= PrologueStage.TraceWitnessed, 6f, minWait: 1.1f);

        Shot("shore_first_hollow", () => _world.SetAutomatedSoulSense(false), () => _hollowSeenFor >= 1f, 12f, everyFrame: () =>
        {
            if (NearestHollow() is { } hollow)
            {
                AimAt(hollow.Position);
            }
        });
    }

    // ---- Arena, wave 1 ----

    private void BuildArena()
    {
        Do("arena_enter", () =>
        {
            _world.ClearAutomatedStaging();
            _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.Arena, 1), _viewport);
            _world.SetAutomatedAim(Vector2.UnitY);
        });
        Shot("arena_hollow_swipe_telegraph", () => _world.PlaceAutomatedPlayer(ArenaCentre),
            () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.OfType<Hollow>().Any(hollow => hollow.State == HollowState.Telegraph),
            20f,
            everyFrame: () =>
            {
                if (NearestHollow() is { } hollow)
                {
                    AimAt(hollow.Position);
                }
            });

        Shot("arena_core_hit", () =>
        {
            _coreHitsBefore = _world.AutomatedCoreHits;
            _world.SetAutomatedSoulSense(true);
            if (NearestHollow() is { } hollow)
            {
                _world.PlaceAutomatedPlayer(hollow.Position + new Vector2(-70f, 10f));
            }
        }, () => _world.AutomatedCoreHits > _coreHitsBefore, 6f, everyFrame: () =>
        {
            if (NearestHollow() is { } hollow)
            {
                AimAt(hollow.CorePosition);
                if (_world.AutomatedPlayer.Scythe.ActiveStep == 0)
                {
                    _input.InjectMousePresses(left: true, right: false);
                }
            }
        });

        Shot("arena_soul_sense", null, () => _world.AutomatedEnemies.Any(enemy => enemy.IsAlive), 2f, minWait: 0.45f);

        Shot("arena_hollow_dissolve", () =>
        {
            _world.SetAutomatedSoulSense(false);
            if (!_world.AutomatedEnemies.OfType<Hollow>().Any(hollow => hollow.IsAlive))
            {
                return;
            }
            _world.DefeatAutomatedEnemies();
        }, () => _world.AutomatedEnemies.OfType<Hollow>().Any(), 2f, minWait: 0.32f);

        Shot("arena_soul_release", null, () => _world.AutomatedSouls.Any(soul => soul.State == SoulState.Releasing), 5f, minWait: 0.1f);

        Wait("arena_wave_clear", () => _world.LoopState == ArenaLoopState.Intermission, 12f);

        Shot("arena_overview", () =>
        {
            _world.PlaceAutomatedPlayer(ArenaCentre);
            _world.SetAutomatedHudHidden(true);
            _world.SetAutomatedOverview(true);
        }, () => true, 2f, minWait: 0.5f);
        Do("arena_overview_done", () =>
        {
            _world.SetAutomatedHudHidden(false);
            _world.SetAutomatedOverview(false);
        });

        foreach (string direction in VisualDirections.All)
        {
            int sector = Array.IndexOf(["e", "se", "s", "sw", "w", "nw", "n", "ne"], direction);
            Vector2 aim = new(MathF.Cos(sector * MathHelper.PiOver4), MathF.Sin(sector * MathHelper.PiOver4));
            Shot($"arena_idle_{direction}", () =>
            {
                _world.PlaceAutomatedPlayer(ArenaCentre);
                _world.SetAutomatedAim(aim);
            }, () => true, 2f, minWait: 0.7f);
        }

        Shot("arena_run", () =>
        {
            _world.PlaceAutomatedPlayer(ArenaCentre - new Vector2(260f, 0f));
            _world.SetAutomatedAim(Vector2.UnitX);
        }, () => _world.AutomatedPlayer.Velocity.LengthSquared() > 1000f, 3f, minWait: 0.6f, everyFrame: () => _input.InjectKeyDown(Keys.D));

        Do("arena_slashes_ready", () =>
        {
            _world.PlaceAutomatedPlayer(ArenaCentre);
            _world.SetAutomatedAim(Vector2.UnitX);
        }, wait: 0.4f);
        for (int step = 1; step <= 3; step++)
        {
            int swing = step;
            Shot($"arena_slash_{swing}", null,
                () => _world.AutomatedPlayer.Scythe.ActiveStep == swing && _world.AutomatedPlayer.Scythe.HasStruck,
                2f,
                everyFrame: () =>
                {
                    // Click until this swing of the chain has started; the previous one has already struck.
                    if (_world.AutomatedPlayer.Scythe.ActiveStep != swing)
                    {
                        _input.InjectMousePresses(left: true, right: false);
                    }
                });
        }

        Shot("arena_cannon_aim", () =>
        {
            _world.PlaceAutomatedPlayer(ArenaCentre);
            _world.SetAutomatedAim(Vector2.Normalize(new Vector2(1f, 0.6f)));
        }, () => _world.AutomatedPlayer.Cannon.State == SoulCannonState.Charging && _world.AutomatedPlayer.Cannon.ChargeProgress > 0.45f, 3f,
            everyFrame: () => _input.InjectMousePresses(left: false, right: true));

        Shot("arena_dash_ignition", () =>
        {
            _world.PlaceAutomatedPlayer(ArenaCentre - new Vector2(200f, 0f));
            _world.SetAutomatedAim(Vector2.UnitX);
        }, () => _world.AutomatedPlayer.IsDashing, 2f, minWait: 0.7f, everyFrame: () =>
        {
            if (_stepTime >= 0.7f)
            {
                _input.InjectKeyDown(Keys.D);
                _input.InjectKeyPress(Keys.Space);
            }
        });

        Shot("arena_occluder", () =>
        {
            _world.PlaceAutomatedPlayer(ArenaCentre);
            _world.SetAutomatedAim(Vector2.UnitY);
            _world.PlaceAutomatedOccluder(ArenaCentre + new Vector2(10f, 110f));
        }, () => true, 2f, minWait: 0.6f);

        Shot("arena_lighting_test", () =>
        {
            _world.ClearAutomatedStaging();
            _world.PlaceAutomatedPlayer(TestBench + new Vector2(0f, 150f));
            _world.ShowAutomatedLightingTest(TestBench, TestBench - new Vector2(95f, 0f), TestBench + new Vector2(95f, 0f));
        }, () => true, 2f, minWait: 0.4f);

        Shot("arena_death_flame_trail", () =>
        {
            _world.ClearAutomatedStaging();
            _world.PlaceAutomatedPlayer(TestBench + new Vector2(0f, 150f));
            _world.ShowAutomatedDeathFlameTrail(TestBench);
        }, () => true, 2f, minWait: 0.3f);

        Shot("arena_blender_figure", () =>
        {
            _world.ClearAutomatedStaging();
            _world.PlaceAutomatedPlayer(TestBench + new Vector2(0f, 220f));
            _world.ShowAutomatedLightingTest(TestBench + new Vector2(150f, -40f), TestBench + new Vector2(-400f, -400f), TestBench + new Vector2(-400f, -400f));
            _world.ShowAutomatedRenderedFigures(TestBench + new Vector2(-170f, 80f));
        }, () => true, 2f, minWait: 0.5f);

        Do("arena_done", () => _world.ClearAutomatedStaging());
    }
}
