using System;

namespace Kobbyist.ProgressionControls.Core
{
    public sealed class ProgressionSettingsState :
        IEquatable<ProgressionSettingsState>
    {
        public ProgressionSettingsState(
            ProgressionPreset preset,
            double xpPerResident,
            int vanillaXpPercentage)
        {
            Preset = preset;
            XpPerResident = xpPerResident;
            VanillaXpPercentage = vanillaXpPercentage;
        }

        public ProgressionPreset Preset { get; }

        public double XpPerResident { get; }

        public int VanillaXpPercentage { get; }

        public bool Equals(ProgressionSettingsState other)
        {
            return other != null &&
                Preset == other.Preset &&
                XpPerResident.Equals(other.XpPerResident) &&
                VanillaXpPercentage == other.VanillaXpPercentage;
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
                hash = hash * 397 ^ VanillaXpPercentage;
                return hash;
            }
        }
    }

    public static class ProgressionSettingsResolver
    {
        public static bool TryResolveInitial(
            ProgressionSettingsState requested,
            out ProgressionConfiguration configuration,
            out ProgressionSettingsState normalized)
        {
            configuration = null;
            normalized = null;
            if (requested == null)
            {
                return false;
            }

            if (requested.Preset == ProgressionPreset.Custom)
            {
                if (!ProgressionConfiguration.TryCreateCustom(
                    requested.XpPerResident,
                    requested.VanillaXpPercentage,
                    out configuration))
                {
                    return false;
                }
            }
            else if (!ProgressionConfiguration.TryFromPreset(
                requested.Preset,
                out configuration))
            {
                return false;
            }

            normalized = Normalize(configuration);
            return true;
        }

        public static bool TryResolveChange(
            ProgressionSettingsState previous,
            ProgressionSettingsState requested,
            ProgressionConfiguration currentConfiguration,
            out ProgressionConfiguration configuration,
            out ProgressionSettingsState normalized)
        {
            configuration = null;
            normalized = null;
            if (previous == null ||
                requested == null ||
                currentConfiguration == null)
            {
                return false;
            }

            if (requested.Preset != previous.Preset &&
                requested.Preset != ProgressionPreset.Custom)
            {
                if (!ProgressionConfiguration.TryFromPreset(
                    requested.Preset,
                    out configuration))
                {
                    return false;
                }

                normalized = Normalize(configuration);
                return true;
            }

            if (!requested.XpPerResident.Equals(
                    previous.XpPerResident) ||
                requested.VanillaXpPercentage !=
                    previous.VanillaXpPercentage)
            {
                if (!ProgressionConfiguration.TryCreateCustom(
                    requested.XpPerResident,
                    requested.VanillaXpPercentage,
                    out configuration))
                {
                    return false;
                }

                normalized = Normalize(configuration);
                return true;
            }

            configuration = currentConfiguration;
            normalized = Normalize(currentConfiguration);
            return true;
        }

        public static ProgressionSettingsState Normalize(
            ProgressionConfiguration configuration)
        {
            if (configuration == null)
            {
                return null;
            }

            return new ProgressionSettingsState(
                configuration.Preset,
                (double)configuration.XpPerResident,
                configuration.VanillaXpPercentage);
        }
    }
}
