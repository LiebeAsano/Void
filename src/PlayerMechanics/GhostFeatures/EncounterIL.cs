using Mono.Cecil.Cil;
using MonoMod.Cil;
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
        }
    }
}
