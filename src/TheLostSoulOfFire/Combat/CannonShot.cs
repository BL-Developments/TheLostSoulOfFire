using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Combat;

public sealed class CannonShot
{
    public Vector2 Position { get; private set; }
    public Vector2 PreviousPosition { get; private set; }
    public Vector2 Direction { get; }
    public float Charge { get; }
    public bool IsFullCharge { get; }
    public bool SoulSenseAtFire { get; }
    public int Damage { get; }
    public float Radius { get; }
    public bool IsFinished { get; private set; }
    private float _remaining = GameBalance.CannonProjectileLifetime;

    public CannonShot(Vector2 position, CannonShotRequest request)
    {
        Position = position;
        PreviousPosition = position;
        Direction = request.Direction;
        Charge = request.Charge;
        IsFullCharge = request.IsFullCharge;
        SoulSenseAtFire = request.SoulSenseAtFire;
        Damage = request.Damage;
        Radius = request.Radius;
    }

    public void Update(float deltaTime, Rectangle bounds)
    {
        PreviousPosition = Position;
        Position += Direction * GameBalance.CannonProjectileSpeed * deltaTime;
        _remaining -= deltaTime;
        IsFinished = IsFinished || _remaining <= 0f || !bounds.Contains(Position.ToPoint());
    }

    public void MarkHit() => IsFinished = true;

    public void Draw(SpriteBatch batch, Texture2D pixel)
    {
        if (IsFinished)
        {
            return;
        }

        Color glow = IsFullCharge ? GameBalance.SoulWhite : GameBalance.DeathFlameBright;
        batch.DrawLine(pixel, Position - Direction * (24f + Charge * 42f), Position, GameBalance.DeepViolet * 0.75f, Radius * 1.8f);
        batch.DrawLine(pixel, Position - Direction * (18f + Charge * 34f), Position, glow * 0.9f, Radius * 0.72f);
        batch.FillCircle(pixel, Position, Radius, GameBalance.DeathFlame * 0.88f);
        batch.FillCircle(pixel, Position, Radius * 0.48f, glow);
    }
}
