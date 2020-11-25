using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace TradeBot
{
    internal class Macd : OscillatorIndicator
    {
        public int DifferencePeriod { get; }
        public int LongPeriod { get; }
        public int ShortPeriod { get; }
        public IMaCalculation MovingAverageCalculation { get; }

        public IReadOnlyList<DataPoint> MacdValues => macdSeries.Points;
        public IReadOnlyList<DataPoint> SignalValues => signalSeries.Points;
        public IReadOnlyList<HistogramItem> histogramValues => histogramSeries.Items;

        public override (double min, double max)? YAxisRange => null;

        private MovingAverage longMovingAverage;
        private MovingAverage shortMovingAverage;
        private LineSeries macdSeries;
        private LineSeries signalSeries;
        private HistogramSeries histogramSeries;

        private ElementCollection<Series> chart;

        public Macd(IMaCalculation calculationMethod,
            int shortPeriod, int longPeriod, int differencePeriod,
            List<HighLowItem> candles)
            : base(candles)
        {
            if (shortPeriod < 1 || longPeriod < 1 || differencePeriod < 1 ||
                shortPeriod >= longPeriod)
                throw new ArgumentOutOfRangeException();

            MovingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();
            this.ShortPeriod = shortPeriod;
            this.LongPeriod = longPeriod;
            this.DifferencePeriod = differencePeriod;

            shortMovingAverage = new MovingAverage(shortPeriod, MovingAverageCalculation, candles);
            longMovingAverage = new MovingAverage(longPeriod, MovingAverageCalculation, candles);

            histogramSeries = new HistogramSeries
            {
                Title = "MACD Histogram",
                ColorMapping = (hli) =>
                {
                    if (hli.Value < 0)
                        return OxyColor.FromRgb(214, 107, 107);
                    return OxyColor.FromRgb(121, 229, 112);
                },
            };
            macdSeries = new LineSeries
            {
                Title = "MACD"
            };
            signalSeries = new LineSeries
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
                for (var i = 0; i < signalSeries.Points.Count && i < macdSeries.Points.Count; ++i)
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

            this.chart.Add(histogramSeries);
            this.chart.Add(macdSeries);
            this.chart.Add(signalSeries);

            AreSeriesAttached = true;
        }

        public override void DetachFromChart()
        {
            if (chart == null)
                return;

            chart.Remove(histogramSeries);
            chart.Remove(macdSeries);
            chart.Remove(signalSeries);

            chart = null;
            AreSeriesAttached = false;
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