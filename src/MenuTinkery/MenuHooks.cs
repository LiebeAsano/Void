using HUD;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery;

internal static class MenuHooks
{
	private const string TextIfDead = "The vessel could not withstand the impact of the void liquid.<LINE>Now the soul is doomed to relive his last cycles forever.";
	private const string TextIfDead11 = "Even after leaving the cycle, life continues to go on as usual.<LINE>The death of another monster leads to the birth of a new one.";
	private const string TextIfEnding = "The soul is crying out for new wanderings, but the body still clings to the past.<LINE>You feel that there is only one last wish left.";
    private const string TextIfEnding11 = "The void sea no longer shackles you, and your past is left behind.<LINE>You are ready to make new journeys and explore this vast world.";
    private static readonly ConditionalWeakTable<SlugcatSelectMenu.SlugcatPageContinue, MenuLabel> assLabel = new();
	public static void Hook()
	{
		//when voidcat is dead, those hide useless hud
		On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += HideKarmaAndFoodSplitterAndAddText;
		//On.HUD.FoodMeter.CharSelectUpdate += HideFoodPips;
		On.Menu.SlugcatSelectMenu.SlugcatPageContinue.GrafUpdate += MakeTextScroll;
		On.Menu.MenuScene.BuildScene += StatisticsSceneReplacement;
		//dictates to RW whether void is unlocked or not
		On.SlugcatStats.SlugcatUnlocked += IsVoidUnlocked;

		On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += TextLabelIfNotUnlocked;
		//fix for select menu dying when there is no karma and food meter for the page
        IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.Update += SlugcatPageContinue_Update;

	}

    private static void SlugcatPageContinue_Update(MonoMod.Cil.ILContext il)
    {
		ILCursor c = new(il);
		ILLabel bubblestart = c.DefineLabel();
		ILLabel bubbleend = c.DefineLabel();
		if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(HUD.HUD).GetMethod(nameof(HUD.HUD.Update)))))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Predicate<SlugcatSelectMenu.SlugcatPageContinue>>((SlugcatSelectMenu.SlugcatPageContinue page) => (page.hud.foodMeter == null));
			c.Emit(OpCodes.Brtrue, bubblestart);
			c.Emit(OpCodes.Br, bubbleend);
			c.MarkLabel(bubblestart);
			c.Emit(OpCodes.Ret);
			c.MarkLabel(bubbleend);

		}
		else LogExErr("failed to find HUD.Update. no jump");
    }

    private static bool IsVoidUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugcatStats.Name i, RainWorld rainWorld)
	{
		var re = orig(i, rainWorld);
		if (i == VoidEnums.SlugcatID.Void &&
			!rainWorld.progression.miscProgressionData.beaten_Hunter)
			return _Plugin.DevEnabled || OptionInterface.OptionAccessors.ForceUnlockCampaign;
		return re;
	}
	private static void TextLabelIfNotUnlocked(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
	{
		if (slugcatNumber == VoidEnums.SlugcatID.Void && SlugBase.SlugBaseCharacter.TryGet(slugcatNumber, out var character))
		{
			character.Description = (menu as SlugcatSelectMenu).SlugcatUnlocked(slugcatNumber) ?
				"An enraged and hungry predator escapes from the void sea.<LINE>Balancing between life and death, the beast seeks its new place in this world."
				: "Clear the game as Hunter to unlock.";
		}
		orig(self, menu, owner, pageIndex, slugcatNumber);
	}
	private static void StatisticsSceneReplacement(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
	{
		if (self.owner?.menu is StoryGameStatisticsScreen && RainWorld.lastActiveSaveSlot == VoidEnums.SlugcatID.Void)
		{
			RainWorld rainWorld = self.menu.manager.rainWorld;
			SaveState save = rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, self.menu.manager.menuSetup, false);
			if (save.GetVoidCatDead() && save.deathPersistentSaveData.karmaCap == 10) self.sceneID = VoidEnums.SceneID.StaticDeath11;
			else if (save.GetVoidCatDead()) self.sceneID = VoidEnums.SceneID.StaticDeath;
			else if (save.GetEndingEncountered() && save.deathPersistentSaveData.karmaCap == 10) self.sceneID = VoidEnums.SceneID.StaticEnd11;
			else self.sceneID = VoidEnums.SceneID.StaticEnd;
		}
		orig(self);
	}
	private static void MakeTextScroll(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_GrafUpdate orig, SlugcatSelectMenu.SlugcatPageContinue self, float timeStacker)
	{
		orig(self, timeStacker);
		if (assLabel.TryGetValue(self, out var label))
		{
			float scroll = self.Scroll(timeStacker);
			float alpha = self.UseAlpha(timeStacker);
			label.label.alpha = alpha;
			label.label.x = self.MidXpos + scroll * self.ScrollMagnitude + 0.01f;
		}
	}

	private static void HideFoodPips(On.HUD.FoodMeter.orig_CharSelectUpdate orig, FoodMeter self)
	{
		orig(self);
		if (self.hud.owner is SlugcatSelectMenu.SlugcatPageContinue page
			&& page.slugcatNumber == VoidEnums.SlugcatID.Void
			&& page.menu.manager.rainWorld is RainWorld rainWorld
			&& rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, rainWorld.processManager.menuSetup, false) is SaveState save
			&& (save.GetVoidCatDead() || save.GetEndingEncountered()))
		{
			self.circles.ForEach(ccircle => Array.ForEach(ccircle.circles, c => c.fade = 0));
		}
	}

	private static void HideKarmaAndFoodSplitterAndAddText(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
	{
		orig(self, menu, owner, pageIndex, slugcatNumber);
		if (slugcatNumber == VoidEnums.SlugcatID.Void
			&& menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false) is SaveState save
			&& (save.GetVoidCatDead() || save.GetEndingEncountered()))
		{
			var hud = self.hud;
			//deleting things from manifesting is prone to null reference exceptions, game definitely doesn't think they don't exist
			//so to counter it we just stop stuff we don't need from rendering
			List<FNode> thingsToNotRender = [hud.karmaMeter.darkFade,
				hud.karmaMeter.karmaSprite,
				hud.karmaMeter.glowSprite,
				hud.foodMeter.darkFade,
				hud.foodMeter.lineSprite];
			hud.foodMeter.circles.ForEach(circle =>
			{
				thingsToNotRender.Add(circle.gradient);
				thingsToNotRender.Add(circle.circles[0].sprite);
				thingsToNotRender.Add(circle.circles[1].sprite);
			});
			thingsToNotRender.ForEach(thingNotToRender => hud.fContainers[1].RemoveChild(thingNotToRender));


			int amountOfPageBreaks = TextIfDead.Count((f) => f == '\n');
			float VerticalOffset = 0f;
			if (amountOfPageBreaks > 1)
			{
				VerticalOffset = 30f;
			}
			string text;
			if (save.GetVoidCatDead() && save.deathPersistentSaveData.karmaCap == 10) text = TextIfDead11;
			else if (save.GetVoidCatDead()) text = TextIfDead;
			else if (save.deathPersistentSaveData.karmaCap == 10) text = TextIfEnding11;
			else text = TextIfEnding;
            var textlabel = new MenuLabel(menu, self, text.TranslateStringComplex(), new Vector2(-1000f, self.imagePos.y - 249f - 60f + VerticalOffset / 2f), new Vector2(400f, 60f), true);
			textlabel.label.alignment = FLabelAlignment.Center;
			self.subObjects.Add(textlabel);
			textlabel.label.color = new HSLColor(0.73055553f, 0.08f, 0.3f).rgb;
			textlabel.label.alpha = 1f;
			assLabel.Add(self, textlabel);
		}
	}
}
