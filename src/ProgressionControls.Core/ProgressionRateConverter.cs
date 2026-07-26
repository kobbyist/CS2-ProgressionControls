using System;

namespace Kobbyist.ProgressionControls.Core
{
    public static class ProgressionRateConverter
    {
        public const decimal MaximumXpPerResident = int.MaxValue;

        public static bool TryRateFromTarget(
            int megalopolisXpRequirement,
            double populationTarget,
            out decimal xpPerResident)
        {
            xpPerResident = 0m;
            if (megalopolisXpRequirement <= 0 ||
                double.IsNaN(populationTarget) ||
                double.IsInfinity(populationTarget) ||
                populationTarget <= 0d)
            {
                return false;
            }

            try
            {
                var target = (decimal)populationTarget;
                if (target <= 0m)
                {
                    return false;
                }

                var rate = megalopolisXpRequirement / target;
                if (!IsValidRate(rate))
                {
                    return false;
                }

                xpPerResident = rate;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
            catch (DivideByZeroException)
            {
                return false;
            }
        }

        public static bool TryTargetFromRate(
            int megalopolisXpRequirement,
            decimal xpPerResident,
            out decimal populationTarget)
        {
            populationTarget = 0m;
            if (megalopolisXpRequirement <= 0 ||
                !IsValidRate(xpPerResident) ||
                xpPerResident == 0m)
            {
                return false;
            }

            try
            {
                populationTarget =
                    megalopolisXpRequirement / xpPerResident;
                return populationTarget > 0m;
            }
            catch (OverflowException)
            {
                populationTarget = 0m;
                return false;
            }
            catch (DivideByZeroException)
            {
                populationTarget = 0m;
                return false;
            }
        }

        public static bool TryConvertRate(
            double xpPerResident,
            out decimal validatedRate)
        {
            validatedRate = 0m;
            if (double.IsNaN(xpPerResident) ||
                double.IsInfinity(xpPerResident) ||
                xpPerResident < 0d)
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

                if (!IsValidRate(rate))
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

        public static bool IsValidRate(decimal xpPerResident)
        {
            return xpPerResident >= 0m &&
                xpPerResident <= MaximumXpPerResident;
        }
    }
}
