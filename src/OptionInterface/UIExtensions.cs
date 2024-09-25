using Menu.Remix.MixedUI;
using VoidTemplate.Useful;
using UnityEngine;

namespace VoidTemplate.OptionInterface;

internal static class UIExtensions
{
	/// <summary>
	/// generates checkbox at specified position bound to configurable.
	/// the position specified refers to the center of checkbox
	/// </summary>
	/// <param name="opTab"></param>
	/// <param name="configurable"></param>
	/// <param name="pos"></param>
	public static void GenerateCheckbox(this OpTab opTab, Configurable<bool> configurable, Vector2 pos)
	{
		OpCheckBox opCheckBox = new OpCheckBox(configurable, pos)
		{
			description = configurable.info.description.TranslateStringComplex()
		};
		OpLabel opLabel = new(pos: pos + new Vector2(30, -3),
			size: new Vector2(240f, 30f),
			text: (configurable.info.Tags.Length > 0 ? configurable.info.Tags[0] as string : "").TranslateStringComplex(),
			FLabelAlignment.Left);
		opTab.AddItems([opCheckBox, opLabel]);
	}
	public static void GenerateColoredCheckbox(this OpTab opTab, Configurable<bool> configurable, Vector2 pos, Color color)
	{
        OpCheckBox opCheckBox = new OpCheckBox(configurable, pos)
        {
            description = configurable.info.description.TranslateStringComplex()
        };
        OpLabel opLabel = new(pos: pos + new Vector2(30, -3),
            size: new Vector2(240f, 30f),
            text: (configurable.info.Tags.Length > 0 ? configurable.info.Tags[0] as string : "").TranslateStringComplex(),
            FLabelAlignment.Left);
		opCheckBox.colorEdge = color;
		opLabel.color = color;
        opTab.AddItems([opCheckBox, opLabel]);
    }
	public static void GenerateBigText(this OpTab opTab, string text, Vector2 pos)
	{
		OpLabel opLabel = new(pos: pos,
			text: text,
			size: new Vector2(260, 30),
			bigText: true);
		opTab.AddItems(opLabel);

	}
}
