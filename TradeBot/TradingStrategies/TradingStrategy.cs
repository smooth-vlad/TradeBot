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
    }
}
