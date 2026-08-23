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
        new ManualMilestoneDefinition(4, 400),
    };

    [TestMethod]
    public void ListsEveryMilestoneSupportedByEffectiveXp()
    {
        var queue = ManualMilestoneQueue.Build(
            achievedMilestone: 1,
            cityXp: 199,
            heldXp: 151,
            claimPending: false,
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
            Milestones);

        Assert.AreEqual(0, queue.Count);
    }
}
