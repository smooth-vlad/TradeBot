using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    public class ExponentialMaCalculation : IMaCalculation
    {
        public string Title => "Exponential Moving Average";

        public void Calculate(Func<int, double> value, int count, int period, LineSeries series)
        {
            series.Points.Clear();

            if (count < period)
                return;

            var multiplier = 2.0 / (period + 1.0);
            var ema = new List<double> {SimpleMaCalculation.CalculateAverage(value, period, count - period)};


            for (var i = 1; i < count - period; ++i)
                ema.Add(value(count - period - i - 1) * multiplier + ema[i - 1] * (1 - multiplier));

            for (var i = ema.Count - 1; i >= 0; --i)
                series.Points.Add(new DataPoint(series.Points.Count, ema[i]));
        }

        public void Calculate(Func<int, double> value, int count, int period, HistogramSeries series)
        {
            series.Items.Clear();

            if (count < period)
                return;

            var multiplier = 2.0 / (period + 1.0);
            var ema = new List<double> {SimpleMaCalculation.CalculateAverage(value, period, count - period)};


            for (var i = 1; i < count - period; ++i)
                ema.Add(value(count - period - i - 1) * multiplier + ema[i - 1] * (1 - multiplier));

            for (var i = ema.Count - 1; i >= 0; --i)
                series.Items.Add(new HistogramItem(series.Items.Count - 0.2, series.Items.Count + 0.2, ema[i], 1));
        }
    }
}