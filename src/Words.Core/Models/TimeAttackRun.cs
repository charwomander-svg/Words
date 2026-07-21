namespace Words.Core.Models;

public sealed class TimeAttackRun
{
    public const int DefaultStartingSeconds = 180;
    public const int DefaultSolveBonusSeconds = 15;
    public const int DefaultFailurePenaltySeconds = 30;

    public TimeAttackRun(
        int startingSeconds = DefaultStartingSeconds,
        int solveBonusSeconds = DefaultSolveBonusSeconds,
        int failurePenaltySeconds = DefaultFailurePenaltySeconds)
    {
        if (startingSeconds < 1)
            throw new ArgumentOutOfRangeException(nameof(startingSeconds), "Starting time must be at least 1 second.");
        if (solveBonusSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(solveBonusSeconds), "Solve bonus cannot be negative.");
        if (failurePenaltySeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(failurePenaltySeconds), "Failure penalty cannot be negative.");

        SecondsRemaining = startingSeconds;
        SolveBonusSeconds = solveBonusSeconds;
        FailurePenaltySeconds = failurePenaltySeconds;
    }

    public int SecondsRemaining { get; private set; }
    public int SolveBonusSeconds { get; }
    public int FailurePenaltySeconds { get; }
    public int WordsSolved { get; private set; }
    public int WordsFailed { get; private set; }
    public int Score { get; private set; }
    public int CurrentCombo { get; private set; }
    public int BestCombo { get; private set; }
    public int LastScoreAwarded { get; private set; }
    public int LastComboBonus { get; private set; }
    public bool IsOver => SecondsRemaining <= 0;

    public void Tick(int seconds = 1)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Tick must be at least 1 second.");
        if (IsOver)
            return;

        SecondsRemaining = Math.Max(0, SecondsRemaining - seconds);
    }

    public void RecordSolved(int score)
    {
        if (score < 0)
            throw new ArgumentOutOfRangeException(nameof(score), "Score cannot be negative.");
        if (IsOver)
            return;

        CurrentCombo++;
        BestCombo = Math.Max(BestCombo, CurrentCombo);
        LastComboBonus = score * Math.Max(0, CurrentCombo - 1) / 5;
        LastScoreAwarded = score + LastComboBonus;
        WordsSolved++;
        Score += LastScoreAwarded;
        SecondsRemaining += SolveBonusSeconds;
    }

    public void AddSeconds(int seconds)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds must be at least 1.");
        if (IsOver)
            return;

        SecondsRemaining += seconds;
    }

    public void RecordFailed()
    {
        if (IsOver)
            return;

        WordsFailed++;
        CurrentCombo = 0;
        LastScoreAwarded = 0;
        LastComboBonus = 0;
        SecondsRemaining = Math.Max(0, SecondsRemaining - FailurePenaltySeconds);
    }
}
