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
        public IReadOnlyList<DataPoint> SignalValues => signalSeries.Points;
        public IReadOnlyList<HistogramItem> histogramValues => histogramSeries.Items;

        private MovingAverage longMovingAverage;
        private MovingAverage shortMovingAverage;
        private LineSeries macdSeries;
        private LineSeries signalSeries;
        private HistogramSeries histogramSeries;

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
            signalSeries = new LineSeries
            {
                Title = "MACD Signal Line"
            };
            histogramSeries = new HistogramSeries
            {
                Title = "MACD Histogram",
                FillColor = OxyColors.Green,
                NegativeFillColor = OxyColors.Red,
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
                if (signalSeries.Points.Count > DifferencePeriod * 2)
                    signalSeries.Points.RemoveRange(signalSeries.Points.Count - DifferencePeriod * 2, DifferencePeriod * 2);

                var movingAverage = MovingAverageCalculation.Calculate(index => macdSeries.Points[index].Y, signalSeries.Points.Count, macdSeries.Points.Count - DifferencePeriod, DifferencePeriod);
                signalSeries.Points.Capacity += movingAverage.Count;

                foreach (var t in movingAverage)
                {
                    var x = signalSeries.Points.Count;
                    signalSeries.Points.Add(new DataPoint(x, t));
                }
            }

            // macdHistogramSeries
            {
                histogramSeries.Items.Clear();
                for (var i = 0; i < candles.Count - LongPeriod; ++i)
                {
                    var val = macdSeries.Points[i].Y - signalSeries.Points[i].Y;
                    histogramSeries.Items.Add(new HistogramItem(i - 0.2, i + 0.2, val, 1));
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
            this.chart.Add(histogramSeries);

            AreSeriesAttached = true;
        }

        public override void DetachFromChart()
        {
            if (chart == null)
                return;

            chart.Remove(macdSeries);
            chart.Remove(signalSeries);
            chart.Remove(histogramSeries);
        }

        public override void ResetSeries()
        {
            shortMovingAverage.ResetSeries();
            longMovingAverage.ResetSeries();
            macdSeries.Points.Clear();
            signalSeries.Points.Clear();
        }
    }
}