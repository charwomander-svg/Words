using Words.Core.Models;

namespace Words.Core.Services;

/// <summary>
/// Tracks a multi-round run so UI layers can share progression behavior.
/// </summary>
public sealed class RunProgressService
{
    private static readonly WordCategory[] CategoryOrder = Enum.GetValues<WordCategory>();
    private GameDifficulty _baseDifficulty = GameDifficulty.Easy;
    private WordCategory _baseCategory = WordCategory.General;

    public bool IsActive { get; private set; }
    public bool AutoDifficultyRamp { get; private set; }
    public bool RotateCategories { get; private set; }
    public int RoundsPerRun { get; private set; }
    public int RoundsPlayed { get; private set; }
    public int RoundsWon { get; private set; }
    public int CurrentStreak { get; private set; }
    public int BestStreak { get; private set; }
    public int StreakBonusPoints { get; private set; }
    public int NextRoundNumber => IsActive ? RoundsPlayed + 1 : 0;

    public void StartRun(
        int roundsPerRun,
        GameDifficulty baseDifficulty,
        WordCategory baseCategory,
        bool autoDifficultyRamp,
        bool rotateCategories
    )
    {
        if (roundsPerRun < 1)
            throw new ArgumentOutOfRangeException(nameof(roundsPerRun), "Rounds per run must be at least 1.");

        IsActive = true;
        AutoDifficultyRamp = autoDifficultyRamp;
        RotateCategories = rotateCategories;
        RoundsPerRun = roundsPerRun;
        RoundsPlayed = 0;
        RoundsWon = 0;
        CurrentStreak = 0;
        BestStreak = 0;
        StreakBonusPoints = 0;
        _baseDifficulty = baseDifficulty;
        _baseCategory = baseCategory;
    }

    public GameDifficulty GetDifficultyForRound(int roundNumber)
    {
        if (roundNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(roundNumber), "Round number must be at least 1.");

        if (!AutoDifficultyRamp)
            return _baseDifficulty;

        return _baseDifficulty switch
        {
            GameDifficulty.Easy when roundNumber >= Math.Max(5, RoundsPerRun) => GameDifficulty.Hard,
            GameDifficulty.Easy when roundNumber >= Math.Max(3, (RoundsPerRun + 1) / 2) => GameDifficulty.Medium,
            GameDifficulty.Medium when roundNumber >= Math.Max(4, RoundsPerRun - 1) => GameDifficulty.Hard,
            _ => _baseDifficulty
        };
    }

    public WordCategory GetCategoryForRound(int roundNumber)
    {
        if (roundNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(roundNumber), "Round number must be at least 1.");
        if (!RotateCategories)
            return _baseCategory;

        var baseIndex = Array.IndexOf(CategoryOrder, _baseCategory);
        if (baseIndex < 0)
            return _baseCategory;

        var offset = (roundNumber - 1) % CategoryOrder.Length;
        return CategoryOrder[(baseIndex + offset) % CategoryOrder.Length];
    }

    public void RecordRoundOutcome(GameStatus roundStatus)
    {
        if (!IsActive)
            throw new InvalidOperationException("Run is not active.");
        if (RoundsPlayed >= RoundsPerRun)
            throw new InvalidOperationException("Run is already complete.");
        if (roundStatus == GameStatus.InProgress)
            throw new ArgumentOutOfRangeException(nameof(roundStatus), "Round outcome must be Won or Lost.");

        RoundsPlayed++;

        if (roundStatus == GameStatus.Won)
        {
            RoundsWon++;
            CurrentStreak++;
            BestStreak = Math.Max(BestStreak, CurrentStreak);
            StreakBonusPoints += CalculateStreakBonus(CurrentStreak);
        }
        else
        {
            CurrentStreak = 0;
        }

        if (RoundsPlayed >= RoundsPerRun)
            IsActive = false;
    }

    public RunSummary GetSummary() =>
        new(RoundsWon, RoundsPerRun, BestStreak, StreakBonusPoints);

    private static int CalculateStreakBonus(int currentStreak) =>
        currentStreak switch
        {
            >= 5 => 60,
            >= 3 => 35,
            >= 2 => 15,
            _ => 0
        };
}
