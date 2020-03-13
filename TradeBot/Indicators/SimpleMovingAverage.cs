﻿using LiveCharts;
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

        override public bool IsBuySignal()
        {
            return (Candles[Candles.Count - 2].Close - SMA[SMA.Count - 2]) *
                (Candles[Candles.Count - 1].Close - SMA[SMA.Count - 1]) < 0;
        }

        override public bool IsSellSignal()
        {
            return false;
        }

        override public void UpdateState()
        {
            Calculate();
        }

        private void Calculate()
        {
            var SMA = new ChartValues<decimal>();
            for (int i = Candles.Count - candlesSpan; i < Candles.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += Candles[i - j].Close;
                SMA.Add(sum / period);
            }
            this.SMA = SMA;
        }

        override public void UpdateGraphs()
        {
            bindedGraph.Values = SMA;
        }

        override public void InitializeGraphs(SeriesCollection series)
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