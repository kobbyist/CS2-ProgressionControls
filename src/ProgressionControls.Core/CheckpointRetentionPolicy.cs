using System;
using System.Collections.Generic;
using System.Linq;

namespace Kobbyist.ProgressionControls.Core
{
    internal sealed class CheckpointRetentionCandidate
    {
        public CheckpointRetentionCandidate(
            string id,
            uint simulationFrame,
            DateTime lastWriteTimeUtc,
            string saveName)
        {
            Id = id;
            SimulationFrame = simulationFrame;
            LastWriteTimeUtc = lastWriteTimeUtc;
            SaveName = saveName;
        }

        public string Id { get; }

        public uint SimulationFrame { get; }

        public DateTime LastWriteTimeUtc { get; }

        public string SaveName { get; }
    }

    internal static class CheckpointRetentionPolicy
    {
        public static IReadOnlyList<CheckpointRetentionCandidate>
            SelectForDeletion(
                IEnumerable<CheckpointRetentionCandidate> candidates,
                string currentId,
                IReadOnlyCollection<string> liveSaveNames,
                bool liveSaveEnumerationTrusted)
        {
            if (candidates == null ||
                string.IsNullOrEmpty(currentId))
            {
                return Array.Empty<CheckpointRetentionCandidate>();
            }

            var all = candidates
                .Where(candidate =>
                    candidate != null &&
                    !string.IsNullOrWhiteSpace(candidate.SaveName))
                .ToList();
            var current = all.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    currentId,
                    StringComparison.Ordinal));
            if (current == null)
            {
                return Array.Empty<CheckpointRetentionCandidate>();
            }

            var retained = new HashSet<string>(StringComparer.Ordinal)
            {
                current.Id,
            };

            var ownerBySaveName =
                new Dictionary<string, CheckpointRetentionCandidate>(
                    StringComparer.Ordinal);
            foreach (var candidate in all)
            {
                if (!ownerBySaveName.TryGetValue(
                        candidate.SaveName,
                        out var owner) ||
                    IsPreferredOwner(candidate, owner, current.Id))
                {
                    ownerBySaveName[candidate.SaveName] = candidate;
                }
            }
            foreach (var owner in ownerBySaveName.Values)
            {
                retained.Add(owner.Id);
            }

            var delete = new HashSet<string>(
                all
                    .Where(candidate => !retained.Contains(candidate.Id))
                    .Select(candidate => candidate.Id),
                StringComparer.Ordinal);

            var live = liveSaveNames == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    liveSaveNames.Where(name => !string.IsNullOrEmpty(name)),
                    StringComparer.Ordinal);
            if (liveSaveEnumerationTrusted &&
                live.Contains(current.SaveName))
            {
                foreach (var candidate in all.Where(candidate =>
                    !string.Equals(
                        candidate.Id,
                        current.Id,
                        StringComparison.Ordinal) &&
                    !live.Contains(candidate.SaveName)))
                {
                    delete.Add(candidate.Id);
                }
            }

            delete.Remove(currentId);
            return all
                .Where(candidate => delete.Contains(candidate.Id))
                .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
                .ToList();
        }

        private static bool IsPreferredOwner(
            CheckpointRetentionCandidate candidate,
            CheckpointRetentionCandidate currentOwner,
            string currentId)
        {
            var candidateIsCurrent = string.Equals(
                candidate.Id,
                currentId,
                StringComparison.Ordinal);
            var ownerIsCurrent = string.Equals(
                currentOwner.Id,
                currentId,
                StringComparison.Ordinal);
            if (candidateIsCurrent || ownerIsCurrent)
            {
                return candidateIsCurrent && !ownerIsCurrent;
            }

            var writeTimeComparison = candidate.LastWriteTimeUtc.CompareTo(
                currentOwner.LastWriteTimeUtc);
            if (writeTimeComparison != 0)
            {
                return writeTimeComparison > 0;
            }

            var frameComparison = candidate.SimulationFrame.CompareTo(
                currentOwner.SimulationFrame);
            return frameComparison != 0
                ? frameComparison > 0
                : string.Compare(
                    candidate.Id,
                    currentOwner.Id,
                    StringComparison.Ordinal) < 0;
        }
    }
}
