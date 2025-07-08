using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.ViyMechanics.ViyTentacles
{
    public class TentaclesPlayerHooks
    {
        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.Player.NewRoom += Player_NewRoom;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.IsViy() && !self.TryGetRot(out _))
            {
                RotCWT.rotModule.Add(self, new(self));
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.TryGetRot(out var rot) && self.room != null)
            {
                rot.Update();
            }
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);
            if (self.TryGetRot(out var rot))
            {
                rot.NewRoom(newRoom);
            }
        }
    }
}
