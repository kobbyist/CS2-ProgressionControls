using Kobbyist.ProgressionControls.Core;

namespace ProgressionControls.Core.Tests;

[TestClass]
public sealed class ProgressionSettingsResolverTests
{
    private const int MegalopolisXp = 100000;

    [TestMethod]
    public void BuiltInPresetOverridesStalePersistedValues()
    {
        var requested = State(
            ProgressionPreset.PopulationHeavy,
            populationXpEnabled: false,
            rate: "999",
            target: "12",
            vanillaXpPercentage: 80,
            PopulationRateInputMode.XpPerResident);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveInitial(
                requested,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(
            ProgressionPreset.PopulationHeavy,
            configuration.Preset);
        Assert.IsTrue(normalized.PopulationXpEnabled);
        Assert.AreEqual("0.5", normalized.XpPerResident);
        Assert.AreEqual(
            "200000",
            normalized.MegalopolisPopulationTarget);
        Assert.AreEqual(25, normalized.VanillaXpPercentage);
        Assert.AreEqual(
            PopulationRateInputMode.MegalopolisTarget,
            normalized.RateInputMode);
    }

    [TestMethod]
    public void PersistedCustomTargetRestoresLinkedRate()
    {
        var requested = State(
            ProgressionPreset.Custom,
            populationXpEnabled: true,
            rate: "999",
            target: "250000",
            vanillaXpPercentage: 10,
            PopulationRateInputMode.MegalopolisTarget);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveInitial(
                requested,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(0.4m, configuration.XpPerResident);
        Assert.AreEqual("0.4", normalized.XpPerResident);
        Assert.AreEqual(
            "250000",
            normalized.MegalopolisPopulationTarget);
        Assert.AreEqual(10, configuration.VanillaXpPercentage);
    }

    [TestMethod]
    public void RateEditCreatesPopulationEnabledCustomConfiguration()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var current);
        var requested = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            rate: "2",
            previous.MegalopolisPopulationTarget,
            previous.VanillaXpPercentage,
            previous.RateInputMode);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(ProgressionPreset.Custom, configuration.Preset);
        Assert.IsTrue(configuration.PopulationXpEnabled);
        Assert.AreEqual(2m, configuration.XpPerResident);
        Assert.AreEqual(
            "50000",
            normalized.MegalopolisPopulationTarget);
        Assert.AreEqual(
            PopulationRateInputMode.XpPerResident,
            normalized.RateInputMode);
    }

    [TestMethod]
    public void TargetEditCreatesLinkedCustomConfiguration()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var current);
        var requested = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            previous.XpPerResident,
            target: "400000",
            previous.VanillaXpPercentage,
            previous.RateInputMode);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(0.25m, configuration.XpPerResident);
        Assert.AreEqual("0.25", normalized.XpPerResident);
        Assert.AreEqual(
            PopulationRateInputMode.MegalopolisTarget,
            normalized.RateInputMode);
    }

    [TestMethod]
    public void LastEditedLinkedFieldWinsWhenBothValuesChanged()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var current);
        var targetLast = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            rate: "2",
            target: "400000",
            previous.VanillaXpPercentage,
            PopulationRateInputMode.MegalopolisTarget);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                targetLast,
                current,
                MegalopolisXp,
                out var targetConfiguration,
                out _));

        var rateLast = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            rate: "2",
            target: "400000",
            previous.VanillaXpPercentage,
            PopulationRateInputMode.XpPerResident);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                rateLast,
                current,
                MegalopolisXp,
                out var rateConfiguration,
                out _));

        Assert.AreEqual(
            0.25m,
            targetConfiguration.XpPerResident);
        Assert.AreEqual(
            2m,
            rateConfiguration.XpPerResident);
    }

    [TestMethod]
    public void MultiplierEditPreservesPopulationRate()
    {
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.Vanilla,
            MegalopolisXp,
            out var current);
        var previous = ProgressionSettingsResolver.Normalize(
            current,
            MegalopolisXp,
            PopulationRateInputMode.MegalopolisTarget);
        var requested = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            previous.XpPerResident,
            previous.MegalopolisPopulationTarget,
            vanillaXpPercentage: 40,
            previous.RateInputMode);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(ProgressionPreset.Custom, configuration.Preset);
        Assert.AreEqual(0.5m, configuration.XpPerResident);
        Assert.AreEqual(40, configuration.VanillaXpPercentage);
        Assert.IsTrue(configuration.PopulationXpEnabled);
        Assert.AreEqual(40, normalized.VanillaXpPercentage);
    }

    [TestMethod]
    public void PresetSelectionAppliesImmutablePresetValues()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var current);
        var requested = State(
            ProgressionPreset.PopulationOnly,
            previous.PopulationXpEnabled,
            previous.XpPerResident,
            previous.MegalopolisPopulationTarget,
            previous.VanillaXpPercentage,
            previous.RateInputMode);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(
            ProgressionPreset.PopulationOnly,
            configuration.Preset);
        Assert.AreEqual(0, configuration.VanillaXpPercentage);
        Assert.AreEqual(0, normalized.VanillaXpPercentage);
    }

    [TestMethod]
    public void InvalidManualRateIsRejected()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var current);
        var requested = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            rate: "not-a-number",
            previous.MegalopolisPopulationTarget,
            previous.VanillaXpPercentage,
            previous.RateInputMode);

        Assert.IsFalse(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                MegalopolisXp,
                out _,
                out _));
    }

    [TestMethod]
    public void InvalidPersistedCustomValuesAreRejected()
    {
        var requested = State(
            ProgressionPreset.Custom,
            populationXpEnabled: true,
            rate: "2",
            target: "0",
            vanillaXpPercentage: 25,
            PopulationRateInputMode.MegalopolisTarget);

        Assert.IsFalse(
            ProgressionSettingsResolver.TryResolveInitial(
                requested,
                MegalopolisXp,
                out _,
                out _));
    }

    [TestMethod]
    public void ZeroRateHasNoProjectedTarget()
    {
        var previous = DefaultState();
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var current);
        var requested = State(
            previous.Preset,
            previous.PopulationXpEnabled,
            rate: "0",
            previous.MegalopolisPopulationTarget,
            previous.VanillaXpPercentage,
            previous.RateInputMode);

        Assert.IsTrue(
            ProgressionSettingsResolver.TryResolveChange(
                previous,
                requested,
                current,
                MegalopolisXp,
                out var configuration,
                out var normalized));

        Assert.AreEqual(0m, configuration.XpPerResident);
        Assert.AreEqual(
            string.Empty,
            normalized.MegalopolisPopulationTarget);
        Assert.AreEqual(
            PopulationRateInputMode.XpPerResident,
            normalized.RateInputMode);
    }

    private static ProgressionSettingsState DefaultState()
    {
        ProgressionConfiguration.TryFromPreset(
            ProgressionPreset.PopulationHeavy,
            MegalopolisXp,
            out var configuration);
        return ProgressionSettingsResolver.Normalize(
            configuration,
            MegalopolisXp,
            PopulationRateInputMode.MegalopolisTarget);
    }

    private static ProgressionSettingsState State(
        ProgressionPreset preset,
        bool populationXpEnabled,
        string rate,
        string target,
        int vanillaXpPercentage,
        PopulationRateInputMode rateInputMode)
    {
        return new ProgressionSettingsState(
            preset,
            populationXpEnabled,
            rate,
            target,
            vanillaXpPercentage,
            rateInputMode);
    }
}
