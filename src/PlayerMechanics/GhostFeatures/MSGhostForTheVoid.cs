using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
    internal static class MSGhostForTheVoid
    {
        public static void Hook()
        {
            IL.World.SpawnGhost += World_SpawnGhost;

            On.World.CheckForRegionGhost += World_CheckForRegionGhost;
        }

        private static bool World_CheckForRegionGhost(On.World.orig_CheckForRegionGhost orig, SlugcatStats.Name slugcatIndex, string regionString)
        {
            bool baseResult = orig(slugcatIndex, regionString);

            GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(regionString);

            if (ModManager.MSC && ghostID == MoreSlugcats.MoreSlugcatsEnums.GhostID.MS && slugcatIndex == VoidEnums.SlugcatID.TheVoid)
            {
                return true;
            }

            return baseResult;
        }

        private static void World_SpawnGhost(ILContext il)
        {
            ILLabel MSEchoCheckPassLabel = null;
            ILCursor iLCursor = new(il);
            if (iLCursor.TryGotoNext(MoveType.Before,
                c => c.MatchLdloc(0),
                c => c.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.GhostID>(nameof(MoreSlugcats.MoreSlugcatsEnums.GhostID.MS)),
                c => c.MatchCall("ExtEnum`1<GhostWorldPresence/GhostID>", "op_Equality"),
                c => c.MatchBrfalse(out MSEchoCheckPassLabel)))
            {
                List<ILLabel> beginLabels = iLCursor.IncomingLabels.ToList();
                iLCursor.Emit(OpCodes.Ldarg_0);

                foreach (ILLabel iLLabel in beginLabels)
                {
                    iLLabel.Target = iLCursor.Previous;
                }

                iLCursor.EmitDelegate(TheVoidPassCheckForMSEcho);
                iLCursor.Emit(OpCodes.Brtrue, MSEchoCheckPassLabel);
            }
            else
            {
                logerr("Failed to IL-hook MS Ghost campaign availability check, MS Ghost won't be available for The Void");
            }
        }

        private static bool TheVoidPassCheckForMSEcho(World self)
        {
            if (ThisIsVoidCampaign(self))
            {
                return TheVoidCanMeetMSEcho(self);
            }

            return false;
        }

        private static bool ThisIsVoidCampaign(World self)
        {
            return (self.game.session as StoryGameSession).saveStateNumber == VoidEnums.SlugcatID.TheVoid;
        }

        private static bool TheVoidCanMeetMSEcho(World self)
        {
            DeathPersistentSaveData deathPersistentSaveData = (self.game.session as StoryGameSession).saveState.deathPersistentSaveData;
            return !deathPersistentSaveData.theMark;
        }
    }
}
