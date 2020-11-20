using System;
using System.Collections.Generic;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    public static class IntervalToMaxPeriodConverter
    {
        public static TimeSpan GetMaxPeriod(CandleInterval interval)
        {
            return interval switch
            {
                CandleInterval.Minute => TimeSpan.FromDays(1),
                CandleInterval.FiveMinutes => TimeSpan.FromDays(1),
                CandleInterval.QuarterHour => TimeSpan.FromDays(1),
                CandleInterval.HalfHour => TimeSpan.FromDays(1),
                CandleInterval.Hour => TimeSpan.FromDays(7).Add(TimeSpan.FromHours(-1)),
                CandleInterval.Day => TimeSpan.FromDays(364),
                CandleInterval.Week => TimeSpan.FromDays(364 * 2),
                CandleInterval.Month => TimeSpan.FromDays(364 * 10),
                _ => throw new KeyNotFoundException(),
            };
        }
    }
}
