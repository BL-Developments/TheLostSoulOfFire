using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;

namespace TheLostSoulOfFire.Debugging;

/// <summary>
/// Drives real input at fixed update ticks. Single-subject fixtures arrange real
/// entities through a narrow debug seam; combat values and state machines stay
/// untouched. Sidecars report observed phases, not just scenario labels.
/// </summary>
public sealed class VisualScenarioRunner
{
    private static readonly IReadOnlyDictionary<string, int> CaptureTicks =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["title-arrival"] = 180,
            ["arena-idle"] = 112,
            ["dash"] = 130,
            ["scythe-combo"] = 164,
            ["hollow-swipe"] = 164,
            ["burning-charge"] = 130,
            ["devourer-slam"] = 205,
            ["cannon-sense"] = 208,
            ["resonance-busy"] = 156,
            ["soul-release"] = 206,
            ["death-retry"] = 220,
            ["ending"] = 2100,
            ["golden-encounter"] = 1400,
            ["severance-window"] = 320,
            ["severance-cut"] = 340,
            ["final-release"] = 2100,

            // Session 2 — local co-op. Each fixture drives both brothers through
            // the real command layer; nothing here bypasses combat state.
            ["coop-idle"] = 150,
            ["coop-split-targets"] = 210,
            ["coop-severance"] = 360,
            ["coop-down"] = 200,
            ["coop-stabilize"] = 320,
            ["coop-separation"] = 300,
            ["coop-soul-release"] = 300,
            ["coop-resonance"] = 190,
            ["coop-golden-encounter"] = 1400,
            ["coop-reduced"] = 210
        };

    private readonly string _scenario;
    private readonly int _captureTick;
    private readonly int[] _captureTicks;
    private readonly bool _forceSense;
    private readonly bool _forceResonance;
    private readonly bool _semanticEnding;
    private readonly bool _semanticSeverance;
    private readonly bool _semanticGoldenBeat;
    private readonly bool _semanticFinalRelease;
    private int _tick;
    private int _combatTicks;
    private int _completedWave;
    private int _severanceDashTick = -1;
    private int _severanceCutTick = -1;
    private int _beatReachedTick = -1;
    private ScriptedInput _second;
    private bool _coopArranged;

    public static string KnownScenarioList => string.Join(", ", CaptureTicks.Keys);
    public bool CaptureRequested { get; private set; }
    public bool TimedOut => _tick > _captureTick + 180;
    public bool IsSemantic => _semanticEnding || _semanticSeverance || _semanticGoldenBeat || _semanticFinalRelease;
    public int Tick => _tick;
    public string Scenario => _scenario;
    public bool IsLastCapture => IsSemantic || _tick >= _captureTick;
    public string OutputName => _captureTicks.Length > 1 ? $"{_scenario}-{_tick:D4}" : _scenario;

    public VisualScenarioRunner(VisualRunOptions options)
    {
        _scenario = options.VisualScenario;
        _forceSense = options.ForceSoulSense;
        _forceResonance = options.ForceResonance;
        bool defaultTiming = options.CaptureTicks.Length == 0 && options.CaptureAfterTicks < 0;
        _semanticEnding = _scenario == "ending" && defaultTiming;
        _semanticSeverance = _scenario is "severance-window" or "severance-cut" or "coop-severance" && defaultTiming;
        _semanticGoldenBeat = _scenario is "golden-encounter" or "coop-golden-encounter" && defaultTiming;
        _semanticFinalRelease = _scenario == "final-release" && defaultTiming;
        _captureTicks = options.CaptureTicks.Length > 0 ? options.CaptureTicks :
            [options.CaptureAfterTicks >= 0 ? options.CaptureAfterTicks : CaptureTicks[_scenario]];
        _captureTick = _captureTicks.Last();
    }

    public static bool IsKnownScenario(string scenario) => CaptureTicks.ContainsKey(scenario);

    /// <summary>Co-op fixtures start the encounter with the brother already present.</summary>
    public static bool RequiresTwoPlayers(string scenario) =>
        scenario.StartsWith("coop-", StringComparison.OrdinalIgnoreCase);

    public void Update(InputState input, GameWorld world, Viewport viewport)
    {
        _tick++;
        if (!string.Equals(_scenario, "title-arrival", StringComparison.OrdinalIgnoreCase))
        {
            input.InjectMousePosition(new Point(viewport.Width / 2 + 280, viewport.Height / 2));
        }

        if (_tick == 2 && !string.Equals(_scenario, "title-arrival", StringComparison.OrdinalIgnoreCase))
        {
            input.InjectKeyPress(Keys.Space);
        }

        switch (_scenario)
        {
            case "dash":
                if (_tick is >= 116 and <= 124)
                {
                    input.InjectHeldKey(Keys.D);
                }
                if (_tick == 120)
                {
                    input.InjectKeyPress(Keys.Space);
                }
                break;

            case "scythe-combo":
                if (_tick == 139) world.ArrangeVisualSubject(_scenario);
                if (_tick is 142 or 150 or 171)
                {
                    input.InjectLeftMouseDown();
                }
                break;

            case "hollow-swipe":
                if (_tick == 100) world.ArrangeVisualSubject(_scenario);
                break;

            case "burning-charge":
                if (_tick == 100) world.ArrangeVisualSubject(_scenario);
                break;

            case "devourer-slam":
                if (_tick == 100) world.ArrangeVisualSubject(_scenario);
                break;

            case "cannon-sense":
                SpawnAt(input, Keys.F2, 100);
                if (_tick == 104)
                {
                    input.InjectKeyPress(Keys.F7);
                }
                if (_tick is >= 120 and <= 200)
                {
                    input.InjectRightMouseDown();
                }
                break;

            case "resonance-busy":
                SpawnAt(input, Keys.F2, 100);
                SpawnAt(input, Keys.F3, 100);
                SpawnAt(input, Keys.F4, 100);
                if (_tick == 112)
                {
                    input.InjectKeyPress(Keys.F5);
                }
                if (_tick == 114)
                {
                    input.InjectKeyPress(Keys.R);
                }
                break;

            case "soul-release":
                SpawnAt(input, Keys.F2, 100);
                if (_tick == 144)
                {
                    input.InjectKeyPress(Keys.F6);
                }
                break;

            case "death-retry":
                if (_tick == 112)
                {
                    world.RequestAudioTestFatalDamage();
                }
                break;

            case "severance-window":
            case "severance-cut":
                AdvanceSeverance(input, world);
                break;

            case "golden-encounter":
                AdvanceToBeat(input, world, 3);
                break;

            case "ending":
            case "final-release":
                AdvanceEnding(input, world);
                break;

            default:
                if (_scenario.StartsWith("coop-", StringComparison.Ordinal))
                {
                    AdvanceCoop(input, world);
                }
                break;
        }

        if (_forceSense && _tick == 104 && _scenario != "cannon-sense")
            input.InjectKeyPress(Keys.F7);
        if (_forceResonance && _scenario != "resonance-busy")
        {
            if (_tick == 112) input.InjectKeyPress(Keys.F5);
            if (_tick == 114) input.InjectKeyPress(Keys.R);
        }

        if (ShouldCapture(world))
        {
            CaptureRequested = true;
        }
    }

    private bool ShouldCapture(GameWorld world)
    {
        if (_semanticEnding)
        {
            return world.LoopState == ArenaLoopState.Complete && world.PresentationStateTime >= 6f;
        }

        if (_semanticFinalRelease)
        {
            // The quiet beat: Souls have gone and the Life Flame is rising.
            return world.LoopState == ArenaLoopState.Complete && world.PresentationStateTime >= 1.9f;
        }

        if (_semanticSeverance)
        {
            // Window fixture: the frame the read is confirmed on.
            // Cut and co-op fixtures: eight ticks into the committed swing.
            return _scenario == "severance-window"
                ? _severanceDashTick >= 0 && _tick == _severanceDashTick + 3
                : _severanceCutTick >= 0 && _tick == _severanceCutTick + 8;
        }

        if (_semanticGoldenBeat)
        {
            return _beatReachedTick >= 0 && _tick == _beatReachedTick + 260;
        }

        return _captureTicks.Contains(_tick);
    }

    public void MarkCaptureHandled() => CaptureRequested = false;

    private void SpawnAt(InputState input, Keys key, int tick)
    {
        if (_tick == tick)
        {
            input.InjectKeyPress(key);
        }
    }

    /// <summary>
    /// Reacts to the real Severance opportunity instead of a hard-coded tick: the
    /// fixture dashes on the frame the enemy's commitment enters the read window,
    /// then swings. If the timing values are re-tuned the evidence still lands.
    /// </summary>
    private void AdvanceSeverance(InputState input, GameWorld world)
    {
        if (_tick == 100)
        {
            world.ArrangeVisualSubject(_scenario);
            return;
        }

        if (_tick < 104)
        {
            return;
        }

        if (_severanceDashTick < 0)
        {
            if (world.SeveranceOpportunityReady)
            {
                _severanceDashTick = _tick;
                input.InjectKeyPress(Keys.Space);
            }
            return;
        }

        if (_scenario == "severance-cut" && _severanceCutTick < 0 && _tick >= _severanceDashTick + 14 && world.SeveranceWindowOpen)
        {
            _severanceCutTick = _tick;
            input.InjectLeftMouseDown();
        }
    }

    private void AdvanceToBeat(InputState input, GameWorld world, int targetBeat)
    {
        if (world.WaveNumber >= targetBeat)
        {
            if (_beatReachedTick < 0 && world.LoopState == ArenaLoopState.Combat)
            {
                _beatReachedTick = _tick;
            }
            return;
        }

        if (world.LoopState != ArenaLoopState.Combat)
        {
            _combatTicks = 0;
            return;
        }

        _combatTicks++;
        if (_combatTicks < 22)
        {
            return;
        }

        _combatTicks = 0;
        input.InjectKeyPress(Keys.F6);
    }

    /// <summary>
    /// Drives the brother through the same <see cref="PlayerCommand"/> every real
    /// device produces, so a co-op capture exercises the shipped path rather than
    /// a debug shortcut.
    /// </summary>
    private void AdvanceCoop(InputState input, GameWorld world)
    {
        _second ??= world.JoinScriptedSecondWarden();
        if (_second is null)
        {
            return;
        }

        if (!_coopArranged && _tick >= 60 && world.LoopState == ArenaLoopState.Combat)
        {
            _coopArranged = true;
            ArrangeCoop(world);
        }

        switch (_scenario)
        {
            case "coop-idle":
            case "coop-reduced":
                _second.Set(new PlayerCommand(Vector2.Zero, world.SecondWardenPosition + new Vector2(-220f, 0f),
                    false, false, false, false, false, false, false));
                break;

            case "coop-split-targets":
                // Both brothers swing at their own enemy on the same frame.
                _second.Set(new PlayerCommand(Vector2.Zero, world.SecondWardenPosition + new Vector2(-200f, 0f),
                    false, _tick is 150 or 168, false, false, false, false, false));
                if (_tick is 150 or 168)
                {
                    input.InjectLeftMouseDown();
                }
                break;

            case "coop-severance":
                AdvanceCoopSeverance(input, world);
                break;

            case "coop-down":
                if (_tick == 120) world.ForceWardenDown(1);
                _second.Set(PlayerCommand.Idle(world.SecondWardenPosition + new Vector2(-200f, 0f)));
                break;

            case "coop-stabilize":
                if (_tick == 110) world.ForceWardenDown(1);
                _second.Set(PlayerCommand.Idle(world.SecondWardenPosition + new Vector2(-200f, 0f)));
                if (_tick is > 112 and < 150)
                {
                    // Player 1 walks to his brother using real movement input.
                    input.InjectHeldKey(Keys.A);
                }
                if (_tick >= 150)
                {
                    // ...and then holds. The hold is the shipped stabilise input.
                    input.InjectHeldKey(Keys.E);
                }
                break;

            case "coop-separation":
                _second.Set(PlayerCommand.Idle(world.SecondWardenPosition + new Vector2(-200f, 0f)));
                break;

            case "coop-soul-release":
                _second.Set(PlayerCommand.Idle(world.SecondWardenPosition + new Vector2(-200f, 0f)));
                if (_tick == 150)
                {
                    input.InjectKeyPress(Keys.F6);
                }
                break;

            case "coop-resonance":
                _second.Set(new PlayerCommand(Vector2.Zero, world.SecondWardenPosition + new Vector2(-200f, 0f),
                    false, false, false, false, false, _tick == 150, false));
                if (_tick == 148)
                {
                    input.InjectKeyPress(Keys.F5);
                }
                break;

            case "coop-golden-encounter":
                _second.Set(PlayerCommand.Idle(world.SecondWardenPosition + new Vector2(-200f, 0f)));
                AdvanceToBeat(input, world, 3);
                break;
        }
    }

    private void ArrangeCoop(GameWorld world)
    {
        switch (_scenario)
        {
            case "coop-idle":
            case "coop-reduced":
            case "coop-down":
            case "coop-stabilize":
            case "coop-resonance":
                world.ArrangeCoopSubject(_scenario);
                break;

            case "coop-split-targets":
            case "coop-severance":
            case "coop-soul-release":
                world.ArrangeCoopSubject(_scenario);
                break;

            case "coop-separation":
                world.ArrangeCoopSubject(_scenario);
                break;
        }
    }

    private void AdvanceCoopSeverance(InputState input, GameWorld world)
    {
        _second.Set(PlayerCommand.Idle(world.SecondWardenPosition + new Vector2(-200f, 0f)));
        if (_tick < 130)
        {
            return;
        }

        if (_severanceDashTick < 0)
        {
            if (world.SeveranceOpportunityReady)
            {
                _severanceDashTick = _tick;
                input.InjectKeyPress(Keys.Space);
                // The brother reads the same telegraph on the same frame.
                _second.Set(new PlayerCommand(Vector2.Zero, world.SecondWardenPosition + new Vector2(200f, 0f),
                    true, false, false, false, false, false, false));
            }
            return;
        }

        if (_severanceCutTick < 0 && _tick >= _severanceDashTick + 14 && world.SeveranceWindowOpen)
        {
            _severanceCutTick = _tick;
            input.InjectLeftMouseDown();
        }
    }

    private void AdvanceEnding(InputState input, GameWorld world)
    {
        if (world.LoopState != ArenaLoopState.Combat)
        {
            _combatTicks = 0;
            return;
        }

        // Beats stage arrivals, so the ending fixture clears the floor on a cadence
        // rather than once per beat.
        _combatTicks++;
        if (_combatTicks < 24)
        {
            return;
        }

        _combatTicks = 0;
        _completedWave = world.WaveNumber;
        input.InjectKeyPress(Keys.F6);
    }
}
