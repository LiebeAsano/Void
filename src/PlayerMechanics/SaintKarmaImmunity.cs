using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Watcher;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using VoidTemplate.OptionInterface;
using static MonoMod.InlineRT.MonoModRule;
using static VoidTemplate.Useful.Utils;
using VoidTemplate.Creatures.VoidDaddyAndProtoViy;
using VoidTemplate.PlayerMechanics.Karma11Features;

namespace VoidTemplate.PlayerMechanics;

public static class SaintKarmaImmunity
{
    public static int[] deathCounter = [-1, -1, -1, -1];

    public static void Hook()
    {
        //gives stun instead of death at karma 11
        On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
        //IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
        On.Player.Update += Player_Update_Void_To_Daddy;
        On.Player.ctor += Player_ctor;
    }

    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        deathCounter[self.playerState.playerNumber] = -1;
    }

    private static void Player_Update_Void_To_Daddy(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);
        if (self.IsVoid() && deathCounter[self.playerState.playerNumber] > -1 && self.room != null)
        {
            deathCounter[self.playerState.playerNumber]++;
            if (deathCounter[self.playerState.playerNumber] == 220)
            {
                /*self.room.AddObject(new ShockWave(self.firstChunk.pos, 350f, 0.285f, 200, true));
                self.room.AddObject(new ShockWave(self.firstChunk.pos, 750f, 0.185f, 180, false));*/
                self.room.PlaySound(WatcherEnums.WatcherSoundID.RotLiz_Vocalize, self.firstChunk.pos, self.abstractCreature);
            }
            if (deathCounter[self.playerState.playerNumber] >= 240)
            {
                AbstractCreature daddy = new(self.abstractCreature.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy), null, self.abstractCreature.pos, self.abstractCreature.world.game.GetNewID());
                (daddy.state as DaddyLongLegs.DaddyState).GetDaddyExt().daddyType = DaddyExt.VoidDaddyType.ProtoViy;
                (daddy.state as DaddyLongLegs.DaddyState).GetDaddyExt().myColor = VoidColors[self.playerState.playerNumber];
                self.abstractCreature.Room.AddEntity(daddy);
                daddy.RealizeInRoom();
                daddy.realizedCreature.Stun(50);
                foreach (var tentacle in (daddy.realizedCreature as DaddyLongLegs).tentacles)
                {
                    tentacle.Reset(tentacle.connectedChunk.pos);
                }
                self.Destroy();
                deathCounter[self.playerState.playerNumber] = -1;
            }
        }
    }

    /*private static void Player_Update_Void_To_Daddy(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        if (self.IsVoid() && deathCounter[self.playerState.playerNumber] > -1 && self.room != null)
        {
            deathCounter[self.playerState.playerNumber]++;

            if (deathCounter[self.playerState.playerNumber] == 220)
            {
                self.room.PlaySound(WatcherEnums.WatcherSoundID.RotLiz_Vocalize, self.firstChunk.pos, self.abstractCreature);
            }

            if (deathCounter[self.playerState.playerNumber] >= 240)
            {
                Color eyeColor = VoidColors[self.playerState.playerNumber];

                AbstractCreature protoViy = new(
                    self.abstractCreature.world,
                    StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC),
                    null,
                    self.abstractCreature.pos,
                    self.abstractCreature.world.game.GetNewID()
                );

                if (protoViy.state is PlayerNPCState npcState)
                {
                    npcState.forceFullGrown = true;
                    npcState.slugcatCharacter = VoidEnums.SlugcatID.ProtoViy;
                }

                new Player(protoViy, protoViy.world)
                {
                    SlugCatClass = VoidEnums.SlugcatID.ProtoViy,
                    standing = true,
                    bodyMode = Player.BodyModeIndex.Stand
                };

                protoViy.abstractAI.RealAI = new SlugNPCAI(protoViy, protoViy.world);

                self.abstractCreature.Room.AddEntity(protoViy);
                protoViy.RealizeInRoom();

                if (protoViy.abstractAI is SlugNPCAbstractAI slugNpcAI)
                {
                    slugNpcAI.toldToStay = protoViy.pos;
                }

                self.Destroy();
                deathCounter[self.playerState.playerNumber] = -1;

                ProtoViyEyeColors[protoViy] = eyeColor;
            }
        }
    }*/

    private static void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
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
                            if (physicalObject is DaddyLongLegs)
                            {
                                flag3 = true;
                            }
                            if (flag3) break;
                        }
                    }
                    if (flag3) break;
                }
                if (flag3) break;
            }
        }
        if (self.killFac >= 0.99f && flag3)
        {
            float num = 60f;
            Vector2 vector2 = new(self.mainBodyChunk.pos.x + self.burstX, self.mainBodyChunk.pos.y + self.burstY + 60f);
            Vector2 saintVector = new(self.mainBodyChunk.pos.x, self.mainBodyChunk.pos.y);
            bool selfSaint = false;
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
                        bool flagged = false;
                        foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
                        {
                            if (Custom.DistLess(bodyChunk.pos, vector2, num + bodyChunk.rad) && self.room.VisualContact(bodyChunk.pos, vector2))
                            {
                                if (!flagged)
                                {
                                    if (physicalObject is Player player)
                                    {
                                        if (!player.dead && player.AreVoidViy())
                                        {
                                            flagged = true;
                                            flag2 = true;
                                            if (player.IsVoid())
                                            {
                                                if (player.KarmaCap == 10 || Karma11Update.VoidKarma11)
                                                {
                                                    self.Stun(200);
                                                    selfSaint = true;
                                                }
                                                else
                                                {
                                                    player.Die();
                                                    deathCounter[player.playerState.playerNumber] = 0;
                                                    bodyChunk.vel += Custom.RNV() * 36f;
                                                }
                                            }
                                        }
                                        if (player.IsViy())
                                        {
                                            player.Stun(10);
                                        }
                                    }
                                    if (physicalObject is DaddyLongLegs pDaddy)
                                    {
                                        flagged = true;
                                        flag2 = true;
                                        pDaddy.Stun(10);
                                    }
                                }
                            }
                        }
                    }
                    if (selfSaint)
                    {
                        foreach (BodyChunk bodyChunk in self.bodyChunks)
                        {
                            bodyChunk.vel += Custom.RNV() * 36f;
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
                if (!selfSaint)
                    self.room.AddObject(new Spark(vector2, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                else
                    self.room.AddObject(new Spark(saintVector, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
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
