using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tinkoff.Trading.OpenApi.Models;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для StockSelection.xaml
    /// </summary>
    public partial class StockSelection : UserControl
    {
        private Context context;
        private TabItem parent;

        public StockSelection(Context context, TabItem parent)
        {
            InitializeComponent();

            this.context = context;
            this.parent = parent;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            tickerErrorTextBlock.Text = string.Empty;

            MarketInstrument activeStock;
            try
            {
                MarketInstrumentList allegedStocks = await context.MarketStocksAsync();
                activeStock = allegedStocks.Instruments.Find(x => x.Ticker == tickerTextBox.Text);
                if (activeStock == null)
                    throw new NullReferenceException();
            }
            catch(NullReferenceException)
            {
                tickerErrorTextBlock.Text = "* There is no such ticker";
                tickerTextBox.Focus();
                return;
            }

            parent.Header = activeStock.Name;

            if (realTimeRadioButton.IsChecked == true)
            {
                parent.Header += " (Real-Time)";
                parent.Content = new RealTimeTrading(context, activeStock);
            }
            else if (simulationRadioButton.IsChecked == true)
            {
                parent.Header += " (Simulation)";
                parent.Content = new SimulationTrading(context, activeStock);
            }
        }
    }
}
