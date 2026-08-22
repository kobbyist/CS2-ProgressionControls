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
            : this(
                id,
                cityId,
                simulationFrame,
                lastWriteTimeUtc,
                string.IsNullOrEmpty(saveName)
                    ? Array.Empty<string>()
                    : new[] { saveName })
        {
        }

        public CheckpointRetentionCandidate(
            string id,
            Guid cityId,
            uint simulationFrame,
            DateTime lastWriteTimeUtc,
            IEnumerable<string> saveNames)
        {
            Id = id;
            CityId = cityId;
            SimulationFrame = simulationFrame;
            LastWriteTimeUtc = lastWriteTimeUtc;
            SaveNames = saveNames == null
                ? Array.Empty<string>()
                : saveNames
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();
        }

        public string Id { get; }

        public Guid CityId { get; }

        public uint SimulationFrame { get; }

        public DateTime LastWriteTimeUtc { get; }

        public IReadOnlyCollection<string> SaveNames { get; }

        public bool IsLegacy => SaveNames.Count == 0;

        public bool OwnsSave(string saveName)
        {
            return SaveNames.Contains(saveName, StringComparer.Ordinal);
        }
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

            var indexed = all
                .Where(candidate => !candidate.IsLegacy)
                .ToList();
            var retained = new HashSet<string>(StringComparer.Ordinal)
            {
                current.Id,
            };

            foreach (var saveName in indexed
                .SelectMany(candidate => candidate.SaveNames)
                .Distinct(StringComparer.Ordinal))
            {
                var owner = indexed.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.Id,
                        current.Id,
                        StringComparison.Ordinal) &&
                    candidate.OwnsSave(saveName)) ??
                    indexed
                        .Where(candidate => candidate.OwnsSave(saveName))
                        .OrderByDescending(candidate =>
                            candidate.LastWriteTimeUtc)
                        .ThenByDescending(candidate =>
                            candidate.SimulationFrame)
                        .ThenBy(
                            candidate => candidate.Id,
                            StringComparer.Ordinal)
                        .First();
                retained.Add(owner.Id);
            }

            var delete = new HashSet<string>(
                indexed
                    .Where(candidate => !retained.Contains(candidate.Id))
                    .Select(candidate => candidate.Id),
                StringComparer.Ordinal);

            var live = liveSaveNames == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    liveSaveNames.Where(name => !string.IsNullOrEmpty(name)),
                    StringComparer.Ordinal);
            if (liveSaveEnumerationTrusted &&
                current.SaveNames.Any(live.Contains))
            {
                foreach (var candidate in indexed.Where(candidate =>
                    !string.Equals(
                        candidate.Id,
                        current.Id,
                        StringComparison.Ordinal) &&
                    !candidate.SaveNames.Any(live.Contains)))
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
