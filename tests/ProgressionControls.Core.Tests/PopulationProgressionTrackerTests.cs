using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class PopulationProgressionTrackerTests
{
    [TestMethod]
    public void FirstObservationEstablishesBaseline()
    {
        var tracker = new PopulationProgressionTracker();

        Assert.IsTrue(tracker.TryObserve(
            100,
            Configuration(0.5d),
            out var awardedXp));
        Assert.AreEqual(0L, awardedXp);
        Assert.AreEqual(100, tracker.MaximumPopulation);
    }

    [TestMethod]
    public void GrowthAwardsOnlyPopulationAboveRecord()
    {
        var tracker = Baseline(100, 0.5d);

        Assert.IsTrue(tracker.TryObserve(
            110,
            Configuration(0.5d),
            out var awardedXp));
        Assert.AreEqual(5L, awardedXp);
        Assert.AreEqual(110, tracker.MaximumPopulation);
    }

    [TestMethod]
    public void DeclineAndRecoveryDoNotAwardXp()
    {
        var tracker = Baseline(100, 1d);

        Assert.IsTrue(tracker.TryObserve(
            80,
            Configuration(1d),
            out var declineXp));
        Assert.AreEqual(0L, declineXp);
        Assert.AreEqual(100, tracker.MaximumPopulation);

        Assert.IsTrue(tracker.TryObserve(
            99,
            Configuration(1d),
            out var recoveryXp));
        Assert.AreEqual(0L, recoveryXp);
        Assert.AreEqual(100, tracker.MaximumPopulation);

        Assert.IsTrue(tracker.TryObserve(
            103,
            Configuration(1d),
            out var newRecordXp));
        Assert.AreEqual(3L, newRecordXp);
        Assert.AreEqual(103, tracker.MaximumPopulation);
    }

    [TestMethod]
    public void ZeroRateTracksRecordWithoutAwardingXp()
    {
        var tracker = Baseline(100, 0d);

        Assert.IsTrue(tracker.TryObserve(
            200,
            Configuration(0d),
            out var awardedXp));
        Assert.AreEqual(0L, awardedXp);
        Assert.AreEqual(200, tracker.MaximumPopulation);
    }

    [TestMethod]
    public void RepeatedGrowthCarriesFractionalXp()
    {
        var tracker = Baseline(100, 0.25d);
        var awards = new long[4];
        for (var index = 0; index < awards.Length; index++)
        {
            Assert.IsTrue(tracker.TryObserve(
                101 + index,
                Configuration(0.25d),
                out awards[index]));
        }

        CollectionAssert.AreEqual(
            new long[] { 0, 0, 0, 1 },
            awards);
        Assert.AreEqual(0m, FractionalXp(tracker));
    }

    [TestMethod]
    public void RebaselineUsesHighestKnownRecordAndClearsFraction()
    {
        var tracker = Baseline(100, 0.25d);
        Assert.IsTrue(tracker.TryObserve(
            101,
            Configuration(0.25d),
            out _));
        Assert.AreEqual(0.25m, FractionalXp(tracker));

        Assert.IsTrue(tracker.TryRebaseline(
            currentPopulation: 90,
            knownMaximumPopulation: 120,
            configuration: Configuration(1d)));
        Assert.AreEqual(120, tracker.MaximumPopulation);
        Assert.AreEqual(0m, FractionalXp(tracker));

        Assert.IsTrue(tracker.TryRebaseline(
            currentPopulation: 100,
            knownMaximumPopulation: 110,
            configuration: Configuration(1d)));
        Assert.AreEqual(120, tracker.MaximumPopulation);

        Assert.IsTrue(tracker.TryObserve(
            119,
            Configuration(1d),
            out var belowRecordXp));
        Assert.AreEqual(0L, belowRecordXp);
        Assert.IsTrue(tracker.TryObserve(
            121,
            Configuration(1d),
            out var newRecordXp));
        Assert.AreEqual(1L, newRecordXp);
    }

    [TestMethod]
    public void RebaselineRejectsInvalidPopulationWithoutChangingState()
    {
        var tracker = Baseline(100, 1d);

        Assert.IsFalse(tracker.TryRebaseline(
            currentPopulation: 101,
            knownMaximumPopulation: -1,
            configuration: Configuration(1d)));
        Assert.AreEqual(100, tracker.MaximumPopulation);
        Assert.AreEqual(0m, FractionalXp(tracker));
    }

    [TestMethod]
    public void RateChangeIsProspectiveAndClearsOldFraction()
    {
        var tracker = Baseline(100, 0.25d);
        Assert.IsTrue(tracker.TryObserve(
            101,
            Configuration(0.25d),
            out _));
        Assert.AreEqual(0.25m, FractionalXp(tracker));

        Assert.IsTrue(tracker.TryObserve(
            103,
            Configuration(0.5d),
            out var reconfiguredXp));
        Assert.AreEqual(0L, reconfiguredXp);
        Assert.AreEqual(0m, FractionalXp(tracker));

        Assert.IsTrue(tracker.TryObserve(
            104,
            Configuration(0.5d),
            out var futureGrowthXp));
        Assert.AreEqual(0L, futureGrowthXp);
        Assert.AreEqual(0.5m, FractionalXp(tracker));
    }

    [TestMethod]
    public void ValidStateRestoresFractionAcrossReload()
    {
        var original = Baseline(100, 0.25d);
        Assert.IsTrue(original.TryObserve(
            103,
            Configuration(0.25d),
            out _));

        Assert.IsTrue(PopulationProgressionTracker.TryRestore(
            original.CaptureState(),
            Configuration(0.25d),
            out var restored));
        Assert.IsTrue(restored.TryObserve(
            104,
            Configuration(0.25d),
            out var awardedXp));
        Assert.AreEqual(1L, awardedXp);
        Assert.AreEqual(0m, FractionalXp(restored));
    }

    [TestMethod]
    public void InvalidStateAndPopulationAreRejected()
    {
        Assert.IsFalse(PopulationProgressionTracker.TryRestore(
            new PopulationProgressionState(-1, 0m),
            Configuration(1d),
            out _));
        Assert.IsFalse(PopulationProgressionTracker.TryRestore(
            new PopulationProgressionState(100, 1m),
            Configuration(1d),
            out _));

        var tracker = Baseline(100, 1d);
        Assert.IsFalse(tracker.TryObserve(
            -1,
            Configuration(1d),
            out _));
        Assert.AreEqual(100, tracker.MaximumPopulation);
    }

    [TestMethod]
    public void LargePopulationDeltaUsesLongXpTotal()
    {
        var tracker = Baseline(0, 2d);

        Assert.IsTrue(tracker.TryObserve(
            int.MaxValue,
            Configuration(2d),
            out var awardedXp));
        Assert.AreEqual(4294967294L, awardedXp);
        Assert.AreEqual(int.MaxValue, tracker.MaximumPopulation);
    }

    private static PopulationProgressionTracker Baseline(
        int population,
        double rate)
    {
        var tracker = new PopulationProgressionTracker();
        Assert.IsTrue(tracker.TryObserve(
            population,
            Configuration(rate),
            out _));
        return tracker;
    }

    private static decimal FractionalXp(
        PopulationProgressionTracker tracker)
    {
        return tracker.CaptureState()!.FractionalXp;
    }

    private static ProgressionConfiguration Configuration(double rate)
    {
        Assert.IsTrue(ProgressionConfiguration.TryCreateCustom(
            rate,
            vanillaXpPercentage: 25,
            out var configuration));
        return configuration;
    }
}
