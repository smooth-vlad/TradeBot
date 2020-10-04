using System.Windows;
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    ///     Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AuthPage.Connect += AuthPage_Connect;
        }

        public static Context Context { get; private set; }

        private void AuthPage_Connect(Context context)
        {
            Context = context;
            Content = new MainPage();
        }
    }
}