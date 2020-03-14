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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddTabButton_Selected(object sender, RoutedEventArgs e)
        {
            var newItem = new TabItem();
            newItem.Content = new RealTimeTrading();
            newItem.Header = string.Format("Tab {0}", tabControl.Items.Count);
            newItem.IsSelected = true;

            var s = sender as TabItem;
            s.IsSelected = false;

            e.Handled = true;
            tabControl.Items.Insert(tabControl.Items.Count - 1, newItem);
        }
    }
}
