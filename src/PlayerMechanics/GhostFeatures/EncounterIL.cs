using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
    internal static class EncounterIL
    {
        public static void Hook()
        {
            IL.SaveState.GhostEncounter += SaveState_GhostEncounterIL;
        }

        private static void SaveState_GhostEncounterIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdcI4(9));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, SaveState, int>>((re, self) =>
                    self.saveStateNumber == VoidEnums.SlugcatID.TheVoid ? 10 : re);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            ILCursor c2 = new ILCursor(il);
            if (c2.TryGotoNext(MoveType.Before, i => i.MatchStfld(typeof(DeathPersistentSaveData).GetField(nameof(DeathPersistentSaveData.karma)))))
            {
                c2.Emit(OpCodes.Ldarg_0);
                c2.EmitDelegate(KarmaRefillControl);
            }
            else
            {
                _Plugin.logger.LogError("Ghost encounter karma update match failed. The Void's echo vs karma interaction will be broken.");
            }

        }

        private static int KarmaRefillControl(int unmodifiedNewKarma, SaveState self)
        {
            int theVoidNewKarma = Custom.IntClamp(self.deathPersistentSaveData.karma + 1, 0, self.deathPersistentSaveData.karmaCap);
            return self.saveStateNumber == VoidEnums.SlugcatID.TheVoid ? theVoidNewKarma : unmodifiedNewKarma;
        }
    }
}
