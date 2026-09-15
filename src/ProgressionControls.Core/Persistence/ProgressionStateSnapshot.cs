using System;

namespace Kobbyist.ProgressionControls.Core
{
    internal sealed class ProgressionStateSnapshot
    {
        public ProgressionStateSnapshot(
            Guid cityId,
            uint simulationFrame,
            PopulationProgressionState populationState,
            int vanillaRemainderHundredths,
            long pendingPopulationXp,
            long heldMilestoneXp = 0,
            PendingMilestoneClaim pendingMilestoneClaim = default)
        {
            CityId = cityId;
            SimulationFrame = simulationFrame;
            PopulationState = populationState;
            VanillaRemainderHundredths = vanillaRemainderHundredths;
            PendingPopulationXp = pendingPopulationXp;
            HeldMilestoneXp = heldMilestoneXp;
            PendingMilestoneClaim = pendingMilestoneClaim;
        }

        public Guid CityId { get; }

        public uint SimulationFrame { get; }

        public PopulationProgressionState PopulationState { get; }

        public int VanillaRemainderHundredths { get; }

        public long PendingPopulationXp { get; }

        public long HeldMilestoneXp { get; }

        public PendingMilestoneClaim PendingMilestoneClaim { get; }

        public decimal RequiredVanillaFailSafeXp
        {
            get
            {
                if (PopulationState == null)
                {
                    return 0m;
                }

                var fractionalXp =
                    PopulationState.FractionalXp +
                    VanillaRemainderHundredths / 100m;
                return PendingPopulationXp +
                    HeldMilestoneXp +
                    decimal.Ceiling(fractionalXp);
            }
        }

        public bool IsValid =>
            CityId != Guid.Empty &&
            PopulationState != null &&
            PopulationState.IsValid &&
            VanillaRemainderHundredths >= 0 &&
            VanillaRemainderHundredths < 100 &&
            PendingPopulationXp >= 0 &&
            HeldMilestoneXp >= 0 &&
            PendingMilestoneClaim.IsValid;
    }
}
