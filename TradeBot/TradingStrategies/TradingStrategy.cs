using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public abstract class TradingStrategy
    {
        public abstract Signal? GetSignal();

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
    }
}
