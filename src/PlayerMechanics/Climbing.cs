using Newtonsoft.Json.Linq;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.OptionInterface;
using VoidTemplate.Useful;
using static Room;
using static VoidTemplate.SaveManager;

namespace VoidTemplate.PlayerMechanics;

internal static class Climbing
{
	public static void Hook()
	{
		On.Player.WallJump += Player_UpdateWallJump;
		On.Player.UpdateBodyMode += Player_UpdateBodyMode;
    }

	private static float currentTimeWall = 0f;

	private static void Player_UpdateWallJump(On.Player.orig_WallJump orig, Player self, int direction)
	{
		if (self.slugcatStats.name == VoidEnums.SlugcatID.Void || self.slugcatStats.name == VoidEnums.SlugcatID.Viy)
		{

			BodyChunk body_chunk_0 = self.bodyChunks[0];
			BodyChunk body_chunk_1 = self.bodyChunks[1];

			if (self.bodyChunks[0].ContactPoint.x != 0 && self.input[0].y > 0 && self.input[0].jmp)
			{

				self.bodyChunks[0].vel.y = 10f;
				self.bodyChunks[1].vel.y = 10f;

				self.bodyChunks[0].vel.x = 8f * -self.input[0].x;
				self.bodyChunks[1].vel.x = 8f * -self.input[0].x;

				self.room.PlaySound(SoundID.Slugcat_Wall_Jump, self.mainBodyChunk, false, 1f, 1f);
				self.standing = true;
				self.jumpBoost = 0;
				self.jumpStun = 0;

				self.canWallJump = 0;

				return;
			}
			else if (self.input[0].y < 0 && self.input[0].jmp && body_chunk_0.pos.y > body_chunk_1.pos.y)
            {
                self.standing = true;
                self.jumpBoost = 0;
                self.jumpStun = 0;

                self.canWallJump = 0;

                return;
            }
        }
		orig(self, direction);
	}

	private static bool KarmaCap_Check(Player self)
	{
		return self.IsVoid() && (self.KarmaCap > 3 || ExternalSaveData.VoidKarma11) || self.IsViy();
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

	private static readonly float CeilCrawlDuration = 0.2f;

	private static int flipTimer = -1;
	private const int ticksToFlip = 10;

	public static bool gamepadController = false;
	public static int gamepadTimer = 0;
	public static int gamepadTimer2 = 0;

    private static readonly ConditionalWeakTable<Player, StrongBox<int>> rightLeft = new();

    private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player player)
	{
		if (player.slugcatStats.name != VoidEnums.SlugcatID.Void && player.slugcatStats.name != VoidEnums.SlugcatID.Viy)
		{
			orig(player);
			return;
		}

		var state = player.abstractCreature.GetPlayerState();

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

		BodyChunk body_chunk_0 = player.bodyChunks[0];
		BodyChunk body_chunk_1 = player.bodyChunks[1];

		if (!rightLeft.TryGetValue(player, out var rightLeftStrongBox))
			rightLeft.Add(player, new(0));

		if (flipTimer > -1)
		{
			if (player.input[0].x < 0)
                rightLeftStrongBox.Value = 1;
			else
                rightLeftStrongBox.Value = -1;

			player.bodyMode = Player.BodyModeIndex.ZeroG;
			body_chunk_0.pos = body_chunk_1.pos + Custom.DegToVec(((float)flipTimer) / ((float)ticksToFlip) * 180 * rightLeftStrongBox.Value) * 17;
			flipTimer++;
			if (flipTimer == ticksToFlip)
			{
				flipTimer = -1;
			}
		}

		if (OptionAccessors.GamepadController)
		{
			if (gamepadController)
				gamepadTimer++;

			if (!gamepadController)
				gamepadTimer2++;

			if (gamepadTimer2 >= 30 && (IsTouchingDiagonalCeiling(player) || IsTouchingCeiling(player)) && KarmaCap_Check(player) && player.input[0].jmp && player.input[0].pckp)
			{
				gamepadController = true;
				gamepadTimer2 = 0;
			}

			if (gamepadTimer >= 30 && player.input[0].jmp && player.input[0].pckp)
			{
				gamepadController = false;
				gamepadTimer = 0;
			}
		}
		if (player.bodyMode == Player.BodyModeIndex.WallClimb)
		{
			UpdateBodyMode_WallClimb(player);
			player.noGrabCounter = 5;
		}
		else if (IsTouchingCeiling(player) && KarmaCap_Check(player) && (player.input[0].y > 0 || gamepadController) &&
				((player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam && player.bodyMode != Player.BodyModeIndex.Swimming && player.bodyMode != Player.BodyModeIndex.Stand && player.bodyMode != Player.BodyModeIndex.ZeroG && player.bodyMode != Player.BodyModeIndex.Crawl) ||
				(player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam && player.input[0].jmp)))
		{
			player.bodyMode = BodyModeIndexExtension.CeilCrawl;
			UpdateBodyMode_CeilCrawl(player, state);
			state.IsCeilCrawling = true;
			state.CeilCrawlStartTime = Time.realtimeSinceStartup - 0.05f;
		}
		else if (IsTouchingDiagonalCeiling(player) && KarmaCap_Check(player) && (player.input[0].y > 0 || gamepadController) &&
				((player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam && player.bodyMode != Player.BodyModeIndex.Swimming && player.bodyMode != Player.BodyModeIndex.Stand && player.bodyMode != Player.BodyModeIndex.ZeroG && player.bodyMode != Player.BodyModeIndex.Crawl) ||
			(player.bodyMode == Player.BodyModeIndex.ClimbingOnBeam && player.input[0].jmp)))
		{
			player.bodyMode = BodyModeIndexExtension.CeilCrawl;
			UpdateBodyMode_CeilCrawl(player, state);
			state.IsCeilCrawling = true;
			state.CeilCrawlStartTime = Time.realtimeSinceStartup;
		}

		player.bodyChunks[1].collideWithTerrain = true;

		if (state.IsCeilCrawling)
		{
			if (player.input[0].y > 0)
			{
				float elapsedTime = Time.realtimeSinceStartup - state.CeilCrawlStartTime;

				if (elapsedTime < CeilCrawlDuration)
				{
					player.bodyMode = BodyModeIndexExtension.CeilCrawl;
					UpdateBodyMode_CeilCrawl(player, state);
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

	private static void TryApplyWallClimbOverride(Player player)
	{
		BodyChunk body_chunk_0 = player.bodyChunks[0];
		BodyChunk body_chunk_1 = player.bodyChunks[1];

		if (player.bodyMode == BodyModeIndexExtension.CeilCrawl && body_chunk_1.pos.y > body_chunk_0.pos.y + 10)
		{
			player.bodyChunks[1].collideWithTerrain = false;
		}
		else
		{
			player.bodyChunks[1].collideWithTerrain = true;
		}
	}

	private static void UpdateBodyMode_CeilCrawl(Player player, PlayerState state)
	{
		BodyChunk body_chunk_0 = player.bodyChunks[0];
		BodyChunk body_chunk_1 = player.bodyChunks[1];
		player.canJump = 1;
		player.standing = true;
		float climbSpeed = 1f + 0.05f * player.KarmaCap;
		if (player.KarmaCap == 10 || ExternalSaveData.VoidKarma11 || player.IsViy())
		{
			climbSpeed = 1.5f;
        }
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

		TryApplyWallClimbOverride(player);

		if (player.input[0].y > 0 || gamepadController)
		{
			if (!player.input[0].jmp)
			{
				if (player.bodyChunks[1].collideWithTerrain)
					body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, ceilingForce, 0.3f, 1f);
				else
					body_chunk_1.vel.y = Custom.LerpAndTick(body_chunk_1.vel.y, -player.gravity * 3, 0.5f, 1f);

			}

			body_chunk_0.vel.y = Custom.LerpAndTick(body_chunk_0.vel.y, ceilingForce, 0.3f, 1f);
			float minusone = 0.05f * player.KarmaCap;
            float minustwo = 0.16f * player.KarmaCap;
			if (player.KarmaCap == 10 || ExternalSaveData.VoidKarma11 || player.IsViy())
			{
				minusone = 0.5f;
				minustwo = 1.6f;

            }
            if (player.input[0].jmp && player.input[0].x != 0)
			{
				float jumpForceX;
				if (gamepadController && player.input[0].y <= 0)
					jumpForceX = (-8f + minustwo) * climbSpeed * player.input[0].x;
				else
					jumpForceX = (-3.4f + minusone) * climbSpeed * player.input[0].x;
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
			player.animationFrame = 0;
		}
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

		currentTimeWall++;

		if (currentTimeWall > 9)
		{
			if (player.input[0].y < 0 && player.input[0].jmp && body_chunk_0.pos.y > body_chunk_1.pos.y && flipTimer == -1)
			{
				flipTimer = 0;
				currentTimeWall = 0;
			}
		}

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
				if (player.input[0].y == 1 && !player.IsTileSolid(bChunk: 1, player.input[0].x, 0) && (body_chunk_1.pos.x < body_chunk_0.pos.x) == (player.input[0].x < 0))
				{
					body_chunk_0.pos.y += Mathf.Abs(body_chunk_0.pos.x - body_chunk_1.pos.x);
					body_chunk_1.pos.x = body_chunk_0.pos.x;
					body_chunk_1.vel.x = -player.input[0].x * velXGain;
				}

				body_chunk_0.vel.y += player.gravity;
				body_chunk_1.vel.y += player.gravity;

				float bonus = 1f + (0.025f * player.KarmaCap);

				if (player.KarmaCap == 10 || player.IsViy())
					bonus = 1.25f;

				if (body_chunk_0.pos.y > body_chunk_1.pos.y)
				{
					body_chunk_0.vel.y = Mathf.Lerp(body_chunk_0.vel.y, player.input[0].y * 2.5f * bonus, 0.3f);
					body_chunk_1.vel.y = Mathf.Lerp(body_chunk_1.vel.y, player.input[0].y * 2.5f * bonus, 0.3f);
				}
				else
				{
					body_chunk_0.vel.y = Mathf.Lerp(body_chunk_0.vel.y, -player.input[0].y * 1.5f, 0.1f);
					body_chunk_1.vel.y = Mathf.Lerp(body_chunk_1.vel.y, -player.input[0].y * 1.5f, 0.1f);
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
	private static readonly ConditionalWeakTable<AbstractCreature, PlayerState> PlayerStates = new();

	public static PlayerState GetPlayerState(this AbstractCreature player)
	{
		if (!PlayerStates.TryGetValue(player, out PlayerState state))
		{
			state = new PlayerState();
			PlayerStates.Add(player, state);
		}

		return state;
	}
}

public class PlayerState
{
	public bool IsCeilCrawling { get; set; } = false;
	public bool IsWallCrawling { get; set; } = true;
	public float CeilCrawlStartTime { get; set; } = 0f;
}

/*public class PlayerRoomChecker
{
	public static bool IsRoomIDSS_AI(Player player)
	{
		if (player.room != null && player.room.abstractRoom != null)
		{
			return player.room.abstractRoom.name == "SS_AI";
		}

		return false;
	}
}*/

