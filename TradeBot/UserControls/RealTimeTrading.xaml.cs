using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        MarketInstrument activeStock;

        Timer candlesTimer;
        Context context;

        public RealTimeTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.context = context;
            this.activeStock = activeStock;

            TradingChart.context = context;
            TradingChart.activeStock = activeStock;

            ChartNameTextBlock.Text = activeStock.Name + " (Real-Time)";

            IntervalComboBox.ItemsSource = TradingChart.intervalToMaxPeriod.Keys;
            IntervalComboBox.SelectedIndex = 0;

            candlesTimer = new Timer(e => CandlesTimerElapsed(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(30));

            DataContext = this;
        }

        // ==================================================
        // events
        // ==================================================

        async void CandlesTimerElapsed()
        {
            if (TradingChart.LoadingCandlesTask == null)
                return;
            await TradingChart.LoadingCandlesTask;

            await TradingChart.LoadNewCandles();
        }

        void intervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CandleInterval? interval = null;
            var selectedInterval = IntervalComboBox.SelectedItem.ToString();
            foreach (var k in
                TradingChart.intervalToMaxPeriod.Keys.Where(k => k.ToString() == selectedInterval))
            {
                interval = k;
                break;
            }

            if (interval == null)
                return;

            TradingChart.candleInterval = interval.Value;

            TradingChart.ResetSeries();
        }
    }
}