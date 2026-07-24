using System;

namespace Kobbyist.ProgressionControls.Core
{
    public sealed class VanillaXpScaler
    {
        private const int ScaleDivisor = 100;

        private bool m_Enabled;
        private int m_Percentage = 100;
        private int m_RemainderHundredths;

        public bool Enabled => m_Enabled;

        public int Percentage => m_Percentage;

        public int RemainderHundredths => m_RemainderHundredths;

        public bool Configure(bool enabled, int percentage)
        {
            var boundedPercentage = Math.Max(0, Math.Min(100, percentage));
            if (m_Enabled == enabled && m_Percentage == boundedPercentage)
            {
                return false;
            }

            m_Enabled = enabled;
            m_Percentage = boundedPercentage;
            m_RemainderHundredths = 0;
            return true;
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

            return (int)Math.Max(
                int.MinValue,
                Math.Min(int.MaxValue, scaled));
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

        public void Reset()
        {
            m_RemainderHundredths = 0;
        }
    }
}

