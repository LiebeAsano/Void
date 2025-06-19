using Menu.Remix.MixedUI;
using System;
using UnityEngine;
using VoidTemplate.Useful;

namespace VoidTemplate.OptionInterface;

public static class UIExtensions
{
	/// <summary>
	/// generates checkbox at specified position bound to configurable.
	/// the position specified refers to the center of checkbox
	/// </summary>
	/// <param name="opTab"></param>
	/// <param name="configurable"></param>
	/// <param name="pos"></param>
	private static void GenerateColoredCheckbox(this OpTab opTab, Configurable<bool> configurable, Vector2 pos,
		Color color)
	{
		OpCheckBox opCheckBox = new OpCheckBox(configurable, pos)
		{
			description = configurable.info.description.TranslateStringComplex()
		};
		OpLabel opLabel = new(pos: pos + new Vector2(30, -3),
			size: new Vector2(240f, 30f),
			text: (configurable.info.Tags.Length > 0 ? configurable.info.Tags[0] as string : "")
			.TranslateStringComplex(),
			FLabelAlignment.Left);
		opCheckBox.colorEdge = color;
		opLabel.color = color;
		opTab.AddItems([opCheckBox, opLabel]);
	}

	private const int sizeOfTab = 60;
	private static void GenerateColoredIntField(this OpTab opTab, Configurable<int> configurable, Vector2 pos,
		Color color)
	{
		
		OpUpdown opUpdown = new OpUpdown(configurable, pos, sizeOfTab)
		{
			description = configurable.info.description.TranslateStringComplex()
		};
		OpLabel opLabel = new(pos: pos + new Vector2(sizeOfTab + 6, 1),
			size: new Vector2(240f, 30f),
			text: (configurable.info.Tags.Length > 0 ? configurable.info.Tags[0] as string : "")
			.TranslateStringComplex(),
			FLabelAlignment.Left);
        opLabel.color = color;
        opUpdown.colorEdge = color;
		opUpdown.colorText = color;
		opTab.AddItems([opUpdown, opLabel]);

	}

	static void GenerateBigText(this OpTab opTab, string text, Vector2 pos)
	{
		OpLabel opLabel = new(pos: pos,
			text: text,
			size: new Vector2(260, 30),
			bigText: true);
		opTab.AddItems(opLabel);
	}

	const int marginBetweenVerticalElements = 30;

	/// <summary>
	/// Generates block of big label and config parameters. Starting point - top left part of rectangle, label gets additional offset
	/// </summary>
	/// <param name="opTab"></param>
	/// <param name="label"></param>
	/// <param name="startingPosition"></param>
	/// <param name="labelHorizontalOffset"></param>
	/// <param name="options"></param>
	/// <exception cref="NotImplementedException"></exception>
	public static void GenerateBlock(this OpTab opTab, string label, Vector2 startingPosition,
		int labelHorizontalOffset = 10, params (ConfigurableBase, Color)[] options)
	{
		opTab.GenerateBigText(label, startingPosition + new Vector2(labelHorizontalOffset, 0));
		int offset = marginBetweenVerticalElements;
		foreach (var configColorPair in options)
		{
			if (configColorPair.Item1 is Configurable<bool> boolcfg)
			{
				opTab.GenerateColoredCheckbox(boolcfg, startingPosition - new Vector2(0, offset),
					configColorPair.Item2);
			}
			else if (configColorPair.Item1 is Configurable<int> intcfg)
			{
				offset += 5;
                opTab.GenerateColoredIntField(intcfg, startingPosition - new Vector2(0, offset), configColorPair.Item2);
			}
			else
				throw new NotImplementedException(
					$"Generating OI block failed due to unsupported configurable type passed in. The passed config is {configColorPair.Item1.key}" +
					$" and its type is {configColorPair.Item1.GetType()}");

			offset += marginBetweenVerticalElements;
		}
	}
}