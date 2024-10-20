namespace VoidTemplate.MenuTinkery;
public static class _MenuMeta
{
	public static void Startup()
	{
		DisablePassage.Hook();
		MenuHooks.Hook();
		SelectScreenScenes.Hook();
		DreamAssociatedSound.Startup();
        MenuStatisticsSound.Hook();

    }
}
