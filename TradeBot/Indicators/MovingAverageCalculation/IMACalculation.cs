using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public interface IMACalculation
    {
        string Title { get; }

        void Calculate(List<HighLowItem> candles, LineSeries series);
    }
}
