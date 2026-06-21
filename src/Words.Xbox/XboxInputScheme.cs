namespace Words.Xbox;

public enum XboxRoundAction
{
    Invalid,
    GuessLetter,
    ShowHelp,
    QuitRound,
    PreviousLetter,
    NextLetter,
    SubmitSelectedLetter
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
            input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("back", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.QuitRound);
        }

        if (input.Equals("left", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("prev", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("previous", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.PreviousLetter);
        }

        if (input.Equals("right", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("next", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.NextLetter);
        }

        if (input.Equals("enter", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("submit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("confirm", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("select", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.SubmitSelectedLetter);
        }

        return input.Length == 1 && char.IsLetter(input[0])
            ? new XboxRoundCommand(XboxRoundAction.GuessLetter, char.ToUpperInvariant(input[0]))
            : new XboxRoundCommand(XboxRoundAction.Invalid);
    }

    public static string Describe() =>
        "Controls: [A-Z]=guess, LEFT/RIGHT=cycle letter, ENTER/SUBMIT=guess selected letter, ? or HELP=show controls, Q or QUIT/BACK=end round";
}
