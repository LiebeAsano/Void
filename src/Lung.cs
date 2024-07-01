namespace TheVoid
{
    public static class Lung
    {
        public static void UpdateLungCapacity(Player player)
        {
            if (player != null && player.slugcatStats != null)
            {
                int karmaCap = player.KarmaCap;

                // Объем легких зависит от KarmaCap.
                float baseLungCapacity = 0.6f;
                float additionalCapacityPerKarma = 0.04f;
                float newLungCapacity = baseLungCapacity - (additionalCapacityPerKarma * (karmaCap + 1));

                player.slugcatStats.lungsFac = newLungCapacity;

                //Debug.Log($"[TheVoid] Updated lung capacity for {player.slugcatStats.name}: {newLungCapacity}");

                // Вызываем LungUpdate для применения изменений
                player.LungUpdate();
            }
        }
    }
}