using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeBot
{
    public class TradingInterface
    {
        public enum States
        {
            Bought, // long trade
            Sold, // short trade
            Empty,
        }

        public double Balance { get; private set; }
        public States State { get; private set; }
        public double DealPrice { get; private set; }
        public int DealLots { get; private set; }

        public TradingInterface(double initialBalance)
        {
            Balance = initialBalance;
            DealLots = 0;
            DealPrice = 0;
            State = States.Empty;
        }

        public void ClosePosition(double price)
        {
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");
            if (State == States.Empty) throw new InvalidOperationException("can't sell because state is not bought");

            var diff = price * DealLots;
            if (State == States.Bought)
                Balance += diff;
            else
                Balance -= diff;

            DealLots = 0;
            State = States.Empty;
        }

        public void OpenPosition(double price, int lots, bool isShort)
        {
            if (State != States.Empty) throw new InvalidOperationException("can't buy because state is bought already (sell first)");
            int maxLots = (int)(Balance / price);
            if (lots < 0 || lots > maxLots) throw new ArgumentOutOfRangeException("lots should be > 0 and <= maxLots");
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");

            DealPrice = price;
            DealLots = lots;
            State = isShort ? States.Sold : States.Bought;

            var diff = DealPrice * DealLots;
            if (State == States.Bought)
                Balance -= diff;
            else
                Balance += diff;
        }

        public void OpenPosition(double price, bool isShort)
        {            
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");

            int maxLots = (int)(Balance / price);
            OpenPosition(price, maxLots, isShort);
        }

        public void ResetState(double newBalance)
        {
            Balance = newBalance;
            State = States.Empty;
        }
    }
}
