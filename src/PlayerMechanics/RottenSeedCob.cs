using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics;

public static class RottenSeedCob
{
    public static void Hook()
    {
        On.Player.Update += Player_Update;
        On.SeedCob.Update += SeedCob_Update;
        On.SeedCob.ApplyPalette += SeedCob_ApplyPalette;
    }

    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (self.IsVoid() && self.eatExternalFoodSourceCounter > 0)
        {
            self.eatExternalFoodSourceCounter--;
            if (self.eatExternalFoodSourceCounter < 1)
            {
                if (self.externalFoodSourceRotten)
                {
                    self.AddFood(1); ;
                }
                else
                {
                    self.AddFood(1);
                }
                self.dontEatExternalFoodSourceCounter = 45;
                self.handOnExternalFoodSource = null;
                self.room.PlaySound(SoundID.Slugcat_Bite_Fly, self.mainBodyChunk);
                self.externalFoodSourceRotten = false;
            }
        }
        orig(self, eu);
    }

    public static bool RotGarbage;

    private static void SeedCob_Update(On.SeedCob.orig_Update orig, SeedCob self, bool eu)
    {
        RotGarbage = self.abstractPhysicalObject.Room.name == "GW_LWA01";
        orig(self, eu);
    }

    private static void SeedCob_ApplyPalette(On.SeedCob.orig_ApplyPalette orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);
        if (RotGarbage)
        {
            UnityEngine.Color pixel = palette.texture.GetPixel(0, 5);
            UnityEngine.Color color = new(0.43f, 0.37f, 0.20f);
            if (self.AbstractCob.rotted)
            {
                self.yellowColor = new(0.65f, 0.61f, 0.34f);
            }
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < (sLeaser.sprites[self.ShellSprite(j)] as TriangleMesh).verticeColors.Length; k++)
                {
                    float f = 1f - (float)k / (float)((sLeaser.sprites[self.ShellSprite(j)] as TriangleMesh).verticeColors.Length - 1);
                    (sLeaser.sprites[self.ShellSprite(j)] as TriangleMesh).verticeColors[k] = UnityEngine.Color.Lerp(palette.blackColor, color, Mathf.Pow(f, 2.5f) * 0.4f);
                }
            }
            sLeaser.sprites[self.CobSprite].color = self.yellowColor;
            UnityEngine.Color color2 = self.yellowColor + new UnityEngine.Color(0.3f, 0.3f, 0.3f) * Mathf.Lerp(1f, 0.15f, rCam.PaletteDarkness());
            if (self.AbstractCob.dead)
            {
                color2 = UnityEngine.Color.Lerp(self.yellowColor, pixel, 0.75f);
            }
            for (int l = 0; l < self.seedPositions.Length; l++)
            {
                sLeaser.sprites[self.SeedSprite(l, 0)].color = self.yellowColor;
                sLeaser.sprites[self.SeedSprite(l, 1)].color = color2;
                sLeaser.sprites[self.SeedSprite(l, 2)].color = UnityEngine.Color.Lerp(color, palette.blackColor, self.AbstractCob.dead ? 0.6f : 0.3f);
                if (self.AbstractCob.rotted)
                {
                    sLeaser.sprites[self.SeedSprite(l, 0)].color = UnityEngine.Color.Lerp(sLeaser.sprites[self.SeedSprite(l, 0)].color, palette.blackColor, 0.7f);
                    sLeaser.sprites[self.SeedSprite(l, 1)].color = UnityEngine.Color.Lerp(sLeaser.sprites[self.SeedSprite(l, 1)].color, palette.blackColor, 0.5f);
                    sLeaser.sprites[self.SeedSprite(l, 2)].color = UnityEngine.Color.Lerp(sLeaser.sprites[self.SeedSprite(l, 2)].color, palette.blackColor, 0.9f);
                }
            }
            for (int m = 0; m < self.leaves.GetLength(0); m++)
            {
                sLeaser.sprites[self.LeafSprite(m)].color = palette.blackColor;
            }
        }
    }
}
