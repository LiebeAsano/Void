using System;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using static Room;
using static VoidTemplate.Useful.Utils;
using VoidTemplate.Oracles;

namespace VoidTemplate
{
    static class PlayerHooks
    {

        public static void Hook()
        {
            On.Player.CanIPickThisUp += Player_CanIPickThisUp;
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
            On.Player.ctor += Player_Ctor;
            On.Player.Update += NoForceSleep;
            On.Player.CanBeSwallowed += Player_CanBeSwallowed;
            On.Player.Grabability += Player_Grabability;

            On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;

            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is PoleMimic || obj is TentaclePlant)
                return Player.ObjectGrabability.CantGrab;
            return orig(self, obj);
        }

        private static bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
        {
            if (self.IsVoid())
            {
                return testObj is not Creature && testObj is not Spear && testObj is not VultureMask || orig(self, testObj);
            }
            return orig(self, testObj);
        }
        private static void NoForceSleep(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.IsVoid() && self.KarmaCap != 10)   
                self.forceSleepCounter = 0;
        }

        private static bool KarmaCap_Check(Player self)
        {
            return self.IsVoid() && self.KarmaCap > 3;
        }

        private static void Player_Ctor(On.Player.orig_ctor orig, Player player, AbstractCreature abstract_creature, World world)
        {
            orig(player, abstract_creature, world);
            if (world.game.session is StoryGameSession session && session.characterStats.name == VoidEnums.SlugcatID.TheVoid)
                player.slugcatStats.foodToHibernate = session.characterStats.foodToHibernate;
            if (player.slugcatStats.name != VoidEnums.SlugcatID.TheVoid) return;
            player.Add_Attached_Fields();


        }


        private static bool IsTouchingDiagonalCeiling(Player player)
        {
            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];

            Vector2[] directions = [
            new Vector2(0, 1)
            ];

            foreach (var direction in directions)
            {
                Vector2 checkPosition_0 = body_chunk_0.pos + direction * (body_chunk_0.rad + 10);
                Vector2 checkPosition_1 = body_chunk_1.pos + direction * (body_chunk_1.rad + 10);

                IntVector2 tileDiagonal_0 = player.room.GetTilePosition(checkPosition_0);
                IntVector2 tileDiagonal_1 = player.room.GetTilePosition(checkPosition_1);

                // Использование IdentifySlope для определения диагонального тайла
                SlopeDirection slopeDirection_0 = player.room.IdentifySlope(tileDiagonal_0);
                SlopeDirection slopeDirection_1 = player.room.IdentifySlope(tileDiagonal_1);

                bool isDiagonal = (slopeDirection_0 == SlopeDirection.DownLeft ||
                           slopeDirection_0 == SlopeDirection.DownRight ||  
                           slopeDirection_1 == SlopeDirection.DownLeft ||
                           slopeDirection_1 == SlopeDirection.DownRight);

                if (isDiagonal)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTouchingCeiling(Player player)
        {
            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];

            Vector2 upperPosition_0 = body_chunk_0.pos + new Vector2(0, body_chunk_0.rad + 5);
            Vector2 upperPosition_1 = body_chunk_1.pos + new Vector2(0, body_chunk_1.rad + 5);

            IntVector2 tileAbove_0 = player.room.GetTilePosition(upperPosition_0);
            IntVector2 tileAbove_1 = player.room.GetTilePosition(upperPosition_1);

            bool isSolid_0 = player.room.GetTile(tileAbove_0).Solid;
            bool isSolid_1 = player.room.GetTile(tileAbove_1).Solid;

            return isSolid_0 || isSolid_1;
        }

        private static readonly float CeilCrawlDuration = 0.3f;

        private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player player)
        {
            if (player.slugcatStats.name != VoidEnums.SlugcatID.TheVoid)
            {
                orig(player);
                return;
            }

            var state = player.GetPlayerState();

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

            if (player.bodyMode == Player.BodyModeIndex.WallClimb && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam && player.bodyMode != Player.BodyModeIndex.Swimming)
            {
                UpdateBodyMode_WallClimb(player);
            }
            else if (IsTouchingCeiling(player) && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.Swimming && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam && player.bodyMode != Player.BodyModeIndex.Stand && KarmaCap_Check(player))
            {
                player.bodyMode = BodyModeIndexExtension.CeilCrawl;
                UpdateBodyMode_CeilCrawl(player);
                state.IsCeilCrawling = true;
                state.CeilCrawlStartTime = Time.realtimeSinceStartup - 0.15f;
            }
            else if (IsTouchingDiagonalCeiling(player) && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.Swimming && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam && player.bodyMode != Player.BodyModeIndex.Stand && KarmaCap_Check(player))
            {
                player.bodyMode = BodyModeIndexExtension.CeilCrawl;
                UpdateBodyMode_CeilCrawl(player);
                state.IsCeilCrawling = true;
                state.CeilCrawlStartTime = Time.realtimeSinceStartup;
            }

            if (state.IsCeilCrawling)
            {
                if (player.input[0].y > 0)
                {
                    float elapsedTime = Time.realtimeSinceStartup - state.CeilCrawlStartTime;

                    if (elapsedTime < CeilCrawlDuration)
                    {
                        player.bodyMode = BodyModeIndexExtension.CeilCrawl;
                        UpdateBodyMode_CeilCrawl(player);
                    }
                    else
                    {
                        state.IsCeilCrawling = false;
                    }
                }
                else
                {
                    state.IsCeilCrawling = false;
                }
            }
            orig(player);
        }

        private static void UpdateBodyMode_CeilCrawl(Player player)
        {
            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];
            player.canJump = 1;
            player.standing = true;

            float climbSpeed = 1f;

            if (!player.input[0].jmp)
                if (body_chunk_0.pos.x > body_chunk_1.pos.x && player.input[0].x < 0)
                    climbSpeed = -0.25f;
                else if (body_chunk_0.pos.x < body_chunk_1.pos.x && player.input[0].x > 0)
                        climbSpeed = -0.25f;

            // Горизонтальное движение при ползке по потолку
            if (player.input[0].x != 0)
            {
                body_chunk_0.vel.x = player.input[0].x * climbSpeed;
                if (!player.input[0].jmp)
                {
                    body_chunk_1.vel.x = player.input[0].x * climbSpeed;
                }
            }
            else
            {
                body_chunk_0.vel.x = 0;
                if (!player.input[0].jmp)
                {
                    body_chunk_1.vel.x = 0;
                }
            }

            float ceilingForce = player.gravity * 6f;

            if (player.input[0].y > 0)
            {
                if (!player.input[0].jmp)
                {
                    body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, ceilingForce, 0.3f, 1f);
                }

                body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, ceilingForce, 0.3f, 1f);

                if (player.input[0].jmp && player.input[0].x != 0)
                {
                    float jumpForceX = -3.4f * climbSpeed * player.input[0].x;
                    body_chunk_1.vel.x = Custom.LerpAndTick(body_chunk_1.vel.x, jumpForceX, 0.3f, 1f);
                }

                if (player.lowerBodyFramesOffGround > 8 && !player.IsClimbingOnBeam())
                {
                    if (player.grasps[0]?.grabbed is Cicada cicada)
                    {
                        body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, ceilingForce - cicada.LiftPlayerPower * 0.5f, 0.3f, 1f);
                    }
                    else
                    {
                        body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, ceilingForce, 0.3f, 1f);
                    }
                }
                if (player.slideLoop != null && player.slideLoop.volume > 0.0f)
                {
                    player.slideLoop.volume = 0.0f;
                }

                if (player.animationFrame <= 20) return;
                player.room?.PlaySound(SoundID.Slugcat_Crawling_Step, player.mainBodyChunk);
                player.animationFrame = 0;
            }
        }

        public static bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, global::SlugcatHand slugcat_hand)
        {
            if (slugcat_hand.owner is not PlayerGraphics player_graphics ||
                player_graphics.owner is not Player player ||
                player.Get_Attached_Fields() is not PlayMod.Player_Attached_Fields attached_fields)
            {
                return orig(slugcat_hand);
            }

            if (player.animation != Player.AnimationIndex.None || player.input[0].y == 0 || (player.bodyMode != Player.BodyModeIndex.WallClimb && player.bodyMode != BodyModeIndexExtension.CeilCrawl))
            {
                attached_fields.initialize_hands = true;
                return orig(slugcat_hand);
            }

            if (attached_fields.initialize_hands)
            {
                if (slugcat_hand.limbNumber == 1)
                {
                    attached_fields.initialize_hands = false;
                    player.animationFrame = 0;
                }
                return orig(slugcat_hand);
            }

            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];

            // Логика для лазания по потолку
            if (player.bodyMode == BodyModeIndexExtension.CeilCrawl)
            {

                if (body_chunk_0.pos.x > body_chunk_1.pos.x)
                {
                    player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(100f, 0.0f), 0f);
                    player_graphics.objectLooker.timeLookingAtThis = 6;
                }
                else
                {
                    player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(-100f, 0.0f), 0f);
                    player_graphics.objectLooker.timeLookingAtThis = 6;
                }
                player.animationFrame++;

                slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;

                orig(slugcat_hand);

                if (!Custom.DistLess(slugcat_hand.pos, slugcat_hand.connection.pos, 20f)) 
                {
                    Vector2 vector = Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
                    Vector2 gripDirectionOffset = new(player.flipDirection * 10f, 5f);
                    slugcat_hand.FindGrip(player.room, slugcat_hand.connection.pos, slugcat_hand.connection.pos, 100f,
                        slugcat_hand.connection.pos + (vector + new Vector2(player.input[0].x, player.input[0].y).normalized * 1.5f).normalized * 20f + gripDirectionOffset, 2, 2, false);
                }
                return false;
            }

            if (player.bodyMode == Player.BodyModeIndex.WallClimb)
            {
                Vector2 current_absolute_hunt_position = slugcat_hand.absoluteHuntPos;
                orig(slugcat_hand);
                slugcat_hand.absoluteHuntPos = current_absolute_hunt_position;

                if (!(player.animationFrame == 1 && slugcat_hand.limbNumber == 0 || player.animationFrame == 11 && slugcat_hand.limbNumber == 1)) return false;
                slugcat_hand.mode = Limb.Mode.HuntAbsolutePosition;
                Vector2 attached_position = slugcat_hand.connection.pos + new Vector2(player.flipDirection * 10f, 0.0f);

                if (player.input[0].y > 0)
                {
                    player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(0.0f, 100f), 0f);
                    player_graphics.objectLooker.timeLookingAtThis = 6;
                }
                else
                {
                    player_graphics.LookAtPoint(player.mainBodyChunk.pos + new Vector2(0.0f, -100f), 0f);
                    player_graphics.objectLooker.timeLookingAtThis = 6;
                }
                player.animationFrame++;

                if (player.input[0].y > 0 && player.bodyMode == Player.BodyModeIndex.WallClimb)
                {
                    if (body_chunk_0.pos.y > body_chunk_1.pos.y)
                    {
                        slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 30f), -player.flipDirection, 2, false);
                        return false;
                    }   
                    else
                    {
                        slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, 0f), -player.flipDirection, 2, false);
                        return false;
                    }
                }

                slugcat_hand.FindGrip(player.room, attached_position, attached_position, 100f, attached_position + new Vector2(0.0f, -10f), -player.flipDirection, 2, false);
                return false;
            }

            return orig(slugcat_hand);
        }

        private static float timeSinceLastForceUpdate = 0f;
        private static readonly float forceUpdateInterval = 1f / 40f;

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            timeSinceLastForceUpdate += Time.deltaTime;

            Player player = self.player;

            BodyChunk body_chunk_0 = player.bodyChunks[0];
            BodyChunk body_chunk_1 = player.bodyChunks[1];

            if ((player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
                player.bodyMode == Player.BodyModeIndex.WallClimb && body_chunk_0.pos.y < body_chunk_1.pos.y) && player.bodyMode != Player.BodyModeIndex.CorridorClimb)
            {
                if (timeSinceLastForceUpdate >= forceUpdateInterval)
                {
                    foreach (TailSegment tailSegment in self.tail)
                    {
                        Vector2 force = Vector2.zero;

                        if (player.bodyMode == Player.BodyModeIndex.WallClimb && player.input[0].x < 0)
                        {
                            force = new Vector2(-0.7f, 0.7f);
                        }
                        else if (player.bodyMode == Player.BodyModeIndex.WallClimb && player.input[0].x > 0)
                        {
                            force = new Vector2(0.7f, 0.7f);
                        }
                        else if (!player.input[0].jmp)
                        {
                            force = new Vector2(0f, 0.7f);
                        }

                        tailSegment.vel += force;
                    }

                    timeSinceLastForceUpdate = 0f; // сбрасываем счётчик
                }
            }

            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
                player.bodyMode == Player.BodyModeIndex.WallClimb && body_chunk_0.pos.y < body_chunk_1.pos.y)
            {
                sLeaser.sprites[4].isVisible = false;
            }
        }

        public static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            if (self.IsVoid())
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

            if (player.input[0].x != 0)
            {
                player.canWallJump = player.IsClimbingOnBeam() ? 0 : player.input[0].x * -15;

                
                
                    float velXGain = 2.4f * Mathf.Lerp(1f, 1.2f, player.Adrenaline) * player.surfaceFriction;
                    if (player.slowMovementStun > 0)
                    {
                        velXGain *= 0.4f + 0.6f * Mathf.InverseLerp(10f, 0.0f, player.slowMovementStun);
                    }

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

                        if (body_chunk_0.pos.y > body_chunk_1.pos.y)
                        {
                            body_chunk_0.vel.y = Mathf.Lerp(body_chunk_0.vel.y, player.input[0].y * 2.5f, 0.3f);
                            body_chunk_1.vel.y = Mathf.Lerp(body_chunk_1.vel.y, player.input[0].y * 2.5f, 0.3f);
                        }
                        else
                        {
                            body_chunk_0.vel.y = Mathf.Lerp(body_chunk_0.vel.y, player.input[0].y * 1.5f, 0.3f);
                            body_chunk_1.vel.y = Mathf.Lerp(body_chunk_1.vel.y, player.input[0].y * 1.5f, 0.3f);
                    }
                        ++player.animationFrame;
                    }
                    else if (player.lowerBodyFramesOffGround > 8 && player.input[0].y != -1)
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

                        if (!player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && player.input[0].x > 0 == body_chunk_1.vel.x > body_chunk_0.pos.x)
                        {
                            body_chunk_1.vel.x = -player.input[0].x * velXGain;
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


    }

    public static class PlayMod
    {
        public static Player_Attached_Fields Get_Attached_Fields(this Player player)
        {
            Player_Attached_Fields attached_fields;
            all_attached_fields.TryGetValue(player, out attached_fields);
            return attached_fields;
        }

        public static void Add_Attached_Fields(this Player player)
        {
            if (!all_attached_fields.TryGetValue(player, out _))
                all_attached_fields.Add(player, new());
        }

        internal static ConditionalWeakTable<Player, PlayMod.Player_Attached_Fields> all_attached_fields = new();

        public sealed class Player_Attached_Fields
        {
            public bool initialize_hands = false;
        }
    }

    public static class BodyModeIndexExtension
    {
        public static readonly Player.BodyModeIndex CeilCrawl;

        static BodyModeIndexExtension()
        {
            CeilCrawl = new Player.BodyModeIndex("CeilCrawl", true);
        }
    }

    public static class PlayerExtensions
    {
        private static readonly Dictionary<Player, PlayerState> PlayerStates = [];

        public static PlayerState GetPlayerState(this Player player)
        {
            if (!PlayerStates.TryGetValue(player, out PlayerState state))
            {
                state = new PlayerState();
                PlayerStates[player] = state;
            }

            return state;
        }
    }

    public class PlayerState
    {
        public bool IsCeilCrawling { get; set; } = false;
        public float CeilCrawlStartTime { get; set; } = 0f;
    }

    public class PlayerRoomChecker
    {
        public static bool IsRoomIDSS_AI(Player player)
        {
            if (player.room != null && player.room.abstractRoom != null)
            {
                return player.room.abstractRoom.name == "SS_AI";
            }

            return false;
        }
    }
}