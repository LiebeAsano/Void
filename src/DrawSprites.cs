using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoidTemplate.Misc;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static Room;
using static VoidTemplate.SaveManager;

namespace VoidTemplate;

internal class DrawSprites
{
	public static void Hook()
	{
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
		On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawTail;
	}

    private static bool IsTouchingCeiling(Player player)
	{
		if (player.room is not null)
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
		return false;
	}

	private static bool IsTouchingDiagonalCeiling(Player player)
	{
		if (player.room is not null)
		{
			BodyChunk body_chunk_0 = player.bodyChunks[0];
			BodyChunk body_chunk_1 = player.bodyChunks[1];

			Vector2[] directions = {
			new(0, 1)
			};

			foreach (var direction in directions)
			{
				Vector2 checkPosition_0 = body_chunk_0.pos + direction * (body_chunk_0.rad + 10);
				Vector2 checkPosition_1 = body_chunk_1.pos + direction * (body_chunk_1.rad + 10);

				IntVector2 tileDiagonal_0 = player.room.GetTilePosition(checkPosition_0);
				IntVector2 tileDiagonal_1 = player.room.GetTilePosition(checkPosition_1);

				SlopeDirection slopeDirection_0 = player.room.IdentifySlope(tileDiagonal_0);
				SlopeDirection slopeDirection_1 = player.room.IdentifySlope(tileDiagonal_1);

				bool isDiagonal = (slopeDirection_0 == SlopeDirection.UpLeft ||
						   slopeDirection_0 == SlopeDirection.UpRight ||
						   slopeDirection_0 == SlopeDirection.DownLeft ||
						   slopeDirection_0 == SlopeDirection.DownRight ||
						   slopeDirection_1 == SlopeDirection.UpLeft ||
						   slopeDirection_1 == SlopeDirection.UpRight ||
						   slopeDirection_1 == SlopeDirection.DownLeft ||
						   slopeDirection_1 == SlopeDirection.DownRight);

				if (isDiagonal)
				{
					return true;
				}
			}
		}
		return false;
	}
	private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (!self.player.IsVoid()) return;
		foreach (var sprite in sLeaser.sprites)
		{
            if (self.player.abstractCreature.world.game.session is StoryGameSession session)
			{
				if (session.saveState.deathPersistentSaveData.karmaCap == 10)
				{
					if (sLeaser.sprites[2] is TriangleMesh tail)
					{
                        tail.element = Futile.atlasManager.GetElementWithName(self.player.Malnourished ? "Void-MalnourishmentTail" : "Void-Tail");
                        tail.color = sLeaser.sprites[9].color;
                        /*
                        Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
                        Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                        Vector2 vector4 = (vector2 * 3f + vector) / 4f;
						Array.Resize(ref self.tail, self.tail.Length + 1);
                        float d2 = 0f;
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 vector5 = Vector2.Lerp(self.tail[i].lastPos, self.tail[i].pos, timeStacker);
                            Vector2 normalized = (vector5 - vector4).normalized;
                            Vector2 a = Custom.PerpendicularVector(normalized);
                            float d3 = Vector2.Distance(vector5, vector4) / 5f;
                            if (i == 0)
                            {
                                d3 = 0f;
                            }

                            (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4, vector4 - a * d2 * 1.5f + normalized * d3 - camPos);
                            (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 1, vector4 + a * d2 * 1.5f + normalized * d3 - camPos);
                            if (i < 3)
                            {
                                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - a * self.tail[i].StretchedRad * 1.5f - normalized * d3 - camPos);
                                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 3, vector5 + a * self.tail[i].StretchedRad * 1.5f - normalized * d3 - camPos);
                            }
                            else
                            {
                                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - camPos);
                            }
                            vector4 = vector5;
                        }
						*/
                        for (var i = tail.vertices.Length - 1; i >= 0; i--)
						{
							var perc = i / 2 / (float)(tail.vertices.Length / 2);

							Vector2 uv;
							if (i % 2 == 0)
								uv = new Vector2(perc, 0f);
							else if (i < tail.vertices.Length - 1)
								uv = new Vector2(perc, 1f);
							else
								uv = new Vector2(1f, 0f);

							uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
							uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

							tail.UVvertices[i] = uv;
						}

					}
				}
				if (sprite.element.name.StartsWith("pixel") && !ModManager.ActiveMods.Any(mod => mod.id == "dressmyslugcat"))
				{

                    sLeaser.sprites[11].scale = 1f;

					if (session.saveState.miscWorldSaveData.SSaiConversationsHad >= 8)
					{
						string pixel = "VoidR-";
						if (Futile.atlasManager.DoesContainElementWithName(pixel + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(pixel + sprite.element.name);
					}
					else if (session.saveState.miscWorldSaveData.SSaiConversationsHad >= 2)
					{
						string pixel = "VoidS-";
						if (Futile.atlasManager.DoesContainElementWithName(pixel + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(pixel + sprite.element.name);
					}
					else
					{
						string pixel = "Void-";
						if (Futile.atlasManager.DoesContainElementWithName(pixel + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(pixel + sprite.element.name);
					}
				}
			}
            if (Karma11Update.VoidKarma11)
            {
                if (sLeaser.sprites[2] is TriangleMesh tail)
                {
                    tail.element = Futile.atlasManager.GetElementWithName(self.player.Malnourished ? "Void-MalnourishmentTail" : "Void-Tail");
                    tail.color = sLeaser.sprites[9].color;

                    /*
                    Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
                    Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                    Vector2 vector4 = (vector2 * 3f + vector) / 4f;
                    Array.Resize(ref self.tail, self.tail.Length + 1);
                    float d2 = 0f;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 vector5 = Vector2.Lerp(self.tail[i].lastPos, self.tail[i].pos, timeStacker);
                        Vector2 normalized = (vector5 - vector4).normalized;
                        Vector2 a = Custom.PerpendicularVector(normalized);
                        float d3 = Vector2.Distance(vector5, vector4) / 5f;
                        if (i == 0)
                        {
                            d3 = 0f;
                        }

                        (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4, vector4 - a * d2 * 1.5f + normalized * d3 - camPos);
                        (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 1, vector4 + a * d2 * 1.5f + normalized * d3 - camPos);
                        if (i < 3)
                        {
                            (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - a * self.tail[i].StretchedRad * 1.5f - normalized * d3 - camPos);
                            (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 3, vector5 + a * self.tail[i].StretchedRad * 1.5f - normalized * d3 - camPos);
                        }
                        else
                        {
                            (sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, vector5 - camPos);
                        }
                        vector4 = vector5;
                    }
                    */
                    for (var i = tail.vertices.Length - 1; i >= 0; i--)
                    {
                        var perc = i / 2 / (float)(tail.vertices.Length / 2);

                        Vector2 uv;
                        if (i % 2 == 0)
                            uv = new Vector2(perc, 0f);
                        else if (i < tail.vertices.Length - 1)
                            uv = new Vector2(perc, 1f);
                        else
                            uv = new Vector2(1f, 0f);

                        uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
                        uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

                        tail.UVvertices[i] = uv;  
                    }

                }
            }
            if (sprite.element.name.StartsWith("Head"))
			{

                sprite.color = new(0f, 0f, 0.005f);

                if (IsTouchingDiagonalCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl && self.player.bodyMode != Player.BodyModeIndex.ZeroG && self.player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
				{
					if (!self.player.input[0].jmp)
					{

						string head = "VoidDCeil-";
						if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
						//sprite.scale = 1.5f;
					}
					else
					{

						string head = "Void-";
						if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);

					}
				}
				else if (IsTouchingCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl && self.player.bodyMode != Player.BodyModeIndex.ZeroG && self.player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
				{
					if (!self.player.input[0].jmp)
					{
						string head = "VoidCeil-";
						if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
					}
					else
					{

						string head = "Void-";
						if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);

					}
				}
			}
			if (sprite.element.name.StartsWith("Face"))
			{
				if (self.player.room != null)
				{
					if (ModManager.CoopAvailable)
					{
                        if ((Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM 
							&& Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.AUTO
                            || Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.AUTO
                            && self.player.abstractCreature == self.player.room.game.Players[0])
                            && !self.player.room.game.IsArenaSession)
                        {
                            sprite.color = new(1f, 0.86f, 0f);
                            Utils.VoidColors[0] = new(1f, 0.86f, 0f);
                        }
                    }
					else if (!self.player.room.game.IsArenaSession)
					{
                        sprite.color = new(1f, 0.86f, 0f);
                    }
                }

				int number = self.player.playerState.playerNumber;
				if (sprite.color != null)
				{
                    Utils.VoidColors[number] = sprite.color;
                }

                BodyChunk body_chunk_0 = self.player.bodyChunks[0];
				BodyChunk body_chunk_1 = self.player.bodyChunks[1];

				if (IsTouchingDiagonalCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
				{
					if (!self.player.input[0].jmp && self.player.bodyMode != Player.BodyModeIndex.ZeroG && self.player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
					{

						string face = "VoidDCeil-";
						if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);

					}
					else
					{

						string face = "Void-";
						if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);

					}
				}

				else if (IsTouchingCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
				{
					if (!self.player.input[0].jmp && self.player.bodyMode != Player.BodyModeIndex.ZeroG
						&& self.player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam
						&& body_chunk_0.pos.y <= body_chunk_1.pos.y + 5)
					{

						string face = "VoidCeil-";
						if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);

					}
					else
					{

						string face = "Void-";
						if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
							sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);

					}
				}
				else
				{
					if (body_chunk_0.pos.y + 10f > body_chunk_1.pos.y || self.player.bodyMode == Player.BodyModeIndex.ZeroG ||
						self.player.bodyMode == Player.BodyModeIndex.Dead || self.player.bodyMode == Player.BodyModeIndex.Stunned ||
						self.player.bodyMode == Player.BodyModeIndex.Crawl)
					{
						if (Climbing.switchMode[self.player.playerState.playerNumber] && OptionAccessors.ComplexControl)
                        {
							string face = "VoidA-";
							if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
								sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
						}
						else
						{
                            string face = "Void-";
                            if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                                sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                        }

					}
					else
					{
						if (Climbing.switchMode[self.player.playerState.playerNumber] && OptionAccessors.ComplexControl)
						{
							string face = "VoidADown-";
							if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
								sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
						}
						else
						{
                            string face = "VoidDown-";
                            if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                                sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                        }
					}
				}
			}
            if (sprite.element.name.StartsWith("PlayerArm"))
			{
                sprite.color = new(0f, 0f, 0.005f);
            }
            if (sprite.element.name.StartsWith("OnTopOfTerrainHand"))
			{
                sprite.color = new(0f, 0f, 0.005f);
            }
            if (sprite.element.name.StartsWith("Body"))
			{
                sprite.color = new(0f, 0f, 0.005f);
            }
            if (sprite.element.name.StartsWith("Hips"))
			{
                sprite.color = new(0f, 0f, 0.005f);
            }
            if (sprite.element.name.StartsWith("Legs"))
			{
                sprite.color = new(0f, 0f, 0.005f);
            }
            if (sLeaser.sprites[2] is TriangleMesh tail2 && !Karma11Update.VoidKarma11)
			{
                tail2.color = new(0f, 0f, 0.005f);
            }
        }
	}


	private static float timeSinceLastForceUpdate = 0f;
	private static readonly float forceUpdateInterval = 1f / 40f;
	private static void PlayerGraphics_DrawTail(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{

		Player player = self.player;

		BodyChunk body_chunk_0 = player.bodyChunks[0];
		BodyChunk body_chunk_1 = player.bodyChunks[1];

		if (player.IsVoid() || player.IsViy())
		{

			timeSinceLastForceUpdate += Time.deltaTime;

			if ((player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
				player.bodyMode == Player.BodyModeIndex.WallClimb &&
				body_chunk_0.pos.y < body_chunk_1.pos.y) &&
				player.bodyMode != Player.BodyModeIndex.CorridorClimb &&
                player.bodyMode != Player.BodyModeIndex.Crawl)
			{
				if (timeSinceLastForceUpdate >= forceUpdateInterval)
				{
					foreach (TailSegment tailSegment in self.tail)
					{
						Vector2 force = Vector2.zero;

						if (player.bodyMode == Player.BodyModeIndex.WallClimb && player.input[0].x < 0)
						{
							force = new Vector2(-0.7f, 1.4f);
						}
						else if (player.bodyMode == Player.BodyModeIndex.WallClimb && player.input[0].x > 0)
						{
							force = new Vector2(0.7f, 1.4f);
						}
						else if (!player.input[0].jmp)
						{
							if (body_chunk_0.pos.x > body_chunk_1.pos.x)
								force = new Vector2(-0.7f, 0.7f);
							else
								force = new Vector2(0.7f, 0.7f);
						}

						tailSegment.vel += force;
					}

					timeSinceLastForceUpdate = 0f;
				}
			}
		}

		orig(self, sLeaser, rCam, timeStacker, camPos);

		if ((player.IsVoid() || player.IsViy()) && player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
			player.bodyMode == Player.BodyModeIndex.WallClimb && body_chunk_0.pos.y < body_chunk_1.pos.y)
		{
			sLeaser.sprites[4].isVisible = false;
		}
	}
}
