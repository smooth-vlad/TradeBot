using System;
using System.Collections.Generic;

namespace TradeBot
{
    class SimpleMovingAverage : IIndicator
    {
        private int period;
        public int Period => period;

        public List<decimal> closures;
        public List<decimal> SMA;

        public int bindedGraph;

        public SimpleMovingAverage(int period, int bindedGraph)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();
            this.period = period;
            this.bindedGraph = bindedGraph;
        }

        public bool IsBuySignal()
        {
            return (closures[closures.Count - 2] - SMA[SMA.Count - 2]) * (closures[closures.Count - 1] - SMA[SMA.Count - 1]) < 0;
        }

        public bool IsSellSignal()
        {
            return false;
        }

        public void UpdateState()
        {
            Calculate();
        }

        private void Calculate()
        {
            var SMA = new List<decimal>(closures.Count - period);
            for (int i = period; i < closures.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += closures[i - j];
                SMA.Add(sum / period);
            }
            this.SMA = SMA;
        }
    }
}
