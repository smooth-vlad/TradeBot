using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;

namespace TradeBot
{
    public abstract class Indicator
    {
        public List<HighLowItem> candles;
        public double priceIncrement;
        public bool AreSeriesInitialized { get; protected set; }

        public abstract void ResetState();
        public abstract void UpdateState(int currentCandleIndex);
        public abstract bool IsBuySignal(int currentCandleIndex);
        public abstract bool IsSellSignal(int currentCandleIndex);

        public abstract void UpdateSeries();
        public abstract void ResetSeries();
        public abstract void RemoveSeries(ElementCollection<Series> chart);
        public abstract void InitializeSeries(ElementCollection<Series> chart);

        public abstract void OnNewCandlesAdded(int count);
    }
}
