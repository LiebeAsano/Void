using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Objects;
using VoidTemplate.OptionInterface;
using VoidTemplate.PlayerMechanics;
using VoidTemplate.PlayerMechanics.Karma11Features;
using VoidTemplate.Useful;
using Watcher;
using static Room;

namespace VoidTemplate;

public static class DrawSprites
{
    public static readonly Color voidColor = new(0f, 0f, 0.005f);

    public static readonly Color voidFluidColor = new(1f, 0.86f, 0f);

    public static readonly Color hunterColor = new(1f, 0.45f, 0.45f);

    public static readonly Color gourmandColor = new(0.94f, 0.76f, 0.59f);

    private static readonly ConditionalWeakTable<PlayerGraphics, PlayerGraphiscExtention> pGExt = new();
    public static PlayerGraphiscExtention GetPlayerGExt(this PlayerGraphics graphics) => pGExt.GetOrCreateValue(graphics);

    public static void Hook()
    {
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;

        On.PlayerGraphics.Update += PlayerGraphics_Update;
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
        if (self?.player == null) return;
            UpdateVoidDeadGlow(self);
    }

    private static FieldInfo _cachedGlowField;

    private class BaseGlow
    {
        public float rad;
        public float alpha;
    }

    private static readonly ConditionalWeakTable<PlayerGraphics, BaseGlow> baseGlow =
        new();

    private static void UpdateVoidDeadGlow(PlayerGraphics self)
    {
        var player = self.player;
        if (player == null || !player.IsVoid()) return;
        if (player.abstractCreature?.GetPlayerState().InDream == true) return;

        LightSource glow = self.lightSource;
        if (glow == null) return;

        int pn = player.playerState?.playerNumber ?? -1;
        if (pn < 0 || SaintKarmaImmunity.deathCounter == null || pn >= SaintKarmaImmunity.deathCounter.Length) return;

        var bg = baseGlow.GetValue(self, _ => new BaseGlow());

        if (!player.dead)
        {
            if (glow.setRad > 0f) bg.rad = (float)glow.setRad;
            else if (glow.rad > 0f) bg.rad = glow.rad;

            if (glow.setAlpha > 0f) bg.alpha = (float)glow.setAlpha;
            else if (glow.alpha > 0f) bg.alpha = glow.alpha;

            return;
        }

        int dc = SaintKarmaImmunity.deathCounter[pn];
        float k = Mathf.Clamp01(dc / 240f);
        float t = Mathf.SmoothStep(1f, 0f, k);

        float baseRad = (bg.rad > 0.01f) ? bg.rad : Mathf.Max((float)glow.setRad, glow.rad);
        float baseAlpha = (bg.alpha > 0.01f) ? bg.alpha : Mathf.Max((float)glow.setAlpha, glow.alpha);

        glow.setRad = baseRad * t;
        glow.setAlpha = baseAlpha * t;

        if (dc >= 240)
        {
            glow.setRad = 0f;
            glow.setAlpha = 0f;

        }
    }

    private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
        Player player = self.player;

        if (player.AreVoidViy() && (player.KarmaCap == 10 || Karma11Update.VoidKarma11))
        {
            var tail = sLeaser.sprites[2] as TriangleMesh;

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

            tail.color = sLeaser.sprites[9].color;
        }

        if (self.player.IsVoid() && !Utils.DressMySlugcatEnabled)
            sLeaser.sprites[11].scale = 1f;
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

    private static readonly float[] timeSinceLastForceUpdate = new float[32];
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

        if (self.player.abstractCreature.world.game.IsVoidWorld())
        {
            if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand
                && self.player.AI != null)
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
                        sprite.color = gourmandColor;
                    if (spritename.StartsWith("Face"))
                        sprite.color = voidColor;
                }
                if (sLeaser.sprites[2] is TriangleMesh tail2)
                {
                    tail2.color = gourmandColor;
                }
            }
        }
        #region drawTail
        if (player.AreVoidViy() && player.bodyMode == BodyModeIndexExtension.CeilCrawl ||
        player.bodyMode == Player.BodyModeIndex.WallClimb)
        {
            sLeaser.sprites[4].isVisible = false;
        }
        #endregion

        if (player.abstractCreature.GetPlayerState().InDream)
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
                    if (SlugStats.illness <= 1800)
                    {
                        sprite.color = hunterColor;
                    }
                    else if (SlugStats.illness <= 3600)
                    {
                        sprite.color = new(1f, 0.5f, 0.5f);
                    }
                    else if (SlugStats.illness <= 5400)
                    {
                        sprite.color = new(1f, 0.53f, 0.53f);
                    }
                    else if (SlugStats.illness <= 7200)
                    {
                        sprite.color = new(1f, 0.56f, 0.56f);
                    }
                    else
                    {
                        sprite.color = new(1f, 0.6f, 0.6f);
                    }

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

                if (SlugStats.illness <= 1800)
                {
                    tail3.color = hunterColor;
                }
                else if (SlugStats.illness <= 3600)
                {
                    tail3.color = new(1f, 0.5f, 0.5f);
                }
                else if (SlugStats.illness <= 5400)
                {
                    tail3.color = new(1f, 0.55f, 0.55f);
                }
                else if (SlugStats.illness <= 7200)
                {
                    tail3.color = new(1f, 0.6f, 0.6f);
                }
                else
                {
                    tail3.color = new(1f, 0.65f, 0.65f);
                }
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
        if (!player.IsVoid() || player.abstractCreature.GetPlayerState().InDream) return;

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
            if (Climbing.IsTouchingCeiling(player))
            {
                if (!player.input[0].jmp)
                {
                    SetVoidHeadSprite("VoidDCeil-");
                }
                else SetVoidHeadSprite("Void-");
            }
            else if (Climbing.IsTouchingCeiling(player))
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
        if (Climbing.IsTouchingDiagonalCeiling(player)
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
        else if (Climbing.IsTouchingCeiling(player)
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
