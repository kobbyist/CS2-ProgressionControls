namespace Kobbyist.ProgressionControls.Core
{
    public sealed class ProgressionConfiguration
    {
        public const int DefaultMegalopolisPopulationTarget = 200000;
        public const int DefaultVanillaXpPercentage = 25;

        private ProgressionConfiguration(
            ProgressionPreset preset,
            bool populationXpEnabled,
            decimal xpPerResident,
            int vanillaXpPercentage)
        {
            Preset = preset;
            PopulationXpEnabled = populationXpEnabled;
            XpPerResident = xpPerResident;
            VanillaXpPercentage = vanillaXpPercentage;
        }

        public ProgressionPreset Preset { get; }

        public bool PopulationXpEnabled { get; }

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
                case ProgressionPreset.PopulationHeavy:
                    configuration = new ProgressionConfiguration(
                        preset,
                        populationXpEnabled: true,
                        rate,
                        DefaultVanillaXpPercentage);
                    return true;

                case ProgressionPreset.PopulationOnly:
                    configuration = new ProgressionConfiguration(
                        preset,
                        populationXpEnabled: true,
                        rate,
                        vanillaXpPercentage: 0);
                    return true;

                default:
                    return false;
            }
        }

        public static bool TryCreateCustom(
            bool populationXpEnabled,
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
                populationXpEnabled,
                rate,
                vanillaXpPercentage);
            return true;
        }

        public bool TryWithXpPerResident(
            double xpPerResident,
            out ProgressionConfiguration configuration)
        {
            return TryCreateCustom(
                PopulationXpEnabled,
                xpPerResident,
                VanillaXpPercentage,
                out configuration);
        }

        public bool TryWithMegalopolisTarget(
            int megalopolisXpRequirement,
            double populationTarget,
            out ProgressionConfiguration configuration)
        {
            configuration = null;
            if (!ProgressionRateConverter.TryRateFromTarget(
                    megalopolisXpRequirement,
                    populationTarget,
                    out var rate))
            {
                return false;
            }

            configuration = new ProgressionConfiguration(
                ProgressionPreset.Custom,
                PopulationXpEnabled,
                rate,
                VanillaXpPercentage);
            return true;
        }

        public bool TryWithVanillaXpPercentage(
            int vanillaXpPercentage,
            out ProgressionConfiguration configuration)
        {
            if (!IsValidVanillaXpPercentage(vanillaXpPercentage))
            {
                configuration = null;
                return false;
            }

            configuration = new ProgressionConfiguration(
                ProgressionPreset.Custom,
                PopulationXpEnabled,
                XpPerResident,
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
