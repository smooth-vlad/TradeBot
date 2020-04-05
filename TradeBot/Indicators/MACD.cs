using System;
using System.Collections.Generic;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    internal class Macd : Indicator
    {
        public int differencePeriod { get; }
        public int longPeriod { get; }
        public int shortPeriod { get; }
        readonly IMaCalculation movingAverageCalculation;

        MovingAverage longMovingAverage;
        MovingAverage shortMovingAverage;
        LineSeries macdSeries;
        HistogramSeries signalSeries;

        ElementCollection<Series> chart;

        public override bool IsOscillator => true;

        public Macd(IMaCalculation calculationMethod, int shortPeriod, int longPeriod, int differencePeriod, List<HighLowItem> candles)
        {
            if (shortPeriod < 1 || longPeriod < 1 || differencePeriod < 1 ||
                shortPeriod >= longPeriod)
                throw new ArgumentOutOfRangeException();

            movingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();
            this.shortPeriod = shortPeriod;
            this.longPeriod = longPeriod;
            this.differencePeriod = differencePeriod;
            this.candles = candles;

            shortMovingAverage = new MovingAverage(shortPeriod, movingAverageCalculation, candles);

            longMovingAverage = new MovingAverage(longPeriod, movingAverageCalculation, candles);
            macdSeries = new LineSeries
            {
                Title = "MACD"
            };
            signalSeries = new HistogramSeries
            {
                Title = "MACD Signal Line"
            };
        }

        public override void UpdateSeries()
        {
            shortMovingAverage.UpdateSeries();

            longMovingAverage.UpdateSeries();

            macdSeries.Points.Clear();
            for (var i = 0; i < candles.Count - longPeriod; ++i)
                macdSeries.Points.Add(new DataPoint(i, shortMovingAverage.Values[i].Y - longMovingAverage.Values[i].Y));

            // signalSeries
            {
                if (signalSeries.Items.Count > differencePeriod * 2)
                    signalSeries.Items.RemoveRange(signalSeries.Items.Count - differencePeriod * 2, differencePeriod * 2);

                var movingAverage = movingAverageCalculation.Calculate(index => macdSeries.Points[index].Y, signalSeries.Items.Count, macdSeries.Points.Count - differencePeriod, differencePeriod);
                signalSeries.Items.Capacity += movingAverage.Count;

                for (int i = 0; i < movingAverage.Count; ++i)
                {
                    var x = signalSeries.Items.Count;
                    signalSeries.Items.Add(new HistogramItem(x - 0.2, x + 0.2, movingAverage[i], 1));
                }
            }
        }

        public override void AttachToChart(ElementCollection<Series> chart)
        {
            if (AreSeriesInitialized || chart == null)
                return;

            this.chart = chart;

            this.chart.Add(macdSeries);
            this.chart.Add(signalSeries);

            AreSeriesInitialized = true;
        }

        public override void DetachFromChart()
        {
            if (chart == null)
                return;

            chart.Remove(macdSeries);
            chart.Remove(signalSeries);
        }

        public override void ResetSeries()
        {
            shortMovingAverage.ResetSeries();
            longMovingAverage.ResetSeries();
            macdSeries.Points.Clear();
            signalSeries.Items.Clear();
        }

        public override void OnNewCandlesAdded(int count)
        {
            ResetSeries();
        }

        public override Signal? GetSignal(int currentCandleIndex)
        {
            if (currentCandleIndex > macdSeries.Points.Count - 2 || currentCandleIndex > signalSeries.Items.Count - 2)
                return null;

            if ((macdSeries.Points[currentCandleIndex + 1].Y - signalSeries.Items[currentCandleIndex + 1].Value) *
                (macdSeries.Points[currentCandleIndex].Y - signalSeries.Items[currentCandleIndex].Value) < 0)
                return macdSeries.Points[currentCandleIndex].Y > signalSeries.Items[currentCandleIndex].Value
                    ? new Signal(Signal.Type.Buy, 1.0f)
                    : new Signal(Signal.Type.Sell, 1.0f);

            return null;
        }
    }
}