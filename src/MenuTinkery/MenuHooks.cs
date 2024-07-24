using HUD;
using System;
using System.Runtime.CompilerServices;
using Menu;
using UnityEngine;
using System.Linq;
using SlugBase.SaveData;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text;

namespace VoidTemplate.MenuTinkery;

internal static class MenuHooks
{
    private const string TextIfDead = "The vessel could not withstand the impact of the void liquid.<LINE>Now the soul is doomed to relive his last cycles forever.";
    private const string TextIfEnding = "The soul is crying out for new wanderings, but the body still clings to the past.<LINE>You have the feeling that you must fulfil the last wish.";
    static ConditionalWeakTable<SlugcatSelectMenu.SlugcatPageContinue, MenuLabel> assLabel = new();
    public static void Hook()
    {
        //when voidcat is dead, those hide useless hud
        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += HideKarmaAndFoodSplitterAndAddText;
        On.HUD.FoodMeter.CharSelectUpdate += HideFoodPips;
        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.GrafUpdate += MakeTextScroll;
        On.Menu.MenuScene.BuildScene += StatisticsSceneReplacement;
        On.SlugcatStats.SlugcatUnlocked += IsVoidUnlocked;
        On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += TextLabelIfNotUnlocked;

    }
    private static bool IsVoidUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugcatStats.Name i, RainWorld rainWorld)
    {
        var re = orig(i, rainWorld);
        if (i == StaticStuff.TheVoid &&
            !rainWorld.progression.miscProgressionData.beaten_Hunter)
            return _Plugin.DevEnabled;
        return re;
    }
    private static void TextLabelIfNotUnlocked(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        if(slugcatNumber == StaticStuff.TheVoid && SlugBase.SlugBaseCharacter.TryGet(slugcatNumber, out var character))
        {
            character.Description = (menu as SlugcatSelectMenu).SlugcatUnlocked(slugcatNumber) ?
                "An enraged and hungry predator escapes from the void sea.<LINE>Balancing between life and death, the beast seeks its new place in this world."
                : "Clear the game as Hunter to unlock.";
        }
        orig(self, menu, owner, pageIndex, slugcatNumber);
    }
    private static void StatisticsSceneReplacement(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if (self.owner.menu is StoryGameStatisticsScreen statscreen && self.sceneID == StaticStuff.SleepSceneID)
        {
            RainWorld rainWorld = self.menu.manager.rainWorld;
            SaveState save = rainWorld.progression.GetOrInitiateSaveState(StaticStuff.TheVoid, null, self.menu.manager.menuSetup, false);
            if (save.GetEndingEncountered()) self.sceneID = StaticStuff.StaticEnd;
            else self.sceneID = StaticStuff.StaticDeath;
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
            && page.slugcatNumber == StaticStuff.TheVoid
            && page.menu.manager.rainWorld is RainWorld rainWorld
            && rainWorld.progression.GetOrInitiateSaveState(StaticStuff.TheVoid, null, rainWorld.processManager.menuSetup, false) is SaveState save
            && (save.GetVoidCatDead() || save.GetEndingEncountered()))
        {
            self.circles.ForEach(ccircle => Array.ForEach(ccircle.circles, c => c.fade = 0));
        }
    }

    private static void HideKarmaAndFoodSplitterAndAddText(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        orig(self, menu, owner, pageIndex, slugcatNumber);
        if (slugcatNumber == StaticStuff.TheVoid
            && menu.manager.rainWorld.progression.GetOrInitiateSaveState(StaticStuff.TheVoid, null, menu.manager.menuSetup, false) is SaveState save
            && (save.GetVoidCatDead() || save.GetEndingEncountered()))
        {
            var hud = self.hud;
            foreach (var part in hud.parts)
            {
                if (part is KarmaMeter k)
                {
                    k.ClearSprites();
                    k.glowSprite.isVisible = false;
                }
                if (part is FoodMeter f)
                {
                    f.ClearSprites();
                    f.initPlopCircle = -1;
                    f.initPlopDelay = 0;
                    f.lastFade = 0;
                    f.fade = 0;
                    f.circles.ForEach(compfoodcircle =>
                    {
                        compfoodcircle.plopped = false;
                        Array.ForEach(compfoodcircle.circles, circle => circle.visible = false);
                    });
                    f.lineSprite.isVisible = false;
                }
            }
            int amountOfPageBreaks = TextIfDead.Count((f) => f == '\n');
            float VerticalOffset = 0f;
            if (amountOfPageBreaks > 1)
            {
                VerticalOffset = 30f;
            }
            string text = save.GetEndingEncountered() ? TextIfEnding : TextIfDead;
            var textlabel = new MenuLabel(menu, self, text.TranslateStringComplex(), new Vector2(-1000f, self.imagePos.y - 249f - 60f + VerticalOffset / 2f), new Vector2(400f, 60f), true);
            textlabel.label.alignment = FLabelAlignment.Center;
            self.subObjects.Add(textlabel);
            textlabel.label.color = new HSLColor(0.73055553f, 0.08f, 0.3f).rgb;
            textlabel.label.alpha = 1f;
            assLabel.Add(self, textlabel);
        }
    }
}
