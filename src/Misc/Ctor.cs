using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.Misc;

internal static class Ctor
{
    public static void Hook()
    {
        On.Player.ctor += Player_Ctor;
    }

    private static void Player_Ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstract_creature, World world)
    {
        orig(player, abstract_creature, world);
        if (world.game.session is StoryGameSession session && session.characterStats.name == VoidEnums.SlugcatID.TheVoid)
            player.slugcatStats.foodToHibernate = session.characterStats.foodToHibernate;
        if (player.slugcatStats.name != VoidEnums.SlugcatID.TheVoid) return;
        player.Add_Attached_Fields();
    }
}
