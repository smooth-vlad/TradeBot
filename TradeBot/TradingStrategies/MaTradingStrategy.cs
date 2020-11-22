using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace TradeBot
{
    public class MaTradingStrategy : TradingStrategy
    {
        private MovingAverage ma;

        public MaTradingStrategy(MovingAverage movingAverage, List<HighLowItem> candles)
            : base(candles)
        {
            if (movingAverage == null)
                throw new ArgumentNullException("moving average is null");
            ma = movingAverage;
        }

        public override Signal? GetSignal(int candleIndex)
        {
            if (candleIndex > candles.Count - ma.Period - 2)
                return null;

            if ((candles[candleIndex + 1].Close - ma.Values[candleIndex + 1].Y) *
                (candles[candleIndex].Close - ma.Values[candleIndex].Y) < 0)
            {
                return candles[candleIndex].Close > ma.Values[candleIndex].Y
                    ? new Signal(Signal.Type.Buy)
                    : new Signal(Signal.Type.Sell);
            }

            return null;
        }

        public override void Reset()
        {
            //throw new NotImplementedException();
        }
    }
}