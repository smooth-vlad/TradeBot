using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    public abstract class Indicator
    {
        public List<CandlePayload> Candles { get; set; }
        public int candlesSpan { get; set; }
        public decimal priceIncrement;
        protected bool areGraphsInitialized;
        public bool AreGraphsInitialized { get => areGraphsInitialized; }
        abstract public int CandlesNeeded { get; }

        public abstract void ResetState();
        public abstract void UpdateState(int rawCandleIndex);
        public abstract bool IsBuySignal(int rawCandleIndex);
        public abstract bool IsSellSignal(int rawCandleIndex);

        public abstract void UpdateSeries();
        public abstract void RemoveSeries(ElementCollection<Series> series);
        public abstract void InitializeSeries(ElementCollection<Series> series);
    }
}
