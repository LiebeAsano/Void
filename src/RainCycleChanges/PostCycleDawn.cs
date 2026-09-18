using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.RainCycleChanges;

public static class PostCycleDawn
{
    private const float DuskStart = 1320f;
    private const float NightStart = DuskStart * 1.47f;
    private const float FullNight = DuskStart * 1.92f;

    public static readonly int FullNightCounter = Mathf.CeilToInt(FullNight) + 1;

    public static void Hook()
    {
        On.RoomCamera.UpdateDayNightPalette += RoomCamera_UpdateDayNightPalette;
        IL.AboveCloudsView.Update += SkyView_Update;
        IL.RoofTopView.Update += SkyView_Update;
    }

    public static bool IsDawn(RainCycle rainCycle)
    {
        return rainCycle.dayNightCounter > 0
            && rainCycle.timer < rainCycle.cycleLength
            && rainCycle.timer < rainCycle.sunDownStartTime
            && rainCycle.world.game.session is StoryGameSession session
            && session.saveStateNumber == VoidEnums.SlugcatID.Void;
    }

    private static void RoomCamera_UpdateDayNightPalette(On.RoomCamera.orig_UpdateDayNightPalette orig, RoomCamera self)
    {
        orig(self);

        if (self.effect_dayNight <= 0f || !IsDawn(self.room.world.rainCycle))
            return;

        RainCycle rainCycle = self.room.world.rainCycle;
        RoomSettings.FadePalette fade = self.room.roomSettings.fadePalette;
        int roomPalette = self.room.roomSettings.Palette;
        int fadePalette = fade?.palette ?? -1;
        float roomBlend = fade != null && self.currentCameraPosition >= 0 && self.currentCameraPosition < fade.fades.Length
            ? fade.fades[self.currentCameraPosition]
            : 0f;

        float counter = rainCycle.dayNightCounter - 1;

        if (counter >= NightStart)
            self.ChangeBothPalettes(rainCycle.duskPalette, rainCycle.nightPalette, Mathf.InverseLerp(NightStart, FullNight, counter) * self.effect_dayNight * 0.99f);
        else if (counter >= DuskStart)
            self.ChangeBothPalettes(fadePalette > -1 ? fadePalette : roomPalette, rainCycle.duskPalette, Mathf.InverseLerp(DuskStart, NightStart, counter));
        else
            self.ChangeBothPalettes(roomPalette, fadePalette, Mathf.Lerp(roomBlend, 1f, counter / DuskStart));
    }

    private static void SkyView_Update(ILContext il)
    {
        ILCursor c = new(il);

        if (c.TryGotoNext(x => x.MatchLdfld<RoomCamera>(nameof(RoomCamera.effect_dayNight)))
            && c.TryGotoNext(MoveType.After, x => x.MatchLdfld<RainCycle>(nameof(RainCycle.timer))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((int timer, UpdatableAndDeletable self) =>
                IsDawn(self.room.world.rainCycle) ? Mathf.Max(timer, self.room.world.rainCycle.cycleLength) : timer);
        }
        else Utils.LogExErr("PostCycleDawn: day/night gate not found in " + il.Method.FullName);
    }
}
