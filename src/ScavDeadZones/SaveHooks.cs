using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.ScavDeadZones
{
    public class SaveHooks
    {
        public static void Hook()
        {
            On.SaveState.SessionEnded += SaveState_SessionEnded;
        }

        private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (survived)
            {
                foreach (var state in self.GetScavRegionStates().Values)
                {
                    state.CycleTick(self);
                }
            }
            orig(self, game, survived, newMalnourished);
        }
    }
}
