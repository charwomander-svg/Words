using Words.Xbox;

namespace Words.Tests;

public class XboxInputSchemeTests
{
    [Theory]
    [InlineData("a", XboxRoundAction.GuessLetter, 'A')]
    [InlineData(" Z ", XboxRoundAction.GuessLetter, 'Z')]
    [InlineData("help", XboxRoundAction.ShowHelp, '\0')]
    [InlineData("?", XboxRoundAction.ShowHelp, '\0')]
    [InlineData("quit", XboxRoundAction.QuitRound, '\0')]
    [InlineData("q", XboxRoundAction.QuitRound, '\0')]
    public void ParseRoundCommand_RecognizesControlInput(string input, XboxRoundAction action, char letter)
    {
        var command = XboxInputScheme.ParseRoundCommand(input);

        Assert.Equal(action, command.Action);
        Assert.Equal(letter, command.Letter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    [InlineData("1")]
    public void ParseRoundCommand_InvalidInput_ReturnsInvalid(string input)
    {
        var command = XboxInputScheme.ParseRoundCommand(input);

        Assert.Equal(XboxRoundAction.Invalid, command.Action);
    }
}
