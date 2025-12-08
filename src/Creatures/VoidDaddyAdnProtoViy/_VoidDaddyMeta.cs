using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Creatures.VoidDaddyAdnProtoViy
{
    public class _VoidDaddyMeta
    {
        public static void Hook()
        {
            VoidDaddy.Hook();
            VoidDaddyGraphics.Hook();
        }
    }
}
