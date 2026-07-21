using Words.Core.Models;

namespace Words.Core.Services;

public static class ModeMusicProfileService
{
    private static readonly IReadOnlyDictionary<WordleGameMode, ModeMusicProfile> Profiles =
        new Dictionary<WordleGameMode, ModeMusicProfile>
        {
            [WordleGameMode.Classic] = new(WordleGameMode.Classic, MusicEnergy.Chill, "classic-chill", "Steady focus music for baseline puzzle play."),
            [WordleGameMode.Hard] = new(WordleGameMode.Hard, MusicEnergy.Metal, "hard-metal", "Sharper pacing and heavier tension."),
            [WordleGameMode.Daily] = new(WordleGameMode.Daily, MusicEnergy.Chill, "daily-chill", "Calm daily ritual vibe."),
            [WordleGameMode.Alphabet] = new(WordleGameMode.Alphabet, MusicEnergy.Chill, "alphabet-chill", "Progression-forward but relaxed."),
            [WordleGameMode.Timed] = new(WordleGameMode.Timed, MusicEnergy.Metal, "timed-metal", "Urgent tempo for speed pressure."),
            [WordleGameMode.Ghost] = new(WordleGameMode.Ghost, MusicEnergy.Chill, "ghost-chill", "Atmospheric focus without excess noise."),
            [WordleGameMode.VsCpu] = new(WordleGameMode.VsCpu, MusicEnergy.Metal, "vscpu-metal", "Competitive duel energy."),
            [WordleGameMode.WordsRoyale] = new(WordleGameMode.WordsRoyale, MusicEnergy.Metal, "royale-metal", "High-stakes survival intensity."),
            [WordleGameMode.MegaBoard] = new(WordleGameMode.MegaBoard, MusicEnergy.Metal, "megaboard-metal", "Chaos-control momentum for multi-board pressure."),
            [WordleGameMode.Survival] = new(WordleGameMode.Survival, MusicEnergy.Metal, "survival-metal", "Escalating run tension."),
            [WordleGameMode.RareHunt] = new(WordleGameMode.RareHunt, MusicEnergy.Metal, "rarehunt-metal", "Aggressive hunt energy."),
            [WordleGameMode.Discovery] = new(WordleGameMode.Discovery, MusicEnergy.Chill, "discovery-chill", "Exploration-oriented mood."),
            [WordleGameMode.WildDictionary] = new(WordleGameMode.WildDictionary, MusicEnergy.Metal, "wild-metal", "Unpredictable mode, heavier soundtrack.")
        };

    public static ModeMusicProfile GetProfile(WordleGameMode mode) => Profiles[mode];

    public static IReadOnlyCollection<ModeMusicProfile> GetAllProfiles() => Profiles.Values.ToArray();

    public static IReadOnlyCollection<ModeMusicProfile> GetProfilesByEnergy(MusicEnergy energy) =>
        Profiles.Values.Where(profile => profile.Energy == energy).ToArray();
}
