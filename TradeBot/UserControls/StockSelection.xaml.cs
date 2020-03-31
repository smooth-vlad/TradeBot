using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для StockSelection.xaml
    /// </summary>
    public partial class StockSelection : UserControl
    {
        readonly Context context;
        readonly TabItem parent;
        List<string> instrumentsLabels;
        MarketInstrumentList instruments;

        public StockSelection(Context context, TabItem parent)
        {
            InitializeComponent();

            this.context = context;
            this.parent = parent;

            StockRadioButton.IsChecked = true;
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            InstrumentErrorTextBlock.Text = string.Empty;
            try
            {
                var activeInstrument =
                    instruments.Instruments[instrumentsLabels.FindIndex(v => v == TickerComboBox.Text)];

                parent.Header = activeInstrument.Name;

                if (RealTimeRadioButton.IsChecked == true)
                {
                    parent.Header += " (Real-Time)";
                    parent.Content = new RealTimeTrading(context, activeInstrument);
                }
                else if (TestingRadioButton.IsChecked == true)
                {
                    parent.Header += " (Testing)";
                    parent.Content = new TestingTrading(context, activeInstrument);
                }
            }
            catch (Exception)
            {
                InstrumentErrorTextBlock.Text = "* Pick an instrument first";
                TickerComboBox.IsDropDownOpen = true;
            }
        }

        void TickerComboBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (TickerComboBox.ItemsSource == null)
                return;
            
            var tb = (TextBox) e.OriginalSource;
            if (tb.SelectionStart != 0)
                TickerComboBox.SelectedItem = null;

            if (TickerComboBox.SelectedItem != null) return;
            var cv = (CollectionView) CollectionViewSource.GetDefaultView(TickerComboBox.ItemsSource);
            cv.Filter = s =>
                ((string) s).IndexOf(TickerComboBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;

            TickerComboBox.IsDropDownOpen = cv.Count < 100;
            tb.SelectionLength = 0;
            tb.SelectionStart = tb.Text.Length;
        }

        async void EtfRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                instruments = await context.MarketEtfsAsync();
                instrumentsLabels = instruments.Instruments.ConvertAll(v => $"{v.Ticker} ({v.Name})");
                TickerComboBox.ItemsSource = instrumentsLabels;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        async void StockRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                instruments = await context.MarketStocksAsync();
                instrumentsLabels = instruments.Instruments.ConvertAll(v => $"{v.Ticker} ({v.Name})");
                TickerComboBox.ItemsSource = instrumentsLabels;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        async void CurrencyRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                instruments = await context.MarketCurrenciesAsync();
                instrumentsLabels = instruments.Instruments.ConvertAll(v => $"{v.Ticker} ({v.Name})");
                TickerComboBox.ItemsSource = instrumentsLabels;
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}