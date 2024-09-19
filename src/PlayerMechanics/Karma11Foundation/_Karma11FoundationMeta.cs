using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.Karma11Foundation;

internal static class _Karma11FoundationMeta
{
    public static void Hook()
    {
        KarmaLadderTweaks.Hook();
        NoKarmaDecreaseOnDeath.Initiate();
        TokenSystem.Initiate();
    }
}
