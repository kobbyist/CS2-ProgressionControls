using System;
using System.Collections.Generic;
using System.Linq;

namespace Kobbyist.ProgressionControls.Core
{
    public sealed class CheckpointRetentionCandidate
    {
        public CheckpointRetentionCandidate(
            string id,
            Guid cityId,
            uint simulationFrame,
            DateTime lastWriteTimeUtc,
            string saveName)
        {
            Id = id;
            CityId = cityId;
            SimulationFrame = simulationFrame;
            LastWriteTimeUtc = lastWriteTimeUtc;
            SaveName = saveName;
        }

        public string Id { get; }

        public Guid CityId { get; }

        public uint SimulationFrame { get; }

        public DateTime LastWriteTimeUtc { get; }

        public string SaveName { get; }

        public bool IsLegacy => string.IsNullOrEmpty(SaveName);
    }

    public static class CheckpointRetentionPolicy
    {
        public static IReadOnlyList<CheckpointRetentionCandidate>
            SelectForDeletion(
                IEnumerable<CheckpointRetentionCandidate> candidates,
                string currentId,
                IReadOnlyCollection<string> liveSaveNames,
                bool liveSaveEnumerationTrusted,
                int legacyLimitPerCity)
        {
            if (candidates == null ||
                string.IsNullOrEmpty(currentId) ||
                legacyLimitPerCity < 0)
            {
                return Array.Empty<CheckpointRetentionCandidate>();
            }

            var all = candidates
                .Where(candidate => candidate != null)
                .ToList();
            var current = all.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    currentId,
                    StringComparison.Ordinal));
            if (current == null || current.IsLegacy)
            {
                return Array.Empty<CheckpointRetentionCandidate>();
            }

            var delete = new HashSet<string>(StringComparer.Ordinal);
            foreach (var saveGroup in all
                .Where(candidate => !candidate.IsLegacy)
                .GroupBy(
                    candidate => candidate.SaveName,
                    StringComparer.Ordinal))
            {
                var retained = saveGroup.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.Id,
                        currentId,
                        StringComparison.Ordinal)) ??
                    saveGroup
                        .OrderByDescending(candidate =>
                            candidate.LastWriteTimeUtc)
                        .ThenByDescending(candidate =>
                            candidate.SimulationFrame)
                        .ThenBy(candidate => candidate.Id, StringComparer.Ordinal)
                        .First();

                foreach (var candidate in saveGroup)
                {
                    if (!ReferenceEquals(candidate, retained))
                    {
                        delete.Add(candidate.Id);
                    }
                }
            }

            var live = liveSaveNames == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    liveSaveNames.Where(name => !string.IsNullOrEmpty(name)),
                    StringComparer.Ordinal);
            if (liveSaveEnumerationTrusted && live.Contains(current.SaveName))
            {
                foreach (var candidate in all.Where(candidate =>
                    !candidate.IsLegacy &&
                    !live.Contains(candidate.SaveName)))
                {
                    delete.Add(candidate.Id);
                }
            }

            foreach (var cityGroup in all
                .Where(candidate => candidate.IsLegacy)
                .GroupBy(candidate => candidate.CityId))
            {
                foreach (var candidate in cityGroup
                    .OrderByDescending(item => item.LastWriteTimeUtc)
                    .ThenByDescending(item => item.SimulationFrame)
                    .ThenBy(item => item.Id, StringComparer.Ordinal)
                    .Skip(legacyLimitPerCity))
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
    }
}
