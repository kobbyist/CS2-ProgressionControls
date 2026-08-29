using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class MilestoneRangeProgressTests
{
    [TestMethod]
    public void PreservesCumulativeXpWithinTheMilestoneRange()
    {
        var progress = MilestoneRangeProgress.Calculate(
            effectiveXp: 4800,
            milestoneThreshold: 8300);

        Assert.AreEqual(4800L, progress.CurrentXp);
        Assert.AreEqual(8300, progress.RequiredXp);
    }

    [TestMethod]
    public void CapsEarnedMilestoneAtItsThreshold()
    {
        var progress = MilestoneRangeProgress.Calculate(
            effectiveXp: 12271,
            milestoneThreshold: 8300);

        Assert.AreEqual(8300L, progress.CurrentXp);
        Assert.AreEqual(8300, progress.RequiredXp);
    }

    [TestMethod]
    public void InvalidThresholdProducesEmptyProgress()
    {
        var progress = MilestoneRangeProgress.Calculate(
            effectiveXp: 4800,
            milestoneThreshold: 0);

        Assert.AreEqual(0L, progress.CurrentXp);
        Assert.AreEqual(0, progress.RequiredXp);
    }
}
