using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;

namespace TradeBot
{
    public abstract class Indicator
    {
        protected List<HighLowItem> candles;
        public float Weight { get; protected set; } = 1.0f;
        public abstract bool IsOscillator { get; }
        public bool AreSeriesAttached { get; protected set; }

        public abstract Signal? GetSignal(int currentCandleIndex);

        public abstract void UpdateSeries();

        public abstract void ResetSeries();

        public abstract void DetachFromChart();

        public abstract void AttachToChart(ElementCollection<Series> chart);

        public abstract void OnNewCandlesAdded(int count);

        public struct Signal
        {
            public enum Type
            {
                Buy,
                Sell
            }

            public readonly Type type;

            public Signal(Type type)
            {
                this.type = type;
            }
        }
    }
}