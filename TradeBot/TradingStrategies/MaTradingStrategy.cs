using System;

namespace TradeBot
{
    public class MaTradingStrategy : ITradingStrategy
    {
        static Random random = new Random();
        public ITradingStrategy.Signal? GetSignal()
        {
            var n = random.Next(100);

            return n switch
            {
                < 80 => null,
                < 90 => new ITradingStrategy.Signal(ITradingStrategy.Signal.Type.Buy),
                _ => new ITradingStrategy.Signal(ITradingStrategy.Signal.Type.Sell)
            };
        }
    }
}