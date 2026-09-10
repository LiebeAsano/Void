using System;
using Watcher;
using VoidTemplate.Useful;
using RuntimeHook = MonoMod.RuntimeDetour.Hook;

namespace VoidTemplate.PortalForViy;

public class WarpHooks
{
    private static RuntimeHook sleepRevealMapHook;

    public static void Hook()
    {
        On.Room.TrySpawnWarpPoint_PlacedObject_bool += Room_TrySpawnWarpPoint_PlacedObject_bool;

        sleepRevealMapHook ??= new RuntimeHook(typeof(Menu.SleepAndDeathScreen).GetProperty(nameof(Menu.SleepAndDeathScreen.RevealMap)).GetGetMethod(), SleepAndDeathScreen_RevealMap);
    }

    private static bool SleepAndDeathScreen_RevealMap(Func<Menu.SleepAndDeathScreen, bool> orig, Menu.SleepAndDeathScreen self)
    {
        if (self.IsSleepScreen && self.saveState?.saveStateNumber == VoidEnums.SlugcatID.Void)
            return false;

        return orig(self);
    }

    private static WarpPoint Room_TrySpawnWarpPoint_PlacedObject_bool(On.Room.orig_TrySpawnWarpPoint_PlacedObject_bool orig, Room self, PlacedObject po, bool saveInRegionState)
    {
        if (self.abstractRoom.name != "MS_COMMS" || !self.game.IsVoidWorld())
            return orig(self, po, saveInRegionState);

        WarpPoint.WarpPointData warpPointData = po.data as WarpPoint.WarpPointData;

        string b = WarpPoint.IdentifyingString(self.game, warpPointData, self.abstractRoom);

        foreach (WarpPoint warpPoint in self.warpPoints)
        {
            if (warpPoint.MyIdentifyingString() == b)
            {
                string destRoom = warpPoint.Data.destRoom;
                string a = destRoom?.ToLowerInvariant();

                string destRoom2 = warpPointData.destRoom;

                if (a == destRoom2?.ToLowerInvariant()) return warpPoint;
            }

            string destRoom3 = warpPoint.Data.destRoom;
            string a2 = destRoom3?.ToLowerInvariant();

            string destRoom4 = warpPointData.destRoom;

            if (a2 == destRoom4?.ToLowerInvariant()) return warpPoint;
        }

        return self.ForceSpawnWarpPoint(po, saveInRegionState);
    }
}