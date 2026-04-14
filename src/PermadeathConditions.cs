using Menu;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static RegionKit.Modules.Particles.V1.PBehaviourModule;
using static VoidTemplate.OptionInterface.OptionAccessors;
using static VoidTemplate.Useful.Utils;
using Object = UnityEngine.Object;

namespace VoidTemplate;

static class PermadeathConditions
{
    public static void Hook()
    {
        On.RainWorldGame.GameOver += GenericGameOver;
        On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
        On.RainWorldGame.GoToStarveScreen += RainWorldGame_GoToStarveScreen;
        On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
        On.RainWorldGame.ExitToMenu += ExitToMenuGameOver;

        On.Menu.KarmaLadder.KarmaSymbol.Update += PulsateKarmaSymbol;

        On.HUD.TextPrompt.EnterGameOverMode += TextPrompt_EnterGameOverMode;

        Application.quitting += ApplicationQuitGameOver;
    }

    #region Core conditions

    private static bool IsVoidStoryGame(RainWorldGame game)
    {
        return game != null
            && game.IsStorySession
            && game.GetStorySession != null
            && game.GetStorySession.saveState != null
            && game.GetStorySession.saveState.saveStateNumber == VoidEnums.SlugcatID.Void
            && game.IsVoidStoryCampaign()
            && !(ModManager.Expedition && game.rainWorld.ExpeditionMode);
    }

    private static bool VoidSpecificGameOverCondition(RainWorldGame game)
    {
        if (!IsVoidStoryGame(game))
            return false;

        StoryGameSession session = game.GetStorySession;
        SaveState save = session.saveState;

        return
            (save.deathPersistentSaveData.karma == 0 && PermaDeath)
            || save.GetKarmaToken() == 0
            || Karma11Update.VoidPermaNightmare
            || (
                save.cycleNumber >= VoidCycleLimit.GetVoidCycleLimit(save)
                && save.deathPersistentSaveData.karmaCap != 10
                && !save.GetVoidMarkV3()
                && PermaDeath
            );
    }

    public static void SetVoidCatDeadTrue(RainWorldGame game)
    {
        if (!IsVoidStoryGame(game) || !PermaDeath)
            return;

        SaveState save = game.GetStorySession.saveState;

        Player player = null;
        foreach (AbstractCreature abstractPlayer in game.Players)
        {
            if (abstractPlayer?.realizedCreature is Player realizedPlayer)
            {
                player = realizedPlayer;
                break;
            }
        }

        if (player != null && player.KarmaCap == 10)
        {
            save.SetKarmaToken(Math.Max(0, save.GetKarmaToken() - 1));
        }

        save.SetVoidCatDead(true);
        save.redExtraCycles = true;
        game.rainWorld.progression.SaveWorldStateAndProgression(false);
    }

    private static bool IsTreeEnding(RainWorldGame game)
    {
        if (game?.Players == null)
            return false;

        for (int i = 0; i < game.Players.Count; i++)
        {
            if (game.Players[i]?.Room != null && game.Players[i].Room.name == "OE_FINAL03")
                return true;
        }

        return false;
    }

    #endregion

    #region Main hooks

    private static void GenericGameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
    {
        if (!IsVoidStoryGame(self))
        {
            orig(self, dependentOnGrasp);
            return;
        }

        if (ModManager.CoopAvailable && self.rainWorld.options.JollyPlayerCount > 1)
        {
            orig(self, dependentOnGrasp);
            return;
        }

        if (!self.playedGameOverSound && dependentOnGrasp == null && self.cameras[0].hud != null)
        {
            self.cameras[0].hud.PlaySound(SoundID.HUD_Game_Over_Prompt);
            self.playedGameOverSound = true;
        }

        if (VoidSpecificGameOverCondition(self) && dependentOnGrasp == null)
        {
            self.GoToRedsGameOver();
            return;
        }

        self.GetStorySession.PlaceKarmaFlowerOnDeathSpot();

        if (self.cameras[0].hud != null)
        {
            if (self.Players[0].realizedCreature != null)
            {
                Player player = self.Players[0].realizedCreature as Player;

                if (self.Players[0].realizedCreature.room != null)
                {
                    self.cameras[0].hud.InitGameOverMode(
                        dependentOnGrasp,
                        player.FoodInStomach,
                        self.Players[0].pos.room,
                        Custom.RestrictInRect(
                            player.mainBodyChunk.pos,
                            self.Players[0].realizedCreature.room.RoomRect.Grow(50f)
                        )
                    );
                }
                else
                {
                    self.cameras[0].hud.InitGameOverMode(
                        dependentOnGrasp,
                        player.FoodInStomach,
                        self.Players[0].pos.room,
                        player.mainBodyChunk.pos
                    );
                }
            }
            else
            {
                self.cameras[0].hud.InitGameOverMode(
                    dependentOnGrasp,
                    0,
                    self.Players[0].pos.room,
                    new Vector2(0f, 0f)
                );
            }
        }

        self.manager.musicPlayer?.DeathEvent();
    }

    private static void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
    {
        if (!IsVoidStoryGame(self))
        {
            orig(self);
            return;
        }

        if (VoidSpecificGameOverCondition(self))
        {
            self.GoToRedsGameOver();
            return;
        }

        self.GetStorySession.saveState.SessionEnded(self, false, false);
        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.DeathScreen);
    }

    private static void RainWorldGame_GoToStarveScreen(On.RainWorldGame.orig_GoToStarveScreen orig, RainWorldGame self)
    {
        if (!IsVoidStoryGame(self))
        {
            orig(self);
            return;
        }

        if (VoidSpecificGameOverCondition(self))
        {
            self.GoToRedsGameOver();
            return;
        }

        self.GetStorySession.PlaceKarmaFlowerOnDeathSpot();
        self.GetStorySession.saveState.SessionEnded(self, false, false);
        self.manager.musicPlayer?.DeathEvent();
        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.StarveScreen);
    }

    private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
    {
        if (!IsVoidStoryGame(self))
        {
            orig(self);
            return;
        }

        if (self.manager.upcomingProcess != null)
            return;

        self.manager.musicPlayer?.FadeOutAllSongs(20f);

        /*if (self.manager.nextSlideshow != null)
          {
            self.manager.statsAfterCredits = true;
            self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
            return;
          }*/

        bool treeEnding = IsTreeEnding(self);

        if (VoidSpecificGameOverCondition(self) && !treeEnding)
        {
            self.GetStorySession.saveState.redExtraCycles = true;
            self.GetStorySession.saveState.SetVoidCatDead(true);
        }

        if (ModManager.CoopAvailable)
        {
            int num = 0;
            using IEnumerator<Player> enumerator =
                (from x in self.session.game.Players select x.realizedCreature as Player).GetEnumerator();

            while (enumerator.MoveNext())
            {
                Player player = enumerator.Current;
                self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
                num++;
            }
        }
        else
        {
            self.GetStorySession.saveState.AppendCycleToStatistics(
                self.Players[0].realizedCreature as Player,
                self.GetStorySession,
                true,
                0
            );
        }

        self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics, 10f);
    }

    #endregion

    #region Exit / quit behavior

    private static void ExitToMenuGameOver(On.RainWorldGame.orig_ExitToMenu orig, RainWorldGame self)
    {
        orig(self);

        if (VoidDreamScript.IsVoidDream)
        {
            VoidDreamScript.IsVoidDream = false;
            return;
        }

        if (!IsVoidStoryGame(self))
            return;

        if (self.world != null && self.world.rainCycle != null && self.world.rainCycle.timer > 30 * TicksPerSecond)
        {
            if (VoidSpecificGameOverCondition(self))
            {
                SetVoidCatDeadTrue(self);
            }

            StoryGameSession session = self.GetStorySession;
            SaveState save = session.saveState;

            save.SetKarmaToken(Math.Max(0, save.GetKarmaToken() - 1));
            save.SessionEnded(self, false, false);
        }
    }

    private static void ApplicationQuitGameOver()
    {
        if (VoidDreamScript.IsVoidDream)
            return;

        RainWorld rainWorld = Object.FindObjectOfType<RainWorld>();
        if (rainWorld == null)
            return;

        if (rainWorld.processManager is not ProcessManager manager)
            return;

        if (manager.currentMainLoop is not RainWorldGame game)
            return;

        if (!IsVoidStoryGame(game))
            return;

        if (VoidSpecificGameOverCondition(game))
        {
            SetVoidCatDeadTrue(game);
        }

        SaveState save = game.GetStorySession.saveState;
        if (save.GetKarmaToken() > 0 && save.deathPersistentSaveData.karmaCap == 10)
        {
            save.SetKarmaToken(Math.Max(0, save.GetKarmaToken() - 1));
            save.SessionEnded(game, false, false);
        }
    }

    #endregion

    #region UI

    private static void PulsateKarmaSymbol(On.Menu.KarmaLadder.KarmaSymbol.orig_Update orig, KarmaLadder.KarmaSymbol self)
    {
        bool vanillaFlag = ModManager.MSC
            && self.parent.displayKarma.x == self.parent.moveToKarma
            && (self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.KarmaToMinScreen
                || self.parent.menu.ID == MoreSlugcatsEnums.ProcessID.VengeanceGhostScreen
                || (ModManager.Expedition
                    && self.parent.menu.manager.rainWorld.ExpeditionMode
                    && self.parent.moveToKarma == 0));

        if (!vanillaFlag
            && ModManager.MSC
            && self.parent.displayKarma.x == self.parent.moveToKarma
            && self.menu is KarmaLadderScreen screen
            && screen.saveState?.saveStateNumber == VoidEnums.SlugcatID.Void
            && self.parent.moveToKarma == 0
            && self.parent.menu.ID == ProcessManager.ProcessID.DeathScreen
            && PermaDeath)
        {
            self.waitForAnimate++;
            if (self.waitForAnimate >= 50 && self.displayKarma.x == 0)
            {
                self.pulsateCounter++;
            }
        }

        orig(self);
    }

    #endregion

    private static void TextPrompt_EnterGameOverMode(On.HUD.TextPrompt.orig_EnterGameOverMode orig, HUD.TextPrompt self, Creature.Grasp dependentOnGrasp, int foodInStomach, int deathRoom, Vector2 deathPos)
    {
        orig(self, dependentOnGrasp, foodInStomach, deathRoom, deathPos);

        if (self.hud.owner is not Player player || player.room == null || player.room.game == null)
            return;

        if (!player.IsVoid() || !player.room.game.IsVoidWorld())
            return;

        if (player.dead)
        {
            if (player.KarmaCap < 4)
                self.gameOverString = "...";
            else if (VoidSpecificGameOverCondition(player.room.game))
                self.gameOverString = "";
            else
            {
                int random = UnityEngine.Random.Range(0, 6);
                switch (random)
                { 
                    case 0:
                        self.gameOverString = player.Karma == 1 ? "We cannot anymore..." : "It is painfull...";
                        break;
                    case 1:
                        self.gameOverString = Karma11Update.VoidKarma11 ? "This is mine." : player.room.game.GetStorySession.saveState.GetVoidMarkV3() ? "This is your for a while..." : "We are hungry...";
                        break;
                    case 2:
                        self.gameOverString = player.room.game.GetStorySession.saveState.GetVoidMarkV3() ? "Still part of you..." : "We are a single entity...";
                        break;
                    case 3:
                        self.gameOverString = player.room.game.GetStorySession.saveState.deathPersistentSaveData.theMark 
                            ? KarmaFlowerChanges.SaveVoidCycle
                                ? "This is not the end..." 
                                : "Betrayer..."
                            : KarmaFlowerChanges.SaveVoidCycle
                                ? "Stop eating that..."
                                : "We need more time..."; 
                        break;
                    case 4:
                        self.gameOverString = player.room.game.GetStorySession.saveState.GetKarmaToken() == 1 ? "Stay out of the way." : "";
                        break;
                    case 5:
                        self.gameOverString = UnityEngine.Random.Range(0, 2) == 0 ? "Do not share your feelings..." : "Do not share your memories...";
                        break;
                }
                    
            }
        }
        else
        {
            self.gameOverString = "Fight to get out of the grip by clicking PICK UP";
        }
    }
}