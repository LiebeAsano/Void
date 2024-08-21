using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VoidTemplate.Useful.Utils;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour;

namespace VoidTemplate.PlayerMechanics;

internal static class SaintKarmaImmunity
{
    public static void Hook()
    {
        //gives stun instead of death at karma 11
        IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
    }

    private static void Player_ClassMechanicsSaint(ILContext il)
    {

        try
        {
            ILCursor c = new ILCursor(il);
            //if (physicalObject is Creature)
            //{
            //    if (!(physicalObject as Creature).dead)
            //    {
            //        flag2 = true;
            //    }
            //    (physicalObject as Creature) <if void and karma 11 TO label > .Die();
            //    <TO label2
            //    label
            //    //this is a bubble for the condition "void and karma 11"
            //    POP creature
            //    if victim is thevoid stun for 11 seconds
            //    label2>
            //}
            c.GotoNext(MoveType.After,
                i => i.MatchLdcI4(1),
                i => i.MatchStloc(15),
                i => i.MatchLdloc(18),
                i => i.MatchIsinst<Creature>());

            var label = c.DefineLabel();
            var label2 = c.DefineLabel();
            c.Emit(OpCodes.Dup);
            c.EmitDelegate<Func<Creature, bool>>((self) =>
                self is Player player && player.IsVoid());
            c.Emit(OpCodes.Brtrue_S, label);
            c.GotoNext(MoveType.After,
                i => i.MatchCallvirt<Creature>("Die"));
            c.Emit(OpCodes.Br, label2);
            c.MarkLabel(label);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloc, 18);
            c.EmitDelegate((PhysicalObject PhysicalObject) =>
            {
                if (PhysicalObject is Player p && p.IsVoid()) p.Stun(TicksPerSecond * 5);
            });
            c.MarkLabel(label2);
        }
        catch (Exception e)
        {
            _Plugin.logger.LogError(e);
            throw;
        }
    }
}
