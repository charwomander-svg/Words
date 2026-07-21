using Words.Core.Models;

namespace Words.Core.Services;

public static class CpuRaceResultFormatter
{
    public static string BuildCeremonyText(CpuRaceRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return string.Join(Environment.NewLine,
            "=== VS CPU RESULTS ===",
            $"Winner: {run.Winner}",
            $"Final score: You {run.PlayerSolved} - CPU {run.CpuSolved}",
            $"Target: {run.TargetWords} | Persona: {run.CpuPersona}",
            $"Elapsed: {run.ElapsedSeconds}s",
            $"Rating: {GetRating(run)}");
    }

    public static string BuildShareText(CpuRaceRun run) =>
        string.Join(Environment.NewLine,
            "Words Vs. CPU",
            BuildCeremonyText(run));

    private static string GetRating(CpuRaceRun run)
    {
        var margin = run.PlayerSolved - run.CpuSolved;
        if (run.Winner == "Player" && margin >= 3)
            return "Dominant";
        if (run.Winner == "Player")
            return "Clutch";
        if (margin >= -1)
            return "Photo Finish";
        return "Outpaced";
    }
}
