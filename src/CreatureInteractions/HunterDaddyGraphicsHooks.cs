using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VoidTemplate.Objects;
using Watcher;

namespace VoidTemplate.CreatureInteractions
{
    public static class HunterDaddyGraphicsHooks
    {
        private static ConditionalWeakTable<DaddyLongLegs, DaddyExt> daddyCWT = new();

        public static DaddyExt GetDaddyExt(this DaddyLongLegs daddy) => daddyCWT.GetValue(daddy, _ => new());

        public static void Hook()
        {
            On.DaddyGraphics.HunterDummy.InitiateSprites += HunterDummy_InitiateSprites;
            On.DaddyGraphics.RotBodyColor += DaddyGraphics_RotBodyColor;
            On.DaddyLongLegs.ctor += DaddyLongLegs_ctor;
            On.DaddyGraphics.HunterDummy.ApplyPalette += HunterDummy_ApplyPalette;
            On.DaddyLongLegs.Update += DaddyLongLegs_Update;
        }

        private static void DaddyLongLegs_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            orig(self, eu);
            if (self.GetDaddyExt().isVoidDaddy && self.room != null && Random.Range(0, 3500) == 0)
            {
                self.room.PlaySound(Random.Range(0, 2) switch
                {
                    0 => WatcherEnums.WatcherSoundID.RotLiz_Vocalize,
                    1 => WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_A,
                    _ => WatcherEnums.WatcherSoundID.Lizard_Voice_Rot_B
                }, self.firstChunk.pos, self.abstractPhysicalObject);
            }
        }

        private static void HunterDummy_ApplyPalette(On.DaddyGraphics.HunterDummy.orig_ApplyPalette orig, DaddyGraphics.HunterDummy self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (self.owner.daddy.GetDaddyExt().isVoidDaddy)
            {
                for (int i = 0; i < self.numberOfSprites - 1; i++)
                {
                    sLeaser.sprites[self.startSprite + i].color = DrawSprites.voidColor;
                }
                sLeaser.sprites[self.startSprite + 5].color = Color.red;
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        private static void DaddyLongLegs_ctor(On.DaddyLongLegs.orig_ctor orig, DaddyLongLegs self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.HDmode && VoidDreamScript.IsVoidDream)
            {
                self.GetDaddyExt().isVoidDaddy = true;
                self.effectColor = self.eyeColor = Color.red;
            }
        }

        private static Color DaddyGraphics_RotBodyColor(On.DaddyGraphics.orig_RotBodyColor orig, DaddyGraphics self)
        {
            if (self.daddy.GetDaddyExt().isVoidDaddy)
            {
                return self.blackColor;
            }
            return orig(self);
        }

        private static void HunterDummy_InitiateSprites(On.DaddyGraphics.HunterDummy.orig_InitiateSprites orig, DaddyGraphics.HunterDummy self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.owner.daddy.GetDaddyExt().isVoidDaddy)
            {
                ReplaceFaceSprite(ref sLeaser.sprites[self.startSprite + 5]);

                void ReplaceFaceSprite(ref FSprite face)
                {
                    face.RemoveFromContainer();
                    face = new FSprite("Void-FaceDead", true);
                    rCam.ReturnFContainer("Midground").AddChild(face);
                }
            }
        }
    }

    public class DaddyExt
    {
        public bool isVoidDaddy;
    }
}
