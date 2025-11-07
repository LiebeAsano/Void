using MonoMod.RuntimeDetour;
using Mosquitoes;
using RWCustom;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.ModsCompatibilty
{
    public class MosquitoCompat
    {
        private static Hook mosquitoStickHook;
        private static Hook mosquitoBitByPlayerHook;

        private static readonly ConditionalWeakTable<Mosquitoes.Mosquito, VoidInfection> infectedMosquitoes = new();

        public static void Init()
        {
            MethodInfo stickIntoChunkMethod = typeof(Mosquitoes.Mosquito).GetMethod("StickIntoChunk",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(PhysicalObject), typeof(int)],
                null);

            if (stickIntoChunkMethod != null)
            {
                mosquitoStickHook = new Hook(stickIntoChunkMethod, StickIntoChunkHook);
            }

            MethodInfo updateMethod = typeof(Mosquitoes.Mosquito).GetMethod("Update",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(bool)],
                null);

            if (updateMethod != null)
            {
                new Hook(updateMethod, UpdateHook);
            }

            MethodInfo applyPaletteMethod = typeof(Mosquitoes.MosquitoGraphics).GetMethod("ApplyPalette",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(RoomCamera.SpriteLeaser), typeof(RoomCamera), typeof(RoomPalette)],
                null);

            if (applyPaletteMethod != null)
            {
                new Hook(applyPaletteMethod, ApplyPaletteHook);
            }

            MethodInfo bitByPlayerMethod = typeof(Mosquitoes.Mosquito).GetMethod("BitByPlayer",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(Creature.Grasp), typeof(bool)],
                null);

            if (bitByPlayerMethod != null)
            {
                mosquitoBitByPlayerHook = new Hook(bitByPlayerMethod, BitByPlayerHook);
            }
        }

        private static readonly Delegate StickIntoChunkHook =
            (Action<Mosquitoes.Mosquito, PhysicalObject, int> orig, Mosquitoes.Mosquito self, PhysicalObject otherObject, int otherChunk) =>
            {
                if (otherObject is Player player && player.IsVoid())
                {
                    InfectMosquito(self);
                }

                orig(self, otherObject, otherChunk);
            };

        private static readonly Delegate UpdateHook =
            (Action<Mosquitoes.Mosquito, bool> orig, Mosquitoes.Mosquito self, bool eu) =>
            {
                orig(self, eu);

                if (infectedMosquitoes.TryGetValue(self, out var infection))
                {
                    infection.timer++;

                    if (infection.timer >= 240 && !self.dead)
                    {
                        self.Die();

                        if (infectedMosquitoes.TryGetValue(self, out var deadInfection))
                        {
                            deadInfection.lethalToNonVoid = true;
                        }
                    }
                }
            };

        private static readonly Delegate ApplyPaletteHook =
            (Action<Mosquitoes.MosquitoGraphics, RoomCamera.SpriteLeaser, RoomCamera, RoomPalette> orig, Mosquitoes.MosquitoGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) =>
            {
                orig(self, sLeaser, rCam, palette);

                if (infectedMosquitoes.TryGetValue(self.bug, out var infection))
                {
                    float progress = infection.timer / 240f;

                    Color currentMainColor = (sLeaser.sprites[0] as TriangleMesh).verticeColors[2];
                    Color currentAccentColor = (sLeaser.sprites[0] as TriangleMesh).verticeColors[(sLeaser.sprites[0] as TriangleMesh).verticeColors.Length - 1];
                    Color currentWhiteColor = (sLeaser.sprites[0] as TriangleMesh).verticeColors[0];

                    Color voidColor = Color.Lerp(currentMainColor, DrawSprites.voidColor, progress);
                    Color voidAccent = Color.Lerp(currentAccentColor, DrawSprites.voidColor, progress);
                    Color voidWhite = Color.Lerp(currentWhiteColor, DrawSprites.voidColor, progress);

                    for (int i = 0; i < (sLeaser.sprites[0] as TriangleMesh).verticeColors.Length; i++)
                    {
                        float value = Mathf.InverseLerp(0f, (float)((sLeaser.sprites[0] as TriangleMesh).verticeColors.Length - 1), (float)i);
                        (sLeaser.sprites[0] as TriangleMesh).verticeColors[i] = Color.Lerp(voidColor, voidAccent, 0.25f + Mathf.InverseLerp(0f, 1f, value) * 0.75f * self.bug.bloat);
                    }

                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[0] = voidWhite;
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[1] = voidWhite;

                    for (int j = 0; j < (sLeaser.sprites[1] as TriangleMesh).verticeColors.Length; j++)
                    {
                        float value2 = Mathf.InverseLerp(0f, (float)((sLeaser.sprites[1] as TriangleMesh).verticeColors.Length - 1), (float)j);
                        (sLeaser.sprites[1] as TriangleMesh).verticeColors[j] = Custom.RGB2RGBA(voidWhite, Mathf.InverseLerp(0f, 0.2f, value2) * 0f);
                    }

                    for (int k = 0; k < 2; k++)
                    {
                        (sLeaser.sprites[2 + k] as CustomFSprite).verticeColors[2] = Color.Lerp(palette.fogColor, voidColor, 0.5f);
                        (sLeaser.sprites[2 + k] as CustomFSprite).verticeColors[3] = Color.Lerp(palette.fogColor, voidColor, 0.5f);
                        (sLeaser.sprites[2 + k] as CustomFSprite).verticeColors[0] = Color.Lerp(palette.fogColor, voidWhite, 0.5f);
                        (sLeaser.sprites[2 + k] as CustomFSprite).verticeColors[1] = Color.Lerp(palette.fogColor, voidWhite, 0.5f);
                    }
                }
            };

        private static readonly Delegate BitByPlayerHook =
            (Action<Mosquitoes.Mosquito, Creature.Grasp, bool> orig, Mosquitoes.Mosquito self, Creature.Grasp grasp, bool eu) =>
            {
                bool wasInfected = infectedMosquitoes.TryGetValue(self, out var infection);
                bool isLethal = wasInfected && infection.lethalToNonVoid;

                if (grasp.grabber is Player player && player.IsVoid())
                {
                    self.bites--;
                }

                orig(self, grasp, eu);

                if (isLethal && grasp.grabber is Player eater && !eater.IsVoid() && self.eaten > 0)
                {
                    eater.Die();
                }
            };

        private static void InfectMosquito(Mosquitoes.Mosquito mosquito)
        {
            if (!infectedMosquitoes.TryGetValue(mosquito, out _) && !mosquito.dead)
            {
                infectedMosquitoes.Add(mosquito, new VoidInfection());
            }
        }

        private class VoidInfection
        {
            public int timer = 0;
            public bool lethalToNonVoid = false;
        }
    }
}