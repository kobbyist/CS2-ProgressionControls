using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ProgressionSettingsResolverTests
{
    [TestMethod]
    public void BuiltInPresetOverridesPersistedRuleValues()
    {
        var requested = State(
            ProgressionPreset.PopulationHeavy,
            rate: 9.75d,
            vanillaXpPercentage: 80);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveInitial(
                requested,
                out var configuration,
                out var normalized));

        Assert.AreEqual(
            ProgressionPreset.PopulationHeavy,
            configuration.Preset);
        Assert.AreEqual(1.5m, configuration.XpPerResident);
        Assert.AreEqual(1.5d, normalized.XpPerResident);
        Assert.AreEqual(25, normalized.VanillaXpPercentage);
    }

    [TestMethod]
    public void PersistedCustomRulesRestore()
    {
        var requested = State(
            ProgressionPreset.Custom,
            rate: 2.25d,
            vanillaXpPercentage: 10);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveInitial(
                requested,
                out var configuration,
                out var normalized));

        Assert.AreEqual(ProgressionPreset.Custom, configuration.Preset);
        Assert.AreEqual(2.25m, configuration.XpPerResident);
        Assert.AreEqual(2.25d, normalized.XpPerResident);
        Assert.AreEqual(10, normalized.VanillaXpPercentage);
    }

    [TestMethod]
    public void InvalidPersistedCustomRulesAreRejected()
    {
        Assert.IsFalse(
            ProgressionSettingsResolver.TryResolveInitial(
                State(
                    ProgressionPreset.Custom,
                    rate: 2.1d,
                    vanillaXpPercentage: 25),
                out _,
                out _));
    }

    [TestMethod]
    public void RateEditCreatesCustomConfiguration()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var current);
        var requested = State(
            previous.Preset,
            rate: 2d,
            previous.VanillaXpPercentage);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                out var configuration,
                out var normalized));

        Assert.AreEqual(ProgressionPreset.Custom, configuration.Preset);
        Assert.AreEqual(2m, configuration.XpPerResident);
        Assert.AreEqual(2d, normalized.XpPerResident);
    }

    [TestMethod]
    public void MultiplierEditCreatesCustomAndPreservesRate()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var current);
        var requested = State(
            previous.Preset,
            previous.XpPerResident,
            vanillaXpPercentage: 40);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                out var configuration,
                out var normalized));

        Assert.AreEqual(ProgressionPreset.Custom, configuration.Preset);
        Assert.AreEqual(1.5m, configuration.XpPerResident);
        Assert.AreEqual(40, normalized.VanillaXpPercentage);
    }

    [TestMethod]
    public void PresetSelectionRestoresPresetRateAndMultiplier()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var current);
        var requested = State(
            ProgressionPreset.PopulationOnly,
            rate: 8d,
            vanillaXpPercentage: 70);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                out var configuration,
                out var normalized));

        Assert.AreEqual(
            ProgressionPreset.PopulationOnly,
            configuration.Preset);
        Assert.AreEqual(1.5m, configuration.XpPerResident);
        Assert.AreEqual(0, normalized.VanillaXpPercentage);
    }

    [TestMethod]
    public void UnchangedRulesPreserveCurrentConfiguration()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var current);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                previous,
                current,
                out var configuration,
                out var normalized));

        Assert.AreSame(current, configuration);
        Assert.IsTrue(previous.Equals(normalized));
    }

    [TestMethod]
    public void SequentialRuleEditsRemainCustomAndPreserveEarlierEdit()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var current);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                State(
                    previous.Preset,
                    rate: 2d,
                    previous.VanillaXpPercentage),
                current,
                out var firstConfiguration,
                out var firstNormalized));

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                firstNormalized,
                State(
                    firstNormalized.Preset,
                    firstNormalized.XpPerResident,
                    vanillaXpPercentage: 40),
                firstConfiguration,
                out var secondConfiguration,
                out var secondNormalized));

        Assert.AreEqual(
            ProgressionPreset.Custom,
            secondConfiguration.Preset);
        Assert.AreEqual(2m, secondConfiguration.XpPerResident);
        Assert.AreEqual(40, secondConfiguration.VanillaXpPercentage);
        Assert.AreEqual(
            ProgressionPreset.Custom,
            secondNormalized.Preset);
        Assert.AreEqual(2d, secondNormalized.XpPerResident);
        Assert.AreEqual(40, secondNormalized.VanillaXpPercentage);
    }

    [DataTestMethod]
    [DataRow(-0.25d)]
    [DataRow(10.25d)]
    [DataRow(1.1d)]
    public void InvalidRuleChangesAreRejected(double rate)
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var current);

        Assert.IsFalse(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                State(previous.Preset, rate, 25),
                current,
                out var configuration,
                out var normalized));

        Assert.IsNull(configuration);
        Assert.IsNull(normalized);
    }

    private static ProgressionSettingsState DefaultState()
    {
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            out var configuration);
        return ProgressionSettingsResolver.Normalize(configuration);
    }

    private static ProgressionSettingsState State(
        ProgressionPreset preset,
        double rate,
        int vanillaXpPercentage)
    {
        return new ProgressionSettingsState(
            preset,
            rate,
            vanillaXpPercentage);
    }
}
