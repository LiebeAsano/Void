using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static VoidTemplate.VoidEnums.DreamID;
using Menu;
using static VoidTemplate.Useful.Utils;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.MenuTinkery;

internal static class DreamAssociatedSound
{

	//This dictionary associates DreamID with the ID of sound to be played. so yeah it does support custom sounds
	static Dictionary<DreamsState.DreamID, SoundID> DreamSoundMap;

	public static void Startup()
	{
		IL.Menu.DreamScreen.Update += DreamScreen_Update;
        DreamSoundMap = new()
		{
			{ FarmDream, SoundID.Bomb_Explode}
		};
    }

	private static void DreamScreen_Update(MonoMod.Cil.ILContext il)
	{
		ILCursor c = new(il);

		//if (!this.initSound)
		//{
		//	base.PlaySound( <if within keys go to bubblestart>
		//	SoundID.MENU_Dream_Init
		//	<Go to bubbleend
		//	bubblestart
		// 	give associated sound
		// 	bubbleend> );
		//	this.initSound = true;
		//}

		var bubblestart = c.DefineLabel();
		var bubbleend = c.DefineLabel();

		if (c.TryGotoNext(MoveType.Before, x => x.MatchLdsfld("SoundID", "MENU_Dream_Init")))
		{
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Predicate<DreamScreen>>((DreamScreen self) => DreamSoundMap.ContainsKey(self.dreamID));
			c.Emit(OpCodes.Brtrue_S, bubblestart);
			c.GotoNext(MoveType.After, x => x.MatchLdsfld("SoundID", "MENU_Dream_Init"));
			c.Emit(OpCodes.Br, bubbleend);
			c.MarkLabel(bubblestart);
			c.EmitDelegate<Func<DreamScreen, SoundID>>((DreamScreen screen) => DreamSoundMap[screen.dreamID]);
			c.MarkLabel(bubbleend);
		}
		else logerr($"{nameof(VoidTemplate.MenuTinkery)}.{nameof(DreamAssociatedSound)}.{nameof(DreamScreen_Update)}: first match failed");

    }
}
