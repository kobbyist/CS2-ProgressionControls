using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class EvaluationIntervalTests
{
    [DataTestMethod]
    [DataRow(16, 16384)]
    [DataRow(64, 4096)]
    [DataRow(256, 1024)]
    [DataRow(1024, 256)]
    [DataRow(4096, 64)]
    [DataRow(16384, 16)]
    public void SupportedCadencesDivideVanillaDayExactly(
        int evaluationsPerDay,
        int expectedInterval)
    {
        Assert.IsTrue(
            EvaluationInterval.TryCalculate(
                ticksPerDay: 262144,
                evaluationsPerDay,
                minimumInterval: 16,
                out var interval));
        Assert.AreEqual(expectedInterval, interval);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(3)]
    [DataRow(32768)]
    public void InvalidOrTooFrequentCadenceIsRejected(
        int evaluationsPerDay)
    {
        Assert.IsFalse(
            EvaluationInterval.TryCalculate(
                ticksPerDay: 262144,
                evaluationsPerDay,
                minimumInterval: 16,
                out var interval));
        Assert.AreEqual(0, interval);
    }
}
