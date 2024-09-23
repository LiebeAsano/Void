using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
    internal static class _GhostFeaturesMeta
    {
        public static void Hook()
        {
            ConversationPath.Hook();
            EncounterIL.Hook();
            KarmaLadderNonRefillCapIncrease.Hook();
            UpdateIL.Hook();
        }
    }
}
