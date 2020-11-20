using System;

namespace TradeBot
{
    public class MaTradingStrategy : TradingStrategy
    {
        static Random random = new Random();
        public override Signal? GetSignal()
        {
            var n = random.Next(100);

            return n switch
            {
                < 80 => null,
                < 90 => new Signal(Signal.Type.Buy),
                _ => new Signal(Signal.Type.Sell)
            };
        }
    }
}