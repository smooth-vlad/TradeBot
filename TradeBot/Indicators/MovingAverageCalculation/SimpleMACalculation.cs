using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public class SimpleMACalculation : IMACalculation
    {
        private int period;

        public string Title => string.Format("Simple Moving Average {0}", period);

        public SimpleMACalculation(int period)
        {
            this.period = period;
        }

        public void Calculate(List<HighLowItem> candles, LineSeries series)
        {
            series.Points.Clear();

            for (int i = 0; i < candles.Count - period; ++i)
                series.Points.Add(new DataPoint(i, CalculateAverage(candles, period, i)));
        }

        public static double CalculateAverage(List<HighLowItem> candles, int period, int startIndex)
        {
            double sum = 0;
            for (int j = 0; j < period; ++j)
                sum += candles[startIndex + j].Close;
            return sum / period;
        }
    }
}
