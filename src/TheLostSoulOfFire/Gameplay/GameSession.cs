namespace TheLostSoulOfFire.Gameplay;

public sealed class ProfileState
{
}

public sealed class RunState
{
    public RunState(int sequence) => Sequence = sequence;
    public int Sequence { get; }
}

public sealed class LevelState
{
    public LevelState(string levelName, int requiredEnemies)
    {
        LevelName = levelName;
        RemainingRequiredEnemies = requiredEnemies;
    }

    public string LevelName { get; }
    public int RemainingRequiredEnemies { get; private set; }
    public bool IsComplete => RemainingRequiredEnemies == 0;

    public void EnemyDefeated()
    {
        if (RemainingRequiredEnemies > 0) RemainingRequiredEnemies--;
    }
}

public sealed class GameSession
{
    private int _runSequence;

    public ProfileState Profile { get; } = new();
    public RunState? CurrentRun { get; private set; }

    public RunState StartRun()
    {
        CurrentRun = new RunState(++_runSequence);
        return CurrentRun;
    }

    public void EndRun() => CurrentRun = null;
}
