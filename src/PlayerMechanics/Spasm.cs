using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Objects;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class Spasm
{
    public static void Hook()
    {
        On.RainWorldGame.Update += RainWorldGame_Update;
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (self?.Players == null) return;

        for (int i = 0; i < self.Players.Count; i++)
        {
            if (self.Players[i]?.realizedCreature is not Player player || player.slugcatStats == null) continue;

            if (self.IsVoidStoryCampaign() && player.IsVoid() && !player.dead)
            {
                if (self.GetStorySession?.saveState == null) continue;

                if (player.KarmaCap != 10
                    && player.KarmaCap > 3
                    && !Karma11Update.VoidKarma11
                    && !self.GetStorySession.saveState.GetVoidMarkV3()
                    && !KarmaFlowerChanges.SaveVoidCycle)
                {
                    float MaxSize = 220000f;
                    float Lenght = 10f;
                    MaxSize = MaxSize * 0.1f * player.KarmaCap;

                    if (VoidCycleLimit.YieldVoidCycleDisplayNumberWithPlayer(player, self.GetStorySession.saveState.cycleNumber) < 10 && OptionAccessors.PermaDeath)
                    {
                        MaxSize = MaxSize * VoidCycleLimit.YieldVoidCycleDisplayNumberWithPlayer(player, self.GetStorySession.saveState.cycleNumber) / 10;
                        Lenght = 20f;
                    }

                    float random = UnityEngine.Random.Range(1, MaxSize);
                    random = (int)random;
                    if (random == 1)
                    {
                        self.GetStorySession.saveState.EnlistDreamIfNotSeen(SaveManager.Dream.Rot);
                        HunterSpasms.Spasm(player, Lenght, 1f);
                    }
                }
                break;
            }
        }
    }
}
