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
    /// Логика взаимодействия для SimulationTrading.xaml
    /// </summary>
    public partial class SimulationTrading : UserControl
    {
        private Context context;
        private MarketInstrument activeStock;

        private List<Indicator> indicators = new List<Indicator>();

        private int candlesSpan = 50;
        private int maxCandlesSpan = 0;
        private CandleInterval candleInterval = CandleInterval.Minute;

        private List<CandlePayload> candles = new List<CandlePayload>();

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

        public SimulationTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.activeStock = activeStock;
            this.context = context;

            chartNameTextBlock.Text = activeStock.Name + " (Simulation)";

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

            intervalComboBox.ItemsSource = intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            DataContext = this;
        }


        public static OhlcPoint CandleToOhlc(CandlePayload candlePayload)
        {
            return new OhlcPoint((double)candlePayload.Open, (double)candlePayload.High, (double)candlePayload.Low, (double)candlePayload.Close);
        }

        private async Task<List<CandlePayload>> GetCandles(string figi, int amount, CandleInterval interval, TimeSpan queryOffset)
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

        private async Task UpdateCandlesList()
        {
            maxCandlesSpan = CalculateMaxCandlesSpan();
            TimeSpan period;
            if (!intervalToMaxPeriod.TryGetValue(candleInterval, out period))
                throw new KeyNotFoundException();
             
            var newCandles = await GetCandles(activeStock.Figi, maxCandlesSpan, candleInterval, period);
            candles = newCandles;
        }

        private int CalculateMaxCandlesSpan()
        {
            var result = candlesSpan;
            foreach (var indicator in indicators)
            {
                if (indicator.CandlesNeeded > result)
                    result = indicator.CandlesNeeded;
            }
            return result;
        }

        private void SetEverythingEnabled(bool value)
        {
            removeIndicatorsButton.IsEnabled = value;
            simulateButton.IsEnabled = value;
            intervalComboBox.IsEnabled = value;
            periodTextBox.IsEnabled = value;
        }

        private async Task Simulate()
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

        private void RemoveBuySellSeries()
        {
            if (Series.Contains(buySeries))
                Series.Remove(buySeries);
            if (Series.Contains(sellSeries))
                Series.Remove(sellSeries);
        }

        private void UpdateCandlesSeries()
        {
            var v = new List<OhlcPoint>(candlesSpan);
            for (int i = maxCandlesSpan - candlesSpan; i < maxCandlesSpan; ++i)
                v.Add(CandleToOhlc(candles[i]));
            candlesSeries.Values = new ChartValues<OhlcPoint>(v);
        }

        private void UpdateXLabels()
        {
            Labels.Clear();
            for (int i = 0; i < candlesSpan; ++i)
                Labels.Add(candles[maxCandlesSpan - candlesSpan + i].Time.ToString("dd.MM.yyyy HH:mm"));
        }

        private void UpdateIndicatorsSeries()
        {
            foreach (var indicator in indicators)
                UpdateIndicatorSeries(indicator);
        }

        private void UpdateIndicatorSeries(Indicator indicator)
        {
            indicator.Candles = candles;

            if (!indicator.AreGraphsInitialized)
                indicator.InitializeSeries(Series);
            indicator.UpdateSeries();
        }

        private void RemoveIndicators()
        {
            foreach (var indicator in indicators)
                indicator.RemoveSeries(chart.Series);
            indicators = new List<Indicator>();
        }

        private void CandlesValuesChanged()
        {
            RemoveBuySellSeries();
            UpdateCandlesSeries();

            foreach (var indicator in indicators)
                indicator.ResetState();

            UpdateIndicatorsSeries();
            UpdateXLabels();
        }

        // ==================================================
        // events
        // ==================================================

        private void removeIndicatorsButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveIndicators();
        }

        private async void intervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CandleInterval interval = CandleInterval.Minute;
            bool intervalFound = false;
            var selectedInterval = intervalComboBox.SelectedItem.ToString();
            foreach (var k in intervalToMaxPeriod.Keys)
            {
                if (k.ToString() == selectedInterval)
                {
                    interval = k;
                    intervalFound = true;
                    break;
                }
            }
            if (!intervalFound)
                return;

            candleInterval = interval;

            await UpdateCandlesList();
            CandlesValuesChanged();
        }

        private async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await Simulate();
            SetEverythingEnabled(true);
        }

        private async void periodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int period;
            if (!int.TryParse(periodTextBox.Text.Trim(), out period))
            {
                MessageBox.Show("Not a number in 'Period'");
                return;
            }

            if (period < 10 || period > 300)
            {
                MessageBox.Show("'Period' should be >= 10 and <= 300");
                return;
            }

            if (candlesSpan == period)
                return;

            candlesSpan = period;

            foreach (var indicator in indicators)
                indicator.candlesSpan = candlesSpan;

            await UpdateCandlesList();
            CandlesValuesChanged();
        }

        private void periodTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                periodTextBox_LostFocus(this, new RoutedEventArgs());
        }
    }
}
