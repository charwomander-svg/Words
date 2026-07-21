using Words.Core.Models;

namespace Words.Core.Services;

public static class PowerUpRewardService
{
    public static IReadOnlyList<PowerUpType> GetRewards(int guessesUsed, int currentStreak)
    {
        var rewards = new List<PowerUpType>();

        rewards.AddRange(GetFastSolveRewards(guessesUsed));
        rewards.AddRange(GetStreakRewards(currentStreak));

        return rewards;
    }

    public static IReadOnlyList<PowerUpType> GetFastSolveRewards(int guessesUsed) => guessesUsed switch
    {
        1 => [PowerUpType.Crown],
        2 => [PowerUpType.Cleanse, PowerUpType.FirstLetter],
        3 => [PowerUpType.Bomb, PowerUpType.PingPongPaddle, PowerUpType.Clock],
        _ => []
    };

    public static IReadOnlyList<PowerUpType> GetStreakRewards(int currentStreak) => currentStreak switch
    {
        3 => [PowerUpType.Shield],
        5 => [PowerUpType.Reveal],
        10 => [PowerUpType.Freeze],
        15 => [PowerUpType.Reroll],
        _ => []
    };
}
