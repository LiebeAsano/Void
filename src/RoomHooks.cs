using System.Linq;
using VoidTemplate;
using VoidTemplate.Objects;

namespace VoidTemplate
{
    static class RoomHooks
    {
        public static void Hook()
        {
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.Room.Loaded += RoomSpeficScript;
            On.TempleGuardAI.Update += TempleGuardAI_Update;
            On.TempleGuardAI.ThrowOutScore += TempleGuardAI_ThrowOutScore;
        }

        private static float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
        {
            if (crit.representedCreature.realizedCreature is Player player && player.slugcatStats.name == StaticStuff.TheVoid)
            {
                return 500f;
            }
            return orig(self, crit);
        }

        private static void TempleGuardAI_Update(On.TempleGuardAI.orig_Update orig, TempleGuardAI self)
        {
            orig(self);
            if (self.guard.room.PlayersInRoom.Any(i => i.slugcatStats.name == StaticStuff.TheVoid))
            {
                self.patience = 9999;
            }
        }

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
        }

        private static void RoomSpeficScript(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self.game?.session?.characterStats?.name == StaticStuff.TheVoid)
            {
                if(!self.game.GetStorySession.saveState.GetMessageShown() && self.game.Players.Exists(x => x.realizedCreature is Player p && p.slugcatStats.name == StaticStuff.TheVoid && p.KarmaCap == 4))
                {
                    self.AddObject(new KarmaCapTrigger(self, new KarmaCapTrigger.Message[]
                    {
                        new("Your body is strong enough to climb the ceilings.", 0, 400),
                        new("Hold down the 'Up' and 'Direction' buttons to climb the ceiling.")
                    }));
                }
                switch(self.abstractRoom.name)
                {
                    case "SB_E05":
                        self.AddObject(new ClimbTutorial(self));
                        break;
                    case StaticStuff.EndingRoomName:
                        self.AddObject(new Ending(self));
                        break;
                }
                
            }
        }

    }
}
