using System.Globalization;
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

    public enum PopulationXpAwardCadence
    {
        Daily = 1,
        Occasional = 4,
        Regular = 16,
        Frequent = 64,
        VeryFrequent = 256,
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
        private ProgressionPreset m_Preset;
        private string m_MegalopolisPopulationTarget;

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool EnableCustomProgression { get; set; }

        [SettingsUISection(kSection, kGeneralGroup)]
        public ProgressionPreset Preset
        {
            get => m_Preset;
            set
            {
                m_Preset = value;
                ApplyPresetRules(value);
            }
        }

        [SettingsUITextInput]
        [SettingsUISection(kSection, kRulesGroup)]
        [SettingsUIAdvanced]
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
        [SettingsUIAdvanced]
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
        [SettingsUIAdvanced]
        public int VanillaXpPercentage { get; set; }

        [SettingsUIButton]
        [SettingsUISection(kSection, kRulesGroup)]
        [SettingsUIAdvanced]
        public bool ApplyCustomRules
        {
            set
            {
                if (value)
                {
                    m_ApplyCustomRulesRequested = true;
                }
            }
        }

        [SettingsUIHidden]
        public ProgressionPreset AppliedPreset { get; set; }

        [SettingsUIHidden]
        public string AppliedXpPerResident { get; set; }

        [SettingsUIHidden]
        public string AppliedMegalopolisPopulationTarget { get; set; }

        [SettingsUIHidden]
        public int AppliedVanillaXpPercentage { get; set; }

        [SettingsUIHidden]
        public PopulationRateInputMode AppliedPopulationRateInputMode
        {
            get;
            set;
        }

        [SettingsUIHidden]
        public PopulationRateInputMode PopulationRateInputMode { get; set; }

        [SettingsUIHidden]
        public int MegalopolisXpRequirement { get; set; }

        [SettingsUISection(kSection, kAdvancedGroup)]
        [SettingsUIAdvanced]
        public PopulationEvaluationCadence PopulationEvaluationCadence { get; set; }

        [SettingsUISection(kSection, kAdvancedGroup)]
        [SettingsUIAdvanced]
        public PopulationXpAwardCadence PopulationXpAwardCadence { get; set; }

        private bool m_ApplyCustomRulesRequested;

        public override void SetDefaults()
        {
            EnableCustomProgression = true;
            MegalopolisXpRequirement = 0;
            Preset = ProgressionPreset.PopulationHeavy;
            PopulationEvaluationCadence =
                PopulationEvaluationCadence.Responsive;
            PopulationXpAwardCadence =
                PopulationXpAwardCadence.Regular;
        }

        internal bool ConsumeApplyCustomRulesRequest()
        {
            if (!m_ApplyCustomRulesRequested)
            {
                return false;
            }

            m_ApplyCustomRulesRequested = false;
            return true;
        }

        internal bool ReapplyPresetRules()
        {
            return ApplyPresetRules(Preset);
        }

        internal bool SetMegalopolisXpRequirement(int requirement)
        {
            if (requirement <= 0 ||
                requirement == MegalopolisXpRequirement)
            {
                return false;
            }

            MegalopolisXpRequirement = requirement;
            ApplyPresetRules(Preset);
            return true;
        }

        private bool ApplyPresetRules(ProgressionPreset preset)
        {
            if (preset == ProgressionPreset.Custom)
            {
                return false;
            }

            var effectiveRequirement =
                MegalopolisXpRequirement > 0
                    ? MegalopolisXpRequirement
                    : 1;
            if (!ProgressionConfiguration.TryFromPreset(
                preset,
                effectiveRequirement,
                out var configuration))
            {
                return false;
            }

            var rate = MegalopolisXpRequirement > 0
                ? configuration.XpPerResident.ToString(
                    "G29",
                    CultureInfo.InvariantCulture)
                : string.Empty;
            var target = ProgressionConfiguration
                .DefaultMegalopolisPopulationTarget
                .ToString(CultureInfo.InvariantCulture);
            var changed =
                !string.Equals(
                    m_XpPerResident,
                    rate,
                    System.StringComparison.Ordinal) ||
                !string.Equals(
                    m_MegalopolisPopulationTarget,
                    target,
                    System.StringComparison.Ordinal) ||
                AppliedPreset != preset ||
                !string.Equals(
                    AppliedXpPerResident,
                    rate,
                    System.StringComparison.Ordinal) ||
                !string.Equals(
                    AppliedMegalopolisPopulationTarget,
                    target,
                    System.StringComparison.Ordinal) ||
                AppliedVanillaXpPercentage !=
                    configuration.VanillaXpPercentage ||
                AppliedPopulationRateInputMode !=
                    PopulationRateInputMode.MegalopolisTarget ||
                VanillaXpPercentage !=
                    configuration.VanillaXpPercentage ||
                PopulationRateInputMode !=
                    PopulationRateInputMode.MegalopolisTarget;

            m_Preset = preset;
            AppliedPreset = preset;
            m_XpPerResident = rate;
            m_MegalopolisPopulationTarget = target;
            VanillaXpPercentage =
                configuration.VanillaXpPercentage;
            PopulationRateInputMode =
                PopulationRateInputMode.MegalopolisTarget;
            AppliedXpPerResident = rate;
            AppliedMegalopolisPopulationTarget = target;
            AppliedVanillaXpPercentage =
                configuration.VanillaXpPercentage;
            AppliedPopulationRateInputMode =
                PopulationRateInputMode.MegalopolisTarget;
            return changed;
        }

        internal bool ApplyResolvedRules(
            ProgressionSettingsState normalized)
        {
            var changed =
                m_Preset != normalized.Preset ||
                !string.Equals(
                    m_XpPerResident,
                    normalized.XpPerResident,
                    System.StringComparison.Ordinal) ||
                !string.Equals(
                    m_MegalopolisPopulationTarget,
                    normalized.MegalopolisPopulationTarget,
                    System.StringComparison.Ordinal) ||
                VanillaXpPercentage !=
                    normalized.VanillaXpPercentage ||
                PopulationRateInputMode != normalized.RateInputMode ||
                AppliedPreset != normalized.Preset ||
                !string.Equals(
                    AppliedXpPerResident,
                    normalized.XpPerResident,
                    System.StringComparison.Ordinal) ||
                !string.Equals(
                    AppliedMegalopolisPopulationTarget,
                    normalized.MegalopolisPopulationTarget,
                    System.StringComparison.Ordinal) ||
                AppliedVanillaXpPercentage !=
                    normalized.VanillaXpPercentage ||
                AppliedPopulationRateInputMode !=
                    normalized.RateInputMode;

            m_Preset = normalized.Preset;
            m_XpPerResident = normalized.XpPerResident;
            m_MegalopolisPopulationTarget =
                normalized.MegalopolisPopulationTarget;
            VanillaXpPercentage = normalized.VanillaXpPercentage;
            PopulationRateInputMode = normalized.RateInputMode;
            AppliedPreset = normalized.Preset;
            AppliedXpPerResident = normalized.XpPerResident;
            AppliedMegalopolisPopulationTarget =
                normalized.MegalopolisPopulationTarget;
            AppliedVanillaXpPercentage =
                normalized.VanillaXpPercentage;
            AppliedPopulationRateInputMode = normalized.RateInputMode;
            return changed;
        }
    }
}
