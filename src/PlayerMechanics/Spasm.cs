using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Objects;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

internal static class Spasm
{
    public static void Hook()
    {
        On.RainWorldGame.Update += RainWorldGame_Update;
    }

    private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
        for (int i = 0; i < self.Players.Count; i++)
        {
            Player player = self.Players[i].realizedCreature as Player;
            if (self.IsVoidStoryCampaign() && player.IsVoid())
            {
                bool hasMark = self.IsStorySession && (self.GetStorySession.saveState.deathPersistentSaveData.theMark);

                if (!hasMark
                    && player.KarmaCap != 10
                    && !Karma11Update.VoidKarma11
                    && player.KarmaCap > 3)
                {
                    float MaxSize = 220000;
                    float Lenght = 2.5f;
                    MaxSize = MaxSize * 0.1f * player.KarmaCap;
                    Lenght = Lenght * 1 / player.KarmaCap;
                    if (VoidCycleLimit.YieldVoidCycleDisplayNumberWithPlayer(player, self.GetStorySession.saveState.cycleNumber) < 10)
                    {
                        MaxSize = MaxSize * VoidCycleLimit.YieldVoidCycleDisplayNumberWithPlayer(player, self.GetStorySession.saveState.cycleNumber) / 10;
                        Lenght = 5f;
                    }
                    float random = UnityEngine.Random.Range(1, MaxSize);
                    if (random == 1)
                    {
                        HunterSpasms.Spasm(player, Lenght, 1f);
                    }
                }
                break;
            }
        }
    }
}
