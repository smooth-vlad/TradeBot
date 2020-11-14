using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;
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
        private readonly CandleStickSeries candlesSeries;

        private List<Indicator> indicators = new List<Indicator>();
        private readonly Dictionary<Indicator, Indicator.Signal?[]> lastSignals = new Dictionary<Indicator, Indicator.Signal?[]>();

        private readonly ScatterSeries buySeries;
        private readonly ScatterSeries sellSeries;

        private readonly PlotModel model;
        private readonly LinearAxis xAxis;
        private readonly LinearAxis yAxis;

        private List<(PlotView plot, LinearAxis x, LinearAxis y)> oscillatorsPlots
            = new List<(PlotView plot, LinearAxis x, LinearAxis y)>();

        private MarketInstrument activeInstrument;

        public MarketInstrument ActiveInstrument
        {
            get => activeInstrument;
            set
            {
                activeInstrument = value;
                candlesSeries.Title = value.Name;
            }
        }

        public enum State
        {
            Bought,
            Sold,
            Empty,
        }

        private State state = State.Empty;
        private double? stopLoss;
        public double Balance { get; private set; } = 10000;
        private double dealPrice;
        private int dealLots;

        public CandleInterval candleInterval = CandleInterval.Hour;

        private int candlesLoadsFailed;
        private int loadedCandles;

        private DateTime rightCandleDate; // newest
        private DateTime leftCandleDate; // oldest

        public Task LoadingCandlesTask { get; private set; }

        private class Candle : HighLowItem
        {
            public DateTime DateTime { get; }

            public Candle(int x, CandlePayload candle)
            {
                Close = (double)candle.Close;
                Open = (double)candle.Open;
                High = (double)candle.High;
                Low = (double)candle.Low;
                DateTime = candle.Time;
                X = x;
            }
        }

        public TradingChart()
        {
            InitializeComponent();

            rightCandleDate = leftCandleDate = DateTime.Now;

            model = new PlotModel
            {
                TextColor = OxyColor.FromArgb(140, 0, 0, 0),
                PlotAreaBorderThickness = new OxyThickness(0, 1, 0, 1),
                PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0),
                LegendPosition = LegendPosition.LeftTop,
                LegendBackground = OxyColor.FromRgb(245, 245, 245),
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
                Title = "Instrument",
                DecreasingColor = OxyColor.FromRgb(214, 107, 107),
                IncreasingColor = OxyColor.FromRgb(121, 229, 112),
                StrokeThickness = 1,
                TrackerFormatString = "Time: {DateTime:dd.MM.yyyy HH:mm}" + Environment.NewLine
                                  + "Price: {4}",
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

            xAxis.LabelFormatter = d =>
            {
                if (candlesSeries.Items.Count <= (int)d || !(d >= 0)) return "";
                var date = ((Candle)candlesSeries.Items[(int)d]).DateTime;
                switch (candleInterval)
                {
                    case CandleInterval.Minute:
                    case CandleInterval.TwoMinutes:
                    case CandleInterval.ThreeMinutes:
                    case CandleInterval.FiveMinutes:
                    case CandleInterval.TenMinutes:
                    case CandleInterval.QuarterHour:
                    case CandleInterval.HalfHour:
                        return date.ToString("HH:mm");

                    case CandleInterval.Hour:
                    case CandleInterval.TwoHours:
                    case CandleInterval.FourHours:
                    case CandleInterval.Day:
                    case CandleInterval.Week:
                        return date.ToString("dd MMMM");

                    case CandleInterval.Month:
                        return date.ToString("yyyy");

                    default:
                        return "";
                }
            };
            xAxis.AxisChanged += XAxis_AxisChanged;

            yAxis.LabelFormatter = d => $"{d} {activeInstrument.Currency}";

            PlotView.Model = model;

            PlotView.ActualController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            PlotView.ActualController.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);

            DataContext = this;
        }

        private (PlotView plot, LinearAxis x, LinearAxis y) AddOscillatorPlot()
        {
            var plot = new PlotView();

            Grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
            Grid.Children.Add(plot);
            plot.SetValue(Grid.RowProperty, Grid.RowDefinitions.Count - 1);

            plot.Model = new PlotModel
            {
                TextColor = OxyColor.FromArgb(140, 0, 0, 0),
                PlotAreaBorderThickness = new OxyThickness(0, 1, 0, 1),
                PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0),
                LegendPosition = LegendPosition.LeftTop,
                LegendBackground = OxyColor.FromRgb(245, 245, 245),
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
            x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);

            plot.ActualController.UnbindAll();

            plot.Model.Axes.Add(x);
            plot.Model.Axes.Add(y);

            AdjustYExtent(x, y, plot.Model);
            plot.InvalidatePlot();

            oscillatorsPlots.Add((plot, x, y));
            return oscillatorsPlots.Last();
        }

        private async void XAxis_AxisChanged(object sender, AxisChangedEventArgs e)
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

        private int CalculateMinSeriesLength()
        {
            var result = (int)xAxis.ActualMaximum;

            // check series on the main plot
            result = (from series in model.Series
                      where series.GetType() == typeof(LineSeries)
                      select ((LineSeries)series).Points.Count).Concat(new[] { result }).Min();

            // check other series outside of the main plot
            foreach (var series in oscillatorsPlots.SelectMany(plot => plot.plot.Model.Series))
            {
                if (series.GetType() == typeof(LineSeries))
                {
                    if (((LineSeries)series).Points.Count < result)
                        result = ((LineSeries)series).Points.Count;
                }
                else if (series.GetType() == typeof(HistogramSeries))
                {
                    if (((HistogramSeries)series).Items.Count < result)
                        result = ((HistogramSeries)series).Items.Count;
                }
            }
            return result;
        }

        private async Task LoadMoreCandlesAndUpdateSeries()
        {
            try
            {
                var loaded = false;
                var minSeriesLength = CalculateMinSeriesLength();

                while ((xAxis.ActualMaximum - 3 >= loadedCandles || xAxis.ActualMaximum - 3 >= minSeriesLength)
                    && candlesLoadsFailed < 10)
                {
                    await LoadMoreCandles();
                    loaded = true;
                    foreach (var indicator in indicators)
                        indicator.UpdateSeries();

                    minSeriesLength = CalculateMinSeriesLength();
                }

                if (loaded)
                {
                    AdjustYExtent(xAxis, yAxis, model);
                    foreach (var (plot, x, y) in oscillatorsPlots)
                        AdjustYExtent(x, y, plot.Model);
                }

                PlotView.InvalidatePlot();
                foreach (var plot in oscillatorsPlots)
                    plot.plot.InvalidatePlot();
            }
            catch (Exception)
            {
                candlesLoadsFailed++;
            }
        }

        public async void ResetSeries()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            candlesSeries.Items.Clear();
            foreach (var v in lastSignals.Values)
            {
                for (var i = 0; i < v.Length; ++i)
                {
                    v[i] = null;
                }
            }

            loadedCandles = 0;
            candlesLoadsFailed = 0;
            leftCandleDate = rightCandleDate = DateTime.Now;

            foreach (var indicator in indicators) indicator.ResetSeries();

            LoadingCandlesTask = LoadMoreCandlesAndUpdateSeries();
            await LoadingCandlesTask;

            xAxis.Zoom(0, 75);

            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        private async Task LoadMoreCandles()
        {
            if (ActiveInstrument == null || TinkoffInterface.Context == null ||
                candlesLoadsFailed >= 10 ||
                loadedCandles > xAxis.ActualMaximum + 100)
                return;

            var period = GetPeriod(candleInterval);
            var candles = await GetCandles(ActiveInstrument.Figi, leftCandleDate - period,
                leftCandleDate, candleInterval);
            leftCandleDate -= period;
            if (candles.Count == 0)
            {
                candlesLoadsFailed += 1;
                return;
            }

            for (var i = 0; i < candles.Count; ++i)
            {
                var candle = candles[i];
                candlesSeries.Items.Add(new Candle(loadedCandles + i, candle));
            }

            loadedCandles += candles.Count;

            candlesLoadsFailed = 0;
        }

        public async Task UpdateTestingSignals()
        {
            Balance = 10000;
            dealLots = 0;
            dealPrice = 0;
            state = State.Empty;

            buySeries.Points.Clear();
            sellSeries.Points.Clear();
            foreach (var v in lastSignals.Values)
            {
                for (var i = 0; i < v.Length; ++i)
                {
                    v[i] = null;
                }
            }

            await Task.Factory.StartNew(() =>
            {
                for (var i = candlesSeries.Items.Count - 1; i >= 0; --i) UpdateSignals(i);
            });
            PlotView.InvalidatePlot();

            if (state != State.Empty)
                Balance += dealLots * dealPrice;
        }

        public void UpdateRealTimeSignals()
        {
            UpdateSignals(0);
            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        private (double max, double min) CalculateMaxMin(int startIndex, int period)
        {
            double maxPrice = double.MinValue;
            double minPrice = double.MaxValue;
            for (int j = startIndex; j < period + startIndex && j < candlesSeries.Items.Count; ++j)
            {
                var h = candlesSeries.Items[j].High;
                var l = candlesSeries.Items[j].Low;
                if (h > maxPrice)
                    maxPrice = h;
                if (l < minPrice)
                    minPrice = l;
            }
            return (maxPrice, minPrice);
        }

        private void UpdateSignals(int i)
        {
            var candle = candlesSeries.Items[i];
            var resLong = CalculateMaxMin(i, 200);
            var resShort = CalculateMaxMin(i, 50);
            (double max, double min) res = ((resLong.max + resShort.max) / 2, (resLong.min + resShort.min) / 2);
            double step = (res.max - res.min) / 1000;

            double stopLossMultiplier = 300;

            foreach (var signals in lastSignals.Values)
            {
                for (var j = 1; j < signals.Length; ++j)
                    signals[j - 1] = signals[j];
                signals[signals.Length - 1] = null;
            }

            foreach (var indicator in indicators)
            {
                if (!lastSignals.TryGetValue(indicator, out var signals))
                    continue;

                signals[signals.Length - 1] = indicator.GetSignal(i);
            }

            if (state == State.Bought && candle.Close < stopLoss)
            { // stop loss to sell
                sellSeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));

                Balance += candle.Close * dealLots;

                state = State.Empty;
            }
            if (state == State.Sold && candle.Close > stopLoss)
            { // stop loss to buy
                buySeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));

                Balance += candle.Close * dealLots;

                state = State.Empty;
            }

            var signalWeight = CalculateSignalWeight();
            if (signalWeight >= 1 && state != State.Bought)
            { // buy signal
                buySeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));

                if (state != State.Empty)
                {
                    Balance += candle.Close * dealLots;
                    buySeries.Points.Add(new ScatterPoint(i - 0.5, candle.Close, 8));
                }

                dealPrice = candle.Close;
                dealLots = (int)(Balance / dealPrice);
                Balance -= dealLots * dealPrice;

                state = State.Bought;
                stopLoss = candle.Close - step * stopLossMultiplier;
            }
            else if (signalWeight <= -1 && state != State.Sold)
            { // sell signal
                sellSeries.Points.Add(new ScatterPoint(i - 0.5, candle.Close, 8));

                if (state != State.Empty)
                {
                    Balance += candle.Close * dealLots;
                    sellSeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));
                }

                dealPrice = candle.Close;
                dealLots = (int)(Balance / dealPrice);
                Balance -= dealLots * dealPrice;

                state = State.Sold;
                stopLoss = candle.Close + step * stopLossMultiplier;
            }
        }

        private float CalculateSignalWeight()
        {
            float result = 0;
            foreach (var signals in lastSignals)
            {
                var v = signals.Value;
                if (v[v.Length - 1] == null) continue;
                switch (v[v.Length - 1].Value.type)
                {
                    case Indicator.Signal.Type.Buy:
                        result += signals.Key.Weight;
                        break;

                    case Indicator.Signal.Type.Sell:
                        result -= signals.Key.Weight;
                        break;
                }
            }

            foreach (var signals in lastSignals)
            {
                var v = signals.Value;
                bool buyFound = false, sellFound = false;
                for (int i = 0; i < v.Length - 1; ++i)
                {
                    if (!v[i].HasValue)
                        continue;

                    var signal = v[i].Value;
                    switch (signal.type)
                    {
                        case Indicator.Signal.Type.Buy:
                            buyFound = true;
                            break;

                        case Indicator.Signal.Type.Sell:
                            sellFound = true;
                            break;
                    }
                }

                if (buyFound)
                    result += signals.Key.Weight;
                if (sellFound)
                    result -= signals.Key.Weight;
            }

            return result;
        }

        public void AddIndicator(Indicator indicator)
        {
            indicators.Add(indicator);
            lastSignals.Add(indicator, new Indicator.Signal?[3]);

            indicator.AttachToChart(indicator.IsOscillator ?
                AddOscillatorPlot().plot.Model.Series : model.Series);

            indicator.UpdateSeries();
            AdjustYExtent(xAxis, yAxis, model);
            foreach (var (plot, x, y) in oscillatorsPlots)
                AdjustYExtent(x, y, plot.Model);

            PlotView.InvalidatePlot();
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();
        }

        public void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.DetachFromChart();
            indicators = new List<Indicator>();
            lastSignals.Clear();

            if (oscillatorsPlots.Count > 0)
            {
                Grid.Children.RemoveRange(1, Grid.RowDefinitions.Count - 1);
                Grid.RowDefinitions.RemoveRange(1, Grid.RowDefinitions.Count - 1);
                oscillatorsPlots.Clear();
            }

            AdjustYExtent(xAxis, yAxis, model);
            PlotView.InvalidatePlot();
        }

        public async Task LoadNewCandles()
        {
            List<CandlePayload> candles;
            try
            {
                candles = await GetCandles(ActiveInstrument.Figi, rightCandleDate,
                    DateTime.Now, candleInterval);
            }
            catch (Exception)
            {
                return;
            }

            if (candles.Count == 0)
                return;

            rightCandleDate = DateTime.Now;

            var c = candles.Select((candle, i) => new Candle(i, candle)).Cast<HighLowItem>().ToList();

            candlesSeries.Items.ForEach(v => v.X += candles.Count);
            candlesSeries.Items.InsertRange(0, c);
            foreach (var indicator in indicators)
            {
                indicator.OnNewCandlesAdded(candles.Count);
                indicator.UpdateSeries();
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

        private static void AdjustYExtent(Axis x, Axis y, PlotModel m)
        {
            var points = new List<float>();

            foreach (var series in m.Series)
                if (series.GetType() == typeof(CandleStickSeries))
                {
                    points.AddRange(((CandleStickSeries)series).Items
                        .FindAll(p => p.X >= x.ActualMinimum && p.X <= x.ActualMaximum)
                        .ConvertAll(v => (float)v.High));
                    points.AddRange(((CandleStickSeries)series).Items
                        .FindAll(p => p.X >= x.ActualMinimum && p.X <= x.ActualMaximum)
                        .ConvertAll(v => (float)v.Low));
                }
                else if (series.GetType() == typeof(LineSeries))
                {
                    points.AddRange(((LineSeries)series).Points
                        .FindAll(p => p.X >= x.ActualMinimum && p.X <= x.ActualMaximum)
                        .ConvertAll(v => (float)v.Y));
                }
                else if (series.GetType() == typeof(HistogramSeries))
                {
                    points.AddRange(((HistogramSeries)series).Items
                        .FindAll(p => p.RangeStart >= x.ActualMinimum && p.RangeStart <= x.ActualMaximum)
                        .ConvertAll(v => (float)v.Value));
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
            var margin = 0;

            y.IsZoomEnabled = true;
            y.Zoom(min - margin, max + margin);
            y.IsZoomEnabled = false;
        }

        public async Task<List<CandlePayload>> GetCandles(string figi, DateTime from, DateTime to, CandleInterval interval)
        {
            var candles = await TinkoffInterface.Context.MarketCandlesAsync(figi, from, to, interval);

            var result = candles.Candles.ToList();

            result.Reverse();
            return result;
        }

        // ================================
        // =========  Events ==============
        // ================================

        private void MovingAverage_Click(object sender, RoutedEventArgs e)
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

            AddIndicator(new MovingAverage(dialog.Period, calculationMethod, candlesSeries.Items, dialog.Weight));
        }

        private void MACD_Click(object sender, RoutedEventArgs e)
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
                calculationMethod, dialog.ShortPeriod, dialog.LongPeriod, dialog.HistogramPeriod,
                candlesSeries.Items, dialog.Weight));
        }

        private void RemoveIndicators_Click(object sender, RoutedEventArgs e)
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

        public static TimeSpan GetPeriod(CandleInterval interval)
        {
            if (!intervalToMaxPeriod.TryGetValue(interval, out var result))
                throw new KeyNotFoundException();
            return result;
        }

        #endregion IntervalToMaxPeriod
    }
}