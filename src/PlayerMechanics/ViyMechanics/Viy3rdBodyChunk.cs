using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics
{
    public class Viy3rdBodyChunk
    {
        public static void Hook()
        {
            On.Player.ctor += Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.IsViy())
            {
                BodyChunk[] chunks = self.bodyChunks;
                Array.Resize(ref chunks, 3);
                self.bodyChunks = chunks;
                float mass = (0.7f * self.slugcatStats.bodyWeightFac) / 3f;
                self.bodyChunks[0].mass = mass;
                self.bodyChunks[1].mass = mass;
                self.bodyChunks[2] = new BodyChunk(self, 2, default, 8f, mass);
                self.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[2];
                self.bodyChunkConnections[0] = new(self.bodyChunks[0], self.bodyChunks[2], 17f, PhysicalObject.BodyChunkConnection.Type.Normal, 1, 0.5f);
                self.bodyChunkConnections[1] = new(self.bodyChunks[2], self.bodyChunks[1], 17f, PhysicalObject.BodyChunkConnection.Type.Normal, 1, 0.5f);
            }
        }
    }
}
