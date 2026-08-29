using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace TheLostSoulOfFire.Core;

public abstract class Scene : IDisposable
{
    protected Scene()
    {
        Content = new ContentManager(GameCore.Instance.Services, "Content");
    }

    protected ContentManager Content { get; }
    public bool IsDisposed { get; private set; }

    public void Initialize()
    {
        PreInitialize();
        LoadContent();
        PostInitialize();
    }

    protected virtual void PreInitialize() { }
    protected virtual void LoadContent() { }
    protected virtual void PostInitialize() { }
    public virtual void Update(GameTime gameTime) { }
    public virtual void Draw(GameTime gameTime) { }

    public void Dispose()
    {
        if (IsDisposed) return;
        Content.Unload();
        Content.Dispose();
        DisposeManaged();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeManaged() { }
}
