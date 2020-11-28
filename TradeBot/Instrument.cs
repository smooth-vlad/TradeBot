using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    public class Instrument
    {
        public MarketInstrument ActiveInstrument { get; private set; }
        public enum States
        {
            Bought, // long trade
            Sold, // short trade
            Empty,
        }

        public States State { get; set; }
        public double DealPrice { get; set; }
        public int DealLots { get; set; }
        public double TotalPrice
        {
            get
            {
                double dealTotalPrice = DealPrice * DealLots;
                return State switch
                {
                    States.Bought => dealTotalPrice,
                    States.Sold => -dealTotalPrice,
                    _ => 0,
                };
            }
        }

        public Instrument(MarketInstrument instrument)
        {
            ActiveInstrument = instrument;
        }

        public void ResetState()
        {
            State = States.Empty;
            DealPrice = 0;
            DealLots = 0;
        }

        public async Task<List<CandlePayload>> GetCandles(DateTime from, DateTime to, CandleInterval interval)
        {
            var candles = await TinkoffInterface.Context.MarketCandlesAsync(ActiveInstrument.Figi, from, to, interval);
            if (candles == null)
                return null;
            var result = candles.Candles.ToList();

            result.Reverse();
            return result;
        }
    }
}
