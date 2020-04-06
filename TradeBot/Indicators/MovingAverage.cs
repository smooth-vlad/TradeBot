using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    public class MovingAverage : Indicator
    {
        public IMaCalculation MovingAverageCalculation { get; }

        public int Period { get; }

        LineSeries series;
        public IReadOnlyList<DataPoint> Values => series.Points;

        ElementCollection<Series> chart;

        public override bool IsOscillator => false;

        public MovingAverage(int period, IMaCalculation calculationMethod,
            List<HighLowItem> candles,
            float weight = 1f)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();

            this.Period = period;
            this.candles = candles;
            this.Weight = weight;
            MovingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();

            series = new LineSeries
            {
                Title = MovingAverageCalculation.Title
            };
        }

        public override Signal? GetSignal(int currentCandleIndex)
        {
            if (currentCandleIndex > candles.Count - Period - 2)
                return null;

            if ((candles[currentCandleIndex + 1].Close - series.Points[currentCandleIndex + 1].Y) *
                (candles[currentCandleIndex].Close - series.Points[currentCandleIndex].Y) < 0)
            {
                return candles[currentCandleIndex].Close > series.Points[currentCandleIndex].Y
                    ? new Signal(Signal.Type.Buy, Weight)
                    : new Signal(Signal.Type.Sell, Weight);
            }

            return null;
        }

        public override void UpdateSeries()
        {
            if (series.Points.Count > Period * 2)
            {
                series.Points.RemoveRange(series.Points.Count - Period * 2, Period * 2);
            }
            var movingAverage = MovingAverageCalculation.Calculate(
                index => candles[index].Close,
                series.Points.Count,
                candles.Count - Period, Period);
            series.Points.Capacity += movingAverage.Count;
            foreach (var t in movingAverage)
            {
                series.Points.Add(new DataPoint(series.Points.Count, t));
            }
        }

        public override void AttachToChart(ElementCollection<Series> chart)
        {
            if (AreSeriesAttached || chart == null)
                return;

            this.chart = chart;
            
            this.chart.Add(series);
            AreSeriesAttached = true;
        }

        public override void DetachFromChart()
        {
            chart?.Remove(series);
        }

        public override void ResetSeries()
        {
            series.Points.Clear();
        }

        public override void OnNewCandlesAdded(int count)
        {
            ResetSeries();
        }
    }
}