﻿using CollapseLauncher.GameSettings.Base;
using CollapseLauncher.Interfaces;

namespace CollapseLauncher.GameSettings.Genshin
{
    internal class GenshinSettings : SettingsBase
    {
        public GeneralData           SettingsGeneralData      { get; set; }
        public VisibleBackground     SettingVisibleBackground { get; set; }
        public WindowsHDR            SettingsWindowsHDR       { get; set; }

        public GenshinSettings(IGameVersionCheck GameVersionManager)
            : base(GameVersionManager)
        {
            // Initialize and Load Settings
            InitializeSettings();
        }

        public override void InitializeSettings()
        {
            // Load Settings
            base.InitializeSettings();
            SettingsScreen           = ScreenManager.Load();
            SettingVisibleBackground = VisibleBackground.Load();
            SettingsWindowsHDR       = WindowsHDR.Load();
        }

        public override void ReloadSettings()
        {
            // To ease up resource and prevent bad JSON locking up launcher
            SettingsGeneralData = GeneralData.Load();
            InitializeSettings();
        } 

        public override void SaveSettings()
        {
            // Save Settings
            base.SaveSettings();
            SettingsScreen.Save();
            SettingsGeneralData.Save();
            SettingVisibleBackground.Save();
            SettingsWindowsHDR.Save();
        }

        public override IGameSettingsUniversal AsIGameSettingsUniversal() => this;
    }
}
