using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using static TradeBot.Instrument;

namespace TradeBot
{
    public class TradingInterface
    {
        public double Balance { get; set; }

        public TradingInterface(double initialBalance)
        {
            Balance = initialBalance;
        }

        public void ClosePosition(Instrument instrument, double price)
        {
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");
            if (instrument.State == States.Empty) throw new InvalidOperationException("can't sell because state is not bought");

            var diff = price * instrument.DealLots;
            if (instrument.State == States.Bought)
                Balance += diff;
            else
                Balance -= diff;

            instrument.DealLots = 0;
            instrument.State = States.Empty;
        }

        public void OpenPosition(Instrument instrument, double price, int lots, bool isShort)
        {
            if (instrument.State != States.Empty) throw new InvalidOperationException("can't buy because state is bought already (sell first)");
            int maxLots = (int)(Balance / price);
            if (lots < 0 || lots > maxLots) throw new ArgumentOutOfRangeException("lots should be > 0 and <= maxLots");
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");

            instrument.DealPrice = price;
            instrument.DealLots = lots;
            instrument.State = isShort ? States.Sold : States.Bought;

            var diff = instrument.DealPrice * instrument.DealLots;
            if (instrument.State == States.Bought)
                Balance -= diff;
            else
                Balance += diff;
        }

        public void OpenPosition(Instrument instrument, double price, bool isShort)
        {            
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");

            int maxLots = (int)(Balance / price);
            OpenPosition(instrument, price, maxLots, isShort);
        }

        public void Reset(double newBalance)
        {
            Balance = newBalance;
        }
    }
}
