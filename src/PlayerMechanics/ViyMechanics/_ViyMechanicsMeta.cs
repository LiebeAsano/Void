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
            ViyResists.Hook();
            ViyTail.Hook();
            ViyThrowSpear.Hook();
        }
    }
}
