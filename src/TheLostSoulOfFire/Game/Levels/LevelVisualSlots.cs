using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game.Levels;

/// <summary>
/// Which of a biome's visual slots are painted (change <c>add-level-visual-slots</c>). A slot without a
/// registry entry is a planned placeholder drawn as grey box; it is checked here first because asking
/// the art for an unknown ID lists it as a missing visual.
/// </summary>
public static class LevelVisualSlots
{
    public static bool IsAssigned(VisualRegistry registry, string id) => registry.TryGet(id, out _);

    /// <summary>The biome's grade once the registry has one, otherwise the neutral grade.</summary>
    public static string GradeOf(VisualRegistry registry, BiomeDefinition biome) =>
        IsAssigned(registry, biome.GradeId) ? biome.GradeId : VisualIds.GradeNeutral;
}
