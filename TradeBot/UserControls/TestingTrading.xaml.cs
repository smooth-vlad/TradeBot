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
        private Context context;
        private MarketInstrument activeStock;

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

        private void SetEverythingEnabled(bool value)
        {
            simulateButton.IsEnabled = value;
            intervalComboBox.IsEnabled = value;
        }

        // ==================================================
        // events
        // ==================================================

        private async void intervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CandleInterval interval = CandleInterval.Minute;
            bool intervalFound = false;
            var selectedInterval = intervalComboBox.SelectedItem.ToString();
            foreach (var k in TradingChart.intervalToMaxPeriod.Keys)
            {
                if (k.ToString() == selectedInterval)
                {
                    interval = k;
                    intervalFound = true;
                    break;
                }
            }
            if (!intervalFound)
                return;

            tradingChart.candleInterval = interval;

            await tradingChart.ResetSeries();
        }

        private async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await tradingChart.Simulate();
            SetEverythingEnabled(true);
            MessageBox.Show("Testing ended");
        }
    }
}
