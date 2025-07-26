using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics
{
    public class LocustResist
    {
        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
            On.LocustSystem.Swarm.Update += Swarm_Update;
            On.LocustSystem.GroundLocust.DoSwarming += GroundLocust_DoSwarming;
        }

        private static int[] SmartLocust = new int[32];

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            SmartLocust[self.playerState.playerNumber] = 0;
        }

        private static void Swarm_Update(On.LocustSystem.Swarm.orig_Update orig, LocustSystem.Swarm self)
        {
            orig(self);
            if (self.target is Player player && self.locusts.Count > self.maxLocusts - 5 && UnityEngine.Random.value < 0.8f && player.AreVoidViy())
            {
                self.killCounter = 0;
                self.RemoveLocust(self.locusts[UnityEngine.Random.Range(0, self.locusts.Count)]);
                SmartLocust[player.playerState.playerNumber]++;
            }
        }

        private static void GroundLocust_DoSwarming(On.LocustSystem.GroundLocust.orig_DoSwarming orig, LocustSystem.GroundLocust self, LocustSystem owner)
        {
            Creature realizedCreature = self.swarm.target;
            if (realizedCreature is Player player && SmartLocust[player.playerState.playerNumber] >= 60 && player.AreVoidViy())
            {
                return;
            }
            orig(self, owner);
        }
    }
}
