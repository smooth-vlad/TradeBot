using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public class ExponentialMACalculation : IMACalculation
    {
        public string Title => "Exponential Moving Average";

        public void Calculate(Func<int, double> value, int count, int period, LineSeries series)
        {
            series.Points.Clear();

            if (count < period)
                return;

            double multiplier = 2.0 / (period + 1.0);
            var EMA = new List<double>();

            EMA.Add(SimpleMACalculation.CalculateAverage(value, period, count - period));

            for (int i = 1; i < count - period; ++i)
                EMA.Add((value(count - period - i - 1) * multiplier) + EMA[i - 1] * (1 - multiplier));

            for (int i = EMA.Count - 1; i >= 0; --i)
                series.Points.Add(new DataPoint(series.Points.Count, EMA[i]));
        }

        public void Calculate(Func<int, double> value, int count, int period, HistogramSeries series)
        {
            series.Items.Clear();

            if (count < period)
                return;

            double multiplier = 2.0 / (period + 1.0);
            var EMA = new List<double>();

            EMA.Add(SimpleMACalculation.CalculateAverage(value, period, count - period));

            for (int i = 1; i < count - period; ++i)
                EMA.Add((value(count - period - i - 1) * multiplier) + EMA[i - 1] * (1 - multiplier));

            for (int i = EMA.Count - 1; i >= 0; --i)
                series.Items.Add(new HistogramItem(series.Items.Count + 0.1, series.Items.Count + 0.9, EMA[i], 1));
        }
    }
}
