namespace VoidTemplate.PlayerMechanics.ViyMechanics;

internal static class VoidViySwitch
{
    public static void Hook()
    {
        On.Menu.SlugcatSelectMenu.SetSlugcatColorOrder += SlugcatSelectMenu_SetSlugcatColorOrder;
    }

    private static void SlugcatSelectMenu_SetSlugcatColorOrder(On.Menu.SlugcatSelectMenu.orig_SetSlugcatColorOrder orig, Menu.SlugcatSelectMenu self)
    {
        orig(self);
        bool voidDead = SaveManager.ExternalSaveData.VoidDead;
        if(voidDead)
        {
            self.slugcatColorOrder.Remove(VoidEnums.SlugcatID.Void);
        }
        else
        {
            self.slugcatColorOrder.Remove(VoidEnums.SlugcatID.Viy);
        }
    }
}
