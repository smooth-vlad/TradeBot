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

        public int candlesSpan = 50;
        public int maxCandlesSpan { get; private set; }
        public CandleInterval candleInterval = CandleInterval.Minute;

        private List<CandlePayload> candles = new List<CandlePayload>();
        public List<Indicator> indicators { get; private set; } = new List<Indicator>();

        private CandleStickSeries candlesSeries;
        private ScatterSeries buySeries;
        private ScatterSeries sellSeries;

        private bool isAddingCandles = false;

        public PlotModel model = new PlotModel();

        public static readonly Dictionary<CandleInterval, TimeSpan> intervalToMaxPeriod
            = new Dictionary<CandleInterval, TimeSpan>
        {
            { CandleInterval.Minute,        TimeSpan.FromDays(1)},
            { CandleInterval.TwoMinutes,    TimeSpan.FromDays(1)},
            { CandleInterval.ThreeMinutes,  TimeSpan.FromDays(1)},
            { CandleInterval.FiveMinutes,   TimeSpan.FromDays(1)},
            { CandleInterval.TenMinutes,    TimeSpan.FromDays(1)},
            { CandleInterval.QuarterHour,   TimeSpan.FromDays(1)},
            { CandleInterval.HalfHour,      TimeSpan.FromDays(1)},
            { CandleInterval.Hour,          TimeSpan.FromDays(7).Add(TimeSpan.FromHours(-1))},
            { CandleInterval.Day,           TimeSpan.FromDays(364)},
            { CandleInterval.Week,          TimeSpan.FromDays(364*2)},
            { CandleInterval.Month,         TimeSpan.FromDays(364*10)},
        };

        public TradingChart()
        {
            InitializeComponent();

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, IsPanEnabled = false, IsZoomEnabled = false });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom });

            candlesSeries = new CandleStickSeries
            {
                Title = "Candles",
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

            candlesSeries.DecreasingColor = OxyColor.FromRgb(230, 63, 60);
            candlesSeries.IncreasingColor = OxyColor.FromRgb(45, 128, 32);
            candlesSeries.StrokeThickness = 1;

            model.Series.Add(candlesSeries);
            model.Series.Add(buySeries);
            model.Series.Add(sellSeries);

            model.Axes[0].MajorGridlineThickness = 2.5;
            model.Axes[0].MinorGridlineThickness = 0;
            model.Axes[0].MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0);
            model.Axes[0].MajorGridlineStyle = LineStyle.Solid;
            model.Axes[0].TicklineColor = OxyColor.FromArgb(10, 0, 0, 0);
            model.Axes[0].TickStyle = TickStyle.Outside;

            Func<double, string> formatLabel = delegate(double d)
            {
                if (candles.Count > d && d >= 0)
                {
                    return candles[(int)d].Time.ToString("dd.MM.yyyy-HH:mm");
                }
                else
                    return "";
            };
            model.Axes[1].LabelFormatter = formatLabel;
            model.Axes[1].MajorGridlineThickness = 0;
            model.Axes[1].MinorGridlineThickness = 0;
            model.Axes[1].MajorGridlineColor = OxyColor.FromArgb(10, 0, 0, 0);
            model.Axes[1].MajorGridlineStyle = LineStyle.Solid;
            model.Axes[1].TicklineColor = OxyColor.FromArgb(10, 0, 0, 0);
            model.Axes[1].TickStyle = TickStyle.Outside;
            model.Axes[1].AxisChanged += (sender, e) => AdjustYExtent(candlesSeries, (LinearAxis)model.Axes[1], (LinearAxis)model.Axes[0]);
            model.Axes[1].Zoom(candlesSpan - 75, candlesSpan);

            model.TextColor = OxyColor.FromArgb(140, 0, 0, 0);
            model.PlotAreaBorderColor = OxyColor.FromArgb(10, 0, 0, 0);

            plotView.Model = model;

            DataContext = this;
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

        public async Task UpdateCandlesList()
        {
            maxCandlesSpan = CalculateMaxCandlesSpan();
            TimeSpan period;
            if (!intervalToMaxPeriod.TryGetValue(candleInterval, out period))
                throw new KeyNotFoundException();

            var newCandles = await GetFixedAmountOfCandles(activeStock.Figi, maxCandlesSpan, candleInterval, period);
            candles = newCandles;
        }

        public int CalculateMaxCandlesSpan()
        {
            var result = candlesSpan;
            foreach (var indicator in indicators)
            {
                if (indicator.CandlesNeeded > result)
                    result = indicator.CandlesNeeded;
            }
            return result;
        }

        public async Task Simulate()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            await Task.Run(() =>
            {
                for (int i = 0; i < candlesSpan; ++i)
                {
                    var candle = candles[maxCandlesSpan - candlesSpan + i];
                    foreach (var indicator in indicators)
                    {
                        indicator.UpdateState(i);
                        if (indicator.IsBuySignal(i))
                            buySeries.Points.Add(new ScatterPoint(i, (double)candle.Close));
                        else if (indicator.IsSellSignal(i))
                            sellSeries.Points.Add(new ScatterPoint(i, (double)candle.Close));
                    }
                }
            });
            plotView.InvalidatePlot();
        }

        public async void AddIndicator(Indicator indicator)
        {
            indicator.candlesSpan = candlesSpan;
            indicator.priceIncrement = activeStock.MinPriceIncrement;
            indicators.Add(indicator);

            await UpdateCandlesList();
            UpdateIndicatorSeries(indicator);
        }

        public void UpdateCandlesSeries()
        {
            candlesSeries.Items.Clear();
            for (int i = maxCandlesSpan - candlesSpan; i < maxCandlesSpan; ++i)
                candlesSeries.Items.Add(CandleToHighLowItem(i, candles[i]));
            model.Axes[1].AbsoluteMaximum = candlesSpan + 10;
            model.Axes[1].Zoom(candlesSpan - 75, candlesSpan);
            plotView.InvalidatePlot();
        }

        public void UpdateIndicatorsSeries()
        {
            foreach (var indicator in indicators)
                UpdateIndicatorSeries(indicator);
        }

        public void UpdateIndicatorSeries(Indicator indicator)
        {
            indicator.Candles = candles;

            if (!indicator.AreGraphsInitialized)
                indicator.InitializeSeries(model.Series);
            indicator.UpdateSeries();
            plotView.InvalidatePlot();
        }

        public void RemoveIndicators()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();
            foreach (var indicator in indicators)
                indicator.RemoveSeries(model.Series);
            indicators = new List<Indicator>();
            plotView.InvalidatePlot();
        }

        public void OnCandlesValuesChanged()
        {
            buySeries.Points.Clear();
            sellSeries.Points.Clear();

            UpdateCandlesSeries();

            foreach (var indicator in indicators)
                indicator.ResetState();

            UpdateIndicatorsSeries();

            CandlesChange?.Invoke();
        }

        private void AdjustYExtent(CandleStickSeries lserie, LinearAxis xaxis, LinearAxis yaxis)
        {
            if (xaxis != null && yaxis != null && lserie.Items.Count != 0 && !isAddingCandles)
            {
                double istart = xaxis.ActualMinimum;
                double iend = xaxis.ActualMaximum;

                var ptlist = lserie.Items.FindAll(p => p.X >= istart && p.X <= iend);
                if (ptlist.Count == 0)
                    return;

                double ymin = double.MaxValue;
                double ymax = double.MinValue;
                for (int i = 0; i < ptlist.Count; i++)
                {
                    ymin = Math.Min(ymin, ptlist[i].Low);
                    ymax = Math.Max(ymax, ptlist[i].High);
                }

                var extent = ymax - ymin;
                var margin = extent * 0.1;

                yaxis.IsZoomEnabled = true;
                yaxis.Zoom(ymin - margin, ymax + margin);
                yaxis.IsZoomEnabled = false;
            }
        }
    }
}
