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

        private List<decimal> lastPrices;

        private System.Threading.Timer candlesTimer;

        public MainWindow()
        {
            InitializeComponent();

            candlesTimer = new System.Threading.Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds(DateTime.Now.Second - 5),
                TimeSpan.FromSeconds(20));
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

        private void AddLineSeries(List<decimal> values, int strokeThiccness)
        {
            chart.Series.Add(new LineSeries
            {
                Values = new ChartValues<decimal>(values),
                StrokeThickness = strokeThiccness
            });
        }

        private void UpdateLineSeries(List<decimal> values, int index)
        {
            if (chart.Series.Count >= index)
                chart.Series[index].Values = new ChartValues<decimal>(values);
        }

        private async Task<List<decimal>> GetCandlesClosures(string figi, int amount, CandleInterval interval, TimeSpan queryOffset)
        {
            var result = new List<decimal>(amount);
            var to = DateTime.Now;

            int m = 0;
            while (result.Count < amount)
            {
                var candles = await context.MarketCandlesAsync(figi, to - queryOffset, to, interval);

                for (int i = candles.Candles.Count - 1; i >= 0 && result.Count < amount; --i)
                    result.Add(candles.Candles[i].Close);
                to = to - queryOffset;
                m++;
            }
            //MessageBox.Show(m.ToString());
            result.Reverse();
            return result;
        }

        // ==================================================
        // events
        // ==================================================

        private async void CandlesTimerElapsed()
        {
            if (activeStock == null)
                return;

            await Update();
            Dispatcher.Invoke(() => CandlesValuesChanged());
        }

        private async Task Update()
        {
            if (activeStock == null)
                return;
            var local_maxCandlesSpan = candlesSpan;
            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = (SimpleMovingAverage)indicators[i];
                if (indicator.Period + candlesSpan > local_maxCandlesSpan)
                    local_maxCandlesSpan = candlesSpan + indicator.Period;
            }
            maxCandlesSpan = local_maxCandlesSpan;

            var candles = await GetCandlesClosures(activeStock.Figi, maxCandlesSpan, candleInterval, TimeSpan.FromDays(1));

            if (lastPrices == null || lastPrices.Count != candles.Count || !candles.SequenceEqual(lastPrices))
                lastPrices = candles;
        }

        private void CandlesValuesChanged()
        {
            if (activeStock == null)
                return;

            if (chart.Series.Count == 0)
                AddLineSeries(lastPrices.GetRange(maxCandlesSpan - candlesSpan, candlesSpan), 3);
            else
                UpdateLineSeries(lastPrices.GetRange(maxCandlesSpan - candlesSpan, candlesSpan), 0);

            for (int i = 0; i < indicators.Count; ++i)
            {
                var indicator = (SimpleMovingAverage)indicators[i];
                indicator.closures = lastPrices.GetRange(maxCandlesSpan - (candlesSpan + indicator.Period), candlesSpan + indicator.Period);
                indicator.UpdateState();

                if (chart.Series.Count <= indicator.bindedGraph)
                    AddLineSeries(indicator.SMA, 2);
                else
                    UpdateLineSeries(indicator.SMA, indicator.bindedGraph);
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

            chart.Series = new SeriesCollection();
            indicators = new List<IIndicator>();

            await Update();
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

            var newIndicator = new SimpleMovingAverage(smaStep, chart.Series.Count);

            indicators.Add(newIndicator);

            await Update();
            CandlesValuesChanged();
        }
    }
}
