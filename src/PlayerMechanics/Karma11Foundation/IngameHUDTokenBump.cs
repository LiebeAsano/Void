namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class IngameHUDTokenBump
{
    public static void Startup()
    {
        On.HUD.KarmaMeter.Update += KarmaMeter_Update;
    }

    private static void KarmaMeter_Update(On.HUD.KarmaMeter.orig_Update orig, HUD.KarmaMeter self)
    {
        orig(self);
        if (self.reinforceAnimation == 135) self.UpdateGraphic();
    }
}
