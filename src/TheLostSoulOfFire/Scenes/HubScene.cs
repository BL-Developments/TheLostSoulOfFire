using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Ecs;
using TheLostSoulOfFire.Gameplay;
using TheLostSoulOfFire.Levels;
using TheLostSoulOfFire.Presentation;

namespace TheLostSoulOfFire.Scenes;

public sealed class HubScene : Scene
{
    private const string Title = "EMBER SANCTUM";
    private const string Hint = "E - FIRE-RAID STARTEN";
    private readonly GameSession _session;
    private readonly Camera2D _camera = new();
    private GameWorld _world = null!;
    private LevelRuntime _level = null!;
    private GameEntity _player = null!;
    private HitboxBehaviour _playerHitbox = null!;
    private FireEffects _fireEffects = null!;
    private bool _canEnterRaid;

    public HubScene(GameSession session)
    {
        _session = session;
    }

    protected override void LoadContent()
    {
        _level = LevelFactory.Create(LevelDefinitionLoader.Load("Hub.json"));
        _world = new GameWorld();
        _player = _world.CreateEntity("Player", _level.PlayerSpawn);
        _playerHitbox = new HitboxBehaviour(28, 28);
        _player.Add(_playerHitbox)
            .Add(new SpriteBehaviour(AtlasRegion.Player, 58, 76, 0.42f))
            .Add(new PlayerMovementBehaviour(_level));
        Vector2 portalCenter = new(_level.Portal.Center.X, _level.Portal.Center.Y);
        _fireEffects = new FireEffects("HubPortal", portalCenter);
        _camera.Follow(_player.Transform.Position, GameCore.Resolution.VirtualWidth, GameCore.Resolution.VirtualHeight, _level.Bounds);
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime);
        _camera.Follow(_player.Transform.Position, GameCore.Resolution.VirtualWidth, GameCore.Resolution.VirtualHeight, _level.Bounds);
        _canEnterRaid = _level.Portal.Intersects(_playerHitbox.Bounds);
        _fireEffects.Update(gameTime);
        if (_canEnterRaid && GameCore.Input.IsKeyPressed(Keys.E))
        {
            _session.StartRun();
            GameCore.Scenes.RequestChange(new RaidScene(_session));
        }
    }

    public override void Draw(GameTime gameTime)
    {
        SpriteBatch spriteBatch = GameCore.SpriteBatch;
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.Matrix);
        SceneRenderer.DrawLevel(spriteBatch, _level, AtlasRegion.HubFloor);
        SceneRenderer.DrawAtlas(spriteBatch, AtlasRegion.Portal, _level.Portal, 0.62f, Color.White);
        _world.Draw(gameTime, spriteBatch);
        spriteBatch.End();
        _fireEffects.Draw(spriteBatch, _camera.Matrix);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        SceneRenderer.DrawCenteredText(spriteBatch, Title, 20f, Palette.Flame);
        SceneRenderer.DrawCenteredText(spriteBatch, "Letzte Zuflucht einer verlorenen Feuerseele", 48f, Palette.Ash);
        if (_canEnterRaid) SceneRenderer.DrawCenteredText(spriteBatch, Hint, 486f, Palette.FlameCore);
        spriteBatch.End();
    }

    protected override void DisposeManaged()
    {
        _fireEffects.Dispose();
        _world.Dispose();
    }
}
