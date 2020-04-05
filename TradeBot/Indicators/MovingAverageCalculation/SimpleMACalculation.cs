using System;
using System.Collections.Generic;

namespace TradeBot
{
    public class SimpleMaCalculation : IMaCalculation
    {
        public string Title => "Simple Moving Average";

        public List<double> Calculate(Func<int, double> valueByIndex, int fromIndex, int toIndex, int period)
        {
            var result = new List<double>(toIndex - fromIndex);
            for (var i = fromIndex; i < toIndex; ++i)
                result.Add(CalculateAverage(valueByIndex, i, period));
            return result;
        }

        public static double CalculateAverage(Func<int, double> valueByIndex, int fromIndex, int period)
        {
            double sum = 0;
            for (var j = 0; j < period; ++j)
                sum += valueByIndex(fromIndex + j);
            return sum / period;
        }
    }
}