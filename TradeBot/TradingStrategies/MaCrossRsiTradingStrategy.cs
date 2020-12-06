using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TradeBot
{
    class MaCrossRsiTradingStrategy : TradingStrategy
    {
        Rsi rsi;
        MovingAverage shortMa;
        MovingAverage longMa;

        Queue<Signal?> lastRsiSignals = new Queue<Signal?>();

        public MaCrossRsiTradingStrategy(List<HighLowItem> candles, Rsi rsi, MovingAverage shortMa, MovingAverage longMa)
            : base(candles)
        {
            this.rsi = rsi;
            this.shortMa = shortMa;
            this.longMa = longMa;
        }

        public override Signal? GetSignal(int candleIndex)
        {
            if (candleIndex > rsi.Values.Count - 2
                || candleIndex > candles.Count - shortMa.Period - 2
                || candleIndex > candles.Count - longMa.Period - 2)
                return null;

            if (rsi.Values[candleIndex].Y > rsi.OverboughtLine)
            {
                lastRsiSignals.Enqueue(Signal.Sell);
            }
            else if (rsi.Values[candleIndex].Y < rsi.OversoldLine)
            {
                lastRsiSignals.Enqueue(Signal.Buy);
            }
            else
                lastRsiSignals.Enqueue(null);
            if (lastRsiSignals.Count > 10)
                lastRsiSignals.Dequeue();

            if ((shortMa.Values[candleIndex + 1].Y - longMa.Values[candleIndex + 1].Y) *
                (shortMa.Values[candleIndex].Y - longMa.Values[candleIndex].Y) < 0)
            {
                if (shortMa.Values[candleIndex].Y > longMa.Values[candleIndex].Y)
                {
                    if (lastRsiSignals.Contains(Signal.Buy))
                        return Signal.Buy;
                    else
                        return Signal.Close;
                }
                else
                {
                    if (lastRsiSignals.Contains(Signal.Sell))
                        return Signal.Sell;
                    else
                        return Signal.Close;
                }
            }

            return null;
        }

        public override void Reset()
        {
        }
    }
}
