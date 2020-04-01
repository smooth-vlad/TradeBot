﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для TestingTrading.xaml
    /// </summary>
    public partial class TestingTrading : UserControl
    {
        MarketInstrument activeStock;
        Context context;

        public TestingTrading(Context context, MarketInstrument activeStock)
        {
            InitializeComponent();

            if (activeStock == null || context == null)
                throw new ArgumentNullException();

            this.activeStock = activeStock;
            this.context = context;

            TradingChart.context = context;
            TradingChart.activeStock = activeStock;

            ChartNameTextBlock.Text = activeStock.Name + " (Testing)";

            DataContext = this;
        }

        void SetEverythingEnabled(bool value)
        {
            SimulateButton.IsEnabled = value;
        }

        // ==================================================
        // events
        // ==================================================

        async void simulateButton_Click(object sender, RoutedEventArgs e)
        {
            SetEverythingEnabled(false);
            await TradingChart.UpdateTestingSignals();
            SetEverythingEnabled(true);
            MessageBox.Show("Testing ended");
        }
    }
}