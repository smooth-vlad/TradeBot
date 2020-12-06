using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;
using static TradeBot.Instrument;

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

            ShowBalance();

            IntervalComboBox.SelectedIndex = 5;
            StrategyComboBox.SelectedIndex = 0;
        }

        private void ShowBalance()
        {
            var balance = TradingChart.TradingInterface.Balance + TradingChart.instrument.TotalPrice;
            BalanceTextBlock.Text = $"Balance: {Math.Round(balance)}";
        }

        // ==================================================
        // events
        // ==================================================

        private void MA_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.RemoveIndicators();

            var ma = new MovingAverage(50, new ExponentialMaCalculation(), TradingChart.Candles);
            TradingChart.AddIndicator(ma);
            var strategy = new MaTradingStrategy(TradingChart.Candles, ma);
            TradingChart.SetStrategy(strategy);
        }

        private void MACD_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.RemoveIndicators();

            var macd = new Macd(new ExponentialMaCalculation(), 12, 26, 9, TradingChart.Candles);
            TradingChart.AddIndicator(macd);
            var strategy = new MacdTradingStrategy(TradingChart.Candles, macd);
            TradingChart.SetStrategy(strategy);
        }

        private void Random_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.RemoveIndicators();

            var strategy = new RandomTradingStrategy(TradingChart.Candles);
            TradingChart.SetStrategy(strategy);
        }

        private async void CandlesTimerElapsed()
        {
            if (TradingChart.LoadingCandlesTask == null)
                return;
            await TradingChart.LoadingCandlesTask;

            // TO TEST 'REAL TIME TRADING'
            TradingChart.rightCandleDateAhead = TradingChart.rightCandleDateAhead.AddDays(1);

            await Dispatcher.InvokeAsync(async () =>
            {
                await TradingChart.LoadNewCandles();
                ShowBalance();
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