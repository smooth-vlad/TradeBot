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

            TradingChart.ActiveInstrument = activeInstrument;

            DataContext = this;

            IntervalComboBox.SelectedIndex = 5;
        }

        private void SetEverythingEnabled(bool value)
        {
            SimulateButton.IsEnabled = value;
        }

        // ==================================================
        // events
        // ==================================================

        private async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await TradingChart.UpdateTestingSignals();
            SetEverythingEnabled(true);
            MessageBox.Show($"Testing ended\nInitial balance = 10000\nBalance after testing = {TradingChart.Balance}");
        }

        private void ListBoxItem1m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Minute;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem5m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.FiveMinutes;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem15m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.QuarterHour;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem30m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.HalfHour;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem1h_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Hour;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem1d_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Day;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem1w_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Week;
            TradingChart.ResetSeries();
        }

        private void ListBoxItem1mn_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Month;
            TradingChart.ResetSeries();
        }
    }
}