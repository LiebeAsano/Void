using VoidTemplate.Useful;

namespace VoidTemplate;

public static class Lung
{
    public static void UpdateLungCapacity(Player player)
    {
        if (player != null && player.slugcatStats != null)
        {
            if (player.KarmaCap != 10)
            {
                int karma = player.Karma;

                float baseLungCapacity = 1.0f;
                float additionalCapacityPerKarma = 0.06f;
                float newLungCapacity = baseLungCapacity - (additionalCapacityPerKarma * (karma + 1));

                player.slugcatStats.lungsFac = newLungCapacity;

            }
            else
                player.slugcatStats.lungsFac = 0.15f;
        }
    }
}