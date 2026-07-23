using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace Kobbyist.ProgressionControls.Spike
{
    [FileLocation("Kobbyist.ProgressionControls.Spike")]
    [SettingsUIGroupOrder(kInterceptionGroup, kInjectionGroup, kDiagnosticsGroup)]
    [SettingsUIShowGroupName(kInterceptionGroup, kInjectionGroup, kDiagnosticsGroup)]
    public sealed class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kInterceptionGroup = "Interception";
        public const string kInjectionGroup = "Injection";
        public const string kDiagnosticsGroup = "Diagnostics";

        public Setting(IMod mod)
            : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(kSection, kInterceptionGroup)]
        public bool EnableQueueInterception { get; set; }

        [SettingsUISlider(min = 0, max = 100, step = 1, scalarMultiplier = 1)]
        [SettingsUISection(kSection, kInterceptionGroup)]
        public int VanillaXpMultiplierPercent { get; set; }

        [SettingsUISlider(min = 1, max = 1000000, step = 1, scalarMultiplier = 1)]
        [SettingsUISection(kSection, kInjectionGroup)]
        public int PopulationXpInjectionAmount { get; set; }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kInjectionGroup)]
        public bool InjectPopulationXp
        {
            set
            {
                if (value)
                {
                    Mod.RequestPopulationXp(PopulationXpInjectionAmount);
                }
            }
        }

        [SettingsUISection(kSection, kDiagnosticsGroup)]
        public bool LogNonEmptyBatches { get; set; }

        public override void SetDefaults()
        {
            EnableQueueInterception = false;
            VanillaXpMultiplierPercent = 100;
            PopulationXpInjectionAmount = 1;
            LogNonEmptyBatches = false;
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
                { m_Setting.GetSettingsLocaleID(), "Progression Controls — XP Spike" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Phase 0" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kInterceptionGroup), "Queue interception" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kInjectionGroup), "Explicit XP test" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kDiagnosticsGroup), "Diagnostics" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableQueueInterception)), "Enable XP queue interception" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableQueueInterception)), "When enabled, vanilla XP queued for the city is scaled before the game's XP system consumes it." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VanillaXpMultiplierPercent)), "Vanilla XP multiplier" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VanillaXpMultiplierPercent)), "Diagnostic multiplier applied to future queued vanilla XP. The passive default is 100%." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PopulationXpInjectionAmount)), "Population XP test amount" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.PopulationXpInjectionAmount)), "Amount submitted through the native XP queue when the confirmed test button is used." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InjectPopulationXp)), "Inject population XP" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InjectPopulationXp)), "Queues one explicit diagnostic award. This permanently changes XP in the current city." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.InjectPopulationXp)), "Run this only in a disposable test city. The injected XP becomes normal city state and is not automatically removed." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogNonEmptyBatches)), "Log non-empty XP batches" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogNonEmptyBatches)), "Writes one aggregate log entry for each intercepted batch that contains XP." },
            };
        }

        public void Unload()
        {
        }
    }
}
