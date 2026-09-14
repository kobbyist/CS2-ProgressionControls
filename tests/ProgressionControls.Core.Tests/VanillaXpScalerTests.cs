using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class VanillaXpScalerTests
{
    [TestMethod]
    public void RepeatedSmallGainsCarryFractionalXp()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: true, percentage: 25);
        var inputs = new[]
        {
            2, 3, 2, 3, 3, 1, 2, 6, 2, 3, 3, 3, 3, 2, 3, 3, 4, 3, 2,
        };

        var output = inputs.Sum(scaler.Scale);

        Assert.AreEqual(53, inputs.Sum());
        Assert.AreEqual(13, output);
        Assert.AreEqual(25, scaler.RemainderHundredths);
    }

    [TestMethod]
    public void ZeroPercentSuppressesAllXp()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: true, percentage: 0);

        Assert.AreEqual(0, scaler.Scale(53));
        Assert.AreEqual(0, scaler.RemainderHundredths);
    }

    [TestMethod]
    public void OneHundredPercentPassesXpThrough()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: true, percentage: 100);

        Assert.AreEqual(53, scaler.Scale(53));
        Assert.AreEqual(0, scaler.RemainderHundredths);
        Assert.IsFalse(scaler.TransformsPositiveXp);
    }

    [TestMethod]
    public void DisabledScalerPassesXpThrough()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: false, percentage: 25);

        Assert.AreEqual(53, scaler.Scale(53));
        Assert.AreEqual(0, scaler.RemainderHundredths);
        Assert.IsFalse(scaler.TransformsPositiveXp);
    }

    [TestMethod]
    public void PartialScaleRequiresQueueTransformation()
    {
        var scaler = new VanillaXpScaler();

        scaler.Configure(enabled: true, percentage: 25);

        Assert.IsTrue(scaler.TransformsPositiveXp);
    }

    [TestMethod]
    public void ConfigurationChangeDiscardsPriorRemainder()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: true, percentage: 25);
        Assert.AreEqual(0, scaler.Scale(3));
        Assert.AreEqual(75, scaler.RemainderHundredths);

        scaler.Configure(enabled: true, percentage: 50);

        Assert.AreEqual(0, scaler.RemainderHundredths);
        Assert.AreEqual(1, scaler.Scale(3));
        Assert.AreEqual(50, scaler.RemainderHundredths);
    }

    [TestMethod]
    public void ValidRemainderCanBeRestored()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: true, percentage: 25);

        Assert.IsTrue(scaler.TryRestoreRemainder(75));
        Assert.AreEqual(75, scaler.RemainderHundredths);
        Assert.AreEqual(1, scaler.Scale(1));
        Assert.AreEqual(0, scaler.RemainderHundredths);
    }

    [TestMethod]
    public void InvalidRemainderIsRejectedWithoutChangingState()
    {
        var scaler = new VanillaXpScaler();
        scaler.Configure(enabled: true, percentage: 25);
        Assert.IsTrue(scaler.TryRestoreRemainder(50));

        Assert.IsFalse(scaler.TryRestoreRemainder(-1));
        Assert.IsFalse(scaler.TryRestoreRemainder(100));
        Assert.AreEqual(50, scaler.RemainderHundredths);
    }
}
