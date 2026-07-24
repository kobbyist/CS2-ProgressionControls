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
        private const int PopulationEvaluationsPerDay = 16;
        private const int PopulationEvaluationInterval =
            TimeSystem.kTicksPerDay / PopulationEvaluationsPerDay;

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
        private Guid m_CityId;
        private uint m_InitializationStartedFrame;
        private uint m_NextPopulationEvaluationFrame;
        private long m_PendingPopulationXp;
        private bool m_HasActiveCity;
        private bool m_InitializationDelayLogged;
        private bool m_InitializationPending;
        private bool m_LastCustomProgressionEnabled;

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
            ResetActiveCity();
        }

        protected override void OnGameLoaded(
            Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            ResetActiveCity();
            m_InitializationStartedFrame =
                m_SimulationSystem.frameIndex;
            m_InitializationPending = true;
        }

        protected override void OnUpdate()
        {
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

            var customProgressionEnabled =
                Mod.Settings.EnableCustomProgression;
            if (customProgressionEnabled !=
                m_LastCustomProgressionEnabled)
            {
                EvaluatePopulation(customProgressionEnabled);
                m_LastCustomProgressionEnabled =
                    customProgressionEnabled;
                m_NextPopulationEvaluationFrame =
                    m_SimulationSystem.frameIndex +
                    PopulationEvaluationInterval;
            }
            else if (IsPopulationEvaluationDue(
                m_SimulationSystem.frameIndex))
            {
                EvaluatePopulation(customProgressionEnabled);
                m_NextPopulationEvaluationFrame =
                    m_SimulationSystem.frameIndex +
                    PopulationEvaluationInterval;
            }

            m_VanillaXpScaler.Configure(
                customProgressionEnabled,
                customProgressionEnabled
                    ? m_Configuration.VanillaXpPercentage
                    : 100);

            if (!customProgressionEnabled)
            {
                m_PendingPopulationXp = 0;
                return;
            }

            ProcessXpQueue();
        }

        private bool TryInitializeActiveCity()
        {
            var city = m_CitySystem.City;
            if (city == Entity.Null ||
                !EntityManager.HasComponent<Population>(city) ||
                !EntityManager.HasComponent<XP>(city))
            {
                return false;
            }

            if (!TryGetMegalopolisXpRequirement(
                out var megalopolisXpRequirement) ||
                !ProgressionConfiguration.TryFromPreset(
                    ProgressionPreset.PopulationHeavy,
                    megalopolisXpRequirement,
                    out var configuration))
            {
                return false;
            }

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
                Mod.Settings != null &&
                Mod.Settings.EnableCustomProgression;
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
            m_NextPopulationEvaluationFrame =
                simulationFrame + PopulationEvaluationInterval;
            m_HasActiveCity = true;
            m_InitializationDelayLogged = false;
            m_InitializationPending = false;

            Mod.Log.Info(
                $"Progression integration active: target={ProgressionConfiguration.DefaultMegalopolisPopulationTarget}, rate={m_Configuration.XpPerResident}, vanilla={m_Configuration.VanillaXpPercentage}%");
            return true;
        }

        private void ResetActiveCity()
        {
            m_HasActiveCity = false;
            m_Configuration = null;
            m_PopulationTracker = null;
            m_PendingSaveSnapshot = null;
            m_CityId = Guid.Empty;
            m_InitializationStartedFrame = 0;
            m_InitializationDelayLogged = false;
            m_InitializationPending = false;
            m_VanillaXpScaler.Configure(
                enabled: false,
                percentage: 100);
            m_PendingPopulationXp = 0;
            m_PendingVanillaXp.Clear();
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

        private void EvaluatePopulation(
            bool customProgressionEnabled)
        {
            var city = m_CitySystem.City;
            if (city == Entity.Null ||
                m_PopulationTracker == null ||
                !EntityManager.HasComponent<Population>(city))
            {
                return;
            }

            var currentPopulation =
                EntityManager.GetComponentData<Population>(city)
                    .m_Population;
            var result = m_PopulationTracker.Observe(
                currentPopulation,
                customProgressionEnabled,
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
                Mod.Log.Info(
                    $"Population XP queued: residents={result.NewRecordDelta}, xp={result.AwardedXp}, maximum={result.MaximumPopulation}");
            }
        }

        private void ProcessXpQueue()
        {
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

            var city = m_CitySystem.City;
            var remaining = m_PendingPopulationXp;
            m_PendingPopulationXp = 0;
            if (city == Entity.Null)
            {
                return;
            }

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
                    PopulationEvaluationInterval)
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
