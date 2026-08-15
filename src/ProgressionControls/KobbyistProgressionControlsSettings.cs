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
    // CS2 1.6.0f1 saves a specific settings asset by the source object's
    // simple type name, so this name must remain globally unique.
    public sealed class KobbyistProgressionControlsSettings : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kRulesGroup = "Rules";
        public const string kAdvancedGroup = "Advanced";

        private float m_XpPerResident;
        private ProgressionPreset m_Preset;

        public KobbyistProgressionControlsSettings(IMod mod)
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

        [SettingsUISlider(
            min = 0f,
            max = 10f,
            step = 0.25f,
            scalarMultiplier = 1f)]
        [SettingsUICustomFormat(fractionDigits = 2)]
        [SettingsUISection(kSection, kRulesGroup)]
        [SettingsUIAdvanced]
        public float XpPerResident
        {
            get => m_XpPerResident;
            set => m_XpPerResident = value;
        }

        [SettingsUISlider(
            min = 0,
            max = 100,
            step = 1,
            scalarMultiplier = 1)]
        [SettingsUISection(kSection, kRulesGroup)]
        [SettingsUIAdvanced]
        public int VanillaXpPercentage { get; set; }

        [SettingsUIHidden]
        public ProgressionPreset AppliedPreset { get; set; }

        [SettingsUIHidden]
        public float AppliedXpPerResident { get; set; }

        [SettingsUIHidden]
        public int AppliedVanillaXpPercentage { get; set; }

        [SettingsUISection(kSection, kAdvancedGroup)]
        [SettingsUIAdvanced]
        public PopulationEvaluationCadence PopulationEvaluationCadence
        {
            get;
            set;
        }

        [SettingsUISection(kSection, kAdvancedGroup)]
        [SettingsUIAdvanced]
        public PopulationXpAwardCadence PopulationXpAwardCadence
        {
            get;
            set;
        }

        public override void SetDefaults()
        {
            EnableCustomProgression = true;
            Preset = ProgressionPreset.PopulationHeavy;
            PopulationEvaluationCadence =
                PopulationEvaluationCadence.Responsive;
            PopulationXpAwardCadence =
                PopulationXpAwardCadence.Regular;
        }

        public override void Apply()
        {
            ApplyRequestedRules();
            base.Apply();
        }

        internal bool NormalizeLoadedRules()
        {
            if (Preset != ProgressionPreset.Custom)
            {
                return ApplyPresetRules(Preset);
            }

            var requested = new ProgressionSettingsState(
                ProgressionPreset.Custom,
                AppliedXpPerResident,
                AppliedVanillaXpPercentage);
            if (ProgressionSettingsResolver.TryResolveInitial(
                requested,
                out _,
                out var normalized))
            {
                return ApplyResolvedRules(normalized);
            }

            return ApplyPresetRules(ProgressionPreset.PopulationHeavy);
        }

        private void ApplyRequestedRules()
        {
            var applied = new ProgressionSettingsState(
                AppliedPreset,
                AppliedXpPerResident,
                AppliedVanillaXpPercentage);
            if (!ProgressionSettingsResolver.TryResolveInitial(
                applied,
                out var currentConfiguration,
                out var normalizedApplied))
            {
                ApplyPresetRules(ProgressionPreset.PopulationHeavy);
                return;
            }

            var requested = new ProgressionSettingsState(
                Preset,
                XpPerResident,
                VanillaXpPercentage);
            if (ProgressionSettingsResolver.TryResolveChange(
                normalizedApplied,
                requested,
                currentConfiguration,
                out _,
                out var normalized))
            {
                ApplyResolvedRules(normalized);
                return;
            }

            ApplyResolvedRules(normalizedApplied);
        }

        private bool ApplyPresetRules(ProgressionPreset preset)
        {
            if (!ProgressionConfiguration.TryFromPreset(
                preset,
                out var configuration))
            {
                return false;
            }

            var rate = (float)configuration.XpPerResident;
            var changed =
                m_XpPerResident != rate ||
                AppliedPreset != preset ||
                AppliedXpPerResident != rate ||
                AppliedVanillaXpPercentage !=
                    configuration.VanillaXpPercentage ||
                VanillaXpPercentage !=
                    configuration.VanillaXpPercentage;

            m_Preset = preset;
            AppliedPreset = preset;
            m_XpPerResident = rate;
            VanillaXpPercentage =
                configuration.VanillaXpPercentage;
            AppliedXpPerResident = rate;
            AppliedVanillaXpPercentage =
                configuration.VanillaXpPercentage;
            return changed;
        }

        internal bool ApplyResolvedRules(
            ProgressionSettingsState normalized)
        {
            var rate = (float)normalized.XpPerResident;
            var changed =
                m_Preset != normalized.Preset ||
                m_XpPerResident != rate ||
                VanillaXpPercentage !=
                    normalized.VanillaXpPercentage ||
                AppliedPreset != normalized.Preset ||
                AppliedXpPerResident != rate ||
                AppliedVanillaXpPercentage !=
                    normalized.VanillaXpPercentage;

            m_Preset = normalized.Preset;
            m_XpPerResident = rate;
            VanillaXpPercentage = normalized.VanillaXpPercentage;
            AppliedPreset = normalized.Preset;
            AppliedXpPerResident = rate;
            AppliedVanillaXpPercentage =
                normalized.VanillaXpPercentage;
            return changed;
        }
    }
}
