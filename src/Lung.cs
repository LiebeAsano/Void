namespace TheVoid
{
    public static class Lung
    {
        public static void UpdateLungCapacity(Player player)
        {
            if (player != null && player.slugcatStats != null)
            {
                int karmaCap = player.KarmaCap;

                float baseLungCapacity = 0.6f;
                float additionalCapacityPerKarma = 0.04f;
                float newLungCapacity = baseLungCapacity - (additionalCapacityPerKarma * (karmaCap + 1));

                player.slugcatStats.lungsFac = newLungCapacity;
#warning check this out later
                player.LungUpdate(); //very questionable
            }
        }
    }
}