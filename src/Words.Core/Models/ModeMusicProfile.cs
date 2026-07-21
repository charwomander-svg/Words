namespace Words.Core.Models;

public sealed record ModeMusicProfile(
    WordleGameMode Mode,
    MusicEnergy Energy,
    string TrackSlot,
    string Notes);
