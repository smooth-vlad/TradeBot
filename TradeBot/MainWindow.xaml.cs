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
    public partial class TradeBotWindow : Window
    {
        private Context context;
        private MarketInstrument stock;

        private System.Timers.Timer candlesTimer = new System.Timers.Timer();

        public TradeBotWindow()
        {
            InitializeComponent();
            candlesTimer.AutoReset = true;
            candlesTimer.Elapsed += CandlesTimer_Elapsed;
            candlesTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            candlesTimer.Start();
        }

        private void CandlesTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CalculateSeries();
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
                SandboxConnection connection = ConnectionFactory.GetSandboxConnection(tokenTextBox.Text);
                context = connection.Context;
                MarketInstrumentList allegedStocks = await context.MarketStocksAsync();

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

        // Gets a list of candles closures in the current context for a given period of time.
        private async Task<List<decimal>> getCandlesClosures(DateTime from, DateTime to, CandleInterval interval)
        {
            CandleList candles = await context.MarketCandlesAsync(stock.Figi, from, to, interval);

            List<decimal> closures = new List<decimal>(candles.Candles.Count);
            candles.Candles.ForEach(x => closures.Add(x.Close));

            return closures;
        }

        // Calculates SMA for every calndle provided and returns them as a list.
        private async Task<List<decimal>> CalculateSMA(List<decimal> closures, int step)
        {
            // Caclulates closures for the previous segment.
            List<decimal> previousClosures = await getCandlesClosures(
                DateTime.Today.AddHours(-2), DateTime.Today.AddHours(-1), CandleInterval.Minute);

            Queue<decimal> queue = new Queue<decimal>(previousClosures.Skip(previousClosures.Count - step));
            List<decimal> SMA = new List<decimal>(closures.Count + 1) { queue.Average() };
            closures.ForEach(x => { queue.Dequeue(); queue.Enqueue(x); SMA.Add(queue.Average()); });

            //Excludes a value that only considers data from the previous segment.
            return SMA.Skip(1).ToList();
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            await CalculateSeries();
        }

        private async Task CalculateSeries()
        {
            bool isInputCorrect = await CheckInput();
            if (!isInputCorrect) return;

            List<decimal> closePrices = await getCandlesClosures(
                DateTime.Today.AddHours(-1), DateTime.Today, CandleInterval.Minute);

            chart.Series = new SeriesCollection();
            while (chart.AxisX.Count < 3)
            chart.AxisX.Add(new Axis());

            // Stock close price.
            chart.Series.Add(new LineSeries { Values = new ChartValues<decimal>(closePrices) });
            // SMA 50
            chart.Series.Add(new LineSeries { Values = new ChartValues<decimal>(await CalculateSMA(closePrices, 50)) });
            // SMA 200
            chart.Series.Add(new LineSeries { Values = new ChartValues<decimal>(await CalculateSMA(closePrices, 200)) });
        }
    }
}
