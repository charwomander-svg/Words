using Words.Core.Models;

namespace Words.Core.Services;

public static class PowerUpEffectService
{
    private static readonly char[] Vowels = ['A', 'E', 'I', 'O', 'U'];

    public static PowerUpUseResult UseBomb(string answer, IEnumerable<char> guessedLetters, int count = 5)
    {
        var normalized = NormalizeAnswer(answer);
        var guessed = guessedLetters.Select(char.ToUpperInvariant).ToHashSet();
        var answerLetters = normalized.ToHashSet();
        var removed = Enumerable.Range('A', 26)
            .Select(value => (char)value)
            .Where(letter => !answerLetters.Contains(letter) && !guessed.Contains(letter))
            .Take(count)
            .ToArray();

        return new PowerUpUseResult(
            PowerUpType.Bomb,
            removed.Length == 0 ? "Bomb found no safe letters to remove." : $"Bomb removed: {string.Join(", ", removed)}.",
            removed);
    }

    public static PowerUpUseResult UsePingPongPaddle(string answer, char vowel)
    {
        var normalized = NormalizeAnswer(answer);
        var selected = char.ToUpperInvariant(vowel);
        if (!Vowels.Contains(selected))
            throw new ArgumentOutOfRangeException(nameof(vowel), "Ping Pong Paddle can only ping A, E, I, O, or U.");

        var appears = normalized.Contains(selected);
        return new PowerUpUseResult(
            PowerUpType.PingPongPaddle,
            appears ? $"{selected} appears in the answer." : $"{selected} does not appear in the answer.",
            [selected],
            Letter: selected);
    }

    public static PowerUpUseResult UseFirstLetter(string answer)
    {
        var normalized = NormalizeAnswer(answer);
        return new PowerUpUseResult(
            PowerUpType.FirstLetter,
            $"First letter: {normalized[0]}.",
            [normalized[0]],
            Position: 0,
            Letter: normalized[0]);
    }

    public static PowerUpUseResult UseReveal(string answer)
    {
        var normalized = NormalizeAnswer(answer);
        return new PowerUpUseResult(
            PowerUpType.Reveal,
            $"Reveal: position 1 is {normalized[0]}.",
            [normalized[0]],
            Position: 0,
            Letter: normalized[0]);
    }

    public static PowerUpUseResult UseClock(int bonusSeconds = 15) =>
        new(PowerUpType.Clock, $"Clock armed: next solve grants +{bonusSeconds}s.", [], TimeDeltaSeconds: bonusSeconds);

    public static PowerUpUseResult UseShield() =>
        new(PowerUpType.Shield, "Shield armed: next fail penalty is reduced.", []);

    public static PowerUpUseResult UseCrown() =>
        new(PowerUpType.Crown, "Crown armed: survive one Royale elimination at 1 second.", []);

    private static string NormalizeAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException("Answer cannot be empty.", nameof(answer));

        var normalized = answer.Trim().ToUpperInvariant();
        if (normalized.Any(letter => letter is < 'A' or > 'Z'))
            throw new ArgumentException("Answer must contain only A-Z letters.", nameof(answer));

        return normalized;
    }
}
