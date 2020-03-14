using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    class SimpleMovingAverage : Indicator
    {
        private int period;

        public ChartValues<decimal> SMA;

        private LineSeries bindedGraph;

        public override int candlesNeeded => candlesSpan + period;

        public SimpleMovingAverage(int period)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();
            this.period = period;
        }

        override public bool IsBuySignal(int candleIndex)
        {
            int candlesStartIndex = Candles.Count - candlesSpan;
            try
            {
                return (Candles[candlesStartIndex + candleIndex - 1].Close - SMA[candleIndex - 1]) *
                    (Candles[candlesStartIndex + candleIndex].Close - SMA[candleIndex]) < 0;
            }
            catch(Exception)
            {
                return false;
            }
        }

        override public bool IsSellSignal(int candleIndex)
        {
            return false;
        }

        override public void UpdateState()
        {
            CalculateSMA();
        }

        private void CalculateSMA()
        {
            var SMA = new List<decimal>(candlesSpan);
            for (int i = Candles.Count - candlesSpan; i < Candles.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += Candles[i - j].Close;
                SMA.Add(sum / period);
            }
            this.SMA = new ChartValues<decimal>(SMA);
        }

        override public void UpdateSeries()
        {
            bindedGraph.Values = SMA;
        }

        override public void InitializeSeries(SeriesCollection series)
        {
            bindedGraph = new LineSeries
            {
                ScalesXAt = 0,
                Values = SMA,
                Title = string.Format("Simple Moving Average {0}", period),
            };
            series.Add(bindedGraph);
            areGraphsInitialized = true;
        }
    }
}
