using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace TradeBot
{
    public abstract class Indicator
    {
        protected List<HighLowItem> candles;
        public bool AreSeriesAttached { get; protected set; }

        public abstract void UpdateSeries();

        public abstract void ResetSeries();

        public abstract void DetachFromChart();

        public abstract void AttachToChart(ElementCollection<Series> chart);

        public delegate void SeriesUpdatedDelegate();
        public SeriesUpdatedDelegate SeriesUpdated;

        protected Indicator(List<HighLowItem> candles)
        {
            this.candles = candles ?? throw new ArgumentNullException("candles should be set");
        }
    }
}