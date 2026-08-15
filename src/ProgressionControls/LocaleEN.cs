using System.Collections.Generic;
using Colossal;
using Kobbyist.ProgressionControls.Core;

namespace Kobbyist.ProgressionControls
{
    public sealed class LocaleEN : IDictionarySource
    {
        private readonly KobbyistProgressionControlsSettings m_Setting;

        public LocaleEN(KobbyistProgressionControlsSettings setting)
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
                { m_Setting.GetOptionTabLocaleID(KobbyistProgressionControlsSettings.kSection), "General" },
                { m_Setting.GetOptionGroupLocaleID(KobbyistProgressionControlsSettings.kGeneralGroup), "Progression" },
                { m_Setting.GetOptionGroupLocaleID(KobbyistProgressionControlsSettings.kRulesGroup), "XP rules" },
                { m_Setting.GetOptionLabelLocaleID(nameof(KobbyistProgressionControlsSettings.EnableCustomProgression)), "Enable custom progression" },
                { m_Setting.GetOptionDescLocaleID(nameof(KobbyistProgressionControlsSettings.EnableCustomProgression)), "Enables population-based progression for future milestone XP. Turn this off to restore vanilla progression and make the mod fully dormant. Existing city XP is never recalculated." },
                { m_Setting.GetOptionLabelLocaleID(nameof(KobbyistProgressionControlsSettings.Preset)), "Preset" },
                { m_Setting.GetOptionDescLocaleID(nameof(KobbyistProgressionControlsSettings.Preset)), "Population Balanced\nAwards 1.5 XP per resident above the population record and retains 50% of vanilla XP. This keeps more of the original progression mix.\n\nPopulation Heavy\nAwards 1.5 XP per resident above the population record and retains 25% of vanilla XP. Population growth becomes the main progression source.\n\nPopulation Only\nAwards 1.5 XP per resident above the population record and ignores vanilla XP.\n\nCustom\nAppears automatically after you edit an XP rule.\n\nTurn off Enable custom progression to restore vanilla progression. Turn on Show Advanced to customize XP rules, record-detection responsiveness, and population XP notification frequency." },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationBalanced), "Population Balanced" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationHeavy), "Population Heavy" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationOnly), "Population Only" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.Custom), "Custom" },
                { m_Setting.GetOptionLabelLocaleID(nameof(KobbyistProgressionControlsSettings.XpPerResident)), "XP per new resident" },
                { m_Setting.GetOptionDescLocaleID(nameof(KobbyistProgressionControlsSettings.XpPerResident)), "Population XP awarded per resident above the historical record. Choose 0 to 10 in 0.25 increments; 1.5 matches the nominal vanilla population rate. Changes apply automatically to future growth and switch the preset to Custom." },
                { m_Setting.GetOptionLabelLocaleID(nameof(KobbyistProgressionControlsSettings.VanillaXpPercentage)), "Vanilla XP multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(KobbyistProgressionControlsSettings.VanillaXpPercentage)), "Percentage of positive XP already in the game's shared queue retained when Progression Controls runs. This includes base-game XP and can affect XP from other mods depending on update order. This mod's population XP is added afterward and is not scaled again. Changes apply automatically and switch the preset to Custom." },
                { m_Setting.GetOptionGroupLocaleID(KobbyistProgressionControlsSettings.kAdvancedGroup), "Advanced controls" },
                { m_Setting.GetOptionLabelLocaleID(nameof(KobbyistProgressionControlsSettings.PopulationEvaluationCadence)), "Population update responsiveness" },
                { m_Setting.GetOptionDescLocaleID(nameof(KobbyistProgressionControlsSettings.PopulationEvaluationCadence)), "Controls how quickly new population records are detected. Higher values reduce detection delay with a small increase in CPU work; total XP is unchanged." },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Low), "Low (16/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Moderate), "Moderate (64/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Balanced), "Balanced (256/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Fast), "Fast (1,024/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Responsive), "Responsive (4,096/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Immediate), "Immediate (16,384/day)" },
                { m_Setting.GetOptionLabelLocaleID(nameof(KobbyistProgressionControlsSettings.PopulationXpAwardCadence)), "Population XP notification frequency" },
                { m_Setting.GetOptionDescLocaleID(nameof(KobbyistProgressionControlsSettings.PopulationXpAwardCadence)), "Batches earned population XP into at most this many awards and notifications per in-game day. Population records are still detected at the selected responsiveness, pending XP is saved, and total XP is unchanged." },
                { m_Setting.GetEnumValueLocaleID(PopulationXpAwardCadence.Daily), "Daily (1/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationXpAwardCadence.Occasional), "Occasional (4/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationXpAwardCadence.Regular), "Regular (16/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationXpAwardCadence.Frequent), "Frequent (64/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationXpAwardCadence.VeryFrequent), "Very frequent (256/day)" },
            };
        }

        public void Unload()
        {
        }
    }
}
