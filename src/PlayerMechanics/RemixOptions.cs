using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;

namespace VoidTemplate.PlayerMechanics;
    // Класс для настроек мода
    public class RemixOptions : OptionInterface
    {
        public static RemixOptions Instance { get; private set; }

        public readonly Configurable<bool> EnableSaintArenaKarma;
        public readonly Configurable<bool> EnableSaintArenaSpears;

        public RemixOptions()
        {
            Instance = this;

            EnableSaintArenaKarma = config.Bind("EnableSaintArenaKarma", true);
            EnableSaintArenaSpears = config.Bind("EnableSaintArenaSpears", true);

            InitializeOptions();
        }

        private const float SPACING = 20.0f;
        private readonly List<Configurable<bool>> CheckBoxConfigurables = new();

        private void InitializeOptions()
        {
            Tabs = new OpTab[1];
            Tabs[0] = new OpTab(this, "Настройки");

            AddCheckBoxOption("Включить Saint Arena Karma", EnableSaintArenaKarma);
            AddCheckBoxOption("Включить Saint Arena Spears", EnableSaintArenaSpears);
        }

        private void AddCheckBoxOption(string label, Configurable<bool> setting)
        {
            CheckBoxConfigurables.Add(setting);
            Tabs[0].AddItems(new OpCheckBox(setting, new Vector2(100, 500 - CheckBoxConfigurables.Count * SPACING)));
            Tabs[0].AddItems(new OpLabel(140, 500 - CheckBoxConfigurables.Count * SPACING, label));
        }

    }