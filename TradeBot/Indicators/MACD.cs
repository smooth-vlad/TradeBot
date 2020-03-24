using OxyPlot;
using OxyPlot.Series;
using System;

namespace TradeBot
{
    class MACD : Indicator
    {
        private IMACalculation movingAverageCalculation;

        private int shortPeriod;
        private int longPeriod;
        private int differencePeriod;

        private LineSeries shortMASeries;
        private LineSeries longMASeries;
        private LineSeries MACDSeries;
        private HistogramSeries signalSeries;

        public MACD(IMACalculation calculationMethod, int shortPeriod, int longPeriod, int differencePeriod)
        {
            if (shortPeriod < 1 || longPeriod < 1 || differencePeriod < 1 ||
                shortPeriod >= longPeriod)
                throw new ArgumentOutOfRangeException();
            if (calculationMethod == null)
                throw new ArgumentNullException();

            this.movingAverageCalculation = calculationMethod;
            this.shortPeriod = shortPeriod;
            this.longPeriod = longPeriod;
            this.differencePeriod = differencePeriod;
        }

        override public void UpdateSeries()
        {
            movingAverageCalculation.Calculate(delegate (int index) { return candles[index].Close; }, candles.Count, shortPeriod, shortMASeries);
            movingAverageCalculation.Calculate(delegate (int index) { return candles[index].Close; }, candles.Count, longPeriod, longMASeries);

            MACDSeries.Points.Clear();
            for (int i = 0; i < candles.Count - longPeriod; ++i)
                MACDSeries.Points.Add(new DataPoint(i, shortMASeries.Points[i].Y - longMASeries.Points[i].Y));

            movingAverageCalculation.Calculate(delegate(int index) { return MACDSeries.Points[index].Y; }, MACDSeries.Points.Count, differencePeriod, signalSeries);
        }

        override public void InitializeSeries(ElementCollection<Series> series)
        {
            if (AreSeriesInitialized)
                return;

            shortMASeries = new LineSeries();
            longMASeries = new LineSeries();
            MACDSeries = new LineSeries
            {
                Title = "MACD",
            };
            signalSeries = new HistogramSeries
            {
                Title = "MACD Signal Line",
            };

            series.Add(signalSeries);
            series.Add(MACDSeries);
        }

        public override void RemoveSeries(ElementCollection<Series> series)
        {
            series.Remove(signalSeries);
            series.Remove(MACDSeries);
        }

        public override void ResetSeries()
        {
            shortMASeries.Points.Clear();
            longMASeries.Points.Clear();
            signalSeries.Items.Clear();
            MACDSeries.Points.Clear();
        }

        public override void OnNewCandlesAdded(int count)
        {
        }

        public override Signal? GetSignal(int currentCandleIndex)
        {
            if (currentCandleIndex > MACDSeries.Points.Count - 2 || currentCandleIndex > signalSeries.Items.Count - 2)
                return null;

            if ((MACDSeries.Points[currentCandleIndex + 1].Y - signalSeries.Items[currentCandleIndex + 1].Value) *
                    (MACDSeries.Points[currentCandleIndex].Y - signalSeries.Items[currentCandleIndex].Value) < 0)
            {
                if (MACDSeries.Points[currentCandleIndex].Y > signalSeries.Items[currentCandleIndex].Value)
                    return new Signal(Signal.SignalType.Buy, 1.0f);
                else
                    return new Signal(Signal.SignalType.Sell, 1.0f);
            }

            return null;
        }
    }
}
