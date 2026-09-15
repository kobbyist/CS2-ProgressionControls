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
            SupersededPendingPaths = supersededPendingPaths;
        }

        public ProgressionStateSnapshot Snapshot { get; }

        public string SaveName { get; }

        internal string PendingPath { get; }

        internal IReadOnlyCollection<string> SupersededPendingPaths { get; }
    }

    internal sealed class ProgressionStateCleanupResult
    {
        public int RemovedIndexedCheckpoints { get; private set; }

        public int RemovedPendingCheckpoints { get; private set; }

        public int ErrorCount { get; private set; }

        public string FirstError { get; private set; }

        internal void RecordIndexedRemoval()
        {
            RemovedIndexedCheckpoints++;
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
        private const int IndexedSchemaVersion = 2;
        private const uint MaximumLoadFrameDrift = 4096;
        private const string PendingExtension = ".pending";
        private const string StateExtension = ".json";
        private const string UnconfirmedCheckpointError =
            "Ignored a prepared progression checkpoint because it was not durably marked as completed";
        private static readonly TimeSpan UnconfirmedPendingRetention =
            TimeSpan.FromDays(7);

        private readonly string m_RootPath;
        private readonly DataContractJsonSerializer m_Serializer;

        public ProgressionStateStore(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException(
                    "A state root path is required",
                    nameof(rootPath));
            }

            m_RootPath = rootPath;
            m_Serializer =
                new DataContractJsonSerializer(typeof(StateFileModel));
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
            if (string.IsNullOrWhiteSpace(saveName))
            {
                error = "The loaded save name is required";
                return false;
            }

            var legacyPath = GetLegacyStatePath(
                cityId,
                simulationFrame);
            var path = GetStatePath(
                cityId,
                simulationFrame,
                saveName);
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
            else if (File.Exists(legacyPath) &&
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
            CheckpointCandidate recoverable = null;
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
                    recoverable = new CheckpointCandidate(
                        pendingPath,
                        pendingSnapshot,
                        pendingWriteTimeUtc,
                        isPending: true);
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
                error = UnconfirmedCheckpointError;
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

            CheckpointCandidate best = null;
            uint? newestUnconfirmedFrame = null;
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
                    !string.Equals(
                        model.SaveName,
                        saveName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (isPending && !IsCompletionConfirmed(model))
                {
                    if (!newestUnconfirmedFrame.HasValue ||
                        candidateFrame > newestUnconfirmedFrame.Value)
                    {
                        newestUnconfirmedFrame = candidateFrame;
                    }

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
                    best = new CheckpointCandidate(
                        path,
                        candidateSnapshot,
                        lastWriteTimeUtc,
                        isPending);
                }
            }

            if (newestUnconfirmedFrame.HasValue &&
                (best == null ||
                    newestUnconfirmedFrame.Value >=
                        best.Snapshot.SimulationFrame))
            {
                error = UnconfirmedCheckpointError;
                return false;
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
                !string.Equals(
                    model.SaveName,
                    preparation.SaveName,
                    StringComparison.Ordinal))
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

            var candidates = ReadRetentionCandidates(
                result,
                out var pendingFiles,
                out var cityDirectories);
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

            var deletions = CheckpointRetentionPolicy.SelectForDeletion(
                candidates,
                currentCandidate.Id,
                liveSaveNames,
                liveSaveEnumerationTrusted);
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
                    result.RecordIndexedRemoval();
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
                result,
                pendingFiles);
            RemoveEmptyCityDirectories(result, cityDirectories);
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
                (model.SchemaVersion == IndexedSchemaVersion ||
                    (model.SchemaVersion == CurrentSchemaVersion &&
                        model.CompletionConfirmed));
        }

        private void CleanupPendingFiles(
            string currentSaveName,
            IReadOnlyCollection<string> liveSaveNames,
            bool liveSaveEnumerationTrusted,
            ProgressionStateCleanupResult result,
            IReadOnlyList<PendingCheckpointFile> pendingFiles)
        {
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

            foreach (var pendingFile in pendingFiles)
            {
                var pendingPath = pendingFile.Path;
                if (!TryGetLastWriteTimeUtc(
                    pendingPath,
                    out var lastWriteTimeUtc,
                    out var timeError))
                {
                    result.RecordError(timeError);
                    continue;
                }

                var readable = TryReadStateFileModel(
                    pendingPath,
                    out var model,
                    out var readError,
                    out _);
                if (readable &&
                    model.SchemaVersion != IndexedSchemaVersion &&
                    model.SchemaVersion != CurrentSchemaVersion)
                {
                    // Unsupported formats are outside this version's cleanup ownership.
                    continue;
                }

                var valid = readable &&
                    TryCreateSnapshot(
                        model,
                        pendingFile.CityId,
                        model.SimulationFrame,
                        out _);
                if (!valid && readError != null)
                {
                    result.RecordError(readError);
                }

                var confirmed = valid &&
                    IsCompletionConfirmed(model);
                var shouldDelete =
                    (!confirmed &&
                        lastWriteTimeUtc <= unconfirmedCutoff) ||
                    (confirmed &&
                        liveEnumerationCanDelete &&
                        !live.Contains(model.SaveName));
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

        private List<CheckpointRetentionCandidate>
            ReadRetentionCandidates(
                ProgressionStateCleanupResult result,
                out List<PendingCheckpointFile> pendingFiles,
                out string[] cityDirectories)
        {
            var candidates = new List<CheckpointRetentionCandidate>();
            pendingFiles = new List<PendingCheckpointFile>();
            cityDirectories = Array.Empty<string>();
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

                string[] checkpointPaths;
                try
                {
                    checkpointPaths = Directory.GetFiles(
                        cityDirectory,
                        "*",
                        SearchOption.TopDirectoryOnly);
                }
                catch (Exception exception)
                    when (IsExpectedStorageException(exception))
                {
                    result.RecordError(exception.Message);
                    continue;
                }

                foreach (var statePath in checkpointPaths)
                {
                    if (statePath.EndsWith(
                        PendingExtension,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        pendingFiles.Add(new PendingCheckpointFile(
                            statePath,
                            cityId));
                        continue;
                    }
                    if (!statePath.EndsWith(
                        StateExtension,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

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
                            simulationFrame,
                            lastWriteTimeUtc,
                            model.SaveName));
                }
            }

            return candidates;
        }

        private bool TryReadCheckpoint(
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

        private bool TryReadOwnedCheckpoint(
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

            if (string.Equals(
                model.SaveName,
                saveName,
                StringComparison.Ordinal))
            {
                return true;
            }

            snapshot = null;
            error = null;
            return false;
        }

        private bool TryReadStateFileModel(
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
                using (var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    model =
                        (StateFileModel)m_Serializer.ReadObject(stream);
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

        private bool TryWriteModel(
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
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    m_Serializer.WriteObject(stream, model);
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
            ProgressionStateCleanupResult result,
            IReadOnlyList<string> cityDirectories)
        {
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
            return (model.SchemaVersion == IndexedSchemaVersion ||
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

        private sealed class CheckpointCandidate
        {
            public CheckpointCandidate(
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

        private sealed class PendingCheckpointFile
        {
            public PendingCheckpointFile(string path, Guid cityId)
            {
                Path = path;
                CityId = cityId;
            }

            public string Path { get; }

            public Guid CityId { get; }
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
