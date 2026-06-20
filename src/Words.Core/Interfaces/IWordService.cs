using Words.Core.Models;

namespace Words.Core.Interfaces;

/// <summary>
/// Provides words for game sessions.
/// </summary>
public interface IWordService
{
    /// <summary>
    /// Returns a random word matching the given category and difficulty.
    /// </summary>
    Word GetRandomWord(WordCategory category, GameDifficulty difficulty);

    /// <summary>
    /// Returns all available words.
    /// </summary>
    IReadOnlyList<Word> GetAllWords();
}
