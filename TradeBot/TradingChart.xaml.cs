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
        private bool isLoadingCandles = false;

        public PlotModel model;

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
                var c = candlesSeries.Items.FindIndex((v) => v.X == d);
                if (c >= 0)
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
                            return candlesDates[c].ToString("HH:mm");
                        case CandleInterval.Hour:
                        case CandleInterval.TwoHours:
                        case CandleInterval.FourHours:
                        case CandleInterval.Day:
                        case CandleInterval.Week:
                            return candlesDates[c].ToString("dd MMMM");
                        case CandleInterval.Month:
                            return candlesDates[c].ToString("yyyy");
                    }
                }
                return "";
            };
            xAxis.AxisChanged += XAxis_AxisChanged;
            UpdateXAxis();

            plotView.Model = model;

            plotView.ActualController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            plotView.ActualController.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);

            DataContext = this;
        }

        private async void XAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {            
            await LoadMoreCandles();
            UpdateIndicatorsSeries();
            AdjustYExtent();
        }

        public static HighLowItem CandleToHighLowItem(double x, CandlePayload candlePayload)
        {
            return new HighLowItem(x, (double)candlePayload.High, (double)candlePayload.Low, (double)candlePayload.Open, (double)candlePayload.Close);
        }

        public async Task<List<CandlePayload>> GetFixedAmountOfCandles(string figi, int amount, CandleInterval interval, TimeSpan queryOffset)
        {
            var result = new List<CandlePayload>(amount);
            var to = DateTime.Now;

            while (result.Count < amount)
            {
                var candles = await context.MarketCandlesAsync(figi, to - queryOffset, to, interval);

                for (int i = candles.Candles.Count - 1; i >= 0 && result.Count < amount; --i)
                    result.Add(candles.Candles[i]);
                to = to - queryOffset;
            }
            result.Reverse();
            return result;
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

        public async Task ResetSeries()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            candlesSeries.Items.Clear();
            candlesDates.Clear();

            loadedCandles = 0;
            candlesLoadsFailed = 0;
            lastCandleDate = DateTime.Now;
            firstCandleDate = lastCandleDate;

            await LoadMoreCandles();
            xAxis.Zoom(0, 75);

            ResetIndicators();

            plotView.InvalidatePlot();
        }

        private void UpdateXAxis() => xAxis.ZoomAtCenter(1);

        private async Task LoadMoreCandles()
        {
            if (isLoadingCandles || candlesLoadsFailed >= 10 || activeStock == null || context == null)
                return;

            if (loadedCandles > xAxis.ActualMaximum + 100)
                return;

            isLoadingCandles = true;

            var period = GetPeriod(candleInterval);
            var candles = await GetCandles(activeStock.Figi, firstCandleDate, candleInterval, period);
            firstCandleDate -= period;
            if (candles.Count == 0)
            {
                isLoadingCandles = false;
                candlesLoadsFailed += 1;
                return;
            }

            for (int i = 0; i < candles.Count; ++i)
            {
                var candle = candles[i];
                candlesSeries.Items.Add(new HighLowItem(loadedCandles + i, (double)candle.High, (double)candle.Low, (double)candle.Open, (double)candle.Close));
                candlesDates.Add(candle.Time);
            }
            loadedCandles += candles.Count;

            isLoadingCandles = false;
            plotView.InvalidatePlot();
        }

        public async Task Simulate()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            await Task.Run(() =>
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
            indicators.Add(indicator);

            UpdateIndicatorSeries(indicator);
            UpdateXAxis();
        }

        public void UpdateIndicatorsSeries()
        {
            foreach (var indicator in indicators)
                UpdateIndicatorSeries(indicator);
        }

        public void UpdateIndicatorSeries(Indicator indicator)
        {
            indicator.Candles = candlesSeries.Items;

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
            UpdateXAxis();
            plotView.InvalidatePlot();
        }

        public void ResetIndicators()
        {
            foreach (var indicator in indicators)
                indicator.RecalculateSeries();
            UpdateXAxis();
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

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadMoreCandles();
            UpdateXAxis();
            plotView.InvalidatePlot();
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
    }
}
