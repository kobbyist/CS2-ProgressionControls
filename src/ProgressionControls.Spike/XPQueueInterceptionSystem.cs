using System;
using System.Collections.Generic;
using Game;
using Game.Simulation;
using Unity.Entities;
using Unity.Jobs;

namespace Kobbyist.ProgressionControls.Spike
{
    public partial class XPQueueInterceptionSystem : GameSystemBase
    {
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
            var requestedPopulationXp = Mod.TakeRequestedPopulationXp();
            if (settings == null)
            {
                return;
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
                    gain.amount = ScaleAmount(
                        gain.amount,
                        settings.VanillaXpMultiplierPercent);
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
                Mod.Log.Warn("Skipped explicit population XP test because no city is active");
            }

            if (settings.LogNonEmptyBatches && pending.Count > 0)
            {
                Mod.Log.Info(
                    $"Intercepted XP batch: count={pending.Count}, input={inputTotal}, output={outputTotal}, multiplier={settings.VanillaXpMultiplierPercent}%");
            }
        }

        private static int ScaleAmount(int amount, int percentage)
        {
            var boundedPercentage = Math.Max(0, Math.Min(100, percentage));
            var scaled = (long)amount * boundedPercentage / 100L;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, scaled));
        }
    }
}
