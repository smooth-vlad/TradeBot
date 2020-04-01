using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;
using Axis = OxyPlot.Axes.Axis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using PlotCommands = OxyPlot.PlotCommands;
using ScatterSeries = OxyPlot.Series.ScatterSeries;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для TradingChart.xaml
    /// </summary>
    public partial class TradingChart : UserControl
    {
        readonly List<DateTime> candlesDates = new List<DateTime>();

        readonly CandleStickSeries candlesSeries;

        List<Indicator> indicators = new List<Indicator>();
        readonly Queue<List<Indicator.Signal>> lastSignals = new Queue<List<Indicator.Signal>>(3);
        
        readonly ScatterSeries buySeries;
        readonly ScatterSeries sellSeries;

        readonly PlotModel model;
        readonly LinearAxis xAxis;
        readonly LinearAxis yAxis;

        List<(PlotView plot, LinearAxis x, LinearAxis y)> oscillatorsPlots
            = new List<(PlotView plot, LinearAxis x, LinearAxis y)>();
        
        public MarketInstrument activeStock;

        public CandleInterval candleInterval = CandleInterval.Minute;

        int candlesLoadsFailed;
        int loadedCandles;
        
        public DateTime LastCandleDate { get; private set; } // on the right side
        public DateTime FirstCandleDate { get; private set; } // on the left side

        public Task LoadingCandlesTask { get; private set; }

        public Context context;

        public TradingChart()
        {
            InitializeComponent();

            LastCandleDate = FirstCandleDate = DateTime.Now;

            model = new PlotModel
            {
                TextColor = OxyColor.FromArgb(140, 0, 0, 0),
                PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0),
                LegendPosition = LegendPosition.LeftTop
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
                TickStyle = TickStyle.Outside
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
                MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0)
            };

            model.Axes.Add(yAxis);
            model.Axes.Add(xAxis);

            candlesSeries = new CandleStickSeries
            {
                Title = "Candles",
                DecreasingColor = OxyColor.FromRgb(230, 63, 60),
                IncreasingColor = OxyColor.FromRgb(45, 128, 32),
                StrokeThickness = 1
            };

            buySeries = new ScatterSeries
            {
                Title = "Buy",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(207, 105, 255),
                MarkerStroke = OxyColor.FromRgb(55, 55, 55),
                MarkerStrokeThickness = 1,
                MarkerSize = 6
            };

            sellSeries = new ScatterSeries
            {
                Title = "Sell",
                MarkerType = MarkerType.Circle,
                MarkerFill = OxyColor.FromRgb(255, 248, 82),
                MarkerStroke = OxyColor.FromRgb(55, 55, 55),
                MarkerStrokeThickness = 1,
                MarkerSize = 6
            };

            model.Series.Add(candlesSeries);
            model.Series.Add(buySeries);
            model.Series.Add(sellSeries);

            xAxis.LabelFormatter = delegate(double d)
            {
                if (candlesSeries.Items.Count <= (int) d || !(d >= 0)) return "";
                switch (candleInterval)
                {
                    case CandleInterval.Minute:
                    case CandleInterval.TwoMinutes:
                    case CandleInterval.ThreeMinutes:
                    case CandleInterval.FiveMinutes:
                    case CandleInterval.TenMinutes:
                    case CandleInterval.QuarterHour:
                    case CandleInterval.HalfHour:
                        return candlesDates[(int) d].ToString("HH:mm");
                    case CandleInterval.Hour:
                    case CandleInterval.TwoHours:
                    case CandleInterval.FourHours:
                    case CandleInterval.Day:
                    case CandleInterval.Week:
                        return candlesDates[(int) d].ToString("dd MMMM");
                    case CandleInterval.Month:
                        return candlesDates[(int) d].ToString("yyyy");
                }

                return "";
            };
            xAxis.AxisChanged += XAxis_AxisChanged;

            PlotView.Model = model;

            PlotView.ActualController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            PlotView.ActualController.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);

            DataContext = this;
        }

        (PlotView plot, LinearAxis x, LinearAxis y) AddOscillatorPlot()
        {
            var plot = new PlotView();

            Grid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(150)});
            Grid.Children.Add(plot);
            plot.SetValue(Grid.RowProperty, Grid.RowDefinitions.Count - 1);

            plot.Model = new PlotModel
            {
                TextColor = OxyColor.FromArgb(140, 0, 0, 0),
                PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0),
                LegendPosition = LegendPosition.LeftTop
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
            y.Zoom(-1, 1);

            var x = new LinearAxis // x axis (bottom)
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.None,
                MinorGridlineStyle = LineStyle.None,
                TicklineColor = OxyColor.FromArgb(10, 0, 0, 0),
                TickStyle = TickStyle.None,
                EndPosition = 0,
                StartPosition = 1,
                MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0)
            };
            x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);
            
            plot.ActualController.UnbindAll();

            plot.Model.Axes.Add(x);
            plot.Model.Axes.Add(y);
            
            AdjustYExtent(x, y, plot.Model);
            plot.InvalidatePlot();

            oscillatorsPlots.Add((plot, x, y));
            return oscillatorsPlots.Last();
        }

        async void XAxis_AxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (LoadingCandlesTask == null || !LoadingCandlesTask.IsCompleted)
                return;

            LoadingCandlesTask = LoadMoreCandlesAndUpdateSeries();
            await LoadingCandlesTask;

            AdjustYExtent(xAxis, yAxis, model);
            foreach (var plot in oscillatorsPlots)
            {
                plot.x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);
                AdjustYExtent(plot.x, plot.y, plot.plot.Model);
                plot.plot.InvalidatePlot();
            }
        }

        async Task LoadMoreCandlesAndUpdateSeries()
        {
            var loaded = false;
            while (loadedCandles < xAxis.ActualMaximum && candlesLoadsFailed < 10)
            {
                await LoadMoreCandles();
                loaded = true;
            }

            if (loaded)
            {
                foreach (var indicator in indicators)
                    indicator.UpdateSeries();
                AdjustYExtent(xAxis, yAxis, model);
                foreach (var plot in oscillatorsPlots)
                    AdjustYExtent(plot.x, plot.y, plot.plot.Model);
            }

            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        public async void ResetSeries()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            candlesSeries.Items.Clear();
            candlesDates.Clear();

            loadedCandles = 0;
            candlesLoadsFailed = 0;
            LastCandleDate = DateTime.Now;
            FirstCandleDate = LastCandleDate;

            foreach (var indicator in indicators) indicator.ResetSeries();

            LoadingCandlesTask = LoadMoreCandlesAndUpdateSeries();
            await LoadingCandlesTask;

            xAxis.Zoom(0, 75);

            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        async Task LoadMoreCandles()
        {
            if (activeStock == null || context == null ||
                candlesLoadsFailed >= 10 ||
                loadedCandles > xAxis.ActualMaximum + 100)
                return;

            var period = GetPeriod(candleInterval);
            var candles = await GetCandles(activeStock.Figi, FirstCandleDate - period, 
                FirstCandleDate, candleInterval);
            FirstCandleDate -= period;
            if (candles.Count == 0)
            {
                candlesLoadsFailed += 1;
                return;
            }

            for (var i = 0; i < candles.Count; ++i)
            {
                var candle = candles[i];
                candlesSeries.Items.Add(CandleToHighLowItem(loadedCandles + i, candle));
                candlesDates.Add(candle.Time);
            }

            loadedCandles += candles.Count;

            candlesLoadsFailed = 0;
        }

        public async Task UpdateTestingSignals()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();
            lastSignals.Clear();

            await Task.Factory.StartNew(() =>
            {
                for (var i = candlesSeries.Items.Count - 1; i >= 0; --i) UpdateSignals(i);
            });
            PlotView.InvalidatePlot();
        }

        public void UpdateRealTimeSignals()
        {
            UpdateSignals(0);
            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        void UpdateSignals(int i)
        {
            var candle = candlesSeries.Items[i];
            var signals = new List<Indicator.Signal>();

            if (lastSignals.Count >= 3)
                lastSignals.Dequeue();
            lastSignals.Enqueue(signals);

            signals.AddRange(
                from indicator in indicators
                select indicator.GetSignal(i)
                into rawSignal
                where rawSignal.HasValue
                select rawSignal.Value);

            var value = CalculateSignalValue();
            if (value > 0)
                buySeries.Points.Add(new ScatterPoint(i, candle.Close, Math.Abs(value / indicators.Count) * 10));
            else if (value < 0)
                sellSeries.Points.Add(new ScatterPoint(i, candle.Close, Math.Abs(value / indicators.Count) * 10));
        }

        float CalculateSignalValue()
        {
            var value = 0.0f;
            var multiplier = 0.25f;
            foreach (var signalsList in lastSignals)
            {
                foreach (var signal in signalsList)
                    if (signal.type == Indicator.Signal.Type.Buy)
                        value += signal.weight * multiplier;
                    else
                        value -= signal.weight * multiplier;

                multiplier *= 2;
            }

            return value;
        }

        public void AddIndicator(Indicator indicator)
        {
            indicator.priceIncrement = (double) activeStock.MinPriceIncrement;
            indicator.candles = candlesSeries.Items;
            indicators.Add(indicator);

            indicator.InitializeSeries(indicator.IsOscillator ?
                AddOscillatorPlot().plot.Model.Series : model.Series);

            indicator.UpdateSeries();
            AdjustYExtent(xAxis, yAxis, model);
            foreach (var plot in oscillatorsPlots)
                AdjustYExtent(plot.x, plot.y, plot.plot.Model);

            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        public void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.RemoveSeries();
            indicators = new List<Indicator>();
            AdjustYExtent(xAxis, yAxis, model);
            foreach (var plot in oscillatorsPlots)
                AdjustYExtent(plot.x, plot.y, plot.plot.Model);

            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        public async Task LoadNewCandles()
        {
            List<CandlePayload> candles;
            try
            {
                candles = await GetCandles(activeStock.Figi, LastCandleDate,
                    DateTime.Now, candleInterval);
            }
            catch (Exception)
            {
                return;
            }

            if (candles.Count == 0)
                return;
            
            LastCandleDate = DateTime.Now;

            var c = new List<HighLowItem>();
            var cd = new List<DateTime>();
            for (var i = 0; i < candles.Count; ++i)
            {
                var candle = candles[i];
                c.Add(CandleToHighLowItem(i, candle));
                cd.Add(candle.Time);
            }

            candlesSeries.Items.ForEach(v => v.X += candles.Count);
            candlesSeries.Items.InsertRange(0, c);
            candlesDates.InsertRange(0, cd);
            foreach (var indicator in indicators)
            {
                indicator.UpdateSeries();
                indicator.OnNewCandlesAdded(candles.Count);
            }

            loadedCandles += candles.Count;

            AdjustYExtent(xAxis, yAxis, model);
            PlotView.InvalidatePlot();

            // move buy points by candles.Count
            var s = new List<ScatterPoint>(buySeries.Points.Count);
            s.AddRange(buySeries.Points.Select(
                point => new ScatterPoint(point.X + candles.Count, point.Y, point.Size, point.Value, point.Tag)));
            buySeries.Points.Clear();
            buySeries.Points.AddRange(s);

            // move sell points by candles.Count
            s = new List<ScatterPoint>(sellSeries.Points.Count);
            s.AddRange(sellSeries.Points.Select(
                point => new ScatterPoint(point.X + candles.Count, point.Y, point.Size, point.Value, point.Tag)));
            sellSeries.Points.Clear();
            sellSeries.Points.AddRange(s);

            UpdateRealTimeSignals();

            PlotView.InvalidatePlot();
            
            foreach (var plot in oscillatorsPlots)
                AdjustYExtent(plot.x, plot.y, plot.plot.Model);
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        void AdjustYExtent(Axis x, Axis y, PlotModel m)
        {
            var points = new List<float>();

            foreach (var series in m.Series)
                if (series.GetType() == typeof(CandleStickSeries))
                {
                    points.AddRange(((CandleStickSeries) series).Items
                        .FindAll(p => p.X >= x.ActualMinimum && p.X <= x.ActualMaximum)
                        .ConvertAll(v => (float) v.High));
                    points.AddRange(((CandleStickSeries) series).Items
                        .FindAll(p => p.X >= x.ActualMinimum && p.X <= x.ActualMaximum)
                        .ConvertAll(v => (float) v.Low));
                }
                else if (series.GetType() == typeof(LineSeries))
                {
                    points.AddRange(((LineSeries) series).Points
                        .FindAll(p => p.X >= x.ActualMinimum && p.X <= x.ActualMaximum)
                        .ConvertAll(v => (float) v.Y));
                }
                else if (series.GetType() == typeof(HistogramSeries))
                {
                    points.AddRange(((HistogramSeries) series).Items
                        .FindAll(p => p.RangeStart >= x.ActualMinimum && p.RangeStart <= x.ActualMaximum)
                        .ConvertAll(v => (float) v.Value));
                }

            var min = double.MaxValue;
            var max = double.MinValue;

            foreach (var point in points)
            {
                if (point > max)
                    max = point;
                if (point < min)
                    min = point;
            }

            if (min == double.MaxValue || max == double.MinValue)
                return;

            var extent = max - min;
            var margin = extent * 0.1;

            y.IsZoomEnabled = true;
            y.Zoom(min - margin, max + margin);
            y.IsZoomEnabled = false;
        }

        public static HighLowItem CandleToHighLowItem(double x, CandlePayload candlePayload)
        {
            return new HighLowItem(x, (double) candlePayload.High, (double) candlePayload.Low,
                (double) candlePayload.Open, (double) candlePayload.Close);
        }

        public async Task<List<CandlePayload>> GetCandles(string figi, DateTime from, DateTime to, CandleInterval interval)
        {
            var candles = await context.MarketCandlesAsync(figi, from, to, interval);

            var result = candles.Candles.ToList();

            result.Reverse();
            return result;
        }

        // ================================
        // =========  Events ==============
        // ================================

        void MovingAverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MovingAverageDialog();
            if (dialog.ShowDialog() != true) return;
            IMaCalculation calculationMethod;
            switch (dialog.Type)
            {
                case MovingAverageDialog.CalculationMethod.Simple:
                    calculationMethod = new SimpleMaCalculation();
                    break;
                case MovingAverageDialog.CalculationMethod.Exponential:
                    calculationMethod = new ExponentialMaCalculation();
                    break;
                default:
                    calculationMethod = new SimpleMaCalculation();
                    break;
            }

            AddIndicator(new MovingAverage(dialog.Period, calculationMethod));
        }
        
        void MACD_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MacdDialog();
            if (dialog.ShowDialog() != true) return;
            IMaCalculation calculationMethod;
            switch (dialog.Type)
            {
                case MacdDialog.CalculationMethod.Simple:
                    calculationMethod = new SimpleMaCalculation();
                    break;
                case MacdDialog.CalculationMethod.Exponential:
                    calculationMethod = new ExponentialMaCalculation();
                    break;
                default:
                    calculationMethod = new SimpleMaCalculation();
                    break;
            }

            AddIndicator(new Macd(
                calculationMethod, dialog.ShortPeriod, dialog.LongPeriod, dialog.HistogramPeriod));
        }

        void RemoveIndicators_Click(object sender, RoutedEventArgs e)
        {
            RemoveIndicators();
        }

        #region IntervalToMaxPeriod

        public static readonly Dictionary<CandleInterval, TimeSpan> intervalToMaxPeriod
            = new Dictionary<CandleInterval, TimeSpan>
            {
                {CandleInterval.Minute, TimeSpan.FromDays(1)},
                {CandleInterval.FiveMinutes, TimeSpan.FromDays(1)},
                {CandleInterval.QuarterHour, TimeSpan.FromDays(1)},
                {CandleInterval.HalfHour, TimeSpan.FromDays(1)},
                {CandleInterval.Hour, TimeSpan.FromDays(7).Add(TimeSpan.FromHours(-1))},
                {CandleInterval.Day, TimeSpan.FromDays(364)},
                {CandleInterval.Week, TimeSpan.FromDays(364 * 2)},
                {CandleInterval.Month, TimeSpan.FromDays(364 * 10)}
            };

        public TimeSpan GetPeriod(CandleInterval interval)
        {
            if (!intervalToMaxPeriod.TryGetValue(interval, out var result))
                throw new KeyNotFoundException();
            return result;
        }

        #endregion
    }
}