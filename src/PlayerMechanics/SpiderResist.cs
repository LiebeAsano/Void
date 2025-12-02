using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics
{
    public class SpiderResist
    {
        public static void Hook()
        {
            On.Spider.Attached += Spider_Attached;
        }

        private static void Spider_Attached(On.Spider.orig_Attached orig, Spider self)
        {
            BodyChunk bodyChunk = self.grasps[0].grabbed.bodyChunks[self.grasps[0].chunkGrabbed];
            self.graphicsAttachedToBodyChunk = bodyChunk;
            if (bodyChunk.owner is Player player && player.IsVoid())
            {
                if (bodyChunk.owner is Creature)
                {
                    if (!(bodyChunk.owner as Creature).dead)
                    {
                        float num = 0f;
                        if (bodyChunk.owner is Creature)
                        {
                            for (int i = 0; i < bodyChunk.owner.grabbedBy.Count; i++)
                            {
                                if (bodyChunk.owner.grabbedBy[i].grabber is Spider)
                                {
                                    num += bodyChunk.owner.grabbedBy[i].grabber.TotalMass;
                                }
                            }
                        }
                    }
                    else if (UnityEngine.Random.value < 0.001f)
                    {
                        (bodyChunk.owner as Creature).leechedOut = true;
                    }
                }
                if (self.centipede != null)
                {
                    self.centipede.lightAdaption = 1f;
                }
                Vector2 a = Custom.DirVec(self.mainBodyChunk.pos, bodyChunk.pos);
                float num2 = Vector2.Distance(self.mainBodyChunk.pos, bodyChunk.pos);
                float num3 = self.mainBodyChunk.rad + bodyChunk.rad;
                float num4 = self.mainBodyChunk.mass / (self.mainBodyChunk.mass + bodyChunk.mass);
                self.mainBodyChunk.vel += a * (num2 - num3) * (1f - num4);
                self.mainBodyChunk.pos += a * (num2 - num3) * (1f - num4);
                bodyChunk.vel -= a * (num2 - num3) * num4;
                bodyChunk.pos -= a * (num2 - num3) * num4;
                for (int j = 0; j < self.grasps[0].grabbed.bodyChunks.Length; j++)
                {
                    self.PushOutOfChunk(self.grasps[0].grabbed.bodyChunks[j]);
                }
                for (int k = 0; k < self.grasps[0].grabbed.grabbedBy.Count; k++)
                {
                    if (self.grasps[0].grabbed.grabbedBy[k].grabber != self)
                    {
                        for (int l = 0; l < self.grasps[0].grabbed.grabbedBy[k].grabber.bodyChunks.Length; l++)
                        {
                            self.PushOutOfChunk(self.grasps[0].grabbed.grabbedBy[k].grabber.bodyChunks[l]);
                        }
                    }
                }
                if (((bodyChunk.owner as Player).IsVoid() && UnityEngine.Random.value < 0.01f) || UnityEngine.Random.value < 0.00083333335f || (bodyChunk.owner as Creature).enteringShortCut != null || self.centipede == null || self.centipede.totalMass < bodyChunk.owner.TotalMass)
                {
                    self.Die();
                }
            }
            else
            {
                orig(self);
            }
        }
    }
}
