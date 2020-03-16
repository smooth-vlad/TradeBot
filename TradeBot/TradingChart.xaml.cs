using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using Tinkoff.Trading.OpenApi.Network;
using Tinkoff.Trading.OpenApi.Models;
using LiveCharts.Defaults;
using System.Windows.Controls;
using System.Diagnostics;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для TradingChart.xaml
    /// </summary>
    public partial class TradingChart : UserControl
    {
        public Context context;
        public MarketInstrument activeStock;

        public int candlesSpan = 50;
        public int maxCandlesSpan = 0;
        public CandleInterval candleInterval = CandleInterval.Minute;

        private List<CandlePayload> candles = new List<CandlePayload>();
        public List<Indicator> indicators = new List<Indicator>();

        private CandleSeries candlesSeries;
        private ScatterSeries buySeries;
        private ScatterSeries sellSeries;

        public SeriesCollection Series { get; private set; } = new SeriesCollection();
        public List<string> Labels { get; private set; } = new List<string>();

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
            { CandleInterval.Hour,          TimeSpan.FromDays(7)},
            { CandleInterval.Day,           TimeSpan.FromDays(364)},
            { CandleInterval.Week,          TimeSpan.FromDays(364*2)},
            { CandleInterval.Month,         TimeSpan.FromDays(364*10)},
        };

        public TradingChart()
        {
            InitializeComponent();

            candlesSeries = new CandleSeries
            {
                ScalesXAt = 0,
                ScalesYAt = 0,
                Values = new ChartValues<OhlcPoint>(),
                StrokeThickness = 3,
                Title = "Candles",

            };
            Series.Add(candlesSeries);

            buySeries = new ScatterSeries
            {
                ScalesXAt = 0,
                ScalesYAt = 0,
                Values = new ChartValues<ObservablePoint>(),
                Title = "Buy",
                Stroke = Brushes.Blue,
                Fill = Brushes.White,
                StrokeThickness = 2,
            };
            sellSeries = new ScatterSeries
            {
                ScalesXAt = 0,
                ScalesYAt = 0,
                Values = new ChartValues<ObservablePoint>(),
                Title = "Sell",
                Stroke = Brushes.Orange,
                Fill = Brushes.White,
                StrokeThickness = 2,
            };

            DataContext = this;
        }

        public static OhlcPoint CandleToOhlc(CandlePayload candlePayload)
        {
            return new OhlcPoint((double)candlePayload.Open, (double)candlePayload.High, (double)candlePayload.Low, (double)candlePayload.Close);
        }

        public async Task<List<CandlePayload>> GetCandles(string figi, int amount, CandleInterval interval, TimeSpan queryOffset)
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

            var newCandles = await GetCandles(activeStock.Figi, maxCandlesSpan, candleInterval, period);
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
            RemoveBuySellSeries();

            var buy = new List<ObservablePoint>();
            var sell = new List<ObservablePoint>();

            await Task.Run(() =>
            {
                for (int i = 0; i < candlesSpan; ++i)
                {
                    var candle = candles[maxCandlesSpan - candlesSpan + i];
                    foreach (var indicator in indicators)
                    {
                        indicator.UpdateState(i);
                        if (indicator.IsBuySignal(i))
                            buy.Add(new ObservablePoint(i, (double)candle.Close));
                        else if (indicator.IsSellSignal(i))
                            sell.Add(new ObservablePoint(i, (double)candle.Close));
                    }
                }
            });

            if (buy.Count > 0)
            {
                buySeries.Values = new ChartValues<ObservablePoint>(buy);
                Series.Add(buySeries);
            }

            if (sell.Count > 0)
            {
                sellSeries.Values = new ChartValues<ObservablePoint>(sell);
                Series.Add(sellSeries);
            }
        }

        public async void AddIndicator(Indicator indicator)
        {
            indicator.candlesSpan = candlesSpan;
            indicator.priceIncrement = activeStock.MinPriceIncrement;
            indicators.Add(indicator);

            await UpdateCandlesList();
            UpdateIndicatorSeries(indicator);
        }

        public void RemoveBuySellSeries()
        {
            if (Series.Contains(buySeries))
                Series.Remove(buySeries);
            if (Series.Contains(sellSeries))
                Series.Remove(sellSeries);
        }

        public void UpdateCandlesSeries()
        {
            var v = new List<OhlcPoint>(candlesSpan);
            for (int i = maxCandlesSpan - candlesSpan; i < maxCandlesSpan; ++i)
                v.Add(CandleToOhlc(candles[i]));
            candlesSeries.Values = new ChartValues<OhlcPoint>(v);
        }

        public void UpdateXLabels()
        {
            Labels.Clear();
            for (int i = 0; i < candlesSpan; ++i)
                Labels.Add(candles[maxCandlesSpan - candlesSpan + i].Time.ToString("dd.MM.yyyy HH:mm"));
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
                indicator.InitializeSeries(Series);
            indicator.UpdateSeries();
        }

        public void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.RemoveSeries(chart.Series);
            indicators = new List<Indicator>();
        }

        public void CandlesValuesChanged()
        {
            RemoveBuySellSeries();
            UpdateCandlesSeries();

            foreach (var indicator in indicators)
                indicator.ResetState();

            UpdateIndicatorsSeries();
            UpdateXLabels();
        }
    }
}
