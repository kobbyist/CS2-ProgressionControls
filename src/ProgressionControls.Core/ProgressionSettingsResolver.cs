using System;
using System.Globalization;

namespace Kobbyist.ProgressionControls.Core
{
    public enum PopulationRateInputMode
    {
        XpPerResident,
        MegalopolisTarget,
    }

    public sealed class ProgressionSettingsState :
        IEquatable<ProgressionSettingsState>
    {
        public ProgressionSettingsState(
            ProgressionPreset preset,
            string xpPerResident,
            string megalopolisPopulationTarget,
            int vanillaXpPercentage,
            PopulationRateInputMode rateInputMode)
        {
            Preset = preset;
            XpPerResident = xpPerResident ?? string.Empty;
            MegalopolisPopulationTarget =
                megalopolisPopulationTarget ?? string.Empty;
            VanillaXpPercentage = vanillaXpPercentage;
            RateInputMode = rateInputMode;
        }

        public ProgressionPreset Preset { get; }

        public string XpPerResident { get; }

        public string MegalopolisPopulationTarget { get; }

        public int VanillaXpPercentage { get; }

        public PopulationRateInputMode RateInputMode { get; }

        public bool Equals(ProgressionSettingsState other)
        {
            return other != null &&
                Preset == other.Preset &&
                string.Equals(
                    XpPerResident,
                    other.XpPerResident,
                    StringComparison.Ordinal) &&
                string.Equals(
                    MegalopolisPopulationTarget,
                    other.MegalopolisPopulationTarget,
                    StringComparison.Ordinal) &&
                VanillaXpPercentage == other.VanillaXpPercentage &&
                RateInputMode == other.RateInputMode;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProgressionSettingsState);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Preset;
                hash = hash * 397 ^ XpPerResident.GetHashCode();
                hash = hash * 397 ^
                    MegalopolisPopulationTarget.GetHashCode();
                hash = hash * 397 ^ VanillaXpPercentage;
                hash = hash * 397 ^ (int)RateInputMode;
                return hash;
            }
        }
    }

    public static class ProgressionSettingsResolver
    {
        private const NumberStyles NumericStyles = NumberStyles.Float;

        public static bool TryResolveInitial(
            ProgressionSettingsState requested,
            int megalopolisXpRequirement,
            out ProgressionConfiguration configuration,
            out ProgressionSettingsState normalized)
        {
            configuration = null;
            normalized = null;
            if (requested == null ||
                megalopolisXpRequirement <= 0)
            {
                return false;
            }

            if (requested.Preset != ProgressionPreset.Custom)
            {
                if (!ProgressionConfiguration.TryFromPreset(
                    requested.Preset,
                    megalopolisXpRequirement,
                    out configuration))
                {
                    return false;
                }

                normalized = Normalize(
                    configuration,
                    megalopolisXpRequirement,
                    PopulationRateInputMode.MegalopolisTarget);
                return true;
            }

            if (!TryCreateCustom(
                requested,
                megalopolisXpRequirement,
                out configuration))
            {
                return false;
            }

            normalized = Normalize(
                configuration,
                megalopolisXpRequirement,
                requested.RateInputMode);
            return true;
        }

        public static bool TryResolveChange(
            ProgressionSettingsState previous,
            ProgressionSettingsState requested,
            ProgressionConfiguration currentConfiguration,
            int megalopolisXpRequirement,
            out ProgressionConfiguration configuration,
            out ProgressionSettingsState normalized)
        {
            configuration = null;
            normalized = null;
            if (previous == null ||
                requested == null ||
                currentConfiguration == null ||
                megalopolisXpRequirement <= 0)
            {
                return false;
            }

            if (requested.Preset != previous.Preset &&
                requested.Preset != ProgressionPreset.Custom)
            {
                if (!ProgressionConfiguration.TryFromPreset(
                    requested.Preset,
                    megalopolisXpRequirement,
                    out configuration))
                {
                    return false;
                }

                normalized = Normalize(
                    configuration,
                    megalopolisXpRequirement,
                    PopulationRateInputMode.MegalopolisTarget);
                return true;
            }

            var rateChanged = !string.Equals(
                requested.XpPerResident,
                previous.XpPerResident,
                StringComparison.Ordinal);
            var targetChanged = !string.Equals(
                requested.MegalopolisPopulationTarget,
                previous.MegalopolisPopulationTarget,
                StringComparison.Ordinal);

            if (rateChanged &&
                (!targetChanged ||
                    requested.RateInputMode ==
                        PopulationRateInputMode.XpPerResident))
            {
                if (!TryParseRate(
                    requested.XpPerResident,
                    out var rate) ||
                    !ProgressionConfiguration.TryCreateCustom(
                        rate,
                        requested.VanillaXpPercentage,
                        out configuration))
                {
                    return false;
                }

                normalized = Normalize(
                    configuration,
                    megalopolisXpRequirement,
                    PopulationRateInputMode.XpPerResident);
                return true;
            }

            if (targetChanged)
            {
                if (!TryParseTarget(
                    requested.MegalopolisPopulationTarget,
                    out var target) ||
                    !ProgressionRateConverter.TryRateFromTarget(
                        megalopolisXpRequirement,
                        target,
                        out var rate) ||
                    !ProgressionConfiguration.TryCreateCustom(
                        (double)rate,
                        requested.VanillaXpPercentage,
                        out configuration))
                {
                    return false;
                }

                normalized = Normalize(
                    configuration,
                    megalopolisXpRequirement,
                    PopulationRateInputMode.MegalopolisTarget);
                return true;
            }

            if (requested.VanillaXpPercentage !=
                previous.VanillaXpPercentage)
            {
                if (!ProgressionConfiguration.TryCreateCustom(
                    (double)currentConfiguration.XpPerResident,
                    requested.VanillaXpPercentage,
                    out configuration))
                {
                    return false;
                }

                normalized = Normalize(
                    configuration,
                    megalopolisXpRequirement,
                    previous.RateInputMode);
                return true;
            }

            configuration = currentConfiguration;
            normalized = Normalize(
                currentConfiguration,
                megalopolisXpRequirement,
                previous.RateInputMode);
            return true;
        }

        public static ProgressionSettingsState Normalize(
            ProgressionConfiguration configuration,
            int megalopolisXpRequirement,
            PopulationRateInputMode rateInputMode)
        {
            if (configuration == null ||
                megalopolisXpRequirement <= 0)
            {
                return null;
            }

            var target = string.Empty;
            if (configuration.TryGetMegalopolisTarget(
                megalopolisXpRequirement,
                out var exactTarget))
            {
                target = decimal.Round(
                    exactTarget,
                    decimals: 0,
                    MidpointRounding.AwayFromZero)
                    .ToString(CultureInfo.InvariantCulture);
            }

            return new ProgressionSettingsState(
                configuration.Preset,
                configuration.XpPerResident.ToString(
                    "G29",
                    CultureInfo.InvariantCulture),
                target,
                configuration.VanillaXpPercentage,
                rateInputMode);
        }

        private static bool TryCreateCustom(
            ProgressionSettingsState requested,
            int megalopolisXpRequirement,
            out ProgressionConfiguration configuration)
        {
            configuration = null;
            if (!ProgressionConfiguration.IsValidVanillaXpPercentage(
                requested.VanillaXpPercentage))
            {
                return false;
            }

            if (requested.RateInputMode ==
                PopulationRateInputMode.XpPerResident)
            {
                return TryParseRate(
                        requested.XpPerResident,
                        out var rate) &&
                    ProgressionConfiguration.TryCreateCustom(
                        rate,
                        requested.VanillaXpPercentage,
                        out configuration);
            }

            if (requested.RateInputMode !=
                PopulationRateInputMode.MegalopolisTarget ||
                !TryParseTarget(
                    requested.MegalopolisPopulationTarget,
                    out var target) ||
                !ProgressionRateConverter.TryRateFromTarget(
                    megalopolisXpRequirement,
                    target,
                    out var targetRate))
            {
                return false;
            }

            return ProgressionConfiguration.TryCreateCustom(
                (double)targetRate,
                requested.VanillaXpPercentage,
                out configuration);
        }

        private static bool TryParseRate(
            string text,
            out double rate)
        {
            return double.TryParse(
                    text,
                    NumericStyles,
                    CultureInfo.InvariantCulture,
                    out rate) &&
                !double.IsNaN(rate) &&
                !double.IsInfinity(rate) &&
                rate >= 0d;
        }

        private static bool TryParseTarget(
            string text,
            out double target)
        {
            return double.TryParse(
                    text,
                    NumericStyles,
                    CultureInfo.InvariantCulture,
                    out target) &&
                !double.IsNaN(target) &&
                !double.IsInfinity(target) &&
                target > 0d;
        }
    }
}
