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

        public static readonly Dictionary<CandleInterval, TimeSpan> intervalToMaxPeriod
            = new Dictionary<CandleInterval, TimeSpan>
            {
                {CandleInterval.Minute, TimeSpan.FromDays(1)},
                {CandleInterval.FiveMinutes, TimeSpan.FromDays(1)},
                {CandleInterval.QuarterHour, TimeSpan.FromDays(1)},
                {CandleInterval.HalfHour, TimeSpan.FromDays(1)},
                {CandleInterval.Hour, TimeSpan.FromDays(7).Add(TimeSpan.FromHours(-1))},
                {CandleInterval.Day, TimeSpan.FromDays(364)},
                {CandleInterval.Week, TimeSpan.FromDays(364 * 2)},
                {CandleInterval.Month, TimeSpan.FromDays(364 * 10)}
            };

        public static TimeSpan GetPeriod(CandleInterval interval)
        {
            if (!intervalToMaxPeriod.TryGetValue(interval, out var result))
                throw new KeyNotFoundException();
            return result;
        }
    }
}
