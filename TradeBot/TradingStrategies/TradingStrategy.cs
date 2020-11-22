using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public abstract class TradingStrategy
    {
        protected TradingStrategy(List<HighLowItem> candles)
        {
            this.candles = candles;
        }

        public List<HighLowItem> candles { get; private set; }

        //public double? StopLoss { get; protected set; }
        public abstract Signal? GetSignal(int candleIndex);

        public struct Signal
        {
            public enum Type
            {
                Buy,
                Sell
            }

            public readonly Type type;

            public Signal(Type type)
            {
                this.type = type;
            }
        }

        public abstract void Reset();

        //public void PlaceStopLoss(int openCandleIndex, double openPrice, double percentage)
        //{
        //    int div = 1000;
        //    var resLong = CalculateMaxMinPrice(openCandleIndex, 200);
        //    var resShort = CalculateMaxMinPrice(openCandleIndex, 50);
        //    (double max, double min) res = ((resLong.max + resShort.max) / 2, (resLong.min + resShort.min) / 2);
        //    double step = (res.max - res.min) / div;

        //    StopLoss = openPrice - step * (percentage * div);
        //}

        //public bool HasCrossedStopLoss(double price)
        //{

        //}

        //private (double max, double min) CalculateMaxMinPrice(int startIndex, int period)
        //{
        //    double maxPrice = double.MinValue;
        //    double minPrice = double.MaxValue;
        //    for (int j = startIndex; j < period + startIndex && j < candles.Count; ++j)
        //    {
        //        var h = candles[j].High;
        //        var l = candles[j].Low;
        //        if (h > maxPrice)
        //            maxPrice = h;
        //        if (l < minPrice)
        //            minPrice = l;
        //    }
        //    return (maxPrice, minPrice);
        //}
    }
}
