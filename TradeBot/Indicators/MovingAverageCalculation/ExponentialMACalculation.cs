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
        private int period;

        public string Title => string.Format("Exponential Moving Average {0}", period);

        public ExponentialMACalculation(int period)
        {
            this.period = period;
        }

        public void Calculate(List<HighLowItem> candles, LineSeries series)
        {
            series.Points.Clear();

            if (candles.Count < period)
                return;

            double multiplier = 2.0 / (period + 1.0);
            var EMA = new List<double>();

            EMA.Add(SimpleMACalculation.CalculateAverage(candles, period, candles.Count - period));

            for (int i = 1; i < candles.Count - period; ++i)
                EMA.Add((candles[candles.Count - period - i - 1].Close * multiplier) + EMA[i - 1] * (1 - multiplier));

            for (int i = EMA.Count - 1; i >= 0; --i)
                series.Points.Add(new DataPoint(series.Points.Count, EMA[i]));
        }
    }
}
