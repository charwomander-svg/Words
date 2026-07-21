namespace Words.Core.Models;

public sealed class CpuRaceRun
{
    private int _cpuProgressSeconds;

    public CpuRaceRun(int targetWords = 5, int cpuSecondsPerWord = 18, string cpuPersona = "Steady")
    {
        if (targetWords < 1)
            throw new ArgumentOutOfRangeException(nameof(targetWords), "Target words must be at least 1.");
        if (cpuSecondsPerWord < 1)
            throw new ArgumentOutOfRangeException(nameof(cpuSecondsPerWord), "CPU pace must be at least 1 second per word.");
        if (string.IsNullOrWhiteSpace(cpuPersona))
            throw new ArgumentException("CPU persona cannot be empty.", nameof(cpuPersona));

        TargetWords = targetWords;
        CpuSecondsPerWord = cpuSecondsPerWord;
        CpuPersona = cpuPersona.Trim();
    }

    public int TargetWords { get; }
    public int CpuSecondsPerWord { get; }
    public string CpuPersona { get; }
    public int PlayerSolved { get; private set; }
    public int CpuSolved { get; private set; }
    public int ElapsedSeconds { get; private set; }
    public bool IsComplete => Winner is not null;
    public string? Winner { get; private set; }

    public void RecordPlayerSolved()
    {
        if (IsComplete)
            return;

        PlayerSolved++;
        UpdateWinner();
    }

    public void Tick(int seconds = 1)
    {
        if (seconds < 1)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Tick must be at least 1 second.");
        if (IsComplete)
            return;

        var pressureGain = GetPersonaPressureGain(seconds);
        _cpuProgressSeconds += seconds + pressureGain;
        ElapsedSeconds += seconds;
        while (_cpuProgressSeconds >= CpuSecondsPerWord && !IsComplete)
        {
            _cpuProgressSeconds -= CpuSecondsPerWord;
            CpuSolved++;
            UpdateWinner();
        }

        UpdateWinner();
    }

    private int GetPersonaPressureGain(int seconds)
    {
        var remainingCpu = Math.Max(0, TargetWords - CpuSolved);
        var remainingPlayer = Math.Max(0, TargetWords - PlayerSolved);
        var trailing = PlayerSolved - CpuSolved;
        return CpuPersona switch
        {
            "Sprinter" => ElapsedSeconds < 30 ? Math.Max(1, seconds / 3) : 0,
            "Aggressive" => trailing >= 1 ? Math.Max(1, seconds / 4) : 0,
            "Closer" => remainingCpu <= 2 ? Math.Max(1, seconds / 3) : 0,
            "Streaky" => (ElapsedSeconds / 12) % 2 == 0 ? Math.Max(1, seconds / 4) : 0,
            "Chaos" => (ElapsedSeconds / 10) % 3 == 0 ? Math.Max(1, seconds / 3) : 0,
            "Marathon" => remainingPlayer <= 2 ? Math.Max(1, seconds / 5) : 0,
            _ => 0
        };
    }

    private void UpdateWinner()
    {
        if (PlayerSolved >= TargetWords)
            Winner = "Player";
        else if (CpuSolved >= TargetWords)
            Winner = "CPU";
    }
}
