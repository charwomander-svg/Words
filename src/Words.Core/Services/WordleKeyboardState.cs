using Words.Core.Models;

namespace Words.Core.Services;

public sealed class WordleKeyboardState
{
    private readonly Dictionary<char, WordleTileState> _letters = [];

    public IReadOnlyDictionary<char, WordleTileState> Letters => _letters;

    public void Clear() => _letters.Clear();

    public bool Apply(WordleGuessResult result)
    {
        var changed = false;
        for (var index = 0; index < result.Guess.Length; index++)
        {
            if (Set(result.Guess[index], result.Tiles[index]))
                changed = true;
        }

        return changed;
    }

    public bool Set(char letter, WordleTileState state)
    {
        var normalized = char.ToUpperInvariant(letter);
        if (_letters.TryGetValue(normalized, out var current) && GetRank(current) >= GetRank(state))
            return false;

        _letters[normalized] = state;
        return true;
    }

    public bool TryGet(char letter, out WordleTileState state) =>
        _letters.TryGetValue(char.ToUpperInvariant(letter), out state);

    public static int GetRank(WordleTileState state) => state switch
    {
        WordleTileState.Correct => 3,
        WordleTileState.Present => 2,
        WordleTileState.Absent => 1,
        _ => 0
    };
}
