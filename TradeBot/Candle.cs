using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    public class Candle : HighLowItem
    {
        public DateTime DateTime { get; }

        public Candle(int x, CandlePayload candle)
        {
            Close = (double)candle.Close;
            Open = (double)candle.Open;
            High = (double)candle.High;
            Low = (double)candle.Low;
            DateTime = candle.Time;
            X = x;
        }
    }
}
