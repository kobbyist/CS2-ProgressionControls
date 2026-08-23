using System;
using System.Collections.Generic;
using System.Linq;

namespace Kobbyist.ProgressionControls.Core
{
    internal sealed class ManualMilestoneDefinition
    {
        public ManualMilestoneDefinition(int index, int requiredXp)
        {
            if (index <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (requiredXp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredXp));
            }

            Index = index;
            RequiredXp = requiredXp;
        }

        public int Index { get; }

        public int RequiredXp { get; }
    }

    internal sealed class ManualMilestoneQueueEntry
    {
        public ManualMilestoneQueueEntry(
            int index,
            int requiredXp,
            bool canClaim)
        {
            Index = index;
            RequiredXp = requiredXp;
            CanClaim = canClaim;
        }

        public int Index { get; }

        public int RequiredXp { get; }

        public bool CanClaim { get; }
    }

    internal static class ManualMilestoneQueue
    {
        public static IReadOnlyList<ManualMilestoneQueueEntry> Build(
            int achievedMilestone,
            int cityXp,
            long heldXp,
            bool claimPending,
            IEnumerable<ManualMilestoneDefinition> milestones)
        {
            if (achievedMilestone < 0 ||
                cityXp < 0 ||
                heldXp < 0 ||
                milestones == null)
            {
                return Array.Empty<ManualMilestoneQueueEntry>();
            }

            var effectiveXp = heldXp > long.MaxValue - cityXp
                ? long.MaxValue
                : heldXp + cityXp;
            var ordered = milestones
                .Where(milestone =>
                    milestone != null &&
                    milestone.Index > achievedMilestone)
                .OrderBy(milestone => milestone.Index)
                .ThenBy(milestone => milestone.RequiredXp)
                .ToArray();
            if (ordered
                .Select(milestone => milestone.Index)
                .Distinct()
                .Count() != ordered.Length)
            {
                return Array.Empty<ManualMilestoneQueueEntry>();
            }

            var claimable = ordered
                .Where(milestone => milestone.RequiredXp <= effectiveXp)
                .ToArray();
            return claimable
                .Select((milestone, index) =>
                    new ManualMilestoneQueueEntry(
                        milestone.Index,
                        milestone.RequiredXp,
                        canClaim: index == 0 && !claimPending))
                .ToArray();
        }
    }
}
