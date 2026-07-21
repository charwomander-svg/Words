using Words.Core.Models;

namespace Words.Core.Services;

public static class RoyaleBotTactics
{
    public static bool ShouldSolve(RoyaleEntrant bot, int index, int pulse, string phase)
    {
        ArgumentNullException.ThrowIfNull(bot);
        var pressure = GetPhasePressure(phase);
        return bot.Persona switch
        {
            "Elite" => index % Math.Max(2, 4 - pressure) == 0,
            "Sprinter" => index % (pressure >= 2 ? 3 : 4) == 0,
            "Streaky" => pulse % (pressure >= 2 ? 7 : 14) == 0 && index % 2 == 0,
            "Steady" => index % Math.Max(4, 7 - pressure) == 0,
            "Technical" => index % Math.Max(5, 8 - pressure) == 0,
            "Aggressive" => index % Math.Max(3, 6 - pressure) == 0,
            "Scholar" => index % Math.Max(6, 9 - pressure) == 0,
            "Patient" => pulse % (pressure >= 3 ? 14 : 21) == 0 && index % 3 == 0,
            "Chaotic" => index % 4 == 1 || (pressure >= 3 && index % 5 == 0),
            "Volatile" => index % Math.Max(3, 6 - pressure) == 2,
            _ => index % Math.Max(6, 10 - pressure) == 0
        };
    }

    public static bool ShouldFail(RoyaleEntrant bot, int index, int pulse, string phase)
    {
        ArgumentNullException.ThrowIfNull(bot);
        var pressure = GetPhasePressure(phase);
        return bot.Persona switch
        {
            "Elite" => index % 17 == 0 && pressure < 3,
            "Sprinter" => index % (pressure >= 2 ? 4 : 5) == 0,
            "Streaky" => pulse % (pressure >= 2 ? 11 : 22) == 0 && index % 3 == 0,
            "Steady" => index % (pressure >= 3 ? 13 : 11) == 0,
            "Technical" => index % (pressure >= 3 ? 15 : 13) == 0,
            "Aggressive" => index % Math.Max(4, 7 - pressure) == 0,
            "Scholar" => index % (pressure >= 3 ? 16 : 14) == 0,
            "Patient" => index % (pressure >= 3 ? 16 : 18) == 0,
            "Chaotic" => index % 3 == 0,
            "Volatile" => index % (pressure >= 2 ? 2 : 3) == 0,
            _ => index % Math.Max(5, 8 - pressure) == 0
        };
    }

    private static int GetPhasePressure(string phase) => phase switch
    {
        "Victory" => 4,
        "Duel" => 4,
        "Final 10" => 3,
        "Bubble" => 2,
        "Midgame" => 1,
        _ => 0
    };
}
