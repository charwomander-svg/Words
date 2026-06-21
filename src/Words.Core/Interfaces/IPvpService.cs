using Words.Core.Models;

namespace Words.Core.Interfaces;

/// <summary>
/// Manages head-to-head matches on the same word.
/// </summary>
public interface IPvpService
{
    /// <summary>
    /// Starts a new PvP match for two players using the same target word length.
    /// </summary>
    PvpMatch StartMatch(Player playerOne, Player playerTwo, int wordLength);

    /// <summary>
    /// Submits a guess for the named player in the match.
    /// </summary>
    GuessResult SubmitGuess(Guid matchId, string gamerTag, char letter);

    /// <summary>
    /// Returns an active or completed match.
    /// </summary>
    PvpMatch GetMatch(Guid matchId);
}
