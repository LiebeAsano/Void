using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PlayerMechanics.ViyMechanics
{
    internal static class _ViyMechanicsMeta
    {
        public static void Hook()
        {
            ViyMaul.Hook();
            ViyTail.Hook();
            ViyThrowSpear.Hook();
            ViyViolence.Hook();
            VoidViySwitch.Hook();
        }
    }
}
