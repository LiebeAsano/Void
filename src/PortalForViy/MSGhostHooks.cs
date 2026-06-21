using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;
using Watcher;

namespace VoidTemplate.PortalForViy
{
    public class MSGhostHooks
    {
        public static void Hook()
        {
            On.Ghost.FadeOutFinished += Ghost_FadeOutFinished;
            On.Ghost.Update += Ghost_Update;
        }

        private static void Ghost_Update(On.Ghost.orig_Update orig, Ghost self, bool eu)
        {
            if (self.worldGhost.ghostID == MoreSlugcatsEnums.GhostID.MS && self.room.game.IsVoidStoryCampaign() && self.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap < 9 && !self.hasRequestedShutDown && self.room.BeingViewed)
            {
                self.hasRequestedShutDown = true;
                self.room.game.GetStorySession.saveState.sessionEndingFromSpinningTopEncounter = true;
                self.room.game.Win(false, false);
                RainWorldGame.ForceSaveNewDenLocation(self.room.game, "MS_S07", false);
            }
            orig(self, eu);
        }

        private static void Ghost_FadeOutFinished(On.Ghost.orig_FadeOutFinished orig, Ghost self)
        {
            if (self.worldGhost.ghostID == MoreSlugcatsEnums.GhostID.MS && self.room.game.IsVoidStoryCampaign())
            {
                var warp = new PlacedObject(PlacedObject.Type.WarpPoint, null)
                {
                    pos = self.placedObject.pos
                };
                var data = warp.data as WarpPoint.WarpPointData;
                data.rippleWarp = true;
                data.accessibility = WarpPoint.WarpPointData.WarpPointSpawnCondition.AnySlugcat;
                var warpID = WarpPoint.IdentifyingString(self.room.game, data, self.room.abstractRoom);
                self.room.game.GetStorySession.saveState.deathPersistentSaveData.spawnedWarpPoints.Add(warpID, warp.ToString());
                self.hasRequestedShutDown = true;
                self.room.game.sawAGhost = self.worldGhost.ghostID;
                self.room.game.GetStorySession.saveState.sessionEndingFromSpinningTopEncounter = true;
                self.room.game.GhostShutDown(self.worldGhost.ghostID);
                RainWorldGame.ForceSaveNewDenLocation(self.room.game, self.room.abstractRoom.name, false);
                return;
            }
            orig(self);
        }
    }
}
