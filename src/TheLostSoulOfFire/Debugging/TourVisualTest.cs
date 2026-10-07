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
        _world.AutomatedAudio.CuePlayed += CountVoice;
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
        Station("arena_swings", BuildSwingDirections);
        Station("arena_burning", BuildBurning);
        Station("arena_devourer", BuildDevourer);
        Station("arena_devourer_end", BuildDevourerEnd);
        Station("arena_devour", BuildDevour);
        Station("arena_spawn", BuildSpawn);
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

    private static readonly HashSet<AudioCue> VoiceCues =
    [
        AudioCue.HollowCall, AudioCue.HollowGrasp, AudioCue.BurningCackle, AudioCue.BurningShriek,
        AudioCue.DevourerGrowl, AudioCue.DevourerHunger, AudioCue.FoundryBell, AudioCue.ShoreHorn, AudioCue.ShoreBoard
    ];
    private readonly SortedDictionary<string, int> _voices = new();
    private float _clock;
    private float _lastCall = -10f;
    private float _closestCalls = float.MaxValue;

    /// <summary>Counts the enemies' voices per station, and how close two calls between attacks came.</summary>
    private void CountVoice(AudioCue cue)
    {
        if (!VoiceCues.Contains(cue) || _index < 0 || _index >= _steps.Count)
        {
            return;
        }
        string key = $"{_steps[_index].Station}:{cue}";
        _voices[key] = _voices.GetValueOrDefault(key) + 1;
        if (cue is AudioCue.HollowCall or AudioCue.BurningCackle or AudioCue.DevourerGrowl)
        {
            _closestCalls = MathF.Min(_closestCalls, _clock - _lastCall);
            _lastCall = _clock;
        }
    }

    public void Update(float deltaTime)
    {
        _clock += deltaTime;
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
            Console.WriteLine("TOUR_NARRATION " + string.Join(" | ", _world.AutomatedNarration.History));
            Console.WriteLine("TOUR_AUDIO voices=" + string.Join(",", _voices.Select(pair => $"{pair.Key}={pair.Value}")) +
                string.Create(System.Globalization.CultureInfo.InvariantCulture, $" closest_calls_s={_closestCalls:0.00}"));
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
    private bool _fullLogged;

    /// <summary>
    /// Cannon handling cues while the tour runs, with the cannon state at that moment, the bound
    /// soul's throb with the player's health, and each soul release.
    /// </summary>
    private void LogCannon(AudioCue cue)
    {
        if (cue is AudioCue.CannonDraw or AudioCue.CannonStow or AudioCue.CannonCharge or AudioCue.CannonFire or AudioCue.CannonFull
            or AudioCue.CannonStage or AudioCue.CannonBlast)
        {
            Console.WriteLine($"TOUR_CUE frame={_frame} cue={cue} state={_world.AutomatedPlayer.Cannon.State}");
        }
        else if (cue == AudioCue.SoulRelease)
        {
            Console.WriteLine($"TOUR_CUE frame={_frame} cue={cue}");
        }
        else if (cue == AudioCue.SoulThrob)
        {
            Console.WriteLine($"TOUR_CUE frame={_frame} cue={cue} health={_world.AutomatedPlayer.Health}");
        }
    }

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
        Do("flames", () => Console.WriteLine($"TOUR_AUDIO station=prologue_search arrive {_world.AutomatedAudio.DescribePresence()}"));
        Overview("overview");
        Shot("hollows", () => _world.PlaceAutomatedPlayer(new Vector2(540f, 600f)),
            () => _world.AutomatedEnemies.Count(enemy => enemy.IsAlive) >= 2, 6f, 1.2f);
        // The combat score takes over in the prologue's fights too, capped below the frenzy.
        Do("score_fight", () => Console.WriteLine($"TOUR_AUDIO station=prologue_search fight {_world.AutomatedAudio.DescribePresence()}"), 1.5f);
        Do("clear", () => _world.DefeatAutomatedEnemies(), 2.4f);
        Do("score_after", () => Console.WriteLine($"TOUR_AUDIO station=prologue_search after {_world.AutomatedAudio.DescribePresence()}"), 3.5f);
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
        Do("flames_arrive", () => Console.WriteLine($"TOUR_AUDIO station=hub arrive {_world.AutomatedAudio.DescribePresence()}"));
        Shot("walk", () => _world.SetAutomatedAim(Vector2.UnitX), minWait: 0.9f, everyFrame: () => _input.InjectKeyDown(Keys.D));
        // The Warden flames are heard where they burn: walking right, the right brazier comes forward.
        Do("flames_walk", () => Console.WriteLine($"TOUR_AUDIO station=hub walk {_world.AutomatedAudio.DescribePresence()}"));
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
        Do("score_intro", () => Console.WriteLine($"TOUR_AUDIO station=arena_wave1 intro {_world.AutomatedAudio.DescribePresence()}"));
        Shot("combat", () => _world.PlaceAutomatedPlayer(ArenaCentre),
            () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.Any(enemy => enemy.IsAlive), 20f, 0.5f);
        // The combat score has come in: the pulse at least, the drive with a few enemies.
        Do("score_combat", () => Console.WriteLine($"TOUR_AUDIO station=arena_wave1 combat {_world.AutomatedAudio.DescribePresence()}"), 2.5f);
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
        // A real full cannon shot at a Hollow: the bolt, the matter it tears off, the scorch.
        Do("place_cannon", () =>
        {
            if (Nearest<Hollow>() is { } hollow)
            {
                _world.PlaceAutomatedPlayer(hollow.Position + new Vector2(-260f, 0f));
                AimAt(hollow.Position);
            }
        }, 0.05f);
        Series("cannon_hold", 22, 4, () =>
        {
            _input.InjectMousePresses(left: false, right: true);
            if (Nearest<Hollow>() is { } hollow) AimAt(hollow.Position);
        });
        Series("cannon_hit", 16, 2, () => { if (Nearest<Hollow>() is { } hollow) AimAt(hollow.Position); });
        // A full cannon throws the Hollows back (1.15 s).
        Do("stagger", () => _world.StaggerAutomatedEnemies<Hollow>());
        Series("hollow_stagger", 10, 7);
        Do("listen_release", () => _world.AutomatedAudio.CuePlayed += LogCannon);
        Do("defeat", () => _world.DefeatAutomatedEnemies());
        Series("hollow_death", 12, 3);
        Series("soul_release", 14, 9);
        Do("stop_listening_release", () => _world.AutomatedAudio.CuePlayed -= LogCannon);
    }

    /// <summary>
    /// The full combo aimed in each of the eight directions: one capture at the contact of every
    /// swing, so blade, sweep and figure can be checked from every side.
    /// </summary>
    private void BuildSwingDirections()
    {
        EnterArena(1);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 20f);
        Do("clear", () => _world.DefeatAutomatedEnemies(), 2.5f);
        string[] names = ["e", "se", "s", "sw", "w", "nw", "n", "ne"];
        for (int index = 0; index < names.Length; index++)
        {
            float angle = index * MathHelper.PiOver4;
            Vector2 aim = new(MathF.Cos(angle), MathF.Sin(angle));
            Do($"place_{names[index]}", () => { _world.PlaceAutomatedPlayer(ArenaCentre); _world.SetAutomatedAim(aim); }, 0.7f);
            // Contacts at 0.062 s, 0.205 + 0.085 s and 0.46 + 0.155 s into the held combo.
            Wait($"swing_{names[index]}_1", () => _world.AutomatedPlayer.Scythe.ActiveStep == 1 && _world.AutomatedPlayer.Scythe.NormalizedProgress >= 0.32f, 2f,
                everyFrame: () => _input.InjectMousePresses(left: true, right: false));
            Shot($"{names[index]}_1", everyFrame: () => _input.InjectMousePresses(left: true, right: false));
            Wait($"swing_{names[index]}_2", () => _world.AutomatedPlayer.Scythe.ActiveStep == 2 && _world.AutomatedPlayer.Scythe.NormalizedProgress >= 0.36f, 2f,
                everyFrame: () => _input.InjectMousePresses(left: true, right: false));
            Shot($"{names[index]}_2", everyFrame: () => _input.InjectMousePresses(left: true, right: false));
            Wait($"swing_{names[index]}_3", () => _world.AutomatedPlayer.Scythe.ActiveStep == 3 && _world.AutomatedPlayer.Scythe.NormalizedProgress >= 0.42f, 2f,
                everyFrame: () => _input.InjectMousePresses(left: true, right: false));
            Shot($"{names[index]}_3");
            Do($"rest_{names[index]}", () => { }, 0.9f);
        }

        // The mouse circles while the combo is held: each swing keeps the aim it started with,
        // the next one turns to the new aim, and no fading flame swings round with it.
        float sweep = 0f;
        Do("place_sweep", () => { _world.PlaceAutomatedPlayer(ArenaCentre); _world.SetAutomatedAim(Vector2.UnitX); }, 0.7f);
        Series("sweep", 30, every: 2, everyFrame: () =>
        {
            sweep += 0.06f;
            _world.SetAutomatedAim(new Vector2(MathF.Cos(sweep), MathF.Sin(sweep)));
            _input.InjectMousePresses(left: true, right: false);
        });
        Do("rest_sweep", () => _world.SetAutomatedAim(Vector2.UnitX), 0.9f);
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
        Do("listen_cannon", () => _world.AutomatedAudio.CuePlayed += LogCannon);
        Series("cannon_draw_charge", 30, 2, () => _input.InjectMousePresses(left: false, right: true));
        // Held past full charge: the chamber strains and the cannon trembles (aim_full), then the full
        // shot. A step without the button would release it and fire, so the level is logged in the hold.
        Series("cannon_full_hold", 12, 4, () =>
        {
            _input.InjectMousePresses(left: false, right: true);
            if (_world.AutomatedPlayer.Cannon.IsFullCharge && !_fullLogged)
            {
                _fullLogged = true;
                Console.WriteLine($"TOUR_AUDIO station=arena_player full {_world.AutomatedAudio.DescribePresence()}");
            }
        });
        Do("hum_audio", () => Console.WriteLine($"TOUR_AUDIO station=arena_player charging {_world.AutomatedAudio.DescribePresence()}"), 0.0f);
        Series("cannon_fire", 30, 1);
        Do("stop_listening_cannon", () => _world.AutomatedAudio.CuePlayed -= LogCannon);
        // Charging on the move: aiming right while backing away to the left walks the legs backward.
        // The flame running from the core into the chamber, seen from the front and from behind.
        Do("place_cannon_south", () => { _world.PlaceAutomatedPlayer(ArenaCentre); _world.SetAutomatedAim(Vector2.UnitY); }, 0.6f);
        Series("cannon_charge_south", 5, 9, () => _input.InjectMousePresses(left: false, right: true));
        Do("release_south", () => { }, 1.4f);
        Do("place_cannon_north", () => { _world.PlaceAutomatedPlayer(ArenaCentre); _world.SetAutomatedAim(-Vector2.UnitY); }, 0.6f);
        Series("cannon_charge_north", 5, 9, () => _input.InjectMousePresses(left: false, right: true));
        Do("release_north", () => { }, 1.4f);
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
        Do("resonance_activate", () => _input.InjectKeyPress(Keys.R), 1.4f);
        Shot("resonance_active");
        Do("resonance_audio", () => Console.WriteLine($"TOUR_AUDIO station=arena_player {_world.AutomatedAudio.DescribePresence()}"));
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
        // Each kind twitches as it calls: the Hollow jerks, the Burning shakes with laughter, the Devourer swells.
        Do("twitch", () => _world.TwitchAutomatedEnemies());
        Series("twitch", 20, 3);
        Do("presence", () => Console.WriteLine($"TOUR_AUDIO station=arena_devourer presence {_world.AutomatedAudio.DescribePresence()}"));
        Shot("attack", ready: () => _world.AutomatedEnemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.SlamTelegraph && devourer.IsAlive), timeout: 15f,
            everyFrame: () => { if (Nearest<Devourer>() is { } devourer) AimAt(devourer.Position); });
        Series("attack_motion", 24, 3);
        Overview("overview");
        Do("defeat", () => _world.DefeatAutomatedEnemies());
        Series("defeat", 10, 4);
    }

    /// <summary>A fresh fight: a full cannon throws the Devourer back (1.4 s), then it dies.</summary>
    private void BuildDevourerEnd()
    {
        EnterArena(3);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.OfType<Devourer>().Any(enemy => enemy.IsAlive), 20f);
        Do("alone", () => _world.DefeatAutomatedEnemiesExcept<Devourer>(), 1.6f);
        Do("place", () =>
        {
            if (Nearest<Devourer>() is { } devourer)
            {
                _world.PlaceAutomatedPlayer(devourer.Position + new Vector2(-230f, 60f));
                AimAt(devourer.Position);
            }
        }, 0.3f);
        Do("stagger", () => _world.StaggerAutomatedEnemies<Devourer>());
        Series("stagger", 12, 7, () => { if (Nearest<Devourer>() is { } devourer) AimAt(devourer.Position); });
        Do("defeat", () => _world.DefeatAutomatedEnemies());
        Series("death", 10, 3);
    }

    /// <summary>A Hollow falls next to the Devourer; the Devourer goes for its soul and swallows it.</summary>
    private void BuildDevour()
    {
        EnterArena(3);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat && _world.AutomatedEnemies.OfType<Devourer>().Any(enemy => enemy.IsAlive), 20f);
        // The others fall first and their souls go their way before the staged one is freed.
        Do("alone", () => _world.DefeatAutomatedEnemiesExcept<Devourer>(), 2.6f);
        Do("place", () =>
        {
            if (Nearest<Devourer>() is { } devourer)
            {
                _world.PlaceAutomatedPlayer(devourer.Position + new Vector2(-330f, 40f));
                AimAt(devourer.Position);
                _world.SpawnAutomatedHollow(devourer.Position + new Vector2(150f, 30f));
            }
        }, 0.5f);
        Do("free", () => _world.DefeatAutomatedEnemiesExcept<Devourer>());
        Wait("hungry", () => _world.AutomatedEnemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.ApproachSoul), 3f);
        Series("devour", 20, 5);
        // Holding a soul, seen with and without Soul Sense: the maw's glow and the prison in its chest.
        Shot("holding", minWait: 0.3f);
        Shot("holding_sense", () => _world.SetAutomatedSoulSense(true), minWait: 1f);
        Do("sense_off", () => _world.SetAutomatedSoulSense(false));
    }

    /// <summary>Enemies called in during a wave: light gathers where they are about to appear.</summary>
    private void BuildSpawn()
    {
        // Wave 5 calls its second push in once the first falls.
        EnterArena(5);
        Wait("combat", () => _world.LoopState == ArenaLoopState.Combat, 20f);
        Wait("spawning", () => _world.AutomatedPendingSpawn is not null, 20f,
            everyFrame: () => { if (_world.AutomatedPendingSpawn is null) _world.DefeatAutomatedLivingEnemies(); });
        Do("look", () => { if (_world.AutomatedPendingSpawn is { } spawn) _world.PlaceAutomatedPlayer(spawn + new Vector2(-160f, 40f)); });
        Series("gather", 8, 6);
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
        // Low first: the bound soul throbs, the edges close in and the bar beats along.
        Do("listen_throb", () => _world.AutomatedAudio.CuePlayed += LogCannon);
        Do("wound", () => _world.RequestAutomatedDamage(_world.AutomatedPlayer.Health - 15), 0.2f);
        Series("low", 18, 8);
        Do("stop_listening_throb", () => _world.AutomatedAudio.CuePlayed -= LogCannon);
        Do("score_low", () => Console.WriteLine($"TOUR_AUDIO station=arena_death low {_world.AutomatedAudio.DescribePresence()}"));
        Do("damage", () => _world.RequestAudioTestFatalDamage());
        Series("dying", 16, 4);
        Shot("dead", minWait: 1.5f);
        Do("score_dead", () => Console.WriteLine($"TOUR_AUDIO station=arena_death dead {_world.AutomatedAudio.DescribePresence()}"));
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
