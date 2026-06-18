using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.PortalForViy
{
    public class _PortalForViy
    {
        public static void Init()
        {
            MSGhostHooks.Hook();
            WarpHooks.Hook();
        }
    }
}
