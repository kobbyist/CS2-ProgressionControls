namespace Kobbyist.ProgressionControls
{
    internal readonly struct ProgressionWidgetSnapshot
    {
        public static readonly ProgressionWidgetSnapshot Empty =
            new ProgressionWidgetSnapshot(
                ready: false,
                active: false,
                currentPopulation: 0,
                historicalMaximumPopulation: 0,
                populationToResume: 0,
                mostRecentPopulationXpAward: 0);

        public ProgressionWidgetSnapshot(
            bool ready,
            bool active,
            int currentPopulation,
            int historicalMaximumPopulation,
            int populationToResume,
            long mostRecentPopulationXpAward)
        {
            Ready = ready;
            Active = active;
            CurrentPopulation = currentPopulation;
            HistoricalMaximumPopulation =
                historicalMaximumPopulation;
            PopulationToResume = populationToResume;
            MostRecentPopulationXpAward =
                mostRecentPopulationXpAward;
        }

        public bool Ready { get; }

        public bool Active { get; }

        public int CurrentPopulation { get; }

        public int HistoricalMaximumPopulation { get; }

        public int PopulationToResume { get; }

        public long MostRecentPopulationXpAward { get; }
    }
}
