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

        public static void Init()
        {
            CreateBloodTextureForVoid();
            MethodBase bloodEmitterCtor = typeof(BloodEmitter).GetConstructor([typeof(Spear), typeof(BodyChunk), typeof(float), typeof(float)]);
            new Hook(bloodEmitterCtor, BloodEmitterHook);
        }

        public static void CreateBloodTextureForVoid()
        {
            Color[] voidColors = BloodMod.bloodTex.GetPixels();
            for (int i = 0; i < voidColors.Length; i++)
            {
                if (voidColors[i].a > 0)
                {
                    voidColors[i] = DrawSprites.voidColor;
                }                
            }
            
            Texture2D voidBloodTexture = new(BloodMod.w, BloodMod.h);
            voidBloodTexture.SetPixels(voidColors);
            voidBloodTexture.Apply(true);
            if (Futile.atlasManager.DoesContainAtlas(voidBloodTexName + "Tex"))
            {
                Futile.atlasManager.UnloadAtlas(voidBloodTexName + "Tex");
            }
            Futile.atlasManager.LoadAtlasFromTexture(voidBloodTexName + "Tex", voidBloodTexture, false);

            Color[] voidFluidColors = BloodMod.bloodTex.GetPixels();
            for (int i = 0; i < voidFluidColors.Length; i++)
            {
                if (voidFluidColors[i].a > 0)
                {
                    voidFluidColors[i] = DrawSprites.voidFluidColor;
                }
            }

            Texture2D voidFluidBloodTexture = new(BloodMod.w, BloodMod.h);
            voidFluidBloodTexture.SetPixels(voidFluidColors);
            voidFluidBloodTexture.Apply(true);
            if (Futile.atlasManager.DoesContainAtlas(voidBloodTexName + "FluidTex"))
            {
                Futile.atlasManager.UnloadAtlas(voidBloodTexName + "FluidTex");
            }
            Futile.atlasManager.LoadAtlasFromTexture(voidBloodTexName + "FluidTex", voidFluidBloodTexture, false);
        }

        private static void BloodEmitterHook(Action<BloodEmitter, Spear, BodyChunk, float, float> orig, BloodEmitter self, Spear spear, BodyChunk chunk, float velocity, float bleedTime)
        {
            orig(self, spear, chunk, velocity, bleedTime);
            if (chunk.owner is Player player && player.IsVoid())
            {
                if (Karma11Update.VoidKarma11)
                    self.creatureColor = DrawSprites.voidColor;
                else
                    self.creatureColor = DrawSprites.voidFluidColor;
                self.splatterColor = voidBloodTexName + (!Karma11Update.VoidKarma11 ? "Fluid" : "");
            }
        }
    }
}
