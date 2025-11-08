using MonoMod.RuntimeDetour;
using Mosquitoes;
using RWCustom;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.ModsCompatibilty
{
    public class MosquitoCompat
    {
        private static readonly ConditionalWeakTable<object, VoidInfection> infectedMosquitoes = new();

        public static void Init()
        {
            Type mosquitoType = typeof(Mosquito);
            MethodInfo stickIntoChunkMethod = mosquitoType.GetMethod("StickIntoChunk",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(PhysicalObject), typeof(int)],
                null);

            if (stickIntoChunkMethod != null)
            {
                new Hook(stickIntoChunkMethod, StickIntoChunkHook);
            }

            MethodInfo updateMethod = mosquitoType.GetMethod("Update",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(bool)],
                null);

            if (updateMethod != null)
            {
                new Hook(updateMethod, UpdateHook);
            }

            MethodInfo applyPaletteMethod = typeof(MosquitoGraphics).GetMethod("ApplyPalette",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(RoomCamera.SpriteLeaser), typeof(RoomCamera), typeof(RoomPalette)],
                null);

            if (applyPaletteMethod != null)
            {
                new Hook(applyPaletteMethod, ApplyPaletteHook);
            }

            MethodInfo bitByPlayerMethod = mosquitoType.GetMethod("BitByPlayer",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [typeof(Creature.Grasp), typeof(bool)],
                null);

            if (bitByPlayerMethod != null)
            {
                new Hook(bitByPlayerMethod, BitByPlayerHook);
            }
        }

        private static void StickIntoChunkHook(Action<Mosquito, PhysicalObject, int> orig, Mosquito self, PhysicalObject otherObject, int otherChunk)
        {
            if (otherObject is Player player && player.IsVoid())
            {
                InfectMosquito(self);
            }

            orig(self, otherObject, otherChunk);
        }

        private static void UpdateHook(Action<Mosquito, bool> orig, Mosquito self, bool eu)
        {
            orig(self, eu);

            if (infectedMosquitoes.TryGetValue(self, out var infection))
            {
                infection.timer++;

                var deadProperty = self.GetType().GetProperty("dead");
                bool isDead = deadProperty != null && (bool)deadProperty.GetValue(self);

                if (infection.timer >= 240 && !isDead)
                {
                    var dieMethod = self.GetType().GetMethod("Die");
                    dieMethod?.Invoke(self, null);

                    if (infectedMosquitoes.TryGetValue(self, out var deadInfection))
                    {
                        deadInfection.lethalToNonVoid = true;
                    }
                }
            }
        }

        private static void ApplyPaletteHook(Action<MosquitoGraphics, RoomCamera.SpriteLeaser, RoomCamera, RoomPalette> orig, MosquitoGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);

            var bugField = self.GetType().GetField("bug");
            if (bugField == null) return;

            var mosquito = bugField.GetValue(self);
            if (mosquito == null) return;

            if (infectedMosquitoes.TryGetValue(mosquito, out var infection))
            {
                float progress = infection.timer / 240f;

                if (sLeaser.sprites[0] is TriangleMesh mesh)
                {
                    Color currentMainColor = mesh.verticeColors[2];
                    Color currentAccentColor = mesh.verticeColors[mesh.verticeColors.Length - 1];
                    Color currentWhiteColor = mesh.verticeColors[0];

                    Color voidColor = Color.Lerp(currentMainColor, DrawSprites.voidColor, progress);
                    Color voidAccent = Color.Lerp(currentAccentColor, DrawSprites.voidColor, progress);
                    Color voidWhite = Color.Lerp(currentWhiteColor, DrawSprites.voidColor, progress);

                    for (int i = 0; i < mesh.verticeColors.Length; i++)
                    {
                        float value = Mathf.InverseLerp(0f, mesh.verticeColors.Length - 1, i);
                        var bloatProperty = mosquito.GetType().GetProperty("bloat");
                        float bloat = bloatProperty != null ? (float)bloatProperty.GetValue(mosquito) : 1f;
                        mesh.verticeColors[i] = Color.Lerp(voidColor, voidAccent, 0.25f + Mathf.InverseLerp(0f, 1f, value) * 0.75f * bloat);
                    }

                    mesh.verticeColors[0] = voidWhite;
                    mesh.verticeColors[1] = voidWhite;


                    if (sLeaser.sprites[1] is TriangleMesh mesh1)
                    {
                        for (int j = 0; j < mesh1.verticeColors.Length; j++)
                        {
                            float value2 = Mathf.InverseLerp(0f, mesh1.verticeColors.Length - 1, j);
                            mesh1.verticeColors[j] = Custom.RGB2RGBA(voidWhite, Mathf.InverseLerp(0f, 0.2f, value2) * 0f);
                        }
                    }

                    for (int k = 0; k < 2; k++)
                    {
                        if (sLeaser.sprites[2 + k] is CustomFSprite wingSprite)
                        {
                            wingSprite.verticeColors[2] = Color.Lerp(palette.fogColor, voidColor, 0.5f);
                            wingSprite.verticeColors[3] = Color.Lerp(palette.fogColor, voidColor, 0.5f);
                            wingSprite.verticeColors[0] = Color.Lerp(palette.fogColor, voidWhite, 0.5f);
                            wingSprite.verticeColors[1] = Color.Lerp(palette.fogColor, voidWhite, 0.5f);
                        }
                    }
                }
            }
        }

        private static void BitByPlayerHook
            (Action<Mosquito, Creature.Grasp, bool> orig, Mosquito self, Creature.Grasp grasp, bool eu)
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
        }

        private static void InfectMosquito(Mosquito mosquito)
        {
            if (!mosquito.dead && !infectedMosquitoes.TryGetValue(mosquito, out _))
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