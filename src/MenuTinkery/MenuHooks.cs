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
using static Menu.CheckBox;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.MenuTinkery;

public static class MenuHooks
{
	private const string TextIfDead = "The vessel could not withstand the impact of the void liquid.<LINE>Now the soul is doomed to relive his last cycles forever.";
	private const string TextIfDead11 = "Even after leaving the cycle, life continues to go on as usual.<LINE>The death of another monster leads to the birth of a new one. (To be continued?)";
	private const string TextIfEnding = "The soul is crying out for new wanderings, but the body still clings to the past.<LINE>You feel that there is only one last wish left. (To be continued...)";
    private const string TextIfSlugEnding = "The hunger is sated, but the void inside still cries out. After destroying<LINE>the colony, you continue your journey in search of survivors.";
    private static readonly ConditionalWeakTable<SlugcatSelectMenu.SlugcatPageContinue, MenuLabel> assLabel = new();

	private static readonly ConditionalWeakTable<SlugcatSelectMenu, StrongBox<bool>> resetForLegalRun = new();

	public static StrongBox<bool> GetResetForLegalRun(this SlugcatSelectMenu menu) => resetForLegalRun.GetOrCreateValue(menu);

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
        On.Menu.MainMenu.ctor += MainMenu_ctor;
        On.Menu.KarmaLadderScreen.AddContinueButton += KarmaLadderScreen_AddContinueButton;
        On.Menu.SlugcatSelectMenu.ctor += SlugcatSelectMenu_ctor;
        On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
	}

    private static void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    {
		orig(self, storyGameCharacter);
        if (self.GetResetForLegalRun().Value)
		{

		}
    }

    private static void SlugcatSelectMenu_ctor(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
    {
		orig(self, manager);
		self.pages[0].subObjects.Add(new LegalSpeedrunResetBox(self, self.pages[0],
            new(self.restartCheckbox.pos.x + 175 + SlugcatSelectMenu.GetRestartTextOffset(self.CurrLang), self.restartCheckbox.pos.y),
            SlugcatSelectMenu.GetRestartTextWidth(self.CurrLang) + 55, "Reset for legal speedrun"));
    }

    private static void KarmaLadderScreen_AddContinueButton(On.Menu.KarmaLadderScreen.orig_AddContinueButton orig, KarmaLadderScreen self, bool black)
    {
		orig(self, black);
		if (self is StoryGameStatisticsScreen menu)
		{
			Vector2 pos = self.continueButton.pos;
			pos.x -= 20 + self.continueButton.size.x;
			self.pages[0].subObjects.Add(new LoadDataButton(menu, self.pages[0], "WRITE DATA", () =>
			{
                self.manager.ActualShowDialog(new DialogConfirm("Вы уверены, что хотите записать данные?", self.manager, () =>
                {

                }, null));
            }, pos, self.continueButton.size));
		}
    }

    private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
		orig(self, manager, showRegionSpecificBkg);
		self.pages[0].subObjects.Add(new LastWishDiscordButton(self, self.pages[0], new(1306, 10)));
        float buttonWidth = MainMenu.GetButtonWidth(self.CurrLang);
        Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
        Vector2 size = new Vector2(buttonWidth, 30f);
		self.AddMainMenuButton(new(self, self.pages[0], "LEADER TABLE", "LEADERTABLE", pos, size), () =>
		{
			self.PlaySound(SoundID.MENU_Switch_Page_In);
			self.manager.RequestMainProcessSwitch(VoidEnums.ProcessID.LeaderTableMenu);
		}, self.mainMenuButtons.Count - 1);
    }

    private static void SlugcatPageContinue_Update(ILContext il)
    {
		ILCursor c = new(il);
		ILLabel bubblestart = c.DefineLabel();
		ILLabel bubbleend = c.DefineLabel();
		if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(HUD.HUD).GetMethod(nameof(HUD.HUD.Update)))))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Predicate<SlugcatSelectMenu.SlugcatPageContinue>>(page => (page.hud.foodMeter == null));
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
				"An enraged and hungry predator escapes from the Void Sea.<LINE>Balancing between life and death, the beast seeks its new place in this world."
				: "Clear the game as Hunter to unlock.";
		}
		orig(self, menu, owner, pageIndex, slugcatNumber);
	}
    private static void StatisticsSceneReplacement(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if (self.owner?.menu is StoryGameStatisticsScreen)
        {
            RainWorld rainWorld = self.menu.manager.rainWorld;

            if (rainWorld.progression.PlayingAsSlugcat == VoidEnums.SlugcatID.Void)
            {
                SaveState save = rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, self.menu.manager.menuSetup, false);

                bool dead = save.GetVoidCatDead();
                bool karma10 = save.deathPersistentSaveData.karmaCap == 10;
                bool treeEnding = save.GetVoidEndingTree();
                bool ending = save.GetEndingEncountered();

                if (dead && karma10)
                    self.sceneID = VoidEnums.SceneID.StaticDeath11;
                else if (dead)
                    self.sceneID = VoidEnums.SceneID.StaticDeath;
                else if (treeEnding)
                    self.sceneID = VoidEnums.SceneID.StaticSlugcat;
                else if (ending && karma10)
                    self.sceneID = VoidEnums.SceneID.StaticEnd11;
                else if (ending)
                    self.sceneID = VoidEnums.SceneID.StaticEnd;
                else
                    self.sceneID = VoidEnums.SceneID.StaticEnd;
            }
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
			&& (save.GetVoidCatDead() || save.GetEndingEncountered() || save.GetVoidEndingTree()))
		{
			self.circles.ForEach(ccircle => Array.ForEach(ccircle.circles, c => c.fade = 0));
		}
	}

	private static void HideKarmaAndFoodSplitterAndAddText(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
	{
		orig(self, menu, owner, pageIndex, slugcatNumber);
		if (slugcatNumber == VoidEnums.SlugcatID.Void
			&& menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false) is SaveState save
			&& (save.GetVoidCatDead() || save.GetEndingEncountered() || save.GetVoidEndingTree()))
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
			else if (save.GetVoidEndingTree()) text = TextIfSlugEnding;
			else text = TextIfEnding;
            var textlabel = new MenuLabel(menu, self, text.TranslateStringComplex(), new Vector2(-1000f, self.imagePos.y - 249f - 60f + VerticalOffset / 2f), new Vector2(400f, 60f), true);
			textlabel.label.alignment = FLabelAlignment.Center;
			self.subObjects.Add(textlabel);
			textlabel.label.color = new HSLColor(0.73055553f, 0.08f, 0.3f).rgb;
			textlabel.label.alpha = 1f;
			assLabel.Add(self, textlabel);
		}
	}

    public class LoadDataButton : SimpleButton
    {
		public Action onClick;

        public LoadDataButton(StoryGameStatisticsScreen menu, MenuObject owner, string displayText, Action clickAction, Vector2 pos, Vector2 size) : base(menu, owner, displayText, null, pos, size)
        {
			onClick = clickAction;
        }

        public override void Clicked()
        {
			if (buttonBehav.greyedOut)
			{
				return;
			}
			onClick();
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        }

        public override void Update()
        {
            base.Update();
			buttonBehav.greyedOut = (menu as StoryGameStatisticsScreen).ButtonsGreyedOut;
        }
    }

    public class LegalSpeedrunResetBox : CheckBox
    {
		public SlugcatSelectMenu SlugMenu => menu as SlugcatSelectMenu;

        public LegalSpeedrunResetBox(SlugcatSelectMenu menu, MenuObject owner, Vector2 pos, float textWidth, string displayText, bool textOnRight = false) : base(menu, owner, default(SelfOwnCheckBox), pos, textWidth, displayText, null, textOnRight)
        {

        }

		public override void Update()
		{
			base.Update();
			pos.y = SlugMenu.restartCheckbox.pos.y;
            selectable = SlugMenu.restartAvailable;
            buttonBehav.greyedOut = !SlugMenu.restartChecked;
			if (!SlugMenu.restartChecked)
			{
				Checked = false;
			}
        }

        public override void Clicked()
        {
            base.Clicked();
			SlugMenu.GetResetForLegalRun().Value = Checked;
        }
    }

    public struct SelfOwnCheckBox : IOwnCheckBox
    {
        private bool _checked;

        public readonly bool GetChecked(CheckBox box)
        {
            return _checked;
        }


        public void SetChecked(CheckBox box, bool c)
        {
            _checked = c;
        }
    }
}
