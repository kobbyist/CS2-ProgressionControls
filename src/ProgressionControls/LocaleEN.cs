using System.Collections.Generic;
using Colossal;
using Kobbyist.ProgressionControls.Core;

namespace Kobbyist.ProgressionControls
{
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
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableCustomProgression)), "Enables population-based progression for future milestone XP. Turn this off to restore vanilla progression and make the mod fully dormant. Existing city XP is never recalculated." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Preset)), "Preset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Preset)), "Population Balanced\nCombines population-based progression with more of the original game. New all-time population records award XP, while 50% of vanilla XP is retained.\n\nPopulation Heavy\nMakes population growth the main source of milestone XP. New all-time population records award XP, while 25% of vanilla XP is retained.\n\nPopulation Only\nAwards milestone XP only when the city reaches a new all-time population record. Vanilla XP sources are ignored.\n\nCustom\nAppears automatically after you edit an XP rule.\n\nTurn off Enable custom progression to restore vanilla progression. Turn on Show Advanced to customize XP rules, record-detection responsiveness, and population XP notification frequency." },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationBalanced), "Population Balanced" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationHeavy), "Population Heavy" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.PopulationOnly), "Population Only" },
                { m_Setting.GetEnumValueLocaleID(ProgressionPreset.Custom), "Custom" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.XpPerResident)), "XP per new resident" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.XpPerResident)), "Population XP awarded per resident above the historical record. It is calculated after a city first loads. Use a period for decimals. Click Apply custom rules to recalculate the projected Megalopolis target and use the new value for future growth." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.MegalopolisPopulationTarget)), "Megalopolis population target" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.MegalopolisPopulationTarget)), "Projected population needed to earn the runtime Megalopolis XP requirement from population alone. Click Apply custom rules to recalculate XP per resident and use the new value for future growth." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VanillaXpPercentage)), "Vanilla XP multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VanillaXpPercentage)), "Percentage of positive XP already in the game's shared queue retained when Progression Controls runs. This includes base-game XP and can affect XP from other mods depending on update order. This mod's population XP is added afterward and is not scaled again. Click Apply custom rules to use the new value." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ApplyCustomRules)), "Apply custom rules" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ApplyCustomRules)), "Validates and applies all advanced XP rule values together. Until clicked, the running city continues using the last applied configuration." },
                { m_Setting.GetOptionGroupLocaleID(Setting.kAdvancedGroup), "Advanced controls" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PopulationEvaluationCadence)), "Population update responsiveness" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PopulationEvaluationCadence)), "Controls how quickly new population records are detected. Higher values reduce detection delay with a small increase in CPU work; total XP is unchanged." },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Low), "Low (16/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Moderate), "Moderate (64/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Balanced), "Balanced (256/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Fast), "Fast (1,024/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Responsive), "Responsive (4,096/day)" },
                { m_Setting.GetEnumValueLocaleID(PopulationEvaluationCadence.Immediate), "Immediate (16,384/day)" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PopulationXpAwardCadence)), "Population XP notification frequency" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PopulationXpAwardCadence)), "Batches earned population XP into at most this many awards and notifications per in-game day. Population records are still detected at the selected responsiveness, pending XP is saved, and total XP is unchanged." },
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
