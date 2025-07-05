using CoralBrain;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

public static class Grabability
{
    public static void Hook()
    {
        //prevents grabbing pole plant for void
        //IL.Player.MovementUpdate += Player_Movement;
        On.Player.Grabability += Player_Grabability;
        On.Player.Update += Player_Update;
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
        On.SlugcatHand.Update += SlugcatHand_Update;
        //allows hand switching when holding big object
        //IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    private static void Player_Movement(ILContext il)
    {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNext(
            i => i.MatchLdflda<PhysicalObject>("dynamicRunSpeed"),
            i => i.MatchLdcR4(3.6f),
            i => i.MatchStfld<float[]>("[1]")
            ))
        {
            cursor.Index += 3;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(self =>
            {
                if (self.slugcatStats.name == VoidEnums.SlugcatID.Void)
                {
                    self.dynamicRunSpeed[0] *= 10f;
                    self.dynamicRunSpeed[1] *= 10f;
                }
            });
        }
    }

    private static void Player_GrabUpdate(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new(il);
        ILLabel skipGrababilityCheck = c.DefineLabel();
        if (c.TryGotoNext(x => x.MatchCall(typeof(Player).GetMethod(nameof(Player.Grabability), bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)))
            && c.TryGotoNext(MoveType.After, x => x.MatchLdcI4(3))
            && c.TryGotoPrev(MoveType.After, x => x.MatchBrfalse(out skipGrababilityCheck)))
        {
            LogExInf("applying hooke");
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Predicate<Player>>((player) => player.IsVoid());
            c.Emit(OpCodes.Brtrue, skipGrababilityCheck);
        }
        else LogExErr("search for grabability check failed. Void won't be able to swap hands with heavy objects");
    }

    public static bool CanOneHandGrabVoidViy(Player self, PhysicalObject obj)
    {
        return self.AreVoidViy() && (obj is LanternMouse || obj is Watcher.Frog || obj is Watcher.Barnacle barnacle && !barnacle.hasShell);
    }
    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (CanOneHandGrabVoidViy(self, obj)) 
            return Player.ObjectGrabability.OneHand;
        if (self.AreVoidViy() && (obj is PoleMimic || obj is TentaclePlant))
            return Player.ObjectGrabability.CantGrab;
        if (self.AreVoidViy() && (obj is Cicada || (obj is Player player && player != self && !player.AreVoidViy() && (player.bodyMode == Player.BodyModeIndex.Crawl || player.bodyMode == Player.BodyModeIndex.Stunned) && !player.room.game.IsArenaSession)))
            return Player.ObjectGrabability.TwoHands;
        return orig(self, obj);
    }

    private static readonly Dictionary<Player, (float chunk0Mass, float chunk1Mass)> OriginalMasses = [];
    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (!OriginalMasses.ContainsKey(self))
        {
            OriginalMasses[self] = (self.bodyChunks[0].mass, self.bodyChunks[1].mass);
        }

        bool shouldBeLight = false;

        if (!self.dead && self.grabbedBy != null && self.grabbedBy.Count > 0)
        {
            foreach (var grasp in self.grabbedBy)
            {
                if (grasp.grabber is Player grabberPlayer && grabberPlayer != self && grabberPlayer.AreVoidViy())
                {
                    self.stun = 20;
                    self.bodyChunks[0].mass = 0.05f;
                    self.bodyChunks[1].mass = 0.05f;
                    shouldBeLight = true;
                    break;
                }
            }
        }

        if (!shouldBeLight && OriginalMasses.TryGetValue(self, out var original))
        {
            self.bodyChunks[0].mass = original.chunk0Mass;
            self.bodyChunks[1].mass = original.chunk1Mass;
        }

        if (self.dead && OriginalMasses.ContainsKey(self))
        {
            OriginalMasses.Remove(self);
        }
    }

    public static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (self.AreVoidViy())
        {
            if (self.grasps[0]?.grabbed is Player || self.grasps[1]?.grabbed is Player)
            {
                return false;
            }
            if (self.grasps[0]?.grabbed is Cicada || self.grasps[1]?.grabbed is Cicada)
            {
                return false;
            }
        }
        return orig(self, obj);
    }

    public static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        orig(self);
    }
}
