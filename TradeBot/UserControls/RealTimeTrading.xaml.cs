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
    /// Логика взаимодействия для RealTimeTrading.xaml
    /// </summary>
    public partial class RealTimeTrading : UserControl
    {
        private Context context;
        private MarketInstrument activeStock;

        private System.Threading.Timer candlesTimer;

        private bool updatingCandlesNow = false;

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

            candlesTimer = new System.Threading.Timer((e) => CandlesTimerElapsed(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(45));

            DataContext = this;
        }

        // returns true if new candles are the same as last candles
        //private async Task<bool> UpdateCandlesList()
        //{
        //    maxCandlesSpan = CalculateMaxCandlesSpan();
        //    TimeSpan period;
        //    if (!intervalToMaxPeriod.TryGetValue(candleInterval, out period))
        //        throw new KeyNotFoundException();

        //    var newCandles = await GetCandles(activeStock.Figi, maxCandlesSpan, candleInterval, period);

        //    if (candles == null || candles.Count != newCandles.Count)
        //    {
        //        candles = newCandles;
        //        return true;
        //    }
        //    for (int i = 0; i < candles.Count; ++i)
        //    {
        //        if (!(candles[i].Close == newCandles[i].Close &&
        //            candles[i].Open == newCandles[i].Open &&
        //            candles[i].Low == newCandles[i].Low &&
        //            candles[i].High == newCandles[i].High))
        //        {
        //            candles = newCandles;
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        //private void CandlesValuesChanged()
        //{
        //    UpdateCandlesSeries();
        //    UpdateIndicatorsSeries();
        //    UpdateXLabels();

        //    for (int i = 0; i < indicators.Count; ++i)
        //    {
        //        var indicator = (MovingAverage)indicators[i];
        //        indicator.UpdateState(candlesSpan - 1);
        //        if (indicator.IsBuySignal(candlesSpan - 1))
        //            MessageBox.Show("It's time to buy the instrument");
        //        if (indicator.IsSellSignal(candlesSpan - 1))
        //            MessageBox.Show("It's time to sell the instrument");
        //    }
        //}

        // ==================================================
        // events
        // ==================================================

        private async void CandlesTimerElapsed()
        {
            if (updatingCandlesNow)
                return;
            await tradingChart.UpdateCandlesList();
            Dispatcher.Invoke(() => tradingChart.CandlesValuesChanged());
        }

        private void resetIndicatorsButton_Click(object sender, RoutedEventArgs e)
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
            tradingChart.CandlesValuesChanged();
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
            tradingChart.CandlesValuesChanged();
        }

        private void periodTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                periodTextBox_LostFocus(this, new RoutedEventArgs());
        }
    }
}
