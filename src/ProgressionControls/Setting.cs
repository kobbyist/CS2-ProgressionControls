using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Kobbyist.ProgressionControls.Core;

namespace Kobbyist.ProgressionControls
{
    public enum PopulationEvaluationCadence
    {
        Low = 16,
        Moderate = 64,
        Balanced = 256,
        Fast = 1024,
        Responsive = 4096,
        Immediate = 16384,
    }

    [FileLocation(Mod.SettingsAssetName)]
    [SettingsUIGroupOrder(kGeneralGroup, kRulesGroup, kAdvancedGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kRulesGroup, kAdvancedGroup)]
    public sealed class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kRulesGroup = "Rules";
        public const string kAdvancedGroup = "Advanced";

        private string m_XpPerResident;
        private string m_MegalopolisPopulationTarget;

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool EnableCustomProgression { get; set; }

        [SettingsUISection(kSection, kGeneralGroup)]
        public ProgressionPreset Preset { get; set; }

        [SettingsUITextInput]
        [SettingsUISection(kSection, kRulesGroup)]
        public string XpPerResident
        {
            get => m_XpPerResident;
            set
            {
                m_XpPerResident = value;
                PopulationRateInputMode =
                    PopulationRateInputMode.XpPerResident;
            }
        }

        [SettingsUITextInput]
        [SettingsUISection(kSection, kRulesGroup)]
        public string MegalopolisPopulationTarget
        {
            get => m_MegalopolisPopulationTarget;
            set
            {
                m_MegalopolisPopulationTarget = value;
                PopulationRateInputMode =
                    PopulationRateInputMode.MegalopolisTarget;
            }
        }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1)]
        [SettingsUISection(kSection, kRulesGroup)]
        public int VanillaXpPercentage { get; set; }

        [SettingsUIHidden]
        public bool PopulationXpEnabled { get; set; }

        [SettingsUIHidden]
        public PopulationRateInputMode PopulationRateInputMode { get; set; }

        [SettingsUISection(kSection, kAdvancedGroup)]
        [SettingsUIAdvanced]
        public PopulationEvaluationCadence PopulationEvaluationCadence { get; set; }

        public override void SetDefaults()
        {
            EnableCustomProgression = true;
            Preset = ProgressionPreset.PopulationHeavy;
            XpPerResident = "0";
            MegalopolisPopulationTarget =
                ProgressionConfiguration
                    .DefaultMegalopolisPopulationTarget
                    .ToString();
            VanillaXpPercentage =
                ProgressionConfiguration.DefaultVanillaXpPercentage;
            PopulationXpEnabled = true;
            PopulationRateInputMode =
                PopulationRateInputMode.MegalopolisTarget;
            PopulationEvaluationCadence =
                PopulationEvaluationCadence.Responsive;
        }
    }

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Progression Controls" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGeneralGroup), "Progression" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kRulesGroup), "XP rules" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableCustomProgression)), "Enable custom progression" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableCustomProgression)), "Enables Progression Controls for future milestone XP. Existing city XP is never recalculated." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Preset)), "Preset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Preset)), "Applies a complete built-in rule set. Editing any rule changes the selection to Custom." },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.Vanilla), "Vanilla" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationHeavy), "Population Heavy" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationOnly), "Population Only" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.Custom), "Custom" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.XpPerResident)), "XP per new resident" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.XpPerResident)), "Population XP awarded per resident above the historical record. Use a period for decimals. Editing this recalculates the projected Megalopolis target and affects future growth only." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MegalopolisPopulationTarget)), "Megalopolis population target" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MegalopolisPopulationTarget)), "Projected population needed to earn the runtime Megalopolis XP requirement from population alone. Editing this recalculates XP per resident and affects future growth only." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VanillaXpPercentage)), "Vanilla XP multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VanillaXpPercentage)), "Percentage of future vanilla XP retained. 0% makes population the only enabled source; 100% preserves vanilla XP." },
                { m_Setting.GetOptionGroupLocaleID(Setting.kAdvancedGroup), "Advanced controls" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PopulationEvaluationCadence)), "Population update responsiveness" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PopulationEvaluationCadence)), "Controls how quickly new population records award XP. Higher values reduce delay with a small increase in CPU work; total XP is unchanged." },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Low), "Low (16/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Moderate), "Moderate (64/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Balanced), "Balanced (256/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Fast), "Fast (1,024/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Responsive), "Responsive (4,096/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Immediate), "Immediate (16,384/day)" },
            };
        }

        public void Unload()
        {
        }
    }
}
