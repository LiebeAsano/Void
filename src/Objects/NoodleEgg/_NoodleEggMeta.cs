using MonoMod.Cil;
using Mono.Cecil.Cil;
using MoreSlugcats;
using static VoidTemplate.Useful.Utils;
using System;
using VoidTemplate.PlayerMechanics;

namespace VoidTemplate.Objects.NoodleEgg
{
    public static class _NoodleEggMeta
    {
        public static void Hook()
        {
            EggBitByPlayer.Hook();
            ShellGrabUpdate.Hook();
        }
    }
}
