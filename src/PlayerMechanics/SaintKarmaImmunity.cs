using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using VoidTemplate.OptionInterface;
using static VoidTemplate.Useful.Utils;

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
            ILCursor c = new(il);
            //if (physicalObject is Creature)
            //{
            //    if (!(physicalObject as Creature).dead)
            //    {
            //        flag2 = true;
            //    }
            //    (physicalObject as Creature) <if void > .Die();
            //    <TO label2
            //    label
            //    //this is a bubble for the condition "void"
            //    POP creature
            //    if victim is the void stun for 5 seconds
            //    label2>
            //}
            c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(1),
                i => i.MatchStloc(15),
                i => i.MatchLdloc(18),
                i => i.MatchIsinst<Creature>());

            var label = c.DefineLabel();
            var label2 = c.DefineLabel();
            c.Emit(OpCodes.Dup);
            c.EmitDelegate<Func<Creature, bool>>((self) =>
                self is Player player && (player.IsVoid() || player.IsViy() || (player.room.game.IsArenaSession && OptionAccessors.ArenaAscensionStun)));
            c.Emit(OpCodes.Brtrue_S, label);
            c.TryGotoNext(MoveType.After,
                i => i.MatchCallvirt<Creature>("Die"));
            c.Emit(OpCodes.Br, label2);
            c.MarkLabel(label);
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldloc, 18);
            c.EmitDelegate((PhysicalObject PhysicalObject) =>
            {
                if (PhysicalObject is Player p && (p.IsVoid() || p.IsViy() || (p.room.game.IsArenaSession && OptionAccessors.ArenaAscensionStun)))
                {
                    p.Stun(TicksPerSecond * 5);
                }
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
