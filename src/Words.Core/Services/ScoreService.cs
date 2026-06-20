using Words.Core.Interfaces;
using Words.Core.Models;

namespace Words.Core.Services;

/// <summary>
/// In-memory leaderboard that tracks awarded scores per player.
/// </summary>
public class ScoreService : IScoreService
{
    private readonly Dictionary<string, Player> _players = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void AwardPoints(Player player, int points)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be non-negative.");

        // Keep a canonical reference per gamer tag for leaderboard lookups
        _players.TryAdd(player.GamerTag, player);
        player.AddScore(points);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Player> GetLeaderboard() =>
        _players.Values
                .OrderByDescending(p => p.Score)
                .ToList();
}
