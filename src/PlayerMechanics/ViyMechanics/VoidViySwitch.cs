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
        bool voidKarma11 = SaveManager.ExternalSaveData.VoidKarma11;

        self.slugcatColorOrder.RemoveAll(slugcat => slugcat == VoidEnums.SlugcatID.Void || slugcat == VoidEnums.SlugcatID.Viy);

        int indexOfHunter = self.slugcatColorOrder.IndexOf(SlugcatStats.Name.Red);
        if (indexOfHunter >= 0)
        {
            if (voidDead && voidKarma11)
            {
                self.slugcatColorOrder.Insert(indexOfHunter + 1, VoidEnums.SlugcatID.Viy);
            }
            else
            {
                self.slugcatColorOrder.Insert(indexOfHunter + 1, VoidEnums.SlugcatID.Void);
            }
        }

        var selectedSlugcat = self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
        int newIndex = self.slugcatColorOrder.IndexOf(selectedSlugcat);

        if (newIndex >= 0)
        {
            self.slugcatPageIndex = newIndex;
        }
        else
        {
            self.slugcatPageIndex = 0;
        }
    }
}
