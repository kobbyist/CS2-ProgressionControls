using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

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
    [SettingsUIGroupOrder(kGeneralGroup, kAdvancedGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kAdvancedGroup)]
    public sealed class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kAdvancedGroup = "Advanced";

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool EnableCustomProgression { get; set; }

        [SettingsUISection(kSection, kAdvancedGroup)]
        [SettingsUIAdvanced]
        public PopulationEvaluationCadence PopulationEvaluationCadence { get; set; }

        public override void SetDefaults()
        {
            EnableCustomProgression = true;
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableCustomProgression)), "Enable custom progression" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableCustomProgression)), "Enables Progression Controls for future milestone XP. Existing city XP is never recalculated." },
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
