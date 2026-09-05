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
            ["final-release"] = 2100
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
        _semanticSeverance = _scenario is "severance-window" or "severance-cut" && defaultTiming;
        _semanticGoldenBeat = _scenario == "golden-encounter" && defaultTiming;
        _semanticFinalRelease = _scenario == "final-release" && defaultTiming;
        _captureTicks = options.CaptureTicks.Length > 0 ? options.CaptureTicks :
            [options.CaptureAfterTicks >= 0 ? options.CaptureAfterTicks : CaptureTicks[_scenario]];
        _captureTick = _captureTicks.Last();
    }

    public static bool IsKnownScenario(string scenario) => CaptureTicks.ContainsKey(scenario);

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
            // Cut fixture: eight ticks into the committed swing.
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
