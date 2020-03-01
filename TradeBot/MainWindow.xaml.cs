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

        // Gets a list of candles closures in the current context for a given period of time.
        private async Task<List<decimal>> getCandlesClosures(DateTime from, DateTime to, CandleInterval interval)
        {
            CandleList candles = await context.MarketCandlesAsync(stock.Figi, from, to, interval);

            List<decimal> closures = new List<decimal>(candles.Candles.Count);
            candles.Candles.ForEach(x => closures.Add(x.Close));

            return closures;
        }

        // Calculates SMA for every calndle provided and returns them as a list.
        private List<decimal> CalculateSMA(List<decimal> closures, int step)
        {
            var SMA = new List<decimal>(closures.Count);
            for (int i = 0; i < closures.Count; ++i)
            {
                decimal sum = 0;
                int j;
                for (j = 0; j < step && i - j >= 0; ++j)
                    sum += closures[i - j];
                SMA.Add(sum / j);
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

            List<decimal> closePrices = await getCandlesClosures(
                DateTime.Today.AddYears(-1).AddDays(1), DateTime.Today, CandleInterval.Day);

            chart.Series = new SeriesCollection();
            chart.AxisX = new AxesCollection();
            chart.AxisX.Add(new Axis());

            // Stock close price.
            chart.Series.Add(new LineSeries { Values = new ChartValues<decimal>(closePrices), StrokeThickness = 3 });
            // SMA 50
            chart.Series.Add(new LineSeries
            {
                Values = new ChartValues<decimal>(CalculateSMA(closePrices, 50)),
                Fill = Brushes.Transparent,
                //Stroke = Brushes.GreenYellow,
            });
            // SMA 200
            chart.Series.Add(new LineSeries
            {
                Values = new ChartValues<decimal>(CalculateSMA(closePrices, 200)),
                Fill = Brushes.Transparent,
                //Stroke = Brushes.MidnightBlue,
            });
        }
    }
}
