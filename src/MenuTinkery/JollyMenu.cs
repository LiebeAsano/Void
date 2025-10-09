namespace VoidTemplate.MenuTinkery;

using HUD;
using Menu;
using MoreSlugcats;
using RWCustom;
using System;
using UnityEngine;
using static VoidTemplate.Useful.Utils;

public static class JollyMenu
{
	public static void Hook()
	{
		//On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.ctor += JollyPlayerSpecificHud_ctor;
        //On.JollyCoop.JollyMenu.SymbolButtonToggle.ctor += SymbolButtonToggle_ctor;
        //change state of player to adult when choosing void
        //grey out slugpup toggle button when void is chosen
        //On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update1;

        //make jolly identify unique face sprite of void
        On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.LoadIcon += SymbolButtonTogglePupButton_LoadIcon;
		On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += JollyPlayerSelector_GetPupButtonOffName;
		//assigns eye color to be yellow on slugpup select button
		On.PlayerGraphics.JollyFaceColorMenu += PlayerGraphics_JollyFaceColorMenu;
        On.PlayerGraphics.JollyUniqueColorMenu += PlayerGraphics_JollyUniqueColorMenu;
		On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite += SymbolButtonTogglePupButton_HasUniqueSprite;
		//On.PlayerGraphics.JollyBodyColorMenu += PlayerGraphics_JollyBodyColorMenu;
		//when making slugpup sprite color, jolly coop does Color.Clamp with L factor not going below 0.25
		//On.JollyCoop.JollyMenu.JollyPlayerSelector.SetPortraitImage_Name_Color += JollyPlayerSelector_SetPortraitImage_Name_Color;
		//this hook assigns bodytintcolor again to bypass that
        On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update;
        On.StoryGameSession.CreateJollySlugStats += StoryGameSession_CreateJollySlugStats;
		//On.JollyCoop.JollyMenu.JollyPlayerSelector.SetPortraitImage_Name_Color += JollyPlayerSelector_SetPortraitImage_Name_Color;

    }

    private static void JollyPlayerSpecificHud_ctor(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.orig_ctor orig, JollyCoop.JollyHUD.JollyPlayerSpecificHud self, HUD hud, FContainer fContainer, AbstractCreature player)
    {
        orig(self, hud, fContainer, player);
		self.playerNumber = self.PlayerState.playerNumber;
        if (Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.CUSTOM
                   && Custom.rainWorld.options.jollyColorMode != Options.JollyColorMode.AUTO
                   || Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.AUTO
                   && self.playerNumber == 0)
        {
            self.playerColor = new(1f, 0.86f, 0f);
        }
    }

    private static void SymbolButtonToggle_ctor(On.JollyCoop.JollyMenu.SymbolButtonToggle.orig_ctor orig, JollyCoop.JollyMenu.SymbolButtonToggle self, Menu menu, MenuObject owner, string signal, Vector2 pos, Vector2 size, string symbolNameOn, string symbolNameOff, bool isOn, bool textAboveButton, string stringLabelOn, string stringLabelOff, FTextParams textParams)
	{
		orig(self, menu, owner, signal, pos, size, symbolNameOn, symbolNameOff, isOn, textAboveButton, stringLabelOn, stringLabelOff, textParams);
	}

	private static void SymbolButtonTogglePupButton_LoadIcon(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_LoadIcon orig, JollyCoop.JollyMenu.SymbolButtonTogglePupButton self)
	{
		if (self.symbolNameOff.Contains("void"))
		{
			string text = self.symbolNameOff;
			self.symbol.fileName = text;
			self.symbol.LoadFile();
			self.symbol.sprite.SetElementByName(text);
			if (self.faceSymbol != null)
			{
				self.faceSymbol.fileName = "face_" + self.symbol.fileName;
                self.faceSymbol.LoadFile();
                self.faceSymbol.sprite.SetElementByName(self.faceSymbol.fileName);
			}
			if (self.uniqueSymbol != null && self.HasUniqueSprite())
			{
                self.uniqueSymbol.fileName = "unique_" + self.symbolNameOff;
                self.uniqueSymbol.LoadFile();
                self.uniqueSymbol.sprite.SetElementByName(self.uniqueSymbol.fileName);
                self.uniqueSymbol.pos.y = self.size.y / 2f;
			}
            return;
        }
		
        orig(self);
    }
	

    private static bool SymbolButtonTogglePupButton_HasUniqueSprite(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_HasUniqueSprite orig, JollyCoop.JollyMenu.SymbolButtonTogglePupButton self)
    {
        if (self.symbolNameOff.Contains("void"))
		{
			return true;
		}
		return orig(self);
    }

    private static void JollyPlayerSelector_Update1(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
	{
		orig(self);
		SlugcatStats.Name name = JollyCoop.JollyCustom.SlugClassMenu(self.index, self.dialog.currentSlugcatPageName);
		if (name == VoidEnums.SlugcatID.Void)
		{

        }
	}

	private static string JollyPlayerSelector_GetPupButtonOffName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
	{
		var result = orig(self);
        SlugcatStats.Name playerclass = JollyCoop.JollyCustom.SlugClassMenu(self.index, self.dialog.currentSlugcatPageName);
        if (playerclass == VoidEnums.SlugcatID.Void)
        {
			result = "void_" + "pup_off";
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

    

    private static Color PlayerGraphics_JollyUniqueColorMenu(On.PlayerGraphics.orig_JollyUniqueColorMenu orig, SlugcatStats.Name slugName, SlugcatStats.Name reference, int playerNumber)
    {
        var res = orig(slugName, reference, playerNumber);
		if (slugName == VoidEnums.SlugcatID.Void)
		{
			res = new Color(1f, 1f, 1f);
		}
		return res;
    }

    private static Color PlayerGraphics_JollyBodyColorMenu(On.PlayerGraphics.orig_JollyBodyColorMenu orig, SlugcatStats.Name slugName, SlugcatStats.Name reference)
    {
        var res = orig(slugName, reference);
        if (slugName == VoidEnums.SlugcatID.Void)
        {
            res = new Color(1f, 0.87f, 0f);
        }
        return res;
    }


    private static void JollyPlayerSelector_Update(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
    {
        orig(self);

        SlugcatStats.Name playerclass = JollyCoop.JollyCustom.SlugClassMenu(self.index, self.dialog.currentSlugcatPageName);

        if (playerclass == VoidEnums.SlugcatID.Void)
        {
            self.bodyTintColor = self.faceTintColor;

            if (self.JollyOptions(self.index).isPup)
            {
                self.JollyOptions(self.index).isPup = false;
            }
            self.pupButton.GetButtonBehavior.greyedOut = true;

            if (self.pupButton.symbolNameOff != "void_pup_off")
            {
                self.pupButton.symbolNameOff = "void_pup_off";
                self.pupButton.LoadIcon();
            }
        }
    }

    private static void StoryGameSession_CreateJollySlugStats(On.StoryGameSession.orig_CreateJollySlugStats orig, StoryGameSession self, bool m)
    {
        orig(self, m);
        PlayerState playerState;
        SlugcatStats slugcatStats = new(self.saveState.saveStateNumber, m);
        for (int i = 0; i < self.game.world.game.Players.Count; i++)
        {
            if (slugcatStats.name == VoidEnums.SlugcatID.Void)
            {
                playerState = self.game.Players[i].state as PlayerState;
                SlugcatStats.Name playerClass = self.game.rainWorld.options.jollyPlayerOptionsArray[playerState.playerNumber].PlayerClass ?? self.saveState.saveStateNumber;
                self.characterStatsJollyplayer[playerState.playerNumber] = new SlugcatStats(playerClass, m)
                {
                    foodToHibernate = self.saveState.malnourished ? 9 + (self.saveState.deathPersistentSaveData.karmaCap == 10 ? self.saveState.GetVoidExtraFood() : 0) : (6 + (self.saveState.GetVoidExtraFood() == 3 ? self.saveState.GetVoidFoodToHibernate() : 0)),
                    maxFood = 9 + (self.saveState.deathPersistentSaveData.karmaCap == 10 ? self.saveState.GetVoidExtraFood() : 0),
                    bodyWeightFac = slugcatStats.bodyWeightFac
                };
            }
        }
    }
}
