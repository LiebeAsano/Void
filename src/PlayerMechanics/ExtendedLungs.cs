using System;
using VoidTemplate.Useful;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

internal static class ExtendedLungs
{
	public static void Hook()
	{
		On.Player.Update += Player_Update;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            if (self.KarmaCap != 10 && !ExternalSaveData.VoidKarma11)
            {
                int karma = self.KarmaCap;

                float baseLungAirConsumption = 1.0f;
                float reducePerKarma = 0.07f;
                float newLungCapacity = baseLungAirConsumption - (reducePerKarma * (karma + 1));

                self.slugcatStats.lungsFac = newLungCapacity;

            }
            else
                self.slugcatStats.lungsFac = 0.2f;
        }
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Viy)
        {
            if (ExternalSaveData.ViyLungExtended)
            {
                self.slugcatStats.lungsFac = 0.0f;
            }
            else if (Utils.IsViyStoryCampaign(self.abstractCreature.world.game))
            {
                int random = UnityEngine.Random.Range(0, 10000);
                if (random == 0)
                {
                    _ = new Objects.KarmaRotator(self.abstractCreature.Room.realizedRoom);
                    ExternalSaveData.ViyLungExtended = true;
                }
                self.slugcatStats.lungsFac = 0.2f;
            }
        }
    }

    private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);
        if (self.player.IsViy() && ExternalSaveData.ViyLungExtended)
        {
            self.breath = 0f;
            self.lastBreath = 0f;
        }
    }
}
