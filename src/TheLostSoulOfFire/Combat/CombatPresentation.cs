using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

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
    public const float SeveranceWindowHitstop = 0.055f;
}

public sealed class CombatPresentation
{
    private readonly ParticleSystem _particles;
    private readonly ScreenEffects _screenEffects;
    private readonly SpriteVfxSystem _spriteVfx;
    private float _resonanceEruptionTimer;
    private Vector2 _resonancePosition;
    private Vector2 _stabilizeFrom;
    private Vector2 _stabilizeTo;
    private Color _stabilizeFlame = Color.White;
    private float _stabilizeProgress;
    private float _stabilizeEmberTimer;
    private bool _stabilizeActive;

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
        _stabilizeEmberTimer = MathF.Max(0f, _stabilizeEmberTimer - deltaTime);
        // The link is re-asserted every frame it is held, so it disappears the
        // instant the rescuer lets go or is driven off.
        _stabilizeActive = false;

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

    public void PresentScytheSwing(int step, Vector2 playerPosition, Vector2 direction)
    {
        string effect = step switch
        {
            2 => "scythe_slash_02",
            3 => "scythe_cleave",
            _ => "scythe_slash_01"
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
            color * (step == 3 ? 0.38f : 0.26f));
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

        _spriteVfx.Spawn("core_hit", position, MathF.Atan2(direction.Y, direction.X), contactScale, color);
        _particles.EmitBurst(
            position,
            direction,
            particleCount,
            color,
            force,
            size,
            coreHit || step == 3 ? VisualEffectPriority.Critical : VisualEffectPriority.Combat);
        if (step == 3)
        {
            _particles.EmitDeathFlame(position, 7, 1.08f, VisualEffectPriority.Combat);
        }
    }

    public void PresentScytheImpact(int step, Vector2 direction)
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
                _screenEffects.AddShake(0.045f, 0.75f);
                _screenEffects.Flash(0.035f, 0.055f, GameBalance.DeathFlameBright);
                break;
            case 2:
                _screenEffects.AddShake(0.075f, 2.2f);
                _screenEffects.Flash(0.05f, 0.1f, GameBalance.DeathFlameBright);
                break;
            default:
                _screenEffects.BeginImpactFrame(0.038f);
                _screenEffects.AddShake(0.19f, 7.5f);
                _screenEffects.AddCameraKick(direction, 5.5f);
                _screenEffects.Flash(0.085f, 0.27f, GameBalance.SoulWhite);
                break;
        }
    }

    public void PresentCannonFire(Vector2 origin, CannonShotRequest request)
    {
        Color color = request.IsFullCharge ? Color.White : new Color(205, 164, 242);
        _spriteVfx.Spawn(
            "cannon_muzzle_full",
            origin,
            MathF.Atan2(request.Direction.Y, request.Direction.X),
            request.IsFullCharge ? 0.76f : 0.43f,
            color);
        _particles.EmitBurst(
            origin,
            request.Direction,
            request.IsFullCharge ? 25 : 10,
            request.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright,
            request.IsFullCharge ? 370f : 185f,
            request.IsFullCharge ? 10f : 5.5f,
            request.IsFullCharge ? VisualEffectPriority.Critical : VisualEffectPriority.Combat);
        _particles.EmitDeathFlame(
            origin,
            request.IsFullCharge ? 13 : 5,
            request.IsFullCharge ? 1.42f : 0.8f,
            request.IsFullCharge ? VisualEffectPriority.Critical : VisualEffectPriority.Combat);
        _screenEffects.AddShake(request.IsFullCharge ? 0.24f : 0.08f, request.IsFullCharge ? 8f : 1.5f);
        _screenEffects.AddCameraKick(-request.Direction, request.IsFullCharge ? 12f : 3f);
        _screenEffects.Flash(
            request.IsFullCharge ? 0.085f : 0.045f,
            request.IsFullCharge ? 0.28f : 0.1f,
            request.IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright);
    }

    public void PresentCannonImpact(
        Vector2 position,
        Vector2 direction,
        bool fullCharge,
        bool coreHit)
    {
        Color color = coreHit || fullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        _spriteVfx.Spawn(
            "core_hit",
            position,
            MathF.Atan2(direction.Y, direction.X),
            coreHit || fullCharge ? 0.82f : 0.42f,
            color);
        _particles.EmitBurst(
            position,
            direction,
            fullCharge ? 30 : 14,
            color,
            fullCharge ? 390f : 215f,
            fullCharge ? 11f : 6f,
            coreHit || fullCharge ? VisualEffectPriority.Critical : VisualEffectPriority.Combat);
        _particles.EmitDeathFlame(
            position,
            fullCharge ? 12 : 5,
            fullCharge ? 1.3f : 0.78f,
            fullCharge ? VisualEffectPriority.Critical : VisualEffectPriority.Combat);
        _screenEffects.BeginHitstop(fullCharge ? CombatFeedbackTuning.FullCannonHitstop : CombatFeedbackTuning.NormalCannonHitstop);
        _screenEffects.AddShake(fullCharge ? 0.25f : 0.09f, fullCharge ? 10.5f : 2.5f);
        _screenEffects.AddCameraKick(-direction, fullCharge ? 4.5f : 1.5f);
        _screenEffects.Flash(
            fullCharge ? 0.095f : 0.05f,
            fullCharge ? 0.34f : 0.13f,
            coreHit || fullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright);
        if (fullCharge)
        {
            _screenEffects.BeginImpactFrame(0.052f);
        }
    }

    public void BeginBurningCompression(Vector2 position, Vector2 incomingDirection)
    {
        _particles.EmitConvergence(position, 18, 84f, GameBalance.DeathFlameBright, 0.2f, 5f);
        _screenEffects.BeginHitstop(0.035f);
        _screenEffects.AddCameraKick(-incomingDirection, 2.5f);
        _screenEffects.Flash(0.045f, 0.11f, GameBalance.DeathFlameBright);
    }

    public void PresentBurningDetonation(Vector2 position)
    {
        _spriteVfx.Spawn("burning_detonation", position, 0f, 0.88f);
        _particles.EmitBurst(
            position,
            Vector2.UnitX,
            42,
            GameBalance.DeathFlameBright,
            430f,
            12f,
            VisualEffectPriority.Critical);
        _particles.EmitDeathFlame(position, 24, 1.55f, VisualEffectPriority.Critical);
        _screenEffects.BeginHitstop(CombatFeedbackTuning.BurningDetonationHitstop);
        _screenEffects.BeginImpactFrame(0.058f);
        _screenEffects.AddShake(0.3f, 12.5f);
        _screenEffects.Flash(0.11f, 0.4f, GameBalance.SoulWhite);
    }

    /// <summary>
    /// Confirms the read. Short, quiet and legible: a held beat plus a thread of
    /// light between the Warden and the Anchor they have just found.
    /// </summary>
    public void PresentSeveranceWindow(Vector2 playerPosition, Vector2 anchorPosition)
    {
        _screenEffects.BeginHitstop(CombatFeedbackTuning.SeveranceWindowHitstop);
        _screenEffects.Flash(0.05f, 0.06f, GameBalance.SoulWhite);
        // Converge from further out and land fewer sparks, so the read brightens
        // the space around the Warden instead of filling his silhouette in.
        _particles.EmitConvergence(playerPosition, 9, 118f, GameBalance.SoulWhite, 0.26f, 3.4f, VisualEffectPriority.Critical);
        _particles.EmitBurst(
            Vector2.Lerp(playerPosition, anchorPosition, 0.5f),
            Vector2.Normalize(SafeDelta(anchorPosition, playerPosition)),
            7,
            GameBalance.DeathFlameBright,
            120f,
            3.5f,
            VisualEffectPriority.Critical);
    }

    /// <summary>
    /// The payoff. The Anchor comes apart: a white cleave, a converging collapse
    /// and a hard held frame. It reads as separation rather than as a bigger hit.
    /// </summary>
    public void PresentSeveranceCut(Vector2 anchorPosition, Vector2 direction)
    {
        float rotation = MathF.Atan2(direction.Y, direction.X);
        _spriteVfx.Spawn("scythe_cleave", anchorPosition, rotation, 1.12f, Color.White * 0.52f);
        _spriteVfx.Spawn("core_hit", anchorPosition, rotation, 0.94f, GameBalance.SoulWhite);
        _spriteVfx.Spawn("soul_release", anchorPosition, 0f, 0.52f, GameBalance.DeathFlameBright * 0.7f);
        _particles.EmitBurst(anchorPosition, direction, 26, GameBalance.SoulWhite, 340f, 9f, VisualEffectPriority.Critical);
        _particles.EmitBurst(anchorPosition, -direction, 14, GameBalance.DeathFlameBright, 210f, 6f, VisualEffectPriority.Critical);
        _particles.EmitDeathFlame(anchorPosition, 14, 1.32f, VisualEffectPriority.Critical);
        _screenEffects.BeginHitstop(GameBalance.SeveranceHitstop);
        _screenEffects.BeginImpactFrame(0.062f);
        _screenEffects.AddShake(0.2f, 8.5f);
        _screenEffects.AddCameraKick(direction, 6.5f);
        _screenEffects.Flash(0.095f, 0.31f, GameBalance.SoulWhite);
    }

    /// <summary>
    /// A Warden's flame guttering. Collapse inward and downward: it must not read
    /// like an enemy death, because nothing has been destroyed yet.
    /// </summary>
    public void PresentWardenDown(Vector2 position, Color flame)
    {
        _spriteVfx.Spawn("death_flame_loop", position, 0f, 0.5f, flame * 0.5f);
        _particles.EmitConvergence(position, 16, 96f, flame, 0.5f, 4f, VisualEffectPriority.Critical);
        _particles.EmitDeathFlame(position, 10, 0.7f, VisualEffectPriority.Critical);
        _screenEffects.AddShake(0.22f, 6.5f);
        _screenEffects.Flash(0.1f, 0.18f, flame);
    }

    /// <summary>
    /// The hold. A thread of the standing brother's flame reaching across to the
    /// one on the floor, brightening as the hold completes. Painted, not drawn.
    /// </summary>
    public void PresentStabilizeHold(Vector2 from, Vector2 to, Color flame, float progress)
    {
        _stabilizeFrom = from;
        _stabilizeTo = to;
        _stabilizeFlame = flame;
        _stabilizeProgress = MathHelper.Clamp(progress, 0f, 1f);
        _stabilizeActive = true;

        if (_stabilizeEmberTimer > 0f)
        {
            return;
        }

        _stabilizeEmberTimer = 0.075f;
        _particles.EmitBurst(
            Vector2.Lerp(from, to, 0.35f),
            Vector2.Normalize(SafeDelta(to, from)),
            2,
            flame,
            150f,
            2.4f);
    }

    public void PresentStabilizeComplete(Vector2 position, Color flame)
    {
        _stabilizeActive = false;
        _spriteVfx.Spawn("resonance_activate", position, 0f, 0.72f, flame * 0.72f);
        _particles.EmitBurst(position, -Vector2.UnitY, 22, flame, 210f, 6f, VisualEffectPriority.Critical);
        _particles.EmitDeathFlame(position, 14, 1.1f, VisualEffectPriority.Critical);
        _screenEffects.Flash(0.12f, 0.2f, flame);
        _screenEffects.AddShake(0.12f, 3f);
    }

    /// <summary>The brother stepping into the encounter through his own Death Flame.</summary>
    public void PresentArrivalFlame(Vector2 position)
    {
        _particles.EmitConvergence(position, 18, 130f, WardenIdentity.Elder.FlameBright, 0.35f, 5f, VisualEffectPriority.Critical);
        _particles.EmitDeathFlame(position, 12, 1.15f, VisualEffectPriority.Critical);
        _screenEffects.AddShake(0.1f, 3f);
    }

    /// <summary>
    /// Drawn in the additive combat-light pass with everything else, so the
    /// rescue line is flame in the room rather than an interface connector.
    /// </summary>
    public void DrawStabilizeLink(SpriteBatch batch, Texture2D brush, float time)
    {
        if (!_stabilizeActive)
        {
            return;
        }

        Vector2 delta = _stabilizeTo - _stabilizeFrom;
        float distance = delta.Length();
        if (distance < 1f)
        {
            return;
        }

        Vector2 direction = delta / distance;
        int steps = Math.Max(4, (int)(distance / 16f));
        for (int i = 0; i < steps; i++)
        {
            float amount = (i + 0.5f) / steps;
            float flow = (amount + time * 1.6f) % 1f;
            float taper = MathF.Sin(flow * MathHelper.Pi);
            Vector2 point = _stabilizeFrom + direction * (distance * flow);
            SoftShapes.Blob(batch, brush, point, (5f + _stabilizeProgress * 4f) * taper,
                _stabilizeFlame * ((0.16f + _stabilizeProgress * 0.24f) * taper));
        }

        SoftShapes.Blob(batch, brush, _stabilizeTo, 26f + _stabilizeProgress * 20f,
            _stabilizeFlame * (0.1f + _stabilizeProgress * 0.24f));
    }

    private static Vector2 SafeDelta(Vector2 to, Vector2 from)
    {
        Vector2 delta = to - from;
        return delta.LengthSquared() > 0.0001f ? delta : Vector2.UnitX;
    }

    public void BeginResonance(Vector2 position)
    {
        _resonancePosition = position;
        _resonanceEruptionTimer = 0.065f;
        _screenEffects.BeginHitstop(CombatFeedbackTuning.ResonanceSilenceDuration);
        _screenEffects.BeginImpactFrame(0.072f);
    }

    public void Clear()
    {
        _resonanceEruptionTimer = 0f;
        _resonancePosition = Vector2.Zero;
    }

    private void PresentResonanceEruption()
    {
        _spriteVfx.Spawn("resonance_activate", _resonancePosition, 0f, 0.56f, GameBalance.DeathFlameBright * 0.4f);
        _particles.EmitBurst(
            _resonancePosition,
            -Vector2.UnitY,
            18,
            GameBalance.SoulWhite,
            345f,
            6f,
            VisualEffectPriority.Critical);
        _particles.EmitDeathFlame(_resonancePosition, 12, 1f, VisualEffectPriority.Critical);
        _screenEffects.AddShake(0.22f, 8f);
        _screenEffects.AddCameraKick(Vector2.UnitY, 6f);
        _screenEffects.Flash(0.07f, 0.16f, GameBalance.SoulWhite);
    }
}
