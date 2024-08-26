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
		Tabs[0].GenerateCheckbox(cfgSaintArenaAscension, new Vector2(600,600));
		Tabs[0].GenerateCheckbox(cfgSaintArenaSpears, new Vector2(0, 0));
		Tabs[0].GenerateBigText("~ Arena ~".TranslateStringComplex(), new Vector2(70, 550));
	}


}
