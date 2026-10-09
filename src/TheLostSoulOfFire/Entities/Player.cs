using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Entities;

public sealed class Player
{
    private sealed class Afterimage
    {
        public Vector2 Position;
        public Vector2 Facing;
        public float Remaining;
        public float Lifetime;
    }

    private readonly List<Afterimage> _afterimages = [];
    private float _idleParticleTimer;
    private float _visualTime;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashTrailTimer;
    private float _afterimageTimer;
    private float _resonanceTimer;
    private float _resonanceActivationTimer;
    private float _resonanceAfterimageTimer;
    private float _activeDashDistance = GameBalance.DashDistance;
    private Vector2 _dashDirection = Vector2.UnitX;
    private Vector2 _attackImpulse;
    private Vector2 _damageKnockback;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.UnitX;
    public Vector2 DashDirection => _dashDirection;
    public int Health { get; private set; } = GameBalance.PlayerMaxHealth;
    /// <summary>Starts at <see cref="GameBalance.PlayerMaxHealth"/>; only the sandbox dev menu changes it.</summary>
    public int MaxHealth { get; private set; } = GameBalance.PlayerMaxHealth;
    public float Radius => GameBalance.PlayerRadius;
    public float InvulnerabilityRemaining { get; private set; }
    public float HitFlashRemaining { get; private set; }

    /// <summary>Presentation only: how far a backward leap (Rückstoßsprung) has run, 0..1, or null without one.</summary>
    public float? LeapProgress { get; set; }

    /// <summary>Presentation only: seconds since the player fell (set by the game each frame), 0 while alive.</summary>
    public float SinceDeath { get; set; }

    /// <summary>Presentation only: how far the waking at the start of the prologue has run, 0..1, or null.</summary>
    public float? WakeProgress { get; set; }

    /// <summary>
    /// Height of the Death Flame core above the feet as drawn: standing, or close to the floor
    /// while the figure lies and rises at the start of the prologue (wake clip in build_player.py).
    /// </summary>
    public float DrawnCoreHeight => WakeProgress is { } wake
        ? MathHelper.Lerp(10f, FigureHeights.Core, MathHelper.SmoothStep(0f, 1f, (wake - 0.3f) / 0.6f))
        : FigureHeights.Core;

    /// <summary>Presentation only: the direction the last blow pushed the player (normalised).</summary>
    public Vector2 LastHitDirection { get; private set; } = Vector2.UnitY;
    public float DashCooldownRemaining => _dashCooldownTimer;
    public bool IsDashing => _dashTimer > 0f;

    /// <summary>Presentation only: how far the current dash has run (0..1).</summary>
    public float DashProgress => MathHelper.Clamp(1f - _dashTimer / GameBalance.DashDuration, 0f, 1f);
    public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
    public bool IsDead => Health <= 0;
    public float Resonance { get; private set; }
    public bool IsResonanceReady => !ResonanceActive && Resonance >= GameBalance.ResonanceRequired;
    public bool ResonanceActive { get; private set; }
    public float ResonanceRemaining => _resonanceTimer;
    public float ResonanceActivationRemaining => _resonanceActivationTimer;
    public bool SoulSenseActive { get; private set; }
    public PlayerAttributes Attributes { get; set; } = PlayerAttributes.Default;
    public AbilityEffects AbilityEffects { get; } = new();
    public ScytheCombat Scythe { get; } = new();
    public SoulCannon Cannon { get; } = new();

    public Player(Vector2 position)
    {
        Position = position;
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        FacingDirection = Vector2.UnitX;
        Health = MaxHealth;
        _idleParticleTimer = 0f;
        _dashTimer = 0f;
        _dashCooldownTimer = 0f;
        InvulnerabilityRemaining = 0f;
        HitFlashRemaining = 0f;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        Resonance = 0f;
        ResonanceActive = false;
        _resonanceTimer = 0f;
        _resonanceActivationTimer = 0f;
        _resonanceAfterimageTimer = 0f;
        _activeDashDistance = GameBalance.DashDistance;
        SoulSenseActive = false;
        _afterimages.Clear();
        AbilityEffects.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    /// <summary>
    /// Moves the player to <paramref name="position"/> and drops movement and attack state, but keeps
    /// health, resonance and abilities. Used where a run continues in another room.
    /// </summary>
    public void PlaceAt(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        _dashTimer = 0f;
        _dashCooldownTimer = 0f;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    /// <summary>Sets maximum health and fills the player up to it.</summary>
    public void SetMaxHealth(int maxHealth)
    {
        MaxHealth = Math.Max(1, maxHealth);
        Health = MaxHealth;
    }

    /// <summary>
    /// Turns the figure toward <paramref name="point"/> after the last wave, when nothing is
    /// steered any more (presentation only; a restart resets the facing).
    /// </summary>
    public void LookToward(Vector2 point)
    {
        Vector2 toward = point - Position;
        if (toward.LengthSquared() > 1f)
        {
            FacingDirection = Vector2.Normalize(toward);
        }
    }

    public void SettleForCompletion()
    {
        Velocity = Vector2.Zero;
        _dashTimer = 0f;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        Resonance = 0f;
        ResonanceActive = false;
        _resonanceTimer = 0f;
        _resonanceActivationTimer = 0f;
        _resonanceAfterimageTimer = 0f;
        SoulSenseActive = false;
        _afterimages.Clear();
        AbilityEffects.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    public void Update(
        float deltaTime,
        InputState input,
        Vector2 mouseWorld,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects,
        bool forceSoulSense = false,
        bool combatEnabled = true)
    {
        AbilityEffects.Update(deltaTime);
        if (IsDead) AbilityEffects.Clear();
        _visualTime += deltaTime;
        _resonanceActivationTimer = MathF.Max(0f, _resonanceActivationTimer - deltaTime);
        HitFlashRemaining = MathF.Max(0f, HitFlashRemaining - deltaTime);
        if (ResonanceActive)
        {
            _resonanceTimer = MathF.Max(0f, _resonanceTimer - deltaTime);
            if (_resonanceTimer <= 0f)
            {
                ResonanceActive = false;
                particles.EmitDeathFlame(Position, 10, 0.72f);
            }
        }

        if (combatEnabled && !IsDead && IsResonanceReady && input.WasKeyPressed(Keys.R))
        {
            StartResonance();
        }

        SoulSenseActive = !IsDead && (ResonanceActive || forceSoulSense || input.IsKeyDown(Keys.Q));
        _dashCooldownTimer = MathF.Max(0f, _dashCooldownTimer - deltaTime);
        InvulnerabilityRemaining = MathF.Max(0f, InvulnerabilityRemaining - deltaTime);
        UpdateAfterimages(deltaTime);

        if (IsDead)
        {
            ResonanceActive = false;
            _resonanceTimer = 0f;
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 toMouse = mouseWorld - Position;
        if (toMouse.LengthSquared() > 4f)
        {
            FacingDirection = Vector2.Normalize(toMouse);
        }

        Vector2 movement = ReadMovement(input);

        if (combatEnabled)
        {
            Cannon.Update(
                deltaTime,
                input,
                Position,
                FacingDirection,
                !IsDashing && Scythe.ActiveStep == 0,
                SoulSenseActive,
                particles,
                ResonanceActive,
                Attributes);

            Scythe.Update(deltaTime, input, FacingDirection, Position, particles, !IsDashing && Cannon.CanUseScythe, ResonanceActive, Attributes);
            if (Scythe.StartedThisFrame)
            {
                _attackImpulse = Scythe.AttackDirection * Scythe.GetForwardImpulse();
            }
        }
        else
        {
            AbilityEffects.Clear();
            Scythe.Reset();
            Cannon.Reset();
            _attackImpulse = Vector2.Zero;
        }

        if (input.WasKeyPressed(Keys.Space) && _dashCooldownTimer <= 0f && Scythe.ActiveStep == 0)
        {
            StartDash(movement, particles, screenEffects);
        }

        if (_dashTimer > 0f)
        {
            UpdateDash(deltaTime, particles);
        }
        else
        {
            float movementMultiplier = SoulSenseActive && !ResonanceActive ? GameBalance.SoulSenseMovementMultiplier : 1f;
            movementMultiplier *= ResonanceActive ? GameBalance.ResonanceMovementMultiplier : 1f;
            movementMultiplier *= Cannon.GetMovementMultiplier();
            Velocity = movement * GameBalance.PlayerMoveSpeed * movementMultiplier + _attackImpulse + _damageKnockback;
            _attackImpulse *= MathF.Pow(0.002f, deltaTime);
            _damageKnockback *= MathF.Pow(0.012f, deltaTime);
        }

        Position += Velocity * deltaTime;
        ClampTo(movementBounds);

        if (ResonanceActive && movement.LengthSquared() > 0.001f && !IsDashing)
        {
            _resonanceAfterimageTimer -= deltaTime;
            if (_resonanceAfterimageTimer <= 0f)
            {
                _resonanceAfterimageTimer = 0.16f;
                AddAfterimage();
            }
        }

        _idleParticleTimer -= deltaTime;
        if (_idleParticleTimer <= 0f && !IsDashing)
        {
            _idleParticleTimer = 0.16f;
            particles.EmitDeathFlame(Position - FacingDirection * 2f, 1, 0.55f);
        }
    }

    public void DrawAfterimages(SpriteBatch batch, Texture2D pixel, ArtAssets? art = null)
    {
        foreach (Afterimage afterimage in _afterimages)
        {
            float alpha = afterimage.Remaining / afterimage.Lifetime;
            // A rendered figure leaves a violet ghost of the very pose it had there.
            Color ghost = new Color(120, 70, 210) * (alpha * alpha * 0.55f);
            ghost.A = (byte)(ghost.A * 0.7f);
            if (art is not null && art.DrawGhost(batch, this, afterimage.Position, afterimage.Lifetime - afterimage.Remaining, ghost))
            {
                continue;
            }

            Vector2 right = new(-afterimage.Facing.Y, afterimage.Facing.X);
            Color silhouette = new Color(69, 28, 112) * (alpha * 0.48f);
            batch.DrawLine(pixel, afterimage.Position - afterimage.Facing * 15f, afterimage.Position + afterimage.Facing * 14f, silhouette, 28f);
            batch.DrawLine(pixel, afterimage.Position - afterimage.Facing * 13f, afterimage.Position - afterimage.Facing * 35f + right * 9f, silhouette, 11f);
            batch.FillCircle(pixel, afterimage.Position + afterimage.Facing * 18f, 10f, silhouette);
            batch.FillCircle(pixel, afterimage.Position, 4f, GameBalance.DeathFlameBright * (alpha * 0.35f));
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, ArtAssets art, bool debugVisible, float soulSenseAmount = 0f)
    {
        if (IsDead && art.HasClip(VisualIds.Player, VisualClips.Death))
        {
            // The rendered figure plays its fall; the Death Flame accent belongs to the presentation.
            return;
        }

        if (IsDead)
        {
            float deathPulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 5f);
            batch.FillCircle(pixel, Position, 13f + deathPulse * 3f, GameBalance.DeepViolet * 0.8f);
            batch.FillCircle(pixel, Position, 6f + deathPulse, GameBalance.SoulWhite * 0.8f);
            return;
        }

        Vector2 right = new(-FacingDirection.Y, FacingDirection.X);
        float pulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 4f);
        // A rendered figure stands on Position and carries scythe and cannon in its frames; the
        // overlays then sit on its body. The older flat art is drawn around Position instead.
        bool rendered = art.HasClip(VisualIds.Player, VisualClips.Swing1);
        Vector2 body = rendered ? Position - new Vector2(0f, DrawnCoreHeight) : Position;

        if (ResonanceActive)
        {
            float flare = 0.5f + 0.5f * MathF.Sin(_visualTime * 11f);
            if (rendered)
            {
                // Resonating, the figure burns: Death Flame climbs its body and rings its core.
                art.DrawLoopingEffect(batch, this, VisualIds.DeathFlameLoop, Position - new Vector2(0f, FigureHeights.Core * 0.62f), 0f, 1.05f + flare * 0.06f, Color.White * 0.55f);
                Color aura = GameBalance.DeathFlame * (0.35f + flare * 0.1f);
                aura.A = 0;
                art.DrawSoftSpot(batch, body, new Vector2(40f + flare * 6f), aura);
            }
            else
            {
                batch.DrawCircle(pixel, body, 34f + flare * 6f, GameBalance.DeathFlame * 0.72f, 8f, 28);
                batch.DrawLine(pixel, body - right * 20f, body - right * 32f - Vector2.UnitY * (30f + flare * 15f), GameBalance.DeathFlame * 0.62f, 8f);
                batch.DrawLine(pixel, body + right * 18f, body + right * 29f - Vector2.UnitY * (37f + flare * 11f), GameBalance.DeathFlameBright * 0.7f, 6f);
            }
        }

        if (!rendered)
        {
            Cannon.DrawBack(batch, pixel, art.GetSpriteTexture(VisualIds.SoulCannon), Position, FacingDirection);
        }
        Scythe.Draw(batch, pixel, art.GetSpriteTexture(VisualIds.Scythe), Position, FacingDirection, debugVisible, rendered, art);

        if (rendered)
        {
            DrawBodyMarks(batch, pixel, art, body, pulse, soulSenseAmount);
        }
        else
        {
            DrawFlatMarks(batch, pixel, right, pulse, soulSenseAmount);
            Cannon.DrawActive(batch, pixel, art.GetSpriteTexture(VisualIds.SoulCannon), Position, FacingDirection);
        }

        if (HitFlashRemaining > 0f)
        {
            // The blow as light bursting from the body (the sprite itself flashes in SpriteLit),
            // widening as it fades: no ring.
            float flash = MathHelper.Clamp(HitFlashRemaining / 0.14f, 0f, 1f);
            Color burst = GameBalance.SoulWhite * (0.45f * flash);
            burst.A = 0;
            art.DrawSoftSpot(batch, body, new Vector2(22f + (1f - flash) * 26f), burst);
            art.DrawSoftSpot(batch, body, new Vector2(12f), GameBalance.SoulWhite * (0.88f * flash));
        }

        if (IsDashing)
        {
            if (rendered)
            {
                // Death Flame streams off the body as soft light along the dash, thinning behind it
                // (the ignition flipbook marks the start, afterimages carry the shape): no streaks.
                for (int index = 0; index < 7; index++)
                {
                    float back = 14f + index * 9f;
                    float fade = 1f - index / 7f;
                    float sway = MathF.Sin(_visualTime * 18f + index * 1.7f) * 4f * (index / 7f);
                    Vector2 at = body - _dashDirection * back + right * sway;
                    Color flame = Color.Lerp(GameBalance.DeathFlameBright, GameBalance.DeathFlame, index / 7f) * (0.5f * fade);
                    flame.A = 0;
                    art.DrawSoftSpot(batch, at, new Vector2(16f - index * 1.4f, 13f - index * 1.2f), flame);
                }
            }
            else
            {
                Vector2 ignitionOrigin = Position - _dashDirection * 15f;
                batch.DrawLine(pixel, ignitionOrigin - right * 8f, ignitionOrigin - _dashDirection * 23f - right * 11f, GameBalance.DeathFlame, 7f);
                batch.DrawLine(pixel, ignitionOrigin + right * 8f, ignitionOrigin - _dashDirection * 27f + right * 12f, GameBalance.DeathFlameBright, 5f);
            }
        }

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, Position, Position + FacingDirection * 70f, new Color(80, 220, 210) * 0.8f, 2f);
        }
    }

    /// <summary>
    /// Core and Soul Sense on a rendered figure: the core glows faintly under the sternum and
    /// rings when Resonance is ready; under Soul Sense the eyes burn violet.
    /// </summary>
    private void DrawBodyMarks(SpriteBatch batch, Texture2D pixel, ArtAssets art, Vector2 core, float pulse, float soulSenseAmount)
    {
        bool coreReady = IsResonanceReady;
        // Seen from behind, the core is hidden by the body; its readiness ring stays visible.
        float front = MathHelper.Clamp(0.5f + FacingDirection.Y, 0f, 1f);
        Vector2 sternum = core + new Vector2(FacingDirection.X * 6f, FacingDirection.Y * 4f);
        if (front > 0f)
        {
            float glow = (ResonanceActive || coreReady || SoulSenseActive ? 0.85f : 0.4f) * front;
            // Feeding the cannon drains the core; after the shot it rekindles as the cannon goes back.
            glow *= Cannon.State switch
            {
                SoulCannonState.Charging => 1f - 0.55f * Cannon.ChargeProgress,
                SoulCannonState.Returning => 0.3f + 0.7f * Cannon.StateProgress,
                _ => 1f
            };
            float size = 7f + pulse * (coreReady ? 3f : 1f);
            art.DrawSoftSpot(batch, sternum, new Vector2(size), GameBalance.DeathFlame * (0.8f * glow));
            art.DrawSoftSpot(batch, sternum, new Vector2(size * 0.4f), GameBalance.SoulWhite * glow);
        }
        if (coreReady)
        {
            // Resonance ready: the core beats with light (no ring), visible from behind as well.
            Color beat = GameBalance.DeathFlameBright * (0.4f + pulse * 0.3f);
            beat.A = 0;
            art.DrawSoftSpot(batch, core, new Vector2(16f + pulse * 6f), beat);
        }

        float sense = MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        if (sense > 0.001f && FacingDirection.Y > -0.35f)
        {
            Vector2 eyes = Position - new Vector2(0f, FigureHeights.Eyes) + new Vector2(FacingDirection.X * 6f, FacingDirection.Y * 3f);
            Vector2 across = new(MathF.Abs(FacingDirection.Y) * 3.5f + 1f, 0f);
            // The eyes glint violet (subtle, owner): no line from the core.
            art.DrawSoftSpot(batch, eyes, new Vector2(8f, 6f), GameBalance.DeathFlame * (0.25f * sense));
            art.DrawSoftSpot(batch, eyes - across, new Vector2(2f), GameBalance.DeathFlameBright * (0.7f * sense));
            art.DrawSoftSpot(batch, eyes + across, new Vector2(2f), GameBalance.DeathFlameBright * (0.7f * sense));
        }
    }

    /// <summary>Eye bar and core on the older flat, top-down art.</summary>
    private void DrawFlatMarks(SpriteBatch batch, Texture2D pixel, Vector2 right, float pulse, float soulSenseAmount)
    {
        Vector2 head = Position + FacingDirection * 18f;

        Vector2 eye = head + FacingDirection * 8f;
        float sense = MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        Color eyeColor = Color.Lerp(new Color(174, 166, 183), GameBalance.SoulWhite, sense);
        if (sense > 0.001f)
        {
            batch.FillCircle(pixel, eye, 8f, GameBalance.DeepViolet * (0.68f * sense));
            batch.DrawLine(pixel, Position + FacingDirection * 4f, head, GameBalance.DeathFlame * (0.5f * sense), 4f);
        }
        batch.DrawLine(pixel, eye - right * 4f, eye + right * 4f, eyeColor, MathHelper.Lerp(2f, 3f, sense));

        bool coreReady = IsResonanceReady;
        float coreRadius = coreReady ? 9f + pulse * 2.4f : 7f + pulse * 1.3f;
        batch.FillCircle(pixel, Position + FacingDirection * 2f, coreRadius, GameBalance.DeepViolet * 0.75f);
        float coreAlpha = ResonanceActive || coreReady || SoulSenseActive ? 1f : 0.88f;
        batch.FillCircle(pixel, Position + FacingDirection * 2f, 3.2f + pulse * (coreReady ? 1.8f : 0.6f), GameBalance.SoulWhite * coreAlpha);
        if (coreReady)
        {
            batch.DrawCircle(pixel, Position + FacingDirection * 2f, 14f + pulse * 5f, GameBalance.DeathFlameBright * 0.78f, 3f, 20);
        }

        if (ResonanceActive)
        {
            batch.DrawLine(pixel, Position + FacingDirection * 2f, Position - right * 14f - Vector2.UnitY * 15f, GameBalance.DeathFlameBright * 0.72f, 3f);
            batch.DrawLine(pixel, Position + FacingDirection * 2f, Position + right * 13f + Vector2.UnitY * 13f, GameBalance.DeathFlame * 0.72f, 3f);
        }
    }

    private void StartDash(Vector2 movement, ParticleSystem particles, ScreenEffects screenEffects)
    {
        _dashDirection = movement.LengthSquared() > 0.001f ? Vector2.Normalize(movement) : FacingDirection;
        _dashTimer = GameBalance.DashDuration;
        _activeDashDistance = GameBalance.DashDistance * (ResonanceActive ? GameBalance.ResonanceDashDistanceMultiplier : 1f);
        _dashCooldownTimer = GameBalance.DashCooldown * (ResonanceActive ? GameBalance.ResonanceDashCooldownMultiplier : 1f);
        InvulnerabilityRemaining = GameBalance.DashInvulnerability;
        _dashTrailTimer = 0f;
        _afterimageTimer = 0f;
        Velocity = _dashDirection * (_activeDashDistance / GameBalance.DashDuration);

        AddAfterimage();
        particles.EmitBurst(Position - _dashDirection * 12f, -_dashDirection, 12, GameBalance.DeathFlameBright, 145f, 6f);
        particles.EmitDeathFlame(Position, 8, 1.35f);
        screenEffects.AddShake(0.1f, 3f);
    }

    public void ApplyDamage(int damage, Vector2 knockback, ScreenEffects screenEffects, bool ignoreArmor = false)
    {
        if (IsDead || IsInvulnerable)
        {
            return;
        }

        if (damage <= 0) return;
        if (!ignoreArmor && AbilityEffects.TryBlock())
        {
            InvulnerabilityRemaining = 0.12f;
            screenEffects.Flash(0.08f, 0.15f);
            return;
        }
        int taken = ignoreArmor ? damage : Attributes.MitigateIncomingDamage(damage);
        Health = Math.Max(0, Health - taken);
        if (IsDead) AbilityEffects.Clear();
        HitFlashRemaining = Health == 0 ? 0.24f : 0.14f;
        _damageKnockback += knockback;
        if (knockback.LengthSquared() > 0.01f)
        {
            LastHitDirection = Vector2.Normalize(knockback);
        }
        InvulnerabilityRemaining = 0.5f;
        screenEffects.BeginHitstop(Health == 0 ? 0.12f : 0.045f);
        screenEffects.AddShake(Health == 0 ? 0.28f : 0.12f, Health == 0 ? 7f : 4f);
        screenEffects.AddCameraKick(knockback, Health == 0 ? 6f : 4f);
        if (Health == 0)
        {
            screenEffects.AddZoomPunch(0.02f);
        }
        screenEffects.FlashAt(Position - new Vector2(0f, FigureHeights.Core), 0.09f, Health == 0 ? 0.34f : 0.2f);
    }

    public void Heal(int amount)
    {
        if (!IsDead && amount > 0) Health = Math.Min(MaxHealth, Health + amount);
    }

    public void MoveByAbility(Vector2 offset, Rectangle bounds)
    {
        if (IsDead) return;
        Position = RunAbilities.Clamp(Position + offset, bounds, Radius);
    }

    public void AddResonance(float amount)
    {
        Resonance = MathHelper.Clamp(Resonance + amount, 0f, GameBalance.ResonanceRequired);
    }

    public void FillResonance()
    {
        if (!ResonanceActive)
        {
            Resonance = GameBalance.ResonanceRequired;
        }
    }

    public void ApplyCannonRecoil(Vector2 shotDirection, float charge)
    {
        _damageKnockback -= shotDirection * MathHelper.Lerp(180f, 520f, charge);
    }

    private void UpdateDash(float deltaTime, ParticleSystem particles)
    {
        _dashTimer = MathF.Max(0f, _dashTimer - deltaTime);
        Velocity = _dashDirection * (_activeDashDistance / GameBalance.DashDuration);

        _dashTrailTimer -= deltaTime;
        if (_dashTrailTimer <= 0f)
        {
            _dashTrailTimer = 0.02f;
            particles.EmitDeathFlame(Position - _dashDirection * 10f, 2, 1.05f);
        }

        _afterimageTimer -= deltaTime;
        if (_afterimageTimer <= 0f && _afterimages.Count < (ResonanceActive ? 5 : 3))
        {
            _afterimageTimer = 0.045f;
            AddAfterimage();
        }

        if (_dashTimer <= 0f)
        {
            Velocity *= 0.18f;
        }
    }

    private void AddAfterimage()
    {
        _afterimages.Add(new Afterimage
        {
            Position = Position,
            Facing = FacingDirection,
            Remaining = 0.19f,
            Lifetime = 0.19f
        });
    }

    private void StartResonance()
    {
        Resonance = 0f;
        ResonanceActive = true;
        _resonanceTimer = GameBalance.ResonanceDuration;
        _resonanceActivationTimer = 0.5f;
        SoulSenseActive = true;
        AddAfterimage();
    }

    private void UpdateAfterimages(float deltaTime)
    {
        for (int i = _afterimages.Count - 1; i >= 0; i--)
        {
            _afterimages[i].Remaining -= deltaTime;
            if (_afterimages[i].Remaining <= 0f)
            {
                _afterimages.RemoveAt(i);
            }
        }
    }

    public static Vector2 ReadMovement(InputState input)
    {
        Vector2 movement = Vector2.Zero;
        if (input.IsKeyDown(Keys.W)) movement.Y -= 1f;
        if (input.IsKeyDown(Keys.S)) movement.Y += 1f;
        if (input.IsKeyDown(Keys.A)) movement.X -= 1f;
        if (input.IsKeyDown(Keys.D)) movement.X += 1f;

        return movement.LengthSquared() > 1f ? Vector2.Normalize(movement) : movement;
    }

    private void ClampTo(Rectangle bounds)
    {
        Position = new Vector2(
            MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
            MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius));
    }
}
