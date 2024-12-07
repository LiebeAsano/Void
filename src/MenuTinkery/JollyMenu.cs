namespace VoidTemplate.MenuTinkery;

using MoreSlugcats;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

internal static class JollyMenu
{
	public static void Hook()
	{
        //change state of player to adult when choosing void
        //grey out slugpup toggle button when void is chosen
        //On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update1;

		//make jolly identify unique face sprite of void
		On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += JollyPlayerSelector_GetPupButtonOffName;
		//assigns eye color to be yellow on slugpup select button
		On.PlayerGraphics.JollyFaceColorMenu += PlayerGraphics_JollyFaceColorMenu;
        //On.PlayerGraphics.JollyBodyColorMenu += PlayerGraphics_JollyBodyColorMenu;
        //when making slugpup sprite color, jolly coop does Color.Clamp with L factor not going below 0.25
        //this hook assigns bodytintcolor again to bypass that
        On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update;
    }

    private static void JollyPlayerSelector_Update1(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
	{
		orig(self);
		SlugcatStats.Name name = JollyCoop.JollyCustom.SlugClassMenu(self.index, self.dialog.currentSlugcatPageName);
		if(name == VoidEnums.SlugcatID.Void)
		{
			//self.pupButton.GetButtonBehavior.greyedOut = true;


            if (self.pupButton.isToggled)
            {
                self.pupButton.Toggle();
            }
        }
	}

	private static string JollyPlayerSelector_GetPupButtonOffName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
	{
		var result = orig(self);
		var playerclass = self.JollyOptions(self.index).playerClass;
		if(playerclass is not null && playerclass == VoidEnums.SlugcatID.Void)
		{
			if (RWCustom.Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM && RWCustom.Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.AUTO)
			{
                result = "void_" + "pup_off";
            }
			else
			{
                result = "void2_" + "pup_off";
            }
		}
		return result;
	}

	private static Color PlayerGraphics_JollyFaceColorMenu(On.PlayerGraphics.orig_JollyFaceColorMenu orig, SlugcatStats.Name slugName, SlugcatStats.Name reference, int playerNumber)
	{
		var res = orig(slugName, reference, playerNumber);
		if (slugName == VoidEnums.SlugcatID.Void && RWCustom.Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM 
			&& (RWCustom.Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.AUTO
            || RWCustom.Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.AUTO && playerNumber == 0))
		{
			res = new Color(1f, 0.87f, 0f);
		}
		return res;

	}

    private static Color PlayerGraphics_JollyBodyColorMenu(On.PlayerGraphics.orig_JollyBodyColorMenu orig, SlugcatStats.Name slugName, SlugcatStats.Name reference)
    {
        var res = orig(slugName, reference);
        if (slugName == VoidEnums.SlugcatID.Void && RWCustom.Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM)
        {
            res = new Color(1f, 1f, 1f);
        }
        return res;
    }
    private static void JollyPlayerSelector_Update(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
	{
		orig(self);
		if(self.JollyOptions(0).playerClass == VoidEnums.SlugcatID.Void)
		{
			self.bodyTintColor = PlayerGraphics.JollyBodyColorMenu(
				new SlugcatStats.Name("JollyPlayer" + (self.index + 1).ToString(), false),
				self.JollyOptions(0).playerClass);
        }
	}
}
