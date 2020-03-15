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

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Context Context { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            authPage.Connect += AuthPage_Connect;
        }

        private void AuthPage_Connect(Context context)
        {
            Context = context;
            Content = new MainPage(Context);
        }
    }
}
