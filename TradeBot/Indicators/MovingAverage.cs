using OxyPlot;
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

        private int? boughtCandleIndex;
        private int? whenToSellIndex;
        private double? whenToBuyPrice;
        private int? whenToBuyPriceSetIndex;
        private double? stopLoss;

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

        override public bool IsBuySignal(int candleIndex)
        {
            if (candles.Count <= candleIndex)
                return false;

            if (whenToBuyPrice != null)
            {
                if (candles[candleIndex].Close > whenToBuyPrice)
                {
                    boughtCandleIndex = candleIndex;
                    whenToBuyPrice = null;
                    whenToBuyPriceSetIndex = null;
                    stopLoss = series.Points[candleIndex].Y - priceIncrement * 10;
                    return true;
                }
            }
            return false;
        }

        override public bool IsSellSignal(int candleIndex)
        {
            if (candles.Count <= candleIndex)
                return false;

            if (candles[candleIndex].Close < stopLoss ||
                whenToSellIndex == candleIndex)
            {
                boughtCandleIndex = null;
                stopLoss = null;
                whenToSellIndex = null;
                return true;
            }

            return false;
        }

        override public void UpdateState(int candleIndex)
        {
            if (candles.Count <= candleIndex)
                return;

            if (boughtCandleIndex != null)
            {
                if (whenToSellIndex == null && candles[candleIndex].Close < series.Points[candleIndex].Y)
                {
                    whenToSellIndex = candleIndex - 1;
                }
            }
            else
            {
                if (whenToBuyPrice != null)
                {
                    if (candles[candleIndex].Close < whenToBuyPrice &&
                        candleIndex - whenToBuyPriceSetIndex > 10)
                    {
                        whenToBuyPrice = null;
                        whenToBuyPriceSetIndex = null;
                    }
                }
                else if (candles.Count > candleIndex + offset + 1 && series.Points.Count > candleIndex + 1)
                {
                    bool isCandleBig = true;
                    double candleSize = Math.Abs(candles[candleIndex].Close - candles[candleIndex + 1].Close);
                    for (int i = 1; i < offset + 1; ++i)
                    {
                        double thisCandleSize = Math.Abs(candles[candleIndex + i].Close - candles[candleIndex + i + 1].Close);
                        if (thisCandleSize > candleSize)
                            isCandleBig = false;
                    }

                    if (isCandleBig &&
                        ((candles[candleIndex + 1].Close - series.Points[candleIndex + 1].Y) *
                            (candles[candleIndex].Close - series.Points[candleIndex].Y) < 0) &&
                        candles[candleIndex].Close > series.Points[candleIndex].Y)
                    {
                        whenToBuyPrice = candles[candleIndex].High + priceIncrement * 2;
                        whenToBuyPriceSetIndex = candleIndex;
                    }
                }
            }
        }

        override public void UpdateSeries()
        {
            calculationMethod.Calculate(candles, series);
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

        public override void ResetState()
        {
            boughtCandleIndex = null;
            whenToSellIndex = null;
            whenToBuyPrice = null;
            whenToBuyPriceSetIndex = null;
            stopLoss = null;
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
            if (boughtCandleIndex.HasValue) boughtCandleIndex += count;
            if (whenToBuyPriceSetIndex.HasValue) whenToBuyPriceSetIndex += count;
            if (whenToSellIndex.HasValue) whenToSellIndex += count;
        }
    }
}
