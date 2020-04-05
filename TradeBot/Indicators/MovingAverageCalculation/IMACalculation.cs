using System;
using System.Collections.Generic;

namespace TradeBot
{
    public interface IMaCalculation
    {
        string Title { get; }

        List<double> Calculate(Func<int, double> valueByIndex, int fromIndex, int toIndex, int period);
    }
}