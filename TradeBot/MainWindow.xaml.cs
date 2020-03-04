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

using Price = System.Decimal;

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
        private CandleInterval candleInterval = CandleInterval.Minute;

        private List<Price> previousPrices;

        private System.Threading.Timer candlesTimer;

        public MainWindow()
        {
            InitializeComponent();

            candlesTimer = new System.Threading.Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds(DateTime.Now.Second - 5),
                TimeSpan.FromSeconds(5));
        }

        // Check if it is possible to build chart using user input.
        private bool CheckInput()
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
                MarketInstrumentList allegedStocks = context.MarketStocksAsync().Result;

                // Check if there is any ticker.
                activeStock = allegedStocks.Instruments.Find(x => x.Ticker == tickerTextBox.Text);
                if (activeStock == null) throw new NullReferenceException();
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

        // Calculates SMA for every calndle provided and returns them as a list.
        private List<Price> CalculateSMA(List<Price> closures, int step)
        {
            var SMA = new List<Price>(closures.Count - step);
            for (int i = step; i < closures.Count; ++i)
            {
                decimal sum = 0;
                for (int j = 0; j < step; ++j)
                    sum += closures[i - j];
                SMA.Add(sum / step);
            }
            return SMA;
        }

        private async Task DisplayClosures()
        {
            if (activeStock == null)
            {
                MessageBox.Show("Pick a stock first");
                return;
            }

            List<Price> closePrices = await GetCandlesClosures(activeStock.Figi, candlesSpan, candleInterval, TimeSpan.FromHours(1));
            previousPrices = closePrices;

            if (chart.Series.Count > 0)
            {
                chart.Series[0].Values = new ChartValues<Price>(closePrices);
            }
            else
            {
                chart.Series.Add(new LineSeries
                {
                    Values = new ChartValues<Price>(closePrices),
                    StrokeThickness = 3
                });
            }
        }
        
        private async Task DisplaySMA()
        {
            if (activeStock == null)
            {
                MessageBox.Show("Pick a stock first");
                return;
            }

            int step = int.Parse(smaStepTextBox.Text.Trim());

            chart.Series.Add(new LineSeries
            {
                Values = new ChartValues<Price>(
                    CalculateSMA(await GetCandlesClosures(activeStock.Figi, candlesSpan + step, candleInterval, TimeSpan.FromHours(1)), step)),
                Fill = Brushes.Transparent,
            });
        }

        private async Task<List<Price>> GetCandlesClosures(string figi, int amount, CandleInterval interval, TimeSpan queryOffset)
        {
            var result = new List<Price>(amount);
            var to = DateTime.Now;

            while (result.Count < amount)
            {
                var candles = await context.MarketCandlesAsync(figi, to - queryOffset, to, interval);

                for (int i = candles.Candles.Count - 1; i >= 0 && result.Count < amount; --i)
                {
                    result.Add(candles.Candles[i].Close);
                }
                to = to - queryOffset;
            }
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

            var candles = await GetCandlesClosures(activeStock.Figi, candlesSpan, candleInterval, TimeSpan.FromHours(1));

            if (candles.SequenceEqual(previousPrices))
                return;
            else
            {
                await Dispatcher.InvokeAsync(() => CandlesValuesChanged());
                previousPrices = candles;
            }
        }

        private async void CandlesValuesChanged()
        {
            if (activeStock == null)
                return;

            await DisplayClosures();
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckInput())
                return;

            chart.Series = new SeriesCollection();

            await DisplayClosures();
        }

        private async void smaButton_Click(object sender, RoutedEventArgs e)
        {
            await DisplaySMA();
        }
    }
}
