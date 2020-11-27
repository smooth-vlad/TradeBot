using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Windows;

namespace TradeBot
{
    public class MaTradingStrategy : TradingStrategy
    {
        private MovingAverage ma;

        public MaTradingStrategy(List<HighLowItem> candles, MovingAverage movingAverage)
            : base(candles)
        {
            ma = movingAverage ?? throw new ArgumentNullException("moving average is null");
        }

        public override Signal? GetSignal(int candleIndex)
        {
            if (candleIndex > candles.Count - ma.Period - 2)
                return null;

            if ((candles[candleIndex + 1].Close - ma.Values[candleIndex + 1].Y) *
                (candles[candleIndex].Close - ma.Values[candleIndex].Y) < 0)
            {
                return candles[candleIndex].Close > ma.Values[candleIndex].Y
                    ? Signal.Buy
                    : Signal.Sell;
            }

            return null;
        }

        public override void Reset()
        {
            //throw new NotImplementedException();
        }
    }
}