using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.IO;
using System.Linq;
using VoidTemplate.Useful;
using static VoidTemplate.VoidEnums.ConversationID;

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
	private static readonly Conversation.ID[] modSpecificConversations = [Moon_VoidConversation];
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
	public static SSOracleBehavior.SubBehavior.SubBehavID VoidTalk = new("VoidTalk", true);




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

		public static int[] cycleLingers = [0, 1, 0, 1, 1, 0, 2, 2, 2, 2, 0];
		public static int[] MooncycleLingers = [0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1];

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
				self.events.Add(new Conversation.TextEvent(self, 0, ". . .", 0));
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
		if (nextAction == MeetVoid_Init)
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
						self.NewAction(SSOracleBehavior.Action.General_GiveMark);
						self.afterGiveMarkAction = MeetVoid_Init;
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

}
