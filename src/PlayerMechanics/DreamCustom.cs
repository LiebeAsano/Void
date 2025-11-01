using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics.Karma11Features;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

public static class DreamCustom
{
    public static void Hook()
    {
        On.DreamsState.EndOfCycleProgress += CustomDream;
        On.Menu.DreamScreen.Singal += DreamScreen_Singal;
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate;
        On.RainWorldGame.GameOver += RainWorldGame_GameOver;
        IL.RainWorldGame.ctor += RainWorldGame_ctor;
        IL.RainWorldGame.CommunicateWithUpcomingProcess += RainWorldGame_CommunicateWithUpcomingProcess;
    }

    private static void RainWorldGame_CommunicateWithUpcomingProcess(ILContext il)
    {
        ILCursor c = new(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdsfld<MoreSlugcatsEnums.SlugcatStatsName>("Artificer"),
            x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Equality"))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool orig, RainWorldGame game) => orig || game.StoryCharacter == VoidEnums.SlugcatID.Void);
        }
        else LogExErr("Error in IL hook. Old saveSate not be used after dream");
    }

    public static void VoidDreamEnd(this RainWorldGame game)
    {
        if (VoidDreamScript.IsVoidDream)
        {
            VoidDreamScript.IsVoidDream = false;
            game.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            List<AbstractCreature> list = [.. game.session.Players];
            game.session = new StoryGameSession(VoidEnums.SlugcatID.Void, game)
            {
                Players = [.. list]
            };
            VoidDreamScript.StateAfterDream = game.session.Players[0].state.alive ? 1 : 2;
            game.manager.musicPlayer?.FadeOutAllSongs(20f);
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SleepScreen, 10f);
        }
    }

    private static void RainWorldGame_ctor(ILContext il)
    {
        ILCursor c = new(il);
        ILCursor endPos = new(il);
        if (c.TryGotoNext(MoveType.After, x => x.MatchStfld<RainWorldGame>(nameof(RainWorldGame.wasAnArtificerDream))) &&
            endPos.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<ProcessManager>(nameof(ProcessManager.artificerDreamNumber)),
            x => x.MatchLdcI4(-1),
            x => x.MatchBeq(out _)))
        {
            ILLabel label = endPos.MarkLabel();
            c.EmitDelegate(() => VoidDreamScript.IsVoidDream);
            c.Emit(OpCodes.Brtrue, label);
        }
        else LogExErr("Can't emit void dream to arti dream");
    }

    private static void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
    {
        if (VoidDreamScript.IsVoidDream)
        {
            self.VoidDreamEnd();
            return;
        }
        orig(self, dependentOnGrasp);
    }

    private static AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
    {
        if (VoidDreamScript.IsVoidDream)
        {
            AbstractCreature hunter = new(self.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new(self.world.offScreenDen.index, -1, -1, 0), new(-1, 0));
            hunter.state = new PlayerState(hunter, 0, VoidEnums.SlugcatID.Void, false);
            self.world.offScreenDen.AddEntity(hunter);
            self.session.AddPlayer(hunter);
            return hunter;
        }
        return orig(self, player1, player2, player4, player4, location);
    }

    private static void DreamScreen_Singal(On.Menu.DreamScreen.orig_Singal orig, Menu.DreamScreen self, Menu.MenuObject sender, string message)
    {
        if (message == "CONTINUE" && (self.dreamID == VoidEnums.DreamID.HunterRotDream ||
            (self.scene != null && self.scene.sceneID == VoidEnums.SceneID.HunterRot)))
        {
            VoidDreamScript.IsVoidDream = true;
            self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            self.StartGame();
            self.PlaySound(SoundID.MENU_Dream_Button);
            return;
        }
        orig(self, sender, message);
    }

    private static void CustomDream(On.DreamsState.orig_EndOfCycleProgress orig, DreamsState self, SaveState saveState, string currentRegion, string denPosition)
    {
        if (saveState.saveStateNumber == VoidEnums.SlugcatID.Void)
        {
            switch (currentRegion)
            {
                case "LF":
                    {
                        int random = UnityEngine.Random.Range(0, 3);
                        if (random == 0)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Farm);
                        break;
                    }
                case "SI":
                    {
                        int random = UnityEngine.Random.Range(0, 3);
                        if (random == 0)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sky);
                        break;
                    }
                case "SB":
                    {
                        int random = UnityEngine.Random.Range(0, 3);
                        if (random == 0)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Sub);
                        break;
                    }
            }
            switch (saveState.cycleNumber)
            {
                case >= 24:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.HunterRot);
                        break;
                    }
                case >= 18:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidSea);
                        break;
                    }
                case >= 12:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidBody);
                        break;
                    }
                case >= 6:
                    {
                        saveState.EnlistDreamIfNotSeen(SaveManager.Dream.NSH);
                        break;
                    }
            }
            switch (saveState.deathPersistentSaveData.karmaCap)
            {
                case 10:
                    {
                        if (Karma11Update.VoidKarma11)
                            saveState.EnlistDreamIfNotSeen(SaveManager.Dream.VoidHeart);
                        break;
                    }
            }
        }
        orig(self, saveState, currentRegion, denPosition);
    }
}
