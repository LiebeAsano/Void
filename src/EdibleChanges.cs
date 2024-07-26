using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate
{
    static class EdibleChanges
    {
        public static void Hook()
        {
            On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
            On.Mushroom.BitByPlayer += Mushroom_EatenByPlayer;
        }
        private static void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
        {
            if (self.bites < 2 && grasp.grabber is Player player && player.slugcatStats.name == StaticStuff.TheVoid)
            {
                self.bites--;
                self.room.PlaySound((self.bites == 0) ? SoundID.Slugcat_Eat_Karma_Flower : SoundID.Slugcat_Bite_Karma_Flower, self.firstChunk.pos);
                self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
                grasp.Release();
                self.Destroy();
                return;
            }
            orig(self, grasp, eu);
        }

        private static void Mushroom_EatenByPlayer(On.Mushroom.orig_BitByPlayer orig, Mushroom self, Creature.Grasp grasp, bool eu)
        {
            if (grasp.grabber is Player player && player.slugcatStats.name == StaticStuff.TheVoid)
            {
                self.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
                grasp.Release();
                self.Destroy();
                return;
            }
            orig(self, grasp, eu);
        }
    }
}
