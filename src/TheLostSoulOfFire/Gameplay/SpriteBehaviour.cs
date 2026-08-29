using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Ecs;

namespace TheLostSoulOfFire.Gameplay;

public sealed class SpriteBehaviour : GameBehaviour
{
    private TransformBehaviour _transform = null!;
    private readonly Point _size;
    private readonly AtlasRegion _region;
    private readonly float _layer;

    public SpriteBehaviour(AtlasRegion region, int width, int height, float layer = 0.5f)
    {
        _region = region;
        _size = new Point(width, height);
        _layer = layer;
    }

    public Color Tint { get; set; } = Color.White;

    public override void Awake() =>
        _transform = Entity.GetComponent<TransformBehaviour>() ?? throw new InvalidOperationException("Sprite needs a transform.");

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Rectangle destination = CollisionMath.BoundsAt(_transform.Position, _size);
        spriteBatch.Draw(GameCore.Assets.Atlas, destination, GameCore.Assets.Region(_region), Tint, 0f, Vector2.Zero, SpriteEffects.None, _layer);
    }
}
