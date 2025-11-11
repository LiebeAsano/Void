using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.ModsCompatibilty
{
    public class _ModsMeta
    {
        public static void PostModsInit()
        {
            foreach (var mod in ModManager.ActiveMods)
            {
                switch (mod.id)
                {
                    case "blood":
                        Blood.Init();
                        break;
                    case "mosquitoes":
                        MosquitoCompat.Init();
                        break;
                    case "SimplifiedMoveset":
                        throw new LWIncompatibleModException(mod.name);
                    case "swalloweverything":
                        throw new LWIncompatibleModException(mod.name);
                }
            }
        }
    }
}
