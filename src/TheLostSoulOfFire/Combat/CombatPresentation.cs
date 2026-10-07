using System;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Combat;

public static class CombatFeedbackTuning
{
    public const float ScytheHitstop1 = 0.034f;
    public const float ScytheHitstop2 = 0.045f;
    public const float SoulCleaveHitstop = 0.088f;
    public const float NormalCannonHitstop = 0.045f;
    public const float FullCannonHitstop = 0.115f;
    public const float BurningCompressionDuration = 0.18f;
    public const float BurningDetonationHitstop = 0.1f;
    public const float ResonanceSilenceDuration = 0.075f;
}

public sealed class CombatPresentation
{
    private readonly ParticleSystem _particles;
    private readonly ScreenEffects _screenEffects;
    private readonly SpriteVfxSystem _spriteVfx;
    private float _resonanceEruptionTimer;
    private Vector2 _resonancePosition;

    public CombatPresentation(
        ParticleSystem particles,
        ScreenEffects screenEffects,
        SpriteVfxSystem spriteVfx)
    {
        _particles = particles;
        _screenEffects = screenEffects;
        _spriteVfx = spriteVfx;
    }

    public void Update(float deltaTime)
    {
        if (_resonanceEruptionTimer <= 0f)
        {
            return;
        }

        _resonanceEruptionTimer = MathF.Max(0f, _resonanceEruptionTimer - deltaTime);
        if (_resonanceEruptionTimer <= 0f)
        {
            PresentResonanceEruption();
        }
    }

    /// <summary>
    /// Set when the scythe draws its own Death Flame ribbon along the blade; the flat slash
    /// sprites are then left out.
    /// </summary>
    public bool SlashRibbons { get; set; }

    public void PresentScytheSwing(int step, Vector2 playerPosition, Vector2 direction)
    {
        if (SlashRibbons)
        {
            return;
        }

        string effect = step switch
        {
            2 => VisualIds.ScytheSlash2,
            3 => VisualIds.ScytheCleave,
            _ => VisualIds.ScytheSlash1
        };
        float scale = step switch
        {
            2 => 0.68f,
            3 => 0.92f,
            _ => 0.5f
        };
        Color color = step switch
        {
            2 => new Color(225, 202, 255),
            3 => Color.White,
            _ => new Color(188, 139, 232)
        };

        _spriteVfx.Spawn(
            effect,
            playerPosition + direction * (step == 3 ? 27f : 20f),
            MathF.Atan2(direction.Y, direction.X),
            scale,
            color);
    }

    public void SpawnScytheContact(
        int step,
        Vector2 position,
        Vector2 direction,
        bool coreHit)
    {
        int particleCount = coreHit ? 18 : step switch { 1 => 7, 2 => 11, _ => 21 };
        float force = step switch { 1 => 125f, 2 => 175f, _ => 285f };
        float size = coreHit ? 8f : step switch { 1 => 4f, 2 => 5.5f, _ => 9f };
        Color color = coreHit || step == 3 ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        float contactScale = coreHit ? 0.66f : step switch { 1 => 0.27f, 2 => 0.38f, _ => 0.62f };

        _spriteVfx.Spawn(VisualIds.CoreHit, position, MathF.Atan2(direction.Y, direction.X), contactScale, color);
        _particles.EmitBurst(position, direction, particleCount, color, force, size);
        if (step == 3)
        {
            _particles.EmitDeathFlame(position, 7, 1.08f);
        }
    }

    /// <summary>
    /// Feedback of a landed swing at <paramref name="at"/>: hitstop (unchanged timing), a short
    /// rock of the camera that grows with the step, a push along the blow and light bursting
    /// from the contact; the Soul Cleave also leans the view in for a moment.
    /// </summary>
    public void PresentScytheImpact(int step, Vector2 direction, Vector2 at)
    {
        float hitstop = step switch
        {
            1 => CombatFeedbackTuning.ScytheHitstop1,
            2 => CombatFeedbackTuning.ScytheHitstop2,
            _ => CombatFeedbackTuning.SoulCleaveHitstop
        };
        _screenEffects.BeginHitstop(hitstop);

        switch (step)
        {
            case 1:
                _screenEffects.AddShake(0.05f, 0.6f);
                _screenEffects.AddCameraKick(direction, 0.8f);
                _screenEffects.FlashAt(InAir(at), 0.035f, 0.05f);
                break;
            case 2:
                _screenEffects.AddShake(0.08f, 1.8f);
                _screenEffects.AddCameraKick(direction, 1.8f);
                _screenEffects.FlashAt(InAir(at), 0.05f, 0.09f);
                break;
            default:
                _screenEffects.BeginImpactFrame(0.038f);
                _screenEffects.AddShake(0.2f, 5.5f);
                _screenEffects.AddCameraKick(direction, 5.5f);
                _screenEffects.AddZoomPunch(0.012f);
                _screenEffects.FlashAt(InAir(at), 0.085f, 0.24f);
                break;
        }
    }

    /// <param name="muzzle">Where the drawn muzzle is while the recoil shoves the player back, so the flash stays on it.</param>
    public void PresentCannonFire(Vector2 origin, CannonShotRequest request, Func<Vector2>? muzzle = null)
    {
        Color color = request.IsFullCharge ? Color.White : new Color(205, 164, 242);
        _spriteVfx.Spawn(
            VisualIds.CannonMuzzleFull,
            origin,
            MathF.Atan2(request.Direction.Y, request.Direction.X),
            // Sized to the 0.85 m cannon (the old flash was made for a muzzle twice as wide).
            request.IsFullCharge ? 0.64f : 0.36f,
            color,
            muzzle);
        _particles.EmitBurst(
            origin,
            request.Direction,
            request.IsFullCharge ? 25 : 10,
            request.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright,
            request.IsFullCharge ? 370f : 185f,
            request.IsFullCharge ? 10f : 5.5f);
        _particles.EmitDeathFlame(origin, request.IsFullCharge ? 13 : 5, request.IsFullCharge ? 1.42f : 0.8f);
        _screenEffects.AddShake(request.IsFullCharge ? 0.22f : 0.08f, request.IsFullCharge ? 6f : 1.2f);
        _screenEffects.AddCameraKick(-request.Direction, request.IsFullCharge ? 11f : 3f);
        _screenEffects.FlashAt(InAir(origin), request.IsFullCharge ? 0.085f : 0.045f, request.IsFullCharge ? 0.26f : 0.09f);
    }

    public void PresentCannonImpact(
        Vector2 position,
        Vector2 direction,
        bool fullCharge,
        bool coreHit)
    {
        Color color = coreHit || fullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        _spriteVfx.Spawn(
            VisualIds.CoreHit,
            position,
            MathF.Atan2(direction.Y, direction.X),
            coreHit || fullCharge ? 0.82f : 0.42f,
            color);
        _particles.EmitBurst(position, direction, fullCharge ? 30 : 14, color, fullCharge ? 390f : 215f, fullCharge ? 11f : 6f);
        _particles.EmitDeathFlame(position, fullCharge ? 12 : 5, fullCharge ? 1.3f : 0.78f);
        _screenEffects.BeginHitstop(fullCharge ? CombatFeedbackTuning.FullCannonHitstop : CombatFeedbackTuning.NormalCannonHitstop);
        _screenEffects.AddShake(fullCharge ? 0.24f : 0.09f, fullCharge ? 8f : 2f);
        _screenEffects.AddCameraKick(direction, fullCharge ? 4.5f : 1.5f);
        _screenEffects.FlashAt(InAir(position), fullCharge ? 0.095f : 0.05f, fullCharge ? 0.32f : 0.12f);
        if (fullCharge)
        {
            _screenEffects.BeginImpactFrame(0.052f);
            _screenEffects.AddZoomPunch(0.014f);
        }
    }

    public void BeginBurningCompression(Vector2 position, Vector2 incomingDirection)
    {
        _particles.EmitConvergence(position, 18, 84f, GameBalance.DeathFlameBright, 0.2f, 5f);
        _screenEffects.BeginHitstop(0.035f);
        _screenEffects.AddCameraKick(incomingDirection, 2.5f);
        _screenEffects.FlashAt(InAir(position), 0.045f, 0.11f);
    }

    public void PresentBurningDetonation(Vector2 position)
    {
        _spriteVfx.Spawn(VisualIds.BurningDetonation, position, 0f, 0.88f);
        _particles.EmitBurst(position, Vector2.UnitX, 42, GameBalance.DeathFlameBright, 430f, 12f);
        _particles.EmitDeathFlame(position, 24, 1.55f);
        _screenEffects.BeginHitstop(CombatFeedbackTuning.BurningDetonationHitstop);
        _screenEffects.BeginImpactFrame(0.058f);
        _screenEffects.AddShake(0.3f, 10f);
        _screenEffects.AddZoomPunch(0.018f);
        _screenEffects.FlashAt(InAir(position), 0.11f, 0.36f);
    }

    public void BeginResonance(Vector2 position)
    {
        _resonancePosition = position;
        _resonanceEruptionTimer = 0.065f;
        _screenEffects.BeginHitstop(CombatFeedbackTuning.ResonanceSilenceDuration);
        _screenEffects.BeginImpactFrame(0.072f);
    }

    /// <summary>Effects of the air pass are drawn at body height above their position; light bursts from there.</summary>
    private static Vector2 InAir(Vector2 position) => position - new Vector2(0f, FigureHeights.Air);

    public void Clear()
    {
        _resonanceEruptionTimer = 0f;
        _resonancePosition = Vector2.Zero;
    }

    private void PresentResonanceEruption()
    {
        _spriteVfx.Spawn(VisualIds.ResonanceActivate, _resonancePosition, 0f, 0.78f);
        _particles.EmitBurst(_resonancePosition, -Vector2.UnitY, 36, GameBalance.SoulWhite, 345f, 11f);
        _particles.EmitDeathFlame(_resonancePosition, 24, 1.7f);
        _screenEffects.AddShake(0.34f, 11f);
        _screenEffects.AddCameraKick(Vector2.UnitY, 6f);
        _screenEffects.AddZoomPunch(0.022f);
        _screenEffects.FlashAt(InAir(_resonancePosition), 0.13f, 0.44f);
    }
}
