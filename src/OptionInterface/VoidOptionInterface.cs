using Menu.Remix.MixedUI;
using VoidTemplate.Useful;
using UnityEngine;
using RWCustom;
using Menu.Remix;
using static VoidTemplate.Useful.Utils;
using static VoidTemplate.OptionInterface.OptionAccessors;
using System;

namespace VoidTemplate.OptionInterface;

internal class VoidOptionInterface : global::OptionInterface
{
	const int marginBetweenVerticalElements = 30;
	public override void Initialize()
	{
		base.Initialize();
		Tabs = [new OpTab(this, "blahblahblah")];
		Tabs[0].GenerateBigText("~ Arena ~".TranslateStringComplex(), new Vector2(60, 550));
		Tabs[0].GenerateCheckbox(cfgSaintArenaAscension, new Vector2(50,550-marginBetweenVerticalElements));
		Tabs[0].GenerateCheckbox(cfgSaintArenaSpears, new Vector2(50, 550-marginBetweenVerticalElements*2));

		Tabs[0].GenerateCheckbox(cfgSimpleFood, new Vector2(50,550 - marginBetweenVerticalElements*4));
	}


}
