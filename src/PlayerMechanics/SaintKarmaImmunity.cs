using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using UnityEngine;
using VoidTemplate.OptionInterface;
using static MonoMod.InlineRT.MonoModRule;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics;

internal static class SaintKarmaImmunity
{
    public static void Hook()
    {
        //gives stun instead of death at karma 11
        On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint1;
        //IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
    }

    private static void Player_ClassMechanicsSaint1(On.Player.orig_ClassMechanicsSaint orig, Player self)
    {
        float num2 = 60f;
        Vector2 vector3 = new(self.mainBodyChunk.pos.x + self.burstX, self.mainBodyChunk.pos.y + self.burstY + 60f);
        bool flag3 = false;
        for (int i = 0; i < self.room.physicalObjects.Length; i++)
        {
            for (int j = self.room.physicalObjects[i].Count - 1; j >= 0; j--)
            {
                if (j >= self.room.physicalObjects[i].Count)
                {
                    j = self.room.physicalObjects[i].Count - 1;
                }
                PhysicalObject physicalObject = self.room.physicalObjects[i][j];
                if (physicalObject != self)
                {
                    foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
                    {
                        if (Custom.DistLess(bodyChunk.pos, vector3, num2 + bodyChunk.rad) && self.room.VisualContact(bodyChunk.pos, vector3))
                        {
                            if (physicalObject is Player)
                            {
                                if (!(physicalObject as Player).dead && ((physicalObject as Player).IsVoid() || (physicalObject as Player).IsViy()))
                                {
                                    flag3 = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        if (self.killFac >= 0.99f && flag3)
        {
            float num = 60f;
            Vector2 vector2 = new(self.mainBodyChunk.pos.x + self.burstX, self.mainBodyChunk.pos.y + self.burstY + 60f);
            bool flag2 = false;
            for (int i = 0; i < self.room.physicalObjects.Length; i++)
            {
                for (int j = self.room.physicalObjects[i].Count - 1; j >= 0; j--)
                {
                    if (j >= self.room.physicalObjects[i].Count)
                    {
                        j = self.room.physicalObjects[i].Count - 1;
                    }
                    PhysicalObject physicalObject = self.room.physicalObjects[i][j];
                    if (physicalObject != self)
                    {
                        foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
                        {
                            if (Custom.DistLess(bodyChunk.pos, vector2, num + bodyChunk.rad) && self.room.VisualContact(bodyChunk.pos, vector2))
                            {
                                bodyChunk.vel += Custom.RNV() * 36f;
                                if (physicalObject is Player)
                                {
                                    if (!(physicalObject as Player).dead && ((physicalObject as Player).IsVoid() || (physicalObject as Player).IsViy()))
                                    {
                                        flag2 = true;
                                    }
                                    if ((physicalObject as Player).IsVoid())
                                    {
                                        (physicalObject as Player).Stun(200);
                                    }
                                    if ((physicalObject as Player).IsViy())
                                    {
                                        (physicalObject as Player).Stun(100);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (flag2)
            {
                self.room.PlaySound(SoundID.Firecracker_Bang, self.mainBodyChunk, false, 1f, 0.75f + UnityEngine.Random.value);
                self.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.mainBodyChunk, false, 1f, 0.5f + UnityEngine.Random.value * 0.5f);
            }
            else
            {
                self.room.PlaySound(SoundID.Snail_Pop, self.mainBodyChunk, false, 1f, 1.5f + UnityEngine.Random.value);
            }
            for (int n = 0; n < 20; n++)
            {
                self.room.AddObject(new Spark(vector2, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
            }
            self.killFac = 0f;
            self.killWait = 0f;
            self.killPressed = true;
            if (self.voidSceneTimer > 0)
            {
                self.voidSceneTimer = 0;
                self.DeactivateAscension();
                self.controller = null;
                self.forceBurst = false;
                return;
            }
            if (flag2)
            {
                return;
            }
        }
        orig(self);
    }

    private static void Player_ClassMechanicsSaint(ILContext il)
    {

        try
        {
            ILCursor c = new ILCursor(il);
            //if (physicalObject is Creature)
            //{
            //    if (!(physicalObject as Creature).dead)
            //    {
            //        flag2 = true;
            //    }
            //    (physicalObject as Creature) <if void > .Die();
            //    <TO label2
            //    label
            //    //this is a bubble for the condition "void"
            //    POP creature
            //    if victim is the void stun for 5 seconds
            //    label2>
            //}
            c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(1),
                i => i.MatchStloc(15),
                i => i.MatchLdloc(18),
                i => i.MatchIsinst<Creature>());

            var label = c.DefineLabel();
            var label2 = c.DefineLabel();
            c.Emit(OpCodes.Dup);
            c.EmitDelegate<Func<Creature, bool>>((self) =>
                self is Player player && (player.IsVoid() || player.IsViy() || player.room.game.IsArenaSession && OptionAccessors.ArenaAscensionStun));
            c.Emit(OpCodes.Brtrue_S, label);
            c.TryGotoNext(MoveType.After,
                i => i.MatchCallvirt<Creature>("Die"));
            c.Emit(OpCodes.Br, label2);
            c.MarkLabel(label);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloc, 18);
            c.EmitDelegate((PhysicalObject PhysicalObject) =>
            {
                if (PhysicalObject is Player p && (p.IsVoid() || p.IsViy() || p.room.game.IsArenaSession && OptionAccessors.ArenaAscensionStun))
                {
                    p.Stun(TicksPerSecond * 5);
                }
            });
            c.MarkLabel(label2);
        }
        catch (Exception e)
        {
            _Plugin.logger.LogError(e);
            throw;
        }
    }
}
