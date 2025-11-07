using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;
using UnityEngine;
using VoidTemplate.PlayerMechanics.Karma11Features;

namespace VoidTemplate.ModsCompatibilty
{
    public class Blood
    {
        private const string voidBloodTexName = "VoidSlugcat";

        private static MethodBase bloodEmitterCtor = typeof(BloodEmitter).GetConstructor([typeof(Spear), typeof(BodyChunk), typeof(float), typeof(float)]);

        public static void Init()
        {
            CreateBloodTextureForVoid();
            new Hook(bloodEmitterCtor, bloodEmitterHook);
        }

        public static void CreateBloodTextureForVoid()
        {
            Color[] defaultColors = BloodMod.bloodTex.GetPixels();
            Color[] newColors = defaultColors;
            for (int i = 0; i < defaultColors.Length; i++)
            {
                if (newColors[i].a > 0)
                {
                    if (Karma11Update.VoidKarma11)
                        newColors[i] = DrawSprites.voidColor;
                    else
                        newColors[i] = DrawSprites.voidFluidColor;
                    newColors[i].a = defaultColors[i].a;
                }
            }
            Texture2D voidBloodTexture = new(BloodMod.w, BloodMod.h);
            voidBloodTexture.SetPixels(newColors);
            voidBloodTexture.Apply(true);
            if (Futile.atlasManager.DoesContainAtlas(voidBloodTexName + "Tex"))
            {
                Futile.atlasManager.UnloadAtlas(voidBloodTexName + "Tex");
            }
            Futile.atlasManager.LoadAtlasFromTexture(voidBloodTexName + "Tex", voidBloodTexture, false);
        }

        private static Delegate bloodEmitterHook =
        (Action<BloodEmitter, Spear, BodyChunk, float, float> orig, BloodEmitter self, Spear spear, BodyChunk chunk, float velocity, float bleedTime) =>
        {
            orig(self, spear, chunk, velocity, bleedTime);
            if (chunk.owner is Player player && player.IsVoid())
            {
                if (Karma11Update.VoidKarma11)
                    self.creatureColor = DrawSprites.voidColor;
                else
                    self.creatureColor = DrawSprites.voidFluidColor;
                self.splatterColor = voidBloodTexName;
            }
        };
    }
}
