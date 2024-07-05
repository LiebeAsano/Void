using HUD;
using System;
using System.Runtime.CompilerServices;
using TheVoid;
using Menu;
using UnityEngine;
using System.Linq;
using VoidTemplate.Useful;

namespace VoidTemplate;

internal static class MenuHooks
{
    private const string MenuLabel = "The vessel could not withstand the impact of the void liquid.\nNow you are doomed to relive your last cycles forever.";
    static ConditionalWeakTable<SlugcatSelectMenu.SlugcatPageContinue, MenuLabel> assLabel = new();
    public static void Hook()
    {
        //when voidcat is dead, those hide useless hud
        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += HideKarmaAndFoodSplitter;
        On.HUD.FoodMeter.CharSelectUpdate += HideFoodPips;
        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.GrafUpdate += MakeTextScroll;
        On.Menu.MenuScene.BuildScene += FinalDeathSceneReplacement;
    }

    private static void FinalDeathSceneReplacement(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
    {
        if (self.owner.menu is StoryGameStatisticsScreen && self.sceneID == StaticStuff.SleepSceneID)
        {
            self.sceneID = StaticStuff.DeathSceneID;
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
        if(self.hud.owner is Menu.SlugcatSelectMenu.SlugcatPageContinue page && page.slugcatNumber == Plugin.TheVoid && page.menu.manager.rainWorld.progression.GetVoidCatDead())
        {
            self.circles.ForEach(ccircle => Array.ForEach(ccircle.circles, c => c.fade = 0));
        }
    }

    private static void HideKarmaAndFoodSplitter(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        orig(self, menu, owner, pageIndex, slugcatNumber);
        if (slugcatNumber == Plugin.TheVoid && menu.manager.rainWorld.progression.GetVoidCatDead())
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
            int amountOfPageBreaks = Enumerable.Count<char>(MenuLabel, (char f) => f == '\n');
            float VerticalOffset = 0f;
            if (amountOfPageBreaks > 1)
            {
                VerticalOffset = 30f;
            }
            var textlabel = new MenuLabel(menu, self, MenuLabel, new Vector2(-1000f, self.imagePos.y - 249f - 60f + VerticalOffset / 2f), new Vector2(400f, 60f), true);
            textlabel.label.alignment = FLabelAlignment.Center;
            self.subObjects.Add(textlabel);
            textlabel.label.color = new HSLColor(0.73055553f, 0.08f, 0.3f).rgb;
            textlabel.label.alpha = 1f;
            assLabel.Add(self, textlabel);
        }
    }
}
