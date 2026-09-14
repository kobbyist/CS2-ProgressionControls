using System.Globalization;
using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ProgressionStateStoreTests
{
    private static readonly Guid CityId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private string? m_RootPath;

    [TestInitialize]
    public void Initialize()
    {
        m_RootPath = Path.Combine(
            Path.GetTempPath(),
            "ProgressionControls.Tests",
            Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (m_RootPath != null && Directory.Exists(m_RootPath))
        {
            Directory.Delete(m_RootPath, recursive: true);
        }
    }

    [TestMethod]
    public void PreparedCheckpointRequiresDurableCompletionMarker()
    {
        var store = CreateStore();
        var expected = Snapshot(frame: 100, pendingPopulationXp: 42);

        Assert.IsTrue(store.TryPrepare(
            expected,
            "Save/A",
            out var preparation,
            out var prepareError),
            prepareError);

        Assert.IsFalse(store.TryLoad(
            CityId,
            100,
            "Save/A",
            out _,
            out var unconfirmedError));
        StringAssert.Contains(
            unconfirmedError,
            "not durably marked");

        MarkCompletedWithFailedPromotion(
            store,
            preparation);
        store = CreateStore();
        Assert.IsTrue(store.TryLoad(
            CityId,
            100,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(expected, restored);
        Assert.AreEqual(0, PendingFiles().Count);
        Assert.AreEqual(1, CheckpointFiles().Count);
    }

    [TestMethod]
    public void LoadRequiresExactSaveName()
    {
        var store = CreateStore();

        Assert.IsFalse(store.TryLoad(
            CityId,
            100,
            saveName: null,
            out _,
            out var loadError));
        Assert.AreEqual(
            "The loaded save name is required",
            loadError);
    }

    [TestMethod]
    public void AtomicWriteTemporaryPathDoesNotRepeatLongCheckpointName()
    {
        var directory = Path.Combine(
            "C:\\",
            new string('d', 132));
        var checkpointName =
            "7959976." +
            new string('a', 64) + "." +
            new string('b', 32) +
            ".pending";
        var checkpointPath = Path.Combine(directory, checkpointName);

        var temporaryPath =
            ProgressionStateStore.CreateAtomicWriteTemporaryPath(
                checkpointPath,
                new string('c', 32));

        Assert.AreEqual(
            Path.GetDirectoryName(checkpointPath),
            Path.GetDirectoryName(temporaryPath));
        Assert.IsTrue(
            temporaryPath.Length < 260,
            $"Atomic write path was {temporaryPath.Length} characters");
        Assert.IsFalse(
            Path.GetFileName(temporaryPath).Contains(
                Path.GetFileName(checkpointPath),
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void FailedSaveDiscardRemovesPreparedCheckpoint()
    {
        var store = CreateStore();
        var snapshot = Snapshot(frame: 200, pendingPopulationXp: 5);

        Assert.IsTrue(store.TryPrepare(
            snapshot,
            "Save/A",
            out var preparation,
            out var prepareError),
            prepareError);
        Assert.IsTrue(store.TryDiscard(
            preparation,
            out var discardError),
            discardError);

        Assert.IsFalse(store.TryLoad(
            CityId,
            200,
            "Save/A",
            out _,
            out var loadError));
        Assert.IsNull(loadError);
        Assert.AreEqual(0, PendingFiles().Count);
        Assert.AreEqual(0, CheckpointFiles().Count);
    }

    [TestMethod]
    public void FailedSaveDiscardPreservesEarlierRecoveryRecord()
    {
        var store = CreateStore();
        var snapshot = Snapshot(frame: 250, pendingPopulationXp: 11);
        Assert.IsTrue(store.TryPrepare(
            snapshot,
            "Save/A",
            out var firstPreparation,
            out var firstPrepareError),
            firstPrepareError);
        MarkCompletedWithFailedPromotion(
            store,
            firstPreparation);

        Assert.IsTrue(store.TryPrepare(
            snapshot,
            "Save/B",
            out var secondPreparation,
            out var secondPrepareError),
            secondPrepareError);
        Assert.AreEqual(2, PendingFiles().Count);

        Assert.IsTrue(store.TryDiscard(
            secondPreparation,
            out var discardError),
            discardError);
        Assert.AreEqual(1, PendingFiles().Count);

        Assert.IsTrue(store.TryLoad(
            CityId,
            250,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(snapshot, restored);
    }

    [TestMethod]
    public void CommittedCheckpointWinsOverUnconfirmedPreparation()
    {
        var store = CreateStore();
        var committed = Snapshot(frame: 275, pendingPopulationXp: 7);
        PrepareAndCommit(store, committed, "Save/A");

        var unconfirmed = Snapshot(frame: 275, pendingPopulationXp: 99);
        Assert.IsTrue(store.TryPrepare(
            unconfirmed,
            "Save/A",
            out _,
            out var prepareError),
            prepareError);

        Assert.IsTrue(store.TryLoad(
            CityId,
            275,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(committed, restored);
    }

    [TestMethod]
    public void PendingCheckpointForAnotherSaveDoesNotWarnOrOverride()
    {
        var store = CreateStore();
        var committed = Snapshot(frame: 280, pendingPopulationXp: 7);
        PrepareAndCommit(store, committed, "Save/A");

        var other = Snapshot(frame: 280, pendingPopulationXp: 99);
        Assert.IsTrue(store.TryPrepare(
            other,
            "Save/B",
            out _,
            out var prepareError),
            prepareError);

        Assert.IsTrue(store.TryLoad(
            CityId,
            280,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        Assert.IsNull(loadError);
        AssertSnapshot(committed, restored);
    }

    [TestMethod]
    public void MalformedPendingCheckpointReportsError()
    {
        var cityDirectory = Path.Combine(
            m_RootPath!,
            CityId.ToString("N"));
        Directory.CreateDirectory(cityDirectory);
        File.WriteAllText(
            Path.Combine(cityDirectory, "290.corrupt.pending"),
            "not-json");

        var store = CreateStore();
        Assert.IsFalse(store.TryLoad(
            CityId,
            290,
            "Save/A",
            out _,
            out var loadError));
        Assert.IsFalse(string.IsNullOrWhiteSpace(loadError));
    }

    [TestMethod]
    public void SuccessfulCommitRemovesSupersededRecoveryRecord()
    {
        var store = CreateStore();
        var snapshot = Snapshot(frame: 295, pendingPopulationXp: 13);
        Assert.IsTrue(store.TryPrepare(
            snapshot,
            "Save/A",
            out var firstPreparation,
            out var firstPrepareError),
            firstPrepareError);
        MarkCompletedWithFailedPromotion(
            store,
            firstPreparation);

        Assert.IsTrue(store.TryPrepare(
            snapshot,
            "Save/A",
            out var secondPreparation,
            out var secondPrepareError),
            secondPrepareError);
        Assert.AreEqual(2, PendingFiles().Count);

        Assert.IsTrue(store.TryCommit(
            secondPreparation,
            out var commitError),
            commitError);
        Assert.AreEqual(0, PendingFiles().Count);
        Assert.AreEqual(1, CheckpointFiles().Count);
    }

    [TestMethod]
    public void FailSafeXpRoundsCombinedFractionUp()
    {
        var snapshot = new ProgressionStateSnapshot(
            CityId,
            simulationFrame: 299,
            new PopulationProgressionState(
                maximumPopulation: 1234,
                fractionalXp: 0.75m),
            vanillaRemainderHundredths: 50,
            pendingPopulationXp: 42);

        Assert.AreEqual(44m, snapshot.RequiredVanillaFailSafeXp);
    }

    [TestMethod]
    public void FailSafeXpIncludesHeldMilestoneXp()
    {
        var snapshot = new ProgressionStateSnapshot(
            CityId,
            simulationFrame: 299,
            new PopulationProgressionState(
                maximumPopulation: 1234,
                fractionalXp: 0.25m),
            vanillaRemainderHundredths: 25,
            pendingPopulationXp: 10,
            heldMilestoneXp: 500);

        Assert.AreEqual(511m, snapshot.RequiredVanillaFailSafeXp);
    }

    [TestMethod]
    public void ManualMilestoneStateRoundTrips()
    {
        var store = CreateStore();
        Assert.IsTrue(PendingMilestoneClaim.TryCreate(
            4,
            500,
            9000,
            out var pendingMilestoneClaim));
        var snapshot = new ProgressionStateSnapshot(
            CityId,
            simulationFrame: 301,
            new PopulationProgressionState(
                maximumPopulation: 1234,
                fractionalXp: 0m),
            vanillaRemainderHundredths: 0,
            pendingPopulationXp: 0,
            heldMilestoneXp: 4500,
            pendingMilestoneClaim: pendingMilestoneClaim);

        PrepareAndCommit(store, snapshot, "Manual Save");

        Assert.IsTrue(store.TryLoad(
            CityId,
            301,
            "Manual Save",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(snapshot, restored);
    }

    [TestMethod]
    public void HeldXpMakesMilestoneClaimableAfterOneFrameLoadDrift()
    {
        var store = CreateStore();
        var snapshot = new ProgressionStateSnapshot(
            CityId,
            simulationFrame: 310,
            new PopulationProgressionState(
                maximumPopulation: 1234,
                fractionalXp: 0m),
            vanillaRemainderHundredths: 0,
            pendingPopulationXp: 0,
            heldMilestoneXp: 1);
        PrepareAndCommit(store, snapshot, "Save/A");

        Assert.IsTrue(store.TryLoad(
            CityId,
            simulationFrame: 311,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);

        Assert.IsTrue(ManualMilestoneCatalog.TryCreate(
            new[]
            {
                new ManualMilestoneDefinition(1, 25),
                new ManualMilestoneDefinition(2, 50),
                new ManualMilestoneDefinition(3, 75),
                new ManualMilestoneDefinition(4, 100, isFinal: true),
            },
            out var catalog));
        var queue = catalog.Build(
            achievedMilestone: 3,
            cityXp: 99,
            restored.HeldMilestoneXp,
            claimPending: false,
            claimsActive: true);
        Assert.AreEqual(1, queue.Count);
        Assert.IsTrue(queue[0].CanClaim);
    }

    [TestMethod]
    public void LoadDriftSelectsClosestEarlierCheckpoint()
    {
        var store = CreateStore();
        var earlier = Snapshot(
            frame: 320,
            pendingPopulationXp: 10);
        var closest = Snapshot(
            frame: 329,
            pendingPopulationXp: 20);
        PrepareAndCommit(store, earlier, "Save/A");
        PrepareAndCommit(store, closest, "Save/A");

        Assert.IsTrue(store.TryLoad(
            CityId,
            simulationFrame: 330,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(closest, restored);
    }

    [TestMethod]
    public void LoadDriftDoesNotUseStaleCheckpoint()
    {
        var store = CreateStore();
        PrepareAndCommit(
            store,
            Snapshot(frame: 340, pendingPopulationXp: 10),
            "Save/A");

        Assert.IsFalse(store.TryLoad(
            CityId,
            simulationFrame: 4437,
            "Save/A",
            out _,
            out _));
    }

    [TestMethod]
    public void LoadDriftAcceptsCheckpointAtMaximumDistance()
    {
        var store = CreateStore();
        var snapshot = Snapshot(
            frame: 500,
            pendingPopulationXp: 10);
        PrepareAndCommit(store, snapshot, "Save/A");

        Assert.IsTrue(store.TryLoad(
            CityId,
            simulationFrame: 4596,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(snapshot, restored);
    }

    [TestMethod]
    public void LoadDriftRecoversConfirmedPendingCheckpoint()
    {
        var store = CreateStore();
        var snapshot = Snapshot(
            frame: 410,
            pendingPopulationXp: 25);
        Assert.IsTrue(store.TryPrepare(
            snapshot,
            "Save/A",
            out var preparation,
            out var prepareError),
            prepareError);
        MarkCompletedWithFailedPromotion(store, preparation);

        Assert.IsTrue(store.TryLoad(
            CityId,
            simulationFrame: 411,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(snapshot, restored);
        Assert.AreEqual(0, PendingFiles().Count);
        Assert.AreEqual(1, CheckpointFiles().Count);
    }

    [TestMethod]
    public void UnconfirmedExactCheckpointBlocksOlderFallback()
    {
        var store = CreateStore();
        PrepareAndCommit(
            store,
            Snapshot(frame: 420, pendingPopulationXp: 10),
            "Save/A");
        Assert.IsTrue(store.TryPrepare(
            Snapshot(frame: 421, pendingPopulationXp: 20),
            "Save/A",
            out _,
            out var prepareError),
            prepareError);

        Assert.IsFalse(store.TryLoad(
            CityId,
            simulationFrame: 421,
            "Save/A",
            out _,
            out var loadError));
        StringAssert.Contains(loadError, "not durably marked");
    }

    [TestMethod]
    public void UnconfirmedNearbyCheckpointBlocksOlderFallback()
    {
        var store = CreateStore();
        PrepareAndCommit(
            store,
            Snapshot(frame: 420, pendingPopulationXp: 10),
            "Save/A");
        Assert.IsTrue(store.TryPrepare(
            Snapshot(frame: 421, pendingPopulationXp: 20),
            "Save/A",
            out _,
            out var prepareError),
            prepareError);

        Assert.IsFalse(store.TryLoad(
            CityId,
            simulationFrame: 422,
            "Save/A",
            out _,
            out var loadError));
        StringAssert.Contains(loadError, "not durably marked");
    }

    [TestMethod]
    public void DivergentSameFrameSavesRetainSeparateSnapshots()
    {
        var store = CreateStore();
        var first = Snapshot(frame: 300, pendingPopulationXp: 17);
        var second = Snapshot(frame: 300, pendingPopulationXp: 99);
        PrepareAndCommit(store, first, "Save/A");
        PrepareAndCommit(store, second, "Save/B");

        Assert.AreEqual(2, CheckpointFiles().Count);
        Assert.IsTrue(store.TryLoad(
            CityId,
            300,
            "Save/A",
            out var restoredFirst,
            out var firstLoadError),
            firstLoadError);
        AssertSnapshot(first, restoredFirst);
        Assert.IsTrue(store.TryLoad(
            CityId,
            300,
            "Save/B",
            out var restoredSecond,
            out var secondLoadError),
            secondLoadError);
        AssertSnapshot(second, restoredSecond);

        var current = Snapshot(frame: 400, pendingPopulationXp: 23);
        PrepareAndCommit(store, current, "Save/C");

        var firstCleanup = store.Cleanup(
            current,
            "Save/C",
            new[] { "Save/A", "Save/C" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, firstCleanup.ErrorCount);
        Assert.AreEqual(1, firstCleanup.RemovedIndexedCheckpoints);
        Assert.IsTrue(store.TryLoad(
            CityId,
            300,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        AssertSnapshot(first, restored);
        Assert.IsFalse(store.TryLoad(
            CityId,
            300,
            "Save/B",
            out _,
            out _));

        var secondCleanup = store.Cleanup(
            current,
            "Save/C",
            new[] { "Save/C" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, secondCleanup.ErrorCount);
        Assert.AreEqual(1, secondCleanup.RemovedIndexedCheckpoints);
        Assert.IsFalse(store.TryLoad(
            CityId,
            300,
            "Save/A",
            out _,
            out _));
    }

    [TestMethod]
    public void LaterOverwriteDoesNotConfirmStalePendingCheckpoint()
    {
        var store = CreateStore();
        var stale = Snapshot(frame: 450, pendingPopulationXp: 31);
        Assert.IsTrue(store.TryPrepare(
            stale,
            "Save/A",
            out _,
            out var prepareError),
            prepareError);

        var later = Snapshot(frame: 451, pendingPopulationXp: 47);
        PrepareAndCommit(store, later, "Save/A");

        store = CreateStore();
        Assert.IsFalse(store.TryLoad(
            CityId,
            450,
            "Save/A",
            out _,
            out var loadError));
        StringAssert.Contains(loadError, "not durably marked");
    }

    [TestMethod]
    public void CleanupRemovesExpiredUnconfirmedPendingCheckpoint()
    {
        var store = CreateStore();
        var abandoned = Snapshot(frame: 600, pendingPopulationXp: 9);
        Assert.IsTrue(store.TryPrepare(
            abandoned,
            "Save/A",
            out _,
            out var prepareError),
            prepareError);
        File.SetLastWriteTimeUtc(
            PendingFiles().Single(),
            DateTime.UtcNow.AddDays(-8));

        var current = Snapshot(frame: 700, pendingPopulationXp: 12);
        PrepareAndCommit(store, current, "Save/C");
        var cleanup = store.Cleanup(
            current,
            "Save/C",
            new[] { "Save/A", "Save/C" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, cleanup.ErrorCount);
        Assert.AreEqual(1, cleanup.RemovedPendingCheckpoints);
        Assert.AreEqual(0, PendingFiles().Count);
    }

    [TestMethod]
    public void CleanupRemovesConfirmedPendingWhenOwnerIsDeleted()
    {
        var store = CreateStore();
        var abandoned = Snapshot(frame: 650, pendingPopulationXp: 15);
        Assert.IsTrue(store.TryPrepare(
            abandoned,
            "Save/A",
            out var preparation,
            out var prepareError),
            prepareError);
        MarkCompletedWithFailedPromotion(store, preparation);

        var current = Snapshot(frame: 700, pendingPopulationXp: 12);
        PrepareAndCommit(store, current, "Save/C");
        var cleanup = store.Cleanup(
            current,
            "Save/C",
            new[] { "Save/C" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, cleanup.ErrorCount);
        Assert.AreEqual(1, cleanup.RemovedPendingCheckpoints);
        Assert.AreEqual(0, PendingFiles().Count);
    }

    [TestMethod]
    public void CleanupKeepsConfirmedPendingWhenCurrentSaveIsNotEnumerated()
    {
        var store = CreateStore();
        var recoverable = Snapshot(frame: 675, pendingPopulationXp: 15);
        Assert.IsTrue(store.TryPrepare(
            recoverable,
            "Save/A",
            out var preparation,
            out var prepareError),
            prepareError);
        MarkCompletedWithFailedPromotion(store, preparation);

        var current = Snapshot(frame: 700, pendingPopulationXp: 12);
        PrepareAndCommit(store, current, "Save/C");
        var cleanup = store.Cleanup(
            current,
            "Save/C",
            new[] { "Save/A" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, cleanup.ErrorCount);
        Assert.AreEqual(0, cleanup.RemovedPendingCheckpoints);
        Assert.AreEqual(1, PendingFiles().Count);
    }

    [TestMethod]
    public void SchemaTwoCheckpointRemainsLoadable()
    {
        var cityDirectory = Path.Combine(
            m_RootPath!,
            CityId.ToString("N"));
        Directory.CreateDirectory(cityDirectory);
        var path = Path.Combine(cityDirectory, "500.json");
        var json = string.Format(
            CultureInfo.InvariantCulture,
            "{{\"CityId\":\"{0:D}\",\"SimulationFrame\":500,\"MaximumPopulation\":1234,\"PopulationFractionalXp\":0.5,\"VanillaRemainderHundredths\":25,\"PendingPopulationXp\":12,\"SchemaVersion\":2,\"SaveName\":\"Save/A\"}}",
            CityId);
        File.WriteAllText(path, json);

        var store = CreateStore();
        Assert.IsTrue(store.TryLoad(
            CityId,
            500,
            "Save/A",
            out var restored,
            out var loadError),
            loadError);
        Assert.AreEqual(1234, restored.PopulationState.MaximumPopulation);
        Assert.AreEqual(0.5m, restored.PopulationState.FractionalXp);
        Assert.AreEqual(25, restored.VanillaRemainderHundredths);
        Assert.AreEqual(12, restored.PendingPopulationXp);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(4)]
    public void UnsupportedCheckpointSchemaIsIgnoredAndPreserved(
        int schemaVersion)
    {
        var store = CreateStore();
        var snapshot = Snapshot(
            frame: 501,
            pendingPopulationXp: 13);
        PrepareAndCommit(store, snapshot, "Save/A");

        var path = CheckpointFiles().Single();
        var json = File.ReadAllText(path);
        StringAssert.Contains(json, @"""SchemaVersion"":5");
        File.WriteAllText(
            path,
            json.Replace(
                @"""SchemaVersion"":5",
                $@"""SchemaVersion"":{schemaVersion}"));

        Assert.IsFalse(store.TryLoad(
            CityId,
            501,
            "Save/A",
            out _,
            out var loadError));
        StringAssert.Contains(
            loadError,
            "schema or value validation");
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void CleanupLeavesUnsupportedCheckpointSchemasUntouched()
    {
        var store = CreateStore();
        var unsupportedPaths = new List<string>();
        foreach (var schemaVersion in new[] { 0, 3, 4 })
        {
            var snapshot = Snapshot(
                frame: (uint)(510 + schemaVersion),
                pendingPopulationXp: 13);
            PrepareAndCommit(
                store,
                snapshot,
                $"Save/{schemaVersion}");
            var path = CheckpointFiles()
                .Single(candidate =>
                    !unsupportedPaths.Contains(candidate));
            var json = File.ReadAllText(path);
            File.WriteAllText(
                path,
                json.Replace(
                    @"""SchemaVersion"":5",
                    $@"""SchemaVersion"":{schemaVersion}"));
            unsupportedPaths.Add(path);
        }

        var current = Snapshot(
            frame: 600,
            pendingPopulationXp: 21);
        PrepareAndCommit(store, current, "Save/Current");

        var cleanup = store.Cleanup(
            current,
            "Save/Current",
            new[] { "Save/Current" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, cleanup.ErrorCount);
        Assert.AreEqual(0, cleanup.RemovedIndexedCheckpoints);
        Assert.IsTrue(unsupportedPaths.All(File.Exists));
    }

    [DataTestMethod]
    [DataRow(0, false)]
    [DataRow(0, true)]
    [DataRow(3, false)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(4, true)]
    [DataRow(6, false)]
    [DataRow(6, true)]
    public void CleanupPreservesUnsupportedPendingCheckpoint(
        int schemaVersion,
        bool completionConfirmed)
    {
        var store = CreateStore();
        Assert.IsTrue(store.TryPrepare(
            Snapshot(frame: 650, pendingPopulationXp: 15),
            "Save/Deleted",
            out var preparation,
            out var prepareError),
            prepareError);
        var path = preparation.PendingPath;
        var originalJson = File.ReadAllText(path);
        var unsupportedJson = originalJson.Replace(
            @"""SchemaVersion"":5",
            $@"""SchemaVersion"":{schemaVersion},""CompletionConfirmed"":{completionConfirmed.ToString().ToLowerInvariant()}");
        Assert.AreNotEqual(originalJson, unsupportedJson);
        File.WriteAllText(path, unsupportedJson);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddDays(-8));

        var current = Snapshot(frame: 700, pendingPopulationXp: 12);
        PrepareAndCommit(store, current, "Save/Current");
        var cleanup = store.Cleanup(
            current,
            "Save/Current",
            new[] { "Save/Current" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, cleanup.ErrorCount);
        Assert.AreEqual(0, cleanup.RemovedPendingCheckpoints);
        Assert.IsTrue(File.Exists(path));
        Assert.AreEqual(unsupportedJson, File.ReadAllText(path));
    }

    private ProgressionStateStore CreateStore()
    {
        return new ProgressionStateStore(m_RootPath!);
    }

    private IReadOnlyList<string> PendingFiles()
    {
        return Directory.Exists(m_RootPath)
            ? Directory.GetFiles(
                m_RootPath!,
                "*.pending",
                SearchOption.AllDirectories)
            : Array.Empty<string>();
    }

    private IReadOnlyList<string> CheckpointFiles()
    {
        return Directory.Exists(m_RootPath)
            ? Directory.GetFiles(
                m_RootPath!,
                "*.json",
                SearchOption.AllDirectories)
            : Array.Empty<string>();
    }

    private void MarkCompletedWithFailedPromotion(
        ProgressionStateStore store,
        ProgressionStatePreparation preparation)
    {
        var pendingName = Path.GetFileName(
            preparation.PendingPath);
        var pendingParts = pendingName.Split('.');
        Assert.AreEqual(4, pendingParts.Length);
        var finalPath = Path.Combine(
            Path.GetDirectoryName(preparation.PendingPath)!,
            pendingParts[0] + "." + pendingParts[1] + ".json");
        Directory.CreateDirectory(finalPath);
        Assert.IsFalse(store.TryCommit(
            preparation,
            out var commitError));
        Assert.IsFalse(string.IsNullOrWhiteSpace(commitError));
        Directory.Delete(finalPath);
        Assert.AreEqual(1, PendingFiles().Count);
    }

    private static ProgressionStateSnapshot Snapshot(
        uint frame,
        long pendingPopulationXp)
    {
        return new ProgressionStateSnapshot(
            CityId,
            frame,
            new PopulationProgressionState(
                maximumPopulation: 1234,
                fractionalXp: 0.5m),
            vanillaRemainderHundredths: 25,
            pendingPopulationXp);
    }

    private static void PrepareAndCommit(
        ProgressionStateStore store,
        ProgressionStateSnapshot snapshot,
        string saveName)
    {
        Assert.IsTrue(store.TryPrepare(
            snapshot,
            saveName,
            out var preparation,
            out var prepareError),
            prepareError);
        Assert.IsTrue(store.TryCommit(
            preparation,
            out var commitError),
            commitError);
    }

    private static void AssertSnapshot(
        ProgressionStateSnapshot expected,
        ProgressionStateSnapshot actual)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.CityId, actual.CityId);
        Assert.AreEqual(expected.SimulationFrame, actual.SimulationFrame);
        Assert.AreEqual(
            expected.PopulationState.MaximumPopulation,
            actual.PopulationState.MaximumPopulation);
        Assert.AreEqual(
            expected.PopulationState.FractionalXp,
            actual.PopulationState.FractionalXp);
        Assert.AreEqual(
            expected.VanillaRemainderHundredths,
            actual.VanillaRemainderHundredths);
        Assert.AreEqual(
            expected.PendingPopulationXp,
            actual.PendingPopulationXp);
        Assert.AreEqual(
            expected.HeldMilestoneXp,
            actual.HeldMilestoneXp);
        Assert.AreEqual(
            expected.PendingMilestoneClaim.Index,
            actual.PendingMilestoneClaim.Index);
        Assert.AreEqual(
            expected.PendingMilestoneClaim.ReleasedXp,
            actual.PendingMilestoneClaim.ReleasedXp);
        Assert.AreEqual(
            expected.PendingMilestoneClaim.Threshold,
            actual.PendingMilestoneClaim.Threshold);
    }
}
