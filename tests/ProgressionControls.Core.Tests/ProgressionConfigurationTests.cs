using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ProgressionConfigurationTests
{
    [DataTestMethod]
    [DataRow(ProgressionPreset.PopulationBalanced, 50)]
    [DataRow(ProgressionPreset.PopulationHeavy, 25)]
    [DataRow(ProgressionPreset.PopulationOnly, 0)]
    public void PresetsUseVanillaPopulationRate(
        ProgressionPreset preset,
        int vanillaXpPercentage)
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryFromPreset(
                preset,
                out var configuration));

        Assert.AreEqual(preset, configuration.Preset);
        Assert.AreEqual(1.5m, configuration.XpPerResident);
        Assert.AreEqual(
            vanillaXpPercentage,
            configuration.VanillaXpPercentage);
    }

    [TestMethod]
    public void CustomPresetCannotBeResolvedAsBuiltIn()
    {
        Assert.IsFalse(
            ProgressionConfiguration.TryFromPreset(
                ProgressionPreset.Custom,
                out _));
    }

    [DataTestMethod]
    [DataRow(0d)]
    [DataRow(0.25d)]
    [DataRow(1.5d)]
    [DataRow(9.75d)]
    [DataRow(10d)]
    public void SliderRateBoundariesAndStepsAreAccepted(double rate)
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                rate,
                25,
                out var configuration));
        Assert.AreEqual((decimal)rate, configuration.XpPerResident);
        Assert.AreEqual(ProgressionPreset.Custom, configuration.Preset);
    }

    [DataTestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(-0.25d)]
    [DataRow(10.25d)]
    [DataRow(1.3d)]
    [DataRow(1e-29d)]
    public void InvalidOrOffStepRatesAreRejectedWithoutThrowing(
        double rate)
    {
        Assert.IsFalse(
            ProgressionConfiguration.TryCreateCustom(
                rate,
                25,
                out _));
    }

    [TestMethod]
    public void DefaultRateMatchesNominalVanillaPopulationRate()
    {
        Assert.AreEqual(
            1.5m,
            ProgressionConfiguration.DefaultXpPerResident);
        Assert.AreEqual(
            0.25m,
            ProgressionConfiguration.XpPerResidentStep);
        Assert.AreEqual(
            10m,
            ProgressionConfiguration.MaximumXpPerResident);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(100)]
    public void VanillaPercentageBoundariesAreAccepted(int percentage)
    {
        Assert.IsTrue(
            ProgressionConfiguration.TryCreateCustom(
                1.5d,
                percentage,
                out _));
    }

    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    public void InvalidVanillaPercentagesAreRejected(int percentage)
    {
        Assert.IsFalse(
            ProgressionConfiguration.TryCreateCustom(
                1.5d,
                percentage,
                out _));
    }
}
