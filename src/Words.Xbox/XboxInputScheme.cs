namespace Words.Xbox;

public enum XboxRoundAction
{
    Invalid,
    MovePrevious,
    MoveNext,
    ConfirmLetter,
    DeleteLetter,
    SubmitWord,
    RequestHint,
    ShowHelp,
    QuitRound,
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
            return new XboxRoundCommand(XboxRoundAction.MovePrevious);
        }

        if (input.Equals("right", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("next", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.MoveNext);
        }

        if (input.Equals("a", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("confirm", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("select", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.ConfirmLetter);
        }

        if (input.Equals("b", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("backspace", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.DeleteLetter);
        }

        if (input.Equals("x", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("submit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("enter", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.SubmitWord);
        }

        if (input.Equals("y", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("hint", StringComparison.OrdinalIgnoreCase))
        {
            return new XboxRoundCommand(XboxRoundAction.RequestHint);
        }

        return new XboxRoundCommand(XboxRoundAction.Invalid);
    }

    public static string Describe() =>
        "Controls: LEFT/RIGHT=cycle QWERTY letters, A=confirm, B=delete, X=submit, Y=hint, ? or HELP=show controls, Q or QUIT/BACK=end round";
}
