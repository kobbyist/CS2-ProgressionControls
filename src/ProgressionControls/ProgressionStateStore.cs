using System;
using System.Globalization;
using System.IO;
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
            int vanillaRemainderHundredths)
        {
            CityId = cityId;
            SimulationFrame = simulationFrame;
            PopulationState = populationState;
            VanillaRemainderHundredths = vanillaRemainderHundredths;
        }

        public Guid CityId { get; }

        public uint SimulationFrame { get; }

        public PopulationProgressionState PopulationState { get; }

        public int VanillaRemainderHundredths { get; }

        public bool IsValid =>
            CityId != Guid.Empty &&
            PopulationState != null &&
            PopulationState.IsValid &&
            VanillaRemainderHundredths >= 0 &&
            VanillaRemainderHundredths < 100;
    }

    internal sealed class ProgressionStateStore
    {
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
            out string error)
        {
            error = null;
            if (snapshot == null || !snapshot.IsValid)
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
                model.VanillaRemainderHundredths);

            if (!candidate.IsValid)
            {
                return false;
            }

            snapshot = candidate;
            return true;
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
        }
    }
}
