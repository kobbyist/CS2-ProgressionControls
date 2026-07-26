using System;
using System.Collections.Generic;
using System.IO;
using Colossal.PSI.Environment;
using Colossal.Serialization.Entities;
using Game;
using Game.City;
using Game.Prefabs;
using Game.PSI;
using Game.SceneFlow;
using Game.Simulation;
using Kobbyist.ProgressionControls.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Kobbyist.ProgressionControls
{
    public partial class ProgressionControlSystem : GameSystemBase
    {
        private const int DefaultPopulationEvaluationsPerDay =
            (int)PopulationEvaluationCadence.Responsive;
        private const int MinimumPopulationEvaluationInterval = 16;
        private const int InitializationWarningInterval =
            TimeSystem.kTicksPerDay /
            (int)PopulationEvaluationCadence.Low;

        private readonly List<XPGain> m_PendingVanillaXp =
            new List<XPGain>();
        private readonly VanillaXpScaler m_VanillaXpScaler =
            new VanillaXpScaler();

        private CitySystem m_CitySystem;
        private SimulationSystem m_SimulationSystem;
        private XPSystem m_XPSystem;
        private EntityQuery m_MilestoneQuery;
        private ProgressionConfiguration m_Configuration;
        private PopulationProgressionTracker m_PopulationTracker;
        private ProgressionStateStore m_StateStore;
        private ProgressionStateSnapshot m_PendingSaveSnapshot;
        private GameMode m_GameMode;
        private Guid m_CityId;
        private int m_MegalopolisXpRequirement;
        private int m_PopulationEvaluationInterval;
        private int m_PopulationEvaluationsPerDay;
        private uint m_InitializationStartedFrame;
        private uint m_NextPopulationEvaluationFrame;
        private long m_PendingPopulationXp;
        private bool m_HasActiveCity;
        private bool m_InitializationDelayLogged;
        private bool m_HasPopulationEvaluationCadence;
        private bool m_InitializationPending;
        private bool m_LastCustomProgressionEnabled;

        private PopulationEvaluationCadence m_LastPopulationEvaluationCadence;
        private ProgressionSettingsState m_LastSettingsState;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CitySystem =
                World.GetOrCreateSystemManaged<CitySystem>();
            m_SimulationSystem =
                World.GetOrCreateSystemManaged<SimulationSystem>();
            m_XPSystem =
                World.GetOrCreateSystemManaged<XPSystem>();
            m_MilestoneQuery = GetEntityQuery(
                ComponentType.ReadOnly<MilestoneData>());
            m_StateStore = new ProgressionStateStore(
                Path.Combine(
                    EnvPath.kUserDataPath,
                    "ModsData",
                    "Kobbyist.ProgressionControls"));

            GameManager.instance.onGameSaveLoad +=
                HandleGameSaveLoad;
        }

        protected override void OnDestroy()
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.onGameSaveLoad -=
                    HandleGameSaveLoad;
            }

            base.OnDestroy();
        }

        protected override void OnGamePreload(
            Purpose purpose,
            GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            m_GameMode = mode;
            ResetActiveCity();
        }

        protected override void OnGameLoaded(
            Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            ResetActiveCity();
            if (m_GameMode != GameMode.Game)
            {
                return;
            }

            m_InitializationStartedFrame =
                m_SimulationSystem.frameIndex;
            m_InitializationPending = true;
        }

        protected override void OnUpdate()
        {
            if (m_GameMode != GameMode.Game)
            {
                return;
            }

            if (m_InitializationPending &&
                !TryInitializeActiveCity())
            {
                LogInitializationDelayIfNeeded();
                return;
            }

            if (!m_HasActiveCity ||
                m_Configuration == null ||
                Mod.Settings == null)
            {
                return;
            }

            var settings = Mod.Settings;
            var customProgressionEnabled =
                settings.EnableCustomProgression;
            if (!customProgressionEnabled)
            {
                if (m_LastCustomProgressionEnabled)
                {
                    m_LastCustomProgressionEnabled = false;
                    m_VanillaXpScaler.Configure(
                        enabled: false,
                        percentage: 100);
                    m_PendingPopulationXp = 0;
                    m_PendingVanillaXp.Clear();
                    Mod.Log.Info(
                        "Progression integration disabled; population observation and XP interception are dormant");
                }

                return;
            }

            var configurationChanged =
                RefreshConfigurationFromSettings(settings);
            var currentFrame =
                m_SimulationSystem.frameIndex;
            var evaluationRequired =
                ConfigurePopulationEvaluationCadence(
                    settings.PopulationEvaluationCadence);
            evaluationRequired =
                evaluationRequired || configurationChanged;
            if (!m_LastCustomProgressionEnabled)
            {
                if (!TryRebaselineAfterEnable())
                {
                    return;
                }

                m_LastCustomProgressionEnabled = true;
                evaluationRequired = false;
                m_NextPopulationEvaluationFrame =
                    currentFrame +
                    (uint)m_PopulationEvaluationInterval;
            }

            if (evaluationRequired ||
                IsPopulationEvaluationDue(currentFrame))
            {
                EvaluatePopulation();
                m_NextPopulationEvaluationFrame =
                    currentFrame +
                    (uint)m_PopulationEvaluationInterval;
            }

            m_VanillaXpScaler.Configure(
                enabled: true,
                percentage: m_Configuration.VanillaXpPercentage);

            ProcessXpQueue();
        }

        private bool TryInitializeActiveCity()
        {
            var settings = Mod.Settings;
            if (settings == null)
            {
                return false;
            }

            if (!TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Population>(city) ||
                !EntityManager.HasComponent<XP>(city))
            {
                return false;
            }

            if (!TryGetMegalopolisXpRequirement(
                out var megalopolisXpRequirement))
            {
                return false;
            }

            if (settings.SetMegalopolisXpRequirement(
                megalopolisXpRequirement))
            {
                settings.ApplyAndSave();
            }

            m_MegalopolisXpRequirement =
                megalopolisXpRequirement;
            var configuration =
                ResolveInitialConfiguration(
                    settings,
                    megalopolisXpRequirement);

            var cityId = Telemetry.GetCurrentSession();
            if (cityId == Guid.Empty)
            {
                return false;
            }

            m_Configuration = configuration;
            m_CityId = cityId;

            var currentPopulation =
                EntityManager.GetComponentData<Population>(city)
                    .m_Population;
            var baseGameXp =
                EntityManager.GetComponentData<XP>(city);
            var customProgressionEnabled =
                settings.EnableCustomProgression;
            var simulationFrame = m_SimulationSystem.frameIndex;

            ProgressionStateSnapshot persisted = null;
            if (!m_StateStore.TryLoad(
                    m_CityId,
                    simulationFrame,
                    out persisted,
                    out var loadError) &&
                loadError != null)
            {
                Mod.Log.Warn(
                    $"Ignored external progression state: {loadError}");
            }

            if (persisted != null &&
                PopulationProgressionTracker.TryRestore(
                    persisted.PopulationState,
                    m_Configuration,
                    customProgressionEnabled,
                    out m_PopulationTracker))
            {
                m_VanillaXpScaler.Configure(
                    customProgressionEnabled,
                    customProgressionEnabled
                        ? m_Configuration.VanillaXpPercentage
                        : 100);
                if (customProgressionEnabled &&
                    !m_VanillaXpScaler.TryRestoreRemainder(
                        persisted.VanillaRemainderHundredths))
                {
                    Mod.Log.Warn(
                        "Ignored an invalid persisted vanilla XP remainder");
                }

                Mod.Log.Info(
                    $"Restored progression state for city {m_CityId:N} at frame {simulationFrame}");
            }
            else
            {
                var baseline = Math.Max(
                    currentPopulation,
                    baseGameXp.m_MaximumPopulation);
                var baselineState = new PopulationProgressionState(
                    baseline,
                    fractionalXp: 0m);
                PopulationProgressionTracker.TryRestore(
                    baselineState,
                    m_Configuration,
                    customProgressionEnabled,
                    out m_PopulationTracker);
                m_VanillaXpScaler.Configure(
                    customProgressionEnabled,
                    customProgressionEnabled
                        ? m_Configuration.VanillaXpPercentage
                        : 100);

                Mod.Log.Info(
                    $"Established progression baseline at population {baseline}");
            }

            m_LastCustomProgressionEnabled =
                customProgressionEnabled;
            if (customProgressionEnabled)
            {
                ConfigurePopulationEvaluationCadence(
                    settings.PopulationEvaluationCadence);
                m_NextPopulationEvaluationFrame =
                    simulationFrame +
                    (uint)m_PopulationEvaluationInterval;
            }
            m_HasActiveCity = true;
            m_InitializationDelayLogged = false;
            m_InitializationPending = false;

            if (customProgressionEnabled)
            {
                Mod.Log.Info(
                    $"Progression integration active: preset={m_Configuration.Preset}, target={m_LastSettingsState.MegalopolisPopulationTarget}, rate={m_Configuration.XpPerResident}, vanilla={m_Configuration.VanillaXpPercentage}%, populationChecks={m_PopulationEvaluationsPerDay}/day");
            }
            else
            {
                Mod.Log.Info(
                    "Progression integration loaded dormant because custom progression is disabled");
            }
            return true;
        }

        private void ResetActiveCity()
        {
            m_HasActiveCity = false;
            m_Configuration = null;
            m_PopulationTracker = null;
            m_PendingSaveSnapshot = null;
            m_CityId = Guid.Empty;
            m_MegalopolisXpRequirement = 0;
            m_LastSettingsState = null;
            m_PopulationEvaluationInterval = 0;
            m_PopulationEvaluationsPerDay = 0;
            m_HasPopulationEvaluationCadence = false;
            m_InitializationStartedFrame = 0;
            m_InitializationDelayLogged = false;
            m_InitializationPending = false;
            m_VanillaXpScaler.Configure(
                enabled: false,
                percentage: 100);
            m_PendingPopulationXp = 0;
            m_PendingVanillaXp.Clear();
        }

        private ProgressionConfiguration ResolveInitialConfiguration(
            Setting settings,
            int megalopolisXpRequirement)
        {
            var requested = ReadAppliedSettingsState(settings);
            if (ProgressionSettingsResolver.TryResolveInitial(
                requested,
                megalopolisXpRequirement,
                out var configuration,
                out var normalized))
            {
                ApplyNormalizedSettings(
                    settings,
                    requested,
                    normalized);
                m_LastSettingsState = normalized;
                return configuration;
            }

            ProgressionConfiguration.TryFromPreset(
                ProgressionPreset.PopulationHeavy,
                megalopolisXpRequirement,
                out configuration);
            normalized = ProgressionSettingsResolver.Normalize(
                configuration,
                megalopolisXpRequirement,
                PopulationRateInputMode.MegalopolisTarget);
            ApplyNormalizedSettings(
                settings,
                requested,
                normalized);
            m_LastSettingsState = normalized;
            Mod.Log.Warn(
                "Invalid progression settings were replaced with Population Heavy defaults");
            return configuration;
        }

        private bool RefreshConfigurationFromSettings(
            Setting settings)
        {
            var applyCustomRules =
                settings.ConsumeApplyCustomRulesRequest();
            if (!applyCustomRules &&
                AppliedSettingsMatch(settings, m_LastSettingsState))
            {
                return false;
            }

            var requested = applyCustomRules
                ? ReadDraftSettingsState(settings)
                : ReadAppliedSettingsState(settings);
            if (m_LastSettingsState != null &&
                requested.Equals(m_LastSettingsState))
            {
                return false;
            }

            if (!ProgressionSettingsResolver.TryResolveChange(
                m_LastSettingsState,
                requested,
                m_Configuration,
                m_MegalopolisXpRequirement,
                out var configuration,
                out var normalized))
            {
                normalized = ProgressionSettingsResolver.Normalize(
                    m_Configuration,
                    m_MegalopolisXpRequirement,
                    m_LastSettingsState.RateInputMode);
                ApplyNormalizedSettings(
                    settings,
                    requested,
                    normalized);
                m_LastSettingsState = normalized;
                Mod.Log.Warn(
                    "Rejected invalid progression settings and restored the active values");
                return false;
            }

            var configurationChanged =
                configuration.Preset != m_Configuration.Preset ||
                configuration.PopulationXpEnabled !=
                    m_Configuration.PopulationXpEnabled ||
                configuration.XpPerResident !=
                    m_Configuration.XpPerResident ||
                configuration.VanillaXpPercentage !=
                    m_Configuration.VanillaXpPercentage;
            m_Configuration = configuration;
            ApplyNormalizedSettings(
                settings,
                requested,
                normalized);
            m_LastSettingsState = normalized;
            if (configurationChanged)
            {
                Mod.Log.Info(
                    $"Progression settings applied prospectively: preset={configuration.Preset}, target={normalized.MegalopolisPopulationTarget}, rate={configuration.XpPerResident}, vanilla={configuration.VanillaXpPercentage}%");
            }

            return configurationChanged;
        }

        private static bool AppliedSettingsMatch(
            Setting settings,
            ProgressionSettingsState state)
        {
            return state != null &&
                settings.AppliedPreset == state.Preset &&
                settings.AppliedPopulationXpEnabled ==
                    state.PopulationXpEnabled &&
                string.Equals(
                    settings.AppliedXpPerResident,
                    state.XpPerResident,
                    StringComparison.Ordinal) &&
                string.Equals(
                    settings.AppliedMegalopolisPopulationTarget,
                    state.MegalopolisPopulationTarget,
                    StringComparison.Ordinal) &&
                settings.AppliedVanillaXpPercentage ==
                    state.VanillaXpPercentage &&
                settings.AppliedPopulationRateInputMode ==
                    state.RateInputMode;
        }

        private static ProgressionSettingsState ReadAppliedSettingsState(
            Setting settings)
        {
            return new ProgressionSettingsState(
                settings.AppliedPreset,
                settings.AppliedPopulationXpEnabled,
                settings.AppliedXpPerResident,
                settings.AppliedMegalopolisPopulationTarget,
                settings.AppliedVanillaXpPercentage,
                settings.AppliedPopulationRateInputMode);
        }

        private static ProgressionSettingsState ReadDraftSettingsState(
            Setting settings)
        {
            return new ProgressionSettingsState(
                settings.Preset,
                settings.PopulationXpEnabled,
                settings.XpPerResident,
                settings.MegalopolisPopulationTarget,
                settings.VanillaXpPercentage,
                settings.PopulationRateInputMode);
        }

        private static void ApplyNormalizedSettings(
            Setting settings,
            ProgressionSettingsState requested,
            ProgressionSettingsState normalized)
        {
            if (requested.Equals(normalized))
            {
                if (!settings.ApplyResolvedRules(normalized))
                {
                    return;
                }
            }
            else
            {
                settings.ApplyResolvedRules(normalized);
            }
            settings.ApplyAndSave();
        }

        private bool TryGetMegalopolisXpRequirement(
            out int xpRequirement)
        {
            xpRequirement = 0;
            if (m_MilestoneQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            var milestones =
                m_MilestoneQuery.ToComponentDataArray<MilestoneData>(
                    Allocator.Temp);
            try
            {
                for (var index = 0;
                    index < milestones.Length;
                    index++)
                {
                    xpRequirement = Math.Max(
                        xpRequirement,
                        milestones[index].m_XpRequried);
                }
            }
            finally
            {
                milestones.Dispose();
            }

            return xpRequirement > 0;
        }

        private void EvaluatePopulation()
        {
            if (m_PopulationTracker == null ||
                !TryGetActiveCity(out var city) ||
                !EntityManager.HasComponent<Population>(city))
            {
                return;
            }

            var currentPopulation =
                EntityManager.GetComponentData<Population>(city)
                    .m_Population;
            var result = m_PopulationTracker.Observe(
                currentPopulation,
                customProgressionEnabled: true,
                m_Configuration);

            if (!result.Accepted)
            {
                Mod.Log.Warn(
                    "Skipped a population observation because its data was invalid");
                return;
            }

            if (result.AwardedXp > 0)
            {
                m_PendingPopulationXp = result.AwardedXp;
            }
        }

        private bool TryRebaselineAfterEnable()
        {
            if (!TryGetActiveCity(out var city) ||
                m_PopulationTracker == null ||
                !EntityManager.HasComponent<Population>(city) ||
                !EntityManager.HasComponent<XP>(city))
            {
                Mod.Log.Warn(
                    "Progression integration could not re-enable because the active city baseline is unavailable");
                return false;
            }

            var currentPopulation =
                EntityManager.GetComponentData<Population>(city)
                    .m_Population;
            var vanillaMaximumPopulation =
                EntityManager.GetComponentData<XP>(city)
                    .m_MaximumPopulation;
            var result = m_PopulationTracker.Rebaseline(
                currentPopulation,
                vanillaMaximumPopulation,
                m_Configuration);
            if (!result.Accepted)
            {
                Mod.Log.Warn(
                    "Progression integration could not re-enable because the active city baseline is invalid");
                return false;
            }

            m_PendingPopulationXp = 0;
            m_VanillaXpScaler.Configure(
                enabled: true,
                percentage: m_Configuration.VanillaXpPercentage);
            Mod.Log.Info(
                $"Progression integration re-enabled at population {currentPopulation}, maximum={result.MaximumPopulation}; disabled-period growth will not award population XP");
            return true;
        }

        private void ProcessXpQueue()
        {
            if (!TryGetActiveCity(out var city))
            {
                m_PendingPopulationXp = 0;
                return;
            }

            var queue =
                m_XPSystem.GetQueue(out JobHandle queueWriters);
            queueWriters.Complete();

            m_PendingVanillaXp.Clear();
            while (queue.TryDequeue(out var gain))
            {
                if (gain.amount > 0)
                {
                    gain.amount =
                        m_VanillaXpScaler.Scale(gain.amount);
                }

                m_PendingVanillaXp.Add(gain);
            }

            foreach (var gain in m_PendingVanillaXp)
            {
                queue.Enqueue(gain);
            }

            var remaining = m_PendingPopulationXp;
            m_PendingPopulationXp = 0;

            while (remaining > 0)
            {
                var chunk = (int)Math.Min(
                    int.MaxValue,
                    remaining);
                queue.Enqueue(
                    new XPGain
                    {
                        entity = city,
                        amount = chunk,
                        reason = XPReason.Population,
                    });
                remaining -= chunk;
            }
        }

        private bool TryGetActiveCity(out Entity city)
        {
            city = Entity.Null;
            if (m_GameMode != GameMode.Game)
            {
                return false;
            }

            var candidate = m_CitySystem.City;
            if (candidate == Entity.Null ||
                !EntityManager.Exists(candidate))
            {
                return false;
            }

            city = candidate;
            return true;
        }

        private bool ConfigurePopulationEvaluationCadence(
            PopulationEvaluationCadence requestedCadence)
        {
            if (m_HasPopulationEvaluationCadence &&
                m_LastPopulationEvaluationCadence ==
                    requestedCadence)
            {
                return false;
            }

            m_HasPopulationEvaluationCadence = true;
            m_LastPopulationEvaluationCadence =
                requestedCadence;

            var evaluationsPerDay = (int)requestedCadence;
            if (!EvaluationInterval.TryCalculate(
                TimeSystem.kTicksPerDay,
                evaluationsPerDay,
                MinimumPopulationEvaluationInterval,
                out var interval))
            {
                evaluationsPerDay =
                    DefaultPopulationEvaluationsPerDay;
                EvaluationInterval.TryCalculate(
                    TimeSystem.kTicksPerDay,
                    evaluationsPerDay,
                    MinimumPopulationEvaluationInterval,
                    out interval);
                Mod.Log.Warn(
                    $"Invalid population evaluation cadence {requestedCadence}; using {evaluationsPerDay}/day");
            }

            m_PopulationEvaluationsPerDay =
                evaluationsPerDay;
            m_PopulationEvaluationInterval =
                interval;
            Mod.Log.Info(
                $"Population evaluation cadence configured: {evaluationsPerDay}/day, interval={interval} frames");
            return true;
        }

        private bool IsPopulationEvaluationDue(
            uint currentFrame)
        {
            return unchecked(
                (int)(currentFrame -
                    m_NextPopulationEvaluationFrame)) >= 0;
        }

        private void LogInitializationDelayIfNeeded()
        {
            if (m_InitializationDelayLogged ||
                unchecked(
                    (int)(m_SimulationSystem.frameIndex -
                        m_InitializationStartedFrame)) <
                    InitializationWarningInterval)
            {
                return;
            }

            m_InitializationDelayLogged = true;
            Mod.Log.Warn(
                "Progression integration is waiting for the active city session and will remain fail-open until it becomes available");
        }

        private void HandleGameSaveLoad(
            string saveName,
            string previewUri,
            bool start,
            bool success)
        {
            if (start)
            {
                CapturePendingSaveSnapshot();
                return;
            }

            var pending = m_PendingSaveSnapshot;
            m_PendingSaveSnapshot = null;
            if (!success || pending == null)
            {
                return;
            }

            if (m_StateStore.TrySave(
                pending,
                out var saveError))
            {
                Mod.Log.Info(
                    $"Saved external progression state for frame {pending.SimulationFrame}");
            }
            else
            {
                Mod.Log.Error(
                    $"Failed to save external progression state: {saveError}");
            }
        }

        private void CapturePendingSaveSnapshot()
        {
            if (!m_HasActiveCity ||
                m_PopulationTracker == null ||
                Mod.Settings == null ||
                !Mod.Settings.EnableCustomProgression ||
                m_CityId == Guid.Empty ||
                GameManager.instance.isGameLoading)
            {
                m_PendingSaveSnapshot = null;
                return;
            }

            var populationState =
                m_PopulationTracker.CaptureState();
            if (populationState == null)
            {
                m_PendingSaveSnapshot = null;
                return;
            }

            m_PendingSaveSnapshot =
                new ProgressionStateSnapshot(
                    m_CityId,
                    m_SimulationSystem.frameIndex,
                    populationState,
                    m_VanillaXpScaler.RemainderHundredths);
        }
    }
}
