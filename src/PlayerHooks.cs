using System;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using TheVoid;
using static SlugBase.Features.FeatureTypes;
using UnityEngine;
using SlugBase.Features;
using Unity.Jobs;
using System.Runtime.CompilerServices;

namespace VoidTemplate
{
    static class PlayerHooks
    {
        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("thevoid/super_jump");

        public static void Hook()
        {
            On.Player.Jump += Player_Jump;
            On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;
            On.Player.CanIPickThisUp += Player_CanIPickThisUp;


            // TODO: check if it works
            // this is easier but overrides orig() in case that your slugcat is used; should be fine though;
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
            On.Player.ctor += Player_Ctor;

            On.Player.SwallowObject += Player_SwallowObject;
            On.Player.Update += Player_Update;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;

            On.Player.Grabability += Player_Grabability;

            IL.Player.UpdateAnimation += Player_UpdateAnimation;

            IL.Player.UpdateMSC += Player_ForbidenDrone;
        }
        private static void Player_ForbidenDrone(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("game"),
                    i => i.MatchLdfld<RainWorldGame>("wasAnArtificerDream"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, Player, bool>>((re, self) =>
                    re && (self.abstractCreature.world.game.session is StoryGameSession session &&
                           session.saveStateNumber == Plugin.TheVoid));

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        private static void Player_UpdateAnimation(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After,
                    i => i.MatchCallvirt<ClimbableVinesSystem>("VineCurrentlyClimbable"));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<bool, Player, bool>>((re, self) =>
                {
                    if (self.slugcatStats.name == Plugin.TheVoid &&
                        self.room.climbableVines.vines[self.vinePos.vine] is PoleMimic)
                        return false;
                    return re;
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is PoleMimic || obj is TentaclePlant)
                return Player.ObjectGrabability.CantGrab;
            return orig(self, obj);
        }



        private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self,
            PhysicalObject testObj)
        {
            if (self.slugcatStats.name == Plugin.TheVoid)
            {
                return testObj is not Creature && testObj is not Spear && testObj is not VultureMask || orig(self,testObj);
            }
            return orig(self, testObj);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.slugcatStats.name == Plugin.TheVoid)
            {
                self.forceSleepCounter = 0;
            }
        }

        private static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
        {
            if (self.slugcatStats.name == Plugin.TheVoid && self.Karma != 10)
            {
                if (self.room != null && self.grasps[grasp].grabbed is PebblesPearl &&
                    self.room.updateList.Any(i => i is Oracle oracle && oracle.oracleBehavior is SSOracleBehavior))
                {

                    ((self.room.updateList.First(i => i is Oracle) as Oracle)
                        .oracleBehavior as SSOracleBehavior).EatPearlsInterrupt();
                }
            }
            orig(self, grasp);

            if (self.slugcatStats.name == Plugin.TheVoid && self.Karma != 10 && self.objectInStomach != null)
            {
                self.objectInStomach.Destroy();
                self.objectInStomach = null;
                self.AddQuarterFood();
            }

        }

        private static void Player_Ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstract_creature, World world)
        {
            orig(player, abstract_creature, world);
            if (world.game.session is StoryGameSession session && session.characterStats.name == Plugin.TheVoid)
                player.slugcatStats.foodToHibernate = session.characterStats.foodToHibernate;
            if (player.slugcatStats.name != Plugin.TheVoid) return;
            player.Add_Attached_Fields();


        }

        private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player player)
        {
            if (player.slugcatStats.name != Plugin.TheVoid)
            {
                orig(player);
                return;
            }
            //if (player.room != null)
            //    Debug.Log($"{player.abstractCreature.pos}");
            if (player.bodyMode != Player.BodyModeIndex.WallClimb)
            {
                orig(player);
                return;
            }

            // don't forget to update the counters when using an On-hook; this is copy&paste vanilla code;
            player.diveForce = Mathf.Max(0f, player.diveForce - 0.05f);
            player.waterRetardationImmunity = Mathf.InverseLerp(0f, 0.3f, player.diveForce) * 0.85f;

            if (player.dropGrabTile.HasValue && player.bodyMode != Player.BodyModeIndex.Default && player.bodyMode != Player.BodyModeIndex.CorridorClimb)
            {
                player.dropGrabTile = null;
            }

            if (player.bodyChunks[0].ContactPoint.y < 0)
            {
                player.upperBodyFramesOnGround++;
                player.upperBodyFramesOffGround = 0;
            }
            else
            {
                player.upperBodyFramesOnGround = 0;
                player.upperBodyFramesOffGround++;
            }

            if (player.bodyChunks[1].ContactPoint.y < 0)
            {
                player.lowerBodyFramesOnGround++;
                player.lowerBodyFramesOffGround = 0;
            }
            else
            {
                player.lowerBodyFramesOnGround = 0;
                player.lowerBodyFramesOffGround++;
            }
            UpdateBodyMode_WallClimb(player);
        }




        // Implement SuperJump
        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);

            if (SuperJump.TryGet(self, out var power))
            {
                self.jumpBoost *= 1f + power;
            }
        }

        public static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            if (self.slugcatStats.name == Plugin.TheVoid)
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
        public static bool IsClimbingOnBeam(this Player player)
        {
            int player_animation = (int)player.animation;
            return (player_animation >= 6 && player_animation <= 12) || player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam;
        }
        public static void UpdateBodyMode_WallClimb(Player player)
        {
            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];

            player.canJump = 1;
            player.standing = true;

            // don't climb on one-tile "walls" instead of crawling (for example)
            if (body_chunk_1.contactPoint.x == 0 && body_chunk_1.contactPoint.y == -1)
            {
                player.animation = Player.AnimationIndex.StandUp;
                player.animationFrame = 0;
                return;
            }

            if (player.input[0].x != 0)
            {
                // bodyMode would change when player.input[0].x != body_chunk_0.contactPoint.x // skip this check for now
                player.canWallJump = player.IsClimbingOnBeam() ? 0 : player.input[0].x * -15;

                // when upside down, flip instead of climbing
                if (body_chunk_0.pos.y < body_chunk_1.pos.y)
                {
                    body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, 2f * player.gravity, 0.8f, 1f);
                    body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, 0.0f, 0.8f, 1f);
                    body_chunk_1.vel.x = -player.input[0].x * 5f;
                }
                else
                {
                    float velXGain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction;
                    if (player.slowMovementStun > 0)
                    {
                        velXGain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
                    }
                    // if (player.slugcatStats.name.value == "TheVoid")
                    if (player.input[0].y != 0)
                    {
                        if (player.input[0].y == 1 && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && (body_chunk_1.pos.x < body_chunk_0.pos.x) == (player.input[0].x < 0)) // climb up even when lower body part is hanging in the air
                        {
                            body_chunk_0.pos.y += Mathf.Abs(body_chunk_0.pos.x - body_chunk_1.pos.x);
                            body_chunk_1.pos.x = body_chunk_0.pos.x;
                            body_chunk_1.vel.x = -player.input[0].x * velXGain;
                        }

                        body_chunk_0.vel.y += player.gravity;
                        body_chunk_1.vel.y += player.gravity;

                        // downward momentum when ContactPoint.x != 0 is limited to -player.gravity bc of Update()
                        body_chunk_0.vel.y = Mathf.Lerp(body_chunk_0.vel.y, player.input[0].y * 2.5f, 0.3f);
                        body_chunk_1.vel.y = Mathf.Lerp(body_chunk_1.vel.y, player.input[0].y * 2.5f, 0.3f);
                        ++player.animationFrame;
                    }
                    else if (player.lowerBodyFramesOffGround > 8 && player.input[0].y != -1) // stay in place // don't slide down // when only Option_WallClimb is enabled then this happens even when holding up // don't slide/climb when doing a normal jump off the ground
                    {
                        if (player.grasps[0]?.grabbed is Cicada cicada)
                        {
                            body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, player.gravity - cicada.LiftPlayerPower * 0.5f, 0.3f, 1f);
                        }
                        else
                        {
                            body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, player.gravity, 0.3f, 1f);
                        }
                        body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, player.gravity, 0.3f, 1f);

                        if (!player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && player.input[0].x > 0 == body_chunk_1.pos.x > body_chunk_0.pos.x)
                        {
                            body_chunk_1.vel.x = -player.input[0].x * velXGain;
                        }
                    }
                }
            }

            if (player.slideLoop != null && player.slideLoop.volume > 0.0f)
            {
                player.slideLoop.volume = 0.0f;
            }
            body_chunk_1.vel.y += body_chunk_1.submersion * player.EffectiveRoomGravity;

            if (player.animationFrame <= 20) return;
            player.room?.PlaySound(SoundID.Slugcat_Crawling_Step, player.mainBodyChunk);
            player.animationFrame = 0;
        }


        private static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, SlugcatHand slugcat_hand) // Option_WallClimb
        {
            if (slugcat_hand.owner is not PlayerGraphics player_graphics ||
                player_graphics.owner is not Player player ||
                player.Get_Attached_Fields() is not PlayMod.Player_Attached_Fields attached_fields) 
                return orig(slugcat_hand);


            if (player.bodyMode != Player.BodyModeIndex.WallClimb || player.input[0].y == 0 || player.animation != Player.AnimationIndex.None)
            {
                attached_fields.initialize_hands = true;
                return orig(slugcat_hand);
            }

            if (attached_fields.initialize_hands)
            {
                if (slugcat_hand.limbNumber == 1)
                {
                    attached_fields.initialize_hands = false;
                    player.animationFrame = 0; // not pretty
                }
                return orig(slugcat_hand);
            }

            // make sure to call orig() for compatibility;
            // the wall climb section in orig() changes absoluteHuntPos;
            Vector2 current_absolute_hunt_position = slugcat_hand.absoluteHuntPos;
            orig(slugcat_hand);
            slugcat_hand.absoluteHuntPos = current_absolute_hunt_position;

            if (!(player.animationFrame == 1 && slugcat_hand.limbNumber == 0 || player.animationFrame == 11 && slugcat_hand.limbNumber == 1)) return false;
            slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;
            Vector2 attached_position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);

            // player.input[0].y is not zero;
            if (player.input[0].y > 0)
            {
                slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 30f), -player.flipDirection, 2, false);
                player_graphics.LookAtPoint(slugcat_hand.absoluteHuntPos, 0f);
                player_graphics.objectLooker.timeLookingAtThis = 6;
                return false;
            }

            slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, -10f), -player.flipDirection, 2, false);
            player_graphics.LookAtPoint(slugcat_hand.absoluteHuntPos + new Vector2(0f, -20f), 0f);
            player_graphics.objectLooker.timeLookingAtThis = 6;
            return false;
        }
    }

    public static class PlayMod
    {
      
        public static PlayMod.Player_Attached_Fields Get_Attached_Fields(this Player player)
        {
            PlayMod.Player_Attached_Fields attached_fields;
            PlayMod.all_attached_fields.TryGetValue(player, out attached_fields);
            return attached_fields;
        }

        public static void Add_Attached_Fields(this Player player)
        {
            if(!PlayMod.all_attached_fields.TryGetValue(player, out _))
                all_attached_fields.Add(player,new());


        }
        internal static ConditionalWeakTable<Player, PlayMod.Player_Attached_Fields> all_attached_fields = new ();

        public sealed class Player_Attached_Fields
        {
            public bool initialize_hands = false;
        }
    }

}
