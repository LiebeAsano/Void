using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
    public class GhostPingControlData
    {
        public string playerLastUpdateRegion = "";
        public bool ghostPingStaged = false;
    }

    public static class MSGhostForTheVoid
    {
        private static ConditionalWeakTable<Player, GhostPingControlData> ghostPingControlDataCWT = new();

        public static void Hook()
        {
            IL.World.SpawnGhost += World_SpawnGhost;

            On.GhostWorldPresence.ctor_World_GhostID += GhostWorldPresence_ctor_World_GhostID;

            On.World.CheckForRegionGhost += World_CheckForRegionGhost;

            IL.Room.Loaded += Room_Loaded;

            On.ShelterDoor.Update += ShelterDoor_Update;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }

        private static void GhostWorldPresence_ctor_World_GhostID(On.GhostWorldPresence.orig_ctor_World_GhostID orig, GhostWorldPresence self, World world, GhostWorldPresence.GhostID ghostID)
        {
            orig(self, world, ghostID);

            if (world.game.session is StoryGameSession storyGameSession)
            {
                if (storyGameSession.saveStateNumber == VoidEnums.SlugcatID.Void && ghostID == MoreSlugcatsEnums.GhostID.MS)
                {
                    self.songName = "Void_Echo_Music";
                }
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);

            if (self.room?.game?.session is StoryGameSession storyGameSession && storyGameSession.saveState.saveStateNumber == VoidEnums.SlugcatID.Void
                && self.room.world.region.name == "MS"
                && TheVoidCanMeetMSGhost(self.room.world) && !HasMetMSGhost(storyGameSession.saveState.deathPersistentSaveData))
            {
                GhostPingControlData ghostPingControlData = ghostPingControlDataCWT.GetValue(self, (player) => new GhostPingControlData() { playerLastUpdateRegion = player.room.world.region.name });

                if (self.room.world.region.name != ghostPingControlData.playerLastUpdateRegion)
                {
                    ghostPingControlData.playerLastUpdateRegion = self.room.world.region.name;
                    ghostPingControlData.ghostPingStaged = true;
                }

                if (ghostPingControlData.ghostPingStaged && !self.room.IsGateRoom())
                {
                    ghostPingControlData.ghostPingStaged = false;
                    self.room.AddObject(new GhostPing(self.room));
                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            ghostPingControlDataCWT.Add(self, new GhostPingControlData() { playerLastUpdateRegion = world.region?.name });
        }

        private static void ShelterDoor_Update(On.ShelterDoor.orig_Update orig, ShelterDoor self, bool eu)
        {
            float frameStartClosedFac = self.closedFac;

            orig(self, eu);

            float frameEndClosedFac = self.closedFac;

            if (frameStartClosedFac >= 0.04f && frameEndClosedFac < 0.04f
                &&
                self.room.game.session is StoryGameSession storyGameSession && storyGameSession.saveStateNumber == VoidEnums.SlugcatID.Void)
            {
                if (!HasMetMSGhost(storyGameSession.saveState.deathPersistentSaveData) && self.room.world.region.name == "MS"
                    && TheVoidCanMeetMSGhost(self.room.world))
                {
                    self.room.AddObject(new GhostPing(self.room));
                }
            }
        }

        private static void Room_Loaded(ILContext il)
        {
            ILCursor iLCursor = new(il);

            if (iLCursor.TryGotoNext(MoveType.After,
                c => c.MatchLdarg(0),
                c => c.MatchLdfld<Room>(nameof(Room.game)),
                c => c.MatchCallvirt(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.world)).GetMethod),
                c => c.MatchLdfld<World>(nameof(World.worldGhost)),
                c => c.MatchBrfalse(out _)))
            {
                ILLabel doNotPlayGhostHunchLabel = null;

                if (iLCursor.TryGotoNext(MoveType.After,
                    c => c.MatchLdarg(0),
                    c => c.MatchLdfld<Room>(nameof(Room.world)),
                    c => c.MatchLdfld<World>(nameof(World.region)),
                    c => c.MatchBrfalse(out doNotPlayGhostHunchLabel)))
                {
                    List<ILLabel> labelsToRetarget = iLCursor.IncomingLabels.ToList();

                    iLCursor.Emit(OpCodes.Ldarg_0);

                    foreach (ILLabel label in labelsToRetarget)
                    {
                        label.Target = iLCursor.Previous;
                    }

                    iLCursor.EmitDelegate(DoNotPlayGhostHunch);
                    iLCursor.Emit(OpCodes.Brtrue, doNotPlayGhostHunchLabel);

                    return;
                }
            }

            logerr("Failed to patch Room.Loaded logics for GhostSpot. The Void may receive erroneus Ghost Hunch.");
        }

        private static bool DoNotPlayGhostHunch(Room self)
        {
            if (self.world.game.session is StoryGameSession storyGameSession)
            {
                return self.world.region.name == "MS" && storyGameSession.saveStateNumber == VoidEnums.SlugcatID.Void;
            }

            return false;
        }

        private static bool World_CheckForRegionGhost(On.World.orig_CheckForRegionGhost orig, SlugcatStats.Name slugcatIndex, string regionString)
        {
            bool baseResult = orig(slugcatIndex, regionString);

            GhostWorldPresence.GhostID ghostID = GhostWorldPresence.GetGhostID(regionString);

            if (ModManager.MSC && ghostID == MoreSlugcats.MoreSlugcatsEnums.GhostID.MS && slugcatIndex == VoidEnums.SlugcatID.Void)
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

                iLCursor.EmitDelegate(TheVoidPassCheckForMSGhost);
                iLCursor.Emit(OpCodes.Brtrue, MSEchoCheckPassLabel);
            }
            else
            {
                logerr("Failed to IL-hook MS Ghost campaign availability check, MS Ghost won't be available for The Void");
            }
        }

        private static bool TheVoidPassCheckForMSGhost(World self)
        {
            if (ThisIsVoidCampaign(self))
            {
                return TheVoidCanMeetMSGhost(self);
            }

            return false;
        }

        private static bool ThisIsVoidCampaign(World self)
        {
            return (self.game.session as StoryGameSession).saveStateNumber == VoidEnums.SlugcatID.Void;
        }

        private static bool TheVoidCanMeetMSGhost(World self)
        {
            SaveState saveState = (self.game.session as StoryGameSession).saveState;
            DeathPersistentSaveData deathPersistentSaveData = (self.game.session as StoryGameSession).saveState.deathPersistentSaveData;
            return !deathPersistentSaveData.theMark && !saveState.GetPunishNonPermaDeath();
        }

        private static bool HasMetMSGhost(DeathPersistentSaveData deathPersistentSaveData)
        {
            if (deathPersistentSaveData.ghostsTalkedTo.TryGetValue(MoreSlugcats.MoreSlugcatsEnums.GhostID.MS, out int metStatus))
            {
                return metStatus == 2;
            }

            return false;
        }
    }
}
