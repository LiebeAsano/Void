using Fisobs;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;


namespace VoidTemplate
{
	static class KarmaHooks
	{
		public static void Hook()
		{

			On.Menu.SleepAndDeathScreen.AddBkgIllustration += SleepAndDeathScreen_AddBkgIllustration;

			//On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
			//echoes think void is at max karma and treat him as hunter
			IL.World.SpawnGhost += KarmaReqTinker;

			On.GhostWorldPresence.SpawnGhost += GhostWorldPresence_SpawnGhost;
            //reset savestate
            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;

			//IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctorIL;
		}

        private static void SleepAndDeathScreen_AddBkgIllustration(On.Menu.SleepAndDeathScreen.orig_AddBkgIllustration orig, SleepAndDeathScreen self)
        {
			if (self.saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
			{
				MenuScene.SceneID sceneID = null;
				if (self.IsSleepScreen && self.saveState.GetVoidMarkV3())
				{
					sceneID = VoidEnums.SceneID.SleepSceneMark;
				}
				else if (self.IsSleepScreen && self.saveState.deathPersistentSaveData.karmaCap != 10)
				{
					sceneID = VoidEnums.SceneID.SleepScene;
				}
				else if (self.IsSleepScreen && self.saveState.deathPersistentSaveData.karmaCap == 10)
				{
					sceneID = VoidEnums.SceneID.SleepScene11;
				}
				else if ((self.IsDeathScreen || self.IsStarveScreen) && self.saveState.deathPersistentSaveData.karmaCap != 10)
				{
					sceneID = VoidEnums.SceneID.DeathScene;
				}
				else if ((self.IsDeathScreen || self.IsStarveScreen) && self.saveState.deathPersistentSaveData.karmaCap == 10)
				{
					sceneID = VoidEnums.SceneID.DeathScene11;
				}
				self.scene = new InteractiveMenuScene(self, self.pages[0], sceneID);
				self.pages[0].subObjects.Add(self.scene);
				return;
			}
			orig(self);
        } 

        private static void KarmaReqTinker(ILContext il)
		{
			ILCursor c = new(il);
			// bool flag = this.game.setupValues.ghosts > 0
			// || GhostWorldPresence.SpawnGhost(ghostID,
			// (this.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma <replace with karmacap, method thinks void is always at max karma>,
			// (this.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap,
			// num,
			// this.game.StoryCharacter == SlugcatStats.Name.Red <OR VOID> );
			if (
				c.TryGotoNext(x => x.MatchLdsfld("SlugcatStats/Name", "Red"))
				&&	c.TryGotoPrev(MoveType.After,	
					x => x.MatchLdfld(typeof(DeathPersistentSaveData).GetField("karma"))
					)
			)
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<int, World, int>>((originalResult, world) =>
				{
					if (world.game.StoryCharacter == VoidEnums.SlugcatID.Void) return (world.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
					return originalResult;
				});
			}
			else LogExErr("Failed to replace karma with karmacap");

			if (
				c.TryGotoNext(MoveType.After, 
					x => x.MatchCall("ExtEnum`1<SlugcatStats/Name>", "op_Equality"))
			)
			{
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<bool, World, bool>>((orig, world) =>
				{
					return orig || world.game.StoryCharacter == VoidEnums.SlugcatID.Void;
				});
			}
			else LogExErr("Failed to find comparison to red in echo spawning ");
		}

		private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
		{
			orig(self, saveStateNumber);
			if (saveStateNumber == VoidEnums.SlugcatID.Void)
			{
				RainWorld rainWorld = self.rainWorld;
				SaveState save = rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, self.rainWorld.processManager.menuSetup, false);
				save.SetVoidCatDead(false);
				save.SetEndingEncountered(false);
			}
		}

        private static bool GhostWorldPresence_SpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
            if (karmaCap == 10)
                return false;
            var re = orig(ghostID, karma, karmaCap, ghostPreviouslyEncountered, playingAsRed);
            return re;
        }

        private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
		{
			orig(self, package);
			MenuScene.SceneID sceneID = null;
			if (self.saveState?.saveStateNumber == VoidEnums.SlugcatID.Void)
			{
				if (self.IsSleepScreen && self.saveState.deathPersistentSaveData.karmaCap != 10)
				{
					sceneID = VoidEnums.SceneID.SleepScene;
				}
				else if (self.IsSleepScreen && self.saveState.deathPersistentSaveData.karmaCap == 10)
				{
					sceneID = VoidEnums.SceneID.SleepScene11;
				}
				else if ((self.IsDeathScreen || self.IsStarveScreen) && self.saveState.deathPersistentSaveData.karmaCap != 10)
				{
					sceneID = VoidEnums.SceneID.DeathScene;
				}
				else if ((self.IsDeathScreen || self.IsStarveScreen) && self.saveState.deathPersistentSaveData.karmaCap == 10)
				{
					sceneID = VoidEnums.SceneID.DeathScene11;
				}

				if (sceneID != null && sceneID.Index != -1)
				{
					self.scene.RemoveSprites();
					self.pages[0].subObjects.RemoveAll(i => i is InteractiveMenuScene);
					self.scene = new InteractiveMenuScene(self, self.pages[0], sceneID);
					self.pages[0].subObjects.Add(self.scene);
					for (int i = self.scene.depthIllustrations.Count - 1; i >= 0; i--)
						self.scene.depthIllustrations[i].sprite.MoveToBack();
				}
			}
		}

		

        /*private static void SlugcatPageContinue_ctorIL(ILContext il)
		{
			try
			{
				ILCursor c = new ILCursor(il);
				while (c.TryGotoNext(MoveType.After, i => i.MatchLdarg(4),
						   i => i.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
						   i => i.MatchLdfld<IntVector2>("x")))
				{
					c.Emit(OpCodes.Ldarg_S, (byte)4);
					c.Emit(OpCodes.Ldarg_0);

					c.EmitDelegate<Func<int, SlugcatStats.Name, SlugcatSelectMenu.SlugcatPageContinue, int>>((x, name, self) =>
					{
						if (name == _Plugin.Void && self.saveGameData.karma == 10)
							return 9;
						return x;
					});
				}
				ILCursor c2 = new ILCursor(il);
				while (c2.TryGotoNext(MoveType.After, i => i.MatchLdarg(4),
						   i => i.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
						   i => i.MatchLdfld<IntVector2>("y")))
				{
					c2.Emit(OpCodes.Ldarg_S, (byte)4);
					c2.Emit(OpCodes.Ldarg_0);

					c2.EmitDelegate<Func<int, SlugcatStats.Name, SlugcatSelectMenu.SlugcatPageContinue, int>>((y, name, self) =>
					{
						if (name == _Plugin.Void && self.saveGameData.karma == 10)
							return 6;
						return y;
					});
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}*/
    }

}
