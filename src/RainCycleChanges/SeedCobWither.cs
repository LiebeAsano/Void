using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class SeedCobWither
{
    private const int Lifetime = 6 * 60 * 40;
    private const int WitherTicks = 30 * 40;

    private static readonly ConditionalWeakTable<SeedCob.AbstractSeedCob, StrongBox<int>> openedAt = new();
    private static readonly ConditionalWeakTable<RoomCamera.SpriteLeaser, StrongBox<float>> shownWither = new();
    private static Color[] deadColors = [];

    public static void Hook()
    {
        On.SeedCob.Open += SeedCob_Open;
        On.SeedCob.Update += SeedCob_Update;
        On.SeedCob.DrawSprites += SeedCob_DrawSprites;
        On.SeedCob.ApplyPalette += SeedCob_ApplyPalette;
        IL.SeedCob.DrawSprites += SeedCob_DrawSpritesIL;
    }

    private static float Wither(SeedCob cob)
    {
        if (cob.AbstractCob.dead)
            return 1f;

        if (!openedAt.TryGetValue(cob.AbstractCob, out StrongBox<int> opened))
            return 0f;

        int age = cob.abstractPhysicalObject.world.game.clock - opened.Value;

        return Mathf.InverseLerp(Lifetime - WitherTicks, Lifetime, age);
    }

    private static void SeedCob_Open(On.SeedCob.orig_Open orig, SeedCob self)
    {
        bool closed = !self.AbstractCob.opened;

        orig(self);

        if (closed && self.room.game.IsVoidWorld())
            openedAt.Add(self.AbstractCob, new StrongBox<int>(self.room.game.clock));
    }

    private static void SeedCob_Update(On.SeedCob.orig_Update orig, SeedCob self, bool eu)
    {
        if (!self.AbstractCob.dead && Wither(self) >= 1f)
            self.AbstractCob.dead = true;

        orig(self, eu);
    }

    private static void SeedCob_DrawSprites(On.SeedCob.orig_DrawSprites orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!openedAt.TryGetValue(self.AbstractCob, out _))
            return;

        float shown = shownWither.TryGetValue(sLeaser, out StrongBox<float> box) ? box.Value : 0f;

        if (shown != Wither(self))
            self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
    }

    private static void SeedCob_ApplyPalette(On.SeedCob.orig_ApplyPalette orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        float wither = Wither(self);

        if (openedAt.TryGetValue(self.AbstractCob, out _))
            shownWither.GetValue(sLeaser, _ => new StrongBox<float>()).Value = wither;

        if (wither <= 0f || self.AbstractCob.dead)
        {
            orig(self, sLeaser, rCam, palette);
            return;
        }

        int first = self.CobSprite;
        int last = self.SeedSprite(self.seedPositions.Length - 1, 2);

        if (deadColors.Length <= last)
            deadColors = new Color[last + 1];

        self.AbstractCob.dead = true;
        orig(self, sLeaser, rCam, palette);

        Color deadYellow = self.yellowColor;

        for (int i = first; i <= last; i++)
            deadColors[i] = sLeaser.sprites[i].color;

        self.AbstractCob.dead = false;
        orig(self, sLeaser, rCam, palette);

        self.yellowColor = Color.Lerp(self.yellowColor, deadYellow, wither);

        for (int i = first; i <= last; i++)
            sLeaser.sprites[i].color = Color.Lerp(sLeaser.sprites[i].color, deadColors[i], wither);
    }

    private static void SeedCob_DrawSpritesIL(ILContext il)
    {
        ILCursor c = new(il);
        int seedScale = -1;

        if (!c.TryGotoNext(MoveType.After,
            x => x.MatchCall<Mathf>(nameof(Mathf.Sin)),
            x => x.MatchAdd(),
            x => x.MatchStloc(out seedScale)))
        {
            Utils.LogExErr("SeedCobWither: seed scale not found.");
            return;
        }

        c.Emit(OpCodes.Ldloc, il.Body.Variables[seedScale]);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(ShrinkSeed);
        c.Emit(OpCodes.Stloc, il.Body.Variables[seedScale]);
    }

    private static float ShrinkSeed(float scale, SeedCob cob) =>
        cob.AbstractCob.dead ? scale : scale * Mathf.Lerp(1f, 0.5f, Wither(cob));
}
