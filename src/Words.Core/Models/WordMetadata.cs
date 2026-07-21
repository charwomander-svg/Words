namespace Words.Core.Models;

public sealed record WordMetadata(
    string Text,
    WordCommonality Commonality,
    string Label,
    string Description);
