using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        private Timer candlesTimer;

        public RealTimeTrading(MarketInstrument activeInstrument)
        {
            InitializeComponent();

            if (activeInstrument == null)
                throw new ArgumentNullException();

            TradingChart.instrument = new Instrument(activeInstrument);

            candlesTimer = new Timer(e => CandlesTimerElapsed(),
                null,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(0.3)
            );

            DataContext = this;

            IntervalComboBox.SelectedIndex = 5;
        }

        // ==================================================
        // events
        // ==================================================

        private async void CandlesTimerElapsed()
        {
            if (TradingChart.LoadingCandlesTask == null)
                return;
            await TradingChart.LoadingCandlesTask;

            // TO TEST 'REAL TIME TRADING'
            TradingChart.rightCandleDateAhead = TradingChart.rightCandleDateAhead.AddDays(1);

            await Dispatcher.InvokeAsync(async () => {
                try
                {
                    await TradingChart.LoadNewCandles();
                    var ti = TradingChart.TradingInterface;
                    BalanceTextBlock.Text = (ti.Balance + ti.DealPrice * ti.DealLots).ToString();
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                }
            });
        }

        private void ListBoxItem1m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Minute;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem5m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.FiveMinutes;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem15m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.QuarterHour;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem30m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.HalfHour;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem1h_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Hour;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem1d_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Day;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem1w_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Week;
            TradingChart.RestartSeries();
        }

        private void ListBoxItem1mn_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Month;
            TradingChart.RestartSeries();
        }
    }
}