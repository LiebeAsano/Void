using Menu.Remix.MixedUI;
using UnityEngine;
using VoidTemplate.Useful;
using static VoidTemplate.OptionInterface.OptionAccessors;
using static VoidTemplate.Useful.Utils;

namespace VoidTemplate.OptionInterface;

internal class VoidOptionInterface : global::OptionInterface
{
	const int marginBetweenVerticalElements = 30;
	static Color MediumGrey = new Color(0.66f, 0.64f, 0.70f);
	static Color CheatingColor = new Color(0.85f, 0.35f, 0.4f);

	public override void Initialize()
	{
		base.Initialize();
		Tabs = [new OpTab(this, "blahblahblah")];
		Tabs[0].GenerateBlock("~ Arena ~".TranslateStringComplex(), new Vector2(50, 550), options: [
            (cfgSaintArenaSpears, MediumGrey),
            (cfgSaintArenaAscension, MediumGrey),
			(cfgArenaAscensionStun, MediumGrey)
			]);
		Tabs[0].GenerateBlock("~ Assist ~".TranslateStringComplex(), new Vector2(50, 430), options: [
			(cfgGamepadController, MediumGrey),
			(cfgSimpleFood, MediumGrey),
			(cfgNoPermaDeath, CheatingColor),
			(cfgForceUnlockCampaign, CheatingColor),
			(cfgPermaDeathCycle, CheatingColor)
			]);
	}


}
