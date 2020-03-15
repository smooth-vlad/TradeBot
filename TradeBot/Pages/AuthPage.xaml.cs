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
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        public delegate void ConnectionHandler(Context context);
        public event ConnectionHandler Connect;

        public AuthPage()
        {
            InitializeComponent();

            tokenTextBox.Focus();
        }

        private async void authButton_Click(object sender, RoutedEventArgs e)
        {
            tokenErrorTextBlock.Text = string.Empty;
            try
            {
                SandboxConnection connection = ConnectionFactory.GetSandboxConnection(tokenTextBox.Text.Trim());
                MarketInstrumentList allegedStocks = await connection.Context.MarketStocksAsync();
                Connect.Invoke(connection.Context);
            }
            catch (Exception)
            {
                tokenErrorTextBlock.Text = "* Token in invalid.";
                tokenTextBox.Focus();
                return;
            }
            tokenErrorTextBlock.Text = string.Empty;
        }
    }
}
