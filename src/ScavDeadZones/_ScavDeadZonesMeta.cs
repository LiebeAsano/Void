using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.ScavDeadZones
{
    public class _ScavDeadZonesMeta
    {
        public static void Init()
        {
            ScavSpawnerManipulate.Hook();
            ScavHooks.Hook();
            SaveHooks.Hook();
        }
    }
}
