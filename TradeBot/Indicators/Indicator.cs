﻿using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    public abstract class Indicator
    {
        public List<HighLowItem> candles;
        public double priceIncrement;
        public bool IsOscillator { get; protected set; }
        public bool AreSeriesInitialized { get; protected set; }

        public abstract Signal? GetSignal(int currentCandleIndex);

        public abstract void UpdateSeries();
        public abstract void ResetSeries();
        public abstract void RemoveSeries();
        public abstract void InitializeSeries(ElementCollection<Series> chart);

        public abstract void OnNewCandlesAdded(int count);

        public struct Signal
        {
            public enum Type
            {
                Buy,
                Sell
            }

            public Type type;
            public float weight;

            public Signal(Type type, float weight)
            {
                this.type = type;
                this.weight = weight;
            }
        }
    }
}