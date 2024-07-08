using HUD;
using System;
using System.Runtime.CompilerServices;
using TheVoid;
using Menu;
using UnityEngine;
using System.Linq;
using SlugBase.SaveData;

namespace VoidTemplate;

internal static class MenuHooks
{
    private const string TextIfDead = "The vessel could not withstand the impact of the void liquid.<LINE>Now the soul is doomed to relive his last cycles forever.";
    private const string TextIfEnding = "there is another text here surprisingly";
    static ConditionalWeakTable<SlugcatSelectMenu.SlugcatPageContinue, MenuLabel> assLabel = new();
    public static void Hook()
    {
        //when voidcat is dead, those hide useless hud
        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += HideKarmaAndFoodSplitterAndAddText;
        On.HUD.FoodMeter.CharSelectUpdate += HideFoodPips;
        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.GrafUpdate += MakeTextScroll;
        On.Menu.MenuScene.BuildScene += SceneReplacement;
        On.SlugcatStats.SlugcatUnlocked += IsVoidUnlocked;
        On.Menu.MenuScene.BuildScene += ImageIfNotUnlocked;
        On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += TextLabelIfNotUnlocked;
    }
    private static void ImageIfNotUnlocked(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if (self.sceneID == new MenuScene.SceneID("Slugcat_Void") &&
            !SlugcatStats.SlugcatUnlocked(StaticStuff.TheVoid, RWCustom.Custom.rainWorld))
            self.sceneID = new MenuScene.SceneID("Slugcat_Void_Dark");
        orig(self);
    }
    private static bool IsVoidUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugcatStats.Name i, RainWorld rainWorld)
    {
        var re = orig(i, rainWorld);
        if (i == StaticStuff.TheVoid &&
            !rainWorld.progression.miscProgressionData.beaten_Hunter)
            return Plugin.DevEnabled;
        return re;
    }
    private static void TextLabelIfNotUnlocked(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor orig, SlugcatSelectMenu.SlugcatPageNewGame self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        orig(self, menu, owner, pageIndex, slugcatNumber);
        if (slugcatNumber == StaticStuff.TheVoid && !(menu as SlugcatSelectMenu).SlugcatUnlocked(slugcatNumber))
            self.infoLabel.text = self.menu.Translate("Clear the game as Hunter to unlock.");
    }
    private static void SceneReplacement(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if (self.owner.menu is StoryGameStatisticsScreen statscreen && self.sceneID == StaticStuff.SleepSceneID)
        {
            if (self.owner.menu.manager.rainWorld.progression.GetEndingEncountered()) self.sceneID = StaticStuff.SleepKarma11ID;
            else self.sceneID = StaticStuff.DeathSceneID;
        }
        if( self.owner is SlugcatSelectMenu.SlugcatPageContinue page && page.slugcatNumber == StaticStuff.TheVoid )
        {
            //Plugin.logger.LogInfo($"trying to load slugcat page. Progression is {page.menu.manager.rainWorld.progression.GetHashCode()}. Encountered? {page.menu.manager.rainWorld.progression.GetEndingEncountered()}\n" +
              //  $"does it exist even? {page.menu.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData().TryGet(SaveManager.endingDone, out bool done)} {done}");
            if (page.menu is SlugcatSelectMenu menu 
                && menu.saveGameData.TryGetValue(page.slugcatNumber, out var saveGameData)
                && page.menu.manager.rainWorld.progression.GetEndingEncountered()) self.sceneID = StaticStuff.SleepKarma11ID;
        }
            orig(self);
    }
    private static void MakeTextScroll(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_GrafUpdate orig, SlugcatSelectMenu.SlugcatPageContinue self, float timeStacker)
    {
        orig(self, timeStacker);
        if(assLabel.TryGetValue(self, out var label))
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
        if (self.hud.owner is Menu.SlugcatSelectMenu.SlugcatPageContinue page
            && page.slugcatNumber == StaticStuff.TheVoid
            && page.menu.manager.rainWorld.progression is PlayerProgression p
            && (p.GetVoidCatDead() || p.GetEndingEncountered()))
        {
            self.circles.ForEach(ccircle => Array.ForEach(ccircle.circles, c => c.fade = 0));
        }
    }

    private static void HideKarmaAndFoodSplitterAndAddText(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        orig(self, menu, owner, pageIndex, slugcatNumber);
        if (slugcatNumber == StaticStuff.TheVoid && menu.manager.rainWorld.progression is PlayerProgression prog && (prog.GetVoidCatDead() || prog.GetEndingEncountered()))
        {
            var hud = self.hud;
            foreach (var part in hud.parts)
            {
                if (part is KarmaMeter k)
                {
                    k.ClearSprites();
                    k.glowSprite.isVisible = false;
                }
                if(part is FoodMeter f)
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
            int amountOfPageBreaks = Enumerable.Count<char>(TextIfDead, (char f) => f == '\n');
            float VerticalOffset = 0f;
            if (amountOfPageBreaks > 1)
            {
                VerticalOffset = 30f;
            }
            string text = prog.GetEndingEncountered() ? TextIfEnding : TextIfDead;
            var textlabel = new MenuLabel(menu, self, text.TranslateStringComplex(), new Vector2(-1000f, self.imagePos.y - 249f - 60f + VerticalOffset / 2f), new Vector2(400f, 60f), true);
            textlabel.label.alignment = FLabelAlignment.Center;
            self.subObjects.Add(textlabel);
            textlabel.label.color = new HSLColor(0.73055553f, 0.08f, 0.3f).rgb;
            textlabel.label.alpha = 1f;
            assLabel.Add(self, textlabel);
        }
    }
}
