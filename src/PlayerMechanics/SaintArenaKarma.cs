using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.OptionInterface.OptionAccessors;

namespace VoidTemplate.PlayerMechanics;

internal static class SaintArenaKarma
{
    public static void Hook()
    {
        IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
    }

    private static void Player_ClassMechanicsSaint(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel? label = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCall<Player>("get_KarmaCap"),
            x => x.MatchLdcI4(9),
            x => x.MatchBge(out label)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Predicate<Player>>((Player p) => SaintArenaAscension && p.room.game.IsArenaSession);
            c.Emit(OpCodes.Brtrue, label);
        }
        else logerr($"{nameof(VoidTemplate.PlayerMechanics)}.{nameof(SaintArenaKarma)}.{nameof(Player_ClassMechanicsSaint)}: karma match failed. saint won't get karma abilities at arena");
    }
}
