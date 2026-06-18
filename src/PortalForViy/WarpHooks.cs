using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watcher;
using VoidTemplate.Useful;
using Mono.Cecil.Cil;

namespace VoidTemplate.PortalForViy
{
    public class WarpHooks
    {
        public static void Hook()
        {
            IL.Room.TrySpawnWarpPoint_PlacedObject_bool += Room_TrySpawnWarpPoint_PlacedObject_bool;
            On.Room.TrySpawnWarpPoint_PlacedObject_bool += Room_TrySpawnWarpPoint_PlacedObject_bool1;
        }

        private static WarpPoint Room_TrySpawnWarpPoint_PlacedObject_bool1(On.Room.orig_TrySpawnWarpPoint_PlacedObject_bool orig, Room self, PlacedObject po, bool saveInRegionState)
        {
            var warp = orig(self, po, saveInRegionState);
            return warp;
        }

        private static void Room_TrySpawnWarpPoint_PlacedObject_bool(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel skip = null;
            if (c.TryGotoNext(x => x.MatchLdfld<WarpPoint.WarpPointData>(nameof(WarpPoint.WarpPointData.wasNonDynamicWarpBeforeWeaverTriggered)),
                x => x.MatchBrfalse(out skip)))
            {
                if (c.TryGotoPrev(MoveType.Before,
                    x => x.MatchLdloc(0),
                    x => x.MatchCallvirt<WarpPoint.WarpPointData>("get_nonDynamicWarpPoint")))
                {
                    c.MoveAfterLabels();
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((Room self) =>
                    {
                        return self.abstractRoom.name == "MS_COMMS" && self.game.IsVoidWorld();
                    });
                    c.Emit(OpCodes.Brtrue, skip);
                    Utils.LogExInf(il.ToString());
                }
                else Utils.LogExErr("Error in second match IL hook");
            }
            else Utils.LogExErr("Error in first match IL hook");
        }
    }
}
