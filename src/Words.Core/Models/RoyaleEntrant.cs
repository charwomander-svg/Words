namespace Words.Core.Models;

public sealed class RoyaleEntrant
{
    internal RoyaleEntrant(string name, bool isBot, int startingSeconds, string persona = "Human")
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Entrant name cannot be empty.", nameof(name))
            : name.Trim();
        IsBot = isBot;
        Persona = persona;
        SecondsRemaining = startingSeconds;
    }

    public string Name { get; }
    public bool IsBot { get; }
    public string Persona { get; }
    public int SecondsRemaining { get; private set; }
    public int WordsSolved { get; private set; }
    public int WordsFailed { get; private set; }
    public int? Placement { get; private set; }
    public bool IsEliminated => SecondsRemaining <= 0;

    internal void Tick(int seconds)
    {
        if (IsEliminated)
            return;

        SecondsRemaining = Math.Max(0, SecondsRemaining - seconds);
    }

    internal void RecordSolved(int bonusSeconds)
    {
        if (IsEliminated)
            return;

        WordsSolved++;
        SecondsRemaining += bonusSeconds;
    }

    internal void AddSeconds(int seconds)
    {
        SecondsRemaining += seconds;
        if (SecondsRemaining > 0)
            Placement = null;
    }

    internal void RemoveSeconds(int seconds)
    {
        if (IsEliminated)
            return;

        SecondsRemaining = Math.Max(0, SecondsRemaining - seconds);
    }

    internal void RecordFailed(int penaltySeconds)
    {
        if (IsEliminated)
            return;

        WordsFailed++;
        SecondsRemaining = Math.Max(0, SecondsRemaining - penaltySeconds);
    }

    internal void SetPlacement(int placement)
    {
        Placement ??= placement;
    }
}
