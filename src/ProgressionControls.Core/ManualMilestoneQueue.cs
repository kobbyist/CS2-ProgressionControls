using System;
using System.Collections.Generic;
using System.Linq;

namespace Kobbyist.ProgressionControls.Core
{
    internal sealed class ManualMilestoneDefinition
    {
        public ManualMilestoneDefinition(
            int index,
            int requiredXp,
            bool isFinal = false)
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
            IsFinal = isFinal;
        }

        public int Index { get; }

        public int RequiredXp { get; }

        public bool IsFinal { get; }
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
            bool claimsActive,
            IEnumerable<ManualMilestoneDefinition> milestones)
        {
            if (achievedMilestone < 0 ||
                cityXp < 0 ||
                heldXp < 0 ||
                !TryOrderMilestones(milestones, out var ordered))
            {
                return Array.Empty<ManualMilestoneQueueEntry>();
            }

            var effectiveXp = heldXp > long.MaxValue - cityXp
                ? long.MaxValue
                : heldXp + cityXp;
            var unachieved = ordered
                .Where(milestone =>
                    milestone.Index > achievedMilestone)
                .ToArray();
            var claimable = unachieved
                .Where(milestone => milestone.RequiredXp <= effectiveXp)
                .ToArray();
            return claimable
                .Select((milestone, index) =>
                    new ManualMilestoneQueueEntry(
                        milestone.Index,
                        milestone.RequiredXp,
                        canClaim: claimsActive &&
                            index == 0 &&
                            !claimPending))
                .ToArray();
        }

        public static bool TryGetNext(
            int achievedMilestone,
            IEnumerable<ManualMilestoneDefinition> milestones,
            out ManualMilestoneDefinition nextMilestone,
            out bool finalMilestoneReached)
        {
            nextMilestone = null;
            finalMilestoneReached = false;
            if (achievedMilestone < 0 ||
                !TryOrderMilestones(milestones, out var ordered))
            {
                return false;
            }

            nextMilestone = ordered.FirstOrDefault(milestone =>
                milestone.Index > achievedMilestone);
            if (nextMilestone != null)
            {
                return true;
            }

            var finalMilestone = ordered[ordered.Length - 1];
            finalMilestoneReached = finalMilestone.IsFinal &&
                finalMilestone.Index == achievedMilestone;
            return false;
        }

        private static bool TryOrderMilestones(
            IEnumerable<ManualMilestoneDefinition> milestones,
            out ManualMilestoneDefinition[] ordered)
        {
            ordered = Array.Empty<ManualMilestoneDefinition>();
            if (milestones == null)
            {
                return false;
            }

            var candidates = milestones.ToArray();
            if (candidates.Any(milestone => milestone == null))
            {
                return false;
            }

            ordered = candidates
                .OrderBy(milestone => milestone.Index)
                .ToArray();
            if (ordered.Length == 0)
            {
                return false;
            }

            for (var index = 0; index < ordered.Length; index++)
            {
                var milestone = ordered[index];
                var isLast = index == ordered.Length - 1;
                if (milestone.Index != index + 1 ||
                    (index > 0 &&
                        milestone.RequiredXp <=
                            ordered[index - 1].RequiredXp) ||
                    milestone.IsFinal != isLast)
                {
                    ordered = Array.Empty<ManualMilestoneDefinition>();
                    return false;
                }
            }

            return true;
        }
    }
}
