using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics
{
    internal class BasiliskResist
    {
        public static void Hook()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (self.injectedPoison > 0 || self.mushroomCounter > 0 && (self.slugcatStats.name == VoidEnums.SlugcatID.Void || self.slugcatStats.name != VoidEnums.SlugcatID.Viy))
            {
                self.mushroomCounter = 0;
                self.injectedPoison = 0;
            }
            orig(self, eu);
        }
    }
}
