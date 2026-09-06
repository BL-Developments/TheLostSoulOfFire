using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

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
    private float _severanceTimer;
    private float _severanceFlareTimer;
    private Vector2 _dashDirection = Vector2.UnitX;
    private Vector2 _attackImpulse;
    private Vector2 _damageKnockback;

    // --- presentation-only facing and gait state -------------------------
    // None of this feeds combat. FacingDirection stays the raw aim vector, so
    // every hitbox, telegraph and light is exactly what it was; these values
    // only decide which frame is drawn.
    private float _bodyAngle;
    private int _facingSector = 2;
    private float _gaitPhase;
    private bool _running;
    private bool _backpedalling;

    /// <summary>
    /// Which brother this Warden is. Drives body tint, flame colour and HUD
    /// accent; it is the only thing that differs between the two local players.
    /// </summary>
    public WardenIdentity Identity { get; }

    /// <summary>Tint applied to the shared Warden sheet.</summary>
    public Color BodyTint => Identity.BodyTint;

    /// <summary>
    /// Per-Warden offset so two brothers standing together never breathe their
    /// flame on the same frame, which would read as one doubled character.
    /// </summary>
    public float LightPhase { get; }

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.UnitX;

    /// <summary>
    /// Where the body is actually pointed, which is not the same thing as where
    /// the mouse is. The Warden turns at a bounded rate toward the aim instead
    /// of teleporting his shoulders onto it every frame.
    /// </summary>
    public Vector2 BodyFacing => new(MathF.Cos(_bodyAngle), MathF.Sin(_bodyAngle));

    /// <summary>
    /// The eight-way sheet the body is currently showing, with hysteresis. A
    /// sector is only given up once the body is clearly past the boundary, so a
    /// mouse resting on a diagonal cannot make the character stutter between
    /// two sheets.
    /// </summary>
    public string FacingSector => SectorNames[_facingSector];

    /// <summary>0..1 position in the run cycle, advanced by distance travelled.</summary>
    public float GaitPhase => _gaitPhase;

    /// <summary>Movement is fast enough to be shown as a run. Hysteretic.</summary>
    public bool IsRunning => _running;

    /// <summary>Moving against the way the body is pointed.</summary>
    public bool IsBackpedalling => _backpedalling;

    public Vector2 DashDirection => _dashDirection;
    public int Health { get; private set; } = GameBalance.PlayerMaxHealth;
    public float Radius => GameBalance.PlayerRadius;
    public float InvulnerabilityRemaining { get; private set; }
    public float HitFlashRemaining { get; private set; }
    public float DashCooldownRemaining => _dashCooldownTimer;
    public bool IsDashing => _dashTimer > 0f;
    public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
    /// <summary>A downed Warden is not dead: his flame has guttered, not gone out.</summary>
    public bool IsDead => Health <= 0 && !IsDowned;
    public float Resonance { get; private set; }
    public bool IsResonanceReady => !ResonanceActive && Resonance >= GameBalance.ResonanceRequired;
    public bool ResonanceActive { get; private set; }
    public float ResonanceRemaining => _resonanceTimer;
    public float ResonanceActivationRemaining => _resonanceActivationTimer;
    public bool SoulSenseActive { get; private set; }

    /// <summary>
    /// Severance is a short, per-Player advantage opened by reading a committed
    /// enemy attack. It is deliberately owned here rather than in a shared combat
    /// service so a second Warden can hold their own window later.
    /// </summary>
    public bool SeveranceReady => _severanceTimer > 0f;
    public float SeveranceRemaining => _severanceTimer;
    public float SeveranceFlare => MathHelper.Clamp(_severanceFlareTimer / 0.28f, 0f, 1f);
    public bool DashStartedThisFrame { get; private set; }

    /// <summary>Set on the frame this Warden asked to spend the team's Resonance.</summary>
    public bool ResonanceRequestedThisFrame { get; private set; }

    /// <summary>
    /// Set in co-op when this Warden's flame has guttered but not gone out.
    /// Never true in solo, where death stays death.
    /// </summary>
    public bool IsDowned { get; private set; }

    /// <summary>Seconds left before a downed Warden's flame goes out for good.</summary>
    public float DownRemaining { get; private set; }

    /// <summary>0..1 hold progress made by the other brother.</summary>
    public float StabilizeProgress { get; private set; }

    /// <summary>True while this Warden is holding a brother's flame steady.</summary>
    public bool IsStabilizing { get; private set; }

    /// <summary>Enemies commit to Wardens that can still fight back.</summary>
    public bool CanBeTargeted => !IsDead && !IsDowned;

    /// <summary>A guttering Warden cannot be hit again; only the clock can finish him.</summary>
    public bool CanBeDamaged => !IsDead && !IsDowned;

    public ScytheCombat Scythe { get; } = new();
    public SoulCannon Cannon { get; } = new();

    public void OpenSeveranceWindow()
    {
        _severanceTimer = GameBalance.SeveranceWindowDuration;
        _severanceFlareTimer = 0.28f;
    }

    public void ConsumeSeveranceWindow()
    {
        _severanceTimer = 0f;
        _severanceFlareTimer = 0f;
    }

    public Player(Vector2 position, WardenIdentity identity = null!)
    {
        Position = position;
        Identity = identity ?? WardenIdentity.Younger;
        LightPhase = ReferenceEquals(Identity, WardenIdentity.Elder) ? 1.37f : 0f;
    }

    public void Reset(Vector2 position)
    {
        Position = position;
        Velocity = Vector2.Zero;
        FacingDirection = Vector2.UnitX;
        Health = GameBalance.PlayerMaxHealth;
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
        _severanceTimer = 0f;
        _severanceFlareTimer = 0f;
        DashStartedThisFrame = false;
        SoulSenseActive = false;
        IsDowned = false;
        DownRemaining = 0f;
        StabilizeProgress = 0f;
        IsStabilizing = false;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
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
        _severanceTimer = 0f;
        _severanceFlareTimer = 0f;
        SoulSenseActive = false;
        IsDowned = false;
        DownRemaining = 0f;
        StabilizeProgress = 0f;
        IsStabilizing = false;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    public void Update(
        float deltaTime,
        PlayerCommand command,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects,
        bool forceSoulSense = false,
        bool stabilizing = false)
    {
        _visualTime += deltaTime;
        DashStartedThisFrame = false;
        _resonanceActivationTimer = MathF.Max(0f, _resonanceActivationTimer - deltaTime);
        _severanceTimer = MathF.Max(0f, _severanceTimer - deltaTime);
        _severanceFlareTimer = MathF.Max(0f, _severanceFlareTimer - deltaTime);
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

        _dashCooldownTimer = MathF.Max(0f, _dashCooldownTimer - deltaTime);
        InvulnerabilityRemaining = MathF.Max(0f, InvulnerabilityRemaining - deltaTime);
        UpdateAfterimages(deltaTime);

        if (IsDowned)
        {
            // A guttering Warden cannot act, cannot be hit and cannot be reached
            // by any of his own systems. The clock is the only thing still running.
            DownRemaining = MathF.Max(0f, DownRemaining - deltaTime);
            ResonanceActive = false;
            _resonanceTimer = 0f;
            SoulSenseActive = false;
            Velocity = Vector2.Zero;
            Scythe.Reset();
            Cannon.Reset();
            return;
        }

        if (IsDead)
        {
            ResonanceActive = false;
            _resonanceTimer = 0f;
            SoulSenseActive = false;
            Velocity = Vector2.Zero;
            return;
        }

        // Resonance is a team decision now: the request is recorded here and the
        // world spends the shared pool, so one brother can never light it alone
        // while the other watches.
        ResonanceRequestedThisFrame = command.ResonancePressed && !stabilizing;

        SoulSenseActive = ResonanceActive || forceSoulSense || command.SenseHeld;

        Vector2 toAim = command.AimPoint - Position;
        if (toAim.LengthSquared() > 4f)
        {
            FacingDirection = Vector2.Normalize(toAim);
        }

        Vector2 movement = command.Move;

        // Holding a brother's flame steady takes both hands. Weapons are locked
        // and movement is halved, which is the danger window that keeps
        // stabilisation a real decision instead of a free reset.
        IsStabilizing = stabilizing;
        if (stabilizing)
        {
            Scythe.Reset();
            Cannon.Reset();
        }
        else
        {
            Cannon.Update(
                deltaTime,
                command,
                Position,
                FacingDirection,
                !IsDashing && Scythe.ActiveStep == 0,
                SoulSenseActive,
                particles,
                ResonanceActive);

            Scythe.Update(deltaTime, command, FacingDirection, Position, particles, !IsDashing && Cannon.CanUseScythe, ResonanceActive, SeveranceReady);
            if (Scythe.StartedThisFrame)
            {
                _attackImpulse = Scythe.AttackDirection * Scythe.GetForwardImpulse();
            }

            if (command.DashPressed && _dashCooldownTimer <= 0f && Scythe.ActiveStep == 0)
            {
                StartDash(movement, particles, screenEffects);
            }
        }

        if (_dashTimer > 0f)
        {
            UpdateDash(deltaTime, particles);
        }
        else
        {
            float movementMultiplier = SoulSenseActive && !ResonanceActive ? GameBalance.SoulSenseMovementMultiplier : 1f;
            movementMultiplier *= ResonanceActive ? GameBalance.ResonanceMovementMultiplier : 1f;
            movementMultiplier *= stabilizing ? GameBalance.StabilizeMoveMultiplier : Cannon.GetMovementMultiplier();
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

        UpdatePresentation(deltaTime, movement);

        _idleParticleTimer -= deltaTime;
        if (_idleParticleTimer <= 0f && !IsDashing)
        {
            _idleParticleTimer = 0.16f;
            particles.EmitDeathFlame(Position - FacingDirection * 2f, 1, 0.55f);
        }
    }

    private static readonly string[] SectorNames = ["e", "se", "s", "sw", "w", "nw", "n", "ne"];

    /// <summary>
    /// Body facing, sheet sector and gait. Presentation only.
    ///
    /// This exists because the old behaviour was to re-bucket the raw aim vector
    /// into one of eight sheets every single frame. Two things went wrong with
    /// that: the shoulders snapped instantly to the mouse, and a mouse sitting
    /// on a sector boundary flipped the sheet back and forth every frame. Both
    /// read as the character being broken rather than as him aiming.
    /// </summary>
    private void UpdatePresentation(float deltaTime, Vector2 movement)
    {
        // 1. Turn toward the aim at a bounded rate. Fast enough to feel
        //    responsive, slow enough that the turn is visible as a turn.
        float target = MathF.Atan2(FacingDirection.Y, FacingDirection.X);
        float delta = MathHelper.WrapAngle(target - _bodyAngle);
        float maxStep = (GameBalance.WardenTurnRate + MathF.Abs(delta) * GameBalance.WardenTurnAcceleration) * deltaTime;
        _bodyAngle = MathHelper.WrapAngle(_bodyAngle + MathHelper.Clamp(delta, -maxStep, maxStep));

        // 2. Pick the sheet with hysteresis around the sector we are already in.
        float sectorCenter = _facingSector * MathHelper.PiOver4;
        if (MathF.Abs(MathHelper.WrapAngle(_bodyAngle - sectorCenter)) > GameBalance.WardenSectorHold)
        {
            float degrees = MathHelper.ToDegrees(_bodyAngle);
            if (degrees < 0f)
            {
                degrees += 360f;
            }

            _facingSector = (int)MathF.Floor((degrees + 22.5f) / 45f) % 8;
        }

        // 3. Advance the run cycle by distance covered, so the gait is correct
        //    at every movement multiplier instead of only at full speed.
        float speed = Velocity.Length();
        _running = _running
            ? speed > GameBalance.WardenRunExitSpeed
            : speed > GameBalance.WardenRunEnterSpeed;

        if (_running)
        {
            _gaitPhase += speed * deltaTime / GameBalance.WardenGaitCycleDistance;
            _gaitPhase -= MathF.Floor(_gaitPhase);
        }
        else
        {
            // Settle to the contact pose rather than freezing mid-stride.
            _gaitPhase = 0f;
        }

        _backpedalling = _running
            && movement.LengthSquared() > 0.001f
            && Vector2.Dot(Vector2.Normalize(movement), BodyFacing) < -0.35f;
    }

    /// <summary>
    /// Called instead of death when a brother is still standing. The flame is not
    /// out; it has guttered, and it will go out on its own if nobody reaches it.
    /// </summary>
    public void Down()
    {
        if (IsDowned)
        {
            return;
        }

        IsDowned = true;
        DownRemaining = GameBalance.WardenDownDuration;
        StabilizeProgress = 0f;
        Velocity = Vector2.Zero;
        _attackImpulse = Vector2.Zero;
        _damageKnockback = Vector2.Zero;
        ResonanceActive = false;
        _resonanceTimer = 0f;
        _severanceTimer = 0f;
        _severanceFlareTimer = 0f;
        SoulSenseActive = false;
        _afterimages.Clear();
        Scythe.Reset();
        Cannon.Reset();
    }

    /// <summary>
    /// Progress made by a brother holding this Warden's flame steady. Returns true
    /// on the frame the hold completes.
    /// </summary>
    public bool AdvanceStabilization(float deltaTime, bool held, float requiredSeconds)
    {
        if (!IsDowned)
        {
            StabilizeProgress = 0f;
            return false;
        }

        if (!held)
        {
            // Interrupted progress bleeds away rather than vanishing, so being
            // driven off for a moment is a setback and not a restart.
            StabilizeProgress = MathF.Max(0f, StabilizeProgress - deltaTime * 0.55f);
            return false;
        }

        StabilizeProgress += deltaTime / MathF.Max(0.05f, requiredSeconds);
        if (StabilizeProgress < 1f)
        {
            return false;
        }

        StabilizeProgress = 0f;
        IsDowned = false;
        DownRemaining = 0f;
        Health = GameBalance.StabilizeRestoredHealth;
        // Bounded, not endless: one short grace so the Warden is not instantly
        // re-downed by the attack that is already in the air.
        InvulnerabilityRemaining = GameBalance.StabilizeGrace;
        HitFlashRemaining = 0.2f;
        return true;
    }

    /// <summary>The down timer ran out. The flame goes out for good.</summary>
    public void ExtinguishFlame()
    {
        IsDowned = false;
        DownRemaining = 0f;
        StabilizeProgress = 0f;
        Health = 0;
        InvulnerabilityRemaining = 0f;
    }

    /// <summary>
    /// Dash afterimages as soft residue rather than stamped silhouettes. Drawn in
    /// the additive combat-light pass with the rest of the Warden's flame.
    /// </summary>
    public void DrawAfterimages(SpriteBatch batch, Texture2D brush)
    {
        foreach (Afterimage afterimage in _afterimages)
        {
            float alpha = afterimage.Remaining / afterimage.Lifetime;
            SoftShapes.Streak(
                batch,
                brush,
                afterimage.Position,
                afterimage.Facing,
                30f,
                17f,
                Identity.Flame * (alpha * 0.24f));
            SoftShapes.Blob(batch, brush, afterimage.Position, 11f, Identity.FlameBright * (alpha * 0.2f));
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, ArtAssets art, bool debugVisible, float soulSenseAmount = 0f)
    {
        if (IsDead)
        {
            return;
        }

        // The directional body sheet already includes the stored cannon. All of
        // the Warden's Death Flame now lives in the additive combat-light pass;
        // nothing here strokes an outline over the pixel art.
        Scythe.Draw(batch, pixel, art.PhysicalScythe, Position, FacingDirection, debugVisible);
        Cannon.DrawActive(batch, pixel, art.SoulCannon, Position, FacingDirection);

        if (debugVisible)
        {
            batch.DrawCircle(pixel, Position, Radius, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, Position, Position + FacingDirection * 70f, new Color(80, 220, 210) * 0.8f, 2f);
        }
    }

    /// <summary>
    /// The Warden's own light: core, Soul Sense, Resonance, Severance, dash
    /// ignition and hit response. Drawn additively with the feathered brush so
    /// the character glows rather than being circled.
    /// </summary>
    public void DrawCombatLight(SpriteBatch batch, Texture2D brush, float soulSenseAmount)
    {
        if (IsDead)
        {
            float deathPulse = 0.5f + 0.5f * MathF.Sin(_visualTime * 5f);
            SoftShapes.Blob(batch, brush, Position, 46f + deathPulse * 10f, Identity.Flame * 0.26f);
            SoftShapes.Blob(batch, brush, Position, 17f + deathPulse * 4f, GameBalance.SoulWhite * 0.36f);
            return;
        }

        float pulse = 0.5f + 0.5f * MathF.Sin((_visualTime + LightPhase) * 4f);
        Vector2 core = Position + FacingDirection * 2f;
        bool coreReady = IsResonanceReady;
        Color flame = Identity.Flame;
        Color flameBright = Identity.FlameBright;

        // Bound Soul core. The authored sheet now carries a two-pixel ember at
        // the sternum, so this only has to make that ember breathe. At the old
        // radius it was a glowing egg laid over the Warden's chest and it cost
        // more readability than it bought.
        SoftShapes.Blob(batch, brush, core, (coreReady ? 13f : 9f) + pulse * 2f, flame * 0.2f);
        SoftShapes.Blob(batch, brush, core, (coreReady ? 4.5f : 3f) + pulse, flameBright * 0.3f);
        if (coreReady)
        {
            SoftShapes.Blob(batch, brush, core, 26f + pulse * 7f, flameBright * 0.1f);
        }

        // Soul Sense opens the Warden's sight forward.
        float sense = MathHelper.Clamp(soulSenseAmount, 0f, 1f);
        if (sense > 0.001f)
        {
            Vector2 eye = Position + FacingDirection * 24f;
            SoftShapes.Blob(batch, brush, eye, 20f, flame * (0.3f * sense));
            SoftShapes.Blob(batch, brush, eye, 8f, flameBright * (0.32f * sense));
        }

        if (ResonanceActive)
        {
            float flare = 0.5f + 0.5f * MathF.Sin((_visualTime + LightPhase) * 3.8f);
            SoftShapes.Blob(batch, brush, core, 62f + flare * 10f, flame * 0.16f);
            SoftShapes.Ring(batch, brush, Position, 34f + flare * 3f, 13f, flame * 0.1f, 16, _visualTime * 1.6f);
        }

        // Severance: the guttering flame draws taut and forward. Session 1 painted
        // a white blob over the chest here, which blew the Warden out to a
        // featureless flare exactly when the Player most needed to read his pose.
        // The gather is now off-body and directional; the edge does the talking.
        if (SeveranceReady)
        {
            float life = MathHelper.Clamp(_severanceTimer / GameBalance.SeveranceWindowDuration, 0f, 1f);
            float flare = SeveranceFlare;
            float taut = 0.5f + 0.5f * MathF.Sin(_visualTime * 15f);

            // Kept off the body. Session 2 moved the gather forward and halved
            // it; with the authored sheet in place it still swallowed the pose,
            // so the remaining wash is smaller again and sits ahead of him. The
            // Warden reading his own Severance must never cost him his outline.
            SoftShapes.Blob(batch, brush, Position + FacingDirection * 40f, 38f + flare * 16f,
                flameBright * (0.07f * life + flare * 0.07f));
            SoftShapes.Streak(batch, brush, Position + FacingDirection * 34f, FacingDirection,
                46f + taut * 10f, 10f, GameBalance.SoulWhite * (0.2f * life));
            SoftShapes.Streak(batch, brush, Position + FacingDirection * 58f, FacingDirection,
                26f + taut * 6f, 4.5f, Color.White * (0.16f * life));
        }

        Scythe.DrawTrail(batch, brush, Position);
        Cannon.DrawChargeLight(batch, brush, Position, FacingDirection);

        if (HitFlashRemaining > 0f)
        {
            float flash = MathHelper.Clamp(HitFlashRemaining / 0.14f, 0f, 1f);
            SoftShapes.Blob(batch, brush, Position, 44f * flash, GameBalance.SoulWhite * (0.28f * flash));
        }

        if (IsDashing)
        {
            Vector2 origin = Position - _dashDirection * 22f;
            SoftShapes.Streak(batch, brush, origin, _dashDirection, 42f, 17f, flame * 0.4f);
            SoftShapes.Streak(batch, brush, origin - _dashDirection * 8f, _dashDirection, 27f, 8f, flameBright * 0.3f);
        }
    }

    private void StartDash(Vector2 movement, ParticleSystem particles, ScreenEffects screenEffects)
    {
        DashStartedThisFrame = true;
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

    public void ApplyDamage(int damage, Vector2 knockback, ScreenEffects screenEffects)
    {
        if (IsDead || IsInvulnerable)
        {
            return;
        }

        Health = Math.Max(0, Health - damage);
        HitFlashRemaining = Health == 0 ? 0.24f : 0.14f;
        _damageKnockback += knockback;
        InvulnerabilityRemaining = 0.5f;
        screenEffects.BeginHitstop(Health == 0 ? 0.12f : 0.045f);
        screenEffects.AddShake(Health == 0 ? 0.28f : 0.12f, Health == 0 ? 9f : 5f);
        screenEffects.Flash(0.09f, Health == 0 ? 0.34f : 0.2f);
    }

    /// <summary>
    /// Mirrors the shared team pool onto this Warden so lighting, HUD and the
    /// Severance core read one number. Wardens no longer bank Resonance
    /// individually; see <see cref="Game.TeamResonance"/>.
    /// </summary>
    public void SyncResonance(float charge)
    {
        Resonance = MathHelper.Clamp(charge, 0f, GameBalance.ResonanceRequired);
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

    public void StartResonance()
    {
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

    /// <summary>
    /// Positional correction applied from outside combat: brother separation and
    /// the shared tether. Deliberately not a velocity change, so it can never
    /// fight the Warden's own movement or dash.
    /// </summary>
    public void Nudge(Vector2 offset, Rectangle bounds)
    {
        Position += offset;
        ClampTo(bounds);
    }

    private void ClampTo(Rectangle bounds)
    {
        Position = new Vector2(
            MathHelper.Clamp(Position.X, bounds.Left + Radius, bounds.Right - Radius),
            MathHelper.Clamp(Position.Y, bounds.Top + Radius, bounds.Bottom - Radius));
    }
}
