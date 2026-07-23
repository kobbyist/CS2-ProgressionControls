using System.Collections.Generic;
using Game;
using Game.City;
using Game.Simulation;
using Kobbyist.ProgressionControls.Core;
using Unity.Entities;
using Unity.Jobs;

namespace Kobbyist.ProgressionControls.Spike
{
    public partial class XPQueueInterceptionSystem : GameSystemBase
    {
        private readonly VanillaXpScaler m_VanillaXpScaler =
            new VanillaXpScaler();
        private CitySystem m_CitySystem;
        private XPSystem m_XPSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_XPSystem = World.GetOrCreateSystemManaged<XPSystem>();
        }

        protected override void OnUpdate()
        {
            var settings = Mod.Settings;
            if (settings == null)
            {
                return;
            }

            var requestedPopulationXp = Mod.TakeRequestedPopulationXp();
            var populationStateLogRequested =
                Mod.TakeRequestedPopulationStateLog();
            var configurationChanged = m_VanillaXpScaler.Configure(
                settings.EnableQueueInterception,
                settings.VanillaXpMultiplierPercent);
            if (configurationChanged)
            {
                Mod.Log.Info(
                    $"XP scaling configured: enabled={m_VanillaXpScaler.Enabled}, multiplier={m_VanillaXpScaler.Percentage}%, fractional remainder reset");
            }

            if (populationStateLogRequested)
            {
                LogPopulationState();
            }

            if (!settings.EnableQueueInterception && requestedPopulationXp == 0)
            {
                return;
            }

            var queue = m_XPSystem.GetQueue(out JobHandle queueWriters);
            queueWriters.Complete();

            var pending = new List<XPGain>();
            long inputTotal = 0;
            long outputTotal = 0;

            while (queue.TryDequeue(out var gain))
            {
                inputTotal += gain.amount;
                if (settings.EnableQueueInterception)
                {
                    gain.amount = m_VanillaXpScaler.Scale(gain.amount);
                }

                outputTotal += gain.amount;
                pending.Add(gain);
            }

            foreach (var gain in pending)
            {
                queue.Enqueue(gain);
            }

            if (requestedPopulationXp > 0 && m_CitySystem.City != Entity.Null)
            {
                queue.Enqueue(
                    new XPGain
                    {
                        entity = m_CitySystem.City,
                        amount = requestedPopulationXp,
                        reason = XPReason.Population,
                    });
                Mod.Log.Info(
                    $"Submitted explicit population XP test through XPSystem: {requestedPopulationXp}");
            }
            else if (requestedPopulationXp > 0)
            {
                Mod.Log.Warn(
                    "Skipped explicit population XP test because no city is active");
            }

            if (settings.LogNonEmptyBatches && pending.Count > 0)
            {
                Mod.Log.Info(
                    $"Intercepted XP batch: count={pending.Count}, input={inputTotal}, output={outputTotal}, multiplier={m_VanillaXpScaler.Percentage}%, remainder={m_VanillaXpScaler.RemainderHundredths}/100 XP");
            }
        }

        private void LogPopulationState()
        {
            var city = m_CitySystem.City;
            if (city == Entity.Null)
            {
                Mod.Log.Warn(
                    "Skipped population state log because no city is active");
                return;
            }

            if (!EntityManager.HasComponent<Population>(city) ||
                !EntityManager.HasComponent<XP>(city))
            {
                Mod.Log.Warn(
                    "Skipped population state log because required city components are unavailable");
                return;
            }

            var population = EntityManager.GetComponentData<Population>(city);
            var xp = EntityManager.GetComponentData<XP>(city);
            Mod.Log.Info(
                $"Population state: current={population.m_Population}, maximum={xp.m_MaximumPopulation}, xp={xp.m_XP}");
        }
    }
}
