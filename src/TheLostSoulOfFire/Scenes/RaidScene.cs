using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Ecs;
using TheLostSoulOfFire.Gameplay;
using TheLostSoulOfFire.Levels;
using TheLostSoulOfFire.Presentation;

namespace TheLostSoulOfFire.Scenes;

public sealed class RaidScene : Scene
{
    private static readonly Vector2 HealthPosition = new(24f, 20f);
    private const string Title = "FIRE-RAID: CINDER VAULT";
    private const string AltarHint = "E - RAID ABSCHLIESSEN";
    private readonly GameSession _session;
    private readonly Camera2D _camera = new();
    private readonly List<EnemyActor> _enemies = new(8);
    private GameWorld _world = null!;
    private LevelRuntime _level = null!;
    private LevelState _levelState = null!;
    private GameEntity _player = null!;
    private HitboxBehaviour _playerHitbox = null!;
    private HealthBehaviour _playerHealth = null!;
    private SpriteBehaviour _playerSprite = null!;
    private PlayerMovementBehaviour _movement = null!;
    private PlayerAttackBehaviour _attack = null!;
    private ProjectilePool _projectiles = null!;
    private FireEffects _fireEffects = null!;
    private bool _canUseAltar;
    private bool _defeated;
    private float _defeatTimer;
    private string _healthText = string.Empty;
    private string _objectiveText = string.Empty;

    public RaidScene(GameSession session)
    {
        _session = session;
    }

    protected override void LoadContent()
    {
        if (_session.CurrentRun is null) throw new InvalidOperationException("A raid requires an active run.");
        _level = LevelFactory.Create(LevelDefinitionLoader.Load("FirstRaid.json"));
        _levelState = new LevelState(_level.Name, _level.EnemySpawns.Length);
        _world = new GameWorld();
        Vector2 altarCenter = new(_level.Altar.Center.X, _level.Altar.Center.Y);
        _fireEffects = new FireEffects("RaidFire", altarCenter) { ContinuousEnabled = false };
        _projectiles = new ProjectilePool(32, OnEnemyHit);
        _projectiles.InitializeRenderingData();

        _player = _world.CreateEntity("Player", _level.PlayerSpawn);
        _playerHitbox = new HitboxBehaviour(28, 28);
        _playerHealth = new HealthBehaviour(5, 0.65f);
        _playerSprite = new SpriteBehaviour(AtlasRegion.Player, 58, 76, 0.42f);
        _movement = new PlayerMovementBehaviour(_level);
        _attack = new PlayerAttackBehaviour(_projectiles, _camera, _fireEffects.TriggerBurst);
        _player.Add(_playerHitbox).Add(_playerHealth).Add(_playerSprite).Add(_movement).Add(_attack);

        for (int i = 0; i < _level.EnemySpawns.Length; i++) CreateEnemy(i, _level.EnemySpawns[i]);
        _camera.Follow(_player.Transform.Position, GameCore.Resolution.VirtualWidth, GameCore.Resolution.VirtualHeight, _level.Bounds);
        UpdateHudText();
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime);
        _projectiles.Update(gameTime, _level, _enemies);
        _fireEffects.Update(gameTime);

        if (!_defeated)
        {
            ApplyContactDamage();
            _canUseAltar = _levelState.IsComplete && _level.Altar.Intersects(_playerHitbox.Bounds);
            if (_canUseAltar && GameCore.Input.IsKeyPressed(Keys.E))
            {
                ReturnToHub();
                return;
            }

            if (_playerHealth.IsDead)
            {
                _defeated = true;
                _movement.AcceptInput = false;
                _attack.AcceptInput = false;
                _playerSprite.Tint = Palette.Locked;
                UpdateHudText();
            }
            else
            {
                _playerSprite.Tint = _playerHealth.WasHitThisFrame ? Palette.FlameCore : Color.White;
            }
        }
        else
        {
            _defeatTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_defeatTimer >= 1.25f)
            {
                ReturnToHub();
                return;
            }
        }

        _fireEffects.ContinuousEnabled = _levelState.IsComplete;
        _camera.Follow(_player.Transform.Position, GameCore.Resolution.VirtualWidth, GameCore.Resolution.VirtualHeight, _level.Bounds);
    }

    public override void Draw(GameTime gameTime)
    {
        SpriteBatch spriteBatch = GameCore.SpriteBatch;
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.Matrix);
        SceneRenderer.DrawLevel(spriteBatch, _level, AtlasRegion.RaidFloor);
        AtlasRegion altarRegion = _levelState.IsComplete ? AtlasRegion.AltarActive : AtlasRegion.AltarLocked;
        SceneRenderer.DrawAtlas(spriteBatch, altarRegion, _level.Altar, 0.62f, Color.White);
        _world.Draw(gameTime, spriteBatch);
        _projectiles.Draw(spriteBatch);
        spriteBatch.End();
        _fireEffects.Draw(spriteBatch, _camera.Matrix);

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        SceneRenderer.DrawCenteredText(spriteBatch, Title, 18f, Palette.Flame);
        spriteBatch.DrawString(GameCore.Assets.Font, _healthText, HealthPosition, Palette.FlameCore);
        SceneRenderer.DrawCenteredText(spriteBatch, _objectiveText, 48f, Palette.Ash);
        if (_canUseAltar) SceneRenderer.DrawCenteredText(spriteBatch, AltarHint, 486f, Palette.FlameCore);
        if (_defeated) SceneRenderer.DrawCenteredText(spriteBatch, "DIE FLAMME ERLISCHT ...", 250f, Palette.Ember);
        spriteBatch.End();
    }

    protected override void DisposeManaged()
    {
        _fireEffects.Dispose();
        _world.Dispose();
    }

    private void CreateEnemy(int index, Vector2 spawn)
    {
        GameEntity entity = _world.CreateEntity($"EmberHusk{index + 1}", spawn);
        HitboxBehaviour hitbox = new(34, 34);
        HealthBehaviour health = new(3);
        entity.Add(hitbox)
            .Add(health)
            .Add(new SpriteBehaviour(AtlasRegion.Enemy, 64, 72, 0.48f))
            .Add(new EnemyChaseBehaviour(_player.Transform, _level));
        _enemies.Add(new EnemyActor(entity, hitbox, health));
    }

    private void OnEnemyHit(EnemyActor enemy, Vector2 position)
    {
        if (!enemy.Health.TakeDamage(1)) return;
        _fireEffects.TriggerBurst(position);
        if (!enemy.Health.IsDead || enemy.Defeated) return;
        enemy.Defeated = true;
        _world.Destroy(enemy.Entity);
        _levelState.EnemyDefeated();
        UpdateHudText();
    }

    private void ApplyContactDamage()
    {
        Rectangle playerBounds = _playerHitbox.Bounds;
        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyActor enemy = _enemies[i];
            if (enemy.Defeated || !enemy.Entity.Active || !playerBounds.Intersects(enemy.Hitbox.Bounds)) continue;
            if (_playerHealth.TakeDamage(1))
            {
                _fireEffects.TriggerBurst(_player.Transform.Position);
                UpdateHudText();
            }
        }
    }

    private void UpdateHudText()
    {
        _healthText = $"FLAMME {_playerHealth.Health}/{_playerHealth.Maximum}";
        _objectiveText = _levelState.IsComplete
            ? "DER ALTAR BRENNT - KEHRE HEIM"
            : $"GLUTWESEN {_levelState.RemainingRequiredEnemies}";
    }

    private void ReturnToHub()
    {
        _session.EndRun();
        GameCore.Scenes.RequestChange(new HubScene(_session));
    }
}
