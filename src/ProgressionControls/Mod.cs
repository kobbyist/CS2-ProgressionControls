using System;
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

        internal static KobbyistProgressionControlsSettings Settings { get; private set; }

        private IDictionarySource m_LocaleSource;
        private bool m_LocaleRegistered;
        private bool m_OptionsRegistered;

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            try
            {
                if (updateSystem == null)
                {
                    throw new ArgumentNullException(
                        nameof(updateSystem));
                }
                if (GameManager.instance == null ||
                    AssetDatabase.global == null ||
                    GameManager.instance.localizationManager == null)
                {
                    throw new InvalidOperationException(
                        "Required game services are unavailable");
                }

                var settings = new KobbyistProgressionControlsSettings(this);
                Settings = settings;

                AssetDatabase.global.LoadSettings(
                    SettingsAssetName,
                    settings,
                    new KobbyistProgressionControlsSettings(this));

                if (settings.NormalizeLoadedRules())
                {
                    settings.ApplyAndSave();
                }

                m_LocaleSource = new LocaleEN(settings);
                m_LocaleRegistered = true;
                GameManager.instance.localizationManager.AddSource(
                    LocaleId,
                    m_LocaleSource);

                m_OptionsRegistered = true;
                settings.RegisterInOptionsUI();

                Log.Info(
                    $"Loaded settings: enabled={settings.EnableCustomProgression}, manualClaims={settings.ManualMilestoneClaims}, preset={settings.Preset}");

                // This is the system's only registration. Running immediately
                // before XPSystem lets us transform queued gains, then append
                // population XP for the native consumer to process unchanged.
                updateSystem.UpdateBefore<
                    ProgressionControlSystem,
                    XPSystem>(
                    SystemUpdatePhase.ModificationEnd);
                updateSystem.UpdateAt<ManualProgressionUISystem>(
                    SystemUpdatePhase.UIUpdate);
            }
            catch (Exception exception)
            {
                Log.Error(
                    $"Progression Controls failed to load: {exception}");
                CleanupRegistrations();
                throw;
            }
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            CleanupRegistrations();
        }

        private void CleanupRegistrations()
        {
            var settings = Settings;
            if (m_OptionsRegistered && settings != null)
            {
                try
                {
                    settings.UnregisterInOptionsUI();
                }
                catch (Exception exception)
                {
                    Log.Warn(
                        $"Failed to unregister the Options entry: {exception}");
                }
            }
            m_OptionsRegistered = false;

            if (m_LocaleRegistered &&
                m_LocaleSource != null &&
                GameManager.instance != null &&
                GameManager.instance.localizationManager != null)
            {
                try
                {
                    GameManager.instance.localizationManager.RemoveSource(
                        LocaleId,
                        m_LocaleSource);
                }
                catch (Exception exception)
                {
                    Log.Warn(
                        $"Failed to remove the locale source: {exception}");
                }
            }

            m_LocaleRegistered = false;
            m_LocaleSource = null;
            Settings = null;
        }
    }
}
