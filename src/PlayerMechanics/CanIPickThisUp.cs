using Mono.Cecil.Cil;
using Mono.Cecil;
using System;
using System.Linq;
using VoidTemplate.Useful;
using MonoMod.Cil;
using MoreSlugcats;
using System.Reflection;
using static VoidTemplate.Useful.Utils;
using Watcher;

namespace VoidTemplate.PlayerMechanics;

public static class CanIPickThisUp
{
	public static void Hook()
	{
		On.Player.CanIPickThisUp += Player_CanIPickThisUp;
		On.Player.CanIPickThisUp += Player_CanIPickThisSpear;
        //On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
        On.Player.SlugOnBack.SlugToBack += Player_SlugToBack;
        On.Player.SlugOnBack.Update += SlugOnBack_Update;
        On.Player.GrabUpdate += Player_GrabUpdate;
        On.Player.Update += Player_Update;
        On.Player.ctor += Player_ctor;
        IL.Player.Grabability += Player_GrababilityHook;
    }

    public static int[] voidBurn = new int[32];

    private static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        bool result = orig(self, obj);
        if (!self.AreVoidViy()) return result;

        int heavyObjectsCount = 0;
        int amountOfSpearsInHands = 0;

        foreach (var grasp in self.grasps)
        {
            if (grasp?.grabbed == null) continue;

            var grabbedObj = grasp.grabbed;
            var grabability = self.Grabability(grabbedObj);

            if (grabability == Player.ObjectGrabability.Drag)
                heavyObjectsCount++;

            if (grabbedObj is Spear)
                amountOfSpearsInHands++;
        }

        if (heavyObjectsCount == 1 && obj is Spear) return true;

        var grabObjType = self.Grabability(obj);
        if (amountOfSpearsInHands == 1 && grabObjType == Player.ObjectGrabability.Drag) return true;

        return result;
    }

    public static bool Player_CanIPickThisSpear(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (self.AreVoidViy() && obj is Spear spear)
        {
            if (spear.mode == Weapon.Mode.StuckInWall && (!ModManager.MSC || !spear.abstractSpear.electric))
            {
                foreach (var grasp in self.grasps)
                {
                    if (grasp?.grabbed != null && self.Grabability(grasp.grabbed) >= Player.ObjectGrabability.BigOneHand)
                        return orig(self, obj);
                }
                return true;
            }
        }
        return orig(self, obj);
    }
    private static void Player_SlugToBack(On.Player.SlugOnBack.orig_SlugToBack orig, Player.SlugOnBack self, Player player)
    {
        if (self.slugcat != null)
        {
			return;
        }

        if (self.owner.AreVoidViy())
		{
            return;
        }

        if (player.IsVoid() && player.room.game.IsArenaSession || player.IsViy())
        {
            return;
        }

        orig(self, player);
    }

    private static void SlugOnBack_Update(On.Player.SlugOnBack.orig_Update orig, Player.SlugOnBack self, bool eu)
    {
        if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugcat != null && self.slugcat.IsVoid() && !self.owner.IsVoid())
        {
            voidBurn[self.owner.playerState.playerNumber]++;
            if (voidBurn[self.owner.playerState.playerNumber] % 40 == 0)
            {
                if (self.owner.playerState is not null)
                {
                    self.owner.playerState.permanentDamageTracking += 0.01f;
                    if (self.owner.playerState.permanentDamageTracking >= 1.0f)
                    {
                        self.owner.Die();
                    }
                }
            }
        }
            
        orig(self, eu);
    }

    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (ModManager.MSC || ModManager.CoopAvailable)
        {
            if (voidBurn != null && self.playerState != null &&
            self.playerState.playerNumber >= 0 && self.playerState.playerNumber < voidBurn.Length)
            {
                if (voidBurn[self.playerState.playerNumber] >= 1200)
                {
                    self.Stun(200);
                    self.LoseAllGrasps();
                    self.standing = false;
                    self.feetStuckPos = null;
                    if (self.spearOnBack != null && self.spearOnBack.spear != null)
                        self.spearOnBack.DropSpear();
                    if (self.slugOnBack != null && self.slugOnBack.slugcat != null)
                        self.slugOnBack.DropSlug();
                    voidBurn[self.playerState.playerNumber] = 1000;
                }


            }
            if (voidBurn[self.playerState.playerNumber] > 0)
            {
                if (self.slugOnBack.slugcat == null)
                {
                    voidBurn[self.playerState.playerNumber]--;
                }
                else if (self.slugOnBack.slugcat != null && !self.slugOnBack.slugcat.IsVoid())
                {
                    voidBurn[self.playerState.playerNumber]--;
                }
            }
        }
        orig(self, eu);
    }

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (ModManager.MSC || ModManager.CoopAvailable)
            voidBurn[self.playerState.playerNumber] = 0;
    }

    private static bool Player_IsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabCheck)
    {
        if (grabCheck is Player player && player.AreVoidViy()) return false;
        return orig(self, grabCheck);
    }

    private static void Player_GrababilityHook(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(
            MoveType.After,
            x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField("Slugpup", BindingFlags.Public | BindingFlags.Static))))
        {
            c.Index++;
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);

            c.EmitDelegate<Func<bool, Player, PhysicalObject, bool>>((orig, player, obj) =>
            {
                if (!player.AreVoidViy()) return orig;

                if (player.IsViy() && obj is Player target && !target.IsViy())
                    return true;

                if (obj is Player targetVoid)
                {
                    if (targetVoid.AreVoidViy())
                        return false;

                    if (targetVoid.bodyMode == Player.BodyModeIndex.Crawl && !targetVoid.room.game.IsArenaSession)
                        return true;
                }

                return orig;
            });

        }
        else
            LogExErr("Failed to find comparison to slugpup. void won't be able to grab slugpups");
    }
}
