using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public class BasiliskResist
{
    public static void Hook() => On.Player.Update += Player_Update;

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if ((self.injectedPoison > 0 || self.mushroomCounter > 0) &&
            self.AreVoidViy() &&
            !self.chatlog)
        {
            self.mushroomCounter = 0;
            self.injectedPoison = 0;
        }
        orig(self, eu);
    }
}
