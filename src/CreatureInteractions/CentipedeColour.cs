using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate.CreatureInteractions;

public static class CentipedeColour
{
    public static void Hook()
    {
        On.CentipedeGraphics.DrawSprites += CentipedeGraphics_DrawSprites;
        On.Centipede.ShortCutColor += Centipede_ShortCutColor;
    }

    private static void CentipedeGraphics_DrawSprites(On.CentipedeGraphics.orig_DrawSprites orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.culled || !self.centipede.Small || self.centipede.abstractCreature.IsVoided() || self.centipede.abstractCreature.Room.world.name != "IW")
            return;

        UnityEngine.Random.State state = UnityEngine.Random.state;
        UnityEngine.Random.InitState(self.centipede.abstractCreature.ID.RandomSeed);

        float hue = Mathf.Lerp(0.55f, 0.65f, UnityEngine.Random.value);
        float saturation = Mathf.Lerp(0.8f, 1f, UnityEngine.Random.value);

        Color blueColor = Custom.HSL2RGB(hue, saturation, 0.5f);
        Color darkBlueColor = Custom.HSL2RGB(hue, saturation * 0.8f, 0.3f);

        for (int i = 0; i < self.centipede.bodyChunks.Length; i++)
        {
            if (self.centipede.BitesLeft > i)
            {
                int shellSprite = self.ShellSprite(i, 0);
                if (shellSprite >= 0 && shellSprite < sLeaser.sprites.Length)
                {
                    sLeaser.sprites[shellSprite].color = Color.Lerp(blueColor, self.blackColor, self.darkness);
                }

                if (i > 0)
                {
                    int secondarySprite = self.SecondarySegmentSprite(i - 1);
                    if (secondarySprite >= 0 && secondarySprite < sLeaser.sprites.Length)
                    {
                        sLeaser.sprites[secondarySprite].color = Color.Lerp(darkBlueColor, self.blackColor, Mathf.Lerp(0.4f, 1f, self.darkness));
                    }
                }
            }
        }

        for (int i = 0; i < self.centipede.bodyChunks.Length; i++)
        {
            for (int l = 0; l < 2; l++)
            {
                if (sLeaser.sprites[self.LegSprite(i, l, 1)] is VertexColorSprite legSprite)
                {
                    Color legColor = Color.Lerp(darkBlueColor, self.blackColor, 0.3f + 0.7f * self.darkness);
                    legSprite.verticeColors[0] = legColor;
                    legSprite.verticeColors[1] = legColor;
                }
            }
        }

        UnityEngine.Random.state = state;
    }

    private static Color Centipede_ShortCutColor(On.Centipede.orig_ShortCutColor orig, Centipede self)
    {
        if (ModManager.DLCShared && self.Small && self.abstractCreature.Room.world.name == "IW")
            return Custom.HSL2RGB(0.65f, 0.75f, 0.5f);
        return orig(self);
    }
}