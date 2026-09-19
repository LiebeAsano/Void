using Modding.Passages;
using UnityEngine;

namespace VoidTemplate.RainCycleChanges;

public class TempestPassage : CustomPassage
{
    private const float RainGain = 0.4f;
    private const float SleepLoss = 0.02f;

    private static bool survivedRain;

    public override WinState.EndgameID ID => VoidEnums.CustomPassageID.Tempest;

    public override string DisplayName => "The Tempest";

    public override bool IsAvailableForSlugcat(SlugcatStats.Name name) => name == VoidEnums.SlugcatID.Void;

    public override WinState.EndgameTracker CreateTracker() => new WinState.FloatTracker(ID, 0f, 0f, 0f, 1f);

    public override WinState.EndgameID[] RequiredPassages => [WinState.EndgameID.Survivor];

    public static void Register()
    {
        CustomPassages.Register(new TempestPassage());

        Futile.atlasManager.LoadAtlasFromTexture($"{VoidEnums.CustomPassageID.Tempest}A", new Texture2D(1, 1), false);
        Futile.atlasManager.LoadAtlasFromTexture($"{VoidEnums.CustomPassageID.Tempest}B", new Texture2D(1, 1), false);
    }

    public static void RainCycleCompleted(WinState winState, RainWorldGame game)
    {
        survivedRain = true;

        try
        {
            winState.CycleCompleted(game);
        }
        finally
        {
            survivedRain = false;
        }
    }

    public override void OnWin(WinState winState, RainWorldGame game, WinState.EndgameTracker tracker)
    {
        if (tracker is not WinState.FloatTracker tempest || tempest.GoalFullfilled)
            return;

        tempest.SetProgress(tempest.progress + (survivedRain ? RainGain : -SleepLoss));
    }

    public override void OnDeath(WinState winState, WinState.EndgameTracker tracker)
    {
        if (tracker is WinState.FloatTracker tempest && !tempest.GoalFullfilled)
            tempest.progress *= 0.5f;
    }
}
