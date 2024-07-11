namespace VoidTemplate;

public static class Lung
{
    public static void UpdateLungCapacity(Player player)
    {
        if (player != null && player.slugcatStats != null)
        {
            int karmaCap = player.KarmaCap;

            float baseLungCapacity = 1.0f;
            float additionalCapacityPerKarma = 0.08f;
            float newLungCapacity = baseLungCapacity - (additionalCapacityPerKarma * (karmaCap + 1));

            player.slugcatStats.lungsFac = newLungCapacity;
        }
    }
}