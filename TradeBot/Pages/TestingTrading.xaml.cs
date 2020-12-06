using System;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для TestingTrading.xaml
    /// </summary>
    public partial class TestingTrading : UserControl
    {
        public TestingTrading(MarketInstrument activeInstrument)
        {
            InitializeComponent();

            if (activeInstrument == null)
                throw new ArgumentNullException();

            TradingChart.instrument = new Instrument(activeInstrument);

            DataContext = this;

            IntervalComboBox.SelectedIndex = 5;
            StrategyComboBox.SelectedIndex = 0;
        }

        private void SetEverythingEnabled(bool value)
        {
            SimulateButton.IsEnabled = value;
        }

        private void ShowBalance()
        {
            var balance = TradingChart.TradingInterface.Balance + TradingChart.instrument.TotalPrice;
            BalanceTextBlock.Text = $"Balance: {Math.Round(balance)}";
            BalanceTextBlock.Visibility = Visibility.Visible;
        }

        private void ResetState()
        {
            TradingChart.RemoveIndicators();
            TradingChart.RemoveMarkers();
            BalanceTextBlock.Visibility = Visibility.Collapsed;
        }

        // ==================================================
        // events
        // ==================================================

        private void MA_OnSelected(object sender, RoutedEventArgs e)
        {
            ResetState();

            var ma = new MovingAverage(50, new ExponentialMaCalculation(), TradingChart.Candles);
            TradingChart.AddIndicator(ma);
            var strategy = new MaTradingStrategy(TradingChart.Candles, ma);
            TradingChart.SetStrategy(strategy);
        }

        private void MACD_OnSelected(object sender, RoutedEventArgs e)
        {
            ResetState();

            var macd = new Macd(new ExponentialMaCalculation(), 12, 26, 9, TradingChart.Candles);
            TradingChart.AddIndicator(macd);
            var strategy = new MacdTradingStrategy(TradingChart.Candles, macd);
            TradingChart.SetStrategy(strategy);
        }

        private async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await TradingChart.BeginTesting();
            SetEverythingEnabled(true);
            ShowBalance();
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