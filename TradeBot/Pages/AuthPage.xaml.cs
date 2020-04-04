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
                var connection = ConnectionFactory.GetSandboxConnection(TokenTextBox.Text);
                var context = connection.Context;
                var allegedStocks = await context.MarketStocksAsync();
                Connect?.Invoke(context);
            }
            catch (Exception)
            {
                TokenErrorTextBlock.Text = "* Token is invalid.";
                TokenTextBox.Focus();
            }
        }

        void TokenTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (AuthButton != null)
                AuthButton.IsEnabled = !string.IsNullOrEmpty(tb.Text);
        }
    }
}