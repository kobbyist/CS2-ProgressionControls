using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class PopulationXpBatchTests
{
    [TestMethod]
    public void PositiveAwardsAccumulateUntilTaken()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryAdd(3));
        Assert.IsTrue(batch.TryAdd(7));
        Assert.AreEqual(10L, batch.PendingXp);
        Assert.AreEqual(10, batch.TakeNextInt32Chunk());
        Assert.AreEqual(0L, batch.PendingXp);
    }

    [TestMethod]
    public void LargeAwardsAreTakenInInt32Chunks()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryAdd((long)int.MaxValue + 7));
        Assert.AreEqual(int.MaxValue, batch.TakeNextInt32Chunk());
        Assert.AreEqual(7L, batch.PendingXp);
        Assert.AreEqual(7, batch.TakeNextInt32Chunk());
        Assert.AreEqual(0L, batch.PendingXp);
        Assert.AreEqual(0, batch.TakeNextInt32Chunk());
    }

    [TestMethod]
    public void ZeroAwardIsAcceptedWithoutChangingBatch()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryAdd(5));
        Assert.IsTrue(batch.TryAdd(0));
        Assert.AreEqual(5L, batch.PendingXp);
    }

    [TestMethod]
    public void NegativeAwardIsRejectedAtomically()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryAdd(5));
        Assert.IsFalse(batch.TryAdd(-1));
        Assert.AreEqual(5L, batch.PendingXp);
    }

    [TestMethod]
    public void OverflowIsRejectedAtomically()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryRestore(long.MaxValue));
        Assert.IsFalse(batch.TryAdd(1));
        Assert.AreEqual(long.MaxValue, batch.PendingXp);
    }

    [TestMethod]
    public void ValidPendingXpCanBeRestored()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryRestore(42));
        Assert.AreEqual(42L, batch.PendingXp);
    }

    [TestMethod]
    public void NegativeRestoreIsRejectedAtomically()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryRestore(8));
        Assert.IsFalse(batch.TryRestore(-1));
        Assert.AreEqual(8L, batch.PendingXp);
    }

    [TestMethod]
    public void ClearDiscardsPendingXp()
    {
        var batch = new PopulationXpBatch();

        Assert.IsTrue(batch.TryAdd(9));
        batch.Clear();

        Assert.AreEqual(0L, batch.PendingXp);
    }
}
