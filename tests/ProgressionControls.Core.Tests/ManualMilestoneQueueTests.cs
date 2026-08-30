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
        var queue = ManualMilestoneQueue.Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: false,
            claimsActive: true,
            Milestones);

        Assert.AreEqual(2, queue.Count);
        Assert.AreEqual(2, queue[0].Index);
        Assert.IsTrue(queue[0].CanClaim);
        Assert.AreEqual(3, queue[1].Index);
        Assert.IsFalse(queue[1].CanClaim);
    }

    [TestMethod]
    public void PendingClaimDisablesEveryQueueEntry()
    {
        var queue = ManualMilestoneQueue.Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: true,
            claimsActive: true,
            Milestones);

        Assert.AreEqual(2, queue.Count);
        Assert.IsTrue(queue.All(entry => !entry.CanClaim));
    }

    [TestMethod]
    public void DuplicateMilestoneIndexesRejectTheQueue()
    {
        var queue = ManualMilestoneQueue.Build(
            achievedMilestone: 0,
            cityXp: 99,
            heldXp: 500,
            claimPending: false,
            claimsActive: true,
            new[]
            {
                new ManualMilestoneDefinition(1, 100),
                new ManualMilestoneDefinition(1, 200),
            });

        Assert.AreEqual(0, queue.Count);
    }

    [TestMethod]
    public void InvalidCityStateProducesNoQueue()
    {
        var queue = ManualMilestoneQueue.Build(
            achievedMilestone: -1,
            cityXp: 0,
            heldXp: 0,
            claimPending: false,
            claimsActive: true,
            Milestones);

        Assert.AreEqual(0, queue.Count);
    }

    [TestMethod]
    public void InactiveClaimsDisableEveryQueueEntry()
    {
        var queue = ManualMilestoneQueue.Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: false,
            claimsActive: false,
            Milestones);

        Assert.AreEqual(2, queue.Count);
        Assert.IsTrue(queue.All(entry => !entry.CanClaim));
    }

    [TestMethod]
    public void EmptyMilestoneDefinitionsDoNotProveFinalMilestone()
    {
        Assert.IsFalse(ManualMilestoneQueue.TryGetNext(
            achievedMilestone: 4,
            Array.Empty<ManualMilestoneDefinition>(),
            out _,
            out var finalMilestoneReached));

        Assert.IsFalse(finalMilestoneReached);
    }

    [TestMethod]
    public void HighestKnownMilestoneWithoutFinalMarkerDoesNotProveFinalMilestone()
    {
        Assert.IsFalse(ManualMilestoneQueue.TryGetNext(
            achievedMilestone: 4,
            Milestones.Select(milestone =>
                new ManualMilestoneDefinition(
                    milestone.Index,
                    milestone.RequiredXp)),
            out _,
            out var finalMilestoneReached));

        Assert.IsFalse(finalMilestoneReached);
    }

    [TestMethod]
    public void MarkedFinalMilestoneProvesFinalMilestone()
    {
        Assert.IsFalse(ManualMilestoneQueue.TryGetNext(
            achievedMilestone: 4,
            Milestones,
            out _,
            out var finalMilestoneReached));

        Assert.IsTrue(finalMilestoneReached);
    }

    [TestMethod]
    public void GappedMilestoneDefinitionsDoNotProveFinalMilestone()
    {
        Assert.IsFalse(ManualMilestoneQueue.TryGetNext(
            achievedMilestone: 3,
            new[]
            {
                new ManualMilestoneDefinition(1, 100),
                new ManualMilestoneDefinition(3, 300),
            },
            out _,
            out var finalMilestoneReached));

        Assert.IsFalse(finalMilestoneReached);
    }

    [TestMethod]
    public void NonIncreasingThresholdsDoNotProveFinalMilestone()
    {
        Assert.IsFalse(ManualMilestoneQueue.TryGetNext(
            achievedMilestone: 2,
            new[]
            {
                new ManualMilestoneDefinition(1, 200),
                new ManualMilestoneDefinition(2, 100),
            },
            out _,
            out var finalMilestoneReached));

        Assert.IsFalse(finalMilestoneReached);
    }

    [TestMethod]
    public void DuplicateMilestoneIndexesDoNotProveFinalMilestone()
    {
        Assert.IsFalse(ManualMilestoneQueue.TryGetNext(
            achievedMilestone: 1,
            new[]
            {
                new ManualMilestoneDefinition(1, 100),
                new ManualMilestoneDefinition(1, 200),
            },
            out _,
            out var finalMilestoneReached));

        Assert.IsFalse(finalMilestoneReached);
    }
}
