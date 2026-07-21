namespace Words.Core.Models;

public sealed class RoyaleRun
{
    public const int DefaultEntrantCount = 99;
    public const int DefaultStartingSeconds = 90;
    public const int DefaultSolveBonusSeconds = 20;
    public const int DefaultFailurePenaltySeconds = 45;

    private readonly List<RoyaleEntrant> _entrants;
    private static readonly (string Name, string Persona)[] BotProfiles =
    [
        ("Lexi Lightning", "Sprinter"),
        ("Vowel Goblin", "Chaotic"),
        ("The Anagrammer", "Technical"),
        ("Queen Bee", "Steady"),
        ("Tile Tyrant", "Aggressive"),
        ("Silent Solver", "Patient"),
        ("Dictionary Dan", "Scholar"),
        ("Combo Witch", "Streaky"),
        ("Panic Panda", "Volatile"),
        ("Final Boss", "Elite")
    ];

    public RoyaleRun(
        IReadOnlyList<string> humanPlayers,
        int entrantCount = DefaultEntrantCount,
        int startingSeconds = DefaultStartingSeconds,
        int solveBonusSeconds = DefaultSolveBonusSeconds,
        int failurePenaltySeconds = DefaultFailurePenaltySeconds)
    {
        if (entrantCount < 2)
            throw new ArgumentOutOfRangeException(nameof(entrantCount), "Royale needs at least 2 entrants.");
        if (humanPlayers.Count > entrantCount)
            throw new ArgumentException("Human player count cannot exceed entrant count.", nameof(humanPlayers));
        if (startingSeconds < 1)
            throw new ArgumentOutOfRangeException(nameof(startingSeconds), "Starting time must be at least 1 second.");
        if (solveBonusSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(solveBonusSeconds), "Solve bonus cannot be negative.");
        if (failurePenaltySeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(failurePenaltySeconds), "Failure penalty cannot be negative.");

        SolveBonusSeconds = solveBonusSeconds;
        FailurePenaltySeconds = failurePenaltySeconds;
        _entrants = humanPlayers
            .Select(name => new RoyaleEntrant(name, isBot: false, startingSeconds))
            .ToList();

        for (var index = _entrants.Count + 1; index <= entrantCount; index++)
        {
            var profile = BotProfiles[(index - 1) % BotProfiles.Length];
            var cycle = ((index - 1) / BotProfiles.Length) + 1;
            _entrants.Add(new RoyaleEntrant($"{profile.Name} {cycle}", isBot: true, startingSeconds, profile.Persona));
        }
    }

    public IReadOnlyList<RoyaleEntrant> Entrants => _entrants;
    public int SolveBonusSeconds { get; }
    public int FailurePenaltySeconds { get; }
    public int AliveCount => _entrants.Count(entrant => !entrant.IsEliminated);
    public bool IsComplete => AliveCount == 1;
    public RoyaleEntrant? Winner => IsComplete
        ? _entrants.Single(entrant => !entrant.IsEliminated)
        : null;
    public string Phase => AliveCount switch
    {
        <= 1 => "Victory",
        <= 2 => "Duel",
        <= 10 => "Final 10",
        <= 25 => "Bubble",
        <= 50 => "Midgame",
        _ => "Opening"
    };
    public string PressureMessage => Phase switch
    {
        "Duel" => "Duel: every solve can decide the match.",
        "Final 10" => "Final 10: leaderboard pressure is peaking.",
        "Bubble" => "Bubble: survive the cut to reach the finale.",
        "Midgame" => "Midgame: timers are thinning the pack.",
        "Victory" => "Victory secured.",
        _ => "Opening: build time before the field collapses."
    };

    public void Tick(int seconds = 1)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Tick must be at least 1 second.");
        if (IsComplete)
            return;

        foreach (var entrant in _entrants)
            entrant.Tick(seconds);
        UpdatePlacements();
    }

    public void RecordSolved(string entrantName)
    {
        FindEntrant(entrantName).RecordSolved(SolveBonusSeconds);
        UpdatePlacements();
    }

    public void AddTime(string entrantName, int seconds)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds must be at least 1.");

        FindEntrant(entrantName).AddSeconds(seconds);
        UpdatePlacements();
    }

    public void ApplyPenalty(string entrantName, int seconds)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds must be at least 1.");

        FindEntrant(entrantName).RemoveSeconds(seconds);
        UpdatePlacements();
    }

    public IReadOnlyList<RoyaleEntrant> SabotageLeaders(string protectedEntrantName, int count = 5, int penaltySeconds = 10)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");
        if (penaltySeconds < 1)
            throw new ArgumentOutOfRangeException(nameof(penaltySeconds), "Penalty must be at least 1 second.");

        var targets = GetLeaders(_entrants.Count)
            .Where(entrant => !entrant.Name.Equals(protectedEntrantName, StringComparison.OrdinalIgnoreCase))
            .Take(count)
            .ToArray();
        foreach (var target in targets)
            target.RemoveSeconds(penaltySeconds);

        UpdatePlacements();
        return targets;
    }

    public void RecordFailed(string entrantName)
    {
        FindEntrant(entrantName).RecordFailed(FailurePenaltySeconds);
        UpdatePlacements();
    }

    public int GetPlacement(string entrantName)
    {
        var entrant = FindEntrant(entrantName);
        return entrant.Placement ?? AliveCount;
    }

    public int GetRank(string entrantName)
    {
        var ranked = GetRankedEntrants();
        var index = Array.FindIndex(ranked, entrant => entrant.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");

        return index + 1;
    }

    public int GetEliminationZoneCount(int secondsThreshold = 15)
    {
        if (secondsThreshold < 1)
            throw new ArgumentOutOfRangeException(nameof(secondsThreshold), "Threshold must be at least 1 second.");

        return _entrants.Count(entrant => !entrant.IsEliminated && entrant.SecondsRemaining <= secondsThreshold);
    }

    public IReadOnlyList<RoyaleEntrant> GetLeaders(int count = 5) =>
        _entrants
            .Where(entrant => !entrant.IsEliminated)
            .OrderByDescending(entrant => entrant.WordsSolved)
            .ThenByDescending(entrant => entrant.SecondsRemaining)
            .Take(count)
            .ToArray();

    public IReadOnlyList<RoyaleEntrant> GetThreats(int count = 5) =>
        _entrants
            .Where(entrant => !entrant.IsEliminated)
            .OrderBy(entrant => entrant.SecondsRemaining)
            .ThenBy(entrant => entrant.WordsSolved)
            .Take(count)
            .ToArray();

    public IReadOnlyList<RoyaleEntrant> GetSurgingBots(int count = 3)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");

        return _entrants
            .Where(entrant => entrant.IsBot && !entrant.IsEliminated)
            .OrderByDescending(entrant => entrant.WordsSolved)
            .ThenByDescending(entrant => entrant.SecondsRemaining)
            .Take(count)
            .ToArray();
    }

    public IReadOnlyList<RoyaleEntrant> GetCollapsingBots(int count = 3)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");

        return _entrants
            .Where(entrant => entrant.IsBot && !entrant.IsEliminated)
            .OrderBy(entrant => entrant.SecondsRemaining)
            .ThenByDescending(entrant => entrant.WordsFailed)
            .Take(count)
            .ToArray();
    }

    public RoyaleEntrant? GetNearestRival(string entrantName)
    {
        var ranked = GetRankedEntrants();
        var index = Array.FindIndex(ranked, entrant => entrant.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");

        return ranked
            .Where((entrant, entrantIndex) => entrantIndex != index && !entrant.IsEliminated)
            .OrderBy(entrant => Math.Abs(Array.IndexOf(ranked, entrant) - index))
            .ThenByDescending(entrant => entrant.WordsSolved)
            .FirstOrDefault();
    }

    public IReadOnlyList<RoyaleEntrant> GetPlayerBubble(string entrantName, int neighborsEachSide = 2)
    {
        if (neighborsEachSide < 0)
            throw new ArgumentOutOfRangeException(nameof(neighborsEachSide), "Neighbor count cannot be negative.");

        var ranked = GetRankedEntrants();
        var index = Array.FindIndex(ranked, entrant => entrant.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");

        var start = Math.Max(0, index - neighborsEachSide);
        var end = Math.Min(ranked.Length - 1, index + neighborsEachSide);
        return ranked[start..(end + 1)];
    }

    public IReadOnlyList<RoyaleEntrant> GetFinalTable(int count = 10)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");

        return _entrants
            .OrderBy(entrant => entrant.Placement ?? 1)
            .ThenByDescending(entrant => entrant.WordsSolved)
            .ThenByDescending(entrant => entrant.SecondsRemaining)
            .Take(count)
            .ToArray();
    }

    private RoyaleEntrant[] GetRankedEntrants() =>
        _entrants
            .OrderBy(entrant => entrant.IsEliminated ? 1 : 0)
            .ThenByDescending(entrant => entrant.WordsSolved)
            .ThenByDescending(entrant => entrant.SecondsRemaining)
            .ToArray();

    private void UpdatePlacements()
    {
        var newlyEliminated = _entrants
            .Where(entrant => entrant.IsEliminated && entrant.Placement is null)
            .OrderBy(entrant => entrant.SecondsRemaining)
            .ToArray();
        for (var index = 0; index < newlyEliminated.Length; index++)
            newlyEliminated[index].SetPlacement(AliveCount + newlyEliminated.Length - index);
    }

    private RoyaleEntrant FindEntrant(string entrantName) =>
        _entrants.FirstOrDefault(entrant => entrant.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");
}
