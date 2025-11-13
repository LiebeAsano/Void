using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using static Room;

namespace VoidTemplate;

public static class DrawSprites
{
    public static readonly Color voidColor = new(0f, 0f, 0.005f);

    public static readonly Color voidFluidColor = new(1f, 0.86f, 0f);

    public static readonly Color hunterColor = new(1f, 0.45f, 0.45f);

    private static ConditionalWeakTable<PlayerGraphics, PlayerGraphiscExtention> pGExt = new();
    public static PlayerGraphiscExtention GetPlayerGExt(this PlayerGraphics graphics) => pGExt.GetOrCreateValue(graphics);

    public static void Hook()
    {
        //handles tail and other stuff
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;

        On.PlayerGraphics.Update += PlayerGraphics_Update;

        //On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
    }

    private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        // Если это не Охотник, вызываем оригинальный метод
        if (self.player?.SlugCatClass != SlugcatStats.Name.Red)
        {
            orig(self, sLeaser, rCam, palette);
            return;
        }

        try
        {
            Color hunterBaseColor = new Color(1f, 0.45f, 0.45f);
            Color color = hunterBaseColor;
            Color color2 = new Color(color.r, color.g, color.b);

            if (self.malnourished > 0f)
            {
                float num = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                color2 = Color.Lerp(color2, Color.gray, 0.4f * num);
            }

            if (self.player.injectedPoison > 0f)
            {
                color2 = Color.Lerp(Color.Lerp(color2, self.player.injectedPoisonColor,
                    Mathf.Clamp01(self.player.injectedPoison) * 0.3f),
                    new Color(0.5f, 0.5f, 0.5f),
                    self.player.injectedPoison * 0.1f);
            }

            color2 = self.HypothermiaColorBlend(color2);
            self.currentAppliedHypothermia = self.player.Hypothermia;

            if (ModManager.MMF && (self.owner as Player).AI == null)
            {
                RainWorld.PlayerObjectBodyColors[self.player.playerState.playerNumber] = color2;
            }

            if (self.gills != null)
            {
                Color effectCol = new Color(0.87451f, 0.17647f, 0.91765f);

                if (!rCam.room.game.setupValues.arenaDefaultColors && !ModManager.CoopAvailable)
                {
                    switch (self.player.playerState.playerNumber)
                    {
                        case 0:
                            if (rCam.room.game.IsArenaSession && rCam.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType != DLCSharedEnums.GameTypeID.Challenge)
                            {
                                effectCol = new Color(0.25f, 0.65f, 0.82f);
                            }
                            break;
                        case 1:
                            effectCol = new Color(0.31f, 0.73f, 0.26f);
                            break;
                        case 2:
                            effectCol = new Color(0.6f, 0.16f, 0.6f);
                            break;
                        case 3:
                            effectCol = new Color(0.96f, 0.75f, 0.95f);
                            break;
                    }
                }

                self.gills.SetGillColors(color2, effectCol);
                self.gills.ApplyPalette(sLeaser, rCam, palette);
            }

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i != 9)
                {
                    sLeaser.sprites[i].color = color2;
                }
            }

            sLeaser.sprites[11].color = Color.Lerp(color, Color.white, 0.3f);
            sLeaser.sprites[10].color = color;

            if (self.weaverGraphics != null)
            {
                self.weaverGraphics.ApplyPalette(sLeaser, rCam, palette, color2);
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка в ApplyPalette для Охотника: {e}");
            orig(self, sLeaser, rCam, palette);
        }
    }

    private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);
        if (self.player.IsVoid() && (Karma11Update.VoidKarma11 || self.player.KarmaCap == 10 && self.player.dead) && self.GetPlayerGExt().toEcxoTail < 1)
        {
            self.GetPlayerGExt().toEcxoTail += 0.005f;
            if (!self.player.abstractCreature.Room.world.game.IsVoidStoryCampaign())
            {
                self.GetPlayerGExt().toEcxoTail = 1f;
            }
        }
    }

    private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        Player player = self.player;

        //game behaves in a really weird way when you try to touch tail, so we just gonna make a new one and overlay it over the old one
        if (player.AreVoidViy() && (player.KarmaCap == 10 || Karma11Update.VoidKarma11))
        {
            var tail = sLeaser.sprites[2] as TriangleMesh;            
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

        if (self.player.IsVoid() && !Utils.DressMySlugcatEnabled)
            sLeaser.sprites[11].scale = 1f;
    }

    private static bool IsTouchingCeiling(this Player player)
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

    private static bool IsTouchingDiagonalCeiling(this Player player)
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

    private static string GetVoidMarkSpriteName(StoryGameSession session, string baseSpriteName)
    {
        if (session.saveState.GetVoidMarkV3())
        {
            return "VoidR-" + baseSpriteName.Split('-').Last();
        }
        else if (session.saveState.GetVoidMarkV2())
        {
            return "VoidS-" + baseSpriteName.Split('-').Last();
        }
        else
        {
            return "Void-" + baseSpriteName.Split('-').Last();
        }
    }

    private static float[] timeSinceLastForceUpdate = new float[32];
    private static readonly float forceUpdateInterval = 1f / 40f;

    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        string originalMarkSpriteName = sLeaser.sprites[11].element.name;

        Player player = self.player;
        BodyChunk playerBodyChunk0 = player.bodyChunks[0];
        BodyChunk playerBodyChunk1 = player.bodyChunks[1];
        #region drawTail
        //make tail cling when climbing
        if (player.AreVoidViy())
        {
            timeSinceLastForceUpdate[player.playerState.playerNumber] += Time.deltaTime;

            if ((player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
                player.bodyMode == Player.BodyModeIndex.WallClimb &&
                playerBodyChunk0.pos.y < playerBodyChunk1.pos.y) &&
                player.bodyMode != Player.BodyModeIndex.CorridorClimb &&
                player.bodyMode != Player.BodyModeIndex.Crawl)
            {
                if (timeSinceLastForceUpdate[player.playerState.playerNumber] >= forceUpdateInterval)
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
                            if (playerBodyChunk0.pos.x > playerBodyChunk1.pos.x)
                                force = new Vector2(-0.7f, 0.7f);
                            else
                                force = new Vector2(0.7f, 0.7f);
                        }

                        tailSegment.vel += force;
                    }

                    timeSinceLastForceUpdate[player.playerState.playerNumber] = 0f;
                }
            }
        }
        #endregion

        orig(self, sLeaser, rCam, timeStacker, camPos);

        #region drawTail
        if (player.AreVoidViy() && player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
            player.bodyMode == Player.BodyModeIndex.WallClimb)
        {
            sLeaser.sprites[4].isVisible = false;
        }
        #endregion

        if (VoidDreamScript.IsVoidDream)
        {
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
                    sprite.color = hunterColor;
                }
                if (spritename.StartsWith("Face"))
                        sprite.color = voidColor;
            }
            if (sLeaser.sprites[2] is TriangleMesh tail3)
            {
                if (tail3.shader != FShader.defaultShader)
                {
                    tail3.shader = FShader.defaultShader;
                }
                    tail3.color = hunterColor;
            }
        }

        if (player.IsViy())
        { 
            Utils.ViyColors[player.playerState.playerNumber] = sLeaser.sprites[9].color;
            if (sLeaser.sprites[2] is TriangleMesh viyTail
            && viyTail.shader != FShader.defaultShader)
            {
                viyTail.shader = FShader.defaultShader;
            }
            if (sLeaser.sprites[2] is TriangleMesh viyTail2)
            {
                viyTail2.color = Utils.ViyColors[player.playerState.playerNumber];
            }
        }
        if (!player.IsVoid()) return;

        string currentMarkSpriteName = sLeaser.sprites[11].element.name;

        if (currentMarkSpriteName == originalMarkSpriteName ||
            currentMarkSpriteName.StartsWith("Void"))
        {
            if (player.abstractCreature.world.game.session is StoryGameSession session
                && !Utils.DressMySlugcatEnabled)
            {
                SetVoidSprite(sLeaser.sprites[11], GetVoidMarkSpriteName(session, currentMarkSpriteName), "");
            }
        }

        #region head
        if (player.bodyMode == BodyModeIndexExtension.CeilCrawl)
        {
            FSprite headSprite = sLeaser.sprites[3];
            string headSpriteName = headSprite.element.name;
            if (player.IsTouchingDiagonalCeiling())
            {
                if (!player.input[0].jmp)
                {
                    SetVoidHeadSprite("VoidDCeil-");
                }
                else SetVoidHeadSprite("Void-");
            }
            else if (player.IsTouchingCeiling())
            {
                if (!player.input[0].jmp)
                {
                    SetVoidHeadSprite("VoidCeil-");
                }
                else SetVoidHeadSprite("Void-");
            }
            void SetVoidHeadSprite(string spriteName) => SetVoidSprite(headSprite, spriteName, headSpriteName);
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

        Utils.VoidColors[player.playerState.playerNumber] = faceSprite.color;
        if (player.IsTouchingDiagonalCeiling()
            && player.bodyMode == BodyModeIndexExtension.CeilCrawl)
        {
            if (!player.input[0].jmp
                && player.bodyMode != Player.BodyModeIndex.ZeroG
                && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam)
            {
                SetVoidFaceSprite("VoidDCeil-");
            }
            else SetVoidFaceSprite("Void-");
        }
        else if (player.IsTouchingCeiling()
            && player.bodyMode == BodyModeIndexExtension.CeilCrawl)
        {
            if (!player.input[0].jmp
                && player.bodyMode != Player.BodyModeIndex.ZeroG
                && player.bodyMode != Player.BodyModeIndex.ClimbingOnBeam
                && playerBodyChunk0.pos.y <= playerBodyChunk1.pos.y + 5)
            {
                SetVoidFaceSprite("VoidCeil-");
            }
            else SetVoidFaceSprite("Void-");
        }
        else
        {
            if (playerBodyChunk0.pos.y + 10f > playerBodyChunk1.pos.y
                || player.bodyMode == Player.BodyModeIndex.ZeroG
                || player.bodyMode == Player.BodyModeIndex.Dead
                || player.bodyMode == Player.BodyModeIndex.Stunned
                || player.bodyMode == Player.BodyModeIndex.Crawl)
            {
                if (!OptionAccessors.ComplexControl || OptionAccessors.ComplexControl && !Climbing.switchMode[player.playerState.playerNumber])
                {
                    SetVoidFaceSprite("Void-");
                }
                else SetVoidFaceSprite("VoidA-");

            }
            else
            {
                if (!OptionAccessors.ComplexControl || OptionAccessors.ComplexControl && !Climbing.switchMode[self.player.playerState.playerNumber])
                {
                    SetVoidFaceSprite("VoidDown-");
                }
                else SetVoidFaceSprite("VoidADown-");
            }
        }
        void SetVoidFaceSprite(string spriteName) => SetVoidSprite(faceSprite, spriteName, faceSpriteName);
        
        #endregion
        
        #region echoTail
        if (sLeaser.sprites[2] is TriangleMesh tail)
        {
            //watcher autosets tail to have a custom watcher shader, which hates color
            if (tail.shader != FShader.defaultShader)
            {
                tail.shader = FShader.defaultShader;
            }

            if (player.KarmaCap != 10 && !Karma11Update.VoidKarma11)
            {
                tail.color = new(0f, 0f, 0.005f);
            }
            else if (self.GetPlayerGExt().toEcxoTail < 0.11f)
            {
                if (tail.element.name != "Futile_White")
                {
                    tail.Init(FFacetType.Triangle, Futile.atlasManager.GetElementWithName("Futile_White"), tail.triangles.Length);
                }
                tail.color = new(0f, 0f, 0.005f);
            }
            else
            {
                if (tail.element.name != "Void-Tail")
                {
                    tail.Init(FFacetType.Triangle, Futile.atlasManager.GetElementWithName("Void-Tail"), tail.triangles.Length);
                }
                tail.color = Color.Lerp(new(0f, 0f, 0.005f), Utils.VoidColors[player.playerState.playerNumber], self.GetPlayerGExt().toEcxoTail);
            }
        }

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
                if (!self.player.abstractCreature.GetPlayerState().InDream)
                    sprite.color = voidColor;
            }
        }

        static void SetVoidSprite(FSprite toSprite, string spriteName, string origSprite)
        {
            string sprite = spriteName + origSprite;
            if (Futile.atlasManager.DoesContainElementWithName(sprite))
                toSprite.element = Futile.atlasManager.GetElementWithName(sprite);
        }
    }

    public class PlayerGraphiscExtention
    {
        public float toEcxoTail;
    }
}
