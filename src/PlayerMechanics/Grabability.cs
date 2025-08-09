using CoralBrain;
using IL.Watcher;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
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
        On.Creature.Update += Creature_Update;
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
        On.Player.IsCreatureLegalToHoldWithoutStun += Player_IsCreatureLegalToHoldWithoutStun;
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
        return self.AreVoidViy() && (obj is LanternMouse || obj is Watcher.Frog || obj is Watcher.Rat || obj is Watcher.Barnacle barnacle && !barnacle.hasShell);
    }
    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (CanOneHandGrabVoidViy(self, obj)) 
            return Player.ObjectGrabability.OneHand;
        if (self.AreVoidViy() && (obj is PoleMimic || obj is TentaclePlant))
            return Player.ObjectGrabability.CantGrab;
        if (self.AreVoidViy() && (obj is Cicada 
            || (obj is Player player && player != self && !player.AreVoidViy() && !player.room.game.IsArenaSession) 
            || obj is Watcher.BigMoth bigMoth && bigMoth.Small))
            return Player.ObjectGrabability.TwoHands;
        return orig(self, obj);
    }

    private static readonly Dictionary<Creature, Dictionary<BodyChunk, float>> OriginalMasses = [];

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);

        bool isGrabbedByVoidViy = false;
        bool maulTimer = false;
        bool inWater = false;

        if (self.grabbedBy != null)
        {
            foreach (var grasp in self.grabbedBy)
            {
                if (grasp?.grabber is Player grabberPlayer && grabberPlayer.AreVoidViy())
                {
                    if (grabberPlayer.mainBodyChunk.submersion >= 0.5f)
                    {
                        inWater = true;
                    }
                    if (grabberPlayer.maulTimer == 0)
                        maulTimer = true;
                    isGrabbedByVoidViy = true;
                    if (self is Player player && !player.AreVoidViy())
                    {
                        if (player.playerState is not null)
                        {
                            player.SetKillTag(grabberPlayer.abstractCreature);
                            player.playerState.permanentDamageTracking += 0.000125f;
                            if (player.playerState.permanentDamageTracking >= 1.0f)
                            {
                                player.Die();
                            }
                        }
                    }
                    else if (self is not Player)
                    {
                        if (self.State is HealthState)
                        {
                            (self.State as HealthState).health -= 0.000125f;
                            if (self.Template.quickDeath && (UnityEngine.Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || ((self.State as HealthState).health < 0f && UnityEngine.Random.value < 0.33f)))
                            {
                                self.Die();
                            }
                        }
                    }
                    break;
                }
            }
        }

        if (isGrabbedByVoidViy && !OriginalMasses.ContainsKey(self))
        {
            var chunkMasses = new Dictionary<BodyChunk, float>();
            foreach (var chunk in self.bodyChunks)
            {
                chunkMasses[chunk] = chunk.mass;
            }
            OriginalMasses[self] = chunkMasses;
        }

        if (OriginalMasses.TryGetValue(self, out var originalChunks))
        {
            foreach (var chunk in self.bodyChunks)
            {
                if (originalChunks.TryGetValue(chunk, out var originalMass))
                {
                    if (self is Player player || self is Cicada)
                    {
                        if (self is Player)
                        {
                            self.stun = 20;
                        }
                        chunk.mass = isGrabbedByVoidViy && maulTimer ? 0.05f : originalMass;
                    }
                    else if (self is Watcher.BigMoth bigMoth && bigMoth.Small)
                    {
                        chunk.mass = isGrabbedByVoidViy ? originalMass * 0.25f : originalMass;
                    }
                    else if (self is Lizard || self is Centipede || self is DropBug || self is BigNeedleWorm || self is BigSpider || self is Scavenger)
                    {
                        chunk.mass = isGrabbedByVoidViy ? originalMass * 0.5f : originalMass;
                    }
                    else if (self is JetFish)
                    {
                        chunk.mass = isGrabbedByVoidViy && !inWater ? originalMass * 0.5f : originalMass;
                    }
                }
            }

            if (!isGrabbedByVoidViy)
            {
                OriginalMasses.Remove(self);
            }
        }

        var deadEntries = OriginalMasses.Keys.Where(c => c.slatedForDeletetion || c.room == null).ToList();
        foreach (var deadCreature in deadEntries)
        {
            OriginalMasses.Remove(deadCreature);
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
            if (self.grasps[0]?.grabbed is Watcher.BigMoth bigMoth && bigMoth.Small || self.grasps[1]?.grabbed is Watcher.BigMoth bigMoth1 && bigMoth1.Small)
            {
                return false;
            }
        }
        if (obj is Player player && player.IsViy() && player.Consious)
        {
            return false;
        }
        if (obj is Player player2 && player2.IsVoid() && player2.Consious && player2.bodyMode != Player.BodyModeIndex.Crawl)
        {
            return false;
        }
        return orig(self, obj);
    }

    private static bool Player_IsCreatureLegalToHoldWithoutStun(On.Player.orig_IsCreatureLegalToHoldWithoutStun orig, Player self, Creature grabCheck)
    {
        return grabCheck is Watcher.BigMoth bigMoth && bigMoth.Small || orig(self, grabCheck);
    }

    public static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        orig(self);
    }
}
