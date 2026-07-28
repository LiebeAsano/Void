using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding.Passages;

namespace VoidTemplate.ScavDeadZones
{
    public class ExtinctionPassage : CustomPassage
    {
        public override WinState.EndgameID ID => VoidEnums.CustomPassageID.Extinction;

        public override string DisplayName => "Extinction";

        public override WinState.EndgameTracker CreateTracker() => new WinState.FloatTracker(ID, 0, 0, 0, 1);

        public override void OnWin(WinState winState, RainWorldGame game, WinState.EndgameTracker tracker)
        {
            var myTracker = tracker as WinState.FloatTracker;
            if (!myTracker.GoalAlreadyFullfilled)
            {
                if (game.GetStorySession.saveState.TryGetScavRegionState(game.world.name, out var state))
                {
                    myTracker.SetProgress(1 - state.deadCount);
                }
                else
                {
                    myTracker.SetProgress(0);
                }
            }
        }

        public override WinState.EndgameID[] RequiredPassages => [ WinState.EndgameID.Survivor ];
    }
}
