using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.Oracles.OracleHooks;
using UnityEngine;
using MoreSlugcats;
using RWCustom;

namespace VoidTemplate.Oracles;

internal static class SLOracle
{
    public static void Hook()
    {
        On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMark_Update;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        On.SLOracleBehaviorHasMark.SpecialEvent += SLOracleBehaviorHasMark_SpecialEvent;
    }

    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        var saveState = self.oracle.room.game.GetStorySession.saveState;
        var miscData = saveState.miscWorldSaveData;

        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void && self.State.likesPlayer >= 0)
        {
            if (self.State.playerEncounters < 0)
            {
                self.State.playerEncounters = 0;
            }
            switch (self.State.playerEncountersWithMark)
            {
                case > 0 when saveState.cycleNumber - saveState.GetEncountersWithMark() <= 0:
                    {
                        SoundID randomTalk = SoundID.SL_AI_Talk_1;
                        switch (UnityEngine.Random.Range(0, 4))
                        {
                            case 0:
                                self.dialogBox.Interrupt("You are here again. Do you want to show me something?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_1;
                                break;
                            case 1:
                                self.dialogBox.Interrupt("Your return pleases me. What secrets of this world have you revealed this time?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_2;
                                break;
                            case 2:
                                self.dialogBox.Interrupt("You are back. In your eyes, I see a reflection of our changing world.".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_3;
                                break;
                            case 3:
                                self.dialogBox.Interrupt("Little creature, what brought you to me again?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_4;
                                break;
                            case 4:
                                self.dialogBox.Interrupt("Oh, is that you? Did you come back to learn something new?".TranslateString(), 50);
                                randomTalk = SoundID.SL_AI_Talk_5;
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.AirVoice(randomTalk);
                        }
                        break;
                    }
                case 4 when self.State.neuronsLeft < 6:
					{
                        self.dialogBox.Interrupt("I am sorry, friend, but I cannot remember of any new stories I can tell you.".TranslateString(), 50);
                        SoundID randomTalk = SoundID.SL_AI_Talk_1;
                        switch (UnityEngine.Random.Range(0, 4))
                        {
                            case 0:
                                randomTalk = SoundID.SL_AI_Talk_1;
                                break;
                            case 1:
                                randomTalk = SoundID.SL_AI_Talk_2;
                                break;
                            case 2:
                                randomTalk = SoundID.SL_AI_Talk_3;
                                break;
                            case 3:
                                randomTalk = SoundID.SL_AI_Talk_4;
                                break;
                            case 4:
                                randomTalk = SoundID.SL_AI_Talk_5;
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.AirVoice(randomTalk);
                        }
                        break;
                    }
                case 8 when self.State.neuronsLeft < 7:
                    {
                        self.dialogBox.Interrupt("I am embarrassed to ask you, but could you ask Five Pebbles for another neuron, I think he will not mind.".TranslateString(), 50);
                        SoundID randomTalk = SoundID.SL_AI_Talk_1;
                        switch (UnityEngine.Random.Range(0, 4))
                        {
                            case 0:
                                randomTalk = SoundID.SL_AI_Talk_1;
                                break;
                            case 1:
                                randomTalk = SoundID.SL_AI_Talk_2;
                                break;
                            case 2:
                                randomTalk = SoundID.SL_AI_Talk_3;
                                break;
                            case 3:
                                randomTalk = SoundID.SL_AI_Talk_4;
                                break;
                            case 4:
                                randomTalk = SoundID.SL_AI_Talk_5;
                                break;
                        }
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.AirVoice(randomTalk);
                        }
                        break;
                    }
                default:
                    {
                        saveState.SetEncountersWithMark(saveState.cycleNumber);
                        self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
                        self.State.playerEncountersWithMark++;
                        if (miscData.SSaiConversationsHad == 1)
                            saveState.SetVoidMeetMoon(true);
                        break;
                    }
            }
        }
        else
            orig(self);
    }

    private static void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
    {
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            {
                if (ModManager.MSC && self.SingularityProtest())
                {
                    self.currentConversation?.Destroy();
                }
                else
                {
                    self.protest = false;
                }
                self.Update(eu);
                if (!self.oracle.Consious || self.stillWakingUp)
                {
                    self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Clear();
                    self.holdingObject = null;
                    self.moveToAndPickUpItem = null;
                    return;
                }
                if (self.player != null && self.hasNoticedPlayer)
                {
                    if (ModManager.MMF && self.player.dead)
                    {
                        TalkToDeadPlayer(self);
                    }
                    if (self.movementBehavior != SLOracleBehavior.MovementBehavior.Meditate && self.movementBehavior != SLOracleBehavior.MovementBehavior.ShowMedia)
                    {
                        self.lookPoint = self.player.DangerPos;
                    }
                    if (self.sayHelloDelay < 0 && ((ModManager.MSC && self.oracle.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Saint) || self.oracle.room.world.rainCycle.TimeUntilRain + self.oracle.room.world.rainCycle.pause > 2000))
                    {
                        self.sayHelloDelay = 30;
                    }
                    else
                    {
                        if (self.sayHelloDelay > 0)
                        {
                            self.sayHelloDelay--;
                        }
                        if (self.sayHelloDelay == 1)
                        {
                            self.InitateConversation();
                            if (!self.conversationAdded && self.oracle.room.game.session is StoryGameSession)
                            {
                                SLOrcacleState sloracleState = (self.oracle.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SLOracleState;
                                int num = sloracleState.playerEncounters;
                                sloracleState.playerEncounters = num + 1;
                                SLOrcacleState sloracleState2 = (self.oracle.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SLOracleState;
                                num = sloracleState2.playerEncountersWithMark;
                                sloracleState2.playerEncountersWithMark = num + 1;
                                if (ModManager.MSC && self.oracle.room.game.GetStorySession.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && (self.oracle.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SLOracleState.playerEncounters == 1 && self.oracle.room.world.overseersWorldAI != null)
                                {
                                    self.oracle.room.world.overseersWorldAI.DitchDirectionGuidance();
                                }
                                Custom.Log(
                                [
                            "player encounter with SL AI logged"
                                ]);
                                self.conversationAdded = true;
                            }
                        }
                    }
                    if (self.player.room != self.oracle.room || self.player.DangerPos.x < 1016f)
                    {
                        self.playerLeavingCounter++;
                    }
                    else
                    {
                        self.playerLeavingCounter = 0;
                    }
                    if (self.player.room == self.oracle.room && Custom.DistLess(self.player.mainBodyChunk.pos, self.oracle.firstChunk.pos, 100f) && !Custom.DistLess(self.player.mainBodyChunk.lastPos, self.player.mainBodyChunk.pos, 1f))
                    {
                        self.playerAnnoyingCounter++;
                    }
                    else
                    {
                        self.playerAnnoyingCounter--;
                    }
                    self.playerAnnoyingCounter = Custom.IntClamp(self.playerAnnoyingCounter, 0, 150);
                    bool flag = false;
                    for (int i = 0; i < self.player.grasps.Length; i++)
                    {
                        if (self.player.grasps[i] != null && self.player.grasps[i].grabbed is SLOracleSwarmer)
                        {
                            flag = true;
                        }
                    }
                    if (!self.State.SpeakingTerms && self.currentConversation != null)
                    {
                        self.currentConversation.Destroy();
                    }
                    if (!self.rainInterrupt && self.player.room == self.oracle.room && self.oracle.room.world.rainCycle.TimeUntilRain < 1600 && self.oracle.room.world.rainCycle.pause < 1)
                    {
                            self.InterruptRain();
                            self.rainInterrupt = true;
                            self.currentConversation?.Destroy();
                    }
                    if (flag)
                    {
                        if (self.currentConversation != null)
                        {
                            if (!self.currentConversation.paused || self.pauseReason != SLOracleBehaviorHasMark.PauseReason.GrabNeuron)
                            {
                                self.currentConversation.paused = true;
                                self.pauseReason = SLOracleBehaviorHasMark.PauseReason.GrabNeuron;
                                self.InterruptPlayerHoldNeuron();
                            }
                        }
                        else if (!self.playerHoldingNeuronNoConvo)
                        {
                            self.playerHoldingNeuronNoConvo = true;
                            self.InterruptPlayerHoldNeuron();
                        }
                    }
                    if (self.currentConversation != null)
                    {
                        self.playerHoldingNeuronNoConvo = false;
                        self.playerIsAnnoyingWhenNoConversation = false;
                        if (self.currentConversation.slatedForDeletion)
                        {
                            self.currentConversation = null;
                        }
                        else
                        {
                            if (self.playerLeavingCounter > 10)
                            {
                                if (!self.currentConversation.paused)
                                {
                                    self.currentConversation.paused = true;
                                    self.pauseReason = SLOracleBehaviorHasMark.PauseReason.Leave;
                                    self.InterruptPlayerLeavingMessage();
                                }
                            }
                            else if (self.playerAnnoyingCounter > 80 && !self.oracle.room.game.IsMoonActive())
                            {
                                if (!self.currentConversation.paused)
                                {
                                    self.currentConversation.paused = true;
                                    self.pauseReason = SLOracleBehaviorHasMark.PauseReason.Annoyance;
                                    self.InterruptPlayerAnnoyingMessage();
                                }
                            }
                            else if (self.currentConversation.paused)
                            {
                                if (self.resumeConversationAfterCurrentDialoge)
                                {
                                    if (self.dialogBox.messages.Count == 0)
                                    {
                                        self.currentConversation.paused = false;
                                        self.resumeConversationAfterCurrentDialoge = false;
                                        self.currentConversation.RestartCurrent();
                                    }
                                }
                                else if ((self.pauseReason == SLOracleBehaviorHasMark.PauseReason.Leave && self.player.room == self.oracle.room && self.player.DangerPos.x > 1036f) || (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.Annoyance && self.playerAnnoyingCounter == 0) || (self.pauseReason == SLOracleBehaviorHasMark.PauseReason.GrabNeuron && !flag))
                                {
                                    self.resumeConversationAfterCurrentDialoge = true;
                                    self.ResumePausedConversation();
                                }
                            }
                            self.currentConversation.Update();
                        }
                    }
                    else if (self.State.SpeakingTerms)
                    {
                        if (self.playerHoldingNeuronNoConvo && !flag)
                        {
                            self.playerHoldingNeuronNoConvo = false;
                            self.PlayerReleaseNeuron();
                        }
                        else if (self.playerAnnoyingCounter > 80 && !self.playerIsAnnoyingWhenNoConversation && !self.oracle.room.game.IsMoonActive())
                        {
                            self.playerIsAnnoyingWhenNoConversation = true;
                            self.PlayerAnnoyingWhenNotTalking();
                        }
                        else if (self.playerAnnoyingCounter < 10 && self.playerIsAnnoyingWhenNoConversation)
                        {
                            self.playerIsAnnoyingWhenNoConversation = false;
                            if (self.State.annoyances == 1)
                            {
                                if (self.State.neuronsLeft == 3)
                                {
                                    self.dialogBox.Interrupt("...thank you.", 7);
                                }
                                else if (self.State.neuronsLeft > 3)
                                {
                                    self.dialogBox.Interrupt(self.Translate("Thank you."), 7);
                                }
                            }
                        }
                    }
                }
                if ((ModManager.MSC || (!self.DamagedMode && self.State.SpeakingTerms)) && self.holdingObject == null && self.reelInSwarmer == null && self.moveToAndPickUpItem == null)
                {
                    for (int j = 0; j < self.oracle.room.socialEventRecognizer.ownedItemsOnGround.Count; j++)
                    {
                        if (Custom.DistLess(self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item.firstChunk.pos, self.oracle.firstChunk.pos, 100f) && self.WillingToInspectItem(self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item))
                        {
                            bool flag2 = true;
                            for (int k = 0; k < self.pickedUpItemsThisRealization.Count; k++)
                            {
                                if (self.pickedUpItemsThisRealization[k] == self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item.abstractPhysicalObject.ID)
                                {
                                    flag2 = false;
                                    break;
                                }
                            }
                            if (flag2)
                            {
                                self.moveToAndPickUpItem = self.oracle.room.socialEventRecognizer.ownedItemsOnGround[j].item;
                                self.currentConversation?.Destroy();
                                self.currentConversation = null;
                                self.PlayerPutItemOnGround();
                                break;
                            }
                        }
                    }
                }
                if (self.moveToAndPickUpItem != null)
                {
                    self.moveToItemDelay++;
                    if (!self.WillingToInspectItem(self.moveToAndPickUpItem) || self.moveToAndPickUpItem.grabbedBy.Count > 0)
                    {
                        self.moveToAndPickUpItem = null;
                    }
                    else if ((self.moveToItemDelay > 40 && Custom.DistLess(self.moveToAndPickUpItem.firstChunk.pos, self.oracle.firstChunk.pos, 40f)) || (self.moveToItemDelay < 20 && !Custom.DistLess(self.moveToAndPickUpItem.firstChunk.lastPos, self.moveToAndPickUpItem.firstChunk.pos, 5f) && Custom.DistLess(self.moveToAndPickUpItem.firstChunk.pos, self.oracle.firstChunk.pos, 20f)))
                    {
                        self.GrabObject(self.moveToAndPickUpItem);
                        self.moveToAndPickUpItem = null;
                    }
                }
                else
                {
                    self.moveToItemDelay = 0;
                }
                if (self.player != null)
                {
                    int l = 0;
                    while (l < self.player.grasps.Length)
                    {
                        if (self.player.grasps[l] != null && self.player.grasps[l].grabbed is SLOracleSwarmer)
                        {
                            self.protest = true;
                            self.holdKnees = false;
                            self.oracle.bodyChunks[0].vel += Custom.RNV() * self.oracle.health * UnityEngine.Random.value;
                            self.oracle.bodyChunks[1].vel += Custom.RNV() * self.oracle.health * UnityEngine.Random.value * 2f;
                            self.protestCounter += 0.045454547f;
                            self.lookPoint = self.oracle.bodyChunks[0].pos + Custom.PerpendicularVector(self.oracle.bodyChunks[1].pos, self.oracle.bodyChunks[0].pos) * Mathf.Sin(self.protestCounter * 3.1415927f * 2f) * 145f;
                            if (UnityEngine.Random.value < 0.033333335f)
                            {
                                self.armsProtest = !self.armsProtest;
                                break;
                            }
                            break;
                        }
                        else
                        {
                            l++;
                        }
                    }
                }
                if (!self.protest)
                {
                    self.armsProtest = false;
                }
                if (self.holdingObject != null)
                {
                    self.describeItemCounter++;
                    if (!self.protest && (self.currentConversation == null || !self.currentConversation.paused) && self.movementBehavior != SLOracleBehavior.MovementBehavior.Meditate && self.movementBehavior != SLOracleBehavior.MovementBehavior.ShowMedia)
                    {
                        self.lookPoint = self.holdingObject.firstChunk.pos + Custom.DirVec(self.oracle.firstChunk.pos, self.holdingObject.firstChunk.pos) * 100f;
                    }
                    if (self.holdingObject is not SSOracleSwarmer && self.describeItemCounter > 40 && self.currentConversation == null)
                    {
                        if (ModManager.MMF && self.throwAwayObjects)
                        {
                            self.holdingObject.firstChunk.vel = new Vector2(-5f + (float)UnityEngine.Random.Range(-8, -11), 8f + (float)UnityEngine.Random.Range(1, 3));
                            self.oracle.room.PlaySound(SoundID.Slugcat_Throw_Rock, self.oracle.firstChunk);
                        }
                        self.holdingObject = null;
                        return;
                    }
                }
                else
                {
                    self.describeItemCounter = 0;
                }
            }
        }
        else
            orig(self, eu);
    }

    public static void TalkToDeadPlayer(SLOracleBehaviorHasMark self)
    {
        if (!self.deadTalk && self.oracle.room.ViewedByAnyCamera(self.oracle.firstChunk.pos, 0f))
        {
            if (self.State.neuronsLeft > 3)
            {
                self.dialogBox.Interrupt(self.Translate("..."), 60);
                MoonVoice(self);
                self.dialogBox.NewMessage(self.Translate("<CapPlayerName>, are you okay?"), 60);
                self.dialogBox.NewMessage(self.Translate("..."), 120);
                self.dialogBox.NewMessage(self.Translate("Oh..."), 60);
            }
            else
            {
                self.dialogBox.Interrupt(self.Translate("..."), 60);
            }
            self.deadTalk = true;
        }
    }

    private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);

        if (OracleConversation.MoonVoidConversation.Contains(self.id))
        {
            string path = AssetManager.ResolveFilePath($"text/oracle/firstmoon/{self.id.value.ToLower()}.txt");
            if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0 
                && self.myBehavior.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/moon11/{self.id.value.ToLower()}.txt");
            else if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0)
                path = AssetManager.ResolveFilePath($"text/oracle/moon/{self.id.value.ToLower()}.txt");
            else if (self.myBehavior.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/firstmoon11/{self.id.value.ToLower()}.txt");
            ConversationParser.GetConversationEvents(self, path);
        }
    }

    private static void SLOracleBehaviorHasMark_SpecialEvent(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
    {
        orig(self, eventName);
        if (eventName == "MoonVoice")
        {
            SoundID randomTalk = SoundID.SL_AI_Talk_1;
            switch (UnityEngine.Random.Range(0, 4))
            {
                case 0:
                    randomTalk = SoundID.SL_AI_Talk_1;
                    break;
                case 1:
                    randomTalk = SoundID.SL_AI_Talk_2;
                    break;
                case 2:
                    randomTalk = SoundID.SL_AI_Talk_3;
                    break;
                case 3:
                    randomTalk = SoundID.SL_AI_Talk_4;
                    break;
                case 4:
                    randomTalk = SoundID.SL_AI_Talk_5;
                    break;
            }
            if (self.currentConversation != null && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
            {
                self.AirVoice(randomTalk);
            }
        }
    }
    
    private static void MoonVoice(SLOracleBehaviorHasMark self)
    {
        SoundID randomTalk = SoundID.SL_AI_Talk_1;
        switch (UnityEngine.Random.Range(0, 4))
        {
            case 0:
                randomTalk = SoundID.SL_AI_Talk_1;
                break;
            case 1:
                randomTalk = SoundID.SL_AI_Talk_2;
                break;
            case 2:
                randomTalk = SoundID.SL_AI_Talk_3;
                break;
            case 3:
                randomTalk = SoundID.SL_AI_Talk_4;
                break;
            case 4:
                randomTalk = SoundID.SL_AI_Talk_5;
                break;
        }
        if (self.currentConversation != null && self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
        {
            self.AirVoice(randomTalk);
        }
    }

}
