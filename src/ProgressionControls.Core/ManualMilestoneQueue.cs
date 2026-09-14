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

            if (requiredXp < 0)
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

    internal sealed class ManualMilestoneCatalog
    {
        private readonly ManualMilestoneDefinition[] m_Ordered;

        private ManualMilestoneCatalog(
            ManualMilestoneDefinition[] ordered)
        {
            m_Ordered = ordered;
        }

        public static bool TryCreate(
            IEnumerable<ManualMilestoneDefinition> milestones,
            out ManualMilestoneCatalog catalog)
        {
            catalog = null;
            if (!TryOrderMilestones(milestones, out var ordered))
            {
                return false;
            }

            catalog = new ManualMilestoneCatalog(ordered);
            return true;
        }

        public IReadOnlyList<ManualMilestoneQueueEntry> Build(
            int achievedMilestone,
            int cityXp,
            long heldXp,
            bool claimPending,
            bool claimsActive)
        {
            if (achievedMilestone < 0 || cityXp < 0 || heldXp < 0)
            {
                return Array.Empty<ManualMilestoneQueueEntry>();
            }

            var effectiveXp = heldXp > long.MaxValue - cityXp
                ? long.MaxValue
                : heldXp + cityXp;
            List<ManualMilestoneQueueEntry> result = null;
            foreach (var milestone in m_Ordered)
            {
                if (milestone.Index <= achievedMilestone ||
                    milestone.RequiredXp > effectiveXp)
                {
                    continue;
                }

                if (result == null)
                {
                    result = new List<ManualMilestoneQueueEntry>();
                }
                result.Add(new ManualMilestoneQueueEntry(
                    milestone.Index,
                    milestone.RequiredXp,
                    canClaim: claimsActive &&
                        result.Count == 0 &&
                        !claimPending));
            }

            return result == null
                ? Array.Empty<ManualMilestoneQueueEntry>()
                : result.ToArray();
        }

        public bool TryGetNext(
            int achievedMilestone,
            out ManualMilestoneDefinition nextMilestone,
            out bool finalMilestoneReached)
        {
            nextMilestone = null;
            finalMilestoneReached = false;
            if (achievedMilestone < 0)
            {
                return false;
            }

            foreach (var milestone in m_Ordered)
            {
                if (milestone.Index > achievedMilestone)
                {
                    nextMilestone = milestone;
                    return true;
                }
            }

            var finalMilestone = m_Ordered[m_Ordered.Length - 1];
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
