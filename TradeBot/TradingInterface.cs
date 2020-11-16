using System;

namespace TradeBot
{
    public class TradingInterface
    {
        public TradingInterface(double initialBalance)
        {
            Balance = initialBalance;
            DealLots = 0;
            DealPrice = 0;
            State = States.Empty;
        }

        public enum States
        {
            Bought, // long trade
            Sold, // short trade
            Empty,
        }

        public double Balance { get; private set; }
        public States State { get; private set; }
        private double? stopLoss;
        public double? StopLoss
        {
            get => stopLoss;
            set
            {
                if (value != null)
                {
                    if (value <= 0) throw new ArgumentOutOfRangeException("stopLoss should be > 0");
                    if (State == States.Empty) throw new InvalidOperationException("can't set stopLoss if state is 'empty' (buy first)");
                }

                stopLoss = value;
            }
        }
        public double DealPrice { get; private set; }
        public int DealLots { get; private set; }

        public void Sell(double price)
        {
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");
            if (State == States.Empty) throw new InvalidOperationException("can't sell because state is not bought");

            var diff = price * DealLots;
            if (State == States.Bought)
                Balance += diff;
            else
                Balance -= diff;
            DealLots = 0;
            stopLoss = null;
            State = States.Empty;
        }

        public void Buy(double price, int lots, bool isShort)
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

        public void Buy(double price, bool isShort)
        {            
            if (price < 0) throw new ArgumentOutOfRangeException("price should be positive");

            int maxLots = (int)(Balance / price);
            Buy(price, maxLots, isShort);
        }
    }
}
