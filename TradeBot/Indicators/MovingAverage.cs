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
        public enum Type
        {
            Simple,
            Exponential,
        }

        private int period;
        private int offset;
        private Type type;

        private LineSeries series;

        private int? boughtCandleIndex;
        private int? whenToSellIndex;
        private double? whenToBuyPrice;
        private int? whenToBuyPriceSetIndex;
        private double? stopLoss;

        public MovingAverage(int period, int offset, Type type)
        {
            if (period < 1 || offset < 0)
                throw new ArgumentOutOfRangeException();

            this.period = period;
            this.offset = offset;
            this.type = type;
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

        private void CalculateSMA()
        {
            series.Points.Clear();

            for (int i = 0; i < candles.Count - period; ++i)
                series.Points.Add(new DataPoint(i, CalculateAverage(i)));
        }

        private void CalculateEMA()
        {
            series.Points.Clear();

            if (candles.Count < period)
                return;

            double multiplier = 2.0 / (period + 1.0);
            var EMA = new List<double>();

            EMA.Add(CalculateAverage(candles.Count - period));

            for (int i = 1; i < candles.Count - period; ++i)
                EMA.Add((candles[candles.Count - period - i - 1].Close * multiplier) + EMA[i - 1] * (1 - multiplier));

            for (int i = EMA.Count - 1; i >= 0; --i)
                series.Points.Add(new DataPoint(series.Points.Count, EMA[i]));
        }

        private double CalculateAverage(int startIndex)
        {
            double sum = 0;
            for (int j = 0; j < period; ++j)
                sum += candles[startIndex + j].Close;
            return sum / period;
        }

        override public void UpdateSeries()
        {
            if (type == Type.Simple)
                CalculateSMA();
            else if (type == Type.Exponential)
                CalculateEMA();
        }

        override public void InitializeSeries(ElementCollection<Series> series)
        {
            if (AreSeriesInitialized)
                return;

            string title = string.Empty;
            if (type == Type.Simple)
                title = string.Format("Simple Moving Average {0}", period);
            else if (type == Type.Exponential)
                title = string.Format("Exponential Moving Average {0}", period);

            this.series = new LineSeries
            {
                Title = title,
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
