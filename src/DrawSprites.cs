using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.Useful;
using static Room;

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

                // Использование IdentifySlope для определения диагонального тайла
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

    const int tailSpriteIndex = 2;
    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (!self.player.IsVoid()) return;
        foreach (var sprite in sLeaser.sprites)
        {
            if (sLeaser.sprites[tailSpriteIndex] is TriangleMesh tail &&
            self.player.abstractCreature.world.game.session is StoryGameSession session &&
            session.saveState.deathPersistentSaveData.karma == 10)
            {
                tail.element = Futile.atlasManager.GetElementWithName(self.player.Malnourished ? "TheVoid-MalnourishmentTail" : "TheVoid-Tail");
                tail.color = new(1f, 0.86f, 0f);
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

                    // Map UV values to the element
                    uv.x = Mathf.Lerp(tail.element.uvBottomLeft.x, tail.element.uvTopRight.x, uv.x);
                    uv.y = Mathf.Lerp(tail.element.uvBottomLeft.y, tail.element.uvTopRight.y, uv.y);

                    tail.UVvertices[i] = uv;
                }
            }
            if (sprite.element.name.StartsWith("Head"))
            {

                if (IsTouchingDiagonalCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!self.player.input[0].jmp)
                    {
                        string head = "TheVoidDCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                    else
                    {
                        string head = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                }
                else if (IsTouchingCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!self.player.input[0].jmp)
                    {
                        string head = "TheVoidCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                    else
                    {
                        string head = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(head + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(head + sprite.element.name);
                    }
                }
            }
            if (sprite.element.name.StartsWith("Face"))
            {

                BodyChunk body_chunk_0 = self.player.bodyChunks[0];
                BodyChunk body_chunk_1 = self.player.bodyChunks[1];

                if (IsTouchingDiagonalCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!self.player.input[0].jmp)
                    {
                        string face = "TheVoidDCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                    else
                    {
                        string face = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                }

                else if (IsTouchingCeiling(self.player) && self.player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                {
                    if (!self.player.input[0].jmp)
                    {
                        string face = "TheVoidCeil-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                    else
                    {
                        string face = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                }

                else
                {
                    if (body_chunk_0.pos.y + 10f > body_chunk_1.pos.y && self.player.bodyMode != Player.BodyModeIndex.ZeroG)
                    {
                        string face = "TheVoid-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                    else
                    {
                        string face = "TheVoidDown-";
                        if (Futile.atlasManager.DoesContainElementWithName(face + sprite.element.name))
                            sprite.element = Futile.atlasManager.GetElementWithName(face + sprite.element.name);
                    }
                }
            }
        }
    }


    private static float timeSinceLastForceUpdate = 0f;
    private static readonly float forceUpdateInterval = 1f / 40f;
    private static void PlayerGraphics_DrawTail(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        timeSinceLastForceUpdate += Time.deltaTime;

        Player player = self.player;

        BodyChunk body_chunk_0 = player.bodyChunks[0];
        BodyChunk body_chunk_1 = player.bodyChunks[1];

        if ((player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
            player.bodyMode == Player.BodyModeIndex.WallClimb &&
            body_chunk_0.pos.y < body_chunk_1.pos.y) &&
            player.bodyMode != Player.BodyModeIndex.CorridorClimb)
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
                        if (body_chunk_0.pos.x > body_chunk_1.pos.x)
                            force = new Vector2(-0.7f, 0.7f);
                        else
                            force = new Vector2(0.7f, 0.7f);
                    }

                    tailSegment.vel += force;
                }

                timeSinceLastForceUpdate = 0f; // сбрасываем счётчик
            }
        }

        else if (player.bodyMode == Player.BodyModeIndex.CorridorClimb &&
            body_chunk_0.pos.y + 10f < body_chunk_1.pos.y)
        {
            if (timeSinceLastForceUpdate >= forceUpdateInterval)
            {
                foreach (TailSegment tailSegment in self.tail)
                {
                    Vector2 force = Vector2.zero;

                    force = new Vector2(0.0f, 1.0f);

                    tailSegment.vel += force;
                }

                timeSinceLastForceUpdate = 0f;
            }
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
            player.bodyMode == Player.BodyModeIndex.WallClimb && body_chunk_0.pos.y < body_chunk_1.pos.y)
        {
            sLeaser.sprites[4].isVisible = false;
        }
    }
}
