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
using Tinkoff.Trading.OpenApi.Network;

namespace TradeBot
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private Context context;

        public MainPage(Context context)
        {
            InitializeComponent();
            this.context = context;

            AddStockSelectionTab();
        }

        private void AddStockSelectionTab()
        {
            var newItem = new TabItem();
            newItem.Content = new StockSelection(context, newItem);
            newItem.Header = "Instrument Selection";
            newItem.IsSelected = true;
            newItem.MouseRightButtonDown += Tab_MouseRightButtonDown;

            tabControl.Items.Insert(tabControl.Items.Count - 1, newItem);
        }

        private void AddTabButton_Selected(object sender, RoutedEventArgs e)
        {
            AddStockSelectionTab();

            var s = sender as TabItem;
            s.IsSelected = false;

            e.Handled = true;
        }

        private void MovingAverage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MovingAverageDialog();
            if (dialog.ShowDialog() == true)
            {
                var tab = tabControl.SelectedContent;
                if (tab.GetType() == typeof(RealTimeTrading))
                {
                    var tab1 = tab as RealTimeTrading;
                    tab1.tradingChart.AddIndicator(new MovingAverage(dialog.Period, dialog.Offset, dialog.Type));
                }
                else if (tab.GetType() == typeof(TestingTrading))
                {
                    var tab1 = tab as TestingTrading;
                    tab1.tradingChart.AddIndicator(new MovingAverage(dialog.Period, dialog.Offset, dialog.Type));
                }
            }
        }

        private void Tab_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tab = sender as TabItem;
            tabControl.Items.Remove(tab);
        }
    }
}
