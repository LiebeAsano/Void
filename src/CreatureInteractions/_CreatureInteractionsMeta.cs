using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.CreatureInteractions;

public static class _CreatureInteractionsMeta
{
    public static void Hook()
    {
        AntiSpiderStun.Hook();
        DLLindigestion.Hook();
        LeechIndigestion.Hook();
        //OverseerBehavior.Hook();
    }

}
