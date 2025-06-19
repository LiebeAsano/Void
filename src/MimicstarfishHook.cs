using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate
{
    static class MimicstarfishHook
    {
        public static void Hook()
        {

            On.DaddyGraphics.DaddyTubeGraphic.ApplyPalette += OnApplyPalette;
            On.DaddyLongLegs.Eat += OnEat;


        }

        private static void OnEat(On.DaddyLongLegs.orig_Eat orig, global::DaddyLongLegs self, bool eu)
        {
            
                orig(self, eu);
            
            if (self.Template.type == CreatureTemplateType.Mimicstarfish)
            {

                Vector2 middleOfBody = self.MiddleOfBody;
                for (int i = self.eatObjects.Count - 1; i >= 0; i--)
                {
                    if (self.eatObjects[i].progression > 1f)
                    {
                        if (self.eatObjects[i].chunk.owner is Creature)
                        {
                            self.AI.tracker.ForgetCreature((self.eatObjects[i].chunk.owner as Creature).abstractCreature);
                            if (self.eatObjects[i].chunk.owner is Player player)
                            {
                                player.PermaDie();
                            }
                        }
                        self.eatObjects[i].chunk.owner.Destroy();
                        self.eatObjects.RemoveAt(i);
                    }
                    else
                    {
                        self.eyesClosed = Math.Max(self.eyesClosed, 15);
                        if (self.eatObjects[i].chunk.owner.collisionLayer != 2)
                        {
                            self.eatObjects[i].chunk.owner.ChangeCollisionLayer(2);
                        }

                    }
                }
            }





        }
        

       


        private static void OnApplyPalette(On.DaddyGraphics.DaddyTubeGraphic.orig_ApplyPalette orig, global::DaddyGraphics.DaddyTubeGraphic self, global::RoomCamera.SpriteLeaser sLeaser, global::RoomCamera rCam, global::RoomPalette palette)
        {

            orig(self, sLeaser, rCam, palette);
            Color color = palette.blackColor;

            var rotGraphics = self.owner as DaddyGraphics;

            if (rotGraphics.daddy.Template.type == CreatureTemplateType.Mimicstarfish)
            {

                color = new Color(1f, 0.8f, 0.8f);
                for (int i = 0; i < (sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length; i++)
                {
                    float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length - 1));
                    (sLeaser.sprites[self.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(color, Custom.HSL2RGB(Custom.WrappedRandomVariation(1f, .48f, .15f), .8f, Custom.ClampedRandomVariation(.86f, .72f, .43f)), self.OnTubeEffectColorFac(floatPos));
                }
                int num = 0;
                for (int j = 0; j < self.bumps.Length; j++)
                {
                    sLeaser.sprites[self.firstSprite + 1 + j].color = Color.Lerp(color, Custom.HSL2RGB(Custom.WrappedRandomVariation(1f, .48f, .15f), .8f, Custom.ClampedRandomVariation(.86f, .72f, .43f)), self.OnTubeEffectColorFac(self.bumps[j].pos.y));
                    if (self.bumps[j].eyeSize > 0f)
                    {
                        sLeaser.sprites[self.firstSprite + 1 + self.bumps.Length + num].color = (rotGraphics.colorClass ? rotGraphics.EffectColor : color);
                        num++;
                    }
                }
            }


        }
        public static readonly ConditionalWeakTable<DaddyGraphics, Color[]> daddyColors = new ConditionalWeakTable<global::DaddyGraphics, Color[]>();

        public static int digestingCounter = 0;
        public class EatObject
        {
            public EatObject(BodyChunk chunk, float distance)
            {
                this.chunk = chunk;
                this.distance = distance;
                this.progression = 0f;
            }

            public BodyChunk chunk;

            public float distance;

            public float progression;
        }



        public static float whiteCamoColorAmountDrag = 1f;
        public static MimicstarfishGraphics owners = null;
        //public static Color whiteCamoColor;
        public static float whiteCamoColorAmount = -1f;
        //public static Color whitePickUpColor;
        public static float showDominance = 0;
        public static float whiteDominanceHue = 0;
        //public static int whiteGlitchFit;
    }
}