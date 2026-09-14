namespace Kobbyist.ProgressionControls.Core
{
    public sealed class VanillaXpScaler
    {
        private const int ScaleDivisor = 100;

        private bool m_Enabled;
        private int m_Percentage = 100;
        private int m_RemainderHundredths;

        public int RemainderHundredths => m_RemainderHundredths;

        public bool TransformsPositiveXp =>
            m_Enabled && m_Percentage != 100;

        public void Configure(bool enabled, int percentage)
        {
            if (m_Enabled == enabled && m_Percentage == percentage)
            {
                return;
            }

            m_Enabled = enabled;
            m_Percentage = percentage;
            m_RemainderHundredths = 0;
        }

        public int Scale(int amount)
        {
            if (!m_Enabled)
            {
                return amount;
            }

            var numerator =
                (long)amount * m_Percentage + m_RemainderHundredths;
            var scaled = numerator / ScaleDivisor;
            m_RemainderHundredths = (int)(numerator % ScaleDivisor);

            return (int)scaled;
        }

        public bool TryRestoreRemainder(int remainderHundredths)
        {
            if (remainderHundredths < 0 ||
                remainderHundredths >= ScaleDivisor)
            {
                return false;
            }

            m_RemainderHundredths = remainderHundredths;
            return true;
        }

        public void ClearRemainder()
        {
            m_RemainderHundredths = 0;
        }
    }
}

