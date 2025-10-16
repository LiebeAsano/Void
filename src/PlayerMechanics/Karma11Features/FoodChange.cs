using DevInterface;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using VoidTemplate.MenuTinkery;
using HUD;
using static VoidTemplate.SaveManager;
using static VoidTemplate.Useful.Utils;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

public static class FoodChange
{
    public static bool VoidFullAnd11Karma(this SaveState save, int currentFood, int foodToHibernate, int maxFood) => save.saveStateNumber == VoidEnums.SlugcatID.Void && save.deathPersistentSaveData.karmaCap == 10 && currentFood + foodToHibernate == maxFood;
    public static bool CanAddExtraFood(this SaveState save, int maxFood) => 9 + save.GetVoidExtraFood() > maxFood;
    public static bool CanAddFoodToHibernate(this SaveState save, int foodToHibernate) => 6 + save.GetVoidFoodToHibernate() > foodToHibernate;

    public static void Hook()
    {
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        IL.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
        IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
        On.HUD.FoodMeter.SleepUpdate += FoodMeter_SleepUpdate;
        On.HUD.FoodMeter.ctor += FoodMeter_ctor;
        On.SaveState.SessionEnded += SaveState_SessionEnded;
        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
    }

    private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        orig(self, package);
        if (self.IsSleepScreen && self.saveState.VoidFullAnd11Karma(self.food, self.hud.foodMeter.survivalLimit, self.hud.foodMeter.maxFood) && (self.saveState.CanAddExtraFood(self.hud.foodMeter.maxFood) || (self.saveState.GetVoidExtraFood() > 0 && self.saveState.CanAddFoodToHibernate(self.hud.foodMeter.survivalLimit))))
        {
            self.saveState.food = 0;
            self.saveState.progression.SaveToDisk(true, false, false);
        }
    }

    private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        if (survived && self.VoidFullAnd11Karma((game.Players[0].realizedCreature as Player).FoodInStomach, 0, game.GetStorySession.characterStats.maxFood))
        {
            int extraFood = self.GetVoidExtraFood();
            int foodToHibernate = self.GetVoidFoodToHibernate();
            if (extraFood < 3)
            {
                self.SetVoidExtraFood(extraFood + 1);
            }
            else if (foodToHibernate < 6) 
            {
                self.SetKarmaToken(Mathf.Max(0, self.GetKarmaToken() - 1));
                self.SetVoidFoodToHibernate(foodToHibernate + 1); 
            }
            else self.SetKarmaToken(0);
        }
        orig(self, game, survived, newMalnourished);
    }

    private static void FoodMeter_ctor(On.HUD.FoodMeter.orig_ctor orig, FoodMeter self, HUD.HUD hud, int maxFood, int survivalLimit, Player associatedPup, int pupNumber)
    {
        if (hud.owner is Player player)
            self.GetMeterExt().showNumFoodTohibernate = 2 * player.abstractCreature.world.game.GetStorySession.saveState.GetVoidFoodToHibernate();
        orig(self, hud, maxFood, survivalLimit, associatedPup, pupNumber);
        if (hud.owner is SleepAndDeathScreen screen && !screen.goalMalnourished && screen.saveState.VoidFullAnd11Karma(screen.food, survivalLimit, maxFood) && (screen.saveState.CanAddExtraFood(maxFood) || (screen.saveState.GetVoidExtraFood() > 0 && screen.saveState.CanAddFoodToHibernate(survivalLimit))))
            self.eatCircles = maxFood;
    }

    private static void FoodMeter_SleepUpdate(On.HUD.FoodMeter.orig_SleepUpdate orig, FoodMeter self)
    {
        orig(self);
        var screen = self.hud.owner as SleepAndDeathScreen;
        if (self.sleepScreenPhase == 0 && self.eatCircles == 0 && screen.saveState.VoidFullAnd11Karma(screen.food, self.survivalLimit, self.maxFood))
        {
            if (screen.saveState.CanAddExtraFood(self.maxFood))
            {
                self.eatCircleDelay = 40;
                self.sleepScreenPhase = 4;
            }
            else if (screen.saveState.CanAddFoodToHibernate(self.survivalLimit))
            {
                self.MoveSurvivalLimit(self.survivalLimit + 1, true);
                self.eatCircleDelay = 80;
                self.sleepScreenPhase = 2;
            }
        }

        if (self.sleepScreenPhase == 4 && self.eatCircleDelay <= 0)
        {
            FoodMeter.MeterCircle circle = new(self, self.circles.Count);
            self.circles.Add(circle);
            circle.AddGradient();
            circle.AddCircles();
            self.eatCircleDelay = 80;
            self.sleepScreenPhase = 3;
        }
    }

    private static void SlugcatPageContinue_ctor(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After, x => x.MatchNewobj<FoodMeter>())
            && c.TryGotoPrev(MoveType.After,
            x => x.MatchLdfld<RWCustom.IntVector2>("x")))
        {
            c.Emit(OpCodes.Ldarg, 4);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg, 1);
            c.EmitDelegate((int origRess, SlugcatStats.Name name, SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue, Menu.Menu menu) =>
            {
                if (name == VoidEnums.SlugcatID.Void && slugcatPageContinue.saveGameData.karmaCap == 10)
                {
                    SaveState save = menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false);
                    return origRess + save.GetVoidExtraFood();
                }
                return origRess;
            });

            //this.hud.AddPart(new FoodMeter(this.hud, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x, SlugcatStats.SlugcatFoodMeter(slugcatNumber).y < 6 if void and at karma 11>, null, 0));
            if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<RWCustom.IntVector2>("y")))
            {
                c.Emit(OpCodes.Ldarg, 4);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg, 1);
                c.EmitDelegate((int origRess, SlugcatStats.Name name, SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue, Menu.Menu menu) =>
                {

                    if (name == VoidEnums.SlugcatID.Void
                    && (slugcatPageContinue.saveGameData.karmaCap == 10
                    || menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false) is SaveState save && save.GetVoidMarkV3()))
                    {
                        save = menu.manager.rainWorld.progression.currentSaveState;
                        return 6 + (save.GetVoidExtraFood() == 3 ? save.GetVoidFoodToHibernate() : 0);
                    }
                    return origRess;
                });
            }
            else LogExErr("couldn't find creation of food meter instruction. expect main menu food meter to always be at 10 required pips for survival");
        }
    }

    //fix for whether slugcat will be malnourished in next cycle
    private static void ShelterDoor_DoorClosed(ILContext il)
    {
        ILCursor c = new(il);
        //used to bypass Jolly's static requirements for food consumption for character
        //int y = SlugcatStats.SlugcatFoodMeter(this.room.game.StoryCharacter).y < if void world and karma 10 make it 6>;
        if (c.TryGotoNext(MoveType.After, x => x.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
            x => x.MatchLdfld(out _)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, ShelterDoor, int>>((int orig, ShelterDoor self) =>
            {
                var game = self.room?.game;
                if (game == null) return orig;

                if (game.Players[0]?.realizedCreature is not Player player) return orig;

                bool isVoidSlugcat = game.StoryCharacter == VoidEnums.SlugcatID.Void;
                bool hasMaxKarma = player.KarmaCap == 10;
                bool hasVoidMark = game.GetStorySession?.saveState?.GetVoidMarkV3() ?? false;

                if (isVoidSlugcat && (hasMaxKarma || hasVoidMark || ExternalSaveData.VoidKarma11))
                {
                    return 6;
                }
                return orig;
            });
        }
        else LogExErr("failed to locate slugcatfoodmeter call in shelterdoor closing. expect mismatch between food requirements and success of hybernation");
    }
    /// <summary>
    /// replaces main menu food requirements
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="saveStateNumber"></param>
    /// <param name="game"></param>
    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);
        if (saveStateNumber == VoidEnums.SlugcatID.Void && (self.saveState.deathPersistentSaveData.karma == 10 || self.saveState.GetVoidMarkV3() || Karma11Update.VoidKarma11))
        {
            int maxFood = 9 + (self.saveState.deathPersistentSaveData.karmaCap == 10 ? self.saveState.GetVoidExtraFood() : 0);
            self.characterStats.foodToHibernate = self.saveState.malnourished ? maxFood : (6 + (self.saveState.GetVoidExtraFood() == 3 ? self.saveState.GetVoidFoodToHibernate() : 0));
            self.characterStats.maxFood = maxFood;
        }
    }
}
