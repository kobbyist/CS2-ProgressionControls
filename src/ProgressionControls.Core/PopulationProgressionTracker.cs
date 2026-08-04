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

    public sealed class PopulationObservationResult
    {
        internal PopulationObservationResult(
            bool accepted,
            bool establishedBaseline,
            long awardedXp,
            int newRecordDelta,
            int maximumPopulation,
            int populationToResume)
        {
            Accepted = accepted;
            EstablishedBaseline = establishedBaseline;
            AwardedXp = awardedXp;
            NewRecordDelta = newRecordDelta;
            MaximumPopulation = maximumPopulation;
            PopulationToResume = populationToResume;
        }

        public bool Accepted { get; }

        public bool EstablishedBaseline { get; }

        public long AwardedXp { get; }

        public int NewRecordDelta { get; }

        public int MaximumPopulation { get; }

        public int PopulationToResume { get; }
    }

    public sealed class PopulationProgressionTracker
    {
        private bool m_Initialized;
        private bool m_HasConfiguredRate;
        private int m_MaximumPopulation;
        private decimal m_FractionalXp;
        private decimal m_ConfiguredRate;

        public bool IsInitialized => m_Initialized;

        public int MaximumPopulation => m_MaximumPopulation;

        public decimal FractionalXp => m_FractionalXp;

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
                m_HasConfiguredRate = true,
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

        public PopulationObservationResult Rebaseline(
            int currentPopulation,
            int knownMaximumPopulation,
            ProgressionConfiguration configuration)
        {
            if (currentPopulation < 0 ||
                knownMaximumPopulation < 0 ||
                configuration == null)
            {
                return Rejected();
            }

            m_Initialized = true;
            m_MaximumPopulation = Math.Max(
                m_MaximumPopulation,
                Math.Max(
                    currentPopulation,
                    knownMaximumPopulation));
            m_FractionalXp = 0m;
            m_ConfiguredRate = configuration.XpPerResident;
            m_HasConfiguredRate = true;

            return Accepted(
                establishedBaseline: true,
                awardedXp: 0,
                newRecordDelta: 0,
                currentPopulation);
        }

        public PopulationObservationResult Observe(
            int currentPopulation,
            ProgressionConfiguration configuration)
        {
            if (currentPopulation < 0 || configuration == null)
            {
                return Rejected();
            }

            if (!m_Initialized)
            {
                EstablishBaseline(
                    currentPopulation,
                    configuration.XpPerResident);
                return Accepted(
                    establishedBaseline: true,
                    awardedXp: 0,
                    newRecordDelta: 0,
                    currentPopulation);
            }

            var rateChanged =
                !m_HasConfiguredRate ||
                m_ConfiguredRate != configuration.XpPerResident;

            if (rateChanged)
            {
                m_ConfiguredRate = configuration.XpPerResident;
                m_HasConfiguredRate = true;
                m_FractionalXp = 0m;
                m_MaximumPopulation = Math.Max(
                    m_MaximumPopulation,
                    currentPopulation);

                return Accepted(
                    establishedBaseline: true,
                    awardedXp: 0,
                    newRecordDelta: 0,
                    currentPopulation);
            }

            if (currentPopulation <= m_MaximumPopulation)
            {
                return Accepted(
                    establishedBaseline: false,
                    awardedXp: 0,
                    newRecordDelta: 0,
                    currentPopulation);
            }

            var newRecordDelta =
                currentPopulation - m_MaximumPopulation;
            var exactXp =
                newRecordDelta * m_ConfiguredRate + m_FractionalXp;
            var awardedXp = decimal.ToInt64(decimal.Truncate(exactXp));

            m_FractionalXp = exactXp - awardedXp;
            m_MaximumPopulation = currentPopulation;

            return Accepted(
                establishedBaseline: false,
                awardedXp,
                newRecordDelta,
                currentPopulation);
        }

        private void EstablishBaseline(
            int currentPopulation,
            decimal configuredRate)
        {
            m_Initialized = true;
            m_MaximumPopulation = currentPopulation;
            m_FractionalXp = 0m;
            m_ConfiguredRate = configuredRate;
            m_HasConfiguredRate = true;
        }

        private PopulationObservationResult Accepted(
            bool establishedBaseline,
            long awardedXp,
            int newRecordDelta,
            int currentPopulation)
        {
            return new PopulationObservationResult(
                accepted: true,
                establishedBaseline,
                awardedXp,
                newRecordDelta,
                m_MaximumPopulation,
                PopulationToResume(currentPopulation));
        }

        private PopulationObservationResult Rejected()
        {
            return new PopulationObservationResult(
                accepted: false,
                establishedBaseline: false,
                awardedXp: 0,
                newRecordDelta: 0,
                m_MaximumPopulation,
                populationToResume: 0);
        }

        private int PopulationToResume(int currentPopulation)
        {
            return Math.Max(0, m_MaximumPopulation - currentPopulation);
        }
    }
}
