using System;

namespace Kobbyist.ProgressionControls.Core
{
    internal readonly struct MilestoneTierProgress
    {
        private MilestoneTierProgress(
            long currentXp,
            int requiredXp)
        {
            CurrentXp = currentXp;
            RequiredXp = requiredXp;
        }

        public long CurrentXp { get; }

        public int RequiredXp { get; }

        public static MilestoneTierProgress Calculate(
            long effectiveXp,
            int achievedMilestoneThreshold,
            int nextMilestoneThreshold)
        {
            if (achievedMilestoneThreshold < 0 ||
                nextMilestoneThreshold <= achievedMilestoneThreshold)
            {
                return new MilestoneTierProgress(0, 0);
            }

            var normalizedRequired =
                nextMilestoneThreshold - achievedMilestoneThreshold;
            var normalizedCurrent = effectiveXp <=
                achievedMilestoneThreshold
                ? 0L
                : Math.Min(
                    effectiveXp - achievedMilestoneThreshold,
                    normalizedRequired);
            return new MilestoneTierProgress(
                normalizedCurrent,
                normalizedRequired);
        }
    }
}
