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

        public SimulationTrading()
        {
            InitializeComponent();

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

        // Check if it is possible to build chart using user input.
        private async Task<bool> CheckInput()
        {
            // Set default values.
            tokenTextBlock.Text = "Token";
            tokenTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            tickerTextBlock.Text = "Ticker";
            tickerTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            try
            {
                // Connect using token.
                SandboxConnection connection = ConnectionFactory.GetSandboxConnection(tokenTextBox.Text.Trim());
                context = connection.Context;
                MarketInstrumentList allegedStocks = await context.MarketStocksAsync();

                // Check if there is any ticker.
                activeStock = allegedStocks.Instruments.Find(x => x.Ticker == tickerTextBox.Text);
                if (activeStock == null)
                    throw new NullReferenceException();
            }
            catch (OpenApiException)
            {
                // Prompt the tocken.
                tokenTextBlock.Text += "* ERROR * Unable to use this token.";
                tokenTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                tokenTextBox.Text = "";
                tokenTextBox.Focus();
                return false;
            }
            catch (NullReferenceException)
            {
                // Prompt the ticker.
                tickerTextBlock.Text += "* ERROR * Unknown ticker.";
                tickerTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                tickerTextBox.Text = "";
                tickerTextBox.Focus();
                return false;
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong...");
                return false;
            }
            return true;
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

        // returns true if new candles are the same as last candles
        private async Task<bool> UpdateCandlesList()
        {
            if (activeStock == null)
                return false;

            maxCandlesSpan = RecalculateMaxCandlesSpan();
            TimeSpan period;
            if (!intervalToMaxPeriod.TryGetValue(candleInterval, out period))
                throw new KeyNotFoundException();

            var newCandles = await GetCandles(activeStock.Figi, maxCandlesSpan, candleInterval, period);

            if (candles == null || candles.Count != newCandles.Count)
            {
                candles = newCandles;
                return true;
            }
            for (int i = 0; i < candles.Count; ++i)
            {
                if (!(candles[i].Close == newCandles[i].Close &&
                    candles[i].Open == newCandles[i].Open &&
                    candles[i].Low == newCandles[i].Low &&
                    candles[i].High == newCandles[i].High))
                {
                    candles = newCandles;
                    return true;
                }
            }
            return false;
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
            findButton.IsEnabled = value;
            simulateButton.IsEnabled = value;
            periodButton.IsEnabled = value;
            smaButton.IsEnabled = value;
            intervalComboBox.IsEnabled = value;
            periodTextBox.IsEnabled = value;
            tickerTextBox.IsEnabled = value;
            tokenTextBox.IsEnabled = value;
            smaStepTextBox.IsEnabled = value;
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
                        Fill = Brushes.Blue,
                        StrokeThickness = 3,
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
                        Fill = Brushes.Yellow,
                        StrokeThickness = 3,
                    });
                    bindedSellSeries = CandlesSeries.Count - 1;
                }
                else
                    CandlesSeries[bindedSellSeries].Values = sellSeries;
            }
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

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await CheckInput())
                return;

            CandlesSeries.Clear();
            bindedBuySeries = -1;
            bindedSellSeries = -1;
            indicators = new List<Indicator>();

            await UpdateCandlesList();
            CandlesValuesChanged();

            chart.AxisY[0].ShowLabels = true;
        }

        private async void smaButton_Click(object sender, RoutedEventArgs e)
        {
            if (activeStock == null)
            {
                MessageBox.Show("Pick a stock first");
                return;
            }

            int smaStep;
            if (!int.TryParse(smaStepTextBox.Text.Trim(), out smaStep))
            {
                MessageBox.Show("Wrong value in 'SMA step'");
                return;
            }

            var newIndicator = new MovingAverage(smaStep, 5, activeStock.MinPriceIncrement);
            newIndicator.candlesSpan = candlesSpan;
            indicators.Add(newIndicator);

            await UpdateCandlesList();
            CandlesValuesChanged();
        }

        private async void periodButton_Click(object sender, RoutedEventArgs e)
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

            candlesSpan = period;
            foreach (var indicator in indicators)
                indicator.candlesSpan = candlesSpan;

            if (activeStock == null)
                return;

            await UpdateCandlesList();
            CandlesValuesChanged();
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
    }
}
