using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        //allows hand switching when holding big object
        //IL.Player.GrabUpdate += Player_GrabUpdate;
        IL.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
        On.Creature.Grasp.Release += Grasp_Release;
        On.Player.IsCreatureImmuneToPlayerGrabStun += Player_IsCreatureImmuneToPlayerGrabStun;
        On.Player.TerrainImpact += Player_TerrainImpact;
    }

    private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
    {
        if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player && player.AreVoidViy() && !self.Consious)
            speed = 0;
        orig(self, chunk, direction, speed, firstContact);
    }

    private static bool Player_IsCreatureImmuneToPlayerGrabStun(On.Player.orig_IsCreatureImmuneToPlayerGrabStun orig, Player self, Creature grabCheck)
    {
        if (self.AreVoidViy() && grabCheck is Player) return false;
        return orig(self, grabCheck);
    }

    private static void Grasp_Release(On.Creature.Grasp.orig_Release orig, Creature.Grasp self)
    {
        orig(self);
        if (self.grabber is Player p && p.AreVoidViy() && self.grabbed is Creature)
        {
            self.grabbed.CollideWithObjects = true;
        }
    }

    private static void Player_GraphicsModuleUpdated(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel skip = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchIsinst<Player>(),
            x => x.MatchBrfalse(out skip)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) =>
            {
                return !self.AreVoidViy();
            });
            c.Emit(OpCodes.Brfalse, skip);
        }
    }

    public static bool CanOneHandGrabVoidViy(Player self, PhysicalObject obj)
    {
        if (self == null || obj == null)
            return false;

        return self.AreVoidViy() && (obj is LanternMouse || obj is Watcher.Frog || obj is Watcher.Rat ||
               (obj is Watcher.Barnacle barnacle && !barnacle.hasShell));
    }
    private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self == null || obj == null)
            return orig(self, obj);

        if (CanOneHandGrabVoidViy(self, obj))
            return Player.ObjectGrabability.OneHand;

        if (obj is PoleMimic || obj is TentaclePlant)
            return Player.ObjectGrabability.CantGrab;

        if (self.AreVoidViy())
        {
            if (obj is Cicada)
                return Player.ObjectGrabability.Drag;

            if (obj is Player player && player != self && !player.AreVoidViy())
            {
                if (player.room?.game?.IsArenaSession != true)
                return Player.ObjectGrabability.OneHand;
            }

            if (obj is Watcher.BigMoth bigMoth && bigMoth.Small)
                return Player.ObjectGrabability.Drag;
        }

        return orig(self, obj);
    }

    private static readonly ConditionalWeakTable<Creature, float[]> OriginalMasses = new();

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        if (self == null || self.slatedForDeletetion || self.room == null)
        {
            orig(self, eu);
            return;
        }

        orig(self, eu);

        bool isGrabbedByVoidViy = false;
        bool maulTimer = false;

        if (self.grabbedBy != null)
        {
            foreach (var grasp in self.grabbedBy)
            {
                if (grasp?.grabber is Player grabberPlayer && grabberPlayer.AreVoidViy())
                {
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
                    else if (self is not Player && (self is not TubeWorm tubeWorm || !tubeWorm.dead))
                    {
                        if (self.State is HealthState)
                        {
                            self.SetKillTag(grabberPlayer.abstractCreature);
                            (self.State as HealthState).health -= 0.000125f;
                            if (self.Template.quickDeath && (UnityEngine.Random.value < -(self.State as HealthState).health || (self.State as HealthState).health < -1f || ((self.State as HealthState).health < 0f && UnityEngine.Random.value < 0.33f)))
                            {
                                self.Die();
                            }
                        }
                    }
                    if (grabberPlayer.Grabability(self) == Player.ObjectGrabability.OneHand && (!(self.Template.smallCreature || (self is Centipede centi && centi.Small))))
                    {
                        self.CollideWithObjects = false;
                    }
                    break;
                }
            }
        }
        float[] origChunkMasses = null;
        if (!OriginalMasses.TryGetValue(self, out origChunkMasses) && isGrabbedByVoidViy)
        {
            origChunkMasses = new float[self.bodyChunks.Length];
            OriginalMasses.Add(self, origChunkMasses);
            foreach (var chunk in self.bodyChunks)
            {
                origChunkMasses[chunk.index] = chunk.mass;
            }
        }

        if (origChunkMasses != null)
        {
            foreach (var chunk in self.bodyChunks)
            {
                float originalMass = origChunkMasses[chunk.index];

                if (self is Player)
                {
                    chunk.mass = isGrabbedByVoidViy && maulTimer ? 0.05f : originalMass;
                }
                else if (self is Cicada || self is JetFish)
                {
                    chunk.mass = isGrabbedByVoidViy && self.dead ? 0.05f : originalMass;
                }
                else if (self is Watcher.BigMoth bigMoth && bigMoth.Small)
                {
                    chunk.mass = isGrabbedByVoidViy ? originalMass * 0.25f : originalMass;
                }
                else if (self is Lizard || self is Centipede || self is DropBug || self is BigNeedleWorm || self is BigSpider || self is Scavenger)
                {
                    chunk.mass = isGrabbedByVoidViy ? originalMass * 0.5f : originalMass;
                }
            }
        }

        if (!isGrabbedByVoidViy)
        {
            OriginalMasses.Remove(self);
        }
    }

    public static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
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

}
