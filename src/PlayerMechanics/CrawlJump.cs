using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class CrawlJump
{
    public static void Hook()
    {
        //On.Player.Jump += Player_Jump;
    }

    private static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        if (self.IsVoid())
        {
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.superLaunchJump < 20)
            {
                return;
            }
        }
        orig(self);
    }
}
