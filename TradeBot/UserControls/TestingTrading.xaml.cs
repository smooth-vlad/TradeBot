using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Tinkoff.Trading.OpenApi.Network;
using Tinkoff.Trading.OpenApi.Models;
using System.Windows.Controls;
using System.Diagnostics;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для TestingTrading.xaml
    /// </summary>
    public partial class TestingTrading : UserControl
    {
        Context context;
        MarketInstrument activeStock;

        public TestingTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.activeStock = activeStock;
            this.context = context;

            tradingChart.context = context;
            tradingChart.activeStock = activeStock;

            chartNameTextBlock.Text = activeStock.Name + " (Testing)";

            intervalComboBox.ItemsSource = TradingChart.intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            DataContext = this;
        }

        void SetEverythingEnabled(bool value)
        {
            simulateButton.IsEnabled = value;
            intervalComboBox.IsEnabled = value;
        }

        // ==================================================
        // events
        // ==================================================

        void intervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CandleInterval? interval = null;
            var selectedInterval = intervalComboBox.SelectedItem.ToString();
            foreach (var k in
                TradingChart.intervalToMaxPeriod.Keys.Where(k => k.ToString() == selectedInterval))
            {
                interval = k;
                break;
            }
            if (interval == null)
                return;

            tradingChart.candleInterval = interval.Value;

            tradingChart.ResetSeries();
        }

        async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await tradingChart.UpdateTestingSignals();
            SetEverythingEnabled(true);
            MessageBox.Show("Testing ended");
        }
    }
}
