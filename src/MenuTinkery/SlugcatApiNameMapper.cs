using System;

namespace VoidTemplate.MenuTinkery;

public static class SlugcatApiNameMapper
{
    public static string GetApiName(SlugcatStats.Name slugcat)
    {
        if (slugcat == VoidEnums.SlugcatID.Void) return "Void";
        if (slugcat == VoidEnums.SlugcatID.Viy) return "Viy";

        string value = slugcat.value;
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slugcat has no API name", nameof(slugcat));
        return value;
    }
}
