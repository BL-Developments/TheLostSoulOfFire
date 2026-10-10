using TheLostSoulOfFire.Game.Levels;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class LevelVisualSlotTests
{
    private static readonly VisualRegistry RoomOnlyRegistry = VisualRegistry.Parse("""
        { "version": 1, "visuals": [
          { "id": "environment.biome1-room", "kind": "environment", "palette": "world", "worldSize": [1800, 910],
            "clips": { "default": { "path": "E/room", "frameSize": [1800, 910], "frames": 1, "fps": 1, "loop": true } } }
        ] }
        """);

    [TestMethod]
    public void BiomeOne_UsesTheBiomeSlotIds()
    {
        BiomeDefinition biome = BiomeCatalog.One;

        Assert.AreEqual(VisualIds.Biome1Room, biome.RoomId);
        Assert.AreEqual(VisualIds.Biome1RoomWall, biome.WallId);
        Assert.AreEqual(VisualIds.Biome1Exit, biome.ExitId);
        Assert.AreEqual(VisualIds.GradeBiome1, biome.GradeId);
    }

    [TestMethod]
    public void Resolve_WithOnlyTheRoomRegistered_ShowsTheRoomAndDummiesTheRest()
    {
        BiomeDefinition biome = BiomeCatalog.One;

        ClipResolution room = VisualResolver.Resolve(RoomOnlyRegistry, biome.RoomId, VisualClips.Default);
        ClipResolution wall = VisualResolver.Resolve(RoomOnlyRegistry, biome.WallId, VisualClips.Default);
        ClipResolution exit = VisualResolver.Resolve(RoomOnlyRegistry, biome.ExitId, LevelExitClips.Closed);

        Assert.IsFalse(room.IsDummy);
        Assert.IsTrue(wall.IsDummy);
        Assert.IsTrue(exit.IsDummy);
    }
}
