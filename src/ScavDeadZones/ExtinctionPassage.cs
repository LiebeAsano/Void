using Modding.Passages;

namespace VoidTemplate.ScavDeadZones;

public class ExtinctionPassage : CustomPassage
{
    public override WinState.EndgameID ID =>
        VoidEnums.CustomPassageID.Extinction;

    public override string DisplayName => "Extinction";

    public override WinState.EndgameTracker CreateTracker() =>  new WinState.FloatTracker(ID, 0f, 0f, 0f, 1f);

    public override void OnWin(WinState winState, RainWorldGame game, WinState.EndgameTracker tracker)
    {
        if (tracker is not WinState.FloatTracker extinction || extinction.GoalAlreadyFullfilled)
            return;
       
        var saveState = game.GetStorySession.saveState;

        float progress = 0f;

        if (saveState.TryGetScavRegionState(game.world.name, out var state))
            progress = 1f - state.deadCount;
        
        extinction.SetProgress(progress);

        if (!extinction.GoalAlreadyFullfilled)
            return;

        ResetChieftain(winState);
        ResetScavengerReputation(game);
    }

    private static void ResetChieftain(WinState winState)
    {
        if (winState.GetTracker(WinState.EndgameID.Chieftain, false) is not WinState.FloatTracker chieftain)
            return;

        chieftain.SetProgress(0f);
        chieftain.lastShownProgress = 0f;
        chieftain.consumed = false;
    }

    private static void ResetScavengerReputation(RainWorldGame game)
    {
        var communities = game.session.creatureCommunities;
        int regionCount = game.rainWorld.progression.regionNames.Length;

        foreach (AbstractCreature abstractPlayer in game.Players)
        {
            if (abstractPlayer.state is not PlayerState playerState)
                continue;

            int playerNumber = playerState.playerNumber;

            communities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, playerNumber, 0f);

            for (int region = 0; region < regionCount; region++)
            {
                communities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, region, playerNumber, 0f);
            }
        }
    }

    public override WinState.EndgameID[] RequiredPassages => [WinState.EndgameID.Survivor];
}