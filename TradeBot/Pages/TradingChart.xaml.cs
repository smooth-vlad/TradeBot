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
using static TradeBot.Instrument;
using Axis = OxyPlot.Axes.Axis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;
using PlotCommands = OxyPlot.PlotCommands;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для TradingChart.xaml
    /// </summary>
    public partial class TradingChart : UserControl
    {
        private readonly PlotModel model;
        private readonly LinearAxis xAxis;
        private readonly LinearAxis yAxis;

        private CandleStickSeries candlesSeries;
        public List<HighLowItem> Candles => candlesSeries.Items;
        private BuySellSeries buySellSeries;

        private List<Indicator> indicators = new List<Indicator>();

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

        public Task LoadingCandlesTask { get; private set; }

        public delegate void NewCandlesLoadedDelegate(int count);
        public event NewCandlesLoadedDelegate NewCandlesLoaded;

        public delegate void CandlesAddedDelegate();
        public event CandlesAddedDelegate CandlesAdded;

        TradingStrategy tradingStrategy;
        public double? StopLoss { get; protected set; }

        public TradingChart()
        {
            InitializeComponent();

            rightCandleDate = leftCandleDate = DateTime.Now;

            TradingInterface = new TradingInterface(10_000);

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

            model.Series.Add(candlesSeries);

            buySellSeries = new BuySellSeries();
            buySellSeries.AttachToChart(model.Series);

            xAxis.LabelFormatter = d =>
            {
                if (Candles.Count <= (int)d || !(d >= 0)) return "";
                var date = ((Candle)Candles[(int)d]).DateTime;
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
                AdjustYExtent(xAxis, yAxis, model);
            };

            yAxis.LabelFormatter = d => $"{d} {instrument.ActiveInstrument.Currency}";

            PlotView.Model = model;

            PlotView.ActualController.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            PlotView.ActualController.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);

            NewCandlesLoaded += (int count) =>
            {
                buySellSeries.OffsetSeries(count);
            };

            CandlesAdded += () =>
            {
                AdjustYExtent(xAxis, yAxis, model);
                PlotView.InvalidatePlot();
            };

            DataContext = this;
        }

        public void SetStrategy(TradingStrategy ts)
        {
            tradingStrategy = ts;
        }

        public void AddIndicator(Indicator indicator)
        {
            indicators.Add(indicator);

            NewCandlesLoaded += (int count) =>
            {
                indicator.ResetSeries();
                indicator.UpdateSeries();
            };

            if (indicator is OscillatorIndicator oscillator)
            {
                var (plot, x, y) = oscillator.Plot;

                Grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
                Grid.Children.Add(plot);
                plot.SetValue(Grid.RowProperty, Grid.RowDefinitions.Count - 1);

                x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);

                xAxis.AxisChanged += (object sender, AxisChangedEventArgs e) =>
                {
                    x.Zoom(xAxis.ActualMinimum, xAxis.ActualMaximum);
                };

                oscillatorsPlots.Add((plot, x, y));

                oscillator.AttachToChart(plot.Model.Series);
            }
            else
            {
                indicator.AttachToChart(model.Series);
                indicator.SeriesUpdated += () =>
                {
                    AdjustYExtent(xAxis, yAxis, model);
                    PlotView.InvalidatePlot();
                };
            }

            indicator.UpdateSeries();
        }

        public void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.DetachFromChart();
            indicators = new List<Indicator>();

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
                    // TODO: check by title is not good
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

        public static void AdjustYExtent(Axis x, Axis y, PlotModel m)
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
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                candlesLoadsFailed++;
            }
        }

        private async Task LoadMoreCandles()
        {
            if (candlesLoadsFailed >= 10 ||
                LoadedCandles > xAxis.ActualMaximum + 100)
                return;

            var period = IntervalToMaxPeriodConverter.GetMaxPeriod(candleInterval);
            var candles = await instrument.GetCandles(leftCandleDate - period,
                leftCandleDate, candleInterval);
            if (candles == null)
                return;
            leftCandleDate -= period;
            if (candles.Count == 0)
            {
                candlesLoadsFailed += 1;
                return;
            }

            for (var i = 0; i < candles.Count; ++i)
            {
                var candle = candles[i];
                Candles.Add(new Candle(LoadedCandles + i, candle));
            }

            LoadedCandles += candles.Count;

            candlesLoadsFailed = 0;
        }

        public async void RestartSeries()
        {
            buySellSeries.ClearSeries();
            Candles.Clear();

            LoadedCandles = 0;
            candlesLoadsFailed = 0;
            // TO TEST 'REAL TIME TRADING'
            leftCandleDate = rightCandleDate = rightCandleDateAhead = DateTime.Now.AddDays(-120);

            tradingStrategy?.Reset();
            StopLoss = null;
            instrument.ResetState();

            foreach (var indicator in indicators)
                indicator.ResetSeries();

            LoadingCandlesTask = LoadMoreCandlesAndUpdateSeries();
            await LoadingCandlesTask;

            xAxis.Zoom(0, 75);
        }

        public async Task BeginTesting()
        {
            TradingInterface.Reset(10_000);
            buySellSeries.ClearSeries();
            tradingStrategy.Reset();

            await Task.Factory.StartNew(() =>
            {
                for (var i = Candles.Count - 1; i >= 0; --i)
                {
                    UpdateSignals(i, tradingStrategy);
                }
            });
            PlotView.InvalidatePlot();

            if (instrument.State != States.Empty)
            {
                buySellSeries.ClosePosition(0, instrument.DealPrice);
                TradingInterface.ClosePosition(instrument, instrument.DealPrice);
            }
        }

        public void PlaceStopLoss(int openCandleIndex, double openPrice, bool isShort, double percentage)
        {
            int div = 1000;
            var resLong = CalculateMaxMinPrice(openCandleIndex, 200);
            var resShort = CalculateMaxMinPrice(openCandleIndex, 50);
            (double max, double min) res = ((resLong.max + resShort.max) / 2, (resLong.min + resShort.min) / 2);
            double step = (res.max - res.min) / div;
            if (isShort)
                step = -step;

            StopLoss = openPrice - step * (percentage * div);
        }

        public bool HasCrossedStopLoss(double price)
        {
            if (StopLoss == null)
                return false;
            return price < StopLoss && instrument.State == States.Bought
                || price > StopLoss && instrument.State == States.Sold;
        }

        private (double max, double min) CalculateMaxMinPrice(int startIndex, int period)
        {
            double maxPrice = double.MinValue;
            double minPrice = double.MaxValue;
            for (int j = startIndex; j < period + startIndex && j < Candles.Count; ++j)
            {
                var h = Candles[j].High;
                var l = Candles[j].Low;
                if (h > maxPrice)
                    maxPrice = h;
                if (l < minPrice)
                    minPrice = l;
            }
            return (maxPrice, minPrice);
        }

        private void UpdateSignals(int i, TradingStrategy tradingStrategy)
        {
            var candle = Candles[i];

            if (HasCrossedStopLoss(Candles[i].Close))
            {
                buySellSeries.ClosePosition(i, candle.Close);
                TradingInterface.ClosePosition(instrument, candle.Close);
                StopLoss = null;
            }

            var signal = tradingStrategy.GetSignal(i);

            if (signal == TradingStrategy.Signal.Buy
                && instrument.State != States.Bought)
            { // buy signal
                if (instrument.State != States.Empty)
                {
                    buySellSeries.ClosePosition(i, candle.Close);
                    TradingInterface.ClosePosition(instrument, candle.Close);
                    StopLoss = null;
                }
                //else
                {
                    buySellSeries.OpenPosition(i, candle.Close, false);
                    TradingInterface.OpenPosition(instrument, candle.Close, false);
                    PlaceStopLoss(i, candle.Close, false, 0.15);
                }
            }
            else if (signal == TradingStrategy.Signal.Sell
                && instrument.State != States.Sold)
            { // sell signal
                if (instrument.State != States.Empty)
                {
                    buySellSeries.ClosePosition(i, candle.Close);
                    TradingInterface.ClosePosition(instrument, candle.Close);
                    StopLoss = null;
                }
                //else
                {
                    buySellSeries.OpenPosition(i, candle.Close, true);
                    TradingInterface.OpenPosition(instrument, candle.Close, true);
                    PlaceStopLoss(i, candle.Close, true, 0.15);
                }
            }
        }

        public async Task LoadNewCandles()
        {
            List<CandlePayload> candles = await instrument.GetCandles(rightCandleDate,
                    rightCandleDateAhead, candleInterval);

            if (candles == null || candles.Count == 0)
                return;

            rightCandleDate = rightCandleDateAhead;

            var c = candles.Select((candle, i) => new Candle(i, candle)).Cast<HighLowItem>().ToList();
            Candles.ForEach(v => v.X += candles.Count);
            Candles.InsertRange(0, c);

            LoadedCandles += candles.Count;
            NewCandlesLoaded?.Invoke(candles.Count);
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
            AddIndicator(new MovingAverage(dialog.Period, calculationMethod, Candles));
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
                Candles));
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