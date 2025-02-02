using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.ViyMechanics;

internal static class ViyTail
{
    public static void Hook()
    {
        //On.TailSegment.ctor += TailSegment_ctor;
        On.PlayerGraphics.ctor += PlayerGraphics_ctor;
        //On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawTail;
    }

    private static void TailSegment_ctor(On.TailSegment.orig_ctor orig, TailSegment self, GraphicsModule ow, float rd, float cnRd, TailSegment cnSeg, float sfFric, float aFric, float affectPrevious, bool pullInPreviousPosition)
    {
        self.rad = rd;
        self.connectionRad = cnRd;
        self.surfaceFric = sfFric;
        self.airFriction = aFric;
        self.connectedSegment = cnSeg;
        self.affectPrevious = affectPrevious;
        self.pullInPreviousPosition = pullInPreviousPosition;
        self.connectedPoint = null;
        self.Reset(self.owner.owner.bodyChunks[1].pos);
    }

    private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        self.InitCachedSpriteNames();
        self.player = (ow as Player);
        self.malnourished = ((self.player.Malnourished || self.player.redsIllness != null) ? 1f : 0f);
        List<BodyPart> list = new List<BodyPart>();
        self.airborneCounter = 0f;
        self.tail = new TailSegment[4];
        if (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
        {
            float num = 0.85f + 0.3f * Mathf.Lerp(self.player.npcStats.Wideness, 0.5f, self.player.playerState.isPup ? 0.5f : 0f);
            float num2 = (0.75f + 0.5f * self.player.npcStats.Size) * (self.player.playerState.isPup ? 0.5f : 1f);
            self.tail[0] = new TailSegment(self, 6f * num, 4f * num2, null, 0.85f, 1f, 1f, true);
            self.tail[1] = new TailSegment(self, 4f * num, 7f * num2, self.tail[0], 0.85f, 1f, 0.5f, true);
            self.tail[2] = new TailSegment(self, 2.5f * num, 7f * num2, self.tail[1], 0.85f, 1f, 0.5f, true);
            self.tail[3] = new TailSegment(self, 1f * num, 7f * num2, self.tail[2], 0.85f, 1f, 0.5f, true);
        }
        else if (ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
        {
            if (self.player.playerState.isPup)
            {
                self.tail[0] = new TailSegment(self, 8f, 2f, null, 0.85f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 6f, 3.5f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 4f, 3.5f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 2f, 3.5f, self.tail[2], 0.85f, 1f, 0.5f, true);
            }
            else
            {
                self.tail[0] = new TailSegment(self, 8f, 4f, null, 0.85f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 6f, 7f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 4f, 7f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 2f, 7f, self.tail[2], 0.85f, 1f, 0.5f, true);
            }
        }
        else if ((ModManager.MSC || ModManager.CoopAvailable) && self.player.playerState.isPup)
        {
            self.tail[0] = new TailSegment(self, 6f, 2f, null, 0.85f, 1f, 1f, true);
            self.tail[1] = new TailSegment(self, 4f, 3.5f, self.tail[0], 0.85f, 1f, 0.5f, true);
            self.tail[2] = new TailSegment(self, 2.5f, 3.5f, self.tail[1], 0.85f, 1f, 0.5f, true);
            self.tail[3] = new TailSegment(self, 1f, 3.5f, self.tail[2], 0.85f, 1f, 0.5f, true);
        }
        else
        {
            self.tail[0] = new TailSegment(self, 6f, 4f, null, 0.85f, 1f, 1f, true);
            self.tail[1] = new TailSegment(self, 4f, 7f, self.tail[0], 0.85f, 1f, 0.5f, true);
            self.tail[2] = new TailSegment(self, 2.5f, 7f, self.tail[1], 0.85f, 1f, 0.5f, true);
            self.tail[3] = new TailSegment(self, 1f, 7f, self.tail[2], 0.85f, 1f, 0.5f, true);
        }
        if (self.player.bool1)
        {
            self.tail[0] = new TailSegment(self, 7f, 1f, null, 0.85f, 1f, 1f, true);
            self.tail[1] = new TailSegment(self, 2f, 2f, self.tail[0], 0.85f, 1f, 0.5f, true);
            self.tail[2] = new TailSegment(self, 0.93500006f, 2f, self.tail[1], 0.85f, 1f, 0.5f, true);
            self.tail[3] = new TailSegment(self, 0.85f, 2f, self.tail[2], 0.85f, 1f, 0.5f, true);
        }
        for (int i = 0; i < self.tail.Length; i++)
        {
            list.Add(self.tail[i]);
        }
        self.hands = new SlugcatHand[2];
        for (int j = 0; j < 2; j++)
        {
            self.hands[j] = new SlugcatHand(self, self.owner.bodyChunks[0], j, 3f, 0.8f, 1f);
            list.Add(self.hands[j]);
        }
        self.head = new GenericBodyPart(self, 4f, 0.8f, 0.99f, self.owner.bodyChunks[0]);
        list.Add(self.head);
        self.legs = new GenericBodyPart(self, 1f, 0.8f, 0.99f, self.owner.bodyChunks[1]);
        list.Add(self.legs);
        self.legsDirection = new Vector2(0f, -1f);
        if (ModManager.MSC)
        {
            if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
            {
                self.gills = new PlayerGraphics.AxolotlGills(self, 12);
            }
            if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                self.tailSpecks = new PlayerGraphics.TailSpeckles(self, 12);
                self.bodyPearl = new PlayerGraphics.CosmeticPearl(self, 12 + self.tailSpecks.numberOfSprites);
            }
            self.numGodPips = 12;
            if (self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                self.ropeSegments = new PlayerGraphics.RopeSegment[20];
                for (int k = 0; k < self.ropeSegments.Length; k++)
                {
                    self.ropeSegments[k] = new PlayerGraphics.RopeSegment(k, self);
                }
                self.tentacles = new PlayerGraphics.Tentacle[4];
                for (int l = 0; l < self.tentacles.Length; l++)
                {
                    self.tentacles[l] = new PlayerGraphics.Tentacle(self, 15 + self.numGodPips + l * 2, 100f, new Vector2?(self.owner.bodyChunks[0].pos));
                }
            }
            self.gown = new PlayerGraphics.Gown(self);
        }
        self.drawPositions = new Vector2[self.owner.bodyChunks.Length, 2];
        self.disbalanceAmount = 0f;
        self.balanceCounter = 0f;
        for (int m = 0; m < self.owner.bodyChunks.Length; m++)
        {
            self.drawPositions[m, 0] = self.owner.bodyChunks[m].pos;
            self.drawPositions[m, 1] = self.owner.bodyChunks[m].lastPos;
        }
        self.lookDirection = new Vector2(0f, 0f);
        self.objectLooker = new PlayerGraphics.PlayerObjectLooker(self);
        if (self.player.AI == null && (self.player.slugcatStats.name == SlugcatStats.Name.Red || (ModManager.MSC && self.player.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)) && self.player.abstractCreature.world.game.IsStorySession)
        {
            self.markBaseAlpha = Mathf.Pow(Mathf.InverseLerp(4f, 14f, (float)self.player.abstractCreature.world.game.GetStorySession.saveState.cycleNumber), 3.5f);
        }
        self.bodyParts = list.ToArray();
        Custom.Log(new string[]
        {
            "Creating player graphics!",
            self.player.playerState.playerNumber.ToString()
        });
        if (self.player.playerState.playerNumber == 0)
        {
            PlayerGraphics.PopulateJollyColorArray(self.player.slugcatStats.name);
        }

    }
    private static void PlayerGraphics_DrawTail(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (!self.player.IsViy()) return;
        if (sLeaser.sprites[2] is TriangleMesh tail)
        {
            tail.element = Futile.atlasManager.GetElementWithName(self.player.Malnourished ? "Void-MalnourishmentTail" : "Void-Tail");
            tail.color = new(1f, 0.86f, 0f);
            
            Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
            Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            Vector2 vector4 = (vector2 * 3f + vector) / 4f;
            Array.Resize(ref self.tail, self.tail.Length + 1);
            float d2 = 0f;
            for (int i = 0; i < 6; i++)
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
                if (i < 5)
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
}
