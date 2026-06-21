namespace Words.Xbox;

public enum XboxRoundAction
{
    Invalid,
    GuessLetter,
    ShowHelp,
    QuitRound
}

public readonly record struct XboxRoundCommand(XboxRoundAction Action, char Letter = '\0');

public static class XboxInputScheme
{
    public static XboxRoundCommand ParseRoundCommand(string? input)
    {
        input = input?.Trim() ?? string.Empty;
        if (input.Length == 0)
            return new XboxRoundCommand(XboxRoundAction.Invalid);

        if (input.Equals("?", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.ShowHelp);
        }

        if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.QuitRound);
        }

        return input.Length == 1 && char.IsLetter(input[0])
            ? new XboxRoundCommand(XboxRoundAction.GuessLetter, char.ToUpperInvariant(input[0]))
            : new XboxRoundCommand(XboxRoundAction.Invalid);
    }

    public static string Describe() =>
        "Controls: [A-Z]=guess, ? or HELP=show controls, Q or QUIT=end round";
}
