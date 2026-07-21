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

    /// <summary>
    /// Returns a random dictionary word with the requested length.
    /// </summary>
    string GetRandomWord(int length);

    /// <summary>
    /// Returns unique random dictionary words with the requested length.
    /// </summary>
    IReadOnlyList<string> GetRandomWords(int length, int count);

    /// <summary>
    /// Returns all dictionary words with the requested length.
    /// </summary>
    IReadOnlyList<string> GetWordsByLength(int length);

    /// <summary>
    /// Returns true when the dictionary contains the word.
    /// </summary>
    bool IsValidDictionaryWord(string word);

    /// <summary>
    /// Returns player-facing metadata for a dictionary word.
    /// </summary>
    WordMetadata GetWordMetadata(string word);
}
