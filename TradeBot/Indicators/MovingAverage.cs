using System;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    public class MovingAverage : Indicator
    {
        readonly IMaCalculation movingAverageCalculation;

        readonly int period;

        LineSeries series;

        ElementCollection<Series> chart;

        public MovingAverage(int period, IMaCalculation calculationMethod)
        {
            if (period < 1)
                throw new ArgumentOutOfRangeException();

            this.period = period;
            movingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();
            
            IsOscillator = false;
        }

        public override Signal? GetSignal(int currentCandleIndex)
        {
            
            if (currentCandleIndex > candles.Count - period - 2)
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
            movingAverageCalculation.Calculate(index => candles[index].Close, candles.Count, period, series);
        }

        public override void InitializeSeries(ElementCollection<Series> chart)
        {
            if (AreSeriesInitialized)
                return;

            series = new LineSeries
            {
                Title = movingAverageCalculation.Title
            };

            this.chart = chart;
            
            this.chart.Add(series);
            AreSeriesInitialized = true;
        }

        public override void RemoveSeries()
        {
            chart.Remove(series);
        }

        public override void ResetSeries()
        {
            series.Points.Clear();
        }

        public override void OnNewCandlesAdded(int count)
        {
        }
    }
}