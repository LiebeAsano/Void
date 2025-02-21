namespace VoidTemplate.MenuTinkery;
public static class _MenuMeta
{
	public static void Startup()
	{
		DisablePassage.Hook();
		InitGame.Hook();
		MenuHooks.Hook();
		SelectScreenScenes.Hook();
		DreamAssociatedSound.Startup();
		JollyMenu.Hook();
        MenuStatisticsSound.Hook();
		MenuTitle.Hook();
        PermaDeathScreen.Hook();
		StopContinueButtonWhenAboutToDie.Hook();
    }
}
