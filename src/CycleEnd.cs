using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using VoidTemplate.Useful;

namespace VoidTemplate;

internal static class CycleEnd
{
    private static void log(object e) => _Plugin.logger.LogInfo(e);
    public static void Hook()
    {
        On.ShelterDoor.Close += CycleEndLogic;
        //On.RainWorldGame.Update += RainWorldGame_Update;
        //IL.ShelterDoor.Update += ShelterDoor_Update;
        On.RainWorldGame.Win += RainWorldGame_Win;
    }

    private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
    {
        if(self.IsVoidWorld() && malnourished)  //while it may seem bad that no orig is summoned while the condition is true, thing is, it's not supposed to be a game win if it's true
        {
            self.GoToDeathScreen();
            return;
        }
        orig(self, malnourished);
    }

    private static void ShelterDoor_Update(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new(il);
        var bubblestart = c.DefineLabel();
        var pastbubble = c.DefineLabel();
        // this.room.game <if TheVoid campaign, go to bubblestart>.GoToStarveScreen();
        // < go to bubbleend 
        // bubblestart
        // pop
        // go to death screen
        // bubbleend >
        if (c.TryGotoNext(MoveType.Before,
            x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToStarveScreen))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<ShelterDoor, bool>>((self) => self.room.game.IsVoidWorld());
            c.Emit(OpCodes.Brtrue_S, bubblestart);
        }
        else
            _Plugin.logger.LogError($"IL hook starting at CycleEnd:23, shelter door update, starve logic tinker, failed to apply");
        if (c.TryGotoNext(MoveType.After,
             x => x.MatchCallvirt<RainWorldGame>(nameof(RainWorldGame.GoToStarveScreen))))
        {
            c.Emit(OpCodes.Br, pastbubble);
            c.MarkLabel(bubblestart);
            c.EmitDelegate((RainWorldGame game) => game.GoToDeathScreen());
            c.MarkLabel(pastbubble);
        }
        else _Plugin.logger.LogError($"IL hook starting at CycleEnd:41, shelter door update, starve logic tinker, failed to apply");
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        if (timerStarted) timer++;
        if (timer > timeToWait)
        {
            self.GoToStarveScreen();
            timerStarted = false;
            timer = 0;
        }

    }

    //immutable
    private const int timeToWait = Utils.TicksPerSecond * 3;

    //mutable
    private static int timer = 0;
    private static bool timerStarted;

    private static void CycleEndLogic(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        orig(self);
        RainWorldGame game = self.room.game;
        game.Players.ForEach(absPlayer =>
        {
            if (absPlayer.realizedCreature is Player player
            && player.IsVoid()
            && player.room != null
            && player.room == self.room
            && player.FoodInStomach < player.slugcatStats.foodToHibernate
            && self.room.game.session is StoryGameSession session
            && session.characterStats.name == VoidEnums.SlugcatID.TheVoid
            && (!ModManager.Expedition || !self.room.game.rainWorld.ExpeditionMode))
            {
                if (session.saveState.deathPersistentSaveData.karma == 0) game.GoToRedsGameOver();
                //else timerStarted = true;
            }
        });
    }

}
