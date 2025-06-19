using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics
{
    public class FrogResist
    {
        public static void Hook()
        {
            On.Watcher.Frog.Update += Frog_Update;
            On.Watcher.Frog.SuckFood += Frog_SuckFood;
        }

        private static int[] frogDead = new int [32];

        private static void Frog_Update(On.Watcher.Frog.orig_Update orig, Watcher.Frog self, bool eu)
        {
            if (self.room == null)
            {
                return;
            }
            if (self.grasps != null && self.graspHasPlayer && self.attachedObject is Player player && (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy))
            {
                frogDead[player.playerState.playerNumber]++;
                if (player.injectedPoison > 0f)
                {
                    player.injectedPoison = 0;
                }
                if (frogDead[player.playerState.playerNumber] >= 120)
                {
                    self.Die();
                    frogDead[player.playerState.playerNumber] = 0;
                }
                if (self.ShouldHaveUmbilical && (((self.umbilical != null || self.umbilical.room != null) && self.umbilical.room != self.room) || self.makeNewUmbilical) && self.Consious)
                {
                    self.umbilical.RemoveFromRoom();
                    self.umbilical.slatedForDeletetion = true;
                    self.makeNewUmbilical = false;
                    self.MakeUmbilical();
                }
            }
            orig(self, eu);
        }

        private static void Frog_SuckFood(On.Watcher.Frog.orig_SuckFood orig, Watcher.Frog self)
        {
            if (self.attachedObject is Player player && (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy))
            {
                return;
            }
            else
            {
                orig(self);
            }
        }
    }
}
