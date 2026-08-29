using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class MilestoneTierProgressTests
{
    [TestMethod]
    public void ConvertsCumulativeXpToCurrentTierProgress()
    {
        var progress = MilestoneTierProgress.Calculate(
            effectiveXp: 3833,
            achievedMilestoneThreshold: 2500,
            nextMilestoneThreshold: 4800);

        Assert.AreEqual(1333L, progress.CurrentXp);
        Assert.AreEqual(2300, progress.RequiredXp);
    }

    [TestMethod]
    public void CapsEarnedProgressAtTheTierRequirement()
    {
        var progress = MilestoneTierProgress.Calculate(
            effectiveXp: 5200,
            achievedMilestoneThreshold: 2500,
            nextMilestoneThreshold: 4800);

        Assert.AreEqual(2300L, progress.CurrentXp);
        Assert.AreEqual(2300, progress.RequiredXp);
    }

    [TestMethod]
    public void InvalidTierProducesEmptyProgress()
    {
        var progress = MilestoneTierProgress.Calculate(
            effectiveXp: 3833,
            achievedMilestoneThreshold: 4800,
            nextMilestoneThreshold: 4800);

        Assert.AreEqual(0L, progress.CurrentXp);
        Assert.AreEqual(0, progress.RequiredXp);
    }
}
