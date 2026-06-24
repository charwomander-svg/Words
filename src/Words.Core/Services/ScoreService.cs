using Words.Core.Interfaces;
using Words.Core.Models;
using System.Text.Json;

namespace Words.Core.Services;

/// <summary>
/// Leaderboard that tracks awarded scores per player and can persist them to disk.
/// </summary>
public class ScoreService : IScoreService
{
    private readonly Dictionary<string, Player> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly string? _storagePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public ScoreService(string? storagePath = null)
    {
        _storagePath = string.IsNullOrWhiteSpace(storagePath) ? null : storagePath;
        LoadFromDisk();
    }

    /// <inheritdoc/>
    public void AwardPoints(Player player, int points)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be non-negative.");

        // Keep a canonical reference per gamer tag for leaderboard lookups
        _players.TryAdd(player.GamerTag, player);
        player.AddScore(points);
        SaveToDisk();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Player> GetLeaderboard() =>
        _players.Values
                .OrderByDescending(p => p.Score)
                .ToList();

    private void LoadFromDisk()
    {
        if (_storagePath is null || !File.Exists(_storagePath))
            return;

        try
        {
            var json = File.ReadAllText(_storagePath);
            var savedPlayers = JsonSerializer.Deserialize<List<LeaderboardEntry>>(json, JsonOptions)
                               ?? [];

            foreach (var saved in savedPlayers)
            {
                if (string.IsNullOrWhiteSpace(saved.GamerTag))
                    continue;

                var player = new Player(saved.GamerTag);
                if (saved.Score > 0)
                    player.AddScore(saved.Score);

                _players[player.GamerTag] = player;
            }
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void SaveToDisk()
    {
        if (_storagePath is null)
            return;

        try
        {
            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var savedPlayers = _players.Values
                .OrderByDescending(player => player.Score)
                .Select(player => new LeaderboardEntry(player.GamerTag, player.Score))
                .ToList();

            File.WriteAllText(_storagePath, JsonSerializer.Serialize(savedPlayers, JsonOptions));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record LeaderboardEntry(string GamerTag, int Score);
}
