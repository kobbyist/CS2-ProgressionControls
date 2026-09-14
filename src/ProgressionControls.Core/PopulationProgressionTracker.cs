using System;

namespace Kobbyist.ProgressionControls.Core
{
    public sealed class PopulationProgressionState
    {
        public PopulationProgressionState(
            int maximumPopulation,
            decimal fractionalXp)
        {
            MaximumPopulation = maximumPopulation;
            FractionalXp = fractionalXp;
        }

        public int MaximumPopulation { get; }

        public decimal FractionalXp { get; }

        public bool IsValid =>
            MaximumPopulation >= 0 &&
            FractionalXp >= 0m &&
            FractionalXp < 1m;
    }

    public sealed class PopulationProgressionTracker
    {
        private bool m_Initialized;
        private int m_MaximumPopulation;
        private decimal m_FractionalXp;
        private decimal m_ConfiguredRate;

        public int MaximumPopulation => m_MaximumPopulation;

        public static bool TryRestore(
            PopulationProgressionState state,
            ProgressionConfiguration configuration,
            out PopulationProgressionTracker tracker)
        {
            tracker = null;
            if (state == null ||
                !state.IsValid ||
                configuration == null)
            {
                return false;
            }

            tracker = new PopulationProgressionTracker
            {
                m_Initialized = true,
                m_MaximumPopulation = state.MaximumPopulation,
                m_FractionalXp = state.FractionalXp,
                m_ConfiguredRate = configuration.XpPerResident,
            };
            return true;
        }

        public PopulationProgressionState CaptureState()
        {
            if (!m_Initialized)
            {
                return null;
            }

            return new PopulationProgressionState(
                m_MaximumPopulation,
                m_FractionalXp);
        }

        public bool TryRebaseline(
            int currentPopulation,
            int knownMaximumPopulation,
            ProgressionConfiguration configuration)
        {
            if (currentPopulation < 0 ||
                knownMaximumPopulation < 0 ||
                configuration == null)
            {
                return false;
            }

            m_Initialized = true;
            m_MaximumPopulation = Math.Max(
                m_MaximumPopulation,
                Math.Max(
                    currentPopulation,
                    knownMaximumPopulation));
            m_FractionalXp = 0m;
            m_ConfiguredRate = configuration.XpPerResident;
            return true;
        }

        public bool TryObserve(
            int currentPopulation,
            ProgressionConfiguration configuration,
            out long awardedXp)
        {
            awardedXp = 0;
            if (currentPopulation < 0 || configuration == null)
            {
                return false;
            }

            if (!m_Initialized)
            {
                EstablishBaseline(
                    currentPopulation,
                    configuration.XpPerResident);
                return true;
            }

            if (m_ConfiguredRate != configuration.XpPerResident)
            {
                m_ConfiguredRate = configuration.XpPerResident;
                m_FractionalXp = 0m;
                m_MaximumPopulation = Math.Max(
                    m_MaximumPopulation,
                    currentPopulation);
                return true;
            }

            if (currentPopulation <= m_MaximumPopulation)
            {
                return true;
            }

            var exactXp =
                (currentPopulation - m_MaximumPopulation) *
                    m_ConfiguredRate +
                m_FractionalXp;
            awardedXp = decimal.ToInt64(decimal.Truncate(exactXp));
            m_FractionalXp = exactXp - awardedXp;
            m_MaximumPopulation = currentPopulation;
            return true;
        }

        private void EstablishBaseline(
            int currentPopulation,
            decimal configuredRate)
        {
            m_Initialized = true;
            m_MaximumPopulation = currentPopulation;
            m_FractionalXp = 0m;
            m_ConfiguredRate = configuredRate;
        }
    }
}
