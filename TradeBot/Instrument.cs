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

        public Instrument(MarketInstrument instrument)
        {
            ActiveInstrument = instrument;
        }

        public async Task<List<CandlePayload>> GetCandles(DateTime from, DateTime to, CandleInterval interval)
        {
            var candles = await TinkoffInterface.Context.MarketCandlesAsync(ActiveInstrument.Figi, from, to, interval);

            var result = candles.Candles.ToList();

            result.Reverse();
            return result;
        }
    }
}
