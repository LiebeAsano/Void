using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.Oracles;

public static class SSOracleBehaviorUpdate
{
    public static void Hook()
    {
        On.SSOracleBehavior.Update += SSOracleBehavior_Update;
    }

    private static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        if (self.oracle.room.game.StoryCharacter != VoidEnums.SlugcatID.Void)
        {
            orig(self, eu);
        }
        if (ModManager.MMF && self.player != null && self.player.dead && self.currSubBehavior.ID != SSOracleBehavior.SubBehavior.SubBehavID.ThrowOut && self.oracle.room.game.Players.Count == 1)
        {
            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
        }
        if (ModManager.MSC)
        {
            if (self.inspectPearl != null)
            {
                if (self.inspectPearl.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.Spearmasterpearl)
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                }
                else
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Meditate;
                }
                if (self.inspectPearl.grabbedBy.Count > 0)
                {
                    for (int i = 0; i < self.inspectPearl.grabbedBy.Count; i++)
                    {
                        Creature grabber = self.inspectPearl.grabbedBy[i].grabber;
                        if (grabber != null)
                        {
                            for (int j = 0; j < grabber.grasps.Length; j++)
                            {
                                if (grabber.grasps[j] != null && grabber.grasps[j].grabbed != null && grabber.grasps[j].grabbed == self.inspectPearl)
                                {
                                    grabber.ReleaseGrasp(j);
                                    break;
                                }
                            }
                        }
                    }
                }
                Vector2 vector = self.oracle.firstChunk.pos - self.inspectPearl.firstChunk.pos;
                float num = Custom.Dist(self.oracle.firstChunk.pos, self.inspectPearl.firstChunk.pos);
                if (self.inspectPearl.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.Spearmasterpearl && num < 64f)
                {
                    self.inspectPearl.firstChunk.vel += Vector2.ClampMagnitude(vector, 2f) / 20f * Mathf.Clamp(16f - num / 100f * 16f, 4f, 16f);
                    if (self.inspectPearl.firstChunk.vel.magnitude < 1f || num < 8f)
                    {
                        self.inspectPearl.firstChunk.vel = Vector2.zero;
                        self.inspectPearl.firstChunk.HardSetPosition(self.oracle.firstChunk.pos);
                    }
                }
                else
                {
                    self.inspectPearl.firstChunk.vel += Vector2.ClampMagnitude(vector, 40f) / 40f * Mathf.Clamp(2f - num / 200f * 2f, 0.5f, 2f);
                    if (self.inspectPearl.firstChunk.vel.magnitude < 1f && num < 16f)
                    {
                        self.inspectPearl.firstChunk.vel = Custom.RNV() * 8f;
                    }
                    if (self.inspectPearl.firstChunk.vel.magnitude > 8f)
                    {
                        self.inspectPearl.firstChunk.vel /= 2f;
                    }
                }
                if (num < 100f && self.pearlConversation == null && self.conversation == null)
                {
                    if (self.inspectPearl.AbstractPearl.dataPearlType == MoreSlugcatsEnums.DataPearlType.Spearmasterpearl && self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior)
                    {
                        self.InitateConversation(MoreSlugcatsEnums.ConversationID.Moon_Spearmaster_Pearl, self.currSubBehavior as SSOracleBehavior.SSSleepoverBehavior);
                    }
                    else
                    {
                        self.StartItemConversation(self.inspectPearl);
                    }
                }
            }
            self.UpdateStoryPearlCollection();
        }
        if (self.timeSinceSeenPlayer >= 0)
        {
            self.timeSinceSeenPlayer++;
        }
        if (self.pearlPickupReaction && self.timeSinceSeenPlayer > 300 && self.oracle.room.game.IsStorySession && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark && (!(self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior) || self.action == SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut))
        {
            bool flag = false;
            if (self.player != null)
            {
                for (int k = 0; k < self.player.grasps.Length; k++)
                {
                    if (self.player.grasps[k] != null && self.player.grasps[k].grabbed is PebblesPearl)
                    {
                        flag = true;
                        break;
                    }
                }
            }
            if (ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                flag = false;
            }
            if (flag && !self.lastPearlPickedUp && (self.conversation == null || (self.conversation.age > 300 && !self.conversation.paused)))
            {
                if (self.conversation != null)
                {
                    self.conversation.paused = true;
                    self.restartConversationAfterCurrentDialoge = true;
                }
                self.dialogBox.Interrupt(self.Translate("Yes, help yourself. They are not edible."), 10);
                self.pearlPickupReaction = false;
            }
            self.lastPearlPickedUp = flag;
        }
        if (self.conversation != null)
        {
            if (self.restartConversationAfterCurrentDialoge && self.conversation.paused && self.action != SSOracleBehavior.Action.General_GiveMark && self.dialogBox.messages.Count == 0 && (!ModManager.MSC || (self.player != null && self.player.room == self.oracle.room)))
            {
                self.conversation.paused = false;
                self.restartConversationAfterCurrentDialoge = false;
                self.conversation.RestartCurrent();
            }
        }
        else if (ModManager.MSC && self.pearlConversation != null)
        {
            if (self.pearlConversation.slatedForDeletion)
            {
                self.pearlConversation = null;
                if (self.inspectPearl != null)
                {
                    if (self.player != null)
                    {
                        self.inspectPearl.firstChunk.vel = Custom.DirVec(self.inspectPearl.firstChunk.pos, self.player.mainBodyChunk.pos) * 3f;
                    }
                    self.readDataPearlOrbits.Add(self.inspectPearl.AbstractPearl);
                    self.inspectPearl = null;
                }
            }
            else
            {
                self.pearlConversation.Update();
                if (self.player == null || self.player.room != self.oracle.room)
                {
                    if ((self.player == null || self.player.room != null) && !self.pearlConversation.paused)
                    {
                        self.pearlConversation.paused = true;
                        self.InterruptPearlMessagePlayerLeaving();
                    }
                }
                else if (self.pearlConversation.paused && !self.restartConversationAfterCurrentDialoge)
                {
                    self.ResumePausedPearlConversation();
                }
                if (self.pearlConversation.paused && self.restartConversationAfterCurrentDialoge && self.dialogBox.messages.Count == 0)
                {
                    self.pearlConversation.paused = false;
                    self.restartConversationAfterCurrentDialoge = false;
                    self.pearlConversation.RestartCurrent();
                }
            }
        }
        else
        {
            self.restartConversationAfterCurrentDialoge = false;
        }
        if (self.voice != null)
        {
            self.voice.alive = true;
            if (self.voice.slatedForDeletetion)
            {
                self.voice = null;
            }
        }
        if (ModManager.MSC && self.oracle.room != null && self.oracle.room.game.rainWorld.safariMode)
        {
            self.safariCreature = null;
            float num = float.MaxValue;
            for (int i = 0; i < self.oracle.room.abstractRoom.creatures.Count; i++)
            {
                if (self.oracle.room.abstractRoom.creatures[i].realizedCreature != null)
                {
                    Creature realizedCreature = self.oracle.room.abstractRoom.creatures[i].realizedCreature;
                    float num2 = Custom.Dist(self.oracle.firstChunk.pos, realizedCreature.mainBodyChunk.pos);
                    if (num2 < num)
                    {
                        num = num2;
                        self.safariCreature = realizedCreature;
                    }
                }
            }
        }
        self.FindPlayer();
        for (int l = 0; l < self.oracle.room.game.cameras.Length; l++)
        {
            if (self.oracle.room.game.cameras[l].room == self.oracle.room)
            {
                self.oracle.room.game.cameras[l].virtualMicrophone.volumeGroups[2] = 1f - self.oracle.room.gravity;
            }
            else
            {
                self.oracle.room.game.cameras[l].virtualMicrophone.volumeGroups[2] = 1f;
            }
        }
        if (!self.oracle.Consious)
        {
            return;
        }
        self.unconciousTick = 0f;
        self.currSubBehavior.Update();
        if (self.oracle.slatedForDeletetion)
        {
            return;
        }
        if (self.conversation != null)
        {
            self.conversation.Update();
        }
        if (!self.currSubBehavior.CurrentlyCommunicating && (!ModManager.MSC || self.pearlConversation == null))
        {
            self.pathProgression = Mathf.Min(1f, self.pathProgression + 1f / Mathf.Lerp(40f + self.pathProgression * 80f, Vector2.Distance(self.lastPos, self.nextPos) / 5f, 0.5f));
        }
        if (ModManager.MSC && self.inspectPearl != null && self.inspectPearl is SpearMasterPearl)
        {
            self.pathProgression = Mathf.Min(1f, self.pathProgression + 1f / Mathf.Lerp(40f + self.pathProgression * 80f, Vector2.Distance(self.lastPos, self.nextPos) / 5f, 0.5f));
        }
        self.currentGetTo = Custom.Bezier(self.lastPos, self.ClampVectorInRoom(self.lastPos + self.lastPosHandle), self.nextPos, self.ClampVectorInRoom(self.nextPos + self.nextPosHandle), self.pathProgression);
        self.floatyMovement = false;
        self.investigateAngle += self.invstAngSpeed;
        self.inActionCounter++;
        if (self.player != null && self.player.room == self.oracle.room)
        {
            if (ModManager.MSC && self.playerOutOfRoomCounter > 0 && self.currSubBehavior != null && self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior && self.pearlConversation == null && (self.currSubBehavior as SSOracleBehavior.SSSleepoverBehavior).firstMetOnThisCycle && self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0)
            {
                self.UrgeAlong();
            }
            if (self.oracle.room.game.StoryCharacter == SlugcatStats.Name.Red && !self.HasSeenGreenNeuron)
            {
                for (int m = 0; m < self.player.grasps.Length; m++)
                {
                    if (self.player.grasps[m] != null && self.player.grasps[m].grabbed is NSHSwarmer)
                    {
                        Custom.Log(new string[]
                        {
                            "PEBBLES SEE GREEN NEURON"
                        });
                        self.SeePlayer();
                        break;
                    }
                }
            }
            self.playerOutOfRoomCounter = 0;
        }
        else
        {
            self.killFac = 0f;
            self.playerOutOfRoomCounter++;
        }
        if (self.pathProgression >= 1f && self.consistentBasePosCounter > 100 && !self.oracle.arm.baseMoving)
        {
            self.allStillCounter++;
        }
        else
        {
            self.allStillCounter = 0;
        }
        self.lastKillFac = self.killFac;
        self.lastKillFacOverseer = self.killFacOverseer;
        if (self.action == SSOracleBehavior.Action.General_Idle)
        {
            if (self.movementBehavior != SSOracleBehavior.MovementBehavior.Idle && self.movementBehavior != SSOracleBehavior.MovementBehavior.Meditate)
            {
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
            }
            self.throwOutCounter = 0;
            if (self.player != null && self.player.room == self.oracle.room)
            {
                bool flag2 = true;
                if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 0 || (ModManager.MSC && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM))
                {
                    if (ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear)
                    {
                        bool flag3 = false;
                        for (int n = 0; n < self.oracle.room.game.Players.Count; n++)
                        {
                            Player player = null;
                            if (self.oracle.room.game.Players[n].realizedCreature != null && self.oracle.room.game.Players[n].realizedCreature.room == self.oracle.room && !self.oracle.room.game.Players[n].realizedCreature.dead)
                            {
                                player = (self.oracle.room.game.Players[n].realizedCreature as Player);
                            }
                            if (player != null && !(player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
                            {
                                flag3 = true;
                                break;
                            }
                        }
                        if (!flag3)
                        {
                            flag2 = false;
                        }
                    }
                    if (ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                    {
                        bool flag4 = false;
                        for (int num2 = 0; num2 < self.oracle.room.game.Players.Count; num2++)
                        {
                            Player player2 = null;
                            if (self.oracle.room.game.Players[num2].realizedCreature != null && self.oracle.room.game.Players[num2].realizedCreature.room == self.oracle.room)
                            {
                                player2 = (self.oracle.room.game.Players[num2].realizedCreature as Player);
                            }
                            if (player2 != null && player2.myRobot != null)
                            {
                                flag4 = true;
                                break;
                            }
                        }
                        if (!flag4)
                        {
                            flag2 = false;
                        }
                    }
                }
                if (flag2)
                {
                    self.discoverCounter++;
                    if (ModManager.MSC && Region.IsRubiconRegion(self.oracle.room.world.name))
                    {
                        self.SeePlayer();
                    }
                    else if (self.oracle.room.GetTilePosition(self.player.mainBodyChunk.pos).y < 32 && (self.discoverCounter > 220 || Custom.DistLess(self.player.mainBodyChunk.pos, self.oracle.firstChunk.pos, 150f) || !Custom.DistLess(self.player.mainBodyChunk.pos, self.oracle.room.MiddleOfTile(self.oracle.room.ShortcutLeadingToNode(1).StartTile), 150f)))
                    {
                        self.SeePlayer();
                    }
                }
            }
        }
        else if (self.action == SSOracleBehavior.Action.General_GiveMark && self.player != null)
        {
            bool flag5 = ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Spear && self.oracle.ID == Oracle.OracleID.SS;
            self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            if ((self.inActionCounter > 30 && self.inActionCounter < 300) || (ModManager.MSC && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM))
            {
                if (self.inActionCounter < 300)
                {
                    if (ModManager.CoopAvailable)
                    {
                        self.StunCoopPlayers(20);
                    }
                    else
                    {
                        self.player.Stun(20);
                    }
                }
                Vector2 b = Vector2.ClampMagnitude(self.oracle.room.MiddleOfTile(24, 14) - self.player.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, (float)self.inActionCounter);
                if (ModManager.CoopAvailable)
                {
                    using (List<Player>.Enumerator enumerator = self.PlayersInRoom.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Player player3 = enumerator.Current;
                            player3.mainBodyChunk.vel += b;
                        }
                        goto IL_10B2;
                    }
                }
                self.player.mainBodyChunk.vel += b;
            }
        IL_10B2:
            if (self.inActionCounter == 30)
            {
                self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
            }
            if (flag5 && self.inActionCounter > 30 && self.inActionCounter < 300 && (self.player.graphicsModule as PlayerGraphics).bodyPearl != null)
            {
                (self.player.graphicsModule as PlayerGraphics).bodyPearl.visible = true;
                (self.player.graphicsModule as PlayerGraphics).bodyPearl.globalAlpha = Mathf.Lerp(0f, 1f, (float)self.inActionCounter / 300f);
            }
            if (self.inActionCounter == 300)
            {
                if (!ModManager.MSC || self.oracle.ID != MoreSlugcatsEnums.OracleID.DM)
                {
                    self.player.mainBodyChunk.vel += Custom.RNV() * 10f;
                    self.player.bodyChunks[1].vel += Custom.RNV() * 10f;
                }
                if (flag5)
                {
                    if ((self.player.graphicsModule as PlayerGraphics).bodyPearl != null)
                    {
                        (self.player.graphicsModule as PlayerGraphics).bodyPearl.visible = false;
                        (self.player.graphicsModule as PlayerGraphics).bodyPearl.scarVisible = true;
                    }
                    self.player.Regurgitate();
                    self.player.aerobicLevel = 1.1f;
                    self.player.exhausted = true;
                    self.player.SetMalnourished(true);
                    if (self.SMCorePearl == null)
                    {
                        int num3 = 0;
                        while (num3 < self.oracle.room.updateList.Count)
                        {
                            if (self.oracle.room.updateList[num3] is SpearMasterPearl)
                            {
                                self.SMCorePearl = (self.oracle.room.updateList[num3] as SpearMasterPearl);
                                if (AbstractPhysicalObject.UsesAPersistantTracker(self.SMCorePearl.abstractPhysicalObject))
                                {
                                    (self.oracle.room.game.session as StoryGameSession).AddNewPersistentTracker(self.SMCorePearl.abstractPhysicalObject, self.oracle.room.world);
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                num3++;
                            }
                        }
                    }
                    if (self.SMCorePearl != null)
                    {
                        self.SMCorePearl.firstChunk.vel *= 0f;
                        self.SMCorePearl.DisableGravity();
                        self.afterGiveMarkAction = MoreSlugcatsEnums.SSOracleBehaviorAction.MeetPurple_GetPearl;
                    }
                    else
                    {
                        self.afterGiveMarkAction = SSOracleBehavior.Action.General_Idle;
                    }
                    self.player.Stun(60);
                }
                else
                {
                    if (ModManager.MSC && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                    {
                        self.afterGiveMarkAction = MoreSlugcatsEnums.SSOracleBehaviorAction.MeetWhite_ThirdCurious;
                        self.player.AddFood(10);
                    }
                    if (ModManager.CoopAvailable)
                    {
                        self.StunCoopPlayers(40);
                    }
                    else
                    {
                        self.player.Stun(40);
                    }
                    (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark = true;
                }
                if (self.oracle.room.game.StoryCharacter == SlugcatStats.Name.Red)
                {
                    self.oracle.room.game.GetStorySession.saveState.redExtraCycles = true;
                    if (self.oracle.room.game.cameras[0].hud != null)
                    {
                        if (self.oracle.room.game.cameras[0].hud.textPrompt != null)
                        {
                            self.oracle.room.game.cameras[0].hud.textPrompt.cycleTick = 0;
                        }
                        if (self.oracle.room.game.cameras[0].hud.map != null && self.oracle.room.game.cameras[0].hud.map.cycleLabel != null)
                        {
                            self.oracle.room.game.cameras[0].hud.map.cycleLabel.UpdateCycleText();
                        }
                    }
                    if (self.player.redsIllness != null)
                    {
                        self.player.redsIllness.GetBetter();
                    }
                    if (ModManager.CoopAvailable)
                    {
                        foreach (AbstractCreature abstractCreature in self.oracle.room.game.AlivePlayers)
                        {
                            if (abstractCreature.Room == self.oracle.room.abstractRoom)
                            {
                                Player player4 = abstractCreature.realizedCreature as Player;
                                if (player4 != null)
                                {
                                    RedsIllness redsIllness = player4.redsIllness;
                                    if (redsIllness != null)
                                    {
                                        redsIllness.GetBetter();
                                    }
                                }
                            }
                        }
                    }
                    if (!self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap)
                    {
                        self.oracle.room.game.GetStorySession.saveState.IncreaseKarmaCapOneStep();
                        self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap = true;
                    }
                    else
                    {
                        Custom.Log(new string[]
                        {
                            "PEBBLES HAS ALREADY GIVEN RED ONE KARMA CAP STEP"
                        });
                    }
                }
                else if (ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
                {
                    if (!self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap)
                    {
                        self.oracle.room.game.GetStorySession.saveState.IncreaseKarmaCapOneStep();
                        self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap = true;
                    }
                    else
                    {
                        Custom.Log(new string[]
                        {
                            "PEBBLES HAS ALREADY GIVEN GOURMAND ONE KARMA CAP STEP"
                        });
                    }
                }
                if (!flag5)
                {
                    (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
                    for (int num4 = 0; num4 < self.oracle.room.game.cameras.Length; num4++)
                    {
                        if (self.oracle.room.game.cameras[num4].hud.karmaMeter != null)
                        {
                            self.oracle.room.game.cameras[num4].hud.karmaMeter.UpdateGraphic();
                        }
                    }
                }
                if (ModManager.CoopAvailable && !flag5)
                {
                    using (List<Player>.Enumerator enumerator = self.PlayersInRoom.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Player player5 = enumerator.Current;
                            for (int num5 = 0; num5 < 20; num5++)
                            {
                                self.oracle.room.AddObject(new Spark(player5.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                        }
                        goto IL_193C;
                    }
                }
                for (int num6 = 0; num6 < 20; num6++)
                {
                    self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                }
            IL_193C:
                if (!flag5)
                {
                    self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);
                }
            }
            if (ModManager.CoopAvailable)
            {
                using (List<Player>.Enumerator enumerator = self.PlayersInRoom.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Player player6 = enumerator.Current;
                        if (self.inActionCounter > 300 && player6.graphicsModule != null && !flag5)
                        {
                            (player6.graphicsModule as PlayerGraphics).markAlpha = Mathf.Max((player6.graphicsModule as PlayerGraphics).markAlpha, Mathf.InverseLerp(500f, 300f, (float)self.inActionCounter));
                        }
                    }
                    goto IL_1A59;
                }
            }
            if (self.inActionCounter > 300 && self.player.graphicsModule != null && !flag5)
            {
                (self.player.graphicsModule as PlayerGraphics).markAlpha = Mathf.Max((self.player.graphicsModule as PlayerGraphics).markAlpha, Mathf.InverseLerp(500f, 300f, (float)self.inActionCounter));
            }
        IL_1A59:
            if (self.inActionCounter >= 500 || (flag5 && self.inActionCounter > 310))
            {
                self.NewAction(self.afterGiveMarkAction);
                if (self.conversation != null)
                {
                    self.conversation.paused = false;
                }
            }
        }
        self.Move();
        if (self.working != self.getToWorking)
        {
            self.working = Custom.LerpAndTick(self.working, self.getToWorking, 0.05f, 0.033333335f);
        }
        if (!ModManager.MSC || !Region.IsRubiconRegion(self.oracle.room.world.name))
        {
            for (int num7 = 0; num7 < self.oracle.room.game.cameras.Length; num7++)
            {
                if (self.oracle.room.game.cameras[num7].room == self.oracle.room && !self.oracle.room.game.cameras[num7].AboutToSwitchRoom && self.oracle.room.game.cameras[num7].paletteBlend != self.working)
                {
                    self.oracle.room.game.cameras[num7].ChangeBothPalettes(25, 26, self.working);
                }
            }
        }
        if (ModManager.MSC)
        {
            if ((self.oracle.ID == MoreSlugcatsEnums.OracleID.DM || (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer)) && self.player != null && self.player.room == self.oracle.room)
            {
                List<PhysicalObject>[] physicalObjects = self.oracle.room.physicalObjects;
                for (int num8 = 0; num8 < physicalObjects.Length; num8++)
                {
                    for (int num9 = 0; num9 < physicalObjects[num8].Count; num9++)
                    {
                        PhysicalObject physicalObject = physicalObjects[num8][num9];
                        if (physicalObject is Weapon && self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                        {
                            Weapon weapon = physicalObject as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(weapon.firstChunk.pos, self.oracle.firstChunk.pos) < 100f)
                            {
                                weapon.ChangeMode(Weapon.Mode.Free);
                                weapon.SetRandomSpin();
                                weapon.firstChunk.vel *= -0.2f;
                                for (int num10 = 0; num10 < 5; num10++)
                                {
                                    self.oracle.room.AddObject(new Spark(weapon.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
                                }
                                self.oracle.room.AddObject(new Explosion.ExplosionLight(weapon.firstChunk.pos, 150f, 1f, 8, Color.white));
                                self.oracle.room.AddObject(new ShockWave(weapon.firstChunk.pos, 60f, 0.1f, 8, false));
                                self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, weapon.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value * 0.5f);
                            }
                        }
                        bool flag6 = false;
                        bool flag7 = (self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty || self.action == MoreSlugcatsEnums.SSOracleBehaviorAction.Moon_SlumberParty || self.action == SSOracleBehavior.Action.General_Idle) && self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior && (self.currSubBehavior as SSOracleBehavior.SSSleepoverBehavior).panicObject == null;
                        if (self.oracle.ID == Oracle.OracleID.SS && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Artificer && self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior)
                        {
                            flag7 = true;
                            flag6 = true;
                        }
                        if (self.inspectPearl == null && (self.conversation == null || flag6) && physicalObject is DataPearl && (physicalObject as DataPearl).grabbedBy.Count == 0 && ((physicalObject as DataPearl).AbstractPearl.dataPearlType != DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl || (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM && ((physicalObject as DataPearl).AbstractPearl as PebblesPearl.AbstractPebblesPearl).color >= 0)) && !self.readDataPearlOrbits.Contains((physicalObject as DataPearl).AbstractPearl) && flag7 && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark && !self.talkedAboutThisSession.Contains(physicalObject.abstractPhysicalObject.ID))
                        {
                            self.inspectPearl = (physicalObject as DataPearl);
                            if (!(self.inspectPearl is SpearMasterPearl) || !(self.inspectPearl.AbstractPearl as SpearMasterPearl.AbstractSpearMasterPearl).broadcastTagged)
                            {
                                Custom.Log(new string[]
                                {
                                    string.Format("---------- INSPECT PEARL TRIGGERED: {0}", self.inspectPearl.AbstractPearl.dataPearlType)
                                });
                                if (self.inspectPearl is SpearMasterPearl)
                                {
                                    self.LockShortcuts();
                                    if (self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.firstChunk.pos.y > 600f)
                                    {
                                        self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.Stun(40);
                                        self.oracle.room.game.cameras[0].followAbstractCreature.realizedCreature.firstChunk.vel = new Vector2(0f, -4f);
                                    }
                                    self.getToWorking = 0.5f;
                                    self.SetNewDestination(new Vector2(600f, 450f));
                                    break;
                                }
                                break;
                            }
                            else
                            {
                                self.inspectPearl = null;
                            }
                        }
                    }
                }
            }
            if (Region.IsRubiconRegion(self.oracle.room.world.name))
            {
                int num11 = 0;
                if (self.oracle.ID == MoreSlugcatsEnums.OracleID.DM)
                {
                    num11 = 2;
                }
                float num12 = Custom.Dist(self.oracle.arm.cornerPositions[0], self.oracle.arm.cornerPositions[2]) * 0.4f;
                if (Custom.Dist(self.baseIdeal, self.oracle.arm.cornerPositions[num11]) >= num12)
                {
                    self.baseIdeal = self.oracle.arm.cornerPositions[num11] + (self.baseIdeal - self.oracle.arm.cornerPositions[num11]).normalized * num12;
                }
            }
            if (self.currSubBehavior.LowGravity >= 0f)
            {
                self.oracle.room.gravity = self.currSubBehavior.LowGravity;
                return;
            }
        }
        if (!self.currSubBehavior.Gravity)
        {
            self.oracle.room.gravity = Custom.LerpAndTick(self.oracle.room.gravity, 0f, 0.05f, 0.02f);
            return;
        }
        if (!ModManager.MSC || !Region.IsRubiconRegion(self.oracle.room.world.name) || !self.oracle.room.game.IsStorySession || !self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.ripMoon || self.oracle.ID != Oracle.OracleID.SS)
        {
            self.oracle.room.gravity = 1f - self.working;
        }
        if (self?.oracle?.room?.game == null) return;
        if (self.player != null && self.player.room == self.oracle.room)
        {
            if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
            {
                var saveState = self.oracle.room.game.GetStorySession.saveState;
                if ((self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior || self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior)
                    && self.pearlConversation == null
                    && ((OracleHooks.VoidPearl(self.oracle.room) is not null && OracleHooks.RotPearl(self.oracle.room) is not null && saveState.GetVoidQuest() && !saveState.GetVoidPearl() && !saveState.GetRotPearl())
                    || (OracleHooks.VoidPearl(self.oracle.room) is not null && saveState.GetVoidQuest() && !saveState.GetVoidPearl())
                    || (OracleHooks.RotPearl(self.oracle.room) is not null && saveState.GetVoidQuest() && !saveState.GetRotPearl())
                    || self.timeSinceSeenPlayer < 0))
                {
                    self.SeePlayer();
                }
                self.playerOutOfRoomCounter = 0;
            }

        }
        else
        {
            if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
            {
                self.playerOutOfRoomCounter++;
                self.timeSinceSeenPlayer = -1;
            }
        }
    }
}
