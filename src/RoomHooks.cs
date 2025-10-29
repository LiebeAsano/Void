using Kittehface.Build;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using System.Linq;
using UnityEngine;
using VoidTemplate.Objects;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate
{
    static class RoomHooks
    {
        public static void Hook()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.Room.Loaded += RoomSpeficScript;
            On.TempleGuardAI.Update += TempleGuardAI_Update;
            On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
            On.RegionGate.customOEGateRequirements += RegionGate_customOEGateRequirements;
            On.World.ctor += World_ctor;
            IL.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += OE_GourmandEnding_Update;
            On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += On_OE_GourmandEnding_Update;
            On.Player.ctor += Player_ctor;
            On.RainWorldGame.BeatGameMode += RainWorldGame_BeatGameMode;
            On.AbstractCreatureAI.Update += AbstractCreatureAI_Update;
            On.AbstractCreatureAI.SetDestination += AbstractCreatureAI_SetDestination;
        }

        private static void AbstractCreatureAI_SetDestination(On.AbstractCreatureAI.orig_SetDestination orig, AbstractCreatureAI self, WorldCoordinate newDest)
        {
            if (self.world.game.overWorld.worldLoader != null && !self.world.game.overWorld.worldLoader.Finished && self.world.game.overWorld.worldLoader.world == self.world)
            {
                return;
            }
            orig(self, newDest);
        }

        private static void AbstractCreatureAI_Update(On.AbstractCreatureAI.orig_Update orig, AbstractCreatureAI self, int time)
        {
            if (self.world.game.overWorld.worldLoader != null && !self.world.game.overWorld.worldLoader.Finished && self.world.game.overWorld.worldLoader.world == self.world)
            {
                return;
            }
            orig(self, time);
        }

        private static void RainWorldGame_BeatGameMode(On.RainWorldGame.orig_BeatGameMode orig, RainWorldGame game, bool standardVoidSea)
        {
            if (standardVoidSea)
            {
                if (game.StoryCharacter == SlugcatStats.Name.White) SaveManager.ExternalSaveData.SurvAscended = true;
                else if (game.StoryCharacter == SlugcatStats.Name.Yellow) SaveManager.ExternalSaveData.MonkAscended = true;
            }
            orig(game, standardVoidSea);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.IsVoid() && abstractCreature.Room.name == "OE_FINAL03")
            {
                self.sleepCounter = 100;
            }
        }

        private static void On_OE_GourmandEnding_Update(On.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.orig_Update orig, MSCRoomSpecificScript.OE_GourmandEnding self, bool eu)
        {
            if (self.room.game.IsVoidStoryCampaign() && self.room.game.GetStorySession.saveState.GetVoidEndingTree() && !self.endTrigger)
            {
                self.Destroy();
                return;
            }
            orig(self, eu);
        }

        private static void OE_GourmandEnding_Update(ILContext il)
        {
            ILCursor c1 = new(il);
            if (c1.TryGotoNext(MoveType.After, x => x.MatchStfld<MSCRoomSpecificScript.OE_GourmandEnding>(nameof(MSCRoomSpecificScript.OE_GourmandEnding.spawnedNPCs))))
            {
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate((MSCRoomSpecificScript.OE_GourmandEnding self) =>
                {
                    if (self.room.game.IsVoidStoryCampaign())
                    {
                        AbstractCreature npcGourmand = new(self.room.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC), null, self.room.GetWorldCoordinate(new Vector2(325, 175)), self.room.game.GetNewID());
                        (npcGourmand.state as PlayerNPCState).forceFullGrown = true;
                        (npcGourmand.state as PlayerNPCState).slugcatCharacter = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
                        new Player(npcGourmand, npcGourmand.world)
                        {
                            SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Gourmand,
                            standing = true,
                            bodyMode = Player.BodyModeIndex.Stand
                        };
                        npcGourmand.abstractAI.RealAI = new SlugNPCAI(npcGourmand, npcGourmand.world);
                        self.room.abstractRoom.AddEntity(npcGourmand);
                        npcGourmand.RealizeInRoom();
                        (npcGourmand.abstractAI as SlugNPCAbstractAI).toldToStay = npcGourmand.pos;
                    }
                });
            }
            else LogExErr("Error in IL hook. Gourmand won't able to appear.");

            ILCursor c2 = new(il);
            for (int i = 0; i < 3; i++)
            {
                if (c2.TryGotoNext(MoveType.After, x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))))
                {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.EmitDelegate((bool orig, MSCRoomSpecificScript.OE_GourmandEnding self) =>
                    {
                        return orig || self.room.game.IsVoidStoryCampaign();
                    });
                }
                else logerr($"{nameof(VoidTemplate)}.{nameof(RoomHooks)}.{nameof(OE_GourmandEnding_Update)}: {i} match error");
            }
            if (c2.TryGotoNext(MoveType.Before, x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToRedsGameOver))))
            {
                c2.Emit(OpCodes.Dup);
                c2.EmitDelegate((RainWorldGame game) =>
                {
                    if (game.IsVoidStoryCampaign() && !game.GetStorySession.saveState.GetVoidEndingTree() && GameFeatures.IntroScene.TryGet(game, out var outro))
                    {
                        //game.manager.nextSlideshow = outro;
                        RainWorldGame.ForceSaveNewDenLocation(game, "OE_FINAL03", false);
                        game.GetStorySession.saveState.SetVoidEndingTree(true);
                        for (int i = 0; i < 8; i++)
                        {
                            game.GetStorySession.playerSessionRecords[0].kills.Add(new(new(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0), new(-1, -1), false));
                        }
                    }
                });
            }
            else logerr($"{nameof(VoidTemplate)}.{nameof(RoomHooks)}.{nameof(OE_GourmandEnding_Update)}: 4 match error");
        }

        private static void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
        {
            orig(self, game, region, name, singleRoomWorld);
            if (name == "OE" && game != null && game.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void && !session.saveState.GetOEUnlockForVoid())
            {
                session.saveState.SetOEUnlockForVoid(true);
            }
        }

        private static bool RegionGate_customOEGateRequirements(On.RegionGate.orig_customOEGateRequirements orig, RegionGate self)
        {
            return orig(self) || (self.room.game.session is StoryGameSession session && session.saveStateNumber == VoidEnums.SlugcatID.Void && session.saveState.GetOEUnlockForVoid());
        }

        private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
        {
            if (crit.representedCreature.realizedCreature is Player player && player.IsVoid())
            {
                return 500f;
            }
            return orig(self, crit);
        }

        private static void TempleGuardAI_Update(On.TempleGuardAI.orig_Update orig, TempleGuardAI self)
        {
            orig(self);
            if (self.guard.room.PlayersInRoom.Any(i => i.IsVoid()))
            {
                self.patience = 9999;
            }
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            for (int i = 0; i < self.Players.Count; i++)
            {
                if (self.Players[i].Room.name == "OE_FINAL03")
                {
                    var spawnPos = Room.StaticGetTilePosition(new Vector2(325, 175));
                    self.Players[i].pos.Tile = new(spawnPos.x, spawnPos.y + i);
                }
            }
        }

        private static void RoomSpeficScript(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self.game != null && self.game.session.characterStats.name == VoidEnums.SlugcatID.Void)
            {
                SaveState saveState = self.game.GetStorySession.saveState;
                if (!saveState.GetCeilClimbMessageShown() && self.game.Players.Exists(x => x.realizedCreature is Player p && p.IsVoid() && p.KarmaCap == 4 && !self.game.rainWorld.ExpeditionMode))
                {
                    self.AddObject(new Tutorial(self,
                    [
                        new("Your body is strong enough to climb on any surface.", 33, 333),
                        new("Your abilities also continue to improve.", 0, 333)
                    ]));
                    saveState.SetCeilClimbMessageShown(true);
                }
                switch (self.abstractRoom.name)
                {
                    case "SB_E05" when !saveState.GetStartClimbingMessageShown():
                        self.AddObject(new ClimbTutorial(self));
                        saveState.SetStartClimbingMessageShown(true);
                        break;
                    case VoidEnums.RoomNames.EndingRoomName:
                        self.AddObject(new Ending(self));
                        break;
                    case "SL_AI" when !saveState.GetDreamData(SaveManager.Dream.Moon).WasShown:
                        self.AddObject(new EnqueueMoonDream(self));
                        break;
                }

            }
        }

    }
}
