using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Ecs;

namespace TheLostSoulOfFire.Gameplay;

public sealed class PlayerAttackBehaviour : GameBehaviour
{
    private const float CooldownDuration = 0.24f;
    private readonly ProjectilePool _pool;
    private readonly Camera2D _camera;
    private readonly Action<Vector2> _onShot;
    private TransformBehaviour _transform = null!;
    private readonly AttackCadence _cadence = new(CooldownDuration);

    public PlayerAttackBehaviour(ProjectilePool pool, Camera2D camera, Action<Vector2> onShot)
    {
        _pool = pool;
        _camera = camera;
        _onShot = onShot;
    }

    public bool AcceptInput { get; set; } = true;
    public float CooldownRemaining => _cadence.Remaining;

    public override void Awake() =>
        _transform = Entity.GetComponent<TransformBehaviour>() ?? throw new InvalidOperationException("Attack needs a transform.");

    public override void Update(GameTime gameTime)
    {
        _cadence.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (!AcceptInput || !GameCore.Input.IsLeftMouseHeld || !_cadence.CanFire) return;
        Vector2 target = _camera.ScreenToWorld(GameCore.Input.MouseVirtualPosition);
        Vector2 direction = AimDirection.FromTo(_transform.Position, target);
        if (!_pool.TrySpawn(_transform.Position, direction)) return;
        _cadence.ConfirmShot();
        _onShot(_transform.Position);
    }
}

public sealed class AttackCadence
{
    private readonly float _duration;
    private float _remaining;

    public AttackCadence(float duration)
    {
        if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
        _duration = duration;
    }

    public float Remaining => Math.Max(0f, _remaining);
    public bool CanFire => _remaining <= 0f;

    public void Update(float deltaSeconds)
    {
        if (_remaining > 0f) _remaining -= Math.Max(0f, deltaSeconds);
    }

    public void ConfirmShot() => _remaining += _duration;
}

public static class AimDirection
{
    public static Vector2 FromTo(Vector2 origin, Vector2 target)
    {
        Vector2 direction = target - origin;
        if (direction.LengthSquared() > 0.0001f) direction.Normalize();
        return direction;
    }
}
