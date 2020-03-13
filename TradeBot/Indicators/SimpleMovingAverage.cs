using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    class SimpleMovingAverage : IIndicator
    {
        private int period;
        public int Period => period;

        public List<CandlePayload> candles { get; set; }
        public int candlesSpan { get; set; }
        public ChartValues<decimal> SMA;

        public LineSeries bindedGraph;

        public bool areGraphsInitialized { get; set; } = false;

        public SimpleMovingAverage(int period)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();
            this.period = period;
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
            var SMA = new ChartValues<decimal>();
            for (int i = candles.Count - candlesSpan; i < candles.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += candles[i - j].Close;
                SMA.Add(sum / period);
            }
            this.SMA = SMA;
        }

        public void UpdateGraphs()
        {
            bindedGraph.Values = SMA;
        }

        public void InitializeGraphs(SeriesCollection series)
        {
            bindedGraph = new LineSeries
            {
                ScalesXAt = 0,
                Values = SMA,
            };
            series.Add(bindedGraph);
            areGraphsInitialized = true;
        }
    }
}
