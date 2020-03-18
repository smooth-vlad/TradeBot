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
            periodTextBox.IsEnabled = value;
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

            await tradingChart.UpdateCandlesList();
            tradingChart.OnCandlesValuesChanged();
        }

        private async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await tradingChart.Simulate();
            SetEverythingEnabled(true);
            MessageBox.Show("Testing ended");
        }

        private async void periodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            periodErrorTextBlock.Text = string.Empty;

            int period;
            if (!int.TryParse(periodTextBox.Text.Trim(), out period))
            {
                periodErrorTextBlock.Text = "* Not a number";
                periodTextBox.Focus();
                return;
            }

            const int minPeriod = 10;
            if (period < minPeriod)
            {
                periodErrorTextBlock.Text = string.Format("* Value should be >= {0}", minPeriod);
                periodTextBox.Focus();
                return;
            }

            if (tradingChart.candlesSpan == period)
                return;

            tradingChart.candlesSpan = period;

            foreach (var indicator in tradingChart.indicators)
                indicator.candlesSpan = tradingChart.candlesSpan;

            await tradingChart.UpdateCandlesList();
            tradingChart.OnCandlesValuesChanged();
        }

        private void periodTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                periodTextBox_LostFocus(this, new RoutedEventArgs());
        }
    }
}
