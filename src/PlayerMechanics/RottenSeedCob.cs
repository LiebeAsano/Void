using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class RottenSeedCob
{
    public static void Hook()
    {
        On.Player.Update += Player_Update;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.IsVoid() && self.eatExternalFoodSourceCounter > 0)
        {
            self.eatExternalFoodSourceCounter--;
            if (self.eatExternalFoodSourceCounter < 1)
            {
                if (self.externalFoodSourceRotten)
                {
                    self.AddFood(1); ;
                }
                else
                {
                    self.AddFood(1);
                }
                self.dontEatExternalFoodSourceCounter = 45;
                self.handOnExternalFoodSource = null;
                self.room.PlaySound(SoundID.Slugcat_Bite_Fly, self.mainBodyChunk);
                self.externalFoodSourceRotten = false;
            }
        }
        orig(self, eu);
    }
}
