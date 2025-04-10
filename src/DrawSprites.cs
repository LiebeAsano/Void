using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static Room;

namespace VoidTemplate;

internal class DrawSprites
{
	public static void Hook()
	{
		//handles tail and other stuff
		On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
		//make tail cling when climbing
		On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawTail;

		On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
	}
	private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig(self, sLeaser, rCam);
		Player player = self.player;

		//game behaves in a really weird way when you try to touch tail, so we just gonna make a new one and overlay it over the old one
		if (player.KarmaCap == 10 && (player.IsVoid() || player.IsViy()))
		{
			var tail = sLeaser.sprites[2] as TriangleMesh;
			//changing tail sprite element
			tail.Init(FFacetType.Triangle, Futile.atlasManager.GetElementWithName(self.player.Malnourished ? "Void-MalnourishmentTail" : "Void-Tail"), tail.triangles.Length);
			//mapping element to tail
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
			//color to match face color for jolly/arena purposes
			tail.color = sLeaser.sprites[9].color;
        }

		//custom sprite of the mark requires scale to be 1
		sLeaser.sprites[11].scale = 1f;
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

        #region mark
        //mark sprite logic
        if (self.player.abstractCreature.world.game.session is StoryGameSession session
				&& !Utils.DressMySlugcatEnabled)
		{
			FSprite markSprite = sLeaser.sprites[11];
			string spritename = markSprite.element.name;
			int AmountOfPebblesConversations = session.saveState.miscWorldSaveData.SSaiConversationsHad;

			//this only works because game doesn't know yet the sprite element has changed,
			//hence only rerendering it when game requests to rerender it in any other way
			//i.e. changing rooms
			//i dislike it, but oh well
            if ( AmountOfPebblesConversations >= 8)
			{
				string pixel = "VoidR-";
				if (Futile.atlasManager.DoesContainElementWithName(pixel + spritename))
					markSprite.element = Futile.atlasManager.GetElementWithName(pixel + spritename);
			}
			else if (AmountOfPebblesConversations >= 2)
			{
				string pixel = "VoidS-";
				if (Futile.atlasManager.DoesContainElementWithName(pixel + spritename))
					markSprite.element = Futile.atlasManager.GetElementWithName(pixel + spritename);
			}
			else
			{
				string pixel = "Void-";
				if (Futile.atlasManager.DoesContainElementWithName(pixel + spritename))
					markSprite.element = Futile.atlasManager.GetElementWithName(pixel + spritename);
			}
		}
        #endregion

        #region head
        //head sprite logic
        if (self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
		{
			FSprite sprite = sLeaser.sprites[3];
			string headSpriteName = sprite.element.name;
			if(IsTouchingDiagonalCeiling(self.player))
			{
				if (!self.player.input[0].jmp)
				{

					string head = "VoidDCeil-";
					if (Futile.atlasManager.DoesContainElementWithName(head + headSpriteName))
						sprite.element = Futile.atlasManager.GetElementWithName(head + headSpriteName);
				}
				else
				{

					string head = "Void-";
					if (Futile.atlasManager.DoesContainElementWithName(head + headSpriteName))
						sprite.element = Futile.atlasManager.GetElementWithName(head + headSpriteName);

				}
			}
			else if (IsTouchingCeiling(self.player))
			{
				if (!self.player.input[0].jmp)
				{
					string head = "VoidCeil-";
					if (Futile.atlasManager.DoesContainElementWithName(head + headSpriteName))
						sprite.element = Futile.atlasManager.GetElementWithName(head + headSpriteName);
				}
				else
				{
					string head = "Void-";
					if (Futile.atlasManager.DoesContainElementWithName(head + headSpriteName))
						sprite.element = Futile.atlasManager.GetElementWithName(head + headSpriteName);
				}
			}
		}
        #endregion

        #region face
        //face sprite logic
        FSprite faceSprite = sLeaser.sprites[9];
		string faceSpriteName = faceSprite.element.name;
        if (self.player.room is not null
                    && Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM
                        && Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.AUTO
                        && !self.player.room.game.IsArenaSession)
        {
            faceSprite.color = new(1f, 0.86f, 0f);
        }
#warning change later pls
        //Utils.VoidColor = sLeaser.sprites[9].color;

        BodyChunk body_chunk_0 = self.player.bodyChunks[0];
        BodyChunk body_chunk_1 = self.player.bodyChunks[1];

        if (IsTouchingDiagonalCeiling(self.player)
            && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
        {
            if (!self.player.input[0].jmp
                && self.player.bodyMode != Player.BodyModeIndex.ZeroG
                && self.player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
            {

                string face = "VoidDCeil-";
                if (Futile.atlasManager.DoesContainElementWithName(face + faceSpriteName))
                    faceSprite.element = Futile.atlasManager.GetElementWithName(face + faceSpriteName);

            }
            else
            {

                string face = "Void-";
                if (Futile.atlasManager.DoesContainElementWithName(face + faceSpriteName))
                    faceSprite.element = Futile.atlasManager.GetElementWithName(face + faceSpriteName);

            }
        }

        else if (IsTouchingCeiling(self.player)
            && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
        {
            if (!self.player.input[0].jmp
                && self.player.bodyMode != Player.BodyModeIndex.ZeroG
                && self.player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam
                && body_chunk_0.pos.y <= body_chunk_1.pos.y + 5)
            {

                string face = "VoidCeil-";
                if (Futile.atlasManager.DoesContainElementWithName(face + faceSpriteName))
                    faceSprite.element = Futile.atlasManager.GetElementWithName(face + faceSpriteName);

            }
            else
            {

                string face = "Void-";
                if (Futile.atlasManager.DoesContainElementWithName(face + faceSpriteName))
                    faceSprite.element = Futile.atlasManager.GetElementWithName(face + faceSpriteName);

            }
        }
        else
        {
            if (body_chunk_0.pos.y + 10f > body_chunk_1.pos.y
                || self.player.bodyMode == Player.BodyModeIndex.ZeroG
                || self.player.bodyMode == Player.BodyModeIndex.Dead
                || self.player.bodyMode == Player.BodyModeIndex.Stunned
                || self.player.bodyMode == Player.BodyModeIndex.Crawl)
            {

                string face = "Void-";
                if (Futile.atlasManager.DoesContainElementWithName(face + faceSpriteName))
                    faceSprite.element = Futile.atlasManager.GetElementWithName(face + faceSpriteName);

            }
            else
            {

                string face = "VoidDown-";
                if (Futile.atlasManager.DoesContainElementWithName(face + faceSpriteName))
                    faceSprite.element = Futile.atlasManager.GetElementWithName(face + faceSpriteName);

            }
        }

        #endregion

        #region echoTail
		//watcher autosets tail to have a custom watcher shader, which hates color
		if(sLeaser.sprites[2] is TriangleMesh tail
			&& tail.shader != FShader.defaultShader)	tail.shader = FShader.defaultShader;
        #endregion

        foreach (var sprite in sLeaser.sprites)
		{
			string spritename = sprite.element.name;
			if (spritename.StartsWith("PlayerArm")
				|| spritename.StartsWith("OnTopOfTerrainHand")
				|| spritename.StartsWith("Body")
				|| spritename.StartsWith("Hips")
				|| spritename.StartsWith("Legs")
				|| spritename.StartsWith("Head"))
			{
				sprite.color = new(0f, 0f, 0.005f);
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
