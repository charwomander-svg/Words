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
    /// Adds an achievement to the player and persists it.
    /// </summary>
    bool AwardAchievement(Player player, string achievementId);

    /// <summary>
    /// Returns a leaderboard sorted by score descending.
    /// </summary>
    IReadOnlyList<Player> GetLeaderboard();
}
