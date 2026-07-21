namespace Words.Core.Models;

public sealed record RunSummary(
    int RoundsWon,
    int RoundsPerRun,
    int BestStreak,
    int StreakBonusPoints
)
{
    public int WinRatePercent => RoundsPerRun == 0
        ? 0
        : (int)Math.Round((double)RoundsWon / RoundsPerRun * 100);

    public bool IsWinningRun => RoundsWon * 2 >= RoundsPerRun;

    public bool IsPerfectRun => RoundsPerRun > 0 && RoundsWon == RoundsPerRun;

    public RunGrade Grade
    {
        get
        {
            if (IsPerfectRun)
                return RunGrade.Perfect;
            if (WinRatePercent >= 75)
                return RunGrade.Gold;
            if (WinRatePercent >= 50)
                return RunGrade.Silver;
            return RunGrade.Bronze;
        }
    }
}
