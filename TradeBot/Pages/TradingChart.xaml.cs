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
        private readonly CandleStickSeries candlesSeriesSmall;

        private List<Indicator> indicators = new List<Indicator>();
        //private readonly Dictionary<Indicator, Indicator.Signal?[]> lastSignalsForIndicator
        //    = new Dictionary<Indicator, Indicator.Signal?[]>();

        private readonly PlotModel model;
        private readonly LinearAxis xAxis;
        private readonly LinearAxis yAxis;

        private List<(PlotView plot, LinearAxis x, LinearAxis y)> oscillatorsPlots
            = new List<(PlotView plot, LinearAxis x, LinearAxis y)>();

        private Instrument _instrument;
        public Instrument instrument
        {
            get => _instrument;
            set
            {
                _instrument = value;
                candlesSeries.Title = value.ActiveInstrument.Name;
            }
        }

        public TradingInterface TradingInterface { get; private set; }

        public CandleInterval candleInterval = CandleInterval.Hour;

        private int candlesLoadsFailed;
        private int loadedCandles;
        private int LoadedCandles
        {
            get => loadedCandles;
            set
            {
                loadedCandles = value;
                CandlesAdded?.Invoke();
            }
        }

        private DateTime rightCandleDate; // newest
        private DateTime leftCandleDate; // oldest
        public DateTime rightCandleDateAhead; // TO TEST 'REAL TIME TRADING'

        private BuySellSeries buySellSeries;

        public Task LoadingCandlesTask { get; private set; }

        public delegate void NewCandlesLoadedDelegate(int count);
        public event NewCandlesLoadedDelegate NewCandlesLoaded;

        public delegate void CandlesAddedDelegate();
        public event CandlesAddedDelegate CandlesAdded;

        public TradingChart()
        {
            InitializeComponent();

            rightCandleDate = leftCandleDate = DateTime.Now;

            TradingInterface = new TradingInterface(1_000);

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

            candlesSeriesSmall = new CandleStickSeries
            {
                Title = "Instrument",
                DecreasingColor = OxyColor.FromRgb(214, 107, 107),
                IncreasingColor = OxyColor.FromRgb(121, 229, 112),
                StrokeThickness = 1,
                TrackerFormatString = "Time: {DateTime:dd.MM.yyyy HH:mm}" + Environment.NewLine
                      + "Price: {4}",
            };

            model.Series.Add(candlesSeriesSmall);

            buySellSeries = new BuySellSeries();
            buySellSeries.AttachToChart(model.Series);

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
            xAxis.AxisChanged += async (object sender, AxisChangedEventArgs e) =>
            {
                if (LoadingCandlesTask != null && LoadingCandlesTask.IsCompleted)
                {
                    LoadingCandlesTask = LoadMoreCandlesAndUpdateSeries();
                    await LoadingCandlesTask;
                }
            };

            xAxis.AxisChanged += (object sender, AxisChangedEventArgs e) =>
            {
                UpdateCandlesSeriesSmall();
            };

            xAxis.AxisChanged += (object sender, AxisChangedEventArgs e) =>
            {
                AdjustYExtent(xAxis, yAxis, model);
            };

            yAxis.LabelFormatter = d => $"{d} {instrument.ActiveInstrument.Currency}";

            PlotView.Model = model;

            PlotView.ActualController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            PlotView.ActualController.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);

            NewCandlesLoaded += (int count) =>
            {
                UpdateCandlesSeriesSmall();
                buySellSeries.OffsetSeries(count);
            };

            CandlesAdded += () =>
            {
                UpdateCandlesSeriesSmall();
                AdjustYExtent(xAxis, yAxis, model);
                PlotView.InvalidatePlot();
            };

            DataContext = this;
        }

        private void UpdateCandlesSeriesSmall()
        {
            var l = xAxis.ActualMinimum;
            if (l < 0)
                l = 0;
            var r = xAxis.ActualMaximum;
            Console.WriteLine($"{l}, {r},,, {candlesSeries.Items.Count}");

            if (candlesSeries.Items.Count < (int)l)
                return;
            if (candlesSeries.Items.Count < (int)r)
                r = candlesSeries.Items.Count;
            var candlesSmall = candlesSeries.Items.GetRange((int)l, (int)r - (int)l);
            candlesSeriesSmall.Items.Clear();
            candlesSeriesSmall.Items.AddRange(candlesSmall);
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
            x.AxisChanged += (object sender, AxisChangedEventArgs e) =>
            {
                AdjustYExtent(x, y, plot.Model);
                plot.InvalidatePlot();
            };
            CandlesAdded += () =>
            {
                AdjustYExtent(x, y, plot.Model);
                plot.InvalidatePlot();
            };

            plot.ActualController.UnbindAll();

            plot.Model.Axes.Add(x);
            plot.Model.Axes.Add(y);

            x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);

            xAxis.AxisChanged += (object sender, AxisChangedEventArgs e) =>
            {
                x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);
            };

            oscillatorsPlots.Add((plot, x, y));
            return oscillatorsPlots.Last();
        }

        public void AddIndicator(Indicator indicator)
        {
            indicators.Add(indicator);

            //lastSignalsForIndicator.Add(indicator, new Indicator.Signal?[3]);

            NewCandlesLoaded += (int count) =>
            {
                indicator.ResetSeries();
                indicator.UpdateSeries();
            };

            if (indicator.IsOscillator)
            {
                var (plot, x, y) = AddOscillatorPlot();
                indicator.AttachToChart(plot.Model.Series);
                indicator.SeriesUpdated += () => AdjustYExtent(x, y, plot.Model);
            }
            else
            {
                indicator.AttachToChart(model.Series);
                indicator.SeriesUpdated += () => AdjustYExtent(xAxis, yAxis, model);
            }

            indicator.UpdateSeries();

            foreach (var (plot, x, y) in oscillatorsPlots)
                AdjustYExtent(x, y, plot.Model);
            foreach (var plot in oscillatorsPlots)
                plot.plot.InvalidatePlot();

            AdjustYExtent(xAxis, yAxis, model);

            PlotView.InvalidatePlot();
        }

        public void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.DetachFromChart();
            indicators = new List<Indicator>();
            //lastSignalsForIndicator.Clear();

            if (oscillatorsPlots.Count > 0)
            {
                Grid.Children.RemoveRange(1, Grid.RowDefinitions.Count - 1);
                Grid.RowDefinitions.RemoveRange(1, Grid.RowDefinitions.Count - 1);
                oscillatorsPlots.Clear();
            }

            AdjustYExtent(xAxis, yAxis, model);
            PlotView.InvalidatePlot();
        }

        public void RemoveMarkers()
        {
            buySellSeries.ClearSeries();
            AdjustYExtent(xAxis, yAxis, model);
            PlotView.InvalidatePlot();
        }

        private int CalculateMinSeriesLength()
        {
            var result = (int)xAxis.ActualMaximum;
            IEnumerable<int> enumerable()
            {
                foreach (var series in model.Series)
                {
                    if (series.GetType() == typeof(LineSeries) && ((LineSeries)series).Title != "Operations")
                    {
                        yield return ((LineSeries)series).Points.Count;
                    }
                }
            }

            // check series on the main plot

            result = enumerable().Concat(new[] { result }).Min();

            // check other series outside of the main plot
            foreach (var series in oscillatorsPlots.SelectMany(plot => plot.plot.Model.Series))
            {
                int count = series switch
                {
                    LineSeries ls => ls.Points.Count,
                    HistogramSeries hs => hs.Items.Count,
                    _ => int.MaxValue,
                };
                if (count < result) result = count;
            }
            return result;
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

        private async Task LoadMoreCandlesAndUpdateSeries()
        {
            try
            {
                var minSeriesLength = CalculateMinSeriesLength();

                while ((xAxis.ActualMaximum - 3 >= LoadedCandles || xAxis.ActualMaximum - 3 >= minSeriesLength)
                    && candlesLoadsFailed < 10)
                {
                    await LoadMoreCandles();
                    foreach (var indicator in indicators)
                        indicator.UpdateSeries();

                    minSeriesLength = CalculateMinSeriesLength();
                }
            }
            catch (Exception)
            {
                candlesLoadsFailed++;
            }
        }

        private async Task LoadMoreCandles()
        {
            if (candlesLoadsFailed >= 10 ||
                LoadedCandles > xAxis.ActualMaximum + 100)
                return;

            var period = Instrument.GetPeriod(candleInterval);
            var candles = await instrument.GetCandles(leftCandleDate - period,
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
                candlesSeries.Items.Add(new Candle(LoadedCandles + i, candle));
            }

            LoadedCandles += candles.Count;

            candlesLoadsFailed = 0;
        }

        //private void ClearLastSignalsForEachIndicator()
        //{
        //    foreach (var v in lastSignalsForIndicator.Values)
        //        for (var i = 0; i < v.Length; ++i)
        //            v[i] = null;
        //}

        public async void RestartSeries()
        {
            buySellSeries.ClearSeries();
            candlesSeries.Items.Clear();
            //ClearLastSignalsForEachIndicator();

            LoadedCandles = 0;
            candlesLoadsFailed = 0;
            // TO TEST 'REAL TIME TRADING'
            leftCandleDate = rightCandleDate = rightCandleDateAhead = DateTime.Now.AddDays(-120);

            foreach (var indicator in indicators)
                indicator.ResetSeries();

            LoadingCandlesTask = LoadMoreCandlesAndUpdateSeries();
            await LoadingCandlesTask;

            xAxis.Zoom(0, 75);
        }

        public async Task BeginTesting()
        {
            //TradingInterface = new TradingInterface(1_000);
            TradingInterface.ResetState(1_000);
            buySellSeries.ClearSeries();

            ITradingStrategy tradingStrategy = new MaTradingStrategy();

            //buySeries.Points.Clear();
            //sellSeries.Points.Clear();
            //ClearLastSignalsForEachIndicator();

            await Task.Factory.StartNew(() =>
            {
                for (var i = candlesSeries.Items.Count - 1; i >= 0; --i)
                {
                    UpdateSignals(i, tradingStrategy);

                    //UpdateSignals(i);
                }
            });
            PlotView.InvalidatePlot();

            if (TradingInterface.State != TradingInterface.States.Empty)
                TradingInterface.ClosePosition(TradingInterface.DealPrice);
        }

        private (double max, double min) CalculateMaxMinPrice(int startIndex, int period)
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

        private void UpdateSignals(int i, ITradingStrategy tradingStrategy)
        {
            var signal = tradingStrategy.GetSignal();
            var candle = candlesSeries.Items[i];

            if (signal != null && signal.Value.type == ITradingStrategy.Signal.Type.Buy
                && TradingInterface.State != TradingInterface.States.Bought)
            { // buy signal
                if (TradingInterface.State != TradingInterface.States.Empty)
                {
                    TradingInterface.ClosePosition(candle.Close);
                    buySellSeries.ClosePosition(i, candle.Close);
                }
                else
                {
                    TradingInterface.OpenPosition(candle.Close, false);
                    buySellSeries.OpenPosition(i, candle.Close, false);
                }
            }
            else if (signal != null && signal.Value.type == ITradingStrategy.Signal.Type.Sell
                && TradingInterface.State != TradingInterface.States.Sold)
            { // sell signal
                if (TradingInterface.State != TradingInterface.States.Empty)
                {
                    TradingInterface.ClosePosition(candle.Close);
                    buySellSeries.ClosePosition(i, candle.Close);
                }
                else
                {
                    TradingInterface.OpenPosition(candle.Close, true);
                    buySellSeries.OpenPosition(i, candle.Close, true);
                }
            }

            //var candle = candlesSeries.Items[i];
            //var resLong = CalculateMaxMinPrice(i, 200);
            //var resShort = CalculateMaxMinPrice(i, 50);
            //(double max, double min) res = ((resLong.max + resShort.max) / 2, (resLong.min + resShort.min) / 2);
            //double step = (res.max - res.min) / 1000;

            //double stopLossMultiplier = 300;

            //OffsetLastSignalsBy1ForEachIndicator();

            //foreach (var indicator in indicators)
            //{
            //    if (!lastSignalsForIndicator.TryGetValue(indicator, out var signals))
            //        continue;

            //    signals[signals.Length - 1] = indicator.GetSignal(i);
            //}

            //if (TradingInterface.State == TradingInterface.States.Bought && candle.Close < TradingInterface.StopLoss)
            //{ // stop loss to sell
            //    sellSeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));
            //    TradingInterface.Sell(candle.Close);
            //}
            //if (TradingInterface.State == TradingInterface.States.Sold && candle.Close > TradingInterface.StopLoss)
            //{ // stop loss to buy
            //    buySeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));
            //    TradingInterface.Sell(candle.Close);
            //}

            //var signalWeight = CalculateSignalWeight();
            //if (signalWeight >= 1 && TradingInterface.State != TradingInterface.States.Bought)
            //{ // buy signal
            //    buySeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));

            //    if (TradingInterface.State != TradingInterface.States.Empty)
            //    {
            //        buySeries.Points.Add(new ScatterPoint(i - 0.5, candle.Close, 8));
            //        TradingInterface.Sell(candle.Close);
            //    }

            //    TradingInterface.Buy(candle.Close, false);
            //    TradingInterface.StopLoss = candle.Close - step * stopLossMultiplier;
            //}
            //else if (signalWeight <= -1 && TradingInterface.State != TradingInterface.States.Sold)
            //{ // sell signal
            //    sellSeries.Points.Add(new ScatterPoint(i - 0.5, candle.Close, 8));

            //    if (TradingInterface.State != TradingInterface.States.Empty)
            //    {
            //        sellSeries.Points.Add(new ScatterPoint(i + 0.5, candle.Close, 8));
            //        TradingInterface.Sell(candle.Close);
            //    }

            //    TradingInterface.Buy(candle.Close, true);
            //    TradingInterface.StopLoss = candle.Close + step * stopLossMultiplier;
            //}
        }

        //private void OffsetLastSignalsBy1ForEachIndicator()
        //{
        //    foreach (var signals in lastSignalsForIndicator.Values)
        //    {
        //        for (var j = 1; j < signals.Length; ++j)
        //            signals[j - 1] = signals[j];
        //        signals[signals.Length - 1] = null;
        //    }
        //}

        private float CalculateSignalWeight()
        {
            //float result = 0;
            //foreach (var signals in lastSignalsForIndicator)
            //{
            //    var v = signals.Value;
            //    if (v[v.Length - 1] == null) continue;
            //    switch (v[v.Length - 1].Value.type)
            //    {
            //        case Indicator.Signal.Type.Buy:
            //            result += signals.Key.Weight;
            //            break;

            //        case Indicator.Signal.Type.Sell:
            //            result -= signals.Key.Weight;
            //            break;
            //    }
            //}

            //foreach (var signals in lastSignalsForIndicator)
            //{
            //    var v = signals.Value;
            //    bool buyFound = false, sellFound = false;
            //    for (int i = 0; i < v.Length - 1; ++i)
            //    {
            //        if (!v[i].HasValue)
            //            continue;

            //        var signal = v[i].Value;
            //        switch (signal.type)
            //        {
            //            case Indicator.Signal.Type.Buy:
            //                buyFound = true;
            //                break;

            //            case Indicator.Signal.Type.Sell:
            //                sellFound = true;
            //                break;
            //        }
            //    }

            //    if (buyFound)
            //        result += signals.Key.Weight;
            //    if (sellFound)
            //        result -= signals.Key.Weight;
            //}

            //return result;
            return 1;
        }

        public async Task LoadNewCandles()
        {
            List<CandlePayload> candles;
            candles = await instrument.GetCandles(rightCandleDate,
                    rightCandleDateAhead, candleInterval);

            if (candles.Count == 0)
                return;

            rightCandleDate = rightCandleDateAhead;

            var c = candles.Select((candle, i) => new Candle(i, candle)).Cast<HighLowItem>().ToList();

            candlesSeries.Items.ForEach(v => v.X += candles.Count);
            candlesSeries.Items.InsertRange(0, c);

            LoadedCandles += candles.Count;

            NewCandlesLoaded?.Invoke(candles.Count);

            // TODO: extract from this method
            ITradingStrategy tradingStrategy = new MaTradingStrategy();
            UpdateSignals(0, tradingStrategy);
        }

        // ================================
        // =========  Events ==============
        // ================================

        private void MovingAverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MovingAverageDialog();
            if (dialog.ShowDialog() != true) return;
            IMaCalculation calculationMethod = dialog.Type switch
            {
                MovingAverageDialog.CalculationMethod.Simple => new SimpleMaCalculation(),
                MovingAverageDialog.CalculationMethod.Exponential => new ExponentialMaCalculation(),
                _ => new SimpleMaCalculation(),
            };
            AddIndicator(new MovingAverage(dialog.Period, calculationMethod, candlesSeries.Items));
        }

        private void MACD_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MacdDialog();
            if (dialog.ShowDialog() != true) return;
            IMaCalculation calculationMethod = dialog.Type switch
            {
                MacdDialog.CalculationMethod.Simple => new SimpleMaCalculation(),
                MacdDialog.CalculationMethod.Exponential => new ExponentialMaCalculation(),
                _ => new SimpleMaCalculation(),
            };
            AddIndicator(new Macd(
                calculationMethod, dialog.ShortPeriod, dialog.LongPeriod, dialog.HistogramPeriod,
                candlesSeries.Items));
        }

        private void RemoveIndicators_Click(object sender, RoutedEventArgs e)
        {
            RemoveIndicators();
        }

        private void RemoveMarkersButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveMarkers();
        }
    }
}