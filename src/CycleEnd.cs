using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using VoidTemplate.Useful;

namespace VoidTemplate;

public static class CycleEnd
{
    public static bool changedMark;

    public static void Hook()
    {
        On.ShelterDoor.Close += CycleEndLogic;
        //On.RainWorldGame.Update += RainWorldGame_Update;
        //IL.ShelterDoor.Update += ShelterDoor_Update;
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        On.RainWorldGame.Win += RainWorldGame_Win;
        On.SaveState.SessionEnded += SaveState_SessionEnded;
    }

    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
        orig(self, saveStateNumber, game);

        if (game.IsVoidStoryCampaign())
            changedMark = false;
    }

    private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
    {
        if (self.manager.upcomingProcess == null)
        {
            if (self.IsVoidWorld() && malnourished && !self.GetStorySession.saveState.GetVoidMarkV3())
            {
                self.GoToDeathScreen();
                return;
            }

            if (self.IsVoidStoryCampaign() && !PermadeathConditions.TryPrepareVoidCycleAdvance(self))
                return;
        }

        orig(self, malnourished, fromWarpPoint);
    }

    private static void ShelterDoor_Update(ILContext il)
    {
        ILCursor c = new(il);
        var bubblestart = c.DefineLabel();
        var pastbubble = c.DefineLabel();

        if (c.TryGotoNext(
            MoveType.Before,
            x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToStarveScreen))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<ShelterDoor, bool>>(self => self.room.game.IsVoidWorld());
            c.Emit(OpCodes.Brtrue_S, bubblestart);
        }
        else
            _Plugin.logger.LogError($"IL hook starting at CycleEnd:23, shelter door update, starve logic tinker, failed to apply");

        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToStarveScreen))))
        {
            c.Emit(OpCodes.Br, pastbubble);
            c.MarkLabel(bubblestart);
            c.EmitDelegate((RainWorldGame game) => game.GoToDeathScreen());
            c.MarkLabel(pastbubble);
        }
        else
            _Plugin.logger.LogError($"IL hook starting at CycleEnd:41, shelter door update, starve logic tinker, failed to apply");
    }

    private static void CycleEndLogic(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        orig(self);

        RainWorldGame game = self.room.game;

        if (!game.IsVoidWorld())
            return;

        game.Players.ForEach(absPlayer =>
        {
            if (absPlayer.realizedCreature is not Player player || !player.IsVoid())
                return;

            if (player.room != null &&
                player.room == self.room &&
                player.FoodInStomach < player.slugcatStats.foodToHibernate &&
                self.room.game.session is StoryGameSession session &&
                session.characterStats.name == VoidEnums.SlugcatID.Void &&
                (!ModManager.Expedition || !self.room.game.rainWorld.ExpeditionMode))
            {
                if (PermadeathConditions.VoidSpecificGameOverCondition(game))
                    game.GoToRedsGameOver();
            }
        });
    }

    private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
    {
        if (game != null && game.IsVoidStoryCampaign())
        {
            if (changedMark)
            {
                if (!survived)
                    self.deathPersistentSaveData.theMark = !self.deathPersistentSaveData.theMark;

                changedMark = false;
            }
        }

        orig(self, game, survived, newMalnourished);
    }
}