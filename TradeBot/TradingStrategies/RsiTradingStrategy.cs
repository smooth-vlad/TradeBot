using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TradeBot
{
    class RsiTradingStrategy : TradingStrategy
    {
        Rsi rsi;

        public RsiTradingStrategy(List<HighLowItem> candles, Rsi rsi)
            : base(candles)
        {
            this.rsi = rsi;
        }

        public override Signal? GetSignal(int candleIndex)
        {
            if (candleIndex > rsi.Values.Count - 2)
                return null;

            if (rsi.Values[candleIndex].Y > rsi.OverboughtLine)
            {
                return Signal.Sell;
            }
            if (rsi.Values[candleIndex].Y < rsi.OversoldLine)
            {
                return Signal.Buy;
            }
            return null;
        }

        public override void Reset()
        {
        }
    }
}
