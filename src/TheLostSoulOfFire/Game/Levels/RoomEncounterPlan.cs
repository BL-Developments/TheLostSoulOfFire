using System;
using System.Collections.Generic;
using System.Linq;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Game.Levels;

/// <summary>
/// The waves of one combat room. The counts depend only on progress; the seed decides where the
/// heavy enemies land among the waves. Pure calculation, so the scaling is testable without MonoGame.
/// </summary>
public sealed record RoomEncounterPlan(IReadOnlyList<ArenaPush> Waves)
{
    public int TotalEnemies => Waves.Sum(wave => wave.Total);

    public int HeavyEnemies => Waves.Sum(wave => wave.Burning + wave.Devourer);

    public int Devourers => Waves.Sum(wave => wave.Devourer);

    /// <summary>The plan for a room at <paramref name="progress"/> (1 for the first combat room of a run).</summary>
    public static RoomEncounterPlan For(int progress, int seed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(progress, 1);

        int waveCount = WaveCount(progress);
        int enemiesPerWave = EnemiesPerWave(progress);
        int heavy = HeavyCount(progress, waveCount, enemiesPerWave);
        int devourers = DevourerCount(progress, heavy);

        // Every wave keeps at least one Hollow, so a wave holds at most enemiesPerWave - 1 heavy enemies.
        int[] heavyPerWave = new int[waveCount];
        int[] devourersPerWave = new int[waveCount];
        Random random = new(seed);
        for (int i = 0; i < heavy; i++)
        {
            int wave = random.Next(waveCount);
            while (heavyPerWave[wave] >= enemiesPerWave - 1)
            {
                wave = (wave + 1) % waveCount;
            }

            heavyPerWave[wave]++;
            if (i < devourers)
            {
                devourersPerWave[wave]++;
            }
        }

        List<ArenaPush> waves = new(waveCount);
        for (int wave = 0; wave < waveCount; wave++)
        {
            int devourer = devourersPerWave[wave];
            int burning = heavyPerWave[wave] - devourer;
            waves.Add(new ArenaPush(enemiesPerWave - heavyPerWave[wave], burning, devourer));
        }

        return new RoomEncounterPlan(waves);
    }

    private static int WaveCount(int progress) =>
        Math.Min(GameBalance.RoomWavesMax, 1 + (progress - 1) / GameBalance.RoomStagesPerExtraWave);

    private static int EnemiesPerWave(int progress) =>
        Math.Min(GameBalance.RoomEnemiesPerWaveMax,
            (int)MathF.Floor(GameBalance.RoomEnemiesPerWaveBase + (progress - 1) * GameBalance.RoomEnemiesPerWaveGrowth));

    private static int HeavyCount(int progress, int waveCount, int enemiesPerWave)
    {
        if (progress < GameBalance.RoomBurningFromProgress)
        {
            return 0;
        }

        int capacity = enemiesPerWave * waveCount - waveCount;
        int wanted = (progress - GameBalance.RoomBurningFromProgress + 1) * GameBalance.RoomHeavyPerProgress;
        return Math.Min(capacity, wanted);
    }

    private static int DevourerCount(int progress, int heavy)
    {
        if (progress < GameBalance.RoomDevourerFromProgress || heavy == 0)
        {
            return 0;
        }

        return Math.Min(heavy, Math.Max(1, (int)MathF.Floor(heavy * GameBalance.RoomDevourerShare)));
    }
}
