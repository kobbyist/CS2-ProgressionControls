using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Kobbyist.ProgressionControls.Core;

namespace Kobbyist.ProgressionControls
{
    internal sealed class ProgressionStateSnapshot
    {
        public ProgressionStateSnapshot(
            Guid cityId,
            uint simulationFrame,
            PopulationProgressionState populationState,
            int vanillaRemainderHundredths,
            long pendingPopulationXp)
        {
            CityId = cityId;
            SimulationFrame = simulationFrame;
            PopulationState = populationState;
            VanillaRemainderHundredths = vanillaRemainderHundredths;
            PendingPopulationXp = pendingPopulationXp;
        }

        public Guid CityId { get; }

        public uint SimulationFrame { get; }

        public PopulationProgressionState PopulationState { get; }

        public int VanillaRemainderHundredths { get; }

        public long PendingPopulationXp { get; }

        public bool IsValid =>
            CityId != Guid.Empty &&
            PopulationState != null &&
            PopulationState.IsValid &&
            VanillaRemainderHundredths >= 0 &&
            VanillaRemainderHundredths < 100 &&
            PendingPopulationXp >= 0;
    }

    internal sealed class ProgressionStateCleanupResult
    {
        public int RemovedIndexedCheckpoints { get; private set; }

        public int RemovedLegacyCheckpoints { get; private set; }

        public int RetainedLegacyCheckpoints { get; internal set; }

        public int ErrorCount { get; private set; }

        public string FirstError { get; private set; }

        internal void RecordIndexedRemoval()
        {
            RemovedIndexedCheckpoints++;
        }

        internal void RecordLegacyRemoval()
        {
            RemovedLegacyCheckpoints++;
        }

        internal void RecordError(string error)
        {
            ErrorCount++;
            if (FirstError == null)
            {
                FirstError = error;
            }
        }
    }

    internal sealed class ProgressionStateStore
    {
        private const int CurrentSchemaVersion = 2;
        private const int LegacyCheckpointLimitPerCity = 16;
        private const string StateExtension = ".json";

        private readonly string m_RootPath;

        public ProgressionStateStore(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException(
                    "A state root path is required",
                    nameof(rootPath));
            }

            m_RootPath = rootPath;
        }

        public bool TryLoad(
            Guid cityId,
            uint simulationFrame,
            out ProgressionStateSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;
            if (cityId == Guid.Empty)
            {
                error = "The active city session identifier is empty";
                return false;
            }

            var path = GetStatePath(cityId, simulationFrame);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                StateFileModel model;
                var serializer =
                    new DataContractJsonSerializer(typeof(StateFileModel));
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    model = (StateFileModel)serializer.ReadObject(stream);
                }

                if (!TryCreateSnapshot(
                    model,
                    cityId,
                    simulationFrame,
                    out snapshot))
                {
                    error = "The state file failed schema or value validation";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TrySave(
            ProgressionStateSnapshot snapshot,
            string saveName,
            out string error)
        {
            error = null;
            if (snapshot == null ||
                !snapshot.IsValid ||
                string.IsNullOrWhiteSpace(saveName))
            {
                error = "The progression state snapshot is invalid";
                return false;
            }

            var path = GetStatePath(
                snapshot.CityId,
                snapshot.SimulationFrame);
            var directory = Path.GetDirectoryName(path);
            var temporaryPath =
                path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                Directory.CreateDirectory(directory);
                var model = new StateFileModel
                {
                    CityId = snapshot.CityId.ToString("D"),
                    SimulationFrame = snapshot.SimulationFrame,
                    MaximumPopulation =
                        snapshot.PopulationState.MaximumPopulation,
                    PopulationFractionalXp =
                        snapshot.PopulationState.FractionalXp,
                    VanillaRemainderHundredths =
                        snapshot.VanillaRemainderHundredths,
                    PendingPopulationXp =
                        snapshot.PendingPopulationXp,
                    SchemaVersion = CurrentSchemaVersion,
                    SaveName = saveName,
                };

                var serializer =
                    new DataContractJsonSerializer(typeof(StateFileModel));
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    serializer.WriteObject(stream, model);
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(path))
                {
                    File.Replace(
                        temporaryPath,
                        path,
                        destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return true;
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception exception)
                        when (IsExpectedStorageException(exception))
                    {
                    }
                }
            }
        }

        public ProgressionStateCleanupResult Cleanup(
            ProgressionStateSnapshot currentSnapshot,
            string currentSaveName,
            IReadOnlyCollection<string> liveSaveNames,
            bool liveSaveEnumerationTrusted)
        {
            var result = new ProgressionStateCleanupResult();
            if (currentSnapshot == null ||
                !currentSnapshot.IsValid ||
                string.IsNullOrWhiteSpace(currentSaveName))
            {
                result.RecordError(
                    "The current checkpoint identity is invalid");
                return result;
            }

            var currentPath = GetStatePath(
                currentSnapshot.CityId,
                currentSnapshot.SimulationFrame);
            if (!File.Exists(currentPath))
            {
                result.RecordError(
                    "The current checkpoint is unavailable for cleanup");
                return result;
            }

            var candidates = ReadRetentionCandidates(result);
            var currentCandidate = candidates.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Id,
                    currentPath,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    candidate.SaveName,
                    currentSaveName,
                    StringComparison.Ordinal));
            if (currentCandidate == null)
            {
                result.RecordError(
                    "The current checkpoint could not be validated for cleanup");
                return result;
            }

            result.RetainedLegacyCheckpoints = candidates
                .Where(candidate => candidate.IsLegacy)
                .GroupBy(candidate => candidate.CityId)
                .Sum(group => Math.Min(
                    LegacyCheckpointLimitPerCity,
                    group.Count()));

            var deletions = CheckpointRetentionPolicy.SelectForDeletion(
                candidates,
                currentCandidate.Id,
                liveSaveNames,
                liveSaveEnumerationTrusted,
                LegacyCheckpointLimitPerCity);
            foreach (var candidate in deletions)
            {
                if (string.Equals(
                    candidate.Id,
                    currentPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (!File.Exists(candidate.Id))
                    {
                        continue;
                    }

                    File.Delete(candidate.Id);
                    if (candidate.IsLegacy)
                    {
                        result.RecordLegacyRemoval();
                    }
                    else
                    {
                        result.RecordIndexedRemoval();
                    }
                }
                catch (Exception exception)
                    when (IsExpectedStorageException(exception))
                {
                    result.RecordError(exception.Message);
                }
            }

            RemoveEmptyCityDirectories(result);
            return result;
        }

        private List<CheckpointRetentionCandidate>
            ReadRetentionCandidates(
                ProgressionStateCleanupResult result)
        {
            var candidates = new List<CheckpointRetentionCandidate>();
            string[] cityDirectories;
            try
            {
                if (!Directory.Exists(m_RootPath))
                {
                    return candidates;
                }

                cityDirectories = Directory.GetDirectories(m_RootPath);
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                result.RecordError(exception.Message);
                return candidates;
            }

            foreach (var cityDirectory in cityDirectories)
            {
                if (!Guid.TryParseExact(
                    Path.GetFileName(cityDirectory),
                    "N",
                    out var cityId))
                {
                    continue;
                }

                string[] statePaths;
                try
                {
                    statePaths = Directory.GetFiles(
                        cityDirectory,
                        "*" + StateExtension,
                        SearchOption.TopDirectoryOnly);
                }
                catch (Exception exception)
                    when (IsExpectedStorageException(exception))
                {
                    result.RecordError(exception.Message);
                    continue;
                }

                foreach (var statePath in statePaths)
                {
                    string readError = null;
                    var malformed = false;
                    if (!uint.TryParse(
                        Path.GetFileNameWithoutExtension(statePath),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var simulationFrame) ||
                        !TryReadStateFileModel(
                            statePath,
                            out var model,
                            out readError,
                            out malformed))
                    {
                        if (!malformed && readError != null)
                        {
                            result.RecordError(readError);
                        }
                        continue;
                    }

                    if (!TryCreateSnapshot(
                        model,
                        cityId,
                        simulationFrame,
                        out _))
                    {
                        continue;
                    }

                    DateTime lastWriteTimeUtc;
                    try
                    {
                        lastWriteTimeUtc =
                            File.GetLastWriteTimeUtc(statePath);
                    }
                    catch (Exception exception)
                        when (IsExpectedStorageException(exception))
                    {
                        result.RecordError(exception.Message);
                        continue;
                    }

                    candidates.Add(
                        new CheckpointRetentionCandidate(
                            statePath,
                            cityId,
                            simulationFrame,
                            lastWriteTimeUtc,
                            model.SchemaVersion == CurrentSchemaVersion
                                ? model.SaveName
                                : null));
                }
            }

            return candidates;
        }

        private static bool TryReadStateFileModel(
            string path,
            out StateFileModel model,
            out string error,
            out bool malformed)
        {
            model = null;
            error = null;
            malformed = false;
            try
            {
                var serializer =
                    new DataContractJsonSerializer(typeof(StateFileModel));
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    model = (StateFileModel)serializer.ReadObject(stream);
                }

                return true;
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                malformed = exception is SerializationException ||
                    exception is InvalidDataException;
                error = exception.Message;
                return false;
            }
        }

        private void RemoveEmptyCityDirectories(
            ProgressionStateCleanupResult result)
        {
            string[] cityDirectories;
            try
            {
                if (!Directory.Exists(m_RootPath))
                {
                    return;
                }

                cityDirectories = Directory.GetDirectories(m_RootPath);
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                result.RecordError(exception.Message);
                return;
            }

            foreach (var cityDirectory in cityDirectories)
            {
                if (!Guid.TryParseExact(
                    Path.GetFileName(cityDirectory),
                    "N",
                    out _))
                {
                    continue;
                }

                try
                {
                    if (!Directory.EnumerateFileSystemEntries(
                        cityDirectory).Any())
                    {
                        Directory.Delete(cityDirectory);
                    }
                }
                catch (Exception exception)
                    when (IsExpectedStorageException(exception))
                {
                    result.RecordError(exception.Message);
                }
            }
        }

        private string GetStatePath(
            Guid cityId,
            uint simulationFrame)
        {
            var cityDirectory = Path.Combine(
                m_RootPath,
                cityId.ToString("N"));
            var filename =
                simulationFrame.ToString(CultureInfo.InvariantCulture) +
                StateExtension;
            return Path.Combine(cityDirectory, filename);
        }

        private static bool TryCreateSnapshot(
            StateFileModel model,
            Guid expectedCityId,
            uint expectedSimulationFrame,
            out ProgressionStateSnapshot snapshot)
        {
            snapshot = null;
            if (model == null ||
                !IsSupportedSchema(model) ||
                !Guid.TryParse(model.CityId, out var parsedCityId) ||
                parsedCityId != expectedCityId ||
                model.SimulationFrame != expectedSimulationFrame)
            {
                return false;
            }

            var populationState = new PopulationProgressionState(
                model.MaximumPopulation,
                model.PopulationFractionalXp);
            var candidate = new ProgressionStateSnapshot(
                parsedCityId,
                model.SimulationFrame,
                populationState,
                model.VanillaRemainderHundredths,
                model.PendingPopulationXp);

            if (!candidate.IsValid)
            {
                return false;
            }

            snapshot = candidate;
            return true;
        }
        private static bool IsSupportedSchema(StateFileModel model)
        {
            return model.SchemaVersion == 0 ||
                (model.SchemaVersion == CurrentSchemaVersion &&
                    !string.IsNullOrWhiteSpace(model.SaveName));
        }


        private static bool IsExpectedStorageException(
            Exception exception)
        {
            return exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is SerializationException ||
                exception is InvalidDataException;
        }

        [DataContract]
        private sealed class StateFileModel
        {
            [DataMember(Order = 1)]
            public string CityId { get; set; }

            [DataMember(Order = 2)]
            public uint SimulationFrame { get; set; }

            [DataMember(Order = 3)]
            public int MaximumPopulation { get; set; }

            [DataMember(Order = 4)]
            public decimal PopulationFractionalXp { get; set; }

            [DataMember(Order = 5)]
            public int VanillaRemainderHundredths { get; set; }

            [DataMember(Order = 6)]
            public long PendingPopulationXp { get; set; }

            [DataMember(Order = 7, EmitDefaultValue = false)]
            public int SchemaVersion { get; set; }

            [DataMember(Order = 8, EmitDefaultValue = false)]
            public string SaveName { get; set; }
        }
    }
}
