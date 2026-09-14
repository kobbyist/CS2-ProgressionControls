using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class CheckpointRetentionPolicyTests
{
    [TestMethod]
    public void CurrentCheckpointIsAlwaysRetained()
    {
        var candidates = new[]
        {
            Indexed("current", 10, "Save/A"),
            Indexed("old", 9, "Save/A"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: Array.Empty<string>(),
            liveSaveEnumerationTrusted: true);

        CollectionAssert.AreEqual(
            new[] { "old" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void RepeatedSaveNameRetainsOnlyCurrentCheckpoint()
    {
        var candidates = new[]
        {
            Indexed("current", 30, "Save/A"),
            Indexed("old-1", 10, "Save/A"),
            Indexed("old-2", 20, "Save/A"),
            Indexed("other", 15, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A", "Save/B" },
            liveSaveEnumerationTrusted: true);

        CollectionAssert.AreEquivalent(
            new[] { "old-1", "old-2" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void TrustedLiveSaveNamesRemoveOrphanedCheckpoints()
    {
        var candidates = new[]
        {
            Indexed("current", 30, "Save/A"),
            Indexed("live", 20, "Save/B"),
            Indexed("orphan", 10, "Save/C"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A", "Save/B" },
            liveSaveEnumerationTrusted: true);

        CollectionAssert.AreEqual(
            new[] { "orphan" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void UntrustedLiveSaveNamesPreservePotentialOrphans()
    {
        var candidates = new[]
        {
            Indexed("current", 30, "Save/A"),
            Indexed("potential-orphan", 10, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: Array.Empty<string>(),
            liveSaveEnumerationTrusted: false);

        Assert.AreEqual(0, deleted.Count);
    }

    [TestMethod]
    public void MissingCurrentSaveInLiveEnumerationPreservesPotentialOrphans()
    {
        var candidates = new[]
        {
            Indexed("current", 30, "Save/A"),
            Indexed("potential-orphan", 10, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/B" },
            liveSaveEnumerationTrusted: true);

        Assert.AreEqual(0, deleted.Count);
    }

    [TestMethod]
    public void OwnerTieBreakUsesSimulationFrameThenId()
    {
        var timestamp = new DateTime(
            2026,
            8,
            30,
            12,
            0,
            0,
            DateTimeKind.Utc);
        var candidates = new[]
        {
            Indexed("current", 100, "Save/A"),
            Candidate("lower-frame", 10, timestamp, "Save/B"),
            Candidate("higher-frame-b", 20, timestamp, "Save/B"),
            Candidate("higher-frame-a", 20, timestamp, "Save/B"),
        };

        var deleted = CheckpointRetentionPolicy.SelectForDeletion(
            candidates,
            currentId: "current",
            liveSaveNames: new[] { "Save/A", "Save/B" },
            liveSaveEnumerationTrusted: true);

        CollectionAssert.AreEquivalent(
            new[] { "higher-frame-b", "lower-frame" },
            deleted.Select(candidate => candidate.Id).ToArray());
    }

    [TestMethod]
    public void InvalidRequestDoesNotSelectAnyDeletion()
    {
        var candidates = new[]
        {
            Indexed("only", 10, "Save/A"),
        };

        Assert.AreEqual(
            0,
            CheckpointRetentionPolicy.SelectForDeletion(
                candidates,
                currentId: "missing",
                liveSaveNames: new[] { "Save/A" },
                liveSaveEnumerationTrusted: true).Count);
    }

    private static CheckpointRetentionCandidate Indexed(
        string id,
        uint frame,
        string saveName)
    {
        return Candidate(id, frame, Timestamp(frame), saveName);
    }

    private static CheckpointRetentionCandidate Candidate(
        string id,
        uint frame,
        DateTime timestamp,
        string saveName)
    {
        return new CheckpointRetentionCandidate(
            id,
            frame,
            timestamp,
            saveName);
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
