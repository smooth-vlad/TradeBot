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
    /// Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        private Context context;
        private MarketInstrument activeStock;

        private List<Indicator> indicators = new List<Indicator>();

        private int candlesSpan = 50;
        private int maxCandlesSpan = 0;
        private CandleInterval candleInterval = CandleInterval.Minute;

        private List<CandlePayload> candles;

        public SeriesCollection CandlesSeries { get; set; }
        public List<string> Labels { get; set; }

        private System.Threading.Timer candlesTimer;

        private bool updatingCandlesNow = false;

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

        public RealTimeTrading(Context context)
        {
            InitializeComponent();

            this.context = context;

            intervalComboBox.ItemsSource = intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            CandlesSeries = new SeriesCollection();
            Labels = new List<string>();

            DataContext = this;

            candlesTimer = new System.Threading.Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5));
        }


        public static OhlcPoint CandleToOhlc(CandlePayload candlePayload)
        {
            return new OhlcPoint((double)candlePayload.Open, (double)candlePayload.High, (double)candlePayload.Low, (double)candlePayload.Close);
        }

        // Check if it is possible to build chart using user input.
        private async Task<bool> CheckInput()
        {
            // Set default values.
            tickerTextBlock.Text = "Ticker";
            tickerTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            try
            {
                MarketInstrumentList allegedStocks = await context.MarketStocksAsync();

                // Check if there is any ticker.
                activeStock = allegedStocks.Instruments.Find(x => x.Ticker == tickerTextBox.Text);
                if (activeStock == null)
                    throw new NullReferenceException();
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
            updatingCandlesNow = true;
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
            updatingCandlesNow = false;
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

        // ==================================================
        // events
        // ==================================================

        private async void CandlesTimerElapsed()
        {
            if (activeStock == null)
                return;

            if (updatingCandlesNow)
                return;
            if (await UpdateCandlesList())
                Dispatcher.Invoke(() => CandlesValuesChanged());
        }

        private void CandlesValuesChanged()
        {
            if (activeStock == null)
                return;

            var v = new List<OhlcPoint>(candlesSpan);
            for (int i = maxCandlesSpan - candlesSpan; i < maxCandlesSpan; ++i)
                v.Add(CandleToOhlc(candles[i]));
            var v2 = new ChartValues<OhlcPoint>(v);

            if (CandlesSeries.Count == 0)
            {
                CandlesSeries.Add(new CandleSeries
                {
                    ScalesXAt = 0,
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

            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = (MovingAverage)indicators[i];
                indicator.UpdateState(candlesSpan - 1);
                if (indicator.IsBuySignal(candlesSpan - 1))
                    MessageBox.Show("BUY STONK RN!1!!");
                if (indicator.IsSellSignal(candlesSpan - 1))
                    MessageBox.Show("SELL STONK RN!1!!");
            }
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await CheckInput())
                return;

            CandlesSeries.Clear();
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
    }
}
