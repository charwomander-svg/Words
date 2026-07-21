using System.Text.Json;
using Words.Core.Models;

namespace Words.Core.Services;

public sealed class DemoStatsStore
{
    private const int MaxRecentWords = 20;
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public int BestSurvivalStreak { get; set; }
    public int TimeAttackRuns { get; set; }
    public int BestTimeAttackScore { get; set; }
    public int BestTimeAttackSolved { get; set; }
    public int BestTimeAttackCombo { get; set; }
    public int RoyaleRuns { get; set; }
    public int BestRoyalePlacement { get; set; }
    public int RoyaleWins { get; set; }
    public int RoyaleTopTenFinishes { get; set; }
    public int BestRoyaleSolved { get; set; }
    public int MegaBoardRuns { get; set; }
    public int BestMegaBoardClears { get; set; }
    public int BestMegaBoardScore { get; set; }
    public int MegaBoardPerfectClears { get; set; }
    public int MegaBoardCenturionClears { get; set; }
    public int TotalScore { get; set; }
    public int BestRoundScore { get; set; }
    public int PerfectGames { get; set; }
    public string LastMode { get; set; } = WordleGameMode.Classic.ToString();
    public int LastWordLength { get; set; } = 5;
    public Dictionary<string, int> WinsByMode { get; set; } = [];
    public Dictionary<int, int> WinsByLength { get; set; } = [];
    public Dictionary<int, int> WinsByAttemptCount { get; set; } = [];
    public Dictionary<string, int> DiscoveredByRarity { get; set; } = [];
    public Dictionary<string, int> PowerUps { get; set; } = [];
    public List<string> DailyWins { get; set; } = [];
    public List<string> DiscoveredWords { get; set; } = [];
    public List<DiscoveredWord> RecentWords { get; set; } = [];

    public static DemoStatsStore Load(string path)
    {
        if (!File.Exists(path))
            return new DemoStatsStore();

        try
        {
            var json = File.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new DemoStatsStore();

            if (TryGetPropertyIgnoreCase(document.RootElement, "stats", out var statsElement))
            {
                var wrapped = statsElement.Deserialize<DemoStatsStore>(SerializerOptions);
                return Normalize(wrapped);
            }

            var legacy = document.RootElement.Deserialize<DemoStatsStore>(SerializerOptions);
            return Normalize(legacy);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new DemoStatsStore();
        }
    }

    public void RecordRound(
        WordleSession session,
        WordleGameMode mode,
        WordMetadata metadata)
    {
        var won = session.Status == GameStatus.Won;
        var roundScore = CalculateRoundScore(session, mode, metadata);
        GamesPlayed++;
        if (won)
        {
            Wins++;
            CurrentStreak++;
            BestStreak = Math.Max(BestStreak, CurrentStreak);
            TotalScore += roundScore;
            BestRoundScore = Math.Max(BestRoundScore, roundScore);
            if (session.Guesses.Count == 1)
                PerfectGames++;
            if (mode == WordleGameMode.Daily)
                RecordDailyWin(DateOnly.FromDateTime(DateTime.Now));
            Increment(WinsByMode, mode.ToString());
            Increment(WinsByLength, session.WordLength);
            Increment(WinsByAttemptCount, session.Guesses.Count);
            foreach (var reward in PowerUpRewardService.GetRewards(session.Guesses.Count, CurrentStreak))
                AddPowerUp(reward);
        }
        else
        {
            Losses++;
            CurrentStreak = 0;
        }

        RecentWords.Insert(0, new DiscoveredWord(
            session.Answer,
            mode.ToString(),
            session.WordLength,
            metadata.Label,
            won,
            session.Guesses.Count,
            roundScore,
            DateTimeOffset.Now));

        if (RecentWords.Count > MaxRecentWords)
            RecentWords.RemoveRange(MaxRecentWords, RecentWords.Count - MaxRecentWords);

        if (!DiscoveredWords.Contains(session.Answer, StringComparer.OrdinalIgnoreCase))
        {
            DiscoveredWords.Add(session.Answer);
            DiscoveredWords.Sort(StringComparer.OrdinalIgnoreCase);
            Increment(DiscoveredByRarity, metadata.Label);
        }
    }

    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var payload = new PersistedDemoStats(CurrentSchemaVersion, this);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, json);
        File.Copy(tempPath, path, overwrite: true);
        File.Delete(tempPath);
    }

    public void Clear()
    {
        GamesPlayed = 0;
        Wins = 0;
        Losses = 0;
        CurrentStreak = 0;
        BestStreak = 0;
        BestSurvivalStreak = 0;
        TimeAttackRuns = 0;
        BestTimeAttackScore = 0;
        BestTimeAttackSolved = 0;
        BestTimeAttackCombo = 0;
        RoyaleRuns = 0;
        BestRoyalePlacement = 0;
        RoyaleWins = 0;
        RoyaleTopTenFinishes = 0;
        BestRoyaleSolved = 0;
        MegaBoardRuns = 0;
        BestMegaBoardClears = 0;
        BestMegaBoardScore = 0;
        MegaBoardPerfectClears = 0;
        MegaBoardCenturionClears = 0;
        TotalScore = 0;
        BestRoundScore = 0;
        PerfectGames = 0;
        LastMode = WordleGameMode.Classic.ToString();
        LastWordLength = 5;
        WinsByMode.Clear();
        WinsByLength.Clear();
        WinsByAttemptCount.Clear();
        DiscoveredByRarity.Clear();
        PowerUps.Clear();
        DailyWins.Clear();
        DiscoveredWords.Clear();
        RecentWords.Clear();
    }

    public void RecordSurvivalRun(int streak)
    {
        BestSurvivalStreak = Math.Max(BestSurvivalStreak, streak);
    }

    public void RecordTimeAttackRun(TimeAttackRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        TimeAttackRuns++;
        BestTimeAttackScore = Math.Max(BestTimeAttackScore, run.Score);
        BestTimeAttackSolved = Math.Max(BestTimeAttackSolved, run.WordsSolved);
        BestTimeAttackCombo = Math.Max(BestTimeAttackCombo, run.BestCombo);
        TotalScore += run.Score;
        BestRoundScore = Math.Max(BestRoundScore, run.Score);
        if (run.WordsSolved > 0)
            Increment(WinsByMode, WordleGameMode.Timed.ToString());
        if (run.BestCombo >= 10)
            AddPowerUp(PowerUpType.Freeze);
        else if (run.BestCombo >= 5)
            AddPowerUp(PowerUpType.Clock);
    }

    public void RecordRoyalePlacement(int placement)
    {
        if (placement < 1)
            throw new ArgumentOutOfRangeException(nameof(placement), "Placement must be at least 1.");

        RoyaleRuns++;
        if (BestRoyalePlacement == 0 || placement < BestRoyalePlacement)
            BestRoyalePlacement = placement;
        if (placement <= 10)
        {
            RoyaleTopTenFinishes++;
            AddPowerUp(PowerUpType.Shield);
        }
        if (placement == 1)
        {
            RoyaleWins++;
            AddPowerUp(PowerUpType.Crown);
        }
    }

    public void RecordRoyaleRun(RoyaleRun run, string entrantName)
    {
        ArgumentNullException.ThrowIfNull(run);
        var entrant = run.Entrants.FirstOrDefault(value => value.Name.Equals(entrantName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Royale entrant '{entrantName}' was not found.");

        RecordRoyalePlacement(run.GetPlacement(entrantName));
        BestRoyaleSolved = Math.Max(BestRoyaleSolved, entrant.WordsSolved);
        if (entrant.WordsSolved >= 5)
            AddPowerUp(PowerUpType.Clock);
        if (entrant.WordsSolved >= 10)
            AddPowerUp(PowerUpType.Freeze);
    }

    public void RecordMegaBoardRun(MegaBoardRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        MegaBoardRuns++;
        BestMegaBoardClears = Math.Max(BestMegaBoardClears, run.SolvedCount);
        var score = run.CalculateScore();
        BestMegaBoardScore = Math.Max(BestMegaBoardScore, score);
        TotalScore += score;
        BestRoundScore = Math.Max(BestRoundScore, score);
        if (run.IsPerfectClear)
            MegaBoardPerfectClears++;
        if (run.BoardCount >= 100 && run.SolvedCount == run.BoardCount)
            MegaBoardCenturionClears++;
        if (run.SolvedCount > 0)
            Increment(WinsByMode, WordleGameMode.MegaBoard.ToString());
        if (run.IsPerfectClear)
            AddPowerUp(PowerUpType.Crown);
        else if (run.SolvedCount >= 50)
            AddPowerUp(PowerUpType.Reveal);
        else if (run.SolvedCount >= 25)
            AddPowerUp(PowerUpType.Bomb);
    }

    public void AddPowerUp(PowerUpType powerUp)
    {
        Increment(PowerUps, powerUp.ToString());
    }

    public bool TrySpendPowerUp(PowerUpType powerUp)
    {
        var key = powerUp.ToString();
        if (!PowerUps.TryGetValue(key, out var count) || count <= 0)
            return false;

        if (count == 1)
            PowerUps.Remove(key);
        else
            PowerUps[key] = count - 1;

        return true;
    }

    private void RecordDailyWin(DateOnly date)
    {
        var value = date.ToString("yyyy-MM-dd");
        if (!DailyWins.Contains(value, StringComparer.Ordinal))
            DailyWins.Add(value);
    }

    public static int CalculateRoundScore(
        WordleSession session,
        WordleGameMode mode,
        WordMetadata metadata)
    {
        if (session.Status != GameStatus.Won)
            return 0;

        var lengthScore = session.WordLength * 100;
        var attemptBonus = Math.Max(0, session.RemainingAttempts) * 50;
        var rarityMultiplier = metadata.Commonality switch
        {
            WordCommonality.Common => 1.0,
            WordCommonality.Uncommon => 1.25,
            WordCommonality.Rare => 1.6,
            WordCommonality.Obscure => 2.0,
            WordCommonality.WildDictionary => 2.5,
            _ => 1.0
        };
        var modeMultiplier = mode switch
        {
            WordleGameMode.Hard => 1.25,
            WordleGameMode.Timed => 1.35,
            WordleGameMode.Survival => 1.5,
            WordleGameMode.RareHunt => 1.4,
            WordleGameMode.WildDictionary => 1.2,
            _ => 1.0
        };

        return (int)Math.Round((lengthScore + attemptBonus) * rarityMultiplier * modeMultiplier);
    }

    private static void Increment<TKey>(Dictionary<TKey, int> values, TKey key)
        where TKey : notnull
    {
        values[key] = values.GetValueOrDefault(key) + 1;
    }

    private static DemoStatsStore Normalize(DemoStatsStore? value)
    {
        var stats = value ?? new DemoStatsStore();
        stats.LastMode = string.IsNullOrWhiteSpace(stats.LastMode)
            ? WordleGameMode.Classic.ToString()
            : stats.LastMode;
        stats.LastWordLength = Math.Clamp(stats.LastWordLength <= 0 ? 5 : stats.LastWordLength, 4, 20);
        stats.WinsByMode ??= [];
        stats.WinsByLength ??= [];
        stats.WinsByAttemptCount ??= [];
        stats.DiscoveredByRarity ??= [];
        stats.PowerUps ??= [];
        stats.DailyWins ??= [];
        stats.DiscoveredWords ??= [];
        stats.RecentWords ??= [];
        return stats;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

public sealed record PersistedDemoStats(int SchemaVersion, DemoStatsStore Stats);

public sealed record DiscoveredWord(
    string Text,
    string Mode,
    int Length,
    string Rarity,
    bool Won,
    int AttemptsUsed,
    int Score,
    DateTimeOffset PlayedAt);
