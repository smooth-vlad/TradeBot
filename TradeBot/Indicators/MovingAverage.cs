using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Windows;
using Tinkoff.Trading.OpenApi.Models;

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

        private LineSeries bindedSeries;
        private (int left, int right) valuesBounds;

        private int? boughtCandle;
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
            try
            {
                if (whenToBuyPrice != null)
                {
                    if (Candles[candleIndex].Close > whenToBuyPrice)
                    {
                        boughtCandle = candleIndex;
                        whenToBuyPrice = null;
                        whenToBuyPriceSetIndex = null;
                        stopLoss = bindedSeries.Points[candleIndex].Y - priceIncrement * 10;
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        override public bool IsSellSignal(int candleIndex)
        {
            try
            {
                if (Candles[candleIndex].Close < stopLoss ||
                    whenToSellIndex == candleIndex)
                {
                    boughtCandle = null;
                    stopLoss = null;
                    whenToSellIndex = null;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        override public void UpdateState(int candleIndex)
        {
            try
            {
                if (boughtCandle != null)
                {
                    if (whenToSellIndex == null && Candles[candleIndex].Close < bindedSeries.Points[candleIndex].Y)
                    {
                        whenToSellIndex = candleIndex - 1;
                    }
                }
                else
                {
                    if (whenToBuyPrice != null)
                    {
                        if (Candles[candleIndex].Close < whenToBuyPrice &&
                            candleIndex - whenToBuyPriceSetIndex > 10)
                        {
                            whenToBuyPrice = null;
                            whenToBuyPriceSetIndex = null;
                        }
                    }
                    else
                    {
                        bool isCandleBig = true;
                        double candleSize = Math.Abs(Candles[candleIndex].Close - Candles[candleIndex + 1].Close);
                        for (int i = 1; i < offset + 1; ++i)
                        {
                            double thisCandleSize = Math.Abs(Candles[candleIndex + i].Close - Candles[candleIndex + i + 1].Close);
                            if (thisCandleSize > candleSize)
                                isCandleBig = false;
                        }

                        if (isCandleBig &&
                            ((Candles[candleIndex + 1].Close - bindedSeries.Points[candleIndex + 1].Y) *
                                (Candles[candleIndex].Close - bindedSeries.Points[candleIndex].Y) < 0) &&
                            Candles[candleIndex].Close > bindedSeries.Points[candleIndex].Y)
                        {
                            whenToBuyPrice = Candles[candleIndex].High + priceIncrement * 2;
                            whenToBuyPriceSetIndex = candleIndex;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void CalculateSMA()
        {
            bindedSeries.Points.Clear();

            for (int i = 0; i < Candles.Count - period; ++i)
            {
                double sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += Candles[i + j].Close;
                bindedSeries.Points.Add(new DataPoint(i, sum / period));
            }
        }

        private void CalculateEMA()
        {
            bindedSeries.Points.Clear();

            if (Candles.Count < period)
                return;

            double multiplier = 2.0 / (period + 1.0);
            var EMA = new List<double>();

            {
                double sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += Candles[Candles.Count - period + j].Close;
                EMA.Add(sum / period);
            }

            for (int i = 1; i < Candles.Count - period; ++i)
            {
                EMA.Add((Candles[Candles.Count - period - i - 1].Close * multiplier) + EMA[i - 1] * (1 - multiplier));
            }
            for (int i = EMA.Count - 1; i >= 0; --i)
            {
                bindedSeries.Points.Add(new DataPoint(bindedSeries.Points.Count, EMA[i]));
            }
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
            string title = string.Empty;
            if (type == Type.Simple)
                title = string.Format("Simple Moving Average {0}", period);
            else if (type == Type.Exponential)
                title = string.Format("Exponential Moving Average {0}", period);

            bindedSeries = new LineSeries
            {
                Title = title,
            };
            series.Add(bindedSeries);
            areSeriesInitialized = true;
        }

        public override void ResetState()
        {
            boughtCandle = null;
            whenToSellIndex = null;
            whenToBuyPrice = null;
            whenToBuyPriceSetIndex = null;
            stopLoss = null;
        }

        public override void RemoveSeries(ElementCollection<Series> series)
        {
            series.Remove(bindedSeries);
        }

        public override void ResetSeries()
        {
            bindedSeries.Points.Clear();
        }

        public override void OnNewCandlesAdded(int count)
        {
            UpdateSeries();

            if (boughtCandle.HasValue) boughtCandle += count;
            if (whenToBuyPriceSetIndex.HasValue) whenToBuyPriceSetIndex += count;
            if (whenToSellIndex.HasValue) whenToSellIndex += count;
        }
    }
}
