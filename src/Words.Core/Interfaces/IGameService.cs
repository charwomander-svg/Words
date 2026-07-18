using Words.Core.Models;

namespace Words.Core.Interfaces;

/// <summary>
/// Manages game session lifecycle.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Starts a new game session for the given player.
    /// </summary>
    GameSession StartGame(Player player, GameConfig config);

    /// <summary>
    /// Processes a letter guess for an existing session.
    /// </summary>
    GuessResult SubmitGuess(Guid sessionId, char letter);

    /// <summary>
    /// Processes a full-word guess for an existing session.
    /// </summary>
    WordGuessResult SubmitWordGuess(Guid sessionId, string guess);

    /// <summary>
    /// Returns the current state of a session.
    /// </summary>
    GameSession GetSession(Guid sessionId);

    /// <summary>
    /// Ends the session, records the result, and awards any earned score.
    /// </summary>
    void EndGame(Guid sessionId);
}
