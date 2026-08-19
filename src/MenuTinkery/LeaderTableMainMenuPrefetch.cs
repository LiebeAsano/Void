using Menu;
using System;
using System.Threading;
using System.Threading.Tasks;
using VoidTemplate.Useful;

namespace VoidTemplate.MenuTinkery;

public static class LeaderTableMainMenuPrefetch
{
    public static void Hook()
    {
        On.Menu.MainMenu.ctor += MainMenu_ctor;
    }

    private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);
        if (manager.rainWorld?.progression?.PlayingAsSlugcat is not SlugcatStats.Name slugcat) return;
        _ = PrefetchAsync(SlugcatApiNameMapper.GetApiName(slugcat));
    }

    private static async Task PrefetchAsync(string slugcatApiName)
    {
        try
        {
            await LeaderTableService.Instance.RefreshAsync(slugcatApiName, CancellationToken.None);
        }
        catch (Exception exception)
        {
            Utils.LogExErr($"Leader table prefetch failed: {exception.Message}");
        }
    }
}
