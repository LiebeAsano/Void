using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class ImmuneToFallDamage
{
    public static void Hook()
    {
        On.Player.UpdateMSC += Player_Update;
    }

    private static void Player_Update(On.Player.orig_UpdateMSC orig, Player self)
    {
        orig(self);
        if (self.IsVoid() || self.IsViy())
        {
            bool hasGuardian = false;
            if (self.room != null)
            {
                foreach (AbstractCreature creature in self.room.abstractRoom.creatures)
                {
                    if (creature.creatureTemplate.type == CreatureTemplate.Type.TempleGuard)
                    {
                        hasGuardian = true;
                        break;
                    }
                }
            }
            if (hasGuardian)
            {
                self.immuneToFallDamage = 1;
            }
        }
    }
}
