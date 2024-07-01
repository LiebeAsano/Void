using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using TheVoid;
using UnityEngine;
using Nutils.hook;

namespace VoidTemplate
{
    static class OracleHooks
    {
        public static void Hook()
        {
            On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
            On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
            IL.SSOracleBehavior.Update += SSOracleBehavior_Update;


        }

        private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            orig(self);
            if (self.id == Moon_VoidConversation)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello little... creature. Is it you?.."), 50));
                self.events.Add(new Conversation.TextEvent(self, 0,  self.Translate("Oh, I'm sorry, I was mistaken."), 50));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("My memory is failing me and I don't know what to do about it..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 100));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Little creature..."), 15));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Are you feeling okay?"), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I've never seen a creature in a more horrible state."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Your body... is still trying to maintain such an unstable structure."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You remind me of a friend of mine."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I miss him."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("..."), 100));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I realise it's selfish of me to ask you in such... state, but could you please pass on a message to my friend."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("His name is No Significant Harassment, you have to overcome the wall that surrounds Five Pebble's terriory to get to him..."), 0));
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I'm sorry, I can't ask a little animal for this..."), 0));
            }
        }

        public static SSOracleBehavior.Action MeetVoid_Init = new  ("MeetVoid_Init", true);
        public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new ("VoidTalk", true);
  
        public static Conversation.ID Moon_VoidConversation = new ("Moon_VoidConversation", true);


        public static void EatPearlsInterrupt(this SSOracleBehavior self)
        {
            if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.TryGetCustomValue(
                    Plugin.SaveName, out VoidSave save))
            {
                if (self.conversation != null)
                {
                    self.conversation.paused = true;
                    self.restartConversationAfterCurrentDialoge = true;
                }

                if (save.eatCounter == 11)
                {
                    if (self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad < 6)
                    {
                        self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
                        self.getToWorking = 1f;
                    }

                }
                else
                {
                    self.dialogBox.Interrupt(self.Translate(
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 6
                            ? SSConversation.eatInterruptMessages6Step[save.eatCounter]
                            : SSConversation.eatInterruptMessages[save.eatCounter]), 10);
                    save.eatCounter++;
                }
            }
        }

        public static class SSConversation
        {
            public static Conversation.ID[] VoidConversation;
            public static Conversation.ID[] MoonVoidConversation;

            public static int[] cycleLingers = new[] { 0, 1, 1, 0, 1, 0, 1, 0, 1, 1, 0 };
            public static int[] MooncycleLingers = new[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            static SSConversation()
            {
                VoidConversation = new Conversation.ID[11];
                for (int i = 0; i < VoidConversation.Length; i++)
                    VoidConversation[i] = new($"VoidConversation_{i + 1}", true);
                MoonVoidConversation = new Conversation.ID[11];
                for (int i = 0; i < MoonVoidConversation.Length; i++)
                    MoonVoidConversation[i] = new($"MoonVoidConversation_{i + 1}", true);
            }

            public static string[] pickInterruptMessages = new[]
            {
                "Yes, help yourself. They are not edible.",
                "You seem to like pearls.",
                "Are you listening to me?",
                "You need to stop being distracted by pearls.",
                "Are you trying my patience?",
                "...never mind...",
                "Yes... this pearl contains data about my structure.",
                "Strange.. This pearl shouldn't be here.",
                "There is nothing useful in this pearl, you can eat it.",
                "This pearl contains information about failed experiments.",
                "I should back up the pearls more often..."
            };

            public static string[] eatInterruptMessages = new[]
            {
                "I'm not sure you can stomach them.",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10",
                "11"
            };

            public static string[] eatInterruptMessages6Step = new[]
            {
                "1-6",
                "2-6",
                "3-6",
                "4-6",
                "5-6",
                "6-6",
                "7-6",
                "8-6",
                "9-6",
                "10-6",
                "11-6"
            };


            
        }

        private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
        {
            if (!self.State.SpeakingTerms)
            {
                self.dialogBox.NewMessage("...", 10);
                return;
            }
            if ((self.oracle.room.game.session is StoryGameSession session) && session.characterStats.name == Plugin.TheVoid &&
                self.State.playerEncountersWithMark <= 0)
            {
                if (self.State.playerEncounters < 0)
                {
                    self.State.playerEncounters = 0;
                }
                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(Moon_VoidConversation, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                return;
            }
            orig(self);
        }

        private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            orig(self);

            if (SSConversation.VoidConversation.Contains(self.id))
            {
                if (!self.owner.playerEnteredWithMark)
                    self.events.Add(new Conversation.TextEvent(self, 0, ".  .  . ", 0));
                else
                    self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 30));
                
                var path = AssetManager.ResolveFilePath($"text/oracle/{self.id.value.ToLower()}.txt");
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        var split = LocalizationTranslator.ConsolidateLineInstructions(line);
                        if (split.Length == 3)
                            self.events.Add(new Conversation.TextEvent(self, int.Parse(split[0]),
                                self.Translate(split[1]), int.Parse(split[2])));
                        else
                            self.events.Add(new Conversation.TextEvent(self, 0,self.Translate(line), 0));
                    }
                }
                else
                {
                    //DEBUG
                    self.events.Add(new Conversation.TextEvent(self, 0,
                        $"text/oracle/{self.id.value.ToLower()}.txt can't find!", 0));
                }
            }
        }



        private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
        {
            if (nextAction == MeetVoid_Init)
            {
                var behaviorID = VoidTalk;

                self.inActionCounter = 0;
                self.action = nextAction;
                if (self.currSubBehavior.ID == behaviorID)
                {
                    self.currSubBehavior.Activate(self.action, nextAction);
                    return;
                }
                SSOracleBehavior.SubBehavior subBehavior = null;
                for (int i = 0; i < self.allSubBehaviors.Count; i++)
                {
                    if (self.allSubBehaviors[i].ID == behaviorID)
                    {
                        subBehavior = self.allSubBehaviors[i];
                        break;
                    }
                }

                if (subBehavior == null)
                {
                    subBehavior = new SSOracleVoidBehavior(self,
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad);
                    self.allSubBehaviors.Add(subBehavior);
                }
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
            bool seePeople = false;
            foreach (var player in self.oracle.room.game.Players)
                if (player.realizedCreature is Player)
                    seePeople = true;
            
            if (seePeople && self.oracle.room.game.session.characterStats.name == Plugin.TheVoid &&
                self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.
                    TryGetCustomValue(Plugin.SaveName, out VoidSave data))
            {
                var saveState = self.oracle.room.game.GetStorySession.saveState;
                var miscData = saveState.miscWorldSaveData;
                var need = miscData.SSaiConversationsHad >= 10
                    ? -1
                    : SSConversation.cycleLingers[miscData.SSaiConversationsHad];
                Debug.Log($"[The Void] HadConv: {miscData.SSaiConversationsHad}, Cycle: {saveState.cycleNumber}, LastCycle: {data.lastMeetCycles}, NeedCycle: {need}");
                if (miscData.SSaiConversationsHad >= 10)
                {
                    //Maybe changed
                    self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                }
                else if ((miscData.SSaiConversationsHad >= 5 && miscData.SLOracleState.playerEncountersWithMark <= 0) ||
                         (miscData.SSaiConversationsHad == 3 && saveState.deathPersistentSaveData.karmaCap < 5) ||
                         (miscData.SSaiConversationsHad == 7 && saveState.deathPersistentSaveData.karmaCap < 8) ||
                    ((saveState.cycleNumber - data.lastMeetCycles) < SSConversation.cycleLingers[miscData.SSaiConversationsHad]))
                {
                    self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                }
                else if (self.action != MeetVoid_Init)
                {
                    data.lastMeetCycles = saveState.cycleNumber;
                    if (self.timeSinceSeenPlayer < 0)
                    {
                        self.timeSinceSeenPlayer = 0;
                    }
                    if (self.currSubBehavior.ID != VoidTalk)
                    {
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad++;
                        if (self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark)
                        {
                            self.NewAction(MeetVoid_Init);
                            self.SlugcatEnterRoomReaction();
                            self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;

                        }
                        else
                        {
                            self.NewAction(SSOracleBehavior.Action.General_GiveMark);
                            self.afterGiveMarkAction = MeetVoid_Init;
                            self.SlugcatEnterRoomReaction();
                            self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        }
                    }

                }
            }
            else
            {
                orig(self);
            }
        }


        public class SSOracleVoidBehavior : SSOracleBehavior.ConversationBehavior
        {
            public SSOracleVoidBehavior(SSOracleBehavior owner,int times) : base(owner, VoidTalk,SSConversation.VoidConversation[times-1])
            {
                if (ModManager.MMF && owner.oracle.room.game.IsStorySession 
                                   && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked && 
                                   oracle.room.world.rainCycle.timer > oracle.room.world.rainCycle.cycleLength / 4)
                {
                    oracle.room.world.rainCycle.timer = oracle.room.world.rainCycle.cycleLength / 4;
                    oracle.room.world.rainCycle.dayNightCounter = 0;
                }

              
            }

            public override void Update()
            {
                base.Update();
                if (owner.conversation == null || owner.conversation.slatedForDeletion == true ||
                    owner.conversation.events == null)
                {
                    owner.UnlockShortcuts();
                    owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                    owner.getToWorking = 1f;
                }
            }

            public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
            {
                if (newAction == MeetVoid_Init && owner.conversation == null)
                {
                    owner.InitateConversation(SSConversation.VoidConversation[MeetTimes-1], this);
                    base.NewAction(oldAction, newAction);
                }

            }

            int MeetTimes =>  owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;
        }
        private static void SSOracleBehavior_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILCursor c2 = new ILCursor(il);
            c.GotoNext(MoveType.After,
                i => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Artificer"),
                i => i.MatchCall(out var method) && method.Name.Contains("op_Inequality"),
                i => i.Match(OpCodes.Brfalse_S),
                i => i.MatchLdloc(10));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, SSOracleBehavior, bool>>((re, self)
                => self.oracle.room.game.StoryCharacter == Plugin.TheVoid || re);

            c2.GotoNext(MoveType.After,i => i.MatchLdstr("Yes, help yourself. They are not edible."));
            c2.Emit(OpCodes.Ldarg_0);
            c2.EmitDelegate<Func<string, SSOracleBehavior, string>>((str, self) =>
            {
                if (self.oracle.room.game.session.characterStats.name == Plugin.TheVoid &&
                    SSConversation.pickInterruptMessages.Length >
                    self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad-1)
                {
                    return SSConversation.pickInterruptMessages[
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad-1];
                }

                return str;
            });
        }

    }
}
