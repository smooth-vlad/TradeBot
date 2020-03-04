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
        private MarketInstrument stock;
        
        private System.Timers.Timer candlesTimer = new System.Timers.Timer();

        public MainWindow()
        {
            InitializeComponent();
            candlesTimer.AutoReset = true;
            candlesTimer.Elapsed += CandlesTimer_Elapsed;
            candlesTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
        }

        private void CandlesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _ = CalculateSeries();
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
                SandboxConnection connection = ConnectionFactory.GetSandboxConnection(tokenTextBox.Text);
                context = connection.Context;
                MarketInstrumentList allegedStocks = context.MarketStocksAsync().Result;

                // Check if there is any ticker.
                stock = allegedStocks.Instruments.Find(x => x.Ticker == tickerTextBox.Text);
                if (stock == null) throw new NullReferenceException();
            }
            catch (OpenApiException)
            {
                // Prompt the tocken.
                tokenTextBlock.Text += "* Unable to use this token.";
                tokenTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                tokenTextBox.Text = "";
                tokenTextBox.Focus();
                return false;
            }
            catch (NullReferenceException)
            {
                // Prompt the ticker.
                tickerTextBlock.Text += "* Unknown ticker.";
                tickerTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                tickerTextBox.Text = "";
                tickerTextBox.Focus();
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

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            candlesTimer.Start();
            await CalculateSeries();
        }

        private async Task CalculateSeries()
        {
            bool isInputCorrect = CheckInput();
            if (!isInputCorrect) return;

            List<Price> closePrices = await GetCandlesClosures(stock.Figi, 100, CandleInterval.Hour, TimeSpan.FromDays(1));

            chart.Series = new SeriesCollection();
            chart.AxisX = new AxesCollection();
            chart.AxisX.Add(new Axis());

            int step = 12;

            // Stock close price.
            chart.Series.Add(new LineSeries
            {
                Values = new ChartValues<Price>(closePrices.GetRange(step, closePrices.Count - step)),
                StrokeThickness = 3
            });
            // SMA 50
            chart.Series.Add(new LineSeries
            {
                Values = new ChartValues<Price>(CalculateSMA(closePrices, step)),
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

                foreach (var candle in candles.Candles)
                {
                    if (result.Count >= amount)
                        return result;
                    result.Add(candle.Close);
                }
                to = to - queryOffset;
            }
            return result;
        }
    }
}
