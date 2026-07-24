namespace Kobbyist.ProgressionControls.Core
{
    public static class EvaluationInterval
    {
        public static bool TryCalculate(
            int ticksPerDay,
            int evaluationsPerDay,
            int minimumInterval,
            out int interval)
        {
            interval = 0;
            if (ticksPerDay <= 0 ||
                evaluationsPerDay <= 0 ||
                minimumInterval <= 0 ||
                ticksPerDay % evaluationsPerDay != 0)
            {
                return false;
            }

            var candidate = ticksPerDay / evaluationsPerDay;
            if (candidate < minimumInterval)
            {
                return false;
            }

            interval = candidate;
            return true;
        }
    }
}
