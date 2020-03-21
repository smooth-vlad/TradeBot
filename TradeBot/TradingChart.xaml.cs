using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Tinkoff.Trading.OpenApi.Network;
using Tinkoff.Trading.OpenApi.Models;
using System.Windows.Controls;
using System.Diagnostics;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Threading;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для TradingChart.xaml
    /// </summary>
    public partial class TradingChart : UserControl
    {
        public Context context;
        public MarketInstrument activeStock;

        public delegate void CandlesChangeHandler();
        public event CandlesChangeHandler CandlesChange;

        public int candlesSpan = 75;
        public CandleInterval candleInterval = CandleInterval.Minute;
        public List<Indicator> indicators { get; private set; } = new List<Indicator>();

        private CandleStickSeries candlesSeries;
        private ScatterSeries buySeries;
        private ScatterSeries sellSeries;

        private LinearAxis xAxis;
        private LinearAxis yAxis;

        private List<DateTime> candlesDates = new List<DateTime>();

        private DateTime lastCandleDate; // on the right side
        private DateTime firstCandleDate; // on the left side

        private int loadedCandles = 0;
        private int candlesLoadsFailed = 0;

        public PlotModel model;

        private Task candlesLoadingTask;

        public TradingChart()
        {
            InitializeComponent();

            lastCandleDate = firstCandleDate = DateTime.Now;

            model = new PlotModel
            {
                TextColor = OxyColor.FromArgb(140, 0, 0, 0),
                PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0),
            };

            yAxis = new LinearAxis // y axis (left)
            {
                Position = AxisPosition.Left,
                IsPanEnabled = false,
                IsZoomEnabled = false,
                MajorGridlineThickness = 0,
                MinorGridlineThickness = 0,
                MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0),
                MajorGridlineStyle = LineStyle.Solid,
                TicklineColor = OxyColor.FromArgb(10, 0, 0, 0),
                TickStyle = TickStyle.Outside,
            };

            xAxis = new LinearAxis // x axis (bottom)
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                TicklineColor = OxyColor.FromArgb(10, 0, 0, 0),
                TickStyle = TickStyle.Outside,
                MaximumRange = 200,
                MinimumRange = 15,
                AbsoluteMinimum = -10,
                EndPosition = 0,
                StartPosition = 1,
                MajorGridlineThickness = 2,
                MinorGridlineThickness = 0,
                MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0),
            };

            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);

            candlesSeries = new CandleStickSeries
            {
                Title = "Candles",
                DecreasingColor = OxyColor.FromRgb(230, 63, 60),
                IncreasingColor = OxyColor.FromRgb(45, 128, 32),
                StrokeThickness = 1,
            };

            buySeries = new ScatterSeries
            {
                Title = "Buy",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(207, 105, 255),
                MarkerStroke = OxyColor.FromRgb(55, 55, 55),
                MarkerStrokeThickness = 1,
                MarkerSize = 7.5,
            };

            sellSeries = new ScatterSeries
            {
                Title = "Sell",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(255, 248, 82),
                MarkerStroke = OxyColor.FromRgb(55, 55, 55),
                MarkerStrokeThickness = 1,
                MarkerSize = 7.5,
            };

            model.Series.Add(candlesSeries);
            model.Series.Add(buySeries);
            model.Series.Add(sellSeries);

            xAxis.LabelFormatter = delegate (double d)
            {
                if (candlesSeries.Items.Count > (int)d && d >= 0)
                {
                    switch (candleInterval)
                    {
                        case CandleInterval.Minute:
                        case CandleInterval.TwoMinutes:
                        case CandleInterval.ThreeMinutes:
                        case CandleInterval.FiveMinutes:
                        case CandleInterval.TenMinutes:
                        case CandleInterval.QuarterHour:
                        case CandleInterval.HalfHour:
                            return candlesDates[(int)d].ToString("HH:mm");
                        case CandleInterval.Hour:
                        case CandleInterval.TwoHours:
                        case CandleInterval.FourHours:
                        case CandleInterval.Day:
                        case CandleInterval.Week:
                            return candlesDates[(int)d].ToString("dd MMMM");
                        case CandleInterval.Month:
                            return candlesDates[(int)d].ToString("yyyy");
                    }
                }
                return "";
            };
            xAxis.AxisChanged += XAxis_AxisChanged;

            plotView.Model = model;

            plotView.ActualController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            plotView.ActualController.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);

            DataContext = this;
        }

        private async void XAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            await LoadMoreCandlesAndUpdateSeries();
        }

        private async Task LoadMoreCandlesAndUpdateSeries()
        {
            if (candlesLoadingTask != null)
                await candlesLoadingTask;

            while (loadedCandles < xAxis.ActualMaximum && candlesLoadsFailed < 10)
            {
                candlesLoadingTask = LoadMoreCandles();
                await candlesLoadingTask;
            }
            foreach (var indicator in indicators)
                indicator.UpdateSeries();
            AdjustYExtent();

            plotView.InvalidatePlot();
        }

        public static HighLowItem CandleToHighLowItem(double x, CandlePayload candlePayload)
        {
            return new HighLowItem(x, (double)candlePayload.High, (double)candlePayload.Low, (double)candlePayload.Open, (double)candlePayload.Close);
        }

        public async Task<List<CandlePayload>> GetCandles(string figi, DateTime to, CandleInterval interval, TimeSpan queryOffset)
        {
            var result = new List<CandlePayload>();
            var candles = await context.MarketCandlesAsync(figi, to - queryOffset, to, interval);

            for (int i = 0; i < candles.Candles.Count; ++i)
                result.Add(candles.Candles[i]);

            result.Reverse();
            return result;
        }

        public async void ResetSeries()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            candlesSeries.Items.Clear();
            candlesDates.Clear();

            loadedCandles = 0;
            candlesLoadsFailed = 0;
            lastCandleDate = DateTime.Now;
            firstCandleDate = lastCandleDate;

            foreach (var indicator in indicators)
            {
                indicator.ResetState();
                indicator.ResetSeries();
            }

            await LoadMoreCandlesAndUpdateSeries();

            xAxis.Zoom(0, 75);

            plotView.InvalidatePlot();
        }

        private async Task LoadMoreCandles()
        {
            if (activeStock == null || context == null ||
                candlesLoadsFailed >= 10 ||
                loadedCandles > xAxis.ActualMaximum + 100)
                return;

            var period = GetPeriod(candleInterval);
            var candles = await GetCandles(activeStock.Figi, firstCandleDate, candleInterval, period);
            firstCandleDate -= period;
            if (candles.Count == 0)
            {
                candlesLoadsFailed += 1;
                return;
            }

            for (int i = 0; i < candles.Count; ++i)
            {
                var candle = candles[i];
                candlesSeries.Items.Add(CandleToHighLowItem(loadedCandles + i, candle));
                candlesDates.Add(candle.Time);
            }
            loadedCandles += candles.Count;

            candlesLoadsFailed = 0;
        }

        public async Task Simulate()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            foreach (var indicator in indicators)
                indicator.ResetState();

            await Task.Factory.StartNew(() =>
            {
                for (int i = candlesSeries.Items.Count - 1; i >= 0; --i)
                {
                    var candle = candlesSeries.Items[i];
                    foreach (var indicator in indicators)
                    {
                        indicator.UpdateState(i);
                        if (indicator.IsBuySignal(i))
                            buySeries.Points.Add(new ScatterPoint(i, candle.Close));
                        else if (indicator.IsSellSignal(i))
                            sellSeries.Points.Add(new ScatterPoint(i, candle.Close));
                    }
                }
            });
            plotView.InvalidatePlot();
        }

        public void AddIndicator(Indicator indicator)
        {
            indicator.priceIncrement = (double)activeStock.MinPriceIncrement;
            indicator.Candles = candlesSeries.Items;
            indicators.Add(indicator);

            UpdateIndicatorSeries(indicator);
            AdjustYExtent();
        }

        public void UpdateIndicatorSeries(Indicator indicator)
        {
            if (!indicator.AreSeriesInitialized)
                indicator.InitializeSeries(model.Series);
            indicator.UpdateSeries();

            plotView.InvalidatePlot();
        }

        public void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.RemoveSeries(model.Series);
            indicators = new List<Indicator>();
            AdjustYExtent();
            plotView.InvalidatePlot();
        }

        private void AdjustYExtent()
        {
            if (xAxis != null && yAxis != null && candlesSeries.Items.Count != 0)
            {
                double istart = xAxis.ActualMinimum;
                double iend = xAxis.ActualMaximum;

                var ptlist = candlesSeries.Items.FindAll(p => p.X >= istart && p.X <= iend);
                if (ptlist.Count == 0)
                    return;

                var lplist = new List<DataPoint>();

                foreach (var series in model.Series)
                {
                    if (series.GetType() == typeof(LineSeries))
                    {
                        lplist.AddRange((series as LineSeries).Points.FindAll(p => p.X >= istart && p.X <= iend));
                    }
                }

                double ymin = double.MaxValue;
                double ymax = double.MinValue;
                for (int i = 0; i < ptlist.Count; ++i)
                {
                    ymin = Math.Min(ymin, ptlist[i].Low);
                    ymax = Math.Max(ymax, ptlist[i].High);
                }

                for (int i = 0; i < lplist.Count; ++i)
                {
                    ymin = Math.Min(ymin, lplist[i].Y);
                    ymax = Math.Max(ymax, lplist[i].Y);
                }

                var extent = ymax - ymin;
                var margin = extent * 0.1;

                yAxis.IsZoomEnabled = true;
                yAxis.Zoom(ymin - margin, ymax + margin);
                yAxis.IsZoomEnabled = false;
            }
        }

        public static readonly Dictionary<CandleInterval, TimeSpan> intervalToMaxPeriod
            = new Dictionary<CandleInterval, TimeSpan>
        {
            { CandleInterval.Minute,        TimeSpan.FromDays(1)},
            { CandleInterval.FiveMinutes,   TimeSpan.FromDays(1)},
            { CandleInterval.QuarterHour,   TimeSpan.FromDays(1)},
            { CandleInterval.HalfHour,      TimeSpan.FromDays(1)},
            { CandleInterval.Hour,          TimeSpan.FromDays(7).Add(TimeSpan.FromHours(-1))},
            { CandleInterval.Day,           TimeSpan.FromDays(364)},
            { CandleInterval.Week,          TimeSpan.FromDays(364*2)},
            { CandleInterval.Month,         TimeSpan.FromDays(364*10)},
        };

        public TimeSpan GetPeriod(CandleInterval interval)
        {
            TimeSpan result;
            if (!intervalToMaxPeriod.TryGetValue(interval, out result))
                throw new KeyNotFoundException();
            return result;
        }

        public async Task LoadNewCandles()
        {
            if (DateTime.Now.Subtract(lastCandleDate) < TimeSpan.FromSeconds(5))
                return;
            var candles = await GetCandles(activeStock.Figi, DateTime.Now, candleInterval, DateTime.Now.Subtract(lastCandleDate));
            if (candles.Count == 0)
                return;

            lastCandleDate = DateTime.Now;

            var c = new List<HighLowItem>();
            var cd = new List<DateTime>();
            for (int i = candles.Count - 1; i >= 0; --i)
            {
                var candle = candles[i];
                c.Add(CandleToHighLowItem(i, candle));
                cd.Add(candle.Time);
            }
            candlesSeries.Items.ForEach((v) => v.X += candles.Count);
            candlesSeries.Items.InsertRange(0, c);
            candlesDates.InsertRange(0, cd);
            // update indicator values

            plotView.InvalidatePlot();
        }

        private void MovingAverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MovingAverageDialog();
            if (dialog.ShowDialog() == true)
            {
                AddIndicator(new MovingAverage(dialog.Period, dialog.Offset, dialog.Type));
            }
        }

        private void RemoveIndicators_Click(object sender, RoutedEventArgs e)
        {
            RemoveIndicators();
        }
    }
}
