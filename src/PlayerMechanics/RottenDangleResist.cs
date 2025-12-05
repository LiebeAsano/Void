using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics;

public static class RottenDangleResist
{
    public static void Hook()
    {
        On.Player.ObjectEaten += Player_ObjectEaten;
        On.DangleFruit.Update += DangleFruit_Update;
        On.DangleFruit.ApplyPalette += DangleFruit_ApplyPalette;
    }

    private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
    {
        if (self.slugcatStats.name == VoidEnums.SlugcatID.Void)
        {
            if (self.graphicsModule != null)
            {
                (self.graphicsModule as PlayerGraphics).LookAtNothing();
            }
            if (edible is DangleFruit && (edible as DangleFruit).AbstrConsumable.rotted)
            {
                self.AddQuarterFood();
                self.AddQuarterFood();
                return;
            }
        }
        orig(self, edible);
    }

    public static bool RotGarbage;

    private static void DangleFruit_Update(On.DangleFruit.orig_Update orig, DangleFruit self, bool eu)
    {
        RotGarbage = self.abstractPhysicalObject.Room.name == "GW_LWA06";
        orig(self, eu);
    }

    private static void DangleFruit_ApplyPalette(On.DangleFruit.orig_ApplyPalette orig, DangleFruit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        if (RotGarbage)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i % 2 == 0)
                {
                    sLeaser.sprites[i].color = palette.blackColor;
                }
            }
            if (self.AbstrConsumable.rotted)
            {
                self.color = Color.Lerp(new(0.65f, 0.61f, 0.34f), palette.blackColor, self.darkness);
                return;
            }
        }
        orig(self, sLeaser, rCam, palette);
    }
}
