using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    class MacdTradingStrategy : TradingStrategy
    {
        Macd macd;

        public MacdTradingStrategy(List<HighLowItem> candles, Macd macd)
            : base(candles)
        {
            this.macd = macd;
        }

        public override Signal? GetSignal(int candleIndex)
        {
            if (candleIndex > macd.MacdValues.Count - 2 || candleIndex > macd.SignalValues.Count - 2)
                return null;

            if ((macd.MacdValues[candleIndex + 1].Y - macd.SignalValues[candleIndex + 1].Value) *
                (macd.MacdValues[candleIndex].Y - macd.SignalValues[candleIndex].Value) < 0)
                return macd.MacdValues[candleIndex].Y > macd.SignalValues[candleIndex].Value
                    ? new Signal(Signal.Type.Buy)
                    : new Signal(Signal.Type.Sell);

            return null;

        }

        public override void Reset()
        {
            //throw new NotImplementedException();
        }
    }
}
