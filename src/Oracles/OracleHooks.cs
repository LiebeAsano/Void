using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using VoidTemplate.Useful;
using static VoidTemplate.VoidEnums.ConversationID;
using RWCustom;

namespace VoidTemplate.Oracles;

static class OracleHooks
{
    public static void Hook()
    {
        On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
        On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
        On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;
        On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehaviorHasMark_InitateConversation;
        On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += AddEventsByID;
        IL.SSOracleBehavior.Update += SSOracleBehavior_Update;
        On.OracleChatLabel.DrawSprites += OracleChatLabel_DrawSprites;
    }

    private static void OracleChatLabel_DrawSprites(On.OracleChatLabel.orig_DrawSprites orig, OracleChatLabel self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.visibleGlyphs > 0 || self.totalGlyphsToShow > 0)
        {
            UnityEngine.Debug.Log("Visible Label: " + self.visibleGlyphs);
            UnityEngine.Debug.Log("Total Label: " + self.totalGlyphsToShow);
        }
        UnityEngine.Debug.Log("Reveal: " + self.revealCounter);
        UnityEngine.Debug.Log("Inplace Counter: " + self.inPlaceCounter);
        //UnityEngine.Debug.Log("Visible Label: " + self.visibleGlyphs);
        //for (int i = 0; i < sLeaser.sprites.Length; i++)
        {
            //UnityEngine.Debug.Log("Label " + i + "; Pos: " + (sLeaser.sprites[i].GetPosition() + camPos).ToString() + "; Alpha: " + sLeaser.sprites[i].alpha);           
            //UnityEngine.Debug.Log("Visible: " + sLeaser.sprites[i].isVisible);
            //UnityEngine.Debug.Log("Glyphs: " + self.glyphs[i]);
        }
    }

    private static void Logerr(object e) => _Plugin.logger.LogError(e);
    private static void Loginf(object e) => _Plugin.logger.LogInfo(e);
    #region Moon look up conversation
    /// <summary>
    /// This thing checks the ID that conversation gets when it is created and looks up file in {anymod}/text/RainWorldLastWishMoonConversations/{ID}.txt
    /// Use >> to split linger time and string
    /// Example: "   5>>I am not sure what this means!   "
    /// </summary>
    #region immutable
    private static readonly Conversation.ID[] modSpecificConversations = new Conversation.ID[] { Moon_VoidConversation };
    #endregion
    private static (int, string) ParseLine(string line)
    {
        string[] res = line.Split(new string[] { ">>" }, StringSplitOptions.None);
        if (res.Length != 2) Logerr($"the line \"{line}\" was invalid for parsing (splitting with '>>' resulted in non-two array)");
        if (!int.TryParse(res[0], out int value)) Logerr($"the line \"{line}\" has invalid int number before '>>'");
        return (value, res[1]);
    }
    private static void AddEventsByID(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
    {
        orig(self);
        if (Array.Exists(modSpecificConversations, x => self.id == x) || Array.Exists(OracleConversation.MoonVoidConversation, x => self.id == x)) //if id is from this mod
        {
            string path = AssetManager.ResolveFilePath("text/RainWorldLastWishMoonConversations/" + self.id + ".txt"); //look it up in our specific folder
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                Array.ForEach(lines, line =>
                {
                    var q = ParseLine(line);
                    self.events.Add(new Conversation.TextEvent(self, 0, Utils.TranslateStringComplex(q.Item2), q.Item1));
                });
            }
            else Logerr($"the path '{path}' has no existing file. No events were loaded.");
        }
    }
    #endregion
    public static SSOracleBehavior.Action MeetVoid_Init = new("MeetVoid_Init", true);
    public static SSOracleBehavior.Action MeetVoid_Curious = new("MeetVoid_Curious", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new("VoidTalk", true);
    public static SSOracleBehavior.SubBehavior.SubBehavID VoidScan = new("VoidScan", true);
    public static List<ProjectedImage> Void_projectImages = new List<ProjectedImage>();

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
        if (amountOfEatenPearls == 6
        && self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad < 6)
        {

            self.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
            self.getToWorking = 1f;


        }
        else if (amountOfEatenPearls < 12)
        {
            self.dialogBox.Interrupt(self.Translate(
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 6
                    ? OracleConversation.eatInterruptMessages6Step[amountOfEatenPearls]
                    : OracleConversation.eatInterruptMessages[amountOfEatenPearls]), 10);
            savestate.SetPebblesPearlsEaten(savestate.GetPebblesPearlsEaten() + 1);
        }

    }

    public static class OracleConversation
    {
        public static Conversation.ID[] VoidConversation;
        public static Conversation.ID[] MoonVoidConversation;

        public static int[] cycleLingers = new int[] { 0, 1, 0, 1, 1, 0, 2, 2, 2, 2, 0 };
        public static int[] MooncycleLingers = new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        static OracleConversation()
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
            "I will tell you about this pearls another time.",
            "Yes... this pearl contains data about my structure.",
            "Strange... This pearl shouldn't be here.",
            "This pearl made by many generations, it contains the stories, technologies and thoughts of long-gone civilizations.",
            "You may notice that this pearl is slightly faded, unfortunately, even in them, information is not eternal.",
            "I wish I could just teach you how to read them..."
        };

        public static string[] eatInterruptMessages = new[]
        {
            "I'm not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature. You shouldn't eat pearls.",
            "You must stop right now.",
            "I'm warning you for the last time."
        };

        public static string[] eatInterruptMessages6Step = new[]
        {
            "I'm not sure you can stomach pearl.",
            ". . .",
            "Do you really eat them?",
            "Little creature. You shouldn't eat pearls.",
            "Can you stop dissolving my pearls?",
            "You just ate something more valuable than you can imagine.",
            "This pearl that you have swallowed, do they disappear without a trace or just become a part of you? In any case, I can't get it back.",
            "Considering that your body weight does not change, this means that all the objects you eat are simply dissolved by the void fluid.",
            "Although if to assume that all the water in your body has been displaced by the void fluid, its concentration is still insufficient to dissolve objects so fast.",
            "Watching you, I can assume that your body is invisibly connected to the void sea, but this thought alone raises even more questions.",
            "I would never have thought that such wasteful use of pearls could bring me closer to understanding the nature of the void fluid.",
            ""
        };



    }

    private static void SLOracleBehaviorHasMark_InitateConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
    {
        if (!self.State.SpeakingTerms)
        {
            self.dialogBox.NewMessage("...", 10);
            return;
        }
        if (self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void &&
            self.State.playerEncountersWithMark <= 0)
        {
            if (self.State.playerEncounters < 0)
            {
                self.State.playerEncounters = 0;
            }
            if (self.State.playerEncountersWithMark < OracleConversation.MoonVoidConversation.Length) self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(OracleConversation.MoonVoidConversation[self.State.playerEncountersWithMark], self, SLOracleBehaviorHasMark.MiscItemType.NA);
            return;
        }
        orig(self);
    }

    private static void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
    {
        orig(self);

        if (OracleConversation.VoidConversation.Contains(self.id))
        {
            if (!self.owner.playerEnteredWithMark)
            {
                self.events.Add(new Conversation.TextEvent(self, 0, ". . .", 0));
            }
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
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate(line), 0));
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
        UnityEngine.Debug.Log("New Action:" + nextAction.value);
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

            if (subBehavior == null)
            {
                subBehavior = new SSOracleMeetVoid_CuriousBehavior(self, self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad);
            }
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
        if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void
            && self.oracle.room.game.Players.Exists(x => x.realizedCreature is Player))
        {
            var saveState = self.oracle.room.game.GetStorySession.saveState;
            var miscData = saveState.miscWorldSaveData;
            var need = miscData.SSaiConversationsHad < OracleConversation.cycleLingers.Length
                ? OracleConversation.cycleLingers[miscData.SSaiConversationsHad]
                : -1;
            Loginf($"HadConv: {miscData.SSaiConversationsHad}, Cycle: {saveState.cycleNumber}, LastCycle: {saveState.GetLastMeetCycles()}, NeedCycle: {need}");

            switch (miscData.SSaiConversationsHad)
            {
                case 0:
                    {
                        miscData.SSaiConversationsHad++;
                        self.NewAction(MeetVoid_Curious);
                        UnityEngine.Debug.Log("MeetVoid_Curious");
                        self.SlugcatEnterRoomReaction();
                        self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Pebble);
                        break;
                    }
                case 2 when saveState.deathPersistentSaveData.karmaCap < 4:
                case < 5 when saveState.cycleNumber - saveState.GetLastMeetCycles() < OracleConversation.cycleLingers[miscData.SSaiConversationsHad]:
                case 5 when miscData.SLOracleState.playerEncountersWithMark <= 0:
                    {
                        self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                        break;
                    }
                case > 5 when saveState.cycleNumber - saveState.GetLastMeetCycles() < OracleConversation.cycleLingers[miscData.SSaiConversationsHad]:
                case > 10:
                    {
                        //Maybe changed
                        self.NewAction(MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty);
                        break;
                    }
                default:
                    {
                        if (self.action != MeetVoid_Init)
                        {
                            saveState.SetLastMeetCycles(saveState.cycleNumber);
                            if (self.timeSinceSeenPlayer < 0) self.timeSinceSeenPlayer = 0;
                            if (self.currSubBehavior.ID != VoidTalk)
                            {
                                miscData.SSaiConversationsHad++;
                                self.NewAction(MeetVoid_Init);
                                self.SlugcatEnterRoomReaction();
                                self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                            }
                        }
                        if (miscData.SSaiConversationsHad == 8)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Rot);
                        break;
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
        public SSOracleVoidBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.VoidConversation[times - 1])
        {
            if (ModManager.MMF && owner.oracle.room.game.IsStorySession
                               && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked &&
                               oracle.room.world.rainCycle.timer > oracle.room.world.rainCycle.cycleLength / 4)
            {
                oracle.room.world.rainCycle.timer = oracle.room.world.rainCycle.cycleLength / 4;
                oracle.room.world.rainCycle.dayNightCounter = 0;
            }

            owner.inActionCounter = 0;
        }

        public override void Update()
        {
            base.Update();
            UnityEngine.Debug.Log(owner.inActionCounter);
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
                owner.InitateConversation(OracleConversation.VoidConversation[MeetTimes - 1], this);
                base.NewAction(oldAction, newAction);
            }

        }

        int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;
    }
    private static void SSOracleBehavior_Update(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        ILCursor c2 = new ILCursor(il);
        c.GotoNext(MoveType.After,
            i => i.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"),
            i => i.MatchCall(out var method) && method.Name.Contains("op_Inequality"),
            i => i.Match(OpCodes.Brfalse_S),
            i => i.MatchLdloc(10));
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<bool, SSOracleBehavior, bool>>((re, self)
            => self.oracle.room.game.StoryCharacter == VoidEnums.SlugcatID.Void || re);

        c2.GotoNext(MoveType.After, i => i.MatchLdstr("Yes, help yourself. They are not edible."));
        c2.Emit(OpCodes.Ldarg_0);
        c2.EmitDelegate<Func<string, SSOracleBehavior, string>>((str, self) =>
        {
            if (self.oracle.room.game.session.characterStats.name == VoidEnums.SlugcatID.Void &&
                OracleConversation.pickInterruptMessages.Length >
                self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1)
            {
                return OracleConversation.pickInterruptMessages[
                    self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad - 1];
            }

            return str;
        });
    }

    public class SSOracleMeetVoid_CuriousBehavior : SSOracleBehavior.ConversationBehavior
    {
        public static SSOracleBehavior.Action MeetVoid_Talking = new SSOracleBehavior.Action("MeetVoid_Talking", true);
        public static SSOracleBehavior.Action MeetVoid_Texting = new SSOracleBehavior.Action("MeetVoid_Texting", true);
        public static SSOracleBehavior.Action MeetVoid_FirstImages = new SSOracleBehavior.Action("MeetVoid_FirstImages", true);
        public static SSOracleBehavior.Action MeetVoid_SecondCurious = new SSOracleBehavior.Action("MeetVoid_SecondCurious", true);
        int MeetTimes => owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad;

        public ChunkSoundEmitter voice
        {
            get
            {
                return this.owner.voice;
            }
            set
            {
                this.owner.voice = value;
            }
        }

        public SSOracleMeetVoid_CuriousBehavior(SSOracleBehavior owner, int times) : base(owner, VoidTalk, OracleConversation.VoidConversation[times - 1])
        {
            this.chatLabel = new OracleChatLabel(owner);
            this.showMediaPos = new Vector2(400f, 300f);
            this.oracle.room.AddObject(this.chatLabel);
            this.chatLabel.Hide();
            if (ModManager.MMF && owner.oracle.room.game.IsStorySession && owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked && base.oracle.room.world.rainCycle.timer > base.oracle.room.world.rainCycle.cycleLength / 4)
            {
                base.oracle.room.world.rainCycle.timer = this.oracle.room.world.rainCycle.cycleLength / 4;
                base.oracle.room.world.rainCycle.dayNightCounter = 0;
            }
        }

        public override void Update()
        {
            if (base.player == null)
            {
                return;
            }
            this.owner.LockShortcuts();
            this.owner.getToWorking = 0f;

            if (base.action == MeetVoid_Curious)
            {
                if (inActionCounter < 100)
                    this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                else
                    this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                if (inActionCounter > 360)
                {
                    this.owner.NewAction(MeetVoid_Talking);
                    UnityEngine.Debug.Log("MeetVoid_Talking");
                    return;
                }
            }
            else if (base.action == MeetVoid_Talking)
            {
                this.owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                if (!this.CurrentlyCommunicating && this.communicationPause > 0)
                {
                    this.communicationPause--;
                }
                if (!this.CurrentlyCommunicating && this.communicationPause < 1)
                {
                    if (this.communicationIndex >= 4)
                    {
                        this.owner.NewAction(MeetVoid_Texting);
                        UnityEngine.Debug.Log("MeetVoid_Texting");
                    }
                    else if (this.owner.allStillCounter > 20)
                    {
                        this.NextCommunication();
                        UnityEngine.Debug.Log("Next Commu");
                    }
                }
                if (!this.CurrentlyCommunicating)
                {
                    this.owner.nextPos += Custom.RNV();
                    return;
                }
            }
            else
            {
                if (base.action == MeetVoid_Texting)
                {
                    base.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                    if (base.oracle.graphicsModule != null)
                    {
                        (base.oracle.graphicsModule as OracleGraphics).halo.connectionsFireChance = 0f;
                    }
                    if (!this.CurrentlyCommunicating && this.communicationPause > 0)
                    {
                        this.communicationPause--;
                    }
                    if (!this.CurrentlyCommunicating && this.communicationPause < 1)
                    {
                        if (this.communicationIndex >= 6)
                        {
                            this.owner.NewAction(MeetVoid_FirstImages);
                            UnityEngine.Debug.Log("MeetVoid_FirstImages");
                        }
                        else if (this.owner.allStillCounter > 20)
                        {
                            this.NextCommunication();
                            this.communicationPause = 150;
                        }
                    }                   
                    return;
                }
                if (base.action == MeetVoid_FirstImages)
                {
                    base.movementBehavior = SSOracleBehavior.MovementBehavior.ShowMedia;
                    if (this.communicationPause > 0)
                    {
                        this.communicationPause--;
                    }

                    if (inActionCounter > 150 && this.communicationPause < 1)
                    {
                        if (base.action == MeetVoid_FirstImages && this.communicationIndex >= 3)
                        {
                            this.owner.NewAction(MeetVoid_SecondCurious);
                            UnityEngine.Debug.Log("MeetVoid_SecondCurious");
                        }
                        else
                        {
                            this.NextCommunication();
                        }
                    }
                    if (this.showImage != null)
                    {
                        this.showImage.setPos = new Vector2?(this.showMediaPos);
                    }
                    if (UnityEngine.Random.value < 0.0333333351f)
                    {
                        this.idealShowMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                        this.showMediaPos += Custom.RNV() * UnityEngine.Random.value * 30f;
                        return;
                    }
                }
                else if (base.action == MeetVoid_SecondCurious)
                {
                    base.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                    if (inActionCounter == 80)
                    {
                        Custom.Log(new string[]
                        {
                        "extra talk"
                        });
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_5, base.oracle.firstChunk);
                        this.voice.requireActiveUpkeep = true;
                    }
                    if (inActionCounter > 240)
                    {
                        this.owner.NewAction(SSOracleBehavior.Action.General_GiveMark);
                        this.owner.afterGiveMarkAction = MeetVoid_Init;
                    }
                    return;
                }                
            }

            if (owner.conversation != null && owner.conversation.slatedForDeletion == true)
            {
                owner.UnlockShortcuts();
                owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                owner.getToWorking = 1f;
            }
        }

        public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            base.NewAction(oldAction, newAction);
            if (oldAction == MeetVoid_Texting)
            {
                this.chatLabel.Hide();
            }
            if ((oldAction == MeetVoid_FirstImages) && this.showImage != null)
            {
                this.showImage.Destroy();
                this.showImage = null;
            }
            if (newAction == MeetVoid_Curious)
            {
                this.owner.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
                this.owner.invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1f : 1f);
                return;
            }
            if (newAction == MeetVoid_Texting)
            {
                this.communicationPause = 170;
                this.chatLabel.pos = this.showMediaPos;
                this.chatLabel.lastPos = this.showMediaPos;
                return;
            }
            if (newAction == MeetVoid_Init && owner.conversation == null)
            {
                owner.InitateConversation(OracleConversation.VoidConversation[MeetTimes - 1], this);
            }
        }

        public override void Deactivate()
        {
            this.chatLabel.Hide();
            if (this.showImage != null)
            {
                this.showImage.Destroy();
            }
            this.voice = null;
            base.Deactivate();
        }

        private void NextCommunication()
        {
            Custom.Log(new string[]
            {
            string.Format("New com att: {0} {1}", base.action, this.communicationIndex)
            });
            UnityEngine.Debug.Log("NextCommu");
            if (base.action == MeetVoid_Talking)
            {
                switch (this.communicationIndex)
                {
                    case 0:
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, base.oracle.firstChunk);
                        this.voice.requireActiveUpkeep = true;
                        this.communicationPause = 10;
                        break;
                    case 1:
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_2, base.oracle.firstChunk);
                        this.voice.requireActiveUpkeep = true;
                        this.communicationPause = 70;
                        break;
                    case 2:
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_3, base.oracle.firstChunk);
                        this.voice.requireActiveUpkeep = true;
                        break;
                    case 3:
                        this.voice = base.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, base.oracle.firstChunk);
                        this.voice.requireActiveUpkeep = true;
                        this.communicationPause = 170;
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
                        this.showImage = base.oracle.myScreen.AddImage("aiimg2_void");
                        this.communicationPause = 290;
                        break;
                    case 2:
                        this.showImage = base.oracle.myScreen.AddImage(new List<string>
                    {
                        "void_glyphs_3",
                        "void_glyphs_5"
                    }, 15);
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
                return base.CurrentlyCommunicating || this.voice != null || (base.action == SSOracleBehavior.Action.MeetWhite_Texting && !this.chatLabel.finishedShowingMessage) || this.showImage != null;
            }
        }

        public ProjectedImage showImage;
        public Vector2 idealShowMediaPos;
        public Vector2 showMediaPos;
        public int consistentShowMediaPosCounter;
        public OracleChatLabel chatLabel;
    }
}
