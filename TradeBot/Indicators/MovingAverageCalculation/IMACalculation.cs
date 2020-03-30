﻿using OxyPlot;
using OxyPlot.Series;
using System;

namespace TradeBot
{
    public interface IMaCalculation
    {
        string Title { get; }

        void Calculate(Func<int, double> value, int count, int period, LineSeries series);
        void Calculate(Func<int, double> value, int count, int period, HistogramSeries series);
    }
}
