using Words.Core.Models;

namespace Words.Core.Services;

public static class TimeAttackResultFormatter
{
    public static string BuildCeremonyText(TimeAttackRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var highlights = new List<string>();
        if (run.BestCombo >= 10)
            highlights.Add("Inferno combo");
        else if (run.BestCombo >= 5)
            highlights.Add("Combo streak");
        if (run.WordsSolved >= 15)
            highlights.Add("Word rush");
        if (run.Score >= 10000)
            highlights.Add("Score spike");
        if (highlights.Count == 0)
            highlights.Add("Keep the chain alive");

        return string.Join(Environment.NewLine,
            "=== TIME ATTACK RESULTS ===",
            $"Score: {run.Score}",
            $"Solved: {run.WordsSolved} | Failed: {run.WordsFailed}",
            $"Best combo: x{run.BestCombo} | Time left: {run.SecondsRemaining}s",
            GetRating(run),
            $"Highlights: {string.Join(", ", highlights)}");
    }

    public static string BuildSummaryText(TimeAttackRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return string.Join(Environment.NewLine,
            "TIME ATTACK COMPLETE",
            $"Score: {run.Score}",
            $"Solved: {run.WordsSolved} | Failed: {run.WordsFailed}",
            $"Best combo: x{run.BestCombo}",
            $"Time left: {run.SecondsRemaining}s",
            GetRating(run));
    }

    public static string BuildShareText(TimeAttackRun run) =>
        string.Join(Environment.NewLine,
            "Words Time Attack",
            BuildSummaryText(run));

    public static string GetRating(TimeAttackRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (run.WordsSolved >= 25 || run.BestCombo >= 15)
            return "Rating: Inferno";
        if (run.WordsSolved >= 15 || run.BestCombo >= 10)
            return "Rating: Blazing";
        if (run.WordsSolved >= 8 || run.BestCombo >= 5)
            return "Rating: Hot Streak";
        if (run.WordsSolved > 0)
            return "Rating: Warmup";
        return "Rating: Cold Start";
    }
}
