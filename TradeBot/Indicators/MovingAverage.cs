﻿using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
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
    class MovingAverage : Indicator
    {
        private int period;
        private int offset;
        private decimal priceIncrement;

        public ChartValues<decimal> MA;
        private LineSeries bindedGraph;

        private int boughtCandle = -1;
        private int whenToSellIndex = -1;
        private decimal whenToBuyPrice = -1;
        private int whenToBuyPriceSetIndex = -1;
        private decimal stopLoss = -1;

        public override int candlesNeeded => candlesSpan + period;

        public MovingAverage(int period, int offset, decimal priceIncrement)
        {
            if (period < 1 || offset < 1 || priceIncrement < 0)
                throw new ArgumentOutOfRangeException();

            this.period = period;
            this.offset = offset;
            this.priceIncrement = priceIncrement;
        }

        override public bool IsBuySignal(int rawCandleIndex)
        {
            try
            {
                int candlesStartIndex = Candles.Count - candlesSpan;
                int candleIndex = candlesStartIndex + rawCandleIndex;

                if (whenToBuyPrice > -1)
                {
                    if (Candles[candleIndex].Close > whenToBuyPrice)
                    {
                        boughtCandle = candleIndex;
                        whenToBuyPrice = -1;
                        whenToBuyPriceSetIndex = -1;
                        stopLoss = MA[rawCandleIndex] - priceIncrement * 10;
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

        override public bool IsSellSignal(int rawCandleIndex)
        {
            try
            {
                int candlesStartIndex = Candles.Count - candlesSpan;
                int candleIndex = candlesStartIndex + rawCandleIndex;

                if (Candles[candleIndex].Close < stopLoss)
                {
                    boughtCandle = -1;
                    stopLoss = -1;
                    whenToSellIndex = -1;
                    return true;
                }

                if (whenToSellIndex == candleIndex)
                {
                    boughtCandle = -1;
                    stopLoss = -1;
                    whenToSellIndex = -1;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        override public void UpdateState(int rawCandleIndex)
        {
            try
            {
                int candlesStartIndex = Candles.Count - candlesSpan;
                int candleIndex = candlesStartIndex + rawCandleIndex;

                if (boughtCandle != -1)
                {
                    if (whenToSellIndex == -1 && Candles[candleIndex].Close < MA[rawCandleIndex])
                    {
                        whenToSellIndex = candleIndex + 1;
                    }
                }
                else
                {
                    if (whenToBuyPrice > -1)
                    {
                        if (Candles[candleIndex].Close < whenToBuyPrice &&
                            candleIndex - whenToBuyPriceSetIndex > 10)
                        {
                            whenToBuyPrice = -1;
                            whenToBuyPriceSetIndex = -1;
                        }
                    }
                    else
                    {
                        bool isCandleBig = true;
                        decimal candleSize = Math.Abs(Candles[candleIndex].Close - Candles[candleIndex - 1].Close);
                        for (int i = 1; i < offset + 1; ++i)
                        {
                            decimal thisCandleSize = Math.Abs(Candles[candleIndex - i].Close - Candles[candleIndex - i - 1].Close);
                            if (thisCandleSize > candleSize)
                                isCandleBig = false;
                        }

                        if (isCandleBig &&
                            ((Candles[candleIndex - 1].Close - MA[rawCandleIndex - 1]) *
                                (Candles[candleIndex].Close - MA[rawCandleIndex]) < 0) &&
                            Candles[candleIndex].Close > MA[rawCandleIndex])
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
            var SMA = new List<decimal>(candlesSpan);
            for (int i = Candles.Count - candlesSpan; i < Candles.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < period; ++j)
                    sum += Candles[i - j].Close;
                SMA.Add(sum / period);
            }
            MA = new ChartValues<decimal>(SMA);
        }

        override public void UpdateSeries()
        {
            CalculateSMA();
            bindedGraph.Values = MA;
        }

        override public void InitializeSeries(SeriesCollection series)
        {
            bindedGraph = new LineSeries
            {
                ScalesXAt = 0,
                Values = MA,
                Title = string.Format("Simple Moving Average {0}", period),
            };
            series.Add(bindedGraph);
            areGraphsInitialized = true;
        }

        public override void ResetState()
        {
            boughtCandle = -1;
            whenToSellIndex = -1;
            whenToBuyPrice = -1;
            whenToBuyPriceSetIndex = -1;
            stopLoss = -1;
        }
    }
}