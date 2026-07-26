using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;

namespace Kobbyist.ProgressionControls
{
    public sealed class Mod : IMod
    {
        private const string LoggerName = "Kobbyist.ProgressionControls";
        private const string LocaleId = "en-US";

        internal const string SettingsAssetName =
            "Kobbyist_ProgressionControls";

        public static readonly ILog Log = LogManager
            .GetLogger(LoggerName)
            .SetShowsErrorsInUI(false);

        internal static Setting Settings { get; private set; }

        private IDictionarySource m_LocaleSource;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();

            m_LocaleSource = new LocaleEN(Settings);
            GameManager.instance.localizationManager.AddSource(
                LocaleId,
                m_LocaleSource);

            AssetDatabase.global.LoadSettings(
                SettingsAssetName,
                Settings,
                new Setting(this));

            if (Settings.ReapplyPresetRules())
            {
                Settings.ApplyAndSave();
            }

            Log.Info(
                $"Loaded settings: enabled={Settings.EnableCustomProgression}, preset={Settings.Preset}");

            // This is the system's only registration. Running immediately
            // before XPSystem lets us transform vanilla gains, then append
            // population XP for the native consumer to process unchanged.
            updateSystem.UpdateBefore<ProgressionControlSystem, XPSystem>(
                SystemUpdatePhase.ModificationEnd);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));

            if (m_LocaleSource != null)
            {
                GameManager.instance.localizationManager.RemoveSource(
                    LocaleId,
                    m_LocaleSource);
                m_LocaleSource = null;
            }

            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }
        }
    }
}
