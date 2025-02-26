using MoreSlugcats;

namespace VoidTemplate.PlayerMechanics.ViyMechanics;

internal static class ViyMaul
{
    public static void Hook()
    {
        //On.Player.SlugcatGrab += Player_SlugcatGrab;
        On.Player.CanMaulCreature += Player_CanMaulCreature;
        On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
        On.Player.Grabability += Player_Grabability;
    }

    private static bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Viy)
        {
            bool critStun = !crit.Stunned || crit.Stunned;
            if (crit != null && !crit.dead && (crit is not IPlayerEdible || (crit is Centipede && !(crit as Centipede).Edible)) && critStun)
            {
                crit.Stun(10);
                return true;
            }
        }
        return orig(self, crit);
    }

    private static bool Player_IsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabCheck)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Viy)
        {
            return true;
        }
        return orig(self, grabCheck);
    }

    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Viy)
        {
            if (obj is Fly
                || obj is Hazer
                || obj is PoleMimic
                || obj is Snail
                || (obj is Centipede && (obj as Centipede).Edible)
                || obj is LanternMouse
                || obj is EggBug
                || obj is FlyLure
                || obj is SmallNeedleWorm
                || obj is TentaclePlant
                || obj is VultureGrub
                || obj is Spider
                || obj is GarbageWorm
                || obj is Leech
                || obj is Deer
                || obj is JellyFish
                || obj is Vulture)
            {
                return orig(self, obj);
            }
            else
            {
                if (obj is BigJellyFish or Yeek or Cicada or JetFish)
                {
                    return Player.ObjectGrabability.TwoHands;
                }
                else
                {
                    if (obj is Creature && self.dontGrabStuff < 1 && obj != self)
                    {
                        return Player.ObjectGrabability.Drag;
                    }
                    else
                    {
                        return orig(self, obj);
                    }
                }
            }
        }
        return orig(self, obj);
    }
}