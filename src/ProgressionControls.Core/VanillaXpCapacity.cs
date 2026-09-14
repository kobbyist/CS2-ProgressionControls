using System;

namespace Kobbyist.ProgressionControls.Core
{
    internal static class VanillaXpCapacity
    {
        public static bool TryAdd(
            int currentXp,
            decimal externalXp,
            out int updatedXp)
        {
            updatedXp = currentXp;
            if (externalXp < 0m ||
                decimal.Truncate(externalXp) != externalXp ||
                externalXp > (decimal)int.MaxValue - currentXp)
            {
                return false;
            }

            updatedXp = decimal.ToInt32(currentXp + externalXp);
            return true;
        }
    }
}
