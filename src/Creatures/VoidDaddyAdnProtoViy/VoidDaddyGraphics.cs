using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.Creatures.VoidDaddyAdnProtoViy
{
    public class VoidDaddyGraphics
    {
        public static void Hook()
        {
            On.DaddyGraphics.RotBodyColor += DaddyGraphics_RotBodyColor;
            On.DaddyGraphics.DaddyDangleTube.DrawSprite += DaddyDangleTube_DrawSprite;
            On.DaddyGraphics.HunterDummy.InitiateSprites += HunterDummy_InitiateSprites;
            On.DaddyGraphics.HunterDummy.ApplyPalette += HunterDummy_ApplyPalette;
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
                return self.blackColor;
            }
            return orig(self);
        }
    }
}
