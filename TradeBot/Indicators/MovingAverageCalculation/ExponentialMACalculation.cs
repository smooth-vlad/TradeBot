using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    public class ExponentialMaCalculation : IMaCalculation
    {
        public string Title => "Exponential Moving Average";

        public List<double> Calculate(Func<int, double> valueByIndex, int fromIndex, int toIndex, int period)
        {
            var multiplier = 2.0 / (period + 1.0);
            var ema = new List<double> { SimpleMaCalculation.CalculateAverage(valueByIndex, toIndex, period) };

            for (var i = 1; i < toIndex - fromIndex; ++i)
                ema.Add(valueByIndex(toIndex - i) * multiplier + ema[i - 1] * (1 - multiplier));

            ema.Reverse();
            return ema;
        }
    }
}