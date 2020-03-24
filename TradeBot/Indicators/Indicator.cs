using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;

namespace TradeBot
{
    public abstract class Indicator
    {
        public struct Signal
        {
            public enum SignalType
            {
                Buy,
                Sell,
            }

            public SignalType type;
            public float weight;

            public Signal(SignalType type, float weight)
            {
                this.type = type;
                this.weight = weight;
            }
        }

        public List<HighLowItem> candles;
        public double priceIncrement;
        public bool AreSeriesInitialized { get; protected set; }

        public abstract Signal? GetSignal(int currentCandleIndex);

        public abstract void UpdateSeries();
        public abstract void ResetSeries();
        public abstract void RemoveSeries(ElementCollection<Series> chart);
        public abstract void InitializeSeries(ElementCollection<Series> chart);

        public abstract void OnNewCandlesAdded(int count);
    }
}
