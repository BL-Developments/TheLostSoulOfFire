using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Ecs;

public sealed class TransformBehaviour : GameBehaviour
{
    public TransformBehaviour(Vector2 position)
    {
        Position = position;
    }

    public Vector2 Position;
}
