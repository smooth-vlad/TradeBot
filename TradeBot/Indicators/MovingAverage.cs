using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    public class MovingAverage : Indicator
    {
        public IMaCalculation movingAverageCalculation { get; }

        public int Period { get; }

        LineSeries series;
        public IReadOnlyList<DataPoint> Values => series.Points;

        ElementCollection<Series> chart;

        public override bool IsOscillator => false;

        public MovingAverage(int period, IMaCalculation calculationMethod, List<HighLowItem> candles)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();

            this.Period = period;
            this.candles = candles;
            movingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();

            series = new LineSeries
            {
                Title = movingAverageCalculation.Title
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
                    ? new Signal(Signal.Type.Buy, 1f)
                    : new Signal(Signal.Type.Sell, 1f);
            }

            return null;
        }

        public override void UpdateSeries()
        {
            if (series.Points.Count > Period * 2)
            {
                series.Points.RemoveRange(series.Points.Count - Period * 2, Period * 2);
            }
            var movingAverage = movingAverageCalculation.Calculate(index => candles[index].Close, series.Points.Count, candles.Count - Period, Period);
            series.Points.Capacity += movingAverage.Count;
            for (int i = 0; i < movingAverage.Count; ++i)
            {
                series.Points.Add(new DataPoint(series.Points.Count, movingAverage[i]));
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
            if (chart == null)
                return;

            chart.Remove(series);
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