using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VoidTemplate.Useful;
using static VoidTemplate.VoidEnums.DreamID;

namespace VoidTemplate.MenuTinkery;

public static class DreamAssociatedSound
{
	//This dictionary associates DreamID with the ID of sound to be played. so yeah it does support custom sounds
	static Dictionary<DreamsState.DreamID, SoundID> DreamSoundMap;

	public static void Startup()
	{
		On.Menu.Menu.PlaySound_SoundID += Menu_PlaySound_SoundID;
        IL.Menu.DreamScreen.Update += DreamScreen_Update;
		DreamSoundMap = new()
		{
			{ Void_NSHDream, VoidEnums.SoundID.VoidNSHDreamSound },
			{ NSHDream, VoidEnums.SoundID.NSHDreamSound },
			{ SkyDream, VoidEnums.SoundID.SkyDreamSound },
			{ SubDream, VoidEnums.SoundID.SubDreamSound },
			{ FarmDream, VoidEnums.SoundID.FarmDreamSound },
			{ HunterRotDream, VoidEnums.SoundID.HunterRotDreamSound },
			{ MoonDream, VoidEnums.SoundID.MoonDreamSound },
			{ PebbleDream, VoidEnums.SoundID.PebbleDreamSound },
			{ RotDream, VoidEnums.SoundID.RotDreamSound },
            { Void_SeaDream, VoidEnums.SoundID.VoidSeaDreamSound },
            { Void_BodyDream, VoidEnums.SoundID.VoidBodyDreamSound },
            { Void_HeartDream, VoidEnums.SoundID.VoidHeartDreamSound },
        };
	}

    private static void DreamScreen_Update(ILContext il)
    {
		ILCursor c = new(il);
		if (c.TryGotoNext(x => x.MatchLdfld<RainWorldGame.SetupValues>("devToolsActive"))
			&& c.TryGotoNext(MoveType.After, x => x.MatchLdcI4(340)))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate((int orig, DreamScreen self) =>
			{
				if (self.dreamID == HunterRotDream || (self.scene != null && self.scene.sceneID == VoidEnums.SceneID.HunterRot))
				{
					return 1500;
				}
				return orig;
			});
		}
    }

    private static void Menu_PlaySound_SoundID(On.Menu.Menu.orig_PlaySound_SoundID orig, Menu.Menu self, SoundID soundID)
	{
		if (self is Menu.DreamScreen screen && soundID == SoundID.MENU_Dream_Switch && DreamSoundMap.ContainsKey(screen.dreamID)) soundID = DreamSoundMap[screen.dreamID];
		orig(self, soundID);
	}
}
