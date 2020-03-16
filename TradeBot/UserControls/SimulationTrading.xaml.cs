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
using LiveCharts.Defaults;
using System.Windows.Controls;
using System.Diagnostics;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для SimulationTrading.xaml
    /// </summary>
    public partial class SimulationTrading : UserControl
    {
        private Context context;
        private MarketInstrument activeStock;

        public SimulationTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.activeStock = activeStock;
            this.context = context;

            tradingChart.context = context;
            tradingChart.activeStock = activeStock;

            chartNameTextBlock.Text = activeStock.Name + " (Simulation)";

            intervalComboBox.ItemsSource = TradingChart.intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            DataContext = this;
        }

        private void SetEverythingEnabled(bool value)
        {
            removeIndicatorsButton.IsEnabled = value;
            simulateButton.IsEnabled = value;
            intervalComboBox.IsEnabled = value;
            periodTextBox.IsEnabled = value;
        }

        // ==================================================
        // events
        // ==================================================

        private void removeIndicatorsButton_Click(object sender, RoutedEventArgs e)
        {
            tradingChart.RemoveIndicators();
        }

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
        }

        private async void periodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int period;
            if (!int.TryParse(periodTextBox.Text.Trim(), out period))
            {
                MessageBox.Show("Not a number in 'Period'");
                return;
            }

            if (period < 10 || period > 300)
            {
                MessageBox.Show("'Period' should be >= 10 and <= 300");
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
