using Words.Core.Models;

namespace Words.Core.Services;

public static class RoyaleResultFormatter
{
    public static string BuildShareText(RoyaleRun run, string entrantName)
    {
        ArgumentNullException.ThrowIfNull(run);
        var entrant = run.Entrants.FirstOrDefault(value => value.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");
        var winner = run.Winner is null ? "TBD" : run.Winner.Name;
        var lines = new List<string>
        {
            "Words Royale",
            $"Placement: #{run.GetPlacement(entrantName)} / {run.Entrants.Count}",
            $"Phase: {run.Phase}",
            $"Solved: {entrant.WordsSolved}",
            $"Failed: {entrant.WordsFailed}",
            $"Time left: {entrant.SecondsRemaining}s",
            $"Alive: {run.AliveCount}",
            $"Winner: {winner}",
            "Final table:"
        };

        lines.AddRange(BuildFinalTableLines(run, count: 5));
        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildCeremonyText(RoyaleRun run, string entrantName)
    {
        ArgumentNullException.ThrowIfNull(run);
        var entrant = run.Entrants.FirstOrDefault(value => value.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");
        var placement = run.GetPlacement(entrantName);
        var winner = run.Winner is null ? "TBD" : FormatEntrant(run.Winner);
        var result = placement == 1 ? "VICTORY" : placement <= 10 ? "FINAL TABLE" : "ELIMINATED";
        var lines = new List<string>
        {
            $"WORDS ROYALE {result}",
            "=== Player Card ===",
            $"Placement: #{placement}/{run.Entrants.Count}",
            $"Solved: {entrant.WordsSolved} | Failed: {entrant.WordsFailed} | Time: {entrant.SecondsRemaining}s",
            $"Winner: {winner}",
            $"Phase: {run.Phase} - {run.PressureMessage}",
            "=== Podium ==="
        };

        lines.AddRange(BuildPodiumLines(run, count: 3));
        lines.Add("=== Final Table ===");
        lines.AddRange(BuildFinalTableLines(run, count: 10));
        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildLiveDramaText(RoyaleRun run, string entrantName)
    {
        ArgumentNullException.ThrowIfNull(run);
        var rival = run.GetNearestRival(entrantName);
        var lines = new List<string>
        {
            $"{run.Phase}: {run.PressureMessage}",
            $"{run.AliveCount} alive | rank #{run.GetRank(entrantName)} | elimination zone {run.GetEliminationZoneCount()}",
            $"Leaders: {FormatEntrants(run.GetLeaders(3))}",
            $"Danger: {FormatEntrants(run.GetThreats(3))}",
            $"Rival: {(rival is null ? "none" : FormatEntrant(rival))}",
            $"Surging: {FormatEntrants(run.GetSurgingBots(2))}",
            $"Collapsing: {FormatEntrants(run.GetCollapsingBots(2))}",
            $"Bubble: {FormatEntrants(run.GetPlayerBubble(entrantName, 1))}"
        };

        return string.Join(Environment.NewLine, lines);
    }

    public static string FormatEntrant(RoyaleEntrant entrant) =>
        entrant.IsBot
            ? $"{entrant.Name} ({entrant.Persona}, {entrant.WordsSolved} solved, {entrant.SecondsRemaining}s)"
            : $"{entrant.Name} ({entrant.WordsSolved} solved, {entrant.SecondsRemaining}s)";

    private static string FormatEntrants(IEnumerable<RoyaleEntrant> entrants)
    {
        var values = entrants.Select(FormatEntrant).ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }

    private static IEnumerable<string> BuildFinalTableLines(RoyaleRun run, int count) =>
        run.GetFinalTable(count)
            .Select(entrant =>
            {
                var placement = entrant.Placement ?? (run.Winner == entrant ? 1 : run.GetRank(entrant.Name));
                return $"#{placement} {FormatEntrant(entrant)}";
            });

    private static IEnumerable<string> BuildPodiumLines(RoyaleRun run, int count) =>
        run.GetFinalTable(count)
            .Select(entrant =>
            {
                var placement = entrant.Placement ?? (run.Winner == entrant ? 1 : run.GetRank(entrant.Name));
                var medal = placement switch
                {
                    1 => "[GOLD]",
                    2 => "[SILVER]",
                    3 => "[BRONZE]",
                    _ => $"#{placement}"
                };
                return $"{medal} {FormatEntrant(entrant)}";
            });
}
