using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Linq;
using LinearAxis = OxyPlot.Axes.LinearAxis;

namespace TradeBot
{
    public abstract class OscillatorIndicator : Indicator
    {
        public abstract (double min, double max)? YAxisRange { get; }

        public (PlotView view, LinearAxis x, LinearAxis y) Plot { get; protected set; }

        public OscillatorIndicator(List<HighLowItem> candles)
            : base(candles)
        {
            CreatePlot();
        }

        public void CreatePlot()
        {
            var plot = new PlotView
            {
                Model = new PlotModel
                {
                    TextColor = OxyColor.FromArgb(140, 0, 0, 0),
                    PlotAreaBorderThickness = new OxyThickness(0, 1, 0, 1),
                    PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0),
                    LegendPosition = LegendPosition.LeftTop,
                    LegendBackground = OxyColor.FromRgb(245, 245, 245),
                }
            };

            var y = new LinearAxis // y axis (left)
            {
                Position = AxisPosition.Left,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MajorGridlineThickness = 0,
                MinorGridlineThickness = 0,
                MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0),
                MajorGridlineStyle = LineStyle.Solid,
                TicklineColor = OxyColor.FromArgb(10, 0, 0, 0),
                TickStyle = TickStyle.Outside
            };

            var x = new LinearAxis // x axis (bottom)
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineThickness = 2,
                MinorGridlineStyle = LineStyle.None,
                TicklineColor = OxyColor.FromArgb(10, 0, 0, 0),
                TickStyle = TickStyle.None,
                LabelFormatter = v => string.Empty,
                EndPosition = 0,
                StartPosition = 1,
                MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0)
            };
            if (!YAxisRange.HasValue)
            {
                x.AxisChanged += (object sender, AxisChangedEventArgs e) =>
                {
                    TradingChart.AdjustYExtent(x, y, plot.Model);
                    plot.InvalidatePlot();
                };
                SeriesUpdated += () =>
                {
                    TradingChart.AdjustYExtent(x, y, plot.Model);
                    plot.InvalidatePlot();
                };
            }
            else
            {
                y.Zoom(YAxisRange.Value.min, YAxisRange.Value.max);
                x.AxisChanged += (object sender, AxisChangedEventArgs e) =>
                {
                    plot.InvalidatePlot();
                };
                SeriesUpdated += () =>
                {
                    plot.InvalidatePlot();
                };
            }

            plot.ActualController.UnbindAll();

            plot.Model.Axes.Add(x);
            plot.Model.Axes.Add(y);

            this.Plot = (plot, x, y);
        }
    }
}
