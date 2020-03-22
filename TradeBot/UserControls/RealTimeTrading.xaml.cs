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
    /// Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        private Context context;
        private MarketInstrument activeStock;

        private System.Threading.Timer candlesTimer;

        public RealTimeTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.context = context;
            this.activeStock = activeStock;

            tradingChart.context = context;
            tradingChart.activeStock = activeStock;
            tradingChart.CandlesChange += TradingChart_CandlesChange;

            chartNameTextBlock.Text = activeStock.Name + " (Real-Time)";

            intervalComboBox.ItemsSource = TradingChart.intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            candlesTimer = new System.Threading.Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(0.25));

            DataContext = this;
        }

        // ==================================================
        // events
        // ==================================================

        private void TradingChart_CandlesChange()
        {
            foreach (var indicator in tradingChart.indicators)
            {
                indicator.UpdateState(0);
                if (indicator.IsBuySignal(0))
                    MessageBox.Show("It's time to buy the instrument");
                if (indicator.IsSellSignal(0))
                    MessageBox.Show("It's time to sell the instrument");
            }
        }

        private Task loadingCandlesTask;

        private async void CandlesTimerElapsed()
        {
            if (tradingChart.LastCandleDate > DateTime.Now)
                return;

            if (tradingChart.LoadingCandlesTask != null)
                await tradingChart.LoadingCandlesTask;
            if (loadingCandlesTask != null)
                await loadingCandlesTask;
            loadingCandlesTask = tradingChart.LoadNewCandles();
            await loadingCandlesTask;
        }

        private void intervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

            tradingChart.ResetSeries();
        }
    }
}
