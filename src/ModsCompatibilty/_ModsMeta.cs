using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoidTemplate.Useful;

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
                    case "swalloweverything":
                        throw new LWIncompatibleModException(mod.name);
                }
            }
            var myMod = ModManager.GetModById(Utils.ModID);
            List<string> activeMods = [.. ModManager.ActiveMods.Select(m => m.name)];
            if (activeMods.Count > myMod.requirementsNames.Length + 1)
            {
                RemoveReqMods(myMod);
                Utils.NotLegitRunMods = new([.. activeMods]);
            }

            void RemoveReqMods(ModManager.Mod mod)
            {
                if (activeMods.Contains(mod.name))
                {
                    activeMods.Remove(mod.name);
                    if (mod.requirements.Length > 0)
                    {
                        for (int i = 0; i < mod.requirements.Length; i++)
                        {
                            var reqMod = ModManager.GetModById(mod.requirements[i]);
                            RemoveReqMods(reqMod);
                        }
                    }
                }
            }
        }
    }
}
