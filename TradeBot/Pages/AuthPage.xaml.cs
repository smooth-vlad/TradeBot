using System;
using System.Windows;
using System.Windows.Controls;
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

        async void authButton_Click(object sender, RoutedEventArgs e)
        {
            tokenErrorTextBlock.Text = string.Empty;
            try
            {
                var connection = ConnectionFactory.GetSandboxConnection(tokenTextBox.Text.Trim());
                var allegedStocks = await connection.Context.MarketStocksAsync();
                Connect?.Invoke(connection.Context);
            }
            catch (Exception)
            {
                tokenErrorTextBlock.Text = "* Token is invalid.";
                tokenTextBox.Focus();
                return;
            }
        }
    }
}
