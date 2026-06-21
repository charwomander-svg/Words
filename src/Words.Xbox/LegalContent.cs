namespace Words.Xbox;

internal static class LegalContent
{
    internal static IReadOnlyList<string> TermsOfService { get; } = new[]
    {
        "These terms govern your use of Guess That Word.",
        "The game is provided for entertainment only and may be changed, updated, or removed at any time without notice.",
        "To the fullest extent permitted by law, the game is provided \"as is\" and without warranties of any kind, express or implied.",
        "You agree not to cheat, tamper with, reverse engineer, or otherwise misuse the game or its data.",
        "You are responsible for complying with the rules of the platform you use to run the game and any local laws that apply to you.",
    };

    internal static IReadOnlyList<string> PrivacyPolicy { get; } = new[]
    {
        "Guess That Word does not require an account, cloud sync, or network connection to play.",
        "The game only uses the information you enter during the current session, such as your GamerTag, guesses, scores, and round progress.",
        "That information is used locally to run the game and is not intentionally sold, shared, or transmitted by this codebase.",
        "If you play through a platform or wrapper that collects telemetry, that collection is governed by that platform's own privacy policy.",
        "If you do not agree with this policy, stop using the game.",
    };

    internal static IReadOnlyList<string> LegalNotice { get; } = new[]
    {
        "Guess That Word is a fictional word game and is not affiliated with, sponsored by, or endorsed by Microsoft or Xbox.",
        "All gameplay text, word lists, and score behavior may be updated to improve the experience.",
        "No legal, financial, or professional advice is provided by this software.",
        "Use of the game is at your own risk, and the developers disclaim liability to the maximum extent permitted by law.",
    };
}
