using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class CheckpointRetentionPolicyTests
{
    private static readonly Guid CityA =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CityB =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [TestMethod]
    public void CurrentCheckpointIsAlwaysRetained()
    {
        var candidates = new[]
        {
            Indexed("current", CityA, 10, "Save/A"),
            Indexed("old", CityA, 9, "Save/A"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: Array.Empty<string>(),
            liveSaveEnumerationTrusted: true,
            legacyLimitPerCity: 0);

        CollectionAssert.AreEqual(
            new[] { "old" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void RepeatedSaveNameRetainsOnlyCurrentCheckpoint()
    {
        var candidates = new[]
        {
            Indexed("current", CityA, 30, "Save/A"),
            Indexed("old-1", CityA, 10, "Save/A"),
            Indexed("old-2", CityA, 20, "Save/A"),
            Indexed("other", CityA, 15, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A", "Save/B" },
            liveSaveEnumerationTrusted: true,
            legacyLimitPerCity: 16);

        CollectionAssert.AreEquivalent(
            new[] { "old-1", "old-2" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void TrustedLiveSaveNamesRemoveOrphanedIndexedCheckpoints()
    {
        var candidates = new[]
        {
            Indexed("current", CityA, 30, "Save/A"),
            Indexed("live", CityA, 20, "Save/B"),
            Indexed("orphan", CityA, 10, "Save/C"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A", "Save/B" },
            liveSaveEnumerationTrusted: true,
            legacyLimitPerCity: 16);

        CollectionAssert.AreEqual(
            new[] { "orphan" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void UntrustedLiveSaveNamesPreservePotentialOrphans()
    {
        var candidates = new[]
        {
            Indexed("current", CityA, 30, "Save/A"),
            Indexed("potential-orphan", CityA, 10, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: Array.Empty<string>(),
            liveSaveEnumerationTrusted: false,
            legacyLimitPerCity: 16);

        Assert.AreEqual(0, deleted.Count);
    }

    [TestMethod]
    public void MissingCurrentSaveInLiveEnumerationPreservesPotentialOrphans()
    {
        var candidates = new[]
        {
            Indexed("current", CityA, 30, "Save/A"),
            Indexed("potential-orphan", CityA, 10, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/B" },
            liveSaveEnumerationTrusted: true,
            legacyLimitPerCity: 16);

        Assert.AreEqual(0, deleted.Count);
    }

    [TestMethod]
    public void LegacyLimitIsAppliedIndependentlyPerCity()
    {
        var candidates = new List<CheckpointRetentionCandidate>
        {
            Indexed("current", CityA, 100, "Save/A"),
        };
        candidates.AddRange(Enumerable.Range(1, 18).Select(index =>
            Legacy($"a-{index:00}", CityA, (uint)index, index)));
        candidates.AddRange(Enumerable.Range(1, 17).Select(index =>
            Legacy($"b-{index:00}", CityB, (uint)index, index)));

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A" },
            liveSaveEnumerationTrusted: true,
            legacyLimitPerCity: 16);

        CollectionAssert.AreEquivalent(
            new[] { "a-01", "a-02", "b-01" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void LegacyTieBreakUsesSimulationFrameThenId()
    {
        var timestamp = new DateTime(
            2026,
            8,
            13,
            12,
            0,
            0,
            DateTimeKind.Utc);
        var candidates = new[]
        {
            Indexed("current", CityA, 100, "Save/A"),
            new CheckpointRetentionCandidate(
                "lower-frame",
                CityA,
                10,
                timestamp,
                saveName: null),
            new CheckpointRetentionCandidate(
                "higher-frame-b",
                CityA,
                20,
                timestamp,
                saveName: null),
            new CheckpointRetentionCandidate(
                "higher-frame-a",
                CityA,
                20,
                timestamp,
                saveName: null),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A" },
            liveSaveEnumerationTrusted: true,
            legacyLimitPerCity: 1);

        CollectionAssert.AreEquivalent(
            new[] { "higher-frame-b", "lower-frame" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void InvalidRequestDoesNotSelectAnyDeletion()
    {
        var candidates = new[]
        {
            Indexed("only", CityA, 10, "Save/A"),
        };

        Assert.AreEqual(
            0,
            CheckpointRetentionPolicy.SelectForDeletion(
                candidates,
                currentId: "missing",
                liveSaveNames: new[] { "Save/A" },
                liveSaveEnumerationTrusted: true,
                legacyLimitPerCity: 16).Count);
        Assert.AreEqual(
            0,
            CheckpointRetentionPolicy.SelectForDeletion(
                candidates,
                currentId: "only",
                liveSaveNames: new[] { "Save/A" },
                liveSaveEnumerationTrusted: true,
                legacyLimitPerCity: -1).Count);
    }

    private static CheckpointRetentionCandidate Indexed(
        string id,
        Guid cityId,
        uint frame,
        string saveName)
    {
        return new CheckpointRetentionCandidate(
            id,
            cityId,
            frame,
            Timestamp(frame),
            saveName);
    }

    private static CheckpointRetentionCandidate Legacy(
        string id,
        Guid cityId,
        uint frame,
        int minutes)
    {
        return new CheckpointRetentionCandidate(
            id,
            cityId,
            frame,
            Timestamp((uint)minutes),
            saveName: null);
    }

    private static DateTime Timestamp(uint minutes)
    {
        return new DateTime(
            2026,
            8,
            13,
            0,
            0,
            0,
            DateTimeKind.Utc).AddMinutes(minutes);
    }
}
