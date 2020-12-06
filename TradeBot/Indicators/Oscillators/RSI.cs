using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public class Rsi : OscillatorIndicator
    {
        public int Period { get; private set; }

        private LineSeries series;
        public IReadOnlyList<DataPoint> Values => series.Points;

        public override (double min, double max)? YAxisRange => (0, 100);

        public const double OverboughtLine = 70;
        public const double OversoldLine = 30;

        private ElementCollection<Series> chart;

        public Rsi(List<HighLowItem> candles, int period)
            : base(candles)
        {
            this.Period = period;

            Plot.y.ExtraGridlineStyle = LineStyle.LongDash;
            Plot.y.ExtraGridlineColor = OxyColor.FromArgb(30, 0, 0, 0);
            Plot.y.ExtraGridlines = new double[]
            {
                OverboughtLine,
                OversoldLine,
            };

            series = new LineSeries
            {
                Title = "RSI",
            };
        }

        public override void AttachToChart(ElementCollection<Series> chart)
        {
            if (AreSeriesAttached || chart == null)
                return;

            this.chart = chart;

            chart.Add(series);

            AreSeriesAttached = true;
        }

        public override void DetachFromChart()
        {
            chart?.Remove(series);

            AreSeriesAttached = false;
        }

        public override void ResetSeries()
        {
            series.Points.Clear();
        }

        public override void UpdateSeries()
        {
            if (candles.Count < Period)
                return;

            if (series.Points.Count > Period * 2)
            {
                series.Points.RemoveRange(series.Points.Count - Period * 2, Period * 2);
            }
            for (int i = series.Points.Count; i < candles.Count - Period; ++i)
            {
                double u = 0;
                {
                    int count = 0;
                    for (int j = 0; j < Period; ++j)
                    {
                        if (candles[i + j].Close > candles[i + j + 1].Close)
                        {
                            u += candles[i + j].Close - candles[i + j + 1].Close;
                            count++;
                        }
                    }
                    u /= count;
                }
                double d = 0;
                {
                    int count = 0;
                    for (int j = 0; j < Period; ++j)
                    {
                        if (candles[i + j].Close < candles[i + j + 1].Close)
                        {
                            d += candles[i + j + 1].Close - candles[i + j].Close;
                            count++;
                        }
                    }
                    d /= count;
                }

                double rs;
                if (u == 0 || d == 0)
                    rs = 100;
                else
                    rs = 100 - (100 / (1 + u / d));
                series.Points.Add(new DataPoint(i, rs));
            }

            SeriesUpdated?.Invoke();
        }
    }
}
