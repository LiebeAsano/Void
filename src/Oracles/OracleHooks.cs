using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.IO;
using System.Linq;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.VoidEnums.ConversationID;
using static VoidTemplate.SaveManager;
using UnityEngine;
using System.Collections.Generic;
using RWCustom;
using static VoidTemplate.Oracles.OracleHooks;
using System.Data.SqlTypes;
using MonoMod.RuntimeDetour;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.Oracles;

static class OracleHooks
{
    public static void Hook()
    {
        //new Hook(typeof(OverseerGraphics).GetProperty(nameof(OverseerGraphics.MainColor)).GetGetMethod(), CustomColor);
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
        On.SSOracleBehavior.SpecialEvent += SSOracleBehavior_SpecialEvent;
        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        On.SSOracleBehavior.ThrowOutBehavior.Update += ThrowOutBehavior_Update;
        //On.SSOracleBehavior.Update += SSOralceBehavior_Update;
        //IL.SSOracleBehavior.Update += ILSSOracleBehavior_Update;
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        fivePebblesGetOut = false;
    }

    private static Color CustomColor(Func<OverseerGraphics, Color> orig, OverseerGraphics self)
    {
        var color = orig(self);
        return new Color(1, 1, 1);
    }

    private static bool fivePebblesGetOut = false;

    private static void SSOralceBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
    {
        if (self.action == SSOracleBehavior.Action.General_GiveMark && self.player != null && self.player.IsVoid())
        {
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
            if (self.inActionCounter == 300)
            {
                if (!ModManager.MSC || self.oracle.ID != MoreSlugcatsEnums.OracleID.DM)
                {
                    self.player.mainBodyChunk.vel += Custom.RNV() * 10f;
                    self.player.bodyChunks[1].vel += Custom.RNV() * 10f;
                }


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


                (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
                for (int num4 = 0; num4 < self.oracle.room.game.cameras.Length; num4++)
                {
                    if (self.oracle.room.game.cameras[num4].hud.karmaMeter != null)
                    {
                        self.oracle.room.game.cameras[num4].hud.karmaMeter.UpdateGraphic();
                    }
                }

                if (ModManager.CoopAvailable)
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

                self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);

            }
            if (ModManager.CoopAvailable)
            {
                using (List<Player>.Enumerator enumerator = self.PlayersInRoom.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Player player6 = enumerator.Current;
                        if (self.inActionCounter > 300 && player6.graphicsModule != null)
                        {
                            (player6.graphicsModule as PlayerGraphics).markAlpha = Mathf.Max((player6.graphicsModule as PlayerGraphics).markAlpha, Mathf.InverseLerp(500f, 300f, (float)self.inActionCounter));
                        }
                    }
                    goto IL_1A59;
                }
            }
            if (self.inActionCounter > 300 && self.player.graphicsModule != null)
            {
                (self.player.graphicsModule as PlayerGraphics).markAlpha = Mathf.Max((self.player.graphicsModule as PlayerGraphics).markAlpha, Mathf.InverseLerp(500f, 300f, (float)self.inActionCounter));
            }
        IL_1A59:
            if (self.inActionCounter >= 500)
            {
                self.NewAction(self.afterGiveMarkAction);
                if (self.conversation != null)
                {
                    self.conversation.paused = false;
                }
            }
        }
        orig(self, eu);
        if (self?.oracle?.room?.game == null) return;
        if (self.player != null && self.player.room == self.oracle.room)
        {
            if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
            {
                var saveState = self.oracle.room.game.GetStorySession.saveState;
                if ((self.currSubBehavior is SSOracleBehavior.SSSleepoverBehavior || self.currSubBehavior is SSOracleBehavior.ThrowOutBehavior)
                    && self.pearlConversation == null
                    && ((VoidPearl(self.oracle.room) is not null && RotPearl(self.oracle.room) is not null && saveState.GetVoidQuest() && !saveState.GetVoidPearl() && !saveState.GetRotPearl())
                    || (VoidPearl(self.oracle.room) is not null && saveState.GetVoidQuest() && !saveState.GetVoidPearl())
                    || (RotPearl(self.oracle.room) is not null && saveState.GetVoidQuest() && !saveState.GetRotPearl())
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

    #region immutable
    public static SSOracleBehavior.Action MeetVoid_Init = new("MeetVoid_Init", true);
    public static SSOracleBehavior.Action MeetVoid_Curious = new("MeetVoid_Curious", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new("VoidTalk", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidScan = new("VoidScan", true);
    public static List<ProjectedImage> Void_projectImages = new();
    #endregion

    public static void EatPearlsInterrupt(this SSOracleBehavior self)
    {
        if (self.oracle.ID == Oracle.OracleID.SL) return;  //only works for FP
        if (self.conversation != null)
        {
            self.conversation.paused = true;
            self.restartConversationAfterCurrentDialoge = true;
        }
        var savestate = self.oracle.room.game.GetStorySession.saveState;
        var amountOfEatenPearls = savestate.GetPebblesPearlsEaten();
        if (amountOfEatenPearls == 6 && !savestate.GetVoidMeetMoon())
        {
            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
            self.getToWorking = 1f;
        }
        else if (amountOfEatenPearls < 12)
        {
            self.dialogBox.Interrupt(self.Translate(
                savestate.GetVoidMeetMoon()
                    ? OracleConversation.eatInterruptMessages6Step[amountOfEatenPearls]
                    : OracleConversation.eatInterruptMessages[amountOfEatenPearls]), 10);
            savestate.SetPebblesPearlsEaten(savestate.GetPebblesPearlsEaten() + 1);
        }

    }

    public static void RegurgitatePearlsInterrupt(this SSOracleBehavior self)
    {
        if (self.oracle.ID == Oracle.OracleID.SL) return;  //only works for FP
        if (self.conversation != null)
        {
            self.conversation.paused = true;
            self.restartConversationAfterCurrentDialoge = true;
        }
        var savestate = self.oracle.room.game.GetStorySession.saveState;
        var amountOfEatenPearls = savestate.GetPebblesPearlsEaten();
        if (amountOfEatenPearls == 6 && !savestate.GetVoidMeetMoon())
        {
            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
            self.getToWorking = 1f;
        }
        else if (amountOfEatenPearls < 12)
        {
            self.dialogBox.Interrupt(self.Translate(
                savestate.GetVoidMeetMoon()
                    ? OracleConversation.regurgitateInterruptMessages6Step[amountOfEatenPearls]
                    : OracleConversation.regurgitateInterruptMessages[amountOfEatenPearls]), 10);
        }

    }

    public static void RegurgitatePearlsInterrupt(this SLOracleBehavior self)
    {
        if (self is SLOracleBehaviorHasMark hasMark)
        {
            if (hasMark.currentConversation != null)
            {
                hasMark.currentConversation.paused = true;
                hasMark.resumeConversationAfterCurrentDialoge = true;
            }
            SLOracle.MoonVoice(hasMark);
            self.dialogBox.Interrupt("Харэ пёрлы жрать!!!", 10);
        }
        else if (self is SLOracleBehaviorNoMark)
        {
            self.AirVoice(UnityEngine.Random.Range(0, 5) switch
            {
                0 => SoundID.SL_AI_Talk_1,
                1 => SoundID.SL_AI_Talk_2,
                2 => SoundID.SL_AI_Talk_3,
                3 => SoundID.SL_AI_Talk_4,
                _ => SoundID.SL_AI_Talk_5
            });
        }
    }

    private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
    {
        orig(self);
        if (OracleConversation.PebbleVoidConversation.Contains(self.id))
        {
            //#warning may be elegible for deletion

            self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 30));

            var path = AssetManager.ResolveFilePath($"text/oracle/pebble/{self.id.value.ToLower()}.txt");

            if (self.owner.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                path = AssetManager.ResolveFilePath($"text/oracle/pebble11/{self.id.value.ToLower()}.txt");
            ConversationParser.GetConversationEvents(self, path);
        }
    }

    private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
    {
        if (nextAction == MeetVoid_Init || nextAction == MeetVoid_Curious)
        {
            self.inActionCounter = 0;
            self.action = nextAction;
            if (self.currSubBehavior.ID == VoidTalk)
            {
                self.currSubBehavior.Activate(self.action, nextAction);
                return;
            }

            SSOracleBehavior.SubBehavior subBehavior = null;
            for (int i = 0; i < self.allSubBehaviors.Count; i++)
            {
                if (self.allSubBehaviors[i].ID == VoidTalk)
                {
                    subBehavior = self.allSubBehaviors[i];
                    break;
                }
            }

            subBehavior ??= new SSOracleMeetVoid_CuriousBehavior(self, self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad);
            self.allSubBehaviors.Add(subBehavior);

            subBehavior.Activate(self.action, nextAction);
            self.currSubBehavior.Deactivate();
            self.currSubBehavior = subBehavior;
        }
        else
        {
            orig(self, nextAction);
        }
    }

    private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
    {
        if (self?.oracle?.room?.game?.GetStorySession?.saveState == null)
        {
            orig(self);
            return;
        }
        if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void
            && self.oracle.room.game.Players.Exists(x => x.realizedCreature is Player))
        {
            if (self.timeSinceSeenPlayer < 0) self.timeSinceSeenPlayer = 0;
            var saveState = self.oracle.room.game.GetStorySession.saveState;
            var miscData = saveState.miscWorldSaveData;
            var need = miscData.SSaiConversationsHad < OracleConversation.cycleLingers.Length
                ? OracleConversation.cycleLingers[miscData.SSaiConversationsHad]
                : -1;
            loginf($"HadConv: {miscData.SSaiConversationsHad}, Cycle: {saveState.cycleNumber}, LastCycle: {saveState.GetLastMeetCycles()}, NeedCycle: {need}");
            if (VoidPearl(self.oracle.room) is not null && RotPearl(self.oracle.room) is not null)
            {
                saveState.SetVoidPearl(true);
                saveState.SetRotPearl(true);
            }
            else if (VoidPearl(self.oracle.room) is not null)
            {
                saveState.SetVoidPearl(true);
            }
            else if (RotPearl(self.oracle.room) is not null)
            {
                saveState.SetRotPearl(true);
            }
            switch (miscData.SSaiConversationsHad)
            {
                case 0:
                    {
                        saveState.EnlistDreamIfNotSeen(Dream.Pebble);
                        miscData.SSaiConversationsHad++;
                        fivePebblesGetOut = false;
                        saveState.SetLastMeetCycles(saveState.cycleNumber);
                        self.afterGiveMarkAction = MeetVoid_Init;
                        self.NewAction(MeetVoid_Curious);
                        self.SlugcatEnterRoomReaction();
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        break;
                    }
                case > 0 when saveState.cycleNumber - saveState.GetLastMeetCycles() <= 0:
                    {
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        {
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                            self.getToWorking = 1f;
                            break;
                        }
                        if (miscData.SSaiConversationsHad == 1 && !fivePebblesGetOut)
                        {

                            PebbleVoice(self);
                            self.conversation.events.Add(new Conversation.TextEvent(self.conversation, 60, self.Translate("I have already told you that I cannot help you in any way. Accept your fate and do not waste your time."), 60));
                            fivePebblesGetOut = true;
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                            break;
                        }
                        if (miscData.SSaiConversationsHad == 3 && !saveState.GetVoidMeetMoon() && !fivePebblesGetOut)
                        {
                            PebbleVoice(self);
                            self.dialogBox.NewMessage(self.Translate("Are you still here? Go east to Looks to the Moon."), 60);
                            fivePebblesGetOut = true;
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                        }
                        if (!saveState.GetVoidMeetMoon())
                        {
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                            break;
                        }
                        else
                        {
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                            miscData.SSaiConversationsHad--;
                            break;
                        }
                    }
                case 1 when self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10:
                    {
                        self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                        self.getToWorking = 1f;
                        break;
                    }
                /*case 1:
                    {
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                            self.NewAction(self.afterGiveMarkAction);
                        self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                        miscData.SSaiConversationsHad--;
                        break;
                    }*/
                case 1:
                    {
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                            self.NewAction(self.afterGiveMarkAction);
                        //self.currSubBehavior.owner.conversation.slatedForDeletion = false;
                        miscData.SSaiConversationsHad++;
                        fivePebblesGetOut = false;
                        self.afterGiveMarkAction = MeetVoid_Init;
                        saveState.SetLastMeetCycles(saveState.cycleNumber);
                        self.NewAction(MeetVoid_Init);
                        self.SlugcatEnterRoomReaction();
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        break;
                    }
                case 3 when !saveState.GetVoidMeetMoon() && !fivePebblesGetOut:
                    {
                        PebbleVoice(self);
                        if (self.conversation?.events != null)
                        {
                            self.conversation.events.Add(new Conversation.TextEvent(self.conversation, 60, self.Translate("I would suggest you cease wandering around. It is not like you have much time."), 60));
                        }
                        fivePebblesGetOut = true;
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                            self.NewAction(self.afterGiveMarkAction);
                        self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                        break;
                    }
                case 3:
                    {
                        if (saveState.GetVoidPearl() && saveState.GetRotPearl())
                        {
                            if (VoidPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractVoidPearl && RotPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractRotPearl2)
                            {
                                if (self.action != MeetVoid_Init)
                                {
                                    saveState.SetLastMeetCycles(saveState.cycleNumber);
                                    if (self.currSubBehavior.ID != VoidTalk)
                                    {
                                        GrabDataPearlAndDestroyIt(self, abstractVoidPearl.realizedObject as DataPearl);
                                        GrabDataPearlAndDestroyIt(self, abstractRotPearl2.realizedObject as DataPearl);
                                        miscData.SSaiConversationsHad++;
                                        fivePebblesGetOut = false;
                                        self.NewAction(MeetVoid_Init);
                                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                            self.NewAction(self.afterGiveMarkAction);
                                        self.SlugcatEnterRoomReaction();
                                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                                    }
                                }
                            }
                            else if (VoidPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractVoidPearl3)
                            {
                                if (self.action != MeetVoid_Init)
                                {
                                    saveState.SetLastMeetCycles(saveState.cycleNumber);
                                    if (self.currSubBehavior.ID != VoidTalk)
                                    {
                                        GrabDataPearlAndDestroyIt(self, abstractVoidPearl3.realizedObject as DataPearl);
                                        miscData.SSaiConversationsHad++;
                                        fivePebblesGetOut = false;
                                        self.NewAction(MeetVoid_Init);
                                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                            self.NewAction(self.afterGiveMarkAction);
                                        self.SlugcatEnterRoomReaction();
                                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                                    }
                                }
                            }
                            else if (RotPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractVoidPearl4)
                            {
                                if (self.action != MeetVoid_Init)
                                {
                                    saveState.SetLastMeetCycles(saveState.cycleNumber);
                                    if (self.currSubBehavior.ID != VoidTalk)
                                    {
                                        GrabDataPearlAndDestroyIt(self, abstractVoidPearl4.realizedObject as DataPearl);
                                        miscData.SSaiConversationsHad++;
                                        fivePebblesGetOut = false;
                                        self.NewAction(MeetVoid_Init);
                                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                            self.NewAction(self.afterGiveMarkAction);
                                        self.SlugcatEnterRoomReaction();
                                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                                    }
                                }
                            }
                        }
                        else if (saveState.GetVoidPearl())
                        {
                            if (VoidPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractVoidPearl)
                            {
                                if (self.action != MeetVoid_Init)
                                {
                                    if (self.currSubBehavior.ID != VoidTalk)
                                    {
                                        GrabDataPearlAndDestroyIt(self, abstractVoidPearl.realizedObject as DataPearl);
                                        fivePebblesGetOut = false;
                                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                            self.NewAction(self.afterGiveMarkAction);
                                        if (self.action != MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty)
                                        {
                                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                                            self.dialogBox.NewMessage(self.Translate("The familiar colour, is this the pearl that contains the void liquid researches?"), 60);
                                            self.dialogBox.NewMessage(self.Translate("What about the other one?"), 60);
                                            miscData.SSaiConversationsHad--;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                    self.NewAction(self.afterGiveMarkAction);
                                if (self.action != MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty)
                                {
                                    self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                                    if (!fivePebblesGetOut)
                                    {
                                        self.dialogBox.NewMessage(self.Translate("Have you brought another pearl?"), 60);
                                    }
                                    miscData.SSaiConversationsHad--;
                                }
                            }
                        }
                        else if (saveState.GetRotPearl())
                        {
                            if (RotPearl(self.oracle.room) is DataPearl.AbstractDataPearl abstractVoidPearl)
                            {
                                if (self.action != MeetVoid_Init)
                                {
                                    if (self.currSubBehavior.ID != VoidTalk)
                                    {
                                        GrabDataPearlAndDestroyIt(self, abstractVoidPearl.realizedObject as DataPearl);
                                        fivePebblesGetOut = false;
                                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                            self.NewAction(self.afterGiveMarkAction);
                                        if (self.action != MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty)
                                        {
                                            self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                                            self.dialogBox.NewMessage(self.Translate("Did you really find it? This pearl will help us both."), 60);
                                            self.dialogBox.NewMessage(self.Translate("What about the other one?"), 60);
                                            miscData.SSaiConversationsHad--;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                    self.NewAction(self.afterGiveMarkAction);
                                if (self.action != MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty)
                                {
                                    self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                                    if (!fivePebblesGetOut)
                                    {
                                        self.dialogBox.NewMessage(self.Translate("Have you brought another pearl?"), 60);
                                    }
                                    miscData.SSaiConversationsHad--;
                                }
                            }
                        }
                        else if (!saveState.GetVoidPearl() && !saveState.GetRotPearl())
                        {
                            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                self.NewAction(self.afterGiveMarkAction);
                            if (self.action != MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty)
                            {
                                self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                                if (!fivePebblesGetOut)
                                {
                                    self.dialogBox.NewMessage(self.Translate("Have you brought the pearls I need?"), 60);
                                }
                                miscData.SSaiConversationsHad--;
                            }
                        }
                        break;
                    }
                case > 4:
                    {
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                            self.NewAction(self.afterGiveMarkAction);
                        self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                        break;
                    }
                default:
                    {
                        if (self.action != MeetVoid_Init)
                        {
                            saveState.SetLastMeetCycles(saveState.cycleNumber);
                            if (self.currSubBehavior.ID != VoidTalk)
                            {
                                miscData.SSaiConversationsHad++;
                                fivePebblesGetOut = false;
                                self.NewAction(MeetVoid_Init);
                                if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                                    self.NewAction(self.afterGiveMarkAction);
                                self.SlugcatEnterRoomReaction();
                                self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                            }
                        }
                        break;
                    }
            }
        }
        else
        {
            orig(self);
        }
    }

#nullable enable
    public static DataPearl.AbstractDataPearl? RotPearl(Room room)
    {
        foreach (Player p in room.PlayersInRoom)
        {
            if (PlayersRotPearl(p) is DataPearl.AbstractDataPearl pearl) return pearl;
        }
        foreach (UpdatableAndDeletable UAD in room.updateList)
        {
            if (UAD is DataPearl pearl
                && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-rot"))
                return pearl.AbstractPearl;
        }
        return null;

        static DataPearl.AbstractDataPearl? PlayersRotPearl(Player p)
        {
            foreach (var grasp in p.grasps)
            {
                if (grasp != null
                    && grasp.grabbed is DataPearl pearl
                    && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-rot"))
                    return pearl.AbstractPearl;
            }
            return null;
        }
    }
    public static DataPearl.AbstractDataPearl? VoidPearl(Room room)
    {
        foreach (Player p in room.PlayersInRoom)
        {
            if (PlayersVoidPearl(p) is DataPearl.AbstractDataPearl pearl) return pearl;
        }
        foreach (UpdatableAndDeletable UAD in room.updateList)
        {
            if (UAD is DataPearl pearl
                && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-void"))
                return pearl.AbstractPearl;
        }
        return null;

        static DataPearl.AbstractDataPearl? PlayersVoidPearl(Player p)
        {
            foreach (var grasp in p.grasps)
            {
                if (grasp != null
                    && grasp.grabbed is DataPearl pearl
                    && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-void"))
                    return pearl.AbstractPearl;
            }
            return null;
        }
    }
#nullable disable

    private static void ThrowOutBehavior_Update(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
    {
        orig(self);
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void)
        {
            if (self.owner is SSOracleBehavior self2 &&
               (self.owner.throwOutCounter == 700 ||
                self.owner.throwOutCounter == 980 ||
                self.owner.throwOutCounter == 1530))
            {
                PebbleVoice(self2);
            }
        }
    }
#nullable enable
    private static void SSOracleBehavior_SpecialEvent(On.SSOracleBehavior.orig_SpecialEvent orig, SSOracleBehavior self, string eventName)
    {
        orig(self, eventName);
        switch (eventName)
        {
            case "PebbleVoice":
                {
                    PebbleVoice(self);
                    break;
                }
            case "GrabPearl":
                {
                    DataPearl? pearl = self.oracle.room.updateList.FirstOrDefault(x => x is DataPearl pearl
                    && pearl.AbstractPearl.dataPearlType == new DataPearl.AbstractDataPearl.DataPearlType("LW-void")) as DataPearl;
                    GrabDataPearlAndDestroyIt(self, pearl);
                    break;
                }
            case "GiveMarkV2":
                {
                    self.oracle.room.game.GetStorySession.saveState.SetVoidMarkV2(true);
                    for (int num4 = 0; num4 < 20; num4++)
                    {
                        self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                    }

                    self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);

                    if ((self.player.input[0].y != 0 || self.player.input[0].x != 0 || self.player.input[0].jmp) && self.player.bodyMode != Player.BodyModeIndex.WallClimb && self.player.bodyMode != BodyModeIndexExtension.CeilCrawl
                        || (self.player.input[0].y != 0 || self.player.input[0].jmp) && self.player.bodyMode == Player.BodyModeIndex.WallClimb
                        || (self.player.input[0].x != 0 || self.player.input[0].jmp) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                    {
                        self.player.Die();
                    }
                    break;
                }
            case "GiveMarkV3":
                {
                    self.oracle.room.game.GetStorySession.saveState.SetVoidMarkV3(true);
                    for (int num4 = 0; num4 < 20; num4++)
                    {
                        self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                    }

                    self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);

                    self.player.AddFood(9);

                    if ((self.player.input[0].y != 0 || self.player.input[0].x != 0 || self.player.input[0].jmp) && self.player.bodyMode != Player.BodyModeIndex.WallClimb && self.player.bodyMode != BodyModeIndexExtension.CeilCrawl
                        || (self.player.input[0].y != 0 || self.player.input[0].jmp) && self.player.bodyMode == Player.BodyModeIndex.WallClimb
                        || (self.player.input[0].x != 0 || self.player.input[0].jmp) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                    {
                        self.player.Die();
                    }
                    break;
                }
        }
    }
#nullable disable

#nullable enable
    static void GrabDataPearlAndDestroyIt(SSOracleBehavior self, DataPearl? pearl)
    {
        //kill realized pearl, replace with a custom one seamlessly
        if (pearl == null)
        {
            self.conversation.Destroy();
            return;
        }
        pearl.AllGraspsLetGoOfThisObject(true);
        pearl.slatedForDeletetion = true;
        var roomref = self.oracle.room;
        //oh my god DataPearl ctor takes in world but does nothing with it
        var hoveringPearl = new HoveringPearl(pearl.abstractPhysicalObject, roomref.world);
        pearl.abstractPhysicalObject.realizedObject = hoveringPearl;
        hoveringPearl.bodyChunks[0].pos = pearl.bodyChunks[0].pos;
        hoveringPearl.hoverPos = roomref.MiddleOfTile(roomref.Width / 2, roomref.Height / 2);
        hoveringPearl.OnPearlTaken += () =>
        {
            self.dialogBox.Interrupt("Good try, but it is not for you.".TranslateString(), 200);
            DestroyPearl(self, hoveringPearl);
        };
        hoveringPearl.OnWaitCompleted += () =>
        {
            DestroyPearl(self, hoveringPearl);
        };
        hoveringPearl.PlaceInRoom(roomref);
        hoveringPearl.AsyncWait(15000);

        static void DestroyPearl(OracleBehavior oracleBehavior, DataPearl pearl)
        {
            pearl.slatedForDeletetion = true;
            pearl.AbstractPearl.Destroy();
            for (int num8 = 0; num8 < 5; num8++)
            {
                oracleBehavior.oracle.room.AddObject(new Spark(pearl.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
            }
            oracleBehavior.oracle.room.AddObject(new Explosion.ExplosionLight(pearl.firstChunk.pos, 150f, 1f, 8, Color.white));
            oracleBehavior.oracle.room.AddObject(new ShockWave(pearl.firstChunk.pos, 60f, 0.1f, 8, false));
            oracleBehavior.oracle.room.PlaySound(SoundID.Snail_Pop, pearl.firstChunk, false, 1f, 1.5f + UnityEngine.Random.value * 0.5f);
        }
    }
#nullable disable
    private static void ILSSOracleBehavior_Update(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After, i => i.MatchLdstr("Yes, help yourself. They are not edible.")))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<string, SSOracleBehavior, string>>((str, self) =>
            {
                var amountOfPreviousMeetings = self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1;
                if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void
                && amountOfPreviousMeetings > 0
                && amountOfPreviousMeetings < OracleConversation.pickInterruptMessages.Length)
                {
                    return OracleConversation.pickInterruptMessages[amountOfPreviousMeetings];
                }
                PebbleVoice(self);
                return str;
            });
        }
        else LogExErr("failed to match eating string");

        c.Index = 0;

        if (c.TryGotoNext(MoveType.Before,
            i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"),
            i => i.MatchCall(out _),
            i => i.MatchBrfalse(out _)))
        {
            // Перемещаемся после call op_Inequality
            c.Index += 2;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 10);
            c.EmitDelegate<Func<bool, SSOracleBehavior, bool, bool>>((result, self, flag2) =>
            {
                bool isVoid = self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void;
                return isVoid || result;
            });
        }
        else
        {
            LogExErr("failed to match comparison to artificer");
        }
    }

    public static class OracleConversation
    {
        public static Conversation.ID[] PebbleVoidConversation;
        public static Conversation.ID[] MoonVoidConversation;

        public static int[] cycleLingers = [0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];
        public static int[] MooncycleLingers = [0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];

        static OracleConversation()
        {
            PebbleVoidConversation = new Conversation.ID[11];
            for (int i = 0; i < PebbleVoidConversation.Length; i++)
                PebbleVoidConversation[i] = new($"pebblevoidconversation_{i + 1}", true);
            MoonVoidConversation = new Conversation.ID[11];
            for (int i = 0; i < MoonVoidConversation.Length; i++)
                MoonVoidConversation[i] = new($"moonvoidconversation_{i + 1}", true);
        }

        public static string[] pickInterruptMessages =
        [
            "Yes, help yourself. They are not edible.",
            "You seem to like pearls.",
            "Are you listening to me?",
            "You need to stop being distracted by pearls.",
            "Are you trying my patience?",
            "I will tell you about this pearls another time.",
            "Yes... this pearl contains data about my structure.",
            "Strange... This pearl should not be here.",
            "This pearl made by many generations, it contains the stories, technologies and thoughts of long-gone civilization.",
            "You may notice that this pearl is slightly faded, unfortunately, even in them, information is not eternal.",
            "I wish I could just teach you how to read them so you would stop bothering me about little things like that."
        ];

        public static string[] eatInterruptMessages =
        [
            "I am not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature, you should not eat pearls.",
            "You must stop right now.",
            "I am warning you for the last time."
        ];

        public static string[] eatInterruptMessages6Step =
        [
            "I am not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature, you should not eat pearls.",
            "Can you stop dissolving my pearls?",
            "You just ate something more valuable than you can imagine.",
            "These pearls that you have swallowed, do they disappear without a trace or just become a part of you? In any case, I cannot get it back.",
            "Considering that your body weight does not change, this means that all the objects you eat are dissolved by the void fluid.",
            "If to assume that all the water in your body has been displaced by the void fluid,<LINE>its concentration is still insufficient to dissolve objects in such a short period of time.",
            "Watching you, I can assume that your body is invisibly connected to the Void Sea, but this thought alone raises even more questions.",
            "I would never have thought that such wasteful use of pearls would bring me closer to understanding the nature of the Void Sea.",
            "Eat as much as you want, from now I will no longer store important information here in your presence."
        ];

        public static string[] regurgitateInterruptMessages =
        [
            "I am not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature, you should not eat pearls.",
            "You must stop right now.",
            "I am warning you for the last time."
        ];

        public static string[] regurgitateInterruptMessages6Step =
        [
            "I am not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature, you should not eat pearls.",
            "Can you stop dissolving my pearls?",
            "You just ate something more valuable than you can imagine.",
            "These pearls that you have swallowed, do they disappear without a trace or just become a part of you? In any case, I cannot get it back.",
            "Considering that your body weight does not change, this means that all the objects you eat are dissolved by the void fluid.",
            "If to assume that all the water in your body has been displaced by the void fluid,<LINE>its concentration is still insufficient to dissolve objects in such a short period of time.",
            "Watching you, I can assume that your body is invisibly connected to the Void Sea, but this thought alone raises even more questions.",
            "I would never have thought that such wasteful use of pearls would bring me closer to understanding the nature of the Void Sea.",
            "Eat as much as you want, from now I will no longer store important information here in your presence."
        ];
    }
    public class SSOracleVoidBehavior : SSOracleBehavior.ConversationBehavior
    {
        public SSOracleVoidBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.PebbleVoidConversation[times - 1])
        {
            if (ModManager.MMF && owner.oracle.room.game.IsStorySession
                               && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked &&
                               oracle.room.world.rainCycle.timer > oracle.room.world.rainCycle.cycleLength / 4)
            {
                oracle.room.world.rainCycle.timer = oracle.room.world.rainCycle.cycleLength / 4;
                oracle.room.world.rainCycle.dayNightCounter = 0;
            }


        }

        public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            if (newAction == MeetVoid_Init && owner.conversation == null)
            {
                owner.InitateConversation(OracleConversation.PebbleVoidConversation[MeetTimes - 1], this);
                base.NewAction(oldAction, newAction);
            }

        }

        int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;
    }
    private static void PebbleVoice(SSOracleBehavior self)
    {
        SoundID randomTalk = SoundID.SS_AI_Talk_1;
        switch (UnityEngine.Random.Range(0, 5))
        {
            case 0:
                randomTalk = SoundID.SS_AI_Talk_1;
                break;
            case 1:
                randomTalk = SoundID.SS_AI_Talk_2;
                break;
            case 2:
                randomTalk = SoundID.SS_AI_Talk_3;
                break;
            case 3:
                randomTalk = SoundID.SS_AI_Talk_4;
                break;
            case 4:
                randomTalk = SoundID.SS_AI_Talk_5;
                break;
        }
        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
        {
            self.AirVoice(randomTalk);
        }
    }
}

public class SSOracleMeetVoid_CuriousBehavior : SSOracleBehavior.ConversationBehavior
{
    public static SSOracleBehavior.Action MeetVoid_Talking = new("MeetVoid_Talking", true);
    public static SSOracleBehavior.Action MeetVoid_Texting = new("MeetVoid_Texting", true);
    public static SSOracleBehavior.Action MeetVoid_FirstImages = new("MeetVoid_FirstImages", true);
    public static SSOracleBehavior.Action MeetVoid_SecondCurious = new("MeetVoid_SecondCurious", true);
    public static SSOracleBehavior.Action MeetVoid_SecondMeetsSameCycle = new("MeetVoid_SecondMeetsSameCycle", true);
    public static SSOracleBehavior.Action MeetVoid_SecondMeets = new("MeetVoid_SecondMeets", true);
    public static SSOracleBehavior.Action MeetVoid_ThirdMeets = new("MeetVoid_ThirdMeets", true);
    public static SSOracleBehavior.Action MeetVoid_FourMeets = new("MeetVoid_FourMeets", true);
    public static SSOracleBehavior.Action MeetVoid_Heal = new("MeetVoid_Heal", true);
    int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;

    public ChunkSoundEmitter Voice
    {
        get
        {
            return owner.voice;
        }
        set
        {
            owner.voice = value;
        }
    }

    public SSOracleMeetVoid_CuriousBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.PebbleVoidConversation[times - 1])
    {
        chatLabel = new OracleChatLabel(owner);
        showMediaPos = new Vector2(400f, 300f);
        oracle.room.AddObject(chatLabel);
        chatLabel.Hide();
        if (ModManager.MMF && owner.oracle.room.game.IsStorySession && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked && oracle.room.world.rainCycle.timer > oracle.room.world.rainCycle.cycleLength / 4)
        {
            oracle.room.world.rainCycle.timer = oracle.room.world.rainCycle.cycleLength / 4;
            oracle.room.world.rainCycle.dayNightCounter = 0;
        }
    }

    public override void Update()
    {
        base.Update();
        if (player == null)
        {
            return;
        }

        owner.LockShortcuts();
        owner.getToWorking = 0f;

        if (action == MeetVoid_Curious)
        {
            if (inActionCounter < 360)
            {
                owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
            }
            else
            {
                owner.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
            }
            if (inActionCounter > 360 && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad == 5)
            {
                owner.NewAction(MeetVoid_Heal);
                return;
            }
            if (inActionCounter > 360)
            {
                owner.NewAction(MeetVoid_Talking);
                return;
            }
        }
        else if (action == MeetVoid_Talking)
        {
            owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
            if (!CurrentlyCommunicating && communicationPause > 0)
            {
                communicationPause--;
            }
            if (!CurrentlyCommunicating && communicationPause < 1)
            {
                if (communicationIndex >= 4)
                {
                    owner.NewAction(MeetVoid_Texting);
                }
                else if (owner.allStillCounter > 20)
                {
                    NextCommunication();
                }
            }
            if (!CurrentlyCommunicating)
            {
                owner.nextPos += Custom.RNV();
                return;
            }
        }
        else
        {
            if (action == MeetVoid_Texting)
            {
                movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                if (oracle.graphicsModule != null)
                {
                    (oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 0f;
                }
                if (!CurrentlyCommunicating && communicationPause > 0)
                {
                    communicationPause--;
                }
                if (!CurrentlyCommunicating && communicationPause < 1)
                {
                    if (communicationIndex >= 6)
                    {
                        owner.NewAction(MeetVoid_FirstImages);
                    }
                    else if (owner.allStillCounter > 20)
                    {
                        NextCommunication();
                        communicationPause = 150;
                    }
                }
                return;
            }
            if (action == MeetVoid_FirstImages)
            {
                movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                if (communicationPause > 0)
                {
                    communicationPause--;
                }

                if (inActionCounter > 150 && communicationPause < 1)
                {
                    if (action == MeetVoid_FirstImages && communicationIndex >= 3)
                    {
                        owner.NewAction(MeetVoid_SecondCurious);
                    }
                    else
                    {
                        NextCommunication();
                    }
                }
                if (showImage != null)
                {
                    showImage.setPos = new Vector2?(showMediaPos);
                }
                if (UnityEngine.Random.value < 0.0333333351f)
                {
                    idealShowMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                    showMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                    return;
                }
            }
            if (action == SSOracleBehavior.Action.General_GiveMark)
            {
                if (inActionCounter == 300 && player.KarmaCap != 10)
                {
                    HunterSpasms.Spasm(player);
                }
            }
            else if (action == MeetVoid_SecondCurious)
            {
                movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                if (inActionCounter == 80)
                {
                    Custom.Log(
                    [
                        "extra talk"
                    ]);
                    Voice = oracle.room.PlaySound(SoundID.SS_AI_Talk_5, oracle.firstChunk);
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        dialogBox.Interrupt(". . .".TranslateString(), 60);
                        dialogBox.NewMessage("I can see by your face that you understand me.".TranslateString(), 60);
                    }
                    Voice.requireActiveUpkeep = true;
                }
                if (inActionCounter > 240)
                {
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap != 10)
                    {
                        owner.NewAction(SSOracleBehavior.Action.General_GiveMark);
                    }
                    if (owner.conversation != null)
                    {
                        owner.conversation.paused = false;
                    }
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        owner.NewAction(owner.afterGiveMarkAction);
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                }
                return;
            }
        }

        if (owner.conversation != null
            && owner.conversation.slatedForDeletion == true)
        {
            this.SSOracleVoidCommonConvoEnd();
        }
    }

    public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
    {
        base.NewAction(oldAction, newAction);
        if (oldAction == MeetVoid_Texting)
        {
            chatLabel.Hide();
        }
        if ((oldAction == MeetVoid_FirstImages) && showImage != null)
        {
            showImage.Destroy();
            showImage = null;
        }
        if (newAction == MeetVoid_Curious)
        {
            owner.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
            owner.invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1f : 1f);
            return;
        }
        if (newAction == MeetVoid_Texting)
        {
            communicationPause = 170;
            chatLabel.pos = showMediaPos;
            chatLabel.lastPos = showMediaPos;
            return;
        }
        if (newAction == MeetVoid_Init && owner.conversation == null)
        {
            owner.InitateConversation(OracleConversation.PebbleVoidConversation[MeetTimes - 1], this);
        }
    }

    public override void Deactivate()
    {
        chatLabel.Hide();
        showImage?.Destroy();
        Voice = null;
        base.Deactivate();
    }

    private void NextCommunication()
    {
        Custom.Log(
        [
            string.Format("New com att: {0} {1}", action, communicationIndex)
        ]);
        //UnityEngine.Debug.Log("NextCommu");
        if (action == MeetVoid_Talking)
        {
            switch (communicationIndex)
            {
                case 0:
                    Voice = oracle.room.PlaySound(SoundID.SS_AI_Talk_1, oracle.firstChunk);
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        dialogBox.Interrupt("Did someone send a messenger to me?".TranslateString(), 60);
                    }
                    Voice.requireActiveUpkeep = true;
                    communicationPause = 10;
                    break;
                case 1:
                    Voice = oracle.room.PlaySound(SoundID.SS_AI_Talk_2, oracle.firstChunk);
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        dialogBox.Interrupt("It does not have a mark.".TranslateString(), 30);
                        dialogBox.NewMessage("It is just another pest was able to get into my structure.".TranslateString(), 30);
                    }
                    Voice.requireActiveUpkeep = true;
                    communicationPause = 70;
                    break;
                case 2:
                    Voice = oracle.room.PlaySound(SoundID.SS_AI_Talk_3, oracle.firstChunk);
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        dialogBox.Interrupt("You look unnatural.".TranslateString(), 60);
                        dialogBox.NewMessage("What happened to you? There are clear signs of external interference here.".TranslateString(), 60);
                    }
                    Voice.requireActiveUpkeep = true;
                    break;
                case 3:
                    Voice = oracle.room.PlaySound(SoundID.SS_AI_Talk_4, oracle.firstChunk);
                    if (oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                    {
                        dialogBox.Interrupt("A rather strange creature.".TranslateString(), 60);
                    }
                    Voice.requireActiveUpkeep = true;
                    communicationPause = 140;
                    break;
            }
        }
        else if (base.action == MeetVoid_Texting)
        {
            this.chatLabel.NewPhrase(this.communicationIndex);
        }
        else if (base.action == MeetVoid_FirstImages)
        {
            if (this.showImage != null)
            {
                this.showImage.Destroy();
            }

            switch (this.communicationIndex)
            {
                case 0:
                    this.showImage = base.oracle.myScreen.AddImage("aiimg1_void");
                    this.communicationPause = 380;
                    break;
                case 1:
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap == 10)
                        this.showImage = base.oracle.myScreen.AddImage("aiimg2_void");
                    else
                        this.showImage = base.oracle.myScreen.AddImage("aiimg3_void");
                    this.communicationPause = 290;
                    break;
                case 2:
                    if (this.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap != 10)
                    {
                        this.Voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, base.oracle.firstChunk);
                        this.showImage = base.oracle.myScreen.AddImage(new List<string>
                        {
                            "void_glyphs_3",
                            "void_glyphs_5"
                        }, 30);
                    }
                    else
                    {
                        this.Voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_3, base.oracle.firstChunk);
                        this.showImage = base.oracle.myScreen.AddImage(new List<string>
                        {
                            "void_glyphs_4",
                            "void_glyphs_5"
                        }, 30);
                        this.dialogBox.Interrupt("Three... four spirals. The genes are twisted into a super-dense structure. This form is almost immune to the external environment.".TranslateString(), 60);
                    }
                    this.communicationPause = 330;
                    break;
            }

            if (this.showImage != null)
            {
                base.oracle.room.PlaySound(SoundID.SS_AI_Image, 0f, 1f, 1f);
                this.showImage.lastPos = this.showMediaPos;
                this.showImage.pos = this.showMediaPos;
                this.showImage.lastAlpha = 0f;
                this.showImage.alpha = 0f;
                this.showImage.setAlpha = new float?(1f);
            }
        }
        this.communicationIndex++;
    }

    public void ShowMediaMovementBehavior()
    {
        if (base.player != null)
        {
            this.owner.lookPoint = base.player.DangerPos;
        }
        Vector2 vector = new Vector2(UnityEngine.Random.value * base.oracle.room.PixelWidth, UnityEngine.Random.value * base.oracle.room.PixelHeight);
        if (this.owner.CommunicatePosScore(vector) + 40f < this.owner.CommunicatePosScore(this.owner.nextPos) && !Custom.DistLess(vector, this.owner.nextPos, 30f))
        {
            this.owner.SetNewDestination(vector);
        }
        this.consistentShowMediaPosCounter += (int)Custom.LerpMap(Vector2.Distance(this.showMediaPos, this.idealShowMediaPos), 0f, 200f, 1f, 10f);
        vector = new Vector2(UnityEngine.Random.value * base.oracle.room.PixelWidth, UnityEngine.Random.value * base.oracle.room.PixelHeight);
        if (this.ShowMediaScore(vector) + 40f < this.ShowMediaScore(this.idealShowMediaPos))
        {
            this.idealShowMediaPos = vector;
            this.consistentShowMediaPosCounter = 0;
        }
        vector = this.idealShowMediaPos + Custom.RNV() * UnityEngine.Random.value * 40f;
        if (this.ShowMediaScore(vector) + 20f < this.ShowMediaScore(this.idealShowMediaPos))
        {
            this.idealShowMediaPos = vector;
            this.consistentShowMediaPosCounter = 0;
        }
        if (this.consistentShowMediaPosCounter > 300)
        {
            this.showMediaPos = Vector2.Lerp(this.showMediaPos, this.idealShowMediaPos, 0.1f);
            this.showMediaPos = Custom.MoveTowards(this.showMediaPos, this.idealShowMediaPos, 10f);
        }
    }

    private float ShowMediaScore(Vector2 tryPos)
    {
        if (base.oracle.room.GetTile(tryPos).Solid || base.player == null)
        {
            return float.MaxValue;
        }
        float num = Mathf.Abs(Vector2.Distance(tryPos, base.player.DangerPos) - 250f);
        num -= Math.Min((float)base.oracle.room.aimap.getTerrainProximity(tryPos), 9f) * 30f;
        num -= Vector2.Distance(tryPos, this.owner.nextPos) * 0.5f;
        for (int i = 0; i < base.oracle.arm.joints.Length; i++)
        {
            num -= Mathf.Min(Vector2.Distance(tryPos, base.oracle.arm.joints[i].pos), 100f) * 10f;
        }
        if (base.oracle.graphicsModule != null)
        {
            for (int j = 0; j < (base.oracle.graphicsModule as OracleGraphics).umbCord.coord.GetLength(0); j += 3)
            {
                num -= Mathf.Min(Vector2.Distance(tryPos, (base.oracle.graphicsModule as OracleGraphics).umbCord.coord[j, 0]), 100f);
            }
        }
        return num;
    }

    public override bool CurrentlyCommunicating
    {
        get
        {
            return base.CurrentlyCommunicating || this.Voice != null || (base.action == SSOracleBehavior.Action.MeetWhite_Texting && !this.chatLabel.finishedShowingMessage) || this.showImage != null;
        }
    }

    public ProjectedImage showImage;
    public Vector2 idealShowMediaPos;
    public Vector2 showMediaPos;
    public int consistentShowMediaPosCounter;
    public OracleChatLabel chatLabel;
}
public static class OracleExtensionMethods
{
    public static void AirVoice(this SSOracleBehavior self, SoundID line)
    {
        if (self.voice != null)
        {
            self.voice.currentSoundObject?.Stop();
            self.voice.Destroy();
        }
        self.voice = self.oracle.room.PlaySound(line, self.oracle.firstChunk);
    }
}