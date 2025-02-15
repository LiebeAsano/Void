using JollyCoop;
using Menu;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        self.slugcatColorOrder.RemoveAll(slugcat => slugcat == VoidEnums.SlugcatID.Void || slugcat == VoidEnums.SlugcatID.Viy);


        int indexOfHunter = self.slugcatColorOrder.IndexOf(SlugcatStats.Name.Red);

        if (indexOfHunter >= 0)
        {
            if (voidDead)
            {
                self.slugcatColorOrder.Insert(indexOfHunter + 1, VoidEnums.SlugcatID.Viy);
            }
            else
            {
                self.slugcatColorOrder.Insert(indexOfHunter + 1, VoidEnums.SlugcatID.Void);
            }
        }

        var selectedSlugcat = self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;

        if (!self.slugcatColorOrder.Contains(selectedSlugcat))
        {
            if (self.slugcatColorOrder.Count > 0)
            {
                self.slugcatPageIndex = 0;
            }
        }

        if (self.slugcatPageIndex < 0 || self.slugcatPageIndex >= self.slugcatColorOrder.Count)
        {
            self.slugcatPageIndex = 0;
        }

    }
}
