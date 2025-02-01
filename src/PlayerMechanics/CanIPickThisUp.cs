using Mono.Cecil.Cil;
using Mono.Cecil;
using System;
using System.Linq;
using VoidTemplate.Useful;
using MonoMod.Cil;
using MoreSlugcats;
using System.Reflection;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class CanIPickThisUp
{
	public static void Hook()
	{
		On.Player.CanIPickThisUp += Player_CanIPickThisUp;
		On.Player.CanIPickThisUp += Player_CanIPickThisSpear;
		On.Player.SlugOnBack.SlugToBack += Player_SlugToBack;
        IL.Player.Grabability += Player_GrababilityHook;
    }

    private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
	{
        var result = orig(self, obj);
        if (self.slugcatStats.name != VoidEnums.SlugcatID.Void && self.slugcatStats.name != VoidEnums.SlugcatID.Viy) return result;
        int heavyObjectsCount = 0;
        foreach (var grasp in self.grasps) if (grasp?.grabbed != null && self.Grabability(grasp.grabbed) == Player.ObjectGrabability.Drag) heavyObjectsCount++;
        if (heavyObjectsCount == 1 && obj is Spear) return true;
        int amountOfSpearsInHands = self.grasps.Aggregate(func: (int acc, Creature.Grasp grasp) => acc + ((grasp?.grabbed is Spear) ? 1 : 0), seed: 0);
		if (amountOfSpearsInHands == 1 && self.Grabability(obj) == Player.ObjectGrabability.Drag) return true;
        return result;
	}

	public static bool Player_CanIPickThisSpear(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
	{
		if (self.IsVoid() || self.IsViy())
		{
			bool canPick = true;
			foreach (var grasp in self.grasps)
			{
				if (grasp != null && self.Grabability(grasp.grabbed) >= Player.ObjectGrabability.BigOneHand)
				{
					canPick = false;
					break;
				}
			}
			if (obj is Spear spear && spear.mode == Weapon.Mode.StuckInWall &&
				(!ModManager.MSC || !spear.abstractSpear.electric) && canPick)
				return true;
		}
		return orig(self, obj);
	}
    private static void Player_SlugToBack(On.Player.SlugOnBack.orig_SlugToBack orig, Player.SlugOnBack self, Player player)
    {
        if (self.slugcat != null)
        {
			return;
        }

        if (self.owner.slugcatStats.name == VoidEnums.SlugcatID.Void || self.owner.slugcatStats.name == VoidEnums.SlugcatID.Viy)
		{
            return;
        }

        if (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy)
        {
            return;
        }

        orig(self, player);
    }

    private static void Player_GrababilityHook(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField("Slugpup", BindingFlags.Public | BindingFlags.Static))))
        {
            c.Index++;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);

            c.EmitDelegate<Func<bool, Player, PhysicalObject, bool>>((orig, player, obj) =>
            {
                if (player.slugcatStats.name == VoidEnums.SlugcatID.Void || player.slugcatStats.name == VoidEnums.SlugcatID.Viy)
                {
                    if (obj is Player targetPlayer && targetPlayer.slugcatStats.name == VoidEnums.SlugcatID.Void)
                        return false;
                    else if (obj is Creature && player.IsViy())
                        return true;
                    else if (obj is Player player2 && player2.bodyMode == Player.BodyModeIndex.Crawl && !player2.room.game.IsArenaSession)
                        return true;
                    else return orig;
                }
                else
                    return orig;
            });

        }
        else
            LogExErr("Failed to find comparison to slugpup. void won't be able to grab slugpups");
    }
}
