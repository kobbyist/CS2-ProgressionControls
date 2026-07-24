using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class PopulationProgressionTrackerTests
{
    [TestMethod]
    public void FirstObservationEstablishesBaseline()
    {
        var tracker = new PopulationProgressionTracker();
        var result = tracker.Observe(
            100,
            customProgressionEnabled: true,
            Configuration(0.5d));

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(result.EstablishedBaseline);
        Assert.AreEqual(0L, result.AwardedXp);
        Assert.AreEqual(100, result.MaximumPopulation);
    }

    [TestMethod]
    public void GrowthAwardsOnlyPopulationAboveRecord()
    {
        var tracker = Baseline(100, 0.5d);

        var result = tracker.Observe(110, true, Configuration(0.5d));

        Assert.AreEqual(10, result.NewRecordDelta);
        Assert.AreEqual(5L, result.AwardedXp);
        Assert.AreEqual(110, result.MaximumPopulation);
    }

    [TestMethod]
    public void DeclineAndRecoveryDoNotAwardXp()
    {
        var tracker = Baseline(100, 1d);

        var decline = tracker.Observe(80, true, Configuration(1d));
        var recovery = tracker.Observe(99, true, Configuration(1d));
        var newRecord = tracker.Observe(103, true, Configuration(1d));

        Assert.AreEqual(0L, decline.AwardedXp);
        Assert.AreEqual(20, decline.PopulationToResume);
        Assert.AreEqual(0L, recovery.AwardedXp);
        Assert.AreEqual(1, recovery.PopulationToResume);
        Assert.AreEqual(3L, newRecord.AwardedXp);
        Assert.AreEqual(3, newRecord.NewRecordDelta);
    }

    [TestMethod]
    public void ZeroRateTracksRecordWithoutAwardingXp()
    {
        var tracker = Baseline(100, 0d);

        var result = tracker.Observe(
            200,
            true,
            Configuration(0d));

        Assert.AreEqual(0L, result.AwardedXp);
        Assert.AreEqual(100, result.NewRecordDelta);
        Assert.AreEqual(200, result.MaximumPopulation);
    }

    [TestMethod]
    public void RepeatedGrowthCarriesFractionalXp()
    {
        var tracker = Baseline(100, 0.25d);
        var awards = new[]
        {
            tracker.Observe(101, true, Configuration(0.25d)).AwardedXp,
            tracker.Observe(102, true, Configuration(0.25d)).AwardedXp,
            tracker.Observe(103, true, Configuration(0.25d)).AwardedXp,
            tracker.Observe(104, true, Configuration(0.25d)).AwardedXp,
        };

        CollectionAssert.AreEqual(
            new long[] { 0, 0, 0, 1 },
            awards);
        Assert.AreEqual(0m, tracker.FractionalXp);
    }

    [TestMethod]
    public void DisabledGrowthBecomesNewBaseline()
    {
        var tracker = Baseline(100, 1d);

        var disabled = tracker.Observe(120, false, Configuration(1d));
        var reenabled = tracker.Observe(125, true, Configuration(1d));
        var futureGrowth = tracker.Observe(126, true, Configuration(1d));

        Assert.IsTrue(disabled.EstablishedBaseline);
        Assert.AreEqual(120, disabled.MaximumPopulation);
        Assert.IsTrue(reenabled.EstablishedBaseline);
        Assert.AreEqual(125, reenabled.MaximumPopulation);
        Assert.AreEqual(0L, reenabled.AwardedXp);
        Assert.AreEqual(1L, futureGrowth.AwardedXp);
    }

    [TestMethod]
    public void RebaselineUsesHighestKnownRecordAndClearsFraction()
    {
        var tracker = Baseline(100, 0.25d);
        tracker.Observe(101, true, Configuration(0.25d));
        Assert.AreEqual(0.25m, tracker.FractionalXp);

        var result = tracker.Rebaseline(
            currentPopulation: 90,
            knownMaximumPopulation: 120,
            configuration: Configuration(1d));

        Assert.IsTrue(result.Accepted);
        Assert.IsTrue(result.EstablishedBaseline);
        Assert.AreEqual(0L, result.AwardedXp);
        Assert.AreEqual(120, result.MaximumPopulation);
        Assert.AreEqual(30, result.PopulationToResume);
        Assert.AreEqual(0m, tracker.FractionalXp);

        var preserved = tracker.Rebaseline(
            currentPopulation: 100,
            knownMaximumPopulation: 110,
            configuration: Configuration(1d));

        Assert.AreEqual(120, preserved.MaximumPopulation);

        var belowRecord = tracker.Observe(
            119,
            true,
            Configuration(1d));
        var newRecord = tracker.Observe(
            121,
            true,
            Configuration(1d));

        Assert.AreEqual(0L, belowRecord.AwardedXp);
        Assert.AreEqual(1L, newRecord.AwardedXp);
    }

    [TestMethod]
    public void RebaselineRejectsInvalidPopulationWithoutChangingState()
    {
        var tracker = Baseline(100, 1d);

        var result = tracker.Rebaseline(
            currentPopulation: 101,
            knownMaximumPopulation: -1,
            configuration: Configuration(1d));

        Assert.IsFalse(result.Accepted);
        Assert.AreEqual(100, tracker.MaximumPopulation);
        Assert.AreEqual(0m, tracker.FractionalXp);
    }

    [TestMethod]
    public void RateChangeIsProspectiveAndClearsOldFraction()
    {
        var tracker = Baseline(100, 0.25d);
        tracker.Observe(101, true, Configuration(0.25d));
        Assert.AreEqual(0.25m, tracker.FractionalXp);

        var reconfigured = tracker.Observe(
            103,
            true,
            Configuration(0.5d));

        Assert.IsTrue(reconfigured.EstablishedBaseline);
        Assert.AreEqual(0m, tracker.FractionalXp);
        Assert.AreEqual(0L, reconfigured.AwardedXp);

        var futureGrowth = tracker.Observe(
            104,
            true,
            Configuration(0.5d));

        Assert.AreEqual(0L, futureGrowth.AwardedXp);
        Assert.AreEqual(0.5m, tracker.FractionalXp);
    }

    [TestMethod]
    public void ValidStateRestoresFractionAcrossReload()
    {
        var original = Baseline(100, 0.25d);
        original.Observe(103, true, Configuration(0.25d));
        var state = original.CaptureState();

        Assert.IsTrue(
            PopulationProgressionTracker.TryRestore(
                state,
                Configuration(0.25d),
                customProgressionEnabled: true,
                out var restored));

        var result = restored.Observe(
            104,
            true,
            Configuration(0.25d));

        Assert.AreEqual(1L, result.AwardedXp);
        Assert.AreEqual(0m, restored.FractionalXp);
    }

    [TestMethod]
    public void InvalidStateAndPopulationAreRejected()
    {
        Assert.IsFalse(
            PopulationProgressionTracker.TryRestore(
                new PopulationProgressionState(-1, 0m),
                Configuration(1d),
                true,
                out _));
        Assert.IsFalse(
            PopulationProgressionTracker.TryRestore(
                new PopulationProgressionState(100, 1m),
                Configuration(1d),
                true,
                out _));

        var tracker = Baseline(100, 1d);
        var rejected = tracker.Observe(
            -1,
            true,
            Configuration(1d));

        Assert.IsFalse(rejected.Accepted);
        Assert.AreEqual(100, tracker.MaximumPopulation);
    }

    [TestMethod]
    public void LargePopulationDeltaUsesLongXpTotal()
    {
        var tracker = Baseline(0, 2d);

        var result = tracker.Observe(
            int.MaxValue,
            true,
            Configuration(2d));

        Assert.AreEqual(4294967294L, result.AwardedXp);
        Assert.AreEqual(int.MaxValue, result.NewRecordDelta);
    }

    private static PopulationProgressionTracker Baseline(
        int population,
        double rate)
    {
        var tracker = new PopulationProgressionTracker();
        tracker.Observe(
            population,
            customProgressionEnabled: true,
            Configuration(rate));
        return tracker;
    }

    private static ProgressionConfiguration Configuration(double rate)
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                populationXpEnabled: true,
                rate,
                vanillaXpPercentage: 25,
                out var configuration));
        return configuration;
    }
}
