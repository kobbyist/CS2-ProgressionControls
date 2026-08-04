using System;

namespace Kobbyist.ProgressionControls.Core
{
    public sealed class ProgressionConfiguration
    {
        public const decimal MinimumXpPerResident = 0m;
        public const decimal MaximumXpPerResident = 10m;
        public const decimal XpPerResidentStep = 0.25m;
        public const decimal DefaultXpPerResident = 1.5m;
        public const int DefaultVanillaXpPercentage = 25;

        private ProgressionConfiguration(
            ProgressionPreset preset,
            decimal xpPerResident,
            int vanillaXpPercentage)
        {
            Preset = preset;
            XpPerResident = xpPerResident;
            VanillaXpPercentage = vanillaXpPercentage;
        }

        public ProgressionPreset Preset { get; }

        public decimal XpPerResident { get; }

        public int VanillaXpPercentage { get; }

        public static bool TryFromPreset(
            ProgressionPreset preset,
            out ProgressionConfiguration configuration)
        {
            configuration = null;
            switch (preset)
            {
                case ProgressionPreset.PopulationBalanced:
                    configuration = new ProgressionConfiguration(
                        preset,
                        DefaultXpPerResident,
                        vanillaXpPercentage: 50);
                    return true;

                case ProgressionPreset.PopulationHeavy:
                    configuration = new ProgressionConfiguration(
                        preset,
                        DefaultXpPerResident,
                        DefaultVanillaXpPercentage);
                    return true;

                case ProgressionPreset.PopulationOnly:
                    configuration = new ProgressionConfiguration(
                        preset,
                        DefaultXpPerResident,
                        vanillaXpPercentage: 0);
                    return true;

                default:
                    return false;
            }
        }

        public static bool TryCreateCustom(
            double xpPerResident,
            int vanillaXpPercentage,
            out ProgressionConfiguration configuration)
        {
            configuration = null;
            if (!TryConvertXpPerResident(
                    xpPerResident,
                    out var rate) ||
                !IsValidVanillaXpPercentage(vanillaXpPercentage))
            {
                return false;
            }

            configuration = new ProgressionConfiguration(
                ProgressionPreset.Custom,
                rate,
                vanillaXpPercentage);
            return true;
        }

        public static bool TryConvertXpPerResident(
            double xpPerResident,
            out decimal validatedRate)
        {
            validatedRate = 0m;
            if (double.IsNaN(xpPerResident) ||
                double.IsInfinity(xpPerResident) ||
                xpPerResident < (double)MinimumXpPerResident ||
                xpPerResident > (double)MaximumXpPerResident)
            {
                return false;
            }

            try
            {
                var rate = (decimal)xpPerResident;
                if (xpPerResident > 0d && rate == 0m)
                {
                    return false;
                }

                if (!IsValidXpPerResident(rate))
                {
                    return false;
                }

                validatedRate = rate;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        public static bool IsValidXpPerResident(decimal xpPerResident)
        {
            return xpPerResident >= MinimumXpPerResident &&
                xpPerResident <= MaximumXpPerResident &&
                xpPerResident % XpPerResidentStep == 0m;
        }

        public static bool IsValidVanillaXpPercentage(int percentage)
        {
            return percentage >= 0 && percentage <= 100;
        }
    }
}
