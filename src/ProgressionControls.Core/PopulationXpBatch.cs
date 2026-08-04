namespace Kobbyist.ProgressionControls.Core
{
    public sealed class PopulationXpBatch
    {
        private long m_PendingXp;

        public long PendingXp => m_PendingXp;

        public bool TryRestore(long pendingXp)
        {
            if (pendingXp < 0)
            {
                return false;
            }

            m_PendingXp = pendingXp;
            return true;
        }

        public bool TryAdd(long xp)
        {
            if (xp < 0 ||
                m_PendingXp > long.MaxValue - xp)
            {
                return false;
            }

            m_PendingXp += xp;
            return true;
        }

        public long TakeUpTo(long maximumXp)
        {
            if (maximumXp <= 0)
            {
                return 0;
            }

            var takenXp = System.Math.Min(m_PendingXp, maximumXp);
            m_PendingXp -= takenXp;
            return takenXp;
        }

        public void Clear()
        {
            m_PendingXp = 0;
        }
    }
}
