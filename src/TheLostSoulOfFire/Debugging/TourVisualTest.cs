using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// <c>--tour-visual-test</c>: walks through every area and state of the game without input
/// (title, all prologue sectors, completion, hub, door, arena waves with every enemy type,
/// chest, death, pause and character menus, sandbox) and saves stills and short frame series
/// to <c>artifacts/tour/&lt;run&gt;/</c> for review of art, animation and readability. The game
/// steps a fixed 1/60 s per frame in this mode, so a series shows every frame of a motion.
/// <c>TOUR_ONLY=arena,hub</c> keeps only the stations whose name starts with one of the prefixes.
/// </summary>
internal sealed class TourVisualTest
{
    private sealed record Step(string Name, Action? Enter, Func<bool> Ready, bool Capture, float Timeout, float MinWait = 0f, Action? EveryFrame = null)
    {
        public string Station { get; init; } = "";
    }

    private readonly Dictionary<string, List<double>> _frameTimes = new();

    private readonly Dictionary<string, List<double>> _wallTimes = new();
    private bool _lastFrameCaptured;

    /// <summary>
    /// Wall time since the previous frame with vertical sync off: CPU and GPU together. Frames
    /// around a capture (reading back and saving the image) are left out.
    /// </summary>
    public void RecordWallTime(double milliseconds, bool capturedThisFrame)
    {
        bool skip = capturedThisFrame || _lastFrameCaptured;
        _lastFrameCaptured = capturedThisFrame;
        if (skip || Finished || _index < 0 || _index >= _steps.Count)
        {
            return;
        }

        string station = _steps[_index].Station;
        if (!_wallTimes.TryGetValue(station, out List<double>? times))
        {
            _wallTimes[station] = times = [];
        }
        times.Add(milliseconds);
    }

    /// <summary>CPU time of one frame (update and draw, without saving a capture), filed under the running station.</summary>
    public void RecordFrameTime(double milliseconds)
    {
        if (Finished || _index < 0 || _index >= _steps.Count)
        {
            return;
        }

        string station = _steps[_index].Station;
        if (!_frameTimes.TryGetValue(station, out List<double>? times))
        {
            _frameTimes[station] = times = [];
        }
        times.Add(milliseconds);
    }

    private void ReportFrameTimes()
    {
        foreach ((string station, List<double> times) in _frameTimes)
        {
            if (times.Count == 0)
            {
                continue;
            }
            times.Sort();
            double average = times.Average();
            double p95 = times[Math.Min(times.Count - 1, (int)(times.Count * 0.95))];
            string wall = "";
            if (_wallTimes.TryGetValue(station, out List<double>? walls) && walls.Count > 10)
            {
                walls.Sort();
                double wallAverage = walls.Average();
                double wallP95 = walls[Math.Min(walls.Count - 1, (int)(walls.Count * 0.95))];
                wall = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $" wall_avg_ms={wallAverage:0.00} wall_p95_ms={wallP95:0.00} fps_avg={1000.0 / wallAverage:0}");
            }
            Console.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"TOUR_PERF station={station} frames={times.Count} avg_ms={average:0.00} p95_ms={p95:0.00} max_ms={times[^1]:0.00}") + wall);
        }
    }

    private static readonly Vector2 ArenaCentre = new(900f, 560f);

    private readonly GameWorld _world;
    private readonly InputState _input;
    private readonly Viewport _viewport;
    private readonly List<Step> _steps = [];
    private readonly string[] _only;
    private readonly string _directory;
    private int _index = -1;
    private int _captured;
    private float _stepTime;
    private string _station = string.Empty;

    private readonly Func<IReadOnlyList<string>>? _missingVisuals;

    public TourVisualTest(GameWorld world, InputState input, Viewport viewport, string? registryError = null,
        Func<IReadOnlyList<string>>? missingVisuals = null)
    {
        _missingVisuals = missingVisuals;
        if (registryError is not null)
        {
            // An invalid registry turns every graphic into a dummy; the tour would only photograph placeholders.
            Console.WriteLine($"TOUR_VISUAL_TEST_FAIL step=start reason=registry {registryError.ReplaceLineEndings(" ")}");
            Finished = true;
            ExitCode = 1;
        }

        _world = world;
        _input = input;
        _viewport = viewport;
        _only = (Environment.GetEnvironmentVariable("TOUR_ONLY") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _directory = Path.Combine(ScreenshotCapture.RepositoryRoot(), "artifacts", "tour", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        Station("title", BuildTitle);
        Station("prologue_waking", BuildWaking);
        Station("prologue_shore", BuildShore);
        Station("prologue_search", BuildSearch);
        Station("prologue_escape", BuildEscape);
        Station("prologue_transit", BuildTransit);
        Station("prologue_threshold", BuildThreshold);
        Station("hub", BuildHub);
        Station("arena_wave1", BuildArenaWave1);
        Station("arena_player", BuildPlayerMotion);
        Station("arena_burning", BuildBurning);
        Station("arena_devourer", BuildDevourer);
        Station("arena_chest", BuildChest);
        Station("arena_death", BuildDeath);
        Station("menus", BuildMenus);
        Station("arena_complete", BuildComplete);
        Station("sandbox", BuildSandbox);
    }

    private bool _advancePending;

    public string? PendingCapture { get; private set; }
    public bool Finished { get; private set; }
    public int ExitCode { get; private set; }

    public void Update(float deltaTime)
    {
        if (Finished || PendingCapture is not null)
        {
            return;
        }

        if (_index < 0 || _advancePending)
        {
            // A step after a capture starts here, not in Capture(): input injected at the end
            // of a frame would be cleared before the world reads it.
            _advancePending = false;
            Advance();
            if (Finished)
            {
                return;
            }
        }

        Step step = _steps[_index];
        _stepTime += deltaTime;
        _frame++;
        // Held input continues on the frame of a capture, so a series never drops it.
        step.EveryFrame?.Invoke();
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
            Console.WriteLine($"TOUR_VISUAL_TEST_FAIL step={step.Name} reason=timeout phase={_world.Phase} loop={_world.LoopState}");
            Finished = true;
            ExitCode = 1;
        }
    }

    /// <summary>Saves the pending capture from the finished frame and moves on.</summary>
    public void Capture(RenderTarget2D target)
    {
        string name = PendingCapture!;
        PendingCapture = null;
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, $"{++_captured:000}_{name}.png");
        if (!ScreenshotCapture.TrySaveVirtualTargetTo(target, path, out string error))
        {
            Console.WriteLine($"TOUR_VISUAL_TEST_FAIL step={name} reason=capture {error}");
            Finished = true;
            ExitCode = 1;
            return;
        }
        _advancePending = true;
    }

    private void Advance()
    {
        _index++;
        _stepTime = 0f;
        if (_index >= _steps.Count)
        {
            ReportFrameTimes();
            // Any visual drawn as a dummy anywhere on the tour is a placeholder left on screen.
            IReadOnlyList<string> missing = _missingVisuals?.Invoke() ?? [];
            if (missing.Count > 0)
            {
                Console.WriteLine($"TOUR_VISUAL_TEST_FAIL step=end reason=placeholders {string.Join(",", missing)}");
                Finished = true;
                ExitCode = 1;
                return;
            }
            Console.WriteLine("TOUR_AUDIO hall_tails=" + string.Join(",",
                _world.AutomatedAudio.HallTailsPlayed.Select(pair => $"{pair.Key}:{pair.Value}")));
            Console.WriteLine($"TOUR_VISUAL_TEST_PASS captures={_captured} dir={_directory}");
            Finished = true;
            ExitCode = 0;
            return;
        }

        _steps[_index].Enter?.Invoke();
    }

    // ---- step builders ----

    private void Station(string name, Action build)
    {
        if (_only.Length > 0 && !_only.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return;
        }
        _station = name;
        Do("reset", () =>
        {
            _input.InjectMousePresses(false, false);
            _world.SetAutomatedAim(null);
            _world.SetAutomatedSoulSense(false);
            _world.SetAutomatedHudHidden(false);
            _world.SetAutomatedOverview(false);
            _world.ClearAutomatedStaging();
        });
        build();
    }

    private string Named(string name) => $"{_station}_{name}";

    private int _frame;

    /// <summary>Footsteps while the tour runs: each with the run phase drawn at that moment.</summary>
    private void LogStep(AudioCue cue)
    {
        if (cue is AudioCue.Footstep or AudioCue.FootstepWood)
        {
            Console.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"TOUR_STEP frame={_frame} phase={_world.AutomatedRunPhase ?? -1f:0.00}"));
        }
    }

    private void Do(string name, Action enter, float wait = 0.02f) =>
        _steps.Add(new Step(Named(name), enter, () => true, false, wait + 1f, wait) { Station = _station });

    private void Shot(string name, Action? enter = null, Func<bool>? ready = null, float timeout = 10f, float minWait = 0f, Action? everyFrame = null) =>
        _steps.Add(new Step(Named(name), enter, ready ?? (() => true), true, timeout, minWait, everyFrame) { Station = _station });

    private void Wait(string name, Func<bool> ready, float timeout, Action? enter = null, Action? everyFrame = null) =>
        _steps.Add(new Step(Named(name), enter, ready, false, timeout, 0f, everyFrame) { Station = _station });

    /// <summary>A series of <paramref name="frames"/> captures, one every <paramref name="every"/> frames.</summary>
    private void Series(string name, int frames, int every = 1, Action? everyFrame = null)
    {
        for (int index = 0; index < frames; index++)
        {
            _steps.Add(new Step(Named($"{name}_{index:00}"), null, () => true, true, 2f, (every - 1) / 60f + 0.0001f, everyFrame) { Station = _station });
        }
    }

    private Enemy? Nearest<T>() where T : Enemy =>
        _world.AutomatedEnemies.OfType<T>().Where(enemy => enemy.IsAlive)
            .OrderBy(enemy => Vector2.DistanceSquared(enemy.Position, _world.PlayerPosition)).FirstOrDefault();

    private void AimAt(Vector2 target) => _world.SetAutomatedAim(target - _world.PlayerPosition);

    private void Overview(string name)
    {
        Shot(name, () =>
        {
            _world.SetAutomatedHudHidden(true);
            _world.SetAutomatedOverview(true);
        }, minWait: 0.6f);
        Do(name + "_done", () =>
        {
            _world.SetAutomatedHudHidden(false);
            _world.SetAutomatedOverview(false);
        }, 0.5f);
    }

    // ---- stations ----

    private void BuildTitle()
    {
        Shot("menu", minWait: 1.6f, ready: () => _world.Phase == GamePhase.Title);
        Series("idle", 6, 20);
        Do("settings", () => { _input.InjectKeyPress(Keys.Down); }, 0.15f);
        Do("settings_down", () => { _input.InjectKeyPress(Keys.Down); }, 0.15f);
        Shot("menu_selection", minWait: 0.2f);
        Do("settings_open", () => _input.InjectKeyPress(Keys.Enter), 0.3f);
        Shot("settings", minWait: 0.2f);
        Do("settings_back", () => _input.InjectKeyPress(Keys.Escape), 0.3f);
        Do("singleplayer_open", () => _input.InjectKeyPress(Keys.Enter), 0.3f);
        Shot("singleplayer", minWait: 0.2f);
        Do("singleplayer_back", () => _input.InjectKeyPress(Keys.Escape), 0.3f);
        Do("quit_up", () => _input.InjectKeyPress(Keys.Up), 0.15f);
        Do("quit_open", () => _input.InjectKeyPress(Keys.Enter), 0.3f);
        Shot("quit_confirm", minWait: 0.2f);
        Do("quit_cancel", () => _input.InjectKeyPress(Keys.Escape), 0.3f);
    }

    private void BuildWaking()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.Prologue, 1), _viewport));
        Series("waking", 8, 24);
        Wait("find_trace", () => _world.PrologueStage == PrologueStage.FindTrace, 8f);
        Shot("find_trace", minWait: 0.8f);
    }

    private void BuildShore()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.PrologueFindTrace, 1), _viewport));
        Overview("overview");
        Shot("walk", () => _world.SetAutomatedAim(Vector2.UnitX), minWait: 0.8f, everyFrame: () => _input.InjectKeyDown(Keys.D));
        Shot("trace", () =>
        {
            _world.PlaceAutomatedPlayer(PrologueDirector.SoulTrace + new Vector2(-150f, 60f));
            _world.SetAutomatedSoulSense(true);
            AimAt(PrologueDirector.SoulTrace);
        }, () => _world.PrologueStage >= PrologueStage.TraceWitnessed, 6f, 1.1f);
        Series("trace_witness", 6, 15);
        Do("sense_off", () => _world.SetAutomatedSoulSense(false));
        Shot("first_hollow", ready: () => _world.AutomatedEnemies.Any(enemy => enemy.IsAlive), timeout: 12f, minWait: 0.1f);
        Series("hollow_spawn", 8, 6);
    }

    private void BuildSearch()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.PrologueSearch, 1), _viewport));
        Shot("arrive", minWait: 0.9f);
        Overview("overview");
        Shot("hollows", () => _world.PlaceAutomatedPlayer(new Vector2(540f, 600f)),
            () => _world.AutomatedEnemies.Count(enemy => enemy.IsAlive) >= 2, 6f, 1.2f);
        Do("clear", () => _world.DefeatAutomatedEnemies(), 2.4f);
        Shot("burning_lesson", () => _world.PlaceAutomatedPlayer(new Vector2(960f, 600f)),
            () => _world.AutomatedEnemies.OfType<Burning>().Any(enemy => enemy.IsAlive), 6f, 1.2f);
        Do("clear_burning", () => _world.DefeatAutomatedEnemies(), 2.4f);
        Shot("leave", minWait: 0.3f);
        // A rare state: the flame gutters in the prologue (its own overlay and restart line).
        Do("fall", () => _world.RequestAudioTestFatalDamage());
        Series("dying", 6, 20);
    }

    private void BuildEscape()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.PrologueDevourer, 1), _viewport));
        Shot("arrive", minWait: 1.4f);
        Overview("overview");
        Series("pressure", 8, 12, () =>
        {
            if (Nearest<Devourer>() is { } devourer)
            {
                AimAt(devourer.Position);
            }
        });
    }

    private void BuildTransit()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.PrologueTransit, 1), _viewport));
        Shot("board", minWait: 1f);
        Overview("overview");
        Shot("first_wave", ready: () => _world.AutomatedEnemies.Any(enemy => enemy.IsAlive), timeout: 8f, minWait: 0.6f);
        Series("ride", 6, 20);
    }

    private void BuildThreshold()
    {
        Do("enter", () => _world.EnterAutomatedPrologueStage(PrologueStage.Arrival, _viewport));
        Shot("arrival", minWait: 1.2f);
        Overview("overview");
        Shot("walk_north", () => _world.SetAutomatedAim(-Vector2.UnitY), minWait: 1.6f, everyFrame: () => _input.InjectKeyDown(Keys.W));
        Wait("complete", () => _world.PrologueStage == PrologueStage.Complete, 10f, everyFrame: () => _input.InjectKeyDown(Keys.W));
        Series("completion", 8, 20);
    }

    private void BuildHub()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.Hub, 1), _viewport));
        Shot("arrive", minWait: 1f);
        Overview("overview");
        Shot("walk", () => _world.SetAutomatedAim(Vector2.UnitX), minWait: 0.9f, everyFrame: () => _input.InjectKeyDown(Keys.D));
        Shot("soul_sense", () => _world.SetAutomatedSoulSense(true), minWait: 1f);
        Do("sense_off", () => _world.SetAutomatedSoulSense(false));
        Shot("door_i", () => _world.PlaceAutomatedPlayerAtDoor(0), minWait: 0.8f);
        Shot("door_sealed", () => _world.PlaceAutomatedPlayerAtDoor(1), minWait: 0.8f);
        Do("door_enter", () => _world.RequestAutomatedDoorEntry());
        Series("door_transition", 8, 10);
    }

    private void EnterArena(int wave) =>
        Do($"enter_wave{wave}", () =>
        {
            _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.Arena, wave), _viewport);
            _world.PlaceAutomatedPlayer(ArenaCentre);
            _world.SetAutomatedAim(Vector2.UnitY);
        });

    private void BuildArenaWave1()
    {
        EnterArena(1);
        Series("intro", 6, 15);
        Shot("combat", () => _world.PlaceAutomatedPlayer(ArenaCentre),
            () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.Any(enemy => enemy.IsAlive), 20f, 0.5f);
        Shot("hollow_telegraph", ready: () => _world.AutomatedEnemies.OfType<Hollow>().Any(hollow => hollow.State == HollowState.Telegraph), timeout: 12f,
            everyFrame: () => { if (Nearest<Hollow>() is { } hollow) AimAt(hollow.Position); });
        Series("hollow_swipe", 12, 2);
        Shot("hit_hollow", () =>
        {
            if (Nearest<Hollow>() is { } hollow)
            {
                _world.PlaceAutomatedPlayer(hollow.Position + new Vector2(-70f, 10f));
            }
        }, () => _world.AutomatedPlayer.Scythe.HasStruck, 3f, everyFrame: () =>
        {
            if (Nearest<Hollow>() is { } hollow)
            {
                AimAt(hollow.Position);
                if (_world.AutomatedPlayer.Scythe.ActiveStep == 0)
                {
                    _input.InjectMousePresses(left: true, right: false);
                }
            }
        });
        Series("hollow_hit_reaction", 10, 2);
        Do("defeat", () => _world.DefeatAutomatedEnemies());
        Series("hollow_death", 12, 3);
        Series("soul_release", 10, 6);
    }

    private void BuildPlayerMotion()
    {
        EnterArena(1);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 20f);
        Do("clear", () => _world.DefeatAutomatedEnemies(), 2.5f);
        Do("place_run", () => { _world.PlaceAutomatedPlayer(ArenaCentre - new Vector2(320f, 0f)); _world.SetAutomatedAim(Vector2.UnitX); }, 0.3f);
        Do("listen_steps", () => _world.AutomatedAudio.CuePlayed += LogStep);
        Series("run", 16, 2, () => _input.InjectKeyDown(Keys.D));
        Do("stop_listening", () => _world.AutomatedAudio.CuePlayed -= LogStep);
        Do("place_combo", () => { _world.PlaceAutomatedPlayer(ArenaCentre); _world.SetAutomatedAim(new Vector2(1f, 0.35f)); }, 0.6f);
        Series("combo", 48, 1, () => _input.InjectMousePresses(left: true, right: false));
        Do("rest", () => { }, 0.6f);
        Series("cannon_draw_charge", 30, 2, () => _input.InjectMousePresses(left: false, right: true));
        Series("cannon_fire", 30, 1);
        // Charging on the move: aiming right while backing away to the left walks the legs backward.
        Do("place_cannon_walk", () => { _world.PlaceAutomatedPlayer(ArenaCentre + new Vector2(200f, 0f)); _world.SetAutomatedAim(Vector2.UnitX); }, 0.6f);
        Series("cannon_walk_back", 16, 3, () =>
        {
            _input.InjectKeyDown(Keys.A);
            _input.InjectMousePresses(left: false, right: true);
        });
        Do("cannon_walk_release", () => { }, 1.6f);
        Do("place_dash", () => { _world.PlaceAutomatedPlayer(ArenaCentre - new Vector2(200f, 0f)); _world.SetAutomatedAim(Vector2.UnitX); }, 0.6f);
        Series("dash", 16, 1, () =>
        {
            _input.InjectKeyDown(Keys.D);
            if (!_world.AutomatedPlayer.IsDashing)
            {
                _input.InjectKeyPress(Keys.Space);
            }
        });
        Do("resonance", () => _input.InjectKeyPress(Keys.F5), 0.3f);
        Shot("resonance_ready", minWait: 0.3f);
    }

    private void BuildBurning()
    {
        EnterArena(2);
        Shot("combat", () => _world.PlaceAutomatedPlayer(ArenaCentre),
            () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.OfType<Burning>().Any(enemy => enemy.IsAlive), 20f, 0.6f);
        Shot("charge", ready: () => _world.AutomatedEnemies.OfType<Burning>().Any(burning => burning.State == BurningState.Telegraph && burning.IsAlive), timeout: 15f,
            everyFrame: () => { if (Nearest<Burning>() is { } burning) AimAt(burning.Position); });
        Series("telegraph", 8, 3, () => { if (Nearest<Burning>() is { } burning) AimAt(burning.Position); });
        // A cannon shot into a charging Burning sets off its detonation.
        Wait("charging", () => _world.AutomatedEnemies.OfType<Burning>().Any(burning => burning.IsCharging && burning.IsAlive), 10f,
            everyFrame: () => { if (Nearest<Burning>() is { } burning) AimAt(burning.Position); });
        Series("shoot", 4, 1, () =>
        {
            if (Nearest<Burning>() is { } burning) AimAt(burning.Position);
            _input.InjectMousePresses(left: false, right: true);
        });
        Series("detonation", 24, 2);
        Overview("overview");
    }

    private void BuildDevourer()
    {
        EnterArena(3);
        Shot("combat", () => _world.PlaceAutomatedPlayer(ArenaCentre),
            () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.OfType<Devourer>().Any(enemy => enemy.IsAlive), 20f, 0.8f);
        Series("approach", 12, 10, () => { if (Nearest<Devourer>() is { } devourer) AimAt(devourer.Position); });
        Shot("attack", ready: () => _world.AutomatedEnemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.SlamTelegraph && devourer.IsAlive), timeout: 15f,
            everyFrame: () => { if (Nearest<Devourer>() is { } devourer) AimAt(devourer.Position); });
        Series("attack_motion", 24, 3);
        Overview("overview");
        Do("defeat", () => _world.DefeatAutomatedEnemies());
        Series("defeat", 10, 4);
    }

    private void BuildChest()
    {
        EnterArena(3);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 20f);
        Do("clear", () => _world.DefeatAutomatedEnemies(), 0.2f);
        Wait("intermission", () => _world.LoopState == ArenaLoopState.Intermission, 15f);
        Shot("wave_clear", minWait: 0.4f);
        Shot("chest", () => _world.PlaceAutomatedPlayerAtNewestChest(), () => _world.ChestCount > 0, 6f, 1.2f);
        Do("open", () => _input.InjectKeyPress(Keys.E), 0.05f);
        Series("open", 10, 4);
        Shot("wave_trigger", () => _world.PlaceAutomatedPlayerAtWaveTrigger(), minWait: 0.8f);
        Do("next_wave", () => _world.RequestAutomatedNextWave());
        Series("wave_banner", 6, 8);
    }

    private void BuildDeath()
    {
        EnterArena(1);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 20f);
        Do("place", () => _world.PlaceAutomatedPlayer(ArenaCentre), 0.3f);
        Do("damage", () => _world.RequestAudioTestFatalDamage());
        Series("dying", 16, 4);
        Shot("dead", minWait: 1.5f);
    }

    private void BuildMenus()
    {
        EnterArena(1);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 20f);
        Do("pause", () => _input.InjectKeyPress(Keys.Escape), 0.4f);
        Shot("pause");
        Do("pause_down", () => _input.InjectKeyPress(Keys.Down), 0.15f);
        Do("pause_confirm", () => _input.InjectKeyPress(Keys.Enter), 0.3f);
        Shot("pause_settings");
        Do("graphics", () => _input.InjectKeyPress(Keys.Down), 0.15f);
        Do("graphics_open", () => _input.InjectKeyPress(Keys.Enter), 0.3f);
        Shot("settings_graphics");
        Do("graphics_back", () => _input.InjectKeyPress(Keys.Escape), 0.2f);
        // Going back puts the selection on the first entry again: two down to AUDIO.
        Do("audio_down1", () => _input.InjectKeyPress(Keys.Down), 0.15f);
        Do("audio", () => _input.InjectKeyPress(Keys.Down), 0.15f);
        Do("audio_open", () => _input.InjectKeyPress(Keys.Enter), 0.3f);
        Shot("settings_audio");
        Do("audio_back", () => _input.InjectKeyPress(Keys.Escape), 0.2f);
        Do("pause_back", () => _input.InjectKeyPress(Keys.Escape), 0.2f);
        Do("pause_close", () => _input.InjectKeyPress(Keys.Escape), 0.4f);
        Do("character", () => _input.InjectKeyPress(Keys.Tab), 0.4f);
        for (int tab = 0; tab < 4; tab++)
        {
            Shot($"character_tab{tab}");
            Do($"character_next{tab}", () => _input.InjectKeyPress(Keys.Right), 0.3f);
        }
        Do("character_close", () => _input.InjectKeyPress(Keys.Tab), 0.3f);
        Shot("hud_combat", minWait: 0.2f);
    }

    private void BuildComplete()
    {
        EnterArena(GameBalance.ArenaWaveCount);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 25f);
        Do("clear", () => _world.DefeatAutomatedEnemies(), 0.2f);
        Wait("complete", () => _world.LoopState == ArenaLoopState.Complete, 20f);
        Series("ending", 16, 27);
        Do("ending_audio", () => Console.WriteLine($"TOUR_AUDIO station=arena_complete {_world.AutomatedAudio.DescribeEnding()}"));
    }

    private void BuildSandbox()
    {
        Do("enter", () => _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.Sandbox, 1), _viewport));
        Shot("arrive", minWait: 1f);
        Do("spawn_dummy", () => _world.SpawnAutomatedSandboxEnemy(SandboxEnemyKind.TrainingDummy), 0.6f);
        Do("approach", () =>
        {
            if (_world.AutomatedEnemies.OfType<TrainingDummy>().FirstOrDefault() is { } dummy)
            {
                _world.PlaceAutomatedPlayer(dummy.Position + new Vector2(-70f, 10f));
                AimAt(dummy.Position);
            }
        }, 0.3f);
        Series("dummy_hits", 12, 4, () =>
        {
            if (_world.AutomatedEnemies.OfType<TrainingDummy>().FirstOrDefault() is { } dummy) AimAt(dummy.Position);
            _input.InjectMousePresses(left: true, right: false);
        });
        // The Rückstoßsprung leaps backward on the ability's own timer (0.22 s).
        Do("retreat", () => { _world.SetAutomatedAim(Vector2.UnitX); _world.ShowAutomatedAbility(RunAbility.Retreat); });
        Series("retreat_leap", 8, 2);
        Do("dev_menu", () => _input.InjectKeyPress(Keys.F), 0.4f);
        Shot("dev_menu");
        Do("dev_menu_close", () => _input.InjectKeyPress(Keys.F), 0.3f);
    }
}
