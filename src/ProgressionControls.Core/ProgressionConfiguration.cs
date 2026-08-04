namespace Kobbyist.ProgressionControls.Core
{
    public sealed class ProgressionConfiguration
    {
        public const int DefaultMegalopolisPopulationTarget = 200000;
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
            int megalopolisXpRequirement,
            out ProgressionConfiguration configuration)
        {
            configuration = null;
            if (preset == ProgressionPreset.Custom ||
                !ProgressionRateConverter.TryRateFromTarget(
                    megalopolisXpRequirement,
                    DefaultMegalopolisPopulationTarget,
                    out var rate))
            {
                return false;
            }

            switch (preset)
            {
                case ProgressionPreset.PopulationBalanced:
                    configuration = new ProgressionConfiguration(
                        preset,
                        rate,
                        vanillaXpPercentage: 50);
                    return true;

                case ProgressionPreset.PopulationHeavy:
                    configuration = new ProgressionConfiguration(
                        preset,
                        rate,
                        DefaultVanillaXpPercentage);
                    return true;

                case ProgressionPreset.PopulationOnly:
                    configuration = new ProgressionConfiguration(
                        preset,
                        rate,
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
            if (!ProgressionRateConverter.TryConvertRate(
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

        public bool TryGetMegalopolisTarget(
            int megalopolisXpRequirement,
            out decimal populationTarget)
        {
            return ProgressionRateConverter.TryTargetFromRate(
                megalopolisXpRequirement,
                XpPerResident,
                out populationTarget);
        }

        public static bool IsValidVanillaXpPercentage(int percentage)
        {
            return percentage >= 0 && percentage <= 100;
        }
    }
}
