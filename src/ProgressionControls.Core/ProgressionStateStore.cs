using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

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

    internal sealed class ProgressionStatePreparation
    {
        internal ProgressionStatePreparation(
            ProgressionStateSnapshot snapshot,
            string saveName,
            string pendingPath,
            IReadOnlyCollection<string> supersededPendingPaths)
        {
            Snapshot = snapshot;
            SaveName = saveName;
            PendingPath = pendingPath;
            SupersededPendingPaths = supersededPendingPaths == null
                ? Array.Empty<string>()
                : supersededPendingPaths.ToArray();
        }

        public ProgressionStateSnapshot Snapshot { get; }

        public string SaveName { get; }

        internal string PendingPath { get; }

        internal IReadOnlyCollection<string> SupersededPendingPaths { get; }
    }

    internal sealed class ProgressionStateCleanupResult
    {
        public int RemovedIndexedCheckpoints { get; private set; }

        public int RemovedLegacyCheckpoints { get; private set; }

        public int RemovedPendingCheckpoints { get; private set; }

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

        internal void RecordPendingRemoval()
        {
            RemovedPendingCheckpoints++;
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
        private const int CurrentSchemaVersion = 5;
        private const int SaveSpecificSchemaVersion = 4;
        private const int MultiOwnerSchemaVersion = 3;
        private const int IndexedSchemaVersion = 2;
        private const int LegacyCheckpointLimitPerCity = 16;
        private const uint MaximumLoadFrameDrift = 4096;
        private const string PendingExtension = ".pending";
        private const string StateExtension = ".json";
        private static readonly TimeSpan UnconfirmedPendingRetention =
            TimeSpan.FromDays(7);

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
            return TryLoad(
                cityId,
                simulationFrame,
                saveName: null,
                out snapshot,
                out error);
        }

        public bool TryLoad(
            Guid cityId,
            uint simulationFrame,
            string saveName,
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

            var legacyPath = GetLegacyStatePath(
                cityId,
                simulationFrame);
            var path = string.IsNullOrWhiteSpace(saveName)
                ? legacyPath
                : GetStatePath(cityId, simulationFrame, saveName);
            ProgressionStateSnapshot committedSnapshot = null;
            var committedWriteTimeUtc = DateTime.MinValue;
            string committedError = null;
            if (File.Exists(path) &&
                TryReadOwnedCheckpoint(
                    path,
                    cityId,
                    simulationFrame,
                    saveName,
                    out committedSnapshot,
                    out _,
                    out committedError))
            {
                TryGetLastWriteTimeUtc(
                    path,
                    out committedWriteTimeUtc,
                    out committedError);
            }
            else if (!string.Equals(
                    path,
                    legacyPath,
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(legacyPath) &&
                TryReadOwnedCheckpoint(
                    legacyPath,
                    cityId,
                    simulationFrame,
                    saveName,
                    out committedSnapshot,
                    out _,
                    out var legacyError))
            {
                TryGetLastWriteTimeUtc(
                    legacyPath,
                    out committedWriteTimeUtc,
                    out legacyError);
                RecordFirstError(ref committedError, legacyError);
            }

            var pendingPaths = GetPendingPaths(
                cityId,
                simulationFrame,
                out var pendingEnumerationError);
            PendingCheckpointCandidate recoverable = null;
            string pendingError = pendingEnumerationError;
            var unconfirmedPendingFound = false;
            foreach (var pendingPath in pendingPaths)
            {
                if (!TryReadOwnedCheckpoint(
                        pendingPath,
                        cityId,
                        simulationFrame,
                        saveName,
                        out var pendingSnapshot,
                        out var pendingModel,
                        out var readError))
                {
                    RecordFirstError(ref pendingError, readError);
                    continue;
                }

                if (!TryGetLastWriteTimeUtc(
                    pendingPath,
                    out var pendingWriteTimeUtc,
                    out var writeTimeError))
                {
                    RecordFirstError(
                        ref pendingError,
                        writeTimeError);
                    continue;
                }

                if (!IsCompletionConfirmed(pendingModel))
                {
                    unconfirmedPendingFound = true;
                    continue;
                }

                if (recoverable == null ||
                    pendingWriteTimeUtc > recoverable.LastWriteTimeUtc)
                {
                    recoverable = new PendingCheckpointCandidate(
                        pendingPath,
                        pendingSnapshot,
                        pendingWriteTimeUtc);
                }
            }

            if (recoverable != null &&
                (committedSnapshot == null ||
                    recoverable.LastWriteTimeUtc >
                    committedWriteTimeUtc))
            {
                snapshot = recoverable.Snapshot;
                if (TryPromote(
                    recoverable.Path,
                    path,
                    out var promoteError))
                {
                    if (pendingError != null)
                    {
                        error = pendingError;
                    }
                }
                else
                {
                    error = promoteError;
                }

                return true;
            }

            if (committedSnapshot != null)
            {
                snapshot = committedSnapshot;
                error = committedError ?? pendingError;
                return true;
            }

            string nearbyError = null;
            if (committedError == null &&
                pendingError == null &&
                !unconfirmedPendingFound &&
                !string.IsNullOrWhiteSpace(saveName) &&
                TryLoadNearbyOwnedCheckpoint(
                    cityId,
                    simulationFrame,
                    saveName,
                    out snapshot,
                    out nearbyError))
            {
                error = committedError ?? pendingError ?? nearbyError;
                return true;
            }

            error = committedError ?? pendingError ?? nearbyError;
            if (error == null && unconfirmedPendingFound)
            {
                error = "Ignored a prepared progression checkpoint because it was not durably marked as completed";
            }

            return false;
        }

        private bool TryLoadNearbyOwnedCheckpoint(
            Guid cityId,
            uint simulationFrame,
            string saveName,
            out ProgressionStateSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;
            var directory = GetCityDirectory(cityId);
            if (!Directory.Exists(directory) || simulationFrame == 0)
            {
                return false;
            }

            string[] paths;
            try
            {
                paths = Directory.GetFiles(
                    directory,
                    "*",
                    SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                error = exception.Message;
                return false;
            }

            CheckpointLoadCandidate best = null;
            var saveKey = GetSaveKey(saveName);
            var committedSuffix =
                "." + saveKey + StateExtension;
            var pendingMarker = "." + saveKey + ".";
            foreach (var path in paths)
            {
                var fileName = Path.GetFileName(path);
                var isPending = path.EndsWith(
                    PendingExtension,
                    StringComparison.OrdinalIgnoreCase);
                var isCommitted = path.EndsWith(
                    StateExtension,
                    StringComparison.OrdinalIgnoreCase);
                if ((!isPending && !isCommitted) ||
                    (isPending && fileName.IndexOf(
                        pendingMarker,
                        StringComparison.Ordinal) < 0) ||
                    (isCommitted && !fileName.EndsWith(
                        committedSuffix,
                        StringComparison.Ordinal)))
                {
                    continue;
                }

                if (!TryReadStateFileModel(
                        path,
                        out var model,
                        out var readError,
                        out _) ||
                    !TryCreateSnapshot(
                        model,
                        cityId,
                        model?.SimulationFrame ?? 0,
                        out var candidateSnapshot))
                {
                    RecordFirstError(ref error, readError);
                    continue;
                }

                var candidateFrame = candidateSnapshot.SimulationFrame;
                if (candidateFrame >= simulationFrame ||
                    simulationFrame - candidateFrame >
                        MaximumLoadFrameDrift ||
                    !GetSaveNames(model).Contains(
                        saveName,
                        StringComparer.Ordinal) ||
                    (isPending && !IsCompletionConfirmed(model)))
                {
                    continue;
                }

                if (!TryGetLastWriteTimeUtc(
                    path,
                    out var lastWriteTimeUtc,
                    out var writeTimeError))
                {
                    RecordFirstError(ref error, writeTimeError);
                    continue;
                }

                if (best == null ||
                    candidateFrame > best.Snapshot.SimulationFrame ||
                    (candidateFrame == best.Snapshot.SimulationFrame &&
                        lastWriteTimeUtc > best.LastWriteTimeUtc))
                {
                    best = new CheckpointLoadCandidate(
                        path,
                        candidateSnapshot,
                        lastWriteTimeUtc,
                        isPending);
                }
            }

            if (best == null)
            {
                return false;
            }

            snapshot = best.Snapshot;
            if (best.IsPending &&
                !TryPromote(
                    best.Path,
                    GetStatePath(
                        cityId,
                        best.Snapshot.SimulationFrame,
                        saveName),
                    out var promoteError))
            {
                error = promoteError;
            }

            return true;
        }

        public bool TryPrepare(
            ProgressionStateSnapshot snapshot,
            string saveName,
            out ProgressionStatePreparation preparation,
            out string error)
        {
            preparation = null;
            error = null;
            if (snapshot == null ||
                !snapshot.IsValid ||
                string.IsNullOrWhiteSpace(saveName))
            {
                error = "The progression state snapshot is invalid";
                return false;
            }

            var supersededPendingPaths =
                CollectConfirmedPendingPaths(
                    snapshot,
                    saveName);

            var pendingPath = CreatePendingPath(
                snapshot.CityId,
                snapshot.SimulationFrame,
                saveName);
            var model = CreateModel(snapshot, saveName);
            if (!TryWriteModel(model, pendingPath, out error))
            {
                return false;
            }

            preparation = new ProgressionStatePreparation(
                snapshot,
                saveName,
                pendingPath,
                supersededPendingPaths);
            return true;
        }

        public bool TryCommit(
            ProgressionStatePreparation preparation,
            out string error)
        {
            error = null;
            if (!IsValidPreparation(preparation))
            {
                error = "The prepared progression checkpoint is invalid";
                return false;
            }

            if (!TryReadCheckpoint(
                    preparation.PendingPath,
                    preparation.Snapshot.CityId,
                    preparation.Snapshot.SimulationFrame,
                    out _,
                    out var model,
                    out error) ||
                !GetSaveNames(model).Contains(
                    preparation.SaveName,
                    StringComparer.Ordinal))
            {
                if (error == null)
                {
                    error = "The prepared checkpoint does not own the completed save";
                }

                return false;
            }

            model.CompletionConfirmed = true;
            if (!TryWriteModel(
                model,
                preparation.PendingPath,
                out error))
            {
                return false;
            }

            if (!TryPromote(
                preparation.PendingPath,
                GetStatePath(
                    preparation.Snapshot.CityId,
                    preparation.Snapshot.SimulationFrame,
                    preparation.SaveName),
                out error))
            {
                return false;
            }

            foreach (var supersededPath in
                preparation.SupersededPendingPaths)
            {
                TryDeleteFile(supersededPath, out _);
            }

            return true;
        }

        public bool TryDiscard(
            ProgressionStatePreparation preparation,
            out string error)
        {
            error = null;
            if (preparation == null ||
                string.IsNullOrWhiteSpace(preparation.PendingPath))
            {
                return true;
            }

            return TryDeleteFile(preparation.PendingPath, out error);
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
                currentSnapshot.SimulationFrame,
                currentSaveName);
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
                candidate.SaveNames.Contains(
                    currentSaveName,
                    StringComparer.Ordinal));
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

            CleanupPendingFiles(
                currentSaveName,
                liveSaveNames,
                liveSaveEnumerationTrusted,
                result);
            RemoveEmptyCityDirectories(result);
            return result;
        }

        private IReadOnlyCollection<string>
            CollectConfirmedPendingPaths(
                ProgressionStateSnapshot expected,
                string saveName)
        {
            var confirmedPaths = new List<string>();
            foreach (var path in GetPendingPaths(
                expected.CityId,
                expected.SimulationFrame,
                out _))
            {
                if (!TryReadOwnedCheckpoint(
                        path,
                        expected.CityId,
                        expected.SimulationFrame,
                        saveName,
                        out _,
                        out var model,
                        out _) ||
                    !IsCompletionConfirmed(model))
                {
                    continue;
                }

                confirmedPaths.Add(path);
            }

            return confirmedPaths;
        }

        private static bool IsCompletionConfirmed(StateFileModel model)
        {
            return model != null &&
                (model.SchemaVersion <= IndexedSchemaVersion ||
                    ((model.SchemaVersion ==
                            SaveSpecificSchemaVersion ||
                        model.SchemaVersion ==
                            CurrentSchemaVersion) &&
                    model.CompletionConfirmed));
        }

        private void CleanupPendingFiles(
            string currentSaveName,
            IReadOnlyCollection<string> liveSaveNames,
            bool liveSaveEnumerationTrusted,
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

            var live = liveSaveNames == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    liveSaveNames.Where(name =>
                        !string.IsNullOrWhiteSpace(name)),
                    StringComparer.Ordinal);
            var liveEnumerationCanDelete =
                liveSaveEnumerationTrusted && live.Contains(currentSaveName);
            var unconfirmedCutoff =
                DateTime.UtcNow - UnconfirmedPendingRetention;

            foreach (var cityDirectory in cityDirectories)
            {
                if (!Guid.TryParseExact(
                    Path.GetFileName(cityDirectory),
                    "N",
                    out var cityId))
                {
                    continue;
                }

                string[] pendingPaths;
                try
                {
                    pendingPaths = Directory.GetFiles(
                        cityDirectory,
                        "*" + PendingExtension,
                        SearchOption.TopDirectoryOnly);
                }
                catch (Exception exception)
                    when (IsExpectedStorageException(exception))
                {
                    result.RecordError(exception.Message);
                    continue;
                }

                foreach (var pendingPath in pendingPaths)
                {
                    if (!TryGetLastWriteTimeUtc(
                        pendingPath,
                        out var lastWriteTimeUtc,
                        out var timeError))
                    {
                        result.RecordError(timeError);
                        continue;
                    }

                    var valid = TryReadStateFileModel(
                        pendingPath,
                        out var model,
                        out var readError,
                        out _) &&
                        TryCreateSnapshot(
                            model,
                            cityId,
                            model.SimulationFrame,
                            out _);
                    if (!valid && readError != null)
                    {
                        result.RecordError(readError);
                    }

                    var confirmed = valid &&
                        IsCompletionConfirmed(model);
                    var owners = valid
                        ? GetSaveNames(model)
                        : Array.Empty<string>();
                    var shouldDelete =
                        (!confirmed &&
                            lastWriteTimeUtc <= unconfirmedCutoff) ||
                        (confirmed &&
                            liveEnumerationCanDelete &&
                            owners.Count > 0 &&
                            !owners.Any(live.Contains));
                    if (!shouldDelete)
                    {
                        continue;
                    }

                    if (TryDeleteFile(pendingPath, out var deleteError))
                    {
                        result.RecordPendingRemoval();
                    }
                    else
                    {
                        result.RecordError(deleteError);
                    }
                }
            }
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
                    if (!TryReadStateFileModel(
                            statePath,
                            out var model,
                            out var readError,
                            out var malformed))
                    {
                        if (!malformed && readError != null)
                        {
                            result.RecordError(readError);
                        }

                        continue;
                    }

                    var simulationFrame = model.SimulationFrame;
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
                            GetSaveNames(model)));
                }
            }

            return candidates;
        }

        private static bool TryReadCheckpoint(
            string path,
            Guid cityId,
            uint simulationFrame,
            out ProgressionStateSnapshot snapshot,
            out StateFileModel model,
            out string error)
        {
            snapshot = null;
            model = null;
            error = null;
            if (!File.Exists(path))
            {
                return false;
            }

            if (!TryReadStateFileModel(
                path,
                out model,
                out error,
                out _))
            {
                return false;
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

        private static bool TryReadOwnedCheckpoint(
            string path,
            Guid cityId,
            uint simulationFrame,
            string saveName,
            out ProgressionStateSnapshot snapshot,
            out StateFileModel model,
            out string error)
        {
            if (!TryReadCheckpoint(
                path,
                cityId,
                simulationFrame,
                out snapshot,
                out model,
                out error))
            {
                return false;
            }

            var owners = GetSaveNames(model);
            if (string.IsNullOrWhiteSpace(saveName) ||
                owners.Count == 0 ||
                owners.Contains(saveName, StringComparer.Ordinal))
            {
                return true;
            }

            snapshot = null;
            error = null;
            return false;
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
                    model =
                        (StateFileModel)serializer.ReadObject(stream);
                }

                if (model == null)
                {
                    malformed = true;
                    error =
                        "The progression checkpoint contains no state";
                    return false;
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

        private static bool TryWriteModel(
            StateFileModel model,
            string path,
            out string error)
        {
            error = null;
            var directory = Path.GetDirectoryName(path);
            var temporaryPath = CreateAtomicWriteTemporaryPath(
                path,
                Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(directory);
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

        internal static string CreateAtomicWriteTemporaryPath(
            string path,
            string operationId)
        {
            var directory = Path.GetDirectoryName(path);
            var temporaryName =
                "." + operationId + ".tmp";
            return string.IsNullOrEmpty(directory)
                ? temporaryName
                : Path.Combine(directory, temporaryName);
        }

        private static bool TryPromote(
            string pendingPath,
            string path,
            out string error)
        {
            error = null;
            try
            {
                if (!File.Exists(pendingPath))
                {
                    error = "The prepared checkpoint file is missing";
                    return false;
                }

                if (File.Exists(path))
                {
                    File.Replace(
                        pendingPath,
                        path,
                        destinationBackupFileName: null);
                }
                else
                {
                    File.Move(pendingPath, path);
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

        private static bool TryDeleteFile(
            string path,
            out string error)
        {
            error = null;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
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

        private static bool TryGetLastWriteTimeUtc(
            string path,
            out DateTime lastWriteTimeUtc,
            out string error)
        {
            lastWriteTimeUtc = DateTime.MinValue;
            error = null;
            try
            {
                lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
                return true;
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
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
            uint simulationFrame,
            string saveName)
        {
            return Path.Combine(
                GetCityDirectory(cityId),
                simulationFrame.ToString(CultureInfo.InvariantCulture) +
                "." + GetSaveKey(saveName) +
                StateExtension);
        }

        private string GetLegacyStatePath(
            Guid cityId,
            uint simulationFrame)
        {
            return Path.Combine(
                GetCityDirectory(cityId),
                simulationFrame.ToString(CultureInfo.InvariantCulture) +
                StateExtension);
        }

        private string CreatePendingPath(
            Guid cityId,
            uint simulationFrame,
            string saveName)
        {
            return Path.Combine(
                GetCityDirectory(cityId),
                simulationFrame.ToString(CultureInfo.InvariantCulture) +
                "." + GetSaveKey(saveName) +
                "." + Guid.NewGuid().ToString("N") +
                PendingExtension);
        }

        private static string GetSaveKey(string saveName)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(saveName));
                var key = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                {
                    key.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return key.ToString();
            }
        }

        private IReadOnlyCollection<string> GetPendingPaths(
            Guid cityId,
            uint simulationFrame,
            out string error)
        {
            error = null;
            var directory = GetCityDirectory(cityId);
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            try
            {
                var prefix =
                    simulationFrame.ToString(
                        CultureInfo.InvariantCulture) + ".";
                return Directory
                    .GetFiles(
                        directory,
                        "*" + PendingExtension,
                        SearchOption.TopDirectoryOnly)
                    .Where(path =>
                        Path.GetFileName(path).StartsWith(
                            prefix,
                            StringComparison.Ordinal))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception exception)
                when (IsExpectedStorageException(exception))
            {
                error = exception.Message;
                return Array.Empty<string>();
            }
        }

        private string GetCityDirectory(Guid cityId)
        {
            return Path.Combine(m_RootPath, cityId.ToString("N"));
        }

        private static bool IsValidPreparation(
            ProgressionStatePreparation preparation)
        {
            return preparation != null &&
                preparation.Snapshot != null &&
                preparation.Snapshot.IsValid &&
                !string.IsNullOrWhiteSpace(preparation.SaveName) &&
                !string.IsNullOrWhiteSpace(preparation.PendingPath) &&
                File.Exists(preparation.PendingPath);
        }

        private static StateFileModel CreateModel(
            ProgressionStateSnapshot snapshot,
            string saveName)
        {
            return new StateFileModel
            {
                CityId = snapshot.CityId.ToString("D"),
                SimulationFrame = snapshot.SimulationFrame,
                MaximumPopulation =
                    snapshot.PopulationState.MaximumPopulation,
                PopulationFractionalXp =
                    snapshot.PopulationState.FractionalXp,
                VanillaRemainderHundredths =
                    snapshot.VanillaRemainderHundredths,
                PendingPopulationXp = snapshot.PendingPopulationXp,
                HeldMilestoneXp = snapshot.HeldMilestoneXp,
                PendingMilestoneClaimIndex =
                    snapshot.PendingMilestoneClaim.Index,
                PendingMilestoneClaimXp =
                    snapshot.PendingMilestoneClaim.ReleasedXp,
                PendingMilestoneClaimThreshold =
                    snapshot.PendingMilestoneClaim.Threshold,
                SchemaVersion = CurrentSchemaVersion,
                SaveName = saveName,
                CompletionConfirmed = false,
            };
        }

        private static IReadOnlyCollection<string> GetSaveNames(
            StateFileModel model)
        {
            if (model == null || model.SchemaVersion == 0)
            {
                return Array.Empty<string>();
            }

            if (model.SchemaVersion == IndexedSchemaVersion ||
                model.SchemaVersion == SaveSpecificSchemaVersion ||
                model.SchemaVersion == CurrentSchemaVersion)
            {
                return string.IsNullOrWhiteSpace(model.SaveName)
                    ? Array.Empty<string>()
                    : new[] { model.SaveName };
            }

            if (model.SchemaVersion != MultiOwnerSchemaVersion)
            {
                return Array.Empty<string>();
            }

            return model.SaveNames == null
                ? Array.Empty<string>()
                : model.SaveNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();
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
            if (!PendingMilestoneClaim.TryCreate(
                model.PendingMilestoneClaimIndex,
                model.PendingMilestoneClaimXp,
                model.PendingMilestoneClaimThreshold,
                out var pendingMilestoneClaim))
            {
                return false;
            }

            var candidate = new ProgressionStateSnapshot(
                parsedCityId,
                model.SimulationFrame,
                populationState,
                model.VanillaRemainderHundredths,
                model.PendingPopulationXp,
                model.HeldMilestoneXp,
                pendingMilestoneClaim);

            if (!candidate.IsValid)
            {
                return false;
            }

            snapshot = candidate;
            return true;
        }

        private static bool IsSupportedSchema(StateFileModel model)
        {
            if (model.SchemaVersion == 0)
            {
                return true;
            }

            if (model.SchemaVersion == IndexedSchemaVersion)
            {
                return !string.IsNullOrWhiteSpace(model.SaveName);
            }

            if (model.SchemaVersion == MultiOwnerSchemaVersion)
            {
                return GetSaveNames(model).Count > 0;
            }

            return (model.SchemaVersion == SaveSpecificSchemaVersion ||
                    model.SchemaVersion == CurrentSchemaVersion) &&
                !string.IsNullOrWhiteSpace(model.SaveName);
        }

        private static bool IsExpectedStorageException(
            Exception exception)
        {
            return exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is SerializationException ||
                exception is InvalidDataException;
        }

        private static void RecordFirstError(
            ref string target,
            string candidate)
        {
            if (target == null && candidate != null)
            {
                target = candidate;
            }
        }

        private sealed class PendingCheckpointCandidate
        {
            public PendingCheckpointCandidate(
                string path,
                ProgressionStateSnapshot snapshot,
                DateTime lastWriteTimeUtc)
            {
                Path = path;
                Snapshot = snapshot;
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            public string Path { get; }

            public ProgressionStateSnapshot Snapshot { get; }

            public DateTime LastWriteTimeUtc { get; }
        }

        private sealed class CheckpointLoadCandidate
        {
            public CheckpointLoadCandidate(
                string path,
                ProgressionStateSnapshot snapshot,
                DateTime lastWriteTimeUtc,
                bool isPending)
            {
                Path = path;
                Snapshot = snapshot;
                LastWriteTimeUtc = lastWriteTimeUtc;
                IsPending = isPending;
            }

            public string Path { get; }

            public ProgressionStateSnapshot Snapshot { get; }

            public DateTime LastWriteTimeUtc { get; }

            public bool IsPending { get; }
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

            [DataMember(Order = 9, EmitDefaultValue = false)]
            public string[] SaveNames { get; set; }

            [DataMember(Order = 10, EmitDefaultValue = false)]
            public bool CompletionConfirmed { get; set; }

            [DataMember(Order = 11, EmitDefaultValue = false)]
            public long HeldMilestoneXp { get; set; }

            [DataMember(Order = 12, EmitDefaultValue = false)]
            public int PendingMilestoneClaimIndex { get; set; }

            [DataMember(Order = 13, EmitDefaultValue = false)]
            public int PendingMilestoneClaimXp { get; set; }

            [DataMember(Order = 14, EmitDefaultValue = false)]
            public int PendingMilestoneClaimThreshold { get; set; }
        }
    }
}
