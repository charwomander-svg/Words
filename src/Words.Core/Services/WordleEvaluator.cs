using Words.Core.Models;

namespace Words.Core.Services;

public static class WordleEvaluator
{
    public static WordleLetterState[] Evaluate(string guess, string answer)
    {
        if (string.IsNullOrWhiteSpace(guess))
        {
            throw new ArgumentException("Guess cannot be empty.", nameof(guess));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer cannot be empty.", nameof(answer));
        }

        var normalizedGuess = guess.Trim().ToUpperInvariant();
        var normalizedAnswer = answer.Trim().ToUpperInvariant();
        if (normalizedGuess.Length != normalizedAnswer.Length)
        {
            throw new ArgumentException("Guess and answer lengths must match.");
        }

        var result = new WordleLetterState[normalizedGuess.Length];
        var remainingAnswerLetters = new Dictionary<char, int>();

        for (var index = 0; index < normalizedGuess.Length; index++)
        {
            if (normalizedGuess[index] == normalizedAnswer[index])
            {
                result[index] = WordleLetterState.Correct;
                continue;
            }

            var answerLetter = normalizedAnswer[index];
            remainingAnswerLetters[answerLetter] = remainingAnswerLetters.GetValueOrDefault(answerLetter) + 1;
        }

        for (var index = 0; index < normalizedGuess.Length; index++)
        {
            if (result[index] == WordleLetterState.Correct)
            {
                continue;
            }

            var guessLetter = normalizedGuess[index];
            if (remainingAnswerLetters.GetValueOrDefault(guessLetter) > 0)
            {
                result[index] = WordleLetterState.Present;
                remainingAnswerLetters[guessLetter]--;
            }
            else
            {
                result[index] = WordleLetterState.Absent;
            }
        }

        return result;
    }
}
