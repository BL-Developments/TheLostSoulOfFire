using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// What the camera is being asked to watch this frame.
/// </summary>
/// <param name="Center">The Warden, or the centroid of both brothers.</param>
/// <param name="Velocity">Their movement, used to look ahead.</param>
/// <param name="Facing">Where they are looking, used to look ahead a little more.</param>
/// <param name="Threat">Centroid of the fight worth acknowledging, if there is one.</param>
/// <param name="SmoothTime">Seconds for the camera to close most of the distance.</param>
/// <param name="Lead">0 disables look-ahead entirely; scripted states use that.</param>
public readonly record struct CameraFocus(
    Vector2 Center,
    Vector2 Velocity,
    Vector2 Facing,
    Vector2? Threat,
    float SmoothTime,
    float Lead = 1f);

public sealed class Camera2D
{
    // --- follow model -------------------------------------------------------
    // The old camera was a single lerp straight onto the Warden's position at a
    // fixed rate. That is why it felt mechanical: it sat exactly on him, it
    // reacted to every one-pixel adjustment, it never looked where he was going,
    // and it took direction changes as instantly as he did.
    //
    // What replaced it is the standard three ingredients of a good action-game
    // follow camera, and nothing more:
    //
    //   1. look-ahead   — the frame leads the movement, so the Player sees the
    //                     space he is running into rather than the space he has
    //                     already crossed;
    //   2. a soft zone  — small adjustments do not move the camera at all, so
    //                     the frame is allowed to be still;
    //   3. critical damping — the camera eases in and out and never overshoots,
    //                     instead of tracking with a constant, visible rate.

    /// <summary>How far ahead of the Warden the frame sits at full sprint.</summary>
    private const float VelocityLeadSeconds = 0.34f;
    private const float FacingLead = 46f;
    private const float MaxLead = 132f;

    /// <summary>
    /// Vertical restraint. A top-down frame that answers vertical movement as
    /// eagerly as horizontal movement reads as seasick, and the arena is wider
    /// than it is tall, so there is more to show sideways anyway.
    /// </summary>
    private const float VerticalResponse = 0.62f;

    /// <summary>Seconds for the look-ahead itself to settle. Deliberately slow.</summary>
    private const float LeadSmoothTime = 0.40f;

    /// <summary>Radius inside which the camera simply does not follow.</summary>
    private const float SoftZone = 26f;

    /// <summary>How much the camera acknowledges the fight rather than only the Warden.</summary>
    private const float ThreatWeight = 0.13f;
    private const float MaxThreatBias = 86f;

    private Vector2 _velocity;
    private Vector2 _lead;
    private Vector2 _leadVelocity;

    public Vector2 Position { get; private set; }
    public float Zoom { get; set; } = 1f;

    public Camera2D(Vector2 initialPosition)
    {
        Position = initialPosition;
    }

    /// <summary>Places the camera with no easing. Used on construction and reset.</summary>
    public void SnapTo(Vector2 target, Rectangle worldBounds, Viewport viewport)
    {
        Position = target;
        _velocity = Vector2.Zero;
        _lead = Vector2.Zero;
        _leadVelocity = Vector2.Zero;
        ClampToBounds(worldBounds, viewport);
    }

    public void Follow(CameraFocus focus, Rectangle worldBounds, Viewport viewport, float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        // 1. Look ahead. Velocity is the honest signal — it comes straight from
        //    the movement keys — so it carries most of the lead. Facing adds a
        //    little on top and is deliberately small: the mouse decides what is
        //    under the cursor, and a camera that chases the cursor hard would
        //    feed back into the aim it is chasing.
        Vector2 desiredLead = focus.Velocity * VelocityLeadSeconds;
        if (focus.Facing.LengthSquared() > 0.0001f)
        {
            desiredLead += Vector2.Normalize(focus.Facing) * FacingLead;
        }

        desiredLead.Y *= VerticalResponse;
        desiredLead *= MathHelper.Clamp(focus.Lead, 0f, 1f);
        if (desiredLead.LengthSquared() > MaxLead * MaxLead)
        {
            desiredLead = Vector2.Normalize(desiredLead) * MaxLead;
        }

        _lead = SmoothDamp(_lead, desiredLead, ref _leadVelocity, LeadSmoothTime, deltaTime);

        Vector2 target = focus.Center + _lead;

        // 2. Acknowledge the fight. Small on purpose: enough that a Devourer
        //    closing from off-frame pulls the eye toward it, never enough to
        //    take the frame away from the Warden.
        if (focus.Threat is { } threat)
        {
            Vector2 bias = (threat - focus.Center) * ThreatWeight;
            bias.Y *= VerticalResponse;
            if (bias.LengthSquared() > MaxThreatBias * MaxThreatBias)
            {
                bias = Vector2.Normalize(bias) * MaxThreatBias;
            }

            target += bias * MathHelper.Clamp(focus.Lead, 0f, 1f);
        }

        // 3. Soft zone, then critical damping. The zone is subtracted from the
        //    error rather than gating it, so the camera does not pop when the
        //    Warden crosses the boundary — it starts from zero and builds.
        Vector2 error = target - Position;
        float distance = error.Length();
        float zone = SoftZone * MathHelper.Clamp(focus.Lead, 0f, 1f);
        if (distance > 0.0001f && zone > 0f)
        {
            float eased = MathF.Max(0f, distance - zone);
            // Ramp back in over the first zone-width so the transition out of
            // the still frame is smooth rather than a step in velocity.
            eased *= MathHelper.Clamp(eased / zone, 0f, 1f) * 0.5f + 0.5f;
            target = Position + error / distance * eased;
        }

        Position = SmoothDamp(Position, target, ref _velocity, MathF.Max(0.02f, focus.SmoothTime), deltaTime);
        ClampToBounds(worldBounds, viewport);
    }

    /// <summary>
    /// Critically damped approach. Framerate independent and cannot overshoot,
    /// which is the difference between a camera that eases and a camera that
    /// visibly tracks at a constant rate.
    /// </summary>
    private static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 velocity, float smoothTime, float deltaTime)
    {
        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        Vector2 change = current - target;
        Vector2 temp = (velocity + change * omega) * deltaTime;
        velocity = (velocity - temp * omega) * exp;
        return target + (change + temp) * exp;
    }

    private void ClampToBounds(Rectangle worldBounds, Viewport viewport)
    {
        float halfWidth = viewport.Width / (2f * Zoom);
        float halfHeight = viewport.Height / (2f * Zoom);
        float minX = worldBounds.Left + halfWidth;
        float maxX = worldBounds.Right - halfWidth;
        float minY = worldBounds.Top + halfHeight;
        float maxY = worldBounds.Bottom - halfHeight;

        float clampedX = minX <= maxX ? MathHelper.Clamp(Position.X, minX, maxX) : worldBounds.Center.X;
        float clampedY = minY <= maxY ? MathHelper.Clamp(Position.Y, minY, maxY) : worldBounds.Center.Y;

        // Kill the damping velocity on any axis that is against the wall, so the
        // camera does not store up motion and lurch when it comes off it.
        if (MathF.Abs(clampedX - Position.X) > 0.0001f)
        {
            _velocity.X = 0f;
        }
        if (MathF.Abs(clampedY - Position.Y) > 0.0001f)
        {
            _velocity.Y = 0f;
        }

        Position = new Vector2(clampedX, clampedY);
    }

    public Matrix GetTransform(Viewport viewport, Vector2 shakeOffset) =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) *
        Matrix.CreateScale(Zoom, Zoom, 1f) *
        Matrix.CreateTranslation(viewport.Width * 0.5f + shakeOffset.X, viewport.Height * 0.5f + shakeOffset.Y, 0f);

    public Vector2 ScreenToWorld(Point screenPosition, Viewport viewport)
    {
        Matrix inverse = Matrix.Invert(GetTransform(viewport, Vector2.Zero));
        return Vector2.Transform(screenPosition.ToVector2(), inverse);
    }
}
