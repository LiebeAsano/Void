using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics;

public static class _PlayerMechanicsMeta
{
    public static void Hook()
    {
        ColdImmunityPatch.Hook();
        Grabability.Hook();
        SpearmasterAntiMechanic.Hook();
    }
}
