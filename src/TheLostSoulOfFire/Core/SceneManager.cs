using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Core;

public sealed class SceneManager : IDisposable
{
    private Scene? _active;
    private Scene? _pending;

    public SceneManager(IServiceProvider services) { }

    public Scene? Active => _active;

    public void RequestChange(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _pending?.Dispose();
        _pending = scene;
    }

    public void ApplyPendingChange()
    {
        if (_pending is null) return;
        _active?.Dispose();
        _active = _pending;
        _pending = null;
        _active.Initialize();
        GameCore.Instance.ResetElapsedTime();
    }

    public void Update(GameTime gameTime) => _active?.Update(gameTime);
    public void Draw(GameTime gameTime) => _active?.Draw(gameTime);

    public void Dispose()
    {
        _pending?.Dispose();
        _active?.Dispose();
        _pending = null;
        _active = null;
    }
}
