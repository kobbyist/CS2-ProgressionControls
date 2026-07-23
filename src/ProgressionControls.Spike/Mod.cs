using System;
using System.Threading;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;

namespace Kobbyist.ProgressionControls.Spike
{
    public sealed class Mod : IMod
    {
        private const string LoggerName = "Kobbyist.ProgressionControls.Spike";
        internal const string SettingsAssetName =
            "Kobbyist_ProgressionControls_Spike";
        private static int s_PendingPopulationXp;

        public static readonly ILog Log = LogManager
            .GetLogger(LoggerName)
            .SetShowsErrorsInUI(false);

        internal static Setting Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource(
                "en-US",
                new LocaleEN(Settings));
            AssetDatabase.global.LoadSettings(
                SettingsAssetName,
                Settings,
                new Setting(this));
            Log.Info(
                $"Loaded settings: interception={Settings.EnableQueueInterception}, multiplier={Settings.VanillaXpMultiplierPercent}%, batchLogging={Settings.LogNonEmptyBatches}");

            // This overload is the system's only registration. It anchors the
            // interceptor directly before the vanilla XP consumer.
            updateSystem.UpdateBefore<XPQueueInterceptionSystem, XPSystem>(
                SystemUpdatePhase.ModificationEnd);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));

            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }

            Interlocked.Exchange(ref s_PendingPopulationXp, 0);
        }

        internal static void RequestPopulationXp(int amount)
        {
            var safeAmount = Math.Max(1, amount);
            Interlocked.Exchange(ref s_PendingPopulationXp, safeAmount);
            Log.Info($"Queued explicit population XP test: {safeAmount}");
        }

        internal static int TakeRequestedPopulationXp()
        {
            return Interlocked.Exchange(ref s_PendingPopulationXp, 0);
        }
    }
}
