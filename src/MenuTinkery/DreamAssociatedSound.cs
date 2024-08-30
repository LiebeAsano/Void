using System.Collections.Generic;
using static VoidTemplate.VoidEnums.DreamID;

namespace VoidTemplate.MenuTinkery;

internal static class DreamAssociatedSound
{

	//This dictionary associates DreamID with the ID of sound to be played. so yeah it does support custom sounds
	static Dictionary<DreamsState.DreamID, SoundID> DreamSoundMap;

	public static void Startup()
	{
		On.Menu.Menu.PlaySound_SoundID += Menu_PlaySound_SoundID;
		DreamSoundMap = new()
		{
			{ FarmDream, SoundID.Bomb_Explode}
		};
	}

	private static void Menu_PlaySound_SoundID(On.Menu.Menu.orig_PlaySound_SoundID orig, Menu.Menu self, SoundID soundID)
	{
		if(self is Menu.DreamScreen screen && !screen.initSound && DreamSoundMap.ContainsKey(screen.dreamID)) orig(self, DreamSoundMap[screen.dreamID]);
		else orig(self, soundID);
	}
}
