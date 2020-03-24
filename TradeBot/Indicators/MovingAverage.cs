﻿using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

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
        private IMACalculation calculationMethod;

        private int period;
        private int offset;

        private LineSeries series;

        public MovingAverage(int period, int offset, IMACalculation calculationMethod)
        {
            if (period < 1 || offset < 0)
                throw new ArgumentOutOfRangeException();
            if (calculationMethod == null)
                throw new ArgumentNullException();

            this.period = period;
            this.offset = offset;
            this.calculationMethod = calculationMethod;
        }

        override public Signal? GetSignal(int currentCandleIndex)
        {
            if (currentCandleIndex > candles.Count - period - offset)
                return null;

            bool isCandleBigEnough = true;
            //double candleSize = Math.Abs(candles[currentCandleIndex].Close - candles[currentCandleIndex + 1].Close);
            //for (int i = 1; i < offset + 1; ++i)
            //{
            //    double thisCandleSize = Math.Abs(candles[currentCandleIndex + i].Close - candles[currentCandleIndex + i + 1].Close);
            //    if (thisCandleSize > candleSize)
            //        isCandleBigEnough = false;
            //}

            if (isCandleBigEnough &&
                ((candles[currentCandleIndex + 1].Close - series.Points[currentCandleIndex + 1].Y) *
                    (candles[currentCandleIndex].Close - series.Points[currentCandleIndex].Y) < 0))
            {
                if (candles[currentCandleIndex].Close > series.Points[currentCandleIndex].Y)
                    return Signal.Buy;
                else
                    return Signal.Sell;
            }
            return null;
        }

        override public void UpdateSeries()
        {
            calculationMethod.Calculate(delegate (int index) { return candles[index].Close; }, candles.Count, period, series);
        }

        override public void InitializeSeries(ElementCollection<Series> series)
        {
            if (AreSeriesInitialized)
                return;

            this.series = new LineSeries
            {
                Title = calculationMethod.Title,
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
