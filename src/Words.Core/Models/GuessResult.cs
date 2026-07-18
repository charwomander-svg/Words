namespace Words.Core.Models;

/// <summary>
/// The outcome of a single letter guess.
/// </summary>
public enum GuessOutcome
{
    /// <summary>The letter is correct and in the right position.</summary>
    Correct,
    /// <summary>The letter has already been guessed.</summary>
    AlreadyGuessed,
    /// <summary>The letter is not in the word.</summary>
    Incorrect,
    /// <summary>The game is already over.</summary>
    GameOver
}

/// <summary>
/// Result returned after a player guesses a letter.
/// </summary>
public record GuessResult(
    char Letter,
    GuessOutcome Outcome,
    string MaskedWord,
    bool IsWordSolved
);

/// <summary>
/// The outcome of a full-word guess.
/// </summary>
public enum WordGuessOutcome
{
    /// <summary>The submitted word exactly matches the answer.</summary>
    Correct,
    /// <summary>The submitted word does not match the answer.</summary>
    Incorrect,
    /// <summary>The game is already over.</summary>
    GameOver
}

/// <summary>
/// Result returned after a player guesses the full word.
/// </summary>
public record WordGuessResult(
    string Guess,
    WordGuessOutcome Outcome,
    string MaskedWord,
    bool IsWordSolved
);
