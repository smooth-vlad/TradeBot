using System;
using System.Windows;
using System.Windows.Controls;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        public delegate void ConnectionHandler(Context context);

        public AuthPage()
        {
            InitializeComponent();

            TokenTextBox.Focus();
        }

        public event ConnectionHandler Connect;

        async void authButton_Click(object sender, RoutedEventArgs e)
        {
            TokenErrorTextBlock.Text = string.Empty;
            try
            {
                var connection = ConnectionFactory.GetSandboxConnection(TokenTextBox.Text.Trim());
                var allegedStocks = await connection.Context.MarketStocksAsync();
                Connect?.Invoke(connection.Context);
            }
            catch (Exception)
            {
                TokenErrorTextBlock.Text = "* Token is invalid.";
                TokenTextBox.Focus();
            }
        }
    }
}