using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game.Levels;

/// <summary>Placeholder colours of a biome's grey-box rooms (change <c>add-level-visual-slots</c>).</summary>
public sealed record LevelPalette(Color Floor, Color Grid, Color Wall, Color ExitLight);

/// <summary>
/// A biome: the number of levels a run passes through, how each level is laid out and which
/// Visual-IDs its rooms are drawn with. A slot without painted art is drawn in <paramref name="Palette"/>.
/// </summary>
public sealed record BiomeDefinition(
    int Number,
    string Numeral,
    int LevelCount,
    LevelLayoutSettings Layout,
    LevelPalette Palette,
    string RoomId,
    string WallId,
    string ExitId,
    string GradeId);

/// <summary>The biomes that exist so far. Only biome I is playable; its levels share one layout.</summary>
public static class BiomeCatalog
{
    // A pale, cool floor stands apart from the dark, violet hub, prologue and arena, and keeps
    // enemies and flame colours readable. Working values, to be checked against a hub capture.
    public static BiomeDefinition One { get; } = new(
        1,
        "I",
        3,
        LevelLayoutSettings.Default,
        new LevelPalette(new Color(178, 184, 190), new Color(132, 138, 146), new Color(26, 31, 44), new Color(236, 244, 255)),
        VisualIds.Biome1Room,
        VisualIds.Biome1RoomWall,
        VisualIds.Biome1Exit,
        VisualIds.GradeBiome1);

    public static bool TryGet(int number, out BiomeDefinition biome)
    {
        biome = One;
        return number == One.Number;
    }
}
