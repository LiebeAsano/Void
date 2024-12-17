using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using VoidTemplate.MenuTinkery;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.PlayerMechanics.Karma11Features;

internal static class FoodChange
{
	public static void Hook()
	{
        On.StoryGameSession.ctor += StoryGameSession_ctor;
        IL.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
		IL.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
	}

	private static void SlugcatPageContinue_ctor(ILContext il)
	{
		ILCursor c = new(il);
		//this.hud.AddPart(new FoodMeter(this.hud, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x, SlugcatStats.SlugcatFoodMeter(slugcatNumber).y < 6 if void and at karma 11>, null, 0));
		if (c.TryGotoNext(MoveType.After, x => x.MatchNewobj<HUD.FoodMeter>())
			&& c.TryGotoPrev(MoveType.After,
			x => x.MatchLdfld<RWCustom.IntVector2>("y")))
		{
			c.Emit(OpCodes.Ldarg, 4);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg, 1);
            c.EmitDelegate((int origRess, SlugcatStats.Name name, Menu.SlugcatSelectMenu.SlugcatPageContinue slugcatPageContinue, Menu.Menu menu) =>
			{
                if (name == VoidEnums.SlugcatID.Void
				&& (slugcatPageContinue.saveGameData.karmaCap == 10
				|| menu.manager.rainWorld.progression.GetOrInitiateSaveState(VoidEnums.SlugcatID.Void, null, menu.manager.menuSetup, false) is SaveState save && save.miscWorldSaveData.SSaiConversationsHad >= 8))
				{
                    return 6;
                }
				return origRess;
			});
		}
		else LogExErr("couldn't find creation of food meter instruction. expect main menu food meter to always be at 10 required pips for survival");
	}

	//fix for whether slugcat will be malnourished in next cycle
	private static void ShelterDoor_DoorClosed(MonoMod.Cil.ILContext il)
	{
		ILCursor c = new(il);
		//used to bypass Jolly's static requirements for food consumption for character
		//int y = SlugcatStats.SlugcatFoodMeter(this.room.game.StoryCharacter).y < if void world and karma 10 make it 6>;
		if (c.TryGotoNext(MoveType.After, x => x.MatchCall<SlugcatStats>("SlugcatFoodMeter"),
			x => x.MatchLdfld(out _)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<int, ShelterDoor, int>>((int orig, ShelterDoor self) =>
			{
                var game = self.room.game;
                if (self.room.world.game.StoryCharacter == VoidEnums.SlugcatID.Void 
				&& ((self.room.game.Players[0].realizedCreature as Player).KarmaCap == 10 
				|| self.room.world.game.GetStorySession.saveState.miscWorldSaveData.SSaiConversationsHad >= 8))
				{
					return 6;
				}
				return orig;
			});
		}
		else LogExErr("failed to locate slugcatfoodmeter call in shelterdoor closing. expect mismatch between food requirements and success of hybernation");
	}
    /// <summary>
    /// replaces main menu food requirements
    /// </summary>
    /// <param name="orig"></param>
    /// <param name="self"></param>
    /// <param name="saveStateNumber"></param>
    /// <param name="game"></param>
    private static void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
    {
		orig(self, saveStateNumber, game);
        if (saveStateNumber == VoidEnums.SlugcatID.Void && (self.saveState.deathPersistentSaveData.karma == 10 || self.saveState.miscWorldSaveData.SSaiConversationsHad >= 8))
        {
            self.characterStats.foodToHibernate = self.saveState.malnourished ? 9 : 6;
            self.characterStats.maxFood = 9;
        }
    }
}
