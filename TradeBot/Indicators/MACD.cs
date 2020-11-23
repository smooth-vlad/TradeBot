using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace TradeBot
{
    internal class Macd : Indicator
    {
        public int DifferencePeriod { get; }
        public int LongPeriod { get; }
        public int ShortPeriod { get; }
        public IMaCalculation MovingAverageCalculation { get; }

        public IReadOnlyList<DataPoint> MacdValues => macdSeries.Points;
        public IReadOnlyList<HistogramItem> SignalValues => signalSeries.Items;

        private MovingAverage longMovingAverage;
        private MovingAverage shortMovingAverage;
        private LineSeries macdSeries;
        private HistogramSeries signalSeries;

        private ElementCollection<Series> chart;

        public override bool IsOscillator => true;

        public Macd(IMaCalculation calculationMethod,
            int shortPeriod, int longPeriod, int differencePeriod,
            List<HighLowItem> candles)
        {
            if (shortPeriod < 1 || longPeriod < 1 || differencePeriod < 1 ||
                shortPeriod >= longPeriod)
                throw new ArgumentOutOfRangeException();

            MovingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();
            this.ShortPeriod = shortPeriod;
            this.LongPeriod = longPeriod;
            this.DifferencePeriod = differencePeriod;
            this.candles = candles;

            shortMovingAverage = new MovingAverage(shortPeriod, MovingAverageCalculation, candles);

            longMovingAverage = new MovingAverage(longPeriod, MovingAverageCalculation, candles);
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
            if (candles.Count < Math.Max(LongPeriod, DifferencePeriod))
                return;

            shortMovingAverage.UpdateSeries();

            longMovingAverage.UpdateSeries();

            macdSeries.Points.Clear();
            for (var i = 0; i < candles.Count - LongPeriod; ++i)
                macdSeries.Points.Add(new DataPoint(i, shortMovingAverage.Values[i].Y - longMovingAverage.Values[i].Y));

            // signalSeries
            {
                if (signalSeries.Items.Count > DifferencePeriod * 2)
                    signalSeries.Items.RemoveRange(signalSeries.Items.Count - DifferencePeriod * 2, DifferencePeriod * 2);

                var movingAverage = MovingAverageCalculation.Calculate(index => macdSeries.Points[index].Y, signalSeries.Items.Count, macdSeries.Points.Count - DifferencePeriod, DifferencePeriod);
                signalSeries.Items.Capacity += movingAverage.Count;

                foreach (var t in movingAverage)
                {
                    var x = signalSeries.Items.Count;
                    signalSeries.Items.Add(new HistogramItem(x - 0.2, x + 0.2, t, 1));
                }
            }

            SeriesUpdated?.Invoke();
        }

        public override void AttachToChart(ElementCollection<Series> chart)
        {
            if (AreSeriesAttached || chart == null)
                return;

            this.chart = chart;

            this.chart.Add(macdSeries);
            this.chart.Add(signalSeries);

            AreSeriesAttached = true;
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
    }
}