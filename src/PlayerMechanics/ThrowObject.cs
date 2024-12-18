using Expedition;
using Kittehface.Framework20;
using Menu;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class ThrowObject
{
    public static void Hook()
    {
        On.Player.ThrowObject += Player_ThrowObject;
        On.RainWorldGame.Update += RainWorldGame_Update;
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        QuickConnectivity.ResetFrameIterationQuota();
        if (self.setupValues.logSpawned && self.world.logCreatures && !self.world.singleRoomWorld)
        {
            self.world.LogCreatures();
        }
        if (self.IsArenaSession)
        {
            self.GetArenaGameSession.Update();
        }
        if (self.pauseMenu != null)
        {
            self.pauseMenu.Update();
        }
        if (self.GamePaused)
        {
            for (int i = 0; i < self.cameras.Length; i++)
            {
                if (self.cameras[i].hud != null)
                {
                    self.cameras[i].hud.Update();
                }
            }
            for (int j = self.world.activeRooms.Count - 1; j >= 0; j--)
            {
                self.world.activeRooms[j].PausedUpdate();
            }
            return;
        }
        if (!self.processActive)
        {
            return;
        }
        for (int k = 0; k < self.cameras.Length; k++)
        {
            self.cameras[k].Update();
        }
        self.clock++;
        if (self.cameras[0].room != null)
        {
            self.devToolsLabel.text = self.cameras[0].room.abstractRoom.name + " : Dev tools active";
        }
        self.evenUpdate = !self.evenUpdate;
        if (!self.pauseUpdate)
        {
            self.globalRain.Update();
        }
        bool flag = RWInput.CheckPauseButton(0, false);
        if (((flag && !self.lastPauseButton) || Platform.systemMenuShowing) && (self.cameras[0].hud == null || self.IsArenaSession || self.cameras[0].hud.map == null || self.cameras[0].hud.map.fade < 0.1f) && (self.cameras[0].hud == null || self.IsArenaSession || !self.cameras[0].hud.textPrompt.gameOverMode) && self.manager.fadeToBlack == 0f && self.cameras[0].roomSafeForPause)
        {
            self.pauseMenu = new PauseMenu(self.manager, self);
        }
        if (self.consoleVisible)
        {
            self.console.Update();
        }
        self.lastPauseButton = flag;
        if (self.devToolsActive)
        {
            bool key = Input.GetKey("r");
            if (key && !self.lastRestartButton)
            {
                self.RestartGame();
            }
            self.lastRestartButton = key;
        }
        if (self.roomRealizer != null)
        {
            self.roomRealizer.Update();
        }
        if (self.AV != null)
        {
            self.AV.Update();
        }
        if (self.mapVisible)
        {
            self.abstractSpaceVisualizer.Update();
        }
        if (self.abstractSpaceVisualizer.room != self.cameras[0].room && self.cameras[0].room != null)
        {
            self.abstractSpaceVisualizer.ChangeRoom(self.cameras[0].room);
        }
        if (self.IsStorySession)
        {
            self.updateAbstractRoom++;
            if (self.updateAbstractRoom >= self.world.NumberOfRooms)
            {
                self.updateAbstractRoom = 0;
            }
            self.world.GetAbstractRoom(self.updateAbstractRoom + self.world.firstRoomIndex).Update(self.world.NumberOfRooms);
        }
        else
        {
            self.world.GetAbstractRoom(0).Update(1);
            if (self.world.rainCycle.timer > 100)
            {
                self.world.offScreenDen.Update(1);
            }
        }
        for (int l = 0; l < self.world.worldProcesses.Count; l++)
        {
            self.world.worldProcesses[l].Update();
        }
        self.world.rainCycle.Update();
        self.overWorld.Update();
        self.pathfinderResourceDivider.Update();
        self.updateShortCut++;
        if (self.updateShortCut > 2)
        {
            self.updateShortCut = 0;
            self.shortcuts.Update();
        }
        for (int m = self.world.activeRooms.Count - 1; m >= 0; m--)
        {
            loginf("processing update for " + self.world.activeRooms[m].abstractRoom.name);
            try
            {
                self.world.activeRooms[m].Update();
                self.world.activeRooms[m].PausedUpdate();
            }
            catch (Exception)
            {
                loginf("aeaaea" + self.world.name);
            }
        }
        if (self.world.loadingRooms.Count > 0)
        {
            for (int n = 0; n < 1; n++)
            {
                for (int num = self.world.loadingRooms.Count - 1; num >= 0; num--)
                {
                    if (self.world.loadingRooms[num].done)
                    {
                        self.world.loadingRooms.RemoveAt(num);
                    }
                    else
                    {
                        self.world.loadingRooms[num].Update();
                    }
                }
            }
        }
        if (self.manager.menuSetup.FastTravelInitCondition && self.Players[0].realizedCreature != null)
        {
            self.CustomEndGameSaveAndRestart(self.manager.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.FastTravel);
        }
        if (self.cameras[0] != null)
        {
            for (int num2 = 0; num2 < 4; num2++)
            {
                PlayerHandler playerHandler = self.rainWorld.GetPlayerHandler(num2);
                if (playerHandler != null && self.RealizedPlayerOfPlayerNumber(num2) != null)
                {
                    playerHandler.ControllerHandler.AttemptScreenShakeRumble(self.cameras[0].controllerShake);
                }
            }
        }
        AbstractCreature firstAlivePlayer = self.FirstAlivePlayer;
        if (ModManager.MSC && self.Players.Count > 0 && firstAlivePlayer != null && self.IsStorySession && self.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && !self.world.GetAbstractRoom(firstAlivePlayer.pos.room).shelter && self.world.GetAbstractRoom(firstAlivePlayer.pos.room).AttractionForCreature(CreatureTemplate.Type.Scavenger) != AbstractRoom.CreatureRoomAttraction.Forbidden && self.timeInRegionThisCycle > 4800)
        {
            self.timeWithoutCorpse++;
            Player player = firstAlivePlayer.realizedCreature as Player;
            for (int num3 = 0; num3 < player.grasps.Length; num3++)
            {
                if (player.grasps[num3] != null && player.grasps[num3].grabbedChunk != null && player.grasps[num3].grabbedChunk.owner is Scavenger && (player.grasps[num3].grabbedChunk.owner as Scavenger).dead)
                {
                    self.timeWithoutCorpse = 0;
                    self.timeSinceScavsSentToPlayer = 0;
                }
            }
            if (self.timeWithoutCorpse >= 1200)
            {
                if (self.timeSinceScavsSentToPlayer % 2400 == 0)
                {
                    self.SendScavsToPlayer();
                }
                self.timeSinceScavsSentToPlayer++;
            }
        }
        self.timeInRegionThisCycle++;
        if (self.session != null && self.session is StoryGameSession && !self.rainWorld.safariMode && self.rainWorld.ExpeditionMode)
        {
            if ((ExpeditionGame.egg == null || (ExpeditionGame.egg != null && ExpeditionGame.egg.rwGame != self)) && ExpeditionGame.ExIndex(ExpeditionData.slugcatPlayer) > -1 && ExpeditionData.ints[ExpeditionGame.ExIndex(ExpeditionData.slugcatPlayer)] == 2)
            {
                ExpeditionGame.egg = new Eggspedition(self);
            }
            if (ExpeditionGame.egg != null)
            {
                ExpeditionGame.egg.Update();
            }
            if (ExpeditionData.devMode && ExpeditionData.challengeList != null)
            {
                if (Input.GetKey(KeyCode.Alpha1) && ExpeditionData.challengeList.Count > 0 && ExpeditionData.challengeList[0] != null)
                {
                    ExpeditionData.challengeList[0].CompleteChallenge();
                }
                if (Input.GetKey(KeyCode.Alpha2) && ExpeditionData.challengeList.Count > 1 && ExpeditionData.challengeList[1] != null)
                {
                    ExpeditionData.challengeList[1].CompleteChallenge();
                }
                if (Input.GetKey(KeyCode.Alpha3) && ExpeditionData.challengeList.Count > 2 && ExpeditionData.challengeList[2] != null)
                {
                    ExpeditionData.challengeList[2].CompleteChallenge();
                }
                if (Input.GetKey(KeyCode.Alpha4) && ExpeditionData.challengeList.Count > 3 && ExpeditionData.challengeList[3] != null)
                {
                    ExpeditionData.challengeList[3].CompleteChallenge();
                }
                if (Input.GetKey(KeyCode.Alpha5) && ExpeditionData.challengeList.Count > 4 && ExpeditionData.challengeList[4] != null)
                {
                    ExpeditionData.challengeList[4].CompleteChallenge();
                }
            }
            for (int num4 = 0; num4 < ExpeditionGame.unlockTrackers.Count; num4++)
            {
                ExpeditionGame.unlockTrackers[num4].Update();
            }
            for (int num5 = 0; num5 < ExpeditionGame.burdenTrackers.Count; num5++)
            {
                ExpeditionGame.burdenTrackers[num5].Update();
            }
            if (Expedition.Expedition.coreFile.coreLoaded)
            {
                int num6 = 0;
                for (int num7 = 0; num7 < ExpeditionData.challengeList.Count; num7++)
                {
                    Challenge challenge = ExpeditionData.challengeList[num7];
                    challenge.game = self;
                    challenge.Update();
                    if (challenge.completed)
                    {
                        num6++;
                    }
                    if (num6 >= ExpeditionData.challengeList.Count && !ExpeditionGame.expeditionComplete)
                    {
                        ExpeditionGame.expeditionComplete = true;
                    }
                }
            }
            if (ExpeditionGame.expeditionComplete)
            {
                if (ExpeditionGame.voidSeaFinish)
                {
                    ExpeditionGame.voidSeaFinish = false;
                    ExpeditionData.AddExpeditionRequirements(ExpeditionData.slugcatPlayer, true);
                    Expedition.Expedition.coreFile.Save(false);
                    ExpeditionGame.runData = SlugcatSelectMenu.MineForSaveData(self.manager, ExpeditionData.slugcatPlayer);
                    self.manager.RequestMainProcessSwitch(ExpeditionEnums.ProcessID.ExpeditionWinScreen);
                }
                return;
            }
        }
        if (RainWorldGame._concurrentHeavyAiDelayedExceptLastRunAis <= 0)
        {
            RainWorldGame._lastRunAiIds.Clear();
        }
        RainWorldGame._concurrentHeavyAi = 0;
        RainWorldGame._concurrentHeavyAiDelayedExceptLastRunAis = 0;
    }

    private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void
        && self.bodyMode == BodyModeIndexExtension.CeilCrawl
        && self.input[0].jmp)
        {
            Creature.Grasp[] grasps = self.grasps;
            object obj = grasps?[grasp]?.grabbed;
            orig(self, grasp, eu);
            if (obj is Weapon weapon)
            {
                for (int i = 0; i < weapon.bodyChunks.Length; i++)
                {
                    BodyChunk bodyChunk = weapon.bodyChunks[i];
                    if (self.input[0].x == 0)
                    {
                        bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(0, -1) * 10f;
                        bodyChunk.vel = new Vector2(0, -1) * 40f;
                    }
                    else if (self.input[0].x > 0)
                    {
                        bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(1, -1) * 10f;
                        bodyChunk.vel = new Vector2(0.71f, -0.71f) * 40f;
                    }
                    else if (self.input[0].x < 0)
                    {
                        bodyChunk.pos = self.mainBodyChunk.pos + new Vector2(-1, -1) * 10f;
                        bodyChunk.vel = new Vector2(-0.71f, -0.71f) * 40f;
                    }
                }
                if (self.input[0].x == 0)
                    weapon.setRotation = new Vector2?(new Vector2(0, -1));
                else if (self.input[0].x > 0)
                    weapon.setRotation = new Vector2?(new Vector2(1, -1));
                else if (self.input[0].x < 0)
                    weapon.setRotation = new Vector2?(new Vector2(-1, -1));
            }
        }
        else 
            orig(self, grasp, eu);
    }
}
