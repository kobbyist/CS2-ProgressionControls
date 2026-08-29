using System;

namespace Kobbyist.ProgressionControls.Core
{
    internal readonly struct MilestoneRangeProgress
    {
        private MilestoneRangeProgress(long currentXp, int requiredXp)
        {
            CurrentXp = currentXp;
            RequiredXp = requiredXp;
        }

        public long CurrentXp { get; }

        public int RequiredXp { get; }

        public static MilestoneRangeProgress Calculate(
            long effectiveXp,
            int milestoneThreshold)
        {
            if (milestoneThreshold <= 0)
            {
                return new MilestoneRangeProgress(0, 0);
            }

            var visibleXp = Math.Min(
                Math.Max(0L, effectiveXp),
                milestoneThreshold);
            return new MilestoneRangeProgress(
                visibleXp,
                milestoneThreshold);
        }
    }
}
