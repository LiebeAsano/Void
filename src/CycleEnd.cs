using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate;

internal static class CycleEnd
{
    public static void Hook()
    {
        On.ShelterDoor.Close += CycleEndLogic;
    }

    //immutable
    private const int preStarveTimer;
    private static void CycleEndLogic(On.ShelterDoor.orig_Close orig, ShelterDoor self)
    {
        orig(self);
        RainWorldGame game = self.room.game;
        game.Players.ForEach(absPlayer =>
        {
            if (absPlayer.realizedCreature is Player player
            && player.slugcatStats.name == StaticStuff.TheVoid
            && player.room != null
            && player.room == self.room
            && player.FoodInStomach < player.slugcatStats.foodToHibernate
            && self.room.game.session is StoryGameSession session
            && session.characterStats.name == StaticStuff.TheVoid
            && (!ModManager.Expedition || !self.room.game.rainWorld.ExpeditionMode))
            {
                if (session.saveState.deathPersistentSaveData.karma == 0 || session.saveState.deathPersistentSaveData.karma == 10) game.GoToRedsGameOver();
            }
        });
    }

}
