using System;
using System.Collections.Generic;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    class SimpleMovingAverage : IIndicator
    {
        private int period;
        public int Period => period;

        public List<CandlePayload> candles;
        public List<decimal> SMA;

        public int bindedGraph;

        public SimpleMovingAverage(int period, int bindedGraph)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();
            if (bindedGraph < 0)
                throw new ArgumentOutOfRangeException();
            this.period = period;
            this.bindedGraph = bindedGraph;
        }

        public bool IsBuySignal()
        {
            return (candles[candles.Count - 2].Close - SMA[SMA.Count - 2]) *
                (candles[candles.Count - 1].Close - SMA[SMA.Count - 1]) < 0;
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
            var SMA = new List<decimal>(candles.Count - period);
            for (int i = period; i < candles.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += candles[i - j].Close;
                SMA.Add(sum / period);
            }
            this.SMA = SMA;
        }
    }
}
