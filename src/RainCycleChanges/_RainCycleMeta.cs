using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.RainCycleChanges
{
    public class _RainCycleMeta
    {
        public static void Init()
        {
            DeadlessRain.Hook();
            RainCyclePlus.Hook();
            ShortcutHooks.Hook();
            RainMeterHooks.Hook();
            FoodMeterHooks.Hook();
        }
    }
}
