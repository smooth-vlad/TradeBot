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

        public RealTimeTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            this.context = context;
            this.activeStock = activeStock;

            chartNameTextBlock.Text = activeStock.Name + " (Real-Time)";

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
                    MessageBox.Show("It's time to buy the instrument");
                if (indicator.IsSellSignal(candlesSpan - 1))
                    MessageBox.Show("It's time to sell the instrument");
            }
        }

        private async void resetIndicatorsButton_Click(object sender, RoutedEventArgs e)
        {
            //CandlesSeries.Clear();
            indicators = new List<Indicator>();

            //await UpdateCandlesList();
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
