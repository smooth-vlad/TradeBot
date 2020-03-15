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

        private List<CandlePayload> candles;
        private ChartValues<ObservablePoint> buySeries = new ChartValues<ObservablePoint>();
        private ChartValues<ObservablePoint> sellSeries = new ChartValues<ObservablePoint>();

        private int bindedBuySeries = -1;
        private int bindedSellSeries = -1;

        public SeriesCollection CandlesSeries { get; set; }
        public List<string> Labels { get; set; }

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

            this.activeStock = activeStock;
            this.context = context;

            chartNameTextBlock.Text = activeStock.Name + " (Simulation)";

            intervalComboBox.ItemsSource = intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            CandlesSeries = new SeriesCollection();
            Labels = new List<string>();

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
            if (activeStock == null)
                return;

            maxCandlesSpan = RecalculateMaxCandlesSpan();
            TimeSpan period;
            if (!intervalToMaxPeriod.TryGetValue(candleInterval, out period))
                throw new KeyNotFoundException();

            var newCandles = await GetCandles(activeStock.Figi, maxCandlesSpan, candleInterval, period);
            candles = newCandles;
        }

        private int RecalculateMaxCandlesSpan()
        {
            var result = candlesSpan;
            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = indicators[i];
                if (indicator.candlesNeeded > result)
                    result = indicator.candlesNeeded;
            }
            return result;
        }

        private void SetEverythingEnabled(bool value)
        {
            resetIndicatorsButton.IsEnabled = value;
            simulateButton.IsEnabled = value;
            intervalComboBox.IsEnabled = value;
            periodTextBox.IsEnabled = value;
        }

        private async Task Simulate()
        {
            await Task.Run(() =>
            {
                buySeries.Clear();
                sellSeries.Clear();
                for (int i = 0; i < candlesSpan; ++i)
                {
                    foreach (var indicator in indicators)
                    {
                        indicator.UpdateState(i);
                        if (indicator.IsBuySignal(i))
                        {
                            var candle = candles[maxCandlesSpan - candlesSpan + i];
                            buySeries.Add(new ObservablePoint(i, (double)candle.Close));
                        }
                        else if (indicator.IsSellSignal(i))
                        {
                            var candle = candles[maxCandlesSpan - candlesSpan + i];
                            sellSeries.Add(new ObservablePoint(i, (double)candle.Close));
                        }
                    }
                }
            });
            if (buySeries.Count > 0)
            {
                if (bindedBuySeries == -1)
                {
                    CandlesSeries.Add(new ScatterSeries
                    {
                        ScalesXAt = 0,
                        ScalesYAt = 0,
                        Values = buySeries,
                        Title = "Buy",
                        Stroke = Brushes.Blue,
                        Fill = Brushes.White,
                        StrokeThickness = 2,
                    });
                    bindedBuySeries = CandlesSeries.Count - 1;
                }
                else
                    CandlesSeries[bindedBuySeries].Values = buySeries;
            }

            if (sellSeries.Count > 0)
            {
                if (bindedSellSeries == -1)
                {
                    CandlesSeries.Add(new ScatterSeries
                    {
                        ScalesXAt = 0,
                        ScalesYAt = 0,
                        Values = sellSeries,
                        Title = "Sell",
                        Stroke = Brushes.Yellow,
                        Fill = Brushes.White,
                        StrokeThickness = 2,
                    });
                    bindedSellSeries = CandlesSeries.Count - 1;
                }
                else
                    CandlesSeries[bindedSellSeries].Values = sellSeries;
            }
        }

        public async void AddIndicator(Indicator indicator)
        {
            if (activeStock == null)
            {
                MessageBox.Show("Pick a stock first");
                return;
            }

            indicator.candlesSpan = candlesSpan;
            indicator.priceIncrement = activeStock.MinPriceIncrement;
            indicators.Add(indicator);

            await UpdateCandlesList();
            CandlesValuesChanged();
        }

        // ==================================================
        // events
        // ==================================================

        private void CandlesValuesChanged()
        {
            if (activeStock == null)
                return;

            if (bindedBuySeries != -1)
                CandlesSeries[bindedBuySeries].Values = null;
            if (bindedSellSeries != -1)
                CandlesSeries[bindedSellSeries].Values = null;

            var v = new List<OhlcPoint>(candlesSpan);
            for (int i = maxCandlesSpan - candlesSpan; i < maxCandlesSpan; ++i)
                v.Add(CandleToOhlc(candles[i]));
            var v2 = new ChartValues<OhlcPoint>(v);

            if (CandlesSeries.Count == 0)
            {
                CandlesSeries.Add(new CandleSeries
                {
                    ScalesXAt = 0,
                    ScalesYAt = 0,
                    Values = v2,
                    StrokeThickness = 3,
                    Title = "Candles",
                });
            }
            else
                CandlesSeries[0].Values = v2;

            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = indicators[i];
                indicator.Candles = candles;

                if (!indicator.AreGraphsInitialized)
                    indicator.InitializeSeries(CandlesSeries);
                indicator.UpdateSeries();
            }

            Labels.Clear();
            for (int i = 0; i < candlesSpan; ++i)
                Labels.Add(candles[maxCandlesSpan - candlesSpan + i].Time.ToString("dd.MM.yyyy HH:mm"));
        }

        private async void resetIndicatorsButton_Click(object sender, RoutedEventArgs e)
        {
            CandlesSeries.Clear();
            bindedBuySeries = -1;
            bindedSellSeries = -1;
            indicators = new List<Indicator>();

            chartNameTextBlock.Text = activeStock.Name;

            await UpdateCandlesList();
            CandlesValuesChanged();

            chart.AxisY[0].ShowLabels = true;
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

            foreach (var indicator in indicators)
            {
                indicator.ResetState();
            }

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
            {
                indicator.ResetState();
                indicator.candlesSpan = candlesSpan;
            }

            if (activeStock == null)
                return;

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
