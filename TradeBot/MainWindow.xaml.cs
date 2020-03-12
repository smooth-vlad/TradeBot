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

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Context context;
        private MarketInstrument activeStock;

        private List<IIndicator> indicators = new List<IIndicator>(); 

        private int candlesSpan = 100;
        private int maxCandlesSpan = 0;
        private CandleInterval candleInterval = CandleInterval.Minute;

        private List<CandlePayload> candles;

        public SeriesCollection CandlesSeries { get; set; }
        public List<string> Labels { get; set; }

        private System.Threading.Timer candlesTimer;

        public MainWindow()
        {
            InitializeComponent();

            CandlesSeries = new SeriesCollection();
            Labels = new List<string>();

            DataContext = this;

            candlesTimer = new System.Threading.Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds(DateTime.Now.Second - 5),
                TimeSpan.FromSeconds(10));
        }

        private OhlcPoint CandleToOhlc(CandlePayload candlePayload)
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

        private async Task<bool> UpdateCandles()
        {
            if (activeStock == null)
                return false;

            var local_maxCandlesSpan = candlesSpan;
            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = (SimpleMovingAverage)indicators[i];
                if (indicator.Period + candlesSpan > local_maxCandlesSpan)
                    local_maxCandlesSpan = candlesSpan + indicator.Period;
            }
            maxCandlesSpan = local_maxCandlesSpan;

            var newCandles = await GetCandles(activeStock.Figi, maxCandlesSpan, candleInterval, TimeSpan.FromDays(1));

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

        // ==================================================
        // events
        // ==================================================

        private async void CandlesTimerElapsed()
        {
            if (activeStock == null)
                return;

            if (await UpdateCandles())
                Dispatcher.Invoke(() => CandlesValuesChanged());
        }

        private void CandlesValuesChanged()
        {
            if (activeStock == null)
                return;

            var v = new ChartValues<OhlcPoint>();
            for (int i = maxCandlesSpan - candlesSpan; i < maxCandlesSpan; ++i)
                v.Add(CandleToOhlc(candles[i]));

            if (CandlesSeries.Count == 0)
            {
                CandlesSeries.Add(new CandleSeries
                {
                    ScalesXAt = 0,
                    Values = v,
                    StrokeThickness = 3
                });
            }
            else
                CandlesSeries[0].Values = v;

            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = (SimpleMovingAverage)indicators[i];
                indicator.candles = candles.GetRange(maxCandlesSpan - (candlesSpan + indicator.Period), candlesSpan + indicator.Period);
                indicator.UpdateState();

                if (indicator.bindedGraph == -1)
                {
                    CandlesSeries.Add(new LineSeries
                    {
                        ScalesXAt = 0,
                        Values = new ChartValues<decimal>(indicator.SMA),
                    });
                    indicator.bindedGraph = CandlesSeries.Count - 1;
                }
                else
                    CandlesSeries[indicator.bindedGraph].Values = new ChartValues<decimal>(indicator.SMA);
            }

            Labels.Clear();
            for (int i = 0; i < candlesSpan; ++i)
            {
                Labels.Add(DateTime.Now.AddMinutes(-(candlesSpan - i)).ToString("HH:mm"));
            }

            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = (SimpleMovingAverage)indicators[i];
                if (indicator.IsBuySignal())
                    MessageBox.Show("BUY STONK RN!1!!");
                if (indicator.IsSellSignal())
                    MessageBox.Show("SELL STONK RN!1!!");
            }
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await CheckInput())
                return;

            CandlesSeries.Clear();
            indicators = new List<IIndicator>();

            await UpdateCandles();
            CandlesValuesChanged();
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

            var newIndicator = new SimpleMovingAverage(smaStep);
            indicators.Add(newIndicator);

            await UpdateCandles();
            CandlesValuesChanged();
        }

        private async void periodButton_Click(object sender, RoutedEventArgs e)
        {
            int period;
            if (!int.TryParse(periodTextBox.Text.Trim(), out period))
            {
                MessageBox.Show("Wrong value in 'Period'");
                return;
            }

            candlesSpan = period;
            if (activeStock == null)
                return;

            await UpdateCandles();
            CandlesValuesChanged();
        }
    }
}
