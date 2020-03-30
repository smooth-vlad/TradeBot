using OxyPlot;
using OxyPlot.Series;
using System;

namespace TradeBot
{
    public class SimpleMaCalculation : IMaCalculation
    {
        public string Title => "Simple Moving Average";

        public void Calculate(Func<int, double> value, int count, int period, LineSeries series)
        {
            series.Points.Clear();

            for (var i = 0; i < count - period; ++i)
                series.Points.Add(new DataPoint(i, CalculateAverage(value, period, i)));
        }

        public void Calculate(Func<int, double> value, int count, int period, HistogramSeries series)
        {
            series.Items.Clear();

            for (var i = 0; i < count - period; ++i)
                series.Items.Add(new HistogramItem(i + 0.1, i + 0.9, CalculateAverage(value, period, i), 1));
        }

        public static double CalculateAverage(Func<int, double> value, int period, int startIndex)
        {
            double sum = 0;
            for (var j = 0; j < period; ++j)
                sum += value(startIndex + j);
            return sum / period;
        }
    }
}
