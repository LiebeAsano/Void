using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using HUD;
using static VoidTemplate.SaveManager;
using static VoidTemplate.Useful.Utils;
using UnityEngine;
using VoidTemplate.RainCycleChanges;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

public static class FoodChange
{
    private static bool hooked;

    public static bool VoidFullAnd11Karma(this SaveState save, int currentFood, int foodToHibernate, int maxFood)
    {
        return save != null && save.saveStateNumber == VoidEnums.SlugcatID.Void
            && save.deathPersistentSaveData.karmaCap == 10
            && currentFood + foodToHibernate == maxFood;
    }

    public static bool CanAddExtraFood(this SaveState save, int maxFood) =>
        9 + save.GetVoidExtraFood() > maxFood;

    public static bool CanAddFoodToHibernate(this SaveState save, int foodToHibernate) =>
        6 + save.GetVoidFoodToHibernate() > foodToHibernate;

    public static void Hook()
    {
        if (hooked) return;
        hooked = true;

        On.StoryGameSession.ctor += StoryGameSession_ctor;
        IL.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
        IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
        On.HUD.FoodMeter.SleepUpdate += FoodMeter_SleepUpdate;
        On.HUD.FoodMeter.ctor += FoodMeter_ctor;
        On.SaveState.SessionEnded += SaveState_SessionEnded;
        On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
    }

    public static void ApplyFullFoodProgression(SaveState save)
    {
        if (save == null || save.saveStateNumber != VoidEnums.SlugcatID.Void) return;

        int extraFood = save.GetVoidExtraFood();
        int foodToHibernate = save.GetVoidFoodToHibernate();

        if (extraFood < 3) save.SetVoidExtraFood(extraFood + 1);
        else if (foodToHibernate < 5)
        {
            save.SetKarmaToken(Mathf.Max(0, save.GetKarmaToken() - 1));
            save.SetVoidFoodToHibernate(foodToHibernate + 1);
        }
        else if (foodToHibernate == 5)
        {
            save.SetKarmaToken(0);
            save.SetVoidFoodToHibernate(foodToHibernate + 1);
        }
        else save.SetKarmaToken(0);
    }

    public static void RefreshLiveFoodStats(RainWorldGame game)
    {
        if (game == null || game.GetStorySession == null)
            return;

        StoryGameSession session = game.GetStorySession;
        SaveState save = session.saveState;

        if (save == null || save.saveStateNumber != VoidEnums.SlugcatID.Void)
            return;

        // Clear the temporary rain override before updating permanent stats.
        RainFoodRequirement.Restore(game);

        int maxFood = 9 + (save.deathPersistentSaveData.karmaCap == 10 ? save.GetVoidExtraFood() : 0);
        int foodToHibernate = save.malnourished ? maxFood
            : 6 + (save.GetVoidExtraFood() == 3 ? save.GetVoidFoodToHibernate() : 0);

        session.characterStats.maxFood = maxFood;
        session.characterStats.foodToHibernate = foodToHibernate;

        if (session.characterStatsJollyplayer != null)
        {
            for (int i = 0; i < session.characterStatsJollyplayer.Length; i++)
            {
                SlugcatStats stats = session.characterStatsJollyplayer[i];
                if (stats == null || stats.name != VoidEnums.SlugcatID.Void)
                    continue;

                stats.maxFood = maxFood;
                stats.foodToHibernate = foodToHibernate;
            }
        }

        for (int i = 0; i < game.Players.Count; i++)
        {
            if (game.Players[i]?.realizedCreature is not Player player)
                continue;

            if (player.slugcatStats.name != VoidEnums.SlugcatID.Void)
                continue;

            player.slugcatStats.maxFood = maxFood;
            player.slugcatStats.foodToHibernate = foodToHibernate;
        }

        for (int i = 0; i < game.cameras.Length; i++)
        {
            if (game.cameras[i].hud is not HUD.HUD hud || hud.owner is not Player player
                || player.slugcatStats.name != VoidEnums.SlugcatID.Void || hud.foodMeter == null)
                continue;

            FoodMeter meter = hud.foodMeter;
            meter.maxFood = maxFood;
            meter.survivalLimit = foodToHibernate;
            meter.GetMeterExt().showNumFoodTohibernate = 2 * save.GetVoidFoodToHibernate();

            while (meter.circles.Count < maxFood)
            {
                FoodMeter.MeterCircle circle = new(meter, meter.circles.Count);
                meter.circles.Add(circle);
                circle.AddGradient();
                circle.AddCircles();
            }

            meter.MoveSurvivalLimit(foodToHibernate, true);
        }
    }

    private static void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
    {
        RainFoodSleep.Bind(self, package);
        orig(self, package);

        // Rain payments and progression were already resolved in SessionEnded.
        // A discounted marker is not proof of a new full-food growth event.
        if (RainFoodSleep.HasData(self))
            return;

        if (self.IsSleepScreen && self.saveState.VoidFullAnd11Karma(self.food, self.hud.foodMeter.survivalLimit, self.hud.foodMeter.maxFood)
            && (self.saveState.CanAddExtraFood(self.hud.foodMeter.maxFood)
                || (self.saveState.GetVoidExtraFood() > 0
                    && self.saveState.CanAddFoodToHibernate(self.hud.foodMeter.survivalLimit))))
        {
            self.saveState.food = 0;
            self.saveState.progression.SaveToDisk(true, false, false);
        }
    }

    private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        RainFoodSleep.TryBegin(self, game, survived, newMalnourished, out RainFoodSleep.SleepData sleep);
        bool succeeded = false;

        try
        {
            bool fullStomach = survived && game != null && game.GetStorySession != null
                && game.Players.Count > 0 && game.Players[0]?.realizedCreature is Player player
                && self.VoidFullAnd11Karma(player.FoodInStomach, 0, game.GetStorySession.characterStats.maxFood);

            // A full rain plan may already have paid some or all of its pips.
            // Completing the remainder in a shelter finishes the same plan.
            bool completedPlan = sleep != null && sleep.CompleteFullFoodPlan
                && self.saveStateNumber == VoidEnums.SlugcatID.Void
                && self.deathPersistentSaveData.karmaCap == 10;

            if (fullStomach || completedPlan)
                ApplyFullFoodProgression(self);

            RainFoodSleep.PrepareProgression(sleep, fullStomach);

            if (game != null && game.IsVoidStoryCampaign() && ExternalSaveData.VoidPermaNightmare == 2)
                self.SetKarmaToken(0);

            orig(self, game, survived, newMalnourished);
            succeeded = true;
        }
        finally
        {
            RainFoodSleep.End(game, sleep, succeeded);
        }
    }

    private static void FoodMeter_ctor(On.HUD.FoodMeter.orig_ctor orig, FoodMeter self, HUD.HUD hud, int maxFood, int survivalLimit, Player associatedPup, int pupNumber)
    {
        if (hud.owner is Player player
            && player.abstractCreature?.world?.game?.GetStorySession?.saveState is SaveState save)
            self.GetMeterExt().showNumFoodTohibernate = 2 * save.GetVoidFoodToHibernate();

        bool rainSleep = RainFoodSleep.PrepareMeter(self, hud, associatedPup, ref maxFood, ref survivalLimit);
        orig(self, hud, maxFood, survivalLimit, associatedPup, pupNumber);

        if (rainSleep)
        {
            RainFoodSleep.FinishMeter(self);
            return;
        }

        if (hud.owner is SleepAndDeathScreen screen && !screen.goalMalnourished
            && screen.saveState.VoidFullAnd11Karma(screen.food, survivalLimit, maxFood)
            && (screen.saveState.CanAddExtraFood(maxFood)
                || (screen.saveState.GetVoidExtraFood() > 0 && screen.saveState.CanAddFoodToHibernate(survivalLimit))))
            self.eatCircles = maxFood;
    }

    private static void FoodMeter_SleepUpdate(On.HUD.FoodMeter.orig_SleepUpdate orig, FoodMeter self)
    {
        if (RainFoodSleep.Update(orig, self))
            return;

        orig(self);

        if (self.hud.owner is not SleepAndDeathScreen screen)
            return;

        if (self.sleepScreenPhase == 0 && self.eatCircles == 0
            && screen.saveState.VoidFullAnd11Karma(screen.food, self.survivalLimit, self.maxFood))
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
            && c.TryGotoPrev(MoveType.After, x => x.MatchLdfld<RWCustom.IntVector2>("x")))
        {
            c.Emit(OpCodes.Ldarg, 4);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg, 1);
            c.EmitDelegate((int original, SlugcatStats.Name name, SlugcatSelectMenu.SlugcatPageContinue page, Menu.Menu menu) =>
            {
                if (name == VoidEnums.SlugcatID.Void && page.saveGameData.karmaCap == 10)
                {
                    SaveState save = menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false);
                    return original + save.GetVoidExtraFood();
                }

                return original;
            });

            if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<RWCustom.IntVector2>("y")))
            {
                c.Emit(OpCodes.Ldarg, 4);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg, 1);
                c.EmitDelegate((int original, SlugcatStats.Name name, SlugcatSelectMenu.SlugcatPageContinue page, Menu.Menu menu) =>
                {
                    if (name == VoidEnums.SlugcatID.Void
                        && (page.saveGameData.karmaCap == 10
                            || menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false) is SaveState save
                            && save.GetVoidMarkV3()))
                    {
                        save = menu.manager.rainWorld.progression.currentSaveState;
                        return 6 + (save.GetVoidExtraFood() == 3 ? save.GetVoidFoodToHibernate() : 0);
                    }

                    return original;
                });
            }
            else LogExErr("FoodChange.SlugcatPageContinue_ctor: food requirement not found.");
        }
    }

    private static void ShelterDoor_DoorClosed(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(MoveType.After, x => x.MatchCall<SlugcatStats>("SlugcatFoodMeter"), x => x.MatchLdfld(out _)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, ShelterDoor, int>>((original, self) =>
            {
                RainWorldGame game = self.room?.game;
                if (game == null || game.Players.Count == 0 || game.Players[0]?.realizedCreature is not Player player)
                    return original;

                bool isVoidSlugcat = game.StoryCharacter == VoidEnums.SlugcatID.Void;
                bool hasMaxKarma = player.KarmaCap == 10;
                bool hasVoidMark = game.GetStorySession?.saveState?.GetVoidMarkV3() ?? false;

                if (isVoidSlugcat && (hasMaxKarma || hasVoidMark || ExternalSaveData.VoidKarma11))
                    return 6;

                return original;
            });
        }
        else LogExErr("FoodChange.ShelterDoor_DoorClosed: food requirement not found.");
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);

        if (saveStateNumber == VoidEnums.SlugcatID.Void
            && (self.saveState.deathPersistentSaveData.karma == 10 || self.saveState.GetVoidMarkV3() || Karma11Update.VoidKarma11))
        {
            int maxFood = 9 + (self.saveState.deathPersistentSaveData.karmaCap == 10 ? self.saveState.GetVoidExtraFood() : 0);
            self.characterStats.foodToHibernate = self.saveState.malnourished ? maxFood
                : 6 + (self.saveState.GetVoidExtraFood() == 3 ? self.saveState.GetVoidFoodToHibernate() : 0);
            self.characterStats.maxFood = maxFood;
        }
    }
}
