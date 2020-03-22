using System;
using Tinkoff.Trading.OpenApi.Network;
using Tinkoff.Trading.OpenApi.Models;
using System.Windows.Controls;
using System.Threading;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        private Context context;
        private MarketInstrument activeStock;

        private Timer candlesTimer;

        public RealTimeTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.context = context;
            this.activeStock = activeStock;

            tradingChart.context = context;
            tradingChart.activeStock = activeStock;

            chartNameTextBlock.Text = activeStock.Name + " (Real-Time)";

            intervalComboBox.ItemsSource = TradingChart.intervalToMaxPeriod.Keys;
            intervalComboBox.SelectedIndex = 0;

            candlesTimer = new Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(0.25));

            DataContext = this;
        }

        // ==================================================
        // events
        // ==================================================

        private async void CandlesTimerElapsed()
        {
            if (tradingChart.LastCandleDate > DateTime.Now)
                return;

            if (tradingChart.LoadingCandlesTask == null || !tradingChart.LoadingCandlesTask.IsCompleted)
                return;

            await tradingChart.LoadNewCandles();
        }

        private void intervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CandleInterval? interval = null;
            var selectedInterval = intervalComboBox.SelectedItem.ToString();
            foreach (var k in TradingChart.intervalToMaxPeriod.Keys)
            {
                if (k.ToString() == selectedInterval)
                {
                    interval = k;
                    break;
                }
            }
            if (interval == null)
                return;

            tradingChart.candleInterval = interval.Value;

            tradingChart.ResetSeries();
        }
    }
}
