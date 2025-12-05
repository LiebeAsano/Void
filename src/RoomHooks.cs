using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics;
using static VoidTemplate.SaveManager;
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
            On.Player.Update += On_Player_Update;
            On.RainWorldGame.BeatGameMode += RainWorldGame_BeatGameMode;
            On.AbstractCreatureAI.Update += AbstractCreatureAI_Update;
            On.AbstractCreatureAI.SetDestination += AbstractCreatureAI_SetDestination;
            On.OverWorld.LoadFirstWorld += OverWorld_LoadVoidDreamWorld;
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues;
            On.OverWorld.GetRegion_string += OverWorld_GetRegion_string_FIX;
            IL.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut_FIX;
            IL.Player.Update += Player_Update;
            On.Music.PlayerThreatTracker.Update += FixWorldMusic;
        }

        private static void FixWorldMusic(On.Music.PlayerThreatTracker.orig_Update orig, Music.PlayerThreatTracker self)
        {
            orig(self);
            if (VoidDreamScript.IsVoidDream && self.musicPlayer.manager.currentMainLoop is RainWorldGame game && game.Players[self.playerNumber].realizedCreature is Player player && player.room != null && player.room.world.singleRoomWorld)
            {
                if (player.room.abstractRoom.index != self.room)
                {
                    self.lastLastRoom = self.lastRoom;
                    self.lastRoom = self.room;
                    self.room = player.room.abstractRoom.index;
                    if (self.room != self.lastLastRoom)
                    {
                        self.roomSwitches++;
                        string text2 = ((player.room.world.region.regionParams.proceduralMusicBank == "") ? player.room.world.region.name : player.room.world.region.regionParams.proceduralMusicBank);
                        if (text2 != self.region)
                        {
                            self.region = text2;
                            self.musicPlayer.NewRegion(self.region);
                        }
                    }
                }
                if (self.roomSwitches > 0 && self.roomSwitchDelay > 0)
                {
                    self.roomSwitchDelay--;
                    if (self.roomSwitchDelay < 1)
                    {
                        self.musicPlayer.song?.PlayerToNewRoom();
                        self.musicPlayer.nextSong?.PlayerToNewRoom();
                        self.roomSwitchDelay = UnityEngine.Random.Range(80, 400);
                        self.roomSwitches--;
                    }
                }
            }
        }

        private static void Player_Update(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel cancel = c.DefineLabel();
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<World>("region"),
                x => x.MatchBrfalse(out cancel)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player self) =>
                {
                    return self.abstractCreature.world.regionState != null;
                });
                c.Emit(OpCodes.Brfalse, cancel);
            }
            else LogExErr("IL Hook match error");
        }

        private static void Player_SpitOutOfShortCut_FIX(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel cancel = c.DefineLabel();
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<World>("region"),
                x => x.MatchBrfalse(out cancel)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player self) =>
                {
                    return self.abstractCreature.world.regionState != null;
                });
                c.Emit(OpCodes.Brfalse, cancel);
            }
            else LogExErr("IL Hook match error");
        }

        private static Region OverWorld_GetRegion_string_FIX(On.OverWorld.orig_GetRegion_string orig, OverWorld self, string rName)
        {
            rName = Regex.Split(rName, "_")[0];
            return orig(self, rName);
        }

        private static void WorldLoader_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, SlugcatStats.Timeline timelinePosition, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (VoidDreamScript.IsVoidDream)
            {
                self.roomAdder = [];
                self.roomTags = [];
                self.swarmRoomsList = [];
                self.sheltersList = [];
                self.gatesList = [];
                self.faultyExits = [];
                self.tempBatBlocks = [];
                self.spawners = [];
                self.abstractRooms = [];
                self.activity = WorldLoader.Activity.Init;

                self.game = game;
                self.timelinePosition = timelinePosition;
                self.playerCharacter = playerCharacter;
                self.creatureStats = new float[ExtEnum<CreatureTemplate.Type>.values.Count + 5];
                self.ConditionalLinkList = [];
                self.ReplaceRoomNames = [];
                float num = 0f;
                int num2 = 0;
                float num3 = 0f;
                float num4 = 0f;
                if (ModManager.PrecycleModule && game != null)
                {
                    num = game.globalRain.preCycleRainPulse_Scale;
                    if (game.overWorld != null && game.world != null)
                    {
                        num2 = game.world.rainCycle.sunDownStartTime;
                        num3 = game.globalRain.drainWorldFlood;
                        num4 = game.globalRain.drainWorldFlood;
                    }
                }
                self.world = new World(game, region, worldName, singleRoomWorld);
                if (game != null)
                {
                    game.timeInRegionThisCycle = 0;
                }
                if (ModManager.PrecycleModule)
                {
                    if (self.game != null && self.game.overWorld != null && self.game.world != null)
                    {
                        game.globalRain.preCycleRainPulse_Scale = num;
                        self.world.rainCycle.sunDownStartTime = num2;
                        game.globalRain.drainWorldFlood = num3;
                        game.globalRain.drainWorldFlood = num4;
                        Custom.Log(
                        [
                "Loaded world, transfering precycle scale",
                self.game.globalRain.preCycleRainPulse_Scale.ToString()
                        ]);
                    }
                    else
                    {
                        Custom.Log(["First world loaded, holding precycle scale."]);
                    }
                }
                self.singleRoomWorld = singleRoomWorld;
                self.worldName = worldName;
                self.setupValues = setupValues;
                self.lines = [];
                if (!singleRoomWorld)
                {
                    string[] array = File.ReadAllLines(AssetManager.ResolveFilePath(string.Concat(
                    [
            "World",
            Path.DirectorySeparatorChar.ToString(),
            worldName,
            Path.DirectorySeparatorChar.ToString(),
            "world_",
            worldName,
            ".txt"
                    ])));
                    for (int i = 0; i < array.Length; i++)
                    {
                        string text = WorldLoader.Preprocessing.PreprocessLine(array[i], game, timelinePosition);
                        if (text != null)
                        {
                            self.lines.Add(text);
                        }
                    }
                }
                if (!singleRoomWorld)
                {
                    self.simulateUpdateTicks = 100;
                }
                Dictionary<string, List<string>> dictionary = [];
                for (int j = self.lines.Count - 1; j > 0; j--)
                {
                    string[] array2 = Regex.Split(self.lines[j], " : ");
                    if (array2.Length == 3 && !(array2[1] != "EXCLUSIVEROOM"))
                    {
                        if (!dictionary.ContainsKey(array2[2]))
                        {
                            dictionary[array2[2]] = [.. array2[0].Split(',')];
                        }
                        else
                        {
                            dictionary[array2[2]].AddRange(array2[0].Split([',']));
                        }
                        self.lines.RemoveAt(j);
                    }
                }
                if (dictionary.Count > 0)
                {
                    int num5 = -1;
                    for (int k = 0; k < self.lines.Count; k++)
                    {
                        if (self.lines[k] == "END CONDITIONAL LINKS")
                        {
                            num5 = k - 1;
                            break;
                        }
                    }
                    if (num5 != -1)
                    {
                        foreach (KeyValuePair<string, List<string>> keyValuePair in dictionary)
                        {
                            self.lines.Insert(num5, string.Join(",", keyValuePair.Value) + " : EXCLUSIVEROOM : " + keyValuePair.Key);
                        }
                    }
                }
                MapExporter.WorldLoader_ctor(self, self.lines);
                return;
            }
            orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
        }

        private static void OverWorld_LoadVoidDreamWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            if (VoidDreamScript.IsVoidDream)
            {
                self.LoadWorld("LWNM_VoidNightmare", self.PlayerCharacterNumber, self.PlayerTimelinePosition, true);
                self.FIRSTROOM = "LWNM_VoidNightmare";
                return;
            }
            orig(self);
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
                if (game.StoryCharacter == SlugcatStats.Name.White) ExternalSaveData.SurvAscended = true;
                else if (game.StoryCharacter == SlugcatStats.Name.Yellow) ExternalSaveData.MonkAscended = true;
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

        private static void On_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.room?.game?.IsVoidWorld() == true)
            {
                if (self.abstractCreature?.Room?.name == "OE_FINAL03")
                {
                    if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand &&
                        self.AI != null)
                    {
                        self.standing = true;
                        self.sleepCounter = 0;
                        self.bodyMode = Player.BodyModeIndex.Stand;
                    }
                }
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
                        if (game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            game.GetStorySession.saveState.SetVoidExtraFood(3);
                            game.GetStorySession.saveState.SetVoidFoodToHibernate(6);
                            game.GetStorySession.saveState.SetKarmaToken(0);
                        }
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
            if (VoidDreamScript.IsVoidDream) manager.musicPlayer?.FadeOutAllSongs(5);
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
            if (VoidDreamScript.IsVoidDream)
            {
                self.AddObject(new VoidDreamScript());
            }
        }

    }
}
