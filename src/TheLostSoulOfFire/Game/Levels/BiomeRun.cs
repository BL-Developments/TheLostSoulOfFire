using System;

namespace TheLostSoulOfFire.Game.Levels;

public enum BiomeRunState
{
    Homebase,
    InLevel,
    LevelEnd,
    Extracted,
    Defeated,
    BiomeComplete
}

/// <summary>
/// One run through a biome: which level is played, the run seed the levels derive from, and the
/// state of the run. Every transition checks its start state; a refused call changes nothing and
/// returns false. Pure state, so the run flow can be tested without MonoGame.
/// </summary>
public sealed class BiomeRun
{
    private const int LevelSeedStride = 7919;

    public BiomeDefinition Biome { get; private set; } = BiomeCatalog.One;

    /// <summary>The level being played, counted from 1.</summary>
    public int Level { get; private set; } = 1;

    public int RunSeed { get; private set; }

    public BiomeRunState State { get; private set; } = BiomeRunState.Homebase;

    /// <summary>Combat rooms entered in the whole run; carried from level to level, so encounters keep scaling.</summary>
    public int CombatRoomsEntered { get; private set; }

    public bool IsLastLevel => Level == Biome.LevelCount;

    /// <summary>Seed of the current level's layout. Plain arithmetic, so it stays the same between program starts.</summary>
    public int LevelSeed => unchecked(RunSeed * LevelSeedStride + Level);

    /// <summary>Begins a run in <paramref name="level"/>, from the homebase or after a run has ended.</summary>
    public bool Start(BiomeDefinition biome, int runSeed, int level = 1)
    {
        if (State is not (BiomeRunState.Homebase or BiomeRunState.Extracted or BiomeRunState.Defeated or BiomeRunState.BiomeComplete))
        {
            return false;
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, biome.LevelCount);

        Biome = biome;
        RunSeed = runSeed;
        Level = level;
        CombatRoomsEntered = 0;
        State = BiomeRunState.InLevel;
        return true;
    }

    /// <summary>The player reached the level end of a level that is not the last one; keeps the room progress.</summary>
    public bool ReachLevelEnd(int combatRoomsEntered)
    {
        if (State != BiomeRunState.InLevel || IsLastLevel)
        {
            return false;
        }

        CombatRoomsEntered = combatRoomsEntered;
        State = BiomeRunState.LevelEnd;
        return true;
    }

    /// <summary>Travel point: continue with the next level.</summary>
    public bool TravelOn()
    {
        if (State != BiomeRunState.LevelEnd || IsLastLevel)
        {
            return false;
        }

        Level++;
        State = BiomeRunState.InLevel;
        return true;
    }

    /// <summary>Travel point: extract to the homebase, which ends the run.</summary>
    public bool Extract()
    {
        if (State != BiomeRunState.LevelEnd)
        {
            return false;
        }

        State = BiomeRunState.Extracted;
        return true;
    }

    /// <summary>The player lost in a level; the run ends and the next one begins at level 1.</summary>
    public bool Defeat()
    {
        if (State != BiomeRunState.InLevel)
        {
            return false;
        }

        State = BiomeRunState.Defeated;
        return true;
    }

    /// <summary>The guardian room of the last level is cleared; the biome is complete.</summary>
    public bool CompleteBiome()
    {
        if (State != BiomeRunState.InLevel || !IsLastLevel)
        {
            return false;
        }

        State = BiomeRunState.BiomeComplete;
        return true;
    }
}
