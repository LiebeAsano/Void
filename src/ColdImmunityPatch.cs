using HarmonyLib;
using UnityEngine;

namespace TheVoid
{
    public class ColdImmunityPatch
    {
        // Патчим метод обновления гипотермии
        [HarmonyPatch(typeof(Creature), nameof(Creature.HypothermiaUpdate))]
        public static class Patch_Creature_HypothermiaUpdate
        {
            public static bool Prefix(Creature __instance)
            {
                if (__instance is Player player && player.slugcatStats.name == Plugin.TheVoid)
                {
                    // Логируем текущее значение гипотермии
                    //Debug.Log($"[TheVoid] Preventing Hypothermia Update. Player: {player.slugcatStats.name} Hypothermia: {player.Hypothermia}");

                    // Обнуляем гипотермию для нашего слизнекота
                    player.Hypothermia = 0f;
                    return false; // Пропускаем оригинальный метод
                }

                return true; // Выполняем оригинальный метод для остальных существ
            }
        }

        // Патчим метод, который может взаимодействовать с гипотермией через контакт с телом
        [HarmonyPatch(typeof(Creature), nameof(Creature.HypothermiaBodyContactWarmup))]
        public static class Patch_Creature_HypothermiaBodyContactWarmup
        {
            public static bool Prefix(Creature __instance, Creature other, ref bool __result)
            {
                if (__instance is Player player && player.slugcatStats.name == Plugin.TheVoid)
                {
                    // Логируем любое взаимодействие с гипотермией через контакт с телом
                    //Debug.Log($"[TheVoid] Preventing Hypothermia Body Contact Warmup. Player: {player.slugcatStats.name}");

                    __result = true; // Препятствуем любым отрицательным эффектам
                    return false; // Пропускаем оригинальный метод
                }

                return true; // Выполняем оригинальный метод для остальных существ
            }
        }
    }
}