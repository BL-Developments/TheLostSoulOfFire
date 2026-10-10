using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Combat;

public readonly record struct ScytheStrike(
    int Step,
    int Damage,
    float Range,
    float ArcRadians,
    float Knockback,
    Vector2 Direction);

public sealed class ScytheCombat
{
    private float _attackElapsed;
    private float _attackDuration;
    private float _strikeTime;
    private float _comboTimer;
    private bool _strikeCreated;
    private bool _strikePending;
    private bool _queuedAttack;
    private int _nextStep = 1;
    private Vector2 _attackDirection = Vector2.UnitX;
    private bool _resonanceActive;
    private PlayerAttributes _attributes = PlayerAttributes.Default;

    public int ActiveStep { get; private set; }
    public bool StartedThisFrame { get; private set; }

    /// <summary>Presentation only: the step of the swing that ended last (0 before any swing).</summary>
    public int LastStep { get; private set; }

    /// <summary>Presentation only: seconds since the last swing ended, while no swing runs.</summary>
    public float SinceSwingEnd { get; private set; }
    public Vector2 AttackDirection => _attackDirection;
    /// <summary>True from the hit moment of the current swing until the next one starts.</summary>
    public bool HasStruck => ActiveStep > 0 && _strikeCreated;
    public float NormalizedProgress => ActiveStep == 0 ? 0f : MathHelper.Clamp(_attackElapsed / _attackDuration, 0f, 1f);
    public string StateLabel => ActiveStep == 0 ? (_comboTimer > 0f ? $"CHAIN {_nextStep}" : "READY") : $"HIT {ActiveStep}";

    public void Reset()
    {
        _attackElapsed = 0f;
        _attackDuration = 0f;
        _comboTimer = 0f;
        _strikeCreated = false;
        _strikePending = false;
        _queuedAttack = false;
        _nextStep = 1;
        ActiveStep = 0;
        LastStep = 0;
        SinceSwingEnd = 0f;
        _ignitionPending = false;
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 facingDirection,
        Vector2 playerPosition,
        ParticleSystem particles,
        bool canStartAttack,
        bool resonanceActive,
        PlayerAttributes attributes)
    {
        StartedThisFrame = false;
        _resonanceActive = resonanceActive;
        _attributes = attributes;

        if (ActiveStep == 0)
        {
            SinceSwingEnd += deltaTime;
            _comboTimer = MathF.Max(0f, _comboTimer - deltaTime);
            if (_comboTimer <= 0f)
            {
                _nextStep = 1;
            }

            if (canStartAttack && (input.WasLeftMousePressed || _queuedAttack))
            {
                _queuedAttack = false;
                StartAttack(facingDirection, playerPosition, particles);
            }

            return;
        }

        if (input.WasLeftMousePressed && _attackElapsed > 0.055f)
        {
            _queuedAttack = true;
        }

        _attackElapsed += deltaTime * attributes.AttackSpeedMultiplier;
        if (_ignitionPending && NormalizedProgress >= StrokeStart(ActiveStep))
        {
            EmitIgnition(playerPosition, particles);
        }
        if (!_strikeCreated && _attackElapsed >= _strikeTime)
        {
            _strikeCreated = true;
            _strikePending = true;
        }

        if (_attackElapsed < _attackDuration)
        {
            return;
        }

        LastStep = ActiveStep;
        SinceSwingEnd = 0f;
        ActiveStep = 0;
        _comboTimer = GameBalance.ComboResetTime;
        if (canStartAttack && _queuedAttack)
        {
            _queuedAttack = false;
            StartAttack(facingDirection, playerPosition, particles);
        }
    }

    public bool TryConsumeStrike(out ScytheStrike strike)
    {
        if (!_strikePending)
        {
            strike = default;
            return false;
        }

        _strikePending = false;
        strike = BuildStrike(ActiveStep, _attackDirection, _resonanceActive, _attributes);
        return true;
    }

    public float GetForwardImpulse()
    {
        float impulse = ActiveStep switch
        {
            1 => 105f,
            2 => 132f,
            3 => 225f,
            _ => 0f
        };
        return impulse * (_resonanceActive ? 1.18f : 1f);
    }

    /// <summary>
    /// The part of the swing's Death Flame that passes behind the rendered figure; drawn before
    /// the figure, so the sweep wraps around the body instead of lying on top of it.
    /// </summary>
    public void DrawBehindFigure(SpriteBatch batch, Vector2 playerPosition, ArtAssets art)
    {
        if (ActiveStep > 0)
        {
            DrawFlameSlash(batch, art, playerPosition, behind: true);
        }
    }

    public void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D? physicalScythe,
        Vector2 playerPosition,
        Vector2 facingDirection,
        bool debugVisible,
        bool figureCarriesScythe = false,
        ArtAssets? art = null)
    {
        if (ActiveStep == 0)
        {
            if (!figureCarriesScythe)
            {
                DrawRestingScythe(batch, pixel, physicalScythe, playerPosition, facingDirection);
            }
            return;
        }

        DrawAttackingScythe(batch, pixel, physicalScythe, playerPosition, debugVisible, figureCarriesScythe, art);
    }

    private void StartAttack(Vector2 facingDirection, Vector2 playerPosition, ParticleSystem particles)
    {
        ActiveStep = _nextStep;
        _nextStep = ActiveStep == 3 ? 1 : ActiveStep + 1;
        _attackDirection = facingDirection.LengthSquared() > 0.001f ? Vector2.Normalize(facingDirection) : Vector2.UnitX;
        _attackElapsed = 0f;
        _strikeCreated = false;
        StartedThisFrame = true;

        (_attackDuration, _strikeTime) = ActiveStep switch
        {
            1 => (0.205f, 0.062f),
            2 => (0.255f, 0.085f),
            _ => (0.42f, 0.155f)
        };

        _ignitionPending = true;
    }

    private bool _ignitionPending;

    /// <summary>Swing progress where the blade's fast stroke begins (after the wind-up).</summary>
    private static float StrokeStart(int step) => step switch { 1 => 0.10f, 2 => 0.12f, _ => 0.235f };

    /// <summary>
    /// The Death Flame core flares as the stroke begins: sparks leave the scythe's collar along
    /// the blade's path, not the aim, so a wind-up behind the body does not spark in front of it.
    /// </summary>
    private void EmitIgnition(Vector2 playerPosition, ParticleSystem particles)
    {
        _ignitionPending = false;
        ScytheBladePaths.Sample core = ScytheBladePaths.At(ActiveStep, NormalizedProgress);
        float angle = MathF.Atan2(_attackDirection.Y, _attackDirection.X) + MathHelper.ToRadians(core.Heading);
        Vector2 at = playerPosition + ScytheBladePaths.Project(angle, core.Distance * ScytheBladePaths.UnitsPerMetre, core.Height)
            + new Vector2(0f, FigureHeights.Air);
        float turn = ActiveStep == 2 ? -MathHelper.PiOver2 : MathHelper.PiOver2;
        Vector2 along = new(MathF.Cos(angle + turn), MathF.Sin(angle + turn));
        Color flame = ActiveStep == 3 ? GameBalance.DeathFlameBright : GameBalance.DeathFlame;
        int ignitionParticles = ActiveStep switch { 1 => 2, 2 => 4, _ => 8 };
        particles.EmitBurst(at, along, ignitionParticles, flame, ActiveStep == 3 ? 115f : 60f, ActiveStep == 3 ? 6f : 3f);
    }

    private readonly List<Vector2> _slashPath = new(40);
    private readonly List<Vector2> _slashTips = new(40);
    private readonly List<float> _slashMask = new(40);

    /// <summary>
    /// The swing as a Death Flame ribbon streaming from the core in the scythe's collar: it
    /// follows the recorded path of the rendered blade (<see cref="ScytheBladePaths"/>) in
    /// heading, height and timing, trailing the blade over the last part of the stroke, and
    /// burns out from the tail as the swing settles. Older parts of the ribbon are flung a
    /// little outward, so the sweep shows the strike's reach. Presentation only; the strike
    /// itself is resolved elsewhere. With <paramref name="behind"/> only the part behind the
    /// figure is drawn, otherwise only the part in front of it.
    /// </summary>
    private bool DrawFlameSlash(SpriteBatch batch, ArtAssets art, Vector2 feet, bool behind)
    {
        if (!art.CanDrawDeathFlame)
        {
            return false;
        }

        float progress = NormalizedProgress;
        float fadeStart = ActiveStep == 3 ? 0.7f : 0.62f;
        float alpha = 1f - MathHelper.Clamp((progress - fadeStart) / (1f - fadeStart), 0f, 1f);
        if (alpha <= 0.01f)
        {
            return true;
        }

        // Trail length in swing progress, where the fast part of the stroke begins, and how far
        // the oldest flame is flung out beyond the blade.
        (float trail, float flung) = ActiveStep switch
        {
            1 => (0.26f, 1.03f),
            2 => (0.28f, 1.08f),
            _ => (0.34f, 1.2f)
        };
        float activeFrom = StrokeStart(ActiveStep);
        float settle = MathHelper.Clamp((progress - 0.55f) / 0.45f, 0f, 1f);
        float head = progress;
        float tail = MathF.Max(activeFrom, head - trail * (1f - settle * 0.85f));
        if (head <= tail + 0.004f)
        {
            return true;
        }

        float aim = MathF.Atan2(_attackDirection.Y, _attackDirection.X);
        float reach = _resonanceActive ? GameBalance.ResonanceScytheRangeMultiplier : 1f;
        _slashPath.Clear();
        _slashTips.Clear();
        _slashMask.Clear();
        bool any = false;
        const int points = 28;
        for (int index = 0; index < points; index++)
        {
            float along = index / (points - 1f);
            float at = MathHelper.Lerp(tail, head, along);
            // The sweep spans the blade from its root at the collar to a little beyond its tip;
            // older parts are flung outward, so the stroke shows the strike's reach.
            ScytheBladePaths.Sample root = ScytheBladePaths.At(ActiveStep, at);
            ScytheBladePaths.Sample tip = ScytheBladePaths.TipAt(ActiveStep, at);
            float fling = MathHelper.Lerp(flung, 1f, along);
            float rootAngle = aim + MathHelper.ToRadians(root.Heading);
            float tipAngle = aim + MathHelper.ToRadians(tip.Heading);
            float rootDistance = root.Distance * ScytheBladePaths.UnitsPerMetre * reach;
            float tipDistance = tip.Distance * ScytheBladePaths.UnitsPerMetre * reach * 1.06f * fling;
            _slashPath.Add(feet + ScytheBladePaths.Project(rootAngle, rootDistance, root.Height));
            _slashTips.Add(feet + ScytheBladePaths.Project(tipAngle, tipDistance, tip.Height));
            // Level depth of the sweep's middle relative to the body: negative is farther from the camera.
            float depth = (MathF.Sin(rootAngle) * rootDistance + MathF.Sin(tipAngle) * tipDistance) * 0.5f;
            float front = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((depth + 10f) / 20f, 0f, 1f));
            float share = behind ? 1f - front : front;
            _slashMask.Add(share);
            any |= share > 0.01f;
        }

        if (!any)
        {
            return true;
        }

        float heat = ActiveStep switch { 1 => 0.85f, 2 => 1.0f, _ => 1.2f };
        if (_resonanceActive)
        {
            heat *= 1.15f;
        }

        // One broad, soft stroke over the surface the blade swept (no ribbon line).
        art.DrawDeathFlameSmear(batch, _slashPath, _slashTips, alpha * heat, _slashMask);
        return true;
    }

    /// <summary>
    /// Light of the swing (presentation only): where the Death Flame core in the collar is while
    /// the stroke is fast, and how bright it burns. It lights the floor and nearby figures; there
    /// is none during the wind-up or after the swing has settled.
    /// </summary>
    public bool TryGetFlameLight(Vector2 feet, out Vector2 position, out float intensity)
    {
        position = default;
        intensity = 0f;
        if (ActiveStep == 0)
        {
            return false;
        }

        float progress = NormalizedProgress;
        float start = StrokeStart(ActiveStep);
        float rise = MathHelper.Clamp((progress - start) / 0.08f, 0f, 1f);
        float fall = 1f - MathHelper.Clamp((progress - 0.5f) / 0.4f, 0f, 1f);
        intensity = rise * fall * ActiveStep switch { 1 => 0.32f, 2 => 0.4f, _ => 0.55f } * (_resonanceActive ? 1.2f : 1f);
        if (intensity <= 0.01f)
        {
            return false;
        }

        ScytheBladePaths.Sample core = ScytheBladePaths.At(ActiveStep, progress);
        float angle = MathF.Atan2(_attackDirection.Y, _attackDirection.X) + MathHelper.ToRadians(core.Heading);
        position = feet + ScytheBladePaths.Project(angle, core.Distance * ScytheBladePaths.UnitsPerMetre, core.Height);
        return true;
    }

    private static ScytheStrike BuildStrike(int step, Vector2 direction, bool resonanceActive, PlayerAttributes attributes)
    {
        ScytheStrike strike = step switch
        {
            1 => new ScytheStrike(1, GameBalance.ScytheDamage1, GameBalance.ScytheRange1, MathHelper.ToRadians(120f), 170f, direction),
            2 => new ScytheStrike(2, GameBalance.ScytheDamage2, GameBalance.ScytheRange2, MathHelper.ToRadians(140f), 220f, direction),
            _ => new ScytheStrike(3, GameBalance.ScytheDamage3, GameBalance.ScytheRange3, MathHelper.ToRadians(198f), 410f, direction)
        };
        strike = strike with { Damage = attributes.ScaleWeaponDamage(strike.Damage) };

        if (!resonanceActive)
        {
            return strike;
        }

        return strike with
        {
            Damage = (int)MathF.Round(strike.Damage * GameBalance.ResonanceScytheDamageMultiplier),
            Range = strike.Range * GameBalance.ResonanceScytheRangeMultiplier,
            Knockback = strike.Knockback * GameBalance.ResonanceScytheKnockbackMultiplier
        };
    }

    private static void DrawRestingScythe(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D? physicalScythe,
        Vector2 playerPosition,
        Vector2 facingDirection)
    {
        Vector2 right = new(-facingDirection.Y, facingDirection.X);
        float rotation = MathF.Atan2(facingDirection.Y, facingDirection.X);
        batch.DrawSpriteOrDummy(pixel, physicalScythe, playerPosition + facingDirection * 10f + right * 3f, rotation, 0.52f);
    }

    private void DrawAttackingScythe(
        SpriteBatch batch,
        Texture2D pixel,
        Texture2D? physicalScythe,
        Vector2 playerPosition,
        bool debugVisible,
        bool figureCarriesScythe,
        ArtAssets? art)
    {
        // A rendered figure swings the scythe itself; the Death Flame trail then follows its
        // blade: a level circle at hand height, seen from the camera's angle.
        Vector2 trailCentre = figureCarriesScythe ? playerPosition - new Vector2(0f, FigureHeights.Hold) : playerPosition;
        float squash = figureCarriesScythe ? FigureHeights.LevelSquash : 1f;
        float aim = MathF.Atan2(_attackDirection.Y, _attackDirection.X);
        float attackProgress = NormalizedProgress;
        float swingProgress = ActiveStep == 3
            ? MathHelper.Clamp((attackProgress - 0.2f) / 0.58f, 0f, 1f)
            : attackProgress;
        float eased = 1f - MathF.Pow(1f - swingProgress, ActiveStep == 3 ? 2.35f : 3f);
        float totalArc = ActiveStep switch
        {
            1 => MathHelper.ToRadians(120f),
            2 => -MathHelper.ToRadians(140f),
            _ => MathHelper.ToRadians(198f)
        };
        float start = aim - totalArc * 0.5f;
        float current = start + totalArc * eased;
        float radius = ActiveStep switch { 1 => 88f, 2 => 99f, _ => 119f };
        float thickness = ActiveStep switch { 1 => 3f, 2 => 7f, _ => 16f };
        if (_resonanceActive)
        {
            radius *= GameBalance.ResonanceScytheRangeMultiplier;
            thickness *= 1.22f;
        }
        Color trail = ActiveStep switch
        {
            1 => GameBalance.DeathFlame * 0.58f,
            2 => GameBalance.DeathFlameBright * 0.78f,
            _ => GameBalance.DeathFlameBright * 0.94f
        };

        float fadeStart = ActiveStep == 3 ? 0.7f : 0.62f;
        float trailAlpha = 1f - MathHelper.Clamp((attackProgress - fadeStart) / (1f - fadeStart), 0f, 1f);
        float visibleSweep = totalArc * MathHelper.Clamp(eased, 0.08f, 1f);
        if (!(figureCarriesScythe && art is not null && DrawFlameSlash(batch, art, playerPosition, behind: false)))
        {
            float outerThickness = thickness + (ActiveStep switch { 1 => 3f, 2 => 6f, _ => 10f });
            batch.DrawArc(pixel, trailCentre, radius, start, visibleSweep, GameBalance.DeepViolet * (0.62f * trailAlpha), outerThickness, ActiveStep == 3 ? 34 : 24, squash);
            batch.DrawArc(pixel, trailCentre, radius, start, visibleSweep, trail * trailAlpha, thickness, ActiveStep == 3 ? 34 : 24, squash);
            if (ActiveStep == 3)
            {
                batch.DrawArc(pixel, trailCentre, radius + 3f, start, visibleSweep, GameBalance.SoulWhite * (0.82f * trailAlpha), 4.5f, 34, squash);
            }
        }

        if (!figureCarriesScythe)
        {
            Vector2 bladeDirection = new(MathF.Cos(current), MathF.Sin(current));
            batch.DrawSpriteOrDummy(pixel, physicalScythe, playerPosition + bladeDirection * 29f, current, ActiveStep switch { 1 => 0.55f, 2 => 0.6f, _ => 0.7f });
        }

        if (debugVisible)
        {
            ScytheStrike strike = BuildStrike(ActiveStep, _attackDirection, _resonanceActive, _attributes);
            batch.DrawArc(pixel, playerPosition, strike.Range, aim - strike.ArcRadians * 0.5f, strike.ArcRadians, new Color(80, 220, 210) * 0.65f, 2f, 28);
        }
    }
}
