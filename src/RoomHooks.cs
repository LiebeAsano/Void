using System.Linq;
using TheVoid;
using VoidTemplate.Objects;

namespace VoidTemplate
{
    static class RoomHooks
    {
        public static void Hook()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.Room.Loaded += Room_Loaded;
            On.TempleGuardAI.Update += TempleGuardAI_Update;
            On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
        }

        private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
        {
            if (crit.representedCreature.realizedCreature is Player player && player.slugcatStats.name == Plugin.TheVoid)
            {
                return 500f;
            }
            return orig(self, crit);
        }

        private static void TempleGuardAI_Update(On.TempleGuardAI.orig_Update orig, TempleGuardAI self)
        {
            orig(self);
            if (self.guard.room.PlayersInRoom.Any(i => i.slugcatStats.name == Plugin.TheVoid))
            {
                self.patience = 9999;
            }
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self.game?.session?.characterStats?.name == Plugin.TheVoid)
            {
                if (self.abstractRoom.name == "SB_E05")
                    self.AddObject(new ClimbTutorial(self));
            }
        }

    }
}
