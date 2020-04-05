using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

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

            IntervalComboBox.SelectedIndex = 4;
        }

        void SetEverythingEnabled(bool value)
        {
            SimulateButton.IsEnabled = value;
        }

        // ==================================================
        // events
        // ==================================================

        async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await TradingChart.UpdateTestingSignals();
            SetEverythingEnabled(true);
            MessageBox.Show("Testing ended");
        }

        void ListBoxItem1m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Minute;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem5m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.FiveMinutes;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem15m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.QuarterHour;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem30m_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.HalfHour;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem1h_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Hour;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem1d_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Day;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem1w_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Week;
            TradingChart.ResetSeries();
        }
        
        void ListBoxItem1mn_OnSelected(object sender, RoutedEventArgs e)
        {
            TradingChart.candleInterval = CandleInterval.Month;
            TradingChart.ResetSeries();
        }
    }
}