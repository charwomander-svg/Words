using Words.Core.Models;

namespace Words.Core.Interfaces;

/// <summary>
/// Tracks and retrieves player scores.
/// </summary>
public interface IScoreService
{
    /// <summary>
    /// Awards points to the player and persists the score.
    /// </summary>
    void AwardPoints(Player player, int points);

    /// <summary>
    /// Returns a leaderboard sorted by score descending.
    /// </summary>
    IReadOnlyList<Player> GetLeaderboard();
}
