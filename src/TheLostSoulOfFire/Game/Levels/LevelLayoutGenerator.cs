namespace TheLostSoulOfFire.Game.Levels;

/// <summary>
/// Builds a level's room sequence from a seed. Every room connects to all rooms of the next
/// stage, so forks and merges follow from the stage sizes and dead ends cannot occur.
/// </summary>
public static class LevelLayoutGenerator
{
    public static LevelLayout Generate(int seed, LevelLayoutSettings settings)
    {
        Random random = new(seed);
        int combatStages = random.Next(settings.CombatStagesMin, settings.CombatStagesMax + 1);
        int stageCount = combatStages + 2;

        // Roll all stage sizes first so the ids of the next stage are known when exits are built.
        int[] stageSizes = new int[stageCount];
        stageSizes[0] = 1;
        stageSizes[stageCount - 1] = 1;
        for (int stage = 1; stage <= combatStages; stage++)
        {
            stageSizes[stage] = random.NextDouble() < settings.ForkChance ? 2 : 1;
        }

        List<IReadOnlyList<LevelRoom>> stages = new(stageCount);
        int firstId = 0;
        for (int stage = 0; stage < stageCount; stage++)
        {
            int nextFirstId = firstId + stageSizes[stage];
            int[] exits = stage == stageCount - 1 ? [] : ExitIds(nextFirstId, stageSizes[stage + 1]);
            LevelRoom[] rooms = new LevelRoom[stageSizes[stage]];
            for (int index = 0; index < rooms.Length; index++)
            {
                rooms[index] = new LevelRoom(firstId + index, KindOf(stage, stageCount), stage, exits);
            }

            stages.Add(rooms);
            firstId = nextFirstId;
        }

        return new LevelLayout(seed, stages);
    }

    private static int[] ExitIds(int firstId, int count)
    {
        int[] ids = new int[count];
        for (int index = 0; index < count; index++)
        {
            ids[index] = firstId + index;
        }

        return ids;
    }

    private static LevelRoomKind KindOf(int stage, int stageCount)
    {
        if (stage == 0)
        {
            return LevelRoomKind.Start;
        }

        return stage == stageCount - 1 ? LevelRoomKind.LevelEnd : LevelRoomKind.Combat;
    }
}
