using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Levels;

public sealed class LevelRuntime
{
    public LevelRuntime(
        string name,
        Rectangle bounds,
        Vector2 playerSpawn,
        Rectangle[] walls,
        Rectangle portal,
        Rectangle altar,
        Vector2[] enemySpawns)
    {
        Name = name;
        Bounds = bounds;
        PlayerSpawn = playerSpawn;
        Walls = walls;
        Portal = portal;
        Altar = altar;
        EnemySpawns = enemySpawns;
    }

    public string Name { get; }
    public Rectangle Bounds { get; }
    public Vector2 PlayerSpawn { get; }
    public Rectangle[] Walls { get; }
    public Rectangle Portal { get; }
    public Rectangle Altar { get; }
    public Vector2[] EnemySpawns { get; }
}

public static class LevelFactory
{
    public static LevelRuntime Create(LevelDefinition definition)
    {
        Rectangle[] walls = new Rectangle[definition.Walls.Count];
        for (int i = 0; i < walls.Length; i++) walls[i] = definition.Walls[i].ToRectangle();
        Vector2[] enemySpawns = new Vector2[definition.EnemySpawns.Count];
        for (int i = 0; i < enemySpawns.Length; i++) enemySpawns[i] = definition.EnemySpawns[i].ToVector2();
        return new LevelRuntime(
            definition.Name,
            definition.Bounds.ToRectangle(),
            definition.PlayerSpawn.ToVector2(),
            walls,
            definition.Portal.ToRectangle(),
            definition.Altar.ToRectangle(),
            enemySpawns);
    }
}
