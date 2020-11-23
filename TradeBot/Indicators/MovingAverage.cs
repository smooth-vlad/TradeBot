using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace TradeBot
{
    public class MovingAverage : Indicator
    {
        public IMaCalculation MovingAverageCalculation { get; }

        public int Period { get; }

        private LineSeries series;
        public IReadOnlyList<DataPoint> Values => series.Points;

        private ElementCollection<Series> chart;

        public override bool IsOscillator => false;

        public MovingAverage(int period, IMaCalculation calculationMethod,
            List<HighLowItem> candles)
            : base(candles)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();

            this.Period = period;
            MovingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();

            series = new LineSeries
            {
                Title = MovingAverageCalculation.Title
            };
        }

        public override void UpdateSeries()
        {
            if (candles.Count < Period)
                return;

            if (series.Points.Count > Period * 2)
            {
                series.Points.RemoveRange(series.Points.Count - Period * 2, Period * 2);
            }
            var movingAverage = MovingAverageCalculation.Calculate(
                index => candles[index].Close,
                series.Points.Count,
                candles.Count - Period,
                Period);
            series.Points.Capacity += movingAverage.Count;
            foreach (var t in movingAverage)
            {
                series.Points.Add(new DataPoint(series.Points.Count, t));
            }

            SeriesUpdated?.Invoke();
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

            chart = null;
            AreSeriesAttached = false;
        }

        public override void ResetSeries()
        {
            series.Points.Clear();
        }
    }
}