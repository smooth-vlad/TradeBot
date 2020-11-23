using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Windows;

namespace TradeBot
{
    public class RandomTradingStrategy : TradingStrategy
    {
        static Random random = new Random();

        public RandomTradingStrategy(List<HighLowItem> candles)
            : base(candles)
        {
        }

        public override Signal? GetSignal(int candleIndex)
        {
            var n = random.Next(100);

            return n switch
            {
                > 90 => new Signal(Signal.Type.Buy),
                > 80 => new Signal(Signal.Type.Sell),
                _ => null,
            };
        }

        public override void Reset()
        {
            //throw new NotImplementedException();
        }
    }
}