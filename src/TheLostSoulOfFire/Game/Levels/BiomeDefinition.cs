namespace TheLostSoulOfFire.Game.Levels;

/// <summary>A biome: the number of levels a run passes through and how each level is laid out.</summary>
public sealed record BiomeDefinition(int Number, string Numeral, int LevelCount, LevelLayoutSettings Layout);

/// <summary>The biomes that exist so far. Only biome I is playable; its levels share one layout.</summary>
public static class BiomeCatalog
{
    public static BiomeDefinition One { get; } = new(1, "I", 3, LevelLayoutSettings.Default);

    public static bool TryGet(int number, out BiomeDefinition biome)
    {
        biome = One;
        return number == One.Number;
    }
}
