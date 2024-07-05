using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OverseerHolograms;
using TheVoid;
using UnityEngine;
using static Creature;

namespace VoidTemplate;

static class CreatureHooks
{
    public static void Hook()
    {
        //On.OverseerCommunicationModule.AnyProgressionDirection += OverseerCommunicationModule_AnyProgressionDirection;
        //On.OverseerCommunicationModule.ReevaluateConcern += OverseerCommunicationModule_ReevaluateConcern;
        //On.OverseerCommunicationModule.WantToShowImage += OverseerCommunicationModule_WantToShowImage;
        On.Leech.Attached += OnLeechAttached;
        On.Creature.Violence += Creature_Violence;
        On.DaddyLongLegs.Eat += OnDaddyLongLegsEat;
    }

    private static bool OverseerCommunicationModule_WantToShowImage(On.OverseerCommunicationModule.orig_WantToShowImage orig, OverseerCommunicationModule self, string roomName)
    {
        if (self.player.abstractCreature.world.game.StoryCharacter == StaticStuff.TheVoid)
            return self.overseerAI.overseer.hologram.message != OverseerHologram.Message.GateScene &&
                   !self.GuideState.HasImageBeenShownInRoom(roomName);
        return orig(self, roomName);
    }

    private static void OverseerCommunicationModule_ReevaluateConcern(On.OverseerCommunicationModule.orig_ReevaluateConcern orig, OverseerCommunicationModule self, Player player)
    {
        if (player.abstractCreature.world.game.StoryCharacter == StaticStuff.TheVoid)
        {
            self.forcedDirectionToGive = null;
            self.inputInstruction = null;
        }
        orig(self, player);
    }

    private static bool OverseerCommunicationModule_AnyProgressionDirection(On.OverseerCommunicationModule.orig_AnyProgressionDirection orig, OverseerCommunicationModule self, Player player)
    {
        if (player.abstractCreature.world.game.StoryCharacter == StaticStuff.TheVoid)
            return false;
        return orig(self, player);
    }
    private static async void OnLeechAttached(On.Leech.orig_Attached orig, Leech self)
    {
        orig(self);

        if (Array.Exists(self.grasps, grasp => grasp.grabbed is Player player
        && player.slugcatStats.name == StaticStuff.TheVoid && self != null && self.room != null))
        {
            await Task.Delay(6000);
            self.Die();
        }
    }
    private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, DamageType type, float damage, float stunBonus)
    {
        if (self is Player player
            && player.slugcatStats.name == StaticStuff.TheVoid
            && type == DamageType.Stab)
        {
            int KarmaCap = player.KarmaCap;// Уменьшаем эффект оглушения
            float StunResistance = 1f - 0.09f * KarmaCap;
            float DamageResistance = 1f - 0.09f * KarmaCap;
            stunBonus *= StunResistance;
            damage *= DamageResistance;
        }
        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
    private static async void OnDaddyLongLegsEat(On.DaddyLongLegs.orig_Eat orig, DaddyLongLegs self, bool eu)
    {
        foreach (var eatObject in self.eatObjects)
        {
            if (eatObject.chunk.owner is Player player
                && player.slugcatStats.name == StaticStuff.TheVoid
                && player.dead)
            {
                await Task.Delay(3000);
                DestroyBody(player);
                self.Die();
                FinishEating(self);
                return;
            }
        }
        orig(self, eu);
    }
    private static void DestroyBody(Player player)
    {
        if (player != null && player.room != null)
        {
            player.room.RemoveObject(player);
        }
        player.dead = true;
    }

    private static void FinishEating(DaddyLongLegs self)
    {
        self.eatObjects.Clear();
        self.digestingCounter = 0;
        self.moving = false;
        self.tentaclesHoldOn = false;
    }
}
