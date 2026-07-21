namespace Words.Core.Models;

public sealed record WordMetadataCoverage(
    int WordLength,
    int TotalWords,
    int TaggedWords,
    int WildDictionaryWords)
{
    public double TaggedPercent => TotalWords == 0 ? 0 : TaggedWords * 100.0 / TotalWords;
}
