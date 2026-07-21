using Words.Core.Models;
using Words.Core.Services;

namespace Words.Tests;

public class ModeMusicProfileServiceTests
{
    [Fact]
    public void GetAllProfiles_CoversEveryGameMode()
    {
        var modes = Enum.GetValues<WordleGameMode>();
        var profiles = ModeMusicProfileService.GetAllProfiles();

        Assert.Equal(modes.Length, profiles.Count);
        foreach (var mode in modes)
            Assert.Equal(mode, ModeMusicProfileService.GetProfile(mode).Mode);
    }

    [Fact]
    public void GetProfilesByEnergy_SeparatesChillAndMetalBuckets()
    {
        var chill = ModeMusicProfileService.GetProfilesByEnergy(MusicEnergy.Chill)
            .Select(profile => profile.Mode)
            .ToHashSet();
        var metal = ModeMusicProfileService.GetProfilesByEnergy(MusicEnergy.Metal)
            .Select(profile => profile.Mode)
            .ToHashSet();

        Assert.Contains(WordleGameMode.Classic, chill);
        Assert.Contains(WordleGameMode.Daily, chill);
        Assert.Contains(WordleGameMode.Discovery, chill);
        Assert.Contains(WordleGameMode.WordsRoyale, metal);
        Assert.Contains(WordleGameMode.MegaBoard, metal);
        Assert.DoesNotContain(WordleGameMode.WordsRoyale, chill);
    }
}
