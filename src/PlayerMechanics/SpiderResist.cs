using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics
{
    public class SpiderResist
    {
        public static void Hook()
        {
            On.Spider.Update += Spider_Update;
            On.Player.ctor += Player_ctor;
        }

        private static int[] SpiderKiller = new int[32];

        private static void Spider_Update(On.Spider.orig_Update orig, Spider self, bool eu)
        {
            if (self.grasps[0] != null && self.grasps[0].grabbed is Player player && (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy))
            {
                self.Attached();
                if (SpiderKiller[player.playerState.playerNumber] >= 60)
                {
                    self.Die();
                    SpiderKiller[player.playerState.playerNumber] = 0;
                }
                return;
            }
            orig(self, eu);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            SpiderKiller[self.playerState.playerNumber] = 0;
        }
    }
}
