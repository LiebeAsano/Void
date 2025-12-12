using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace VoidTemplate.Creatures.VoidDaddyAndProtoViy

{
    public class VoidDaddyGraphics
    {
        public static void Hook()
        {
            On.DaddyGraphics.RotBodyColor += DaddyGraphics_RotBodyColor;
            On.DaddyGraphics.DaddyDangleTube.DrawSprite += DaddyDangleTube_DrawSprite;
            On.DaddyGraphics.HunterDummy.InitiateSprites += HunterDummy_InitiateSprites;
            On.DaddyGraphics.HunterDummy.ApplyPalette += HunterDummy_ApplyPalette;
            On.DaddyGraphics.DaddyLegGraphic.ctor += DaddyLegGraphic_ctor;
            On.DaddyGraphics.DaddyLegGraphic.DrawSprite += DaddyLegGraphic_DrawSprite;
            On.DaddyGraphics.DaddyTubeGraphic.ApplyPalette += DaddyTubeGraphic_ApplyPalette;
        }

        private static void DaddyTubeGraphic_ApplyPalette(On.DaddyGraphics.DaddyTubeGraphic.orig_ApplyPalette orig, DaddyGraphics.DaddyTubeGraphic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (self.owner.owner is DaddyLongLegs daddy && daddy.GetDaddyExt().IsProtoViy && sLeaser.sprites[self.firstSprite] is TriangleMesh mesh)
            {
                for (int i = 0; i < mesh.verticeColors.Length; i++)
                {
                    mesh.verticeColors[i] = DrawSprites.voidColor;
                }
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        private static void DaddyLegGraphic_DrawSprite(On.DaddyGraphics.DaddyLegGraphic.orig_DrawSprite orig, DaddyGraphics.DaddyLegGraphic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (self.owner.owner is DaddyLongLegs daddy && daddy.GetDaddyExt().IsProtoViy)
            {
                var triangleMesh = sLeaser.sprites[self.firstSprite] as TriangleMesh;
                Vector2 vector = Vector2.Lerp(self.segments[0].lastPos, self.segments[0].pos, timeStacker);
                vector += Custom.DirVec(Vector2.Lerp(self.segments[1].lastPos, self.segments[1].pos, timeStacker), vector) * 1f;

                float baseWidth = 3.4f;
                float midWidth = baseWidth * 0.5f;
                float tipWidth = 0.5f;

                for (int i = 0; i < self.segments.Length; i++)
                {
                    Vector2 vector2 = Vector2.Lerp(self.segments[i].lastPos, self.segments[i].pos, timeStacker);
                    Vector2 normalized = (vector - vector2).normalized;
                    Vector2 a = Custom.PerpendicularVector(normalized);

                    float progress = (float)i / (self.segments.Length - 1);

                    float currentWidth;
                    if (progress < 0.85f)
                    {
                        currentWidth = baseWidth - (baseWidth - midWidth) * (progress / 0.85f);
                    }
                    else
                    {
                        float coneProgress = (progress - 0.85f) / 0.15f;
                        currentWidth = midWidth - (midWidth - tipWidth) * coneProgress;
                    }

                    triangleMesh.MoveVertice(i * 4, vector - a * currentWidth - camPos);
                    triangleMesh.MoveVertice(i * 4 + 1, vector + a * currentWidth - camPos);
                    triangleMesh.MoveVertice(i * 4 + 2, vector2 - a * currentWidth - camPos);
                    triangleMesh.MoveVertice(i * 4 + 3, vector2 + a * currentWidth - camPos);
                    vector = vector2;
                }
                return;
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }


        private static void DaddyLegGraphic_ctor(On.DaddyGraphics.DaddyLegGraphic.orig_ctor orig, DaddyGraphics.DaddyLegGraphic self, GraphicsModule owner, DaddyGraphics.IHaveRotGraphics rotOwner, int index, int firstSprite)
        {
            orig(self, owner, rotOwner, index, firstSprite);
            if (self.owner.owner is DaddyLongLegs daddy && daddy.GetDaddyExt().IsProtoViy)
            {
                self.bumps = [];
                self.sprites = 1;
                return;
            }
        }

        private static void DaddyDangleTube_DrawSprite(On.DaddyGraphics.DaddyDangleTube.orig_DrawSprite orig, DaddyGraphics.DaddyDangleTube self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.owner.owner is DaddyLongLegs daddy && daddy.GetDaddyExt().IsProtoViy)
            {
                for (int i = self.firstSprite; i < self.firstSprite + self.sprites; i++)
                {
                    sLeaser.sprites[i].isVisible = false;
                }
            }
        }

        private static void HunterDummy_ApplyPalette(On.DaddyGraphics.HunterDummy.orig_ApplyPalette orig, DaddyGraphics.HunterDummy self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (self.owner.daddy.GetDaddyExt().HaveType)
            {
                for (int i = 0; i < self.numberOfSprites - 1; i++)
                {
                    sLeaser.sprites[self.startSprite + i].color = DrawSprites.voidColor;
                }
                sLeaser.sprites[self.startSprite + 5].color = self.owner.daddy.GetDaddyExt().daddyColor;
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        private static void HunterDummy_InitiateSprites(On.DaddyGraphics.HunterDummy.orig_InitiateSprites orig, DaddyGraphics.HunterDummy self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.owner.daddy.GetDaddyExt().HaveType)
            {
                ReplaceFaceSprite(ref sLeaser.sprites[self.startSprite + 5]);

                void ReplaceFaceSprite(ref FSprite face)
                {
                    face.RemoveFromContainer();
                    face = new FSprite("Viy-FaceA0", true);
                    rCam.ReturnFContainer("Midground").AddChild(face);
                }
            }
        }

        private static Color DaddyGraphics_RotBodyColor(On.DaddyGraphics.orig_RotBodyColor orig, DaddyGraphics self)
        {
            if (self.daddy.GetDaddyExt().HaveType)
            {
                return DrawSprites.voidColor;
            }
            return orig(self);
        }
    }
}
