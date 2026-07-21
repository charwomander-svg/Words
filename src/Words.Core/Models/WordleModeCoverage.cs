namespace Words.Core.Models;

public sealed record WordleModeCoverage(
    WordleGameMode Mode,
    int WordLength,
    int CandidateCount,
    int TotalWords)
{
    public double CandidatePercent => TotalWords == 0 ? 0 : CandidateCount * 100.0 / TotalWords;
}
