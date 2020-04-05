using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        Timer candlesTimer;

        public RealTimeTrading(MarketInstrument activeInstrument)
        {
            InitializeComponent();

            if (activeInstrument == null)
                throw new ArgumentNullException();

            TradingChart.ActiveInstrument = activeInstrument;

            candlesTimer = new Timer(e => CandlesTimerElapsed(),
                null,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30));

            DataContext = this;
            
            IntervalComboBox.SelectedIndex = 5;
        }

        // ==================================================
        // events
        // ==================================================

        async void CandlesTimerElapsed()
        {
            if (TradingChart.LoadingCandlesTask == null)
                return;
            await TradingChart.LoadingCandlesTask;

            await Dispatcher.InvokeAsync(async () => await TradingChart.LoadNewCandles());
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