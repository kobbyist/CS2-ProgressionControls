using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ProgressionConfigurationTests
{
    private const int MegalopolisXp = 100000;

    [DataTestMethod]
    [DataRow(ProgressionPreset.PopulationBalanced, 50)]
    [DataRow(ProgressionPreset.PopulationHeavy, 25)]
    [DataRow(ProgressionPreset.PopulationOnly, 0)]
    public void PresetsExposeExpectedPopulationAndVanillaRules(
        ProgressionPreset preset,
        int vanillaXpPercentage)
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryFromPreset(
                preset,
                MegalopolisXp,
                out var configuration));

        Assert.AreEqual(preset, configuration.Preset);
        Assert.AreEqual(
            vanillaXpPercentage,
            configuration.VanillaXpPercentage);
        Assert.AreEqual(0.5m, configuration.XpPerResident);
    }

    [TestMethod]
    public void CustomConfigurationUsesCustomPreset()
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                0.5d,
                10,
                out var custom));

        Assert.AreEqual(ProgressionPreset.Custom, custom.Preset);
        Assert.AreEqual(10, custom.VanillaXpPercentage);
    }

    [TestMethod]
    public void TargetAndRateRoundTrip()
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                0.4d,
                25,
                out var custom));
        Assert.AreEqual(0.4m, custom.XpPerResident);
        Assert.IsTrue(
            custom.TryGetMegalopolisTarget(
                MegalopolisXp,
                out var target));
        Assert.AreEqual(250000m, target);
    }

    [TestMethod]
    public void VanillaPercentageBoundariesAreAccepted()
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                0.5d,
                0,
                out _));
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                0.5d,
                100,
                out _));
    }

    [DataTestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(-1d)]
    public void InvalidRatesAreRejected(double rate)
    {
        Assert.IsFalse(
            ProgressionConfiguration.TryCreateCustom(
                rate,
                25,
                out _));
    }

    [TestMethod]
    public void ZeroRateIsValidWithoutFiniteTarget()
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                0d,
                25,
                out var configuration));
        Assert.IsFalse(
            configuration.TryGetMegalopolisTarget(
                MegalopolisXp,
                out _));
    }

    [TestMethod]
    public void MaximumRateBoundaryIsValidated()
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                (double)int.MaxValue,
                25,
                out _));
        Assert.IsFalse(
            ProgressionConfiguration.TryCreateCustom(
                (double)int.MaxValue + 1d,
                25,
                out _));
    }

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    public void InvalidVanillaPercentagesAreRejected(int percentage)
    {
        Assert.IsFalse(
            ProgressionConfiguration.TryCreateCustom(
                0.5d,
                percentage,
                out _));
    }

    [TestMethod]
    public void InvalidTargetInputsAreRejected()
    {
        Assert.IsFalse(
            ProgressionConfiguration.TryFromPreset(
                ProgressionPreset.Custom,
                MegalopolisXp,
                out _));
        Assert.IsFalse(
            ProgressionConfiguration.TryFromPreset(
                ProgressionPreset.PopulationHeavy,
                0,
                out _));
        Assert.IsFalse(
            ProgressionRateConverter.TryRateFromTarget(
                MegalopolisXp,
                double.NaN,
                out _));
        Assert.IsFalse(
            ProgressionRateConverter.TryRateFromTarget(
                MegalopolisXp,
                0d,
                out _));
    }

    [DataTestMethod]
    [DataRow(1e-28d)]
    [DataRow(1e-29d)]
    [DataRow(double.Epsilon)]
    public void TinyPositiveTargetsAreRejectedWithoutThrowing(
        double target)
    {
        Assert.IsFalse(
            ProgressionRateConverter.TryRateFromTarget(
                MegalopolisXp,
                target,
                out var rate));
        Assert.AreEqual(0m, rate);
    }

    [TestMethod]
    public void TinyRateTargetOverflowIsRejected()
    {
        Assert.IsTrue(
            ProgressionRateConverter.TryConvertRate(
                1e-28d,
                out var tinyRate));
        Assert.IsFalse(
            ProgressionRateConverter.TryTargetFromRate(
                MegalopolisXp,
                tinyRate,
                out _));
    }

    [TestMethod]
    public void PositiveRateBelowDecimalPrecisionIsRejected()
    {
        Assert.IsFalse(
            ProgressionRateConverter.TryConvertRate(
                1e-29d,
                out var rate));
        Assert.AreEqual(0m, rate);
    }
}
