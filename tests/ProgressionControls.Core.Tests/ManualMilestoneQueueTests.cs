using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ManualMilestoneQueueTests
{
    private static readonly ManualMilestoneDefinition[] Milestones =
    {
        new ManualMilestoneDefinition(1, 100),
        new ManualMilestoneDefinition(2, 200),
        new ManualMilestoneDefinition(3, 300),
        new ManualMilestoneDefinition(4, 400, isFinal: true),
    };

    [TestMethod]
    public void ListsEveryMilestoneSupportedByEffectiveXp()
    {
        var queue = CreateCatalog(Milestones).Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: false,
            claimsActive: true);

        Assert.AreEqual(2, queue.Count);
        Assert.AreEqual(2, queue[0].Index);
        Assert.IsTrue(queue[0].CanClaim);
        Assert.AreEqual(3, queue[1].Index);
        Assert.IsFalse(queue[1].CanClaim);
    }

    [TestMethod]
    public void ValidatedCatalogCanServeRepeatedQueries()
    {
        var catalog = CreateCatalog(Milestones.Reverse());

        var firstQueue = catalog.Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: false,
            claimsActive: true);
        var secondQueue = catalog.Build(
            achievedMilestone: 2,
            cityXp: 299,
            heldXp: 101,
            claimPending: false,
            claimsActive: true);

        Assert.AreEqual(2, firstQueue.Count);
        Assert.AreEqual(2, firstQueue[0].Index);
        Assert.AreEqual(2, secondQueue.Count);
        Assert.AreEqual(3, secondQueue[0].Index);
        Assert.IsTrue(catalog.TryGetNext(
            achievedMilestone: 2,
            out var next,
            out var finalMilestoneReached));
        Assert.AreEqual(3, next.Index);
        Assert.IsFalse(finalMilestoneReached);
    }

    [TestMethod]
    public void ZeroXpFirstMilestoneProducesAValidCatalog()
    {
        var catalog = CreateCatalog(new[]
        {
            new ManualMilestoneDefinition(1, 0),
            new ManualMilestoneDefinition(2, 100, isFinal: true),
        });

        Assert.IsTrue(catalog.TryGetNext(
            achievedMilestone: 1,
            out var next,
            out var finalMilestoneReached));
        Assert.AreEqual(2, next.Index);
        Assert.IsFalse(finalMilestoneReached);
    }

    [TestMethod]
    public void PendingClaimDisablesEveryQueueEntry()
    {
        var queue = CreateCatalog(Milestones).Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: true,
            claimsActive: true);

        Assert.AreEqual(2, queue.Count);
        Assert.IsTrue(queue.All(entry => !entry.CanClaim));
    }

    [TestMethod]
    public void InvalidCityStateProducesNoQueue()
    {
        var queue = CreateCatalog(Milestones).Build(
            achievedMilestone: -1,
            cityXp: 0,
            heldXp: 0,
            claimPending: false,
            claimsActive: true);

        Assert.AreEqual(0, queue.Count);
    }

    [TestMethod]
    public void InactiveClaimsDisableEveryQueueEntry()
    {
        var queue = CreateCatalog(Milestones).Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: false,
            claimsActive: false);

        Assert.AreEqual(2, queue.Count);
        Assert.IsTrue(queue.All(entry => !entry.CanClaim));
    }

    [TestMethod]
    public void MarkedFinalMilestoneProvesFinalMilestone()
    {
        Assert.IsFalse(CreateCatalog(Milestones).TryGetNext(
            achievedMilestone: 4,
            out _,
            out var finalMilestoneReached));

        Assert.IsTrue(finalMilestoneReached);
    }

    [TestMethod]
    public void EmptyMilestoneDefinitionsAreRejected()
    {
        Assert.IsFalse(ManualMilestoneCatalog.TryCreate(
            Array.Empty<ManualMilestoneDefinition>(),
            out _));
    }

    [TestMethod]
    public void MissingFinalMarkerIsRejected()
    {
        Assert.IsFalse(ManualMilestoneCatalog.TryCreate(
            Milestones.Select(milestone =>
                new ManualMilestoneDefinition(
                    milestone.Index,
                    milestone.RequiredXp)),
            out _));
    }

    [TestMethod]
    public void GappedMilestoneDefinitionsAreRejected()
    {
        Assert.IsFalse(ManualMilestoneCatalog.TryCreate(
            new[]
            {
                new ManualMilestoneDefinition(1, 100),
                new ManualMilestoneDefinition(3, 300, isFinal: true),
            },
            out _));
    }

    [TestMethod]
    public void NonIncreasingThresholdsAreRejected()
    {
        Assert.IsFalse(ManualMilestoneCatalog.TryCreate(
            new[]
            {
                new ManualMilestoneDefinition(1, 200),
                new ManualMilestoneDefinition(2, 100, isFinal: true),
            },
            out _));
    }

    [TestMethod]
    public void DuplicateMilestoneIndexesAreRejected()
    {
        Assert.IsFalse(ManualMilestoneCatalog.TryCreate(
            new[]
            {
                new ManualMilestoneDefinition(1, 100),
                new ManualMilestoneDefinition(1, 200, isFinal: true),
            },
            out _));
    }

    private static ManualMilestoneCatalog CreateCatalog(
        IEnumerable<ManualMilestoneDefinition> definitions)
    {
        Assert.IsTrue(ManualMilestoneCatalog.TryCreate(
            definitions,
            out var catalog));
        return catalog;
    }
}
