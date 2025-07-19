using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;
public static class Crawl
{
    public static void Hook()
    {
        On.Player.Update += Player_Update;
    }

    public static int[] crawlTicks = new int[32];

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.AreVoidViy())
        {
            if (!self.input[0].jmp && self.input[0].y == 0 && self.bodyMode == Player.BodyModeIndex.Crawl)
            {
                crawlTicks[self.playerState.playerNumber]++;
                int karmaCap;
                if (self.KarmaCap >= 4 || Karma11Update.VoidKarma11)
                {
                    karmaCap = 6;
                }
                else if (self.KarmaCap == 3)
                {
                    karmaCap = 3;
                }
                else
                {
                    karmaCap = 0;
                }
                if (crawlTicks[self.playerState.playerNumber] >= 10 && self.bodyChunks[0].ContactPoint.x != 0 && self.bodyChunks[0].ContactPoint.x == self.input[0].x)
                {
                    BodyChunk body_chunk_0 = self.bodyChunks[0];
                    float force = self.gravity * 3 + karmaCap;
                    body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, force, 0.3f, 1f);
                }
            }
            else
                crawlTicks[self.playerState.playerNumber] = 0;
        }
        orig(self, eu);
    }
}
