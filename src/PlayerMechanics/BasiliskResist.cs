using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public class BasiliskResist
{
    public static void Hook()
    {
        On.Player.Update += Player_Update;
        On.Creature.InjectPoison += Creature_InjectPoison;
        On.Creature.Update += Creature_Update;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.mushroomCounter > 0 &&
            self.AreVoidViy() &&
            !self.chatlog)
        {
            self.mushroomCounter = 0;
        }
        if (self.injectedPoison > 0 && self.AreVoidViy())
        {
            int karma = self.KarmaCap;
            if (self.KarmaCap == 10)
                karma = 9;
            self.injectedPoison -= 0.00025f * 40 * 0.1f * (karma + 1);
        }
        orig(self, eu);
    }

    private static void Creature_InjectPoison(On.Creature.orig_InjectPoison orig, Creature self, float amount, Color poisonColor)
    {
        if (self is Player player && player.AreVoidViy())
        {
            int karma = player.KarmaCap;
            if (player.KarmaCap == 10)
                karma = 9;
            self.injectedPoison += amount * (1f - 0.5f * karma + 1);
            return;
        }
        orig(self, amount, poisonColor);
    }

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        if (self is Player player && player.AreVoidViy())
        {
            if (player.injectedPoison / player.Template.instantDeathDamageLimit >= 1f)
            {
                player.injectedPoison = player.Template.instantDeathDamageLimit - 0.1f;
            }
        }
        orig(self, eu);
    }
}
