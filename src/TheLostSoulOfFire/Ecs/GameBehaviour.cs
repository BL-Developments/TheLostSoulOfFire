using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Ecs;

public abstract class GameBehaviour
{
    public GameEntity Entity { get; internal set; } = null!;
    public bool Enabled { get; set; } = true;
    public virtual void Awake() { }
    public virtual void Start() { }
    public virtual void Update(GameTime gameTime) { }
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
    public virtual void OnDestroy() { }
}
