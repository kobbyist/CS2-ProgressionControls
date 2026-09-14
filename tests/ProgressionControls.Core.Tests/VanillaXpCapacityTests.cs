using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class VanillaXpCapacityTests
{
    [TestMethod]
    public void CompleteAmountFits()
    {
        Assert.IsTrue(VanillaXpCapacity.TryAdd(
            currentXp: int.MaxValue - 10,
            externalXp: 10m,
            out var updatedXp));

        Assert.AreEqual(int.MaxValue, updatedXp);
    }

    [TestMethod]
    public void SaturatingAmountIsRejectedAtomically()
    {
        const int currentXp = int.MaxValue - 10;

        Assert.IsFalse(VanillaXpCapacity.TryAdd(
            currentXp,
            externalXp: 11m,
            out var updatedXp));

        Assert.AreEqual(currentXp, updatedXp);
    }

    [TestMethod]
    public void FractionalAmountIsRejected()
    {
        Assert.IsFalse(VanillaXpCapacity.TryAdd(
            currentXp: 100,
            externalXp: 0.5m,
            out var updatedXp));

        Assert.AreEqual(100, updatedXp);
    }
}
