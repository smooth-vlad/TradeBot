using System;
using OxyPlot;
using OxyPlot.Series;

namespace TradeBot
{
    //--- Покупка
    //Если ордер на покупку есть
    //    Если цена выше ордера на покупку
    //        Покупка
    //        Поставить стоп-лосс на 10 пунктов ниже MA

    //--- Продажа
    //Если инструмент куплен и стоп-лосс сработал
    //    Продажа
    //Если сигнал к продаже на открытии следующей свечи стоит
    //    Продажа

    //--- Обновление
    //Если инструмент куплен
    //    Если цена закрытия ниже MA
    //        Установить сигнал к продаже на открытии следующей свечи
    //Иначе
    //    Если ордер на покупку есть
    //        Если цена ниже ордера и дальше ордера на 10 свечей
    //            Удалить ордер на покупку
    //    Иначе
    //        Если свеча большая и пробила MA вверх
    //            Поставить ордер на 2 пункта выше от максимума
    public class MovingAverage : Indicator
    {
        readonly IMaCalculation movingAverageCalculation;
        readonly int offset;

        readonly int period;

        LineSeries series;

        public MovingAverage(int period, int offset, IMaCalculation calculationMethod)
        {
            if (period < 1 || offset < 0)
                throw new ArgumentOutOfRangeException();

            this.period = period;
            this.offset = offset;
            movingAverageCalculation = calculationMethod ?? throw new ArgumentNullException();
        }

        public override Signal? GetSignal(int currentCandleIndex)
        {
            if (currentCandleIndex > candles.Count - period - offset)
                return null;

            if ((candles[currentCandleIndex + 1].Close - series.Points[currentCandleIndex + 1].Y) *
                (candles[currentCandleIndex].Close - series.Points[currentCandleIndex].Y) < 0)
            {
                return candles[currentCandleIndex].Close > series.Points[currentCandleIndex].Y
                    ? new Signal(Signal.SignalType.Buy, 0.6f)
                    : new Signal(Signal.SignalType.Sell, 0.6f);
            }

            return null;
        }

        public override void UpdateSeries()
        {
            movingAverageCalculation.Calculate(index => candles[index].Close, candles.Count, period, series);
        }

        public override void InitializeSeries(ElementCollection<Series> series)
        {
            if (AreSeriesInitialized)
                return;

            this.series = new LineSeries
            {
                Title = movingAverageCalculation.Title
            };
            series.Add(this.series);
            AreSeriesInitialized = true;
        }

        public override void RemoveSeries(ElementCollection<Series> series)
        {
            series.Remove(this.series);
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